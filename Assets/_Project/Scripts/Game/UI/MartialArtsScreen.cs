using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>무공 tab with arts and inner arts; companion training opens the shared workspace. Learn arts with 두 once their stage is unlocked, then equip up to <see cref="Progression.MaxEquippedArts"/> for battle.</summary>
    public class MartialArtsScreen : MonoBehaviour
    {
        private sealed class Row
        {
            public ArtData Art;
            public Text Title, Detail, ButtonLabel, Tag;
            public Button Button;
            public Image ButtonImage, Icon;
            public Text IconGlyph;
            public RectTransform Chips;
        }

        private readonly List<Row> _rows = new List<Row>();
        private Text _header, _hint;
        private bool _built;
        private int _page;   // 0 arts, 1 inner arts
        private static readonly string[] PageNames = { "무공", "심법" };
        private readonly Image[] _pageTabs = new Image[PageNames.Length];
        private readonly GameObject[] _pageDots = new GameObject[PageNames.Length];
        private readonly Text[] _pageDotTexts = new Text[PageNames.Length];
        private readonly HashSet<string> _newArtIds = new HashSet<string>();   // arts flagged new when this page was opened (tagged in the list)
        private RectTransform _artsRoot, _slotBar;
        private InnerArtPanel _inner;

        public void Build(RectTransform tab)
        {
            if (_built) return;
            _built = true;

            // Page switcher on top; the page below is either the art list or an embedded panel.
            for (int p = 0; p < PageNames.Length; p++)
            {
                int page = p;
                float x0 = 0.04f + p * .47f;
                var btn = UiFactory.Button("Page" + p, tab, new Vector2(x0, .895f), new Vector2(x0 + .45f, .995f), PageNames[p], UiFactory.InkSoft, () => ShowPage(page), 30);
                _pageTabs[p] = btn.GetComponent<Image>();
                if (p < 2)
                {
                    var dot = UiFactory.Panel("NewDot", btn.transform, new Vector2(0.80f, 0.55f), new Vector2(.98f, .98f), UiFactory.Red);
                    _pageDotTexts[p] = UiFactory.Label("Count", dot.transform, Vector2.zero, Vector2.one, "", 30, UiFactory.PaperText);
                    _pageDots[p] = dot.gameObject;
                    _pageDots[p].SetActive(false);
                }
            }
            var root = UiFactory.Rect("ArtsPage", tab, Vector2.zero, new Vector2(.645f, .88f));
            _artsRoot = root;
            _inner = new InnerArtPanel(UiFactory.Rect("InnerArtPage", tab, Vector2.zero, new Vector2(1f, .88f)), Refresh) { Embedded = true };

            _header = UiFactory.TabHeader(root, "arts");
            UiFactory.HelpButton(root, () => { if (HelpPanel.Instance != null) HelpPanel.Instance.Show("mastery"); });   // right next to the 두 figure: the full 숙련·변초 help
            _hint = UiFactory.ScrollBody(UiFactory.Label("Hint", root, new Vector2(0.05f, .755f), new Vector2(0.95f, .84f), "", 28, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal), 30);

            // What is equipped right now: four art slots and the 필살기 slot.
            _slotBar = UiFactory.Rect("SlotBar", root, new Vector2(.04f, .625f), new Vector2(.96f, .745f));

            // The list scrolls: the protagonist has far more arts than fit on one screen.
            var viewport = UiFactory.Rect("Viewport", root, Vector2.zero, new Vector2(1f, .615f));
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UiFactory.Rect("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f));
            content.pivot = new Vector2(0.5f, 1f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            UiFactory.ScrollIndicator(scroll, viewport);

            var arts = EarlyGameContent.Arts.Where(a => a.OwnerId == null).ToArray();
            var grid = new ReadingGrid(content, 2, 440f);
            for (int i = 0; i < arts.Length; i++)
            {
                var art = arts[i];
                var row = new Row { Art = art };
                var rt = grid.Add("Card" + i);
                var bg = rt.GetComponent<Image>();
                var tap = rt.gameObject.AddComponent<Button>();   // tapping the row itself opens the detail window
                tap.targetGraphic = bg;
                tap.transition = Selectable.Transition.None;
                tap.onClick.AddListener(() => OpenDetail(art));
                row.Icon = UiFactory.Panel("Icon", rt, new Vector2(0f, 1f), new Vector2(0f, 1f), IconColor(art));
                row.Icon.rectTransform.anchorMax = row.Icon.rectTransform.anchorMin;
                row.Icon.rectTransform.pivot = new Vector2(0f, 1f);
                row.Icon.rectTransform.anchoredPosition = new Vector2(16f, -16f);
                row.Icon.rectTransform.sizeDelta = new Vector2(IconH, IconH);
                UiFactory.Frame(row.Icon.transform, UiFactory.Gold, 2f);
                row.IconGlyph = UiFactory.Label("Glyph", row.Icon.transform, Vector2.zero, Vector2.one, art.Name.Substring(0, 1), 50, UiFactory.PaperText);
                // Keep state tags on the icon; the title can wrap beside it.
                var tagBack = UiFactory.Panel("TagBack", row.Icon.transform, new Vector2(0f, 0f), new Vector2(1f, 0.26f), new Color(0f, 0f, 0f, 0.72f));
                tagBack.raycastTarget = false;
                row.Tag = UiFactory.Label("Tag", tagBack.transform, Vector2.zero, Vector2.one, "", 22, UiFactory.Gold);
                // Keep the title beside the large icon and put wrapped badges and the action underneath.
                row.Title = UiFactory.Body(UiFactory.Label("Title", rt, new Vector2(0f, 1f), new Vector2(0.96f, 1f), "", 30, UiFactory.PaperText, TextAnchor.MiddleLeft));
                row.Title.rectTransform.offsetMin = new Vector2(176f, -160f);
                row.Title.rectTransform.offsetMax = new Vector2(0f, -16f);
                row.Chips = UiFactory.Rect("Chips", rt, new Vector2(.04f, 1f), new Vector2(.96f, 1f));
                row.Chips.pivot = new Vector2(0f, 1f);
                row.Chips.offsetMin = new Vector2(0f, -334f);
                row.Chips.offsetMax = new Vector2(0f, -184f);
                row.Detail = UiFactory.Body(UiFactory.Label("Detail", rt, new Vector2(.04f, .25f), new Vector2(.96f, .56f), "", 28, UiFactory.Dim, TextAnchor.UpperLeft, FontStyle.Normal));
                row.Button = UiFactory.Button("Btn" + i, rt, new Vector2(.04f, 0f), new Vector2(.96f, 0f), "", UiFactory.Red, () => OnPressed(art), 28);
                row.Button.GetComponent<RectTransform>().offsetMin = new Vector2(0f, 18f);
                row.Button.GetComponent<RectTransform>().offsetMax = new Vector2(0f, 18f + ButtonH);
                row.ButtonImage = row.Button.GetComponent<Image>();
                row.ButtonLabel = row.Button.GetComponentInChildren<Text>();
                _rows.Add(row);
            }
            var placeholder = UiFactory.Panel("InspectorPlaceholder", root.parent, new Vector2(.66f, .02f), new Vector2(.985f, .88f), UiFactory.Ink);
            UiFactory.Frame(placeholder.transform, UiFactory.GoldDim, 2f);
            UiFactory.ScrollBody(UiFactory.Label("Hint", placeholder.transform, new Vector2(.06f, .30f), new Vector2(.94f, .70f), "무공을 선택하면 효과·숙련 단계·변초를 확인할 수 있습니다.", 30, UiFactory.Dim), 30);
            _detail = UiFactory.Rect("ArtDetail", tab, new Vector2(.66f, .02f), new Vector2(.985f, .88f));
            _detail.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);   // dims and blocks the list underneath
            _detail.gameObject.SetActive(false);
            ShowPage(0, false);
        }

        // Row parts keep one size: learning, equipping or a 변초 changes only the words, never the row or its buttons.
        private const float ButtonH = 72f, IconH = 144f;

        private RectTransform _detail;
        private string _detailId;

        private void CloseDetail()
        {
            _detailId = null;
            if (_detail != null) _detail.gameObject.SetActive(false);
        }

        /// <summary>Full description of one art: numbers with the current mastery applied, the mastery steps and the 변초.</summary>
        private void OpenDetail(ArtData art)
        {
            var save = GameManager.Instance.Save;
            _detailId = art.Id;
            LandscapeMenu.Clear(_detail);
            _detail.gameObject.SetActive(true);

            bool learned = save.LearnedArts.Contains(art.Id);
            bool equipped = save.EquippedArts.Contains(art.Id);
            bool unlocked = art.IsUnlocked(save);
            bool ultimate = Progression.IsUltimate(art);
            int level = Mastery.Level(save, art.Id);
            var variant = Mastery.VariantOf(save, art.Id);
            var spec = learned ? Mastery.Apply(save, art) : art.ToSpec();

            var card = UiFactory.Panel("Card", _detail, Vector2.zero, Vector2.one, new Color(0.05f, 0.07f, 0.11f, 0.99f));
            UiFactory.Frame(card.transform, ultimate ? UiFactory.Gold : UiFactory.GoldDim, 3f);
            var big = UiFactory.Panel("Picture", card.transform, new Vector2(0.66f, 0.84f), new Vector2(0.96f, 0.985f), IconColor(art));
            var picture = ArtLibrary.Art(art.Id);
            big.sprite = picture;
            big.preserveAspect = true;
            if (picture != null) big.color = Color.white;
            UiFactory.Frame(big.transform, UiFactory.Gold, 2f);
            if (picture == null) UiFactory.Label("Glyph", big.transform, Vector2.zero, Vector2.one, art.Name.Substring(0, 1), 60, UiFactory.PaperText);
            UiFactory.Label("Title", card.transform, new Vector2(0.05f, 0.91f), new Vector2(0.63f, 0.985f), art.Name + (variant != ArtVariant.None && learned ? "(" + Mastery.VariantName(variant) + ")" : ""), 40, UiFactory.PaperText, TextAnchor.MiddleLeft);
            string kind = (ultimate ? "비기 (기세 " + art.SpiritCost + ")" : art.AllEnemies ? "전체 무공" : art.Effect == SkillEffect.None ? "일반 무공" : "보조 무공")
                + (equipped ? "  ·  장착 중" : learned ? "  ·  배움" : "");
            // Command icon of the kind of use: 비기 → 필살기 slot, evade/guard → 방어, heal → 운기, otherwise → 공격.
            var kindIcon = ArtLibrary.Icon(ultimate ? "ultimate_slot" : art.Effect == SkillEffect.Evade || art.Effect == SkillEffect.GuardParty ? "cmd_guard" : art.Effect == SkillEffect.HealAlly ? "cmd_meditate" : "cmd_attack");
            float kindX = 0.05f;
            if (kindIcon != null)
            {
                var ki = UiFactory.Panel("KindIcon", card.transform, new Vector2(0.05f, 0.855f), new Vector2(0.115f, 0.915f), Color.white);
                ki.sprite = kindIcon;
                IconCutout.Apply(ki);
                ki.preserveAspect = true;
                kindX = 0.125f;
            }
            UiFactory.Label("Kind", card.transform, new Vector2(kindX, 0.86f), new Vector2(0.63f, 0.91f), kind, 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);

            var lines = new List<string>();
            lines.Add(art.Description);
            string cost = art.SpiritCost > 0 ? "기세 " + art.SpiritCost : spec.QiCost > 0 ? "내공 " + spec.QiCost : "내공 0";
            string effect = art.Effect == SkillEffect.Evade ? "다음 차례까지 공격을 " + Mathf.RoundToInt(spec.EvadeChance * 100f) + "% 확률로 회피"
                : art.Effect == SkillEffect.GuardParty ? "파티 전원 방어 태세"
                : art.Effect == SkillEffect.HealAlly ? "아군 체력 " + Mathf.RoundToInt(spec.HealPct * 100f) + "% 회복"
                : "위력 ×" + spec.Power.ToString("0.00") + (spec.Hits > 1 ? " ×" + spec.Hits + "타" : "") + (spec.AllEnemies ? " · 적 전체" : "") + (art.Effect == SkillEffect.Stun ? " · 점혈(굳음)" : "")
                + (art.Inflict != StatusKind.None ? " · " + Statuses.Name(art.Inflict) + " " + Mathf.RoundToInt(art.InflictChance * 100f) + "%" : "");
            lines.Add("<color=#9ADB9F>" + effect + "</color>\n" + cost + (spec.Cooldown > 0 ? " · 재사용 " + spec.Cooldown + "턴" : "") + " · 닿는 범위: " + SkillSpec.ReachText(art.EffectiveRange)
                + (learned && (variant != ArtVariant.None || level > 0) ? "\n(숙련·변초가 반영된 값입니다)" : ""));
            if (art.Inflict != StatusKind.None) lines.Add(Statuses.Describe(art.Inflict));
            if (!learned && !unlocked) lines.Add(art.UnlockText + "하면 배울 수 있습니다.");

            var steps = Mastery.LevelLines(art);
            if (steps.Count > 0)
            {
                var sb = new System.Text.StringBuilder("숙련 " + (learned ? "(" + Mastery.Uses(save, art.Id) + "회 사용)" : "") + "\n");
                for (int i = 0; i < steps.Count; i++) sb.Append(learned && i == level ? "▶ " : "   ").Append(steps[i]).Append(i < steps.Count - 1 ? "\n" : "");
                lines.Add(sb.ToString());
                var variants = Mastery.AvailableVariants(art);
                if (ultimate) lines.Add("비기는 변초가 없는 대신 숙련 위력 보너스가 큽니다 (5% · 10% · 20% · 40%).");
                else if (variants.Count > 0)
                    lines.Add("변초 (" + Mastery.LevelNames[Mastery.VariantLevel] + "부터)\n" + string.Join("\n", variants.Select(v => Mastery.VariantHint(v))));
                else lines.Add("이 무공에는 변초가 없습니다.");
            }
            UiFactory.ScrollBody(UiFactory.Label("Body", card.transform, new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.85f), string.Join("\n\n", lines), 30, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal), 36);

            string mainLabel; Color mainColor; bool mainOn;
            if (learned) { mainLabel = equipped ? "해제" : "장착"; mainColor = equipped ? UiFactory.InkSoft : UiFactory.Red; mainOn = true; }
            else if (!unlocked) { mainLabel = "잠김"; mainColor = UiFactory.InkSoft; mainOn = false; }
            else { mainOn = save.Doo >= art.LearnCost; mainLabel = "습득 " + art.LearnCost.ToString("N0") + "은자"; mainColor = mainOn ? UiFactory.Red : UiFactory.InkSoft; }
            var main = UiFactory.Button("Main", card.transform, new Vector2(0.05f, 0.19f), new Vector2(0.95f, 0.285f), mainLabel, mainColor, () => { OnPressed(art); OpenDetail(art); }, 34);
            main.interactable = mainOn;
            bool canVariant = learned && level >= Mastery.VariantLevel && Mastery.AvailableVariants(art).Count > 0;
            if (canVariant)
                UiFactory.Button("Variant", card.transform, new Vector2(0.05f, 0.10f), new Vector2(0.95f, 0.18f), "변초 바꾸기 (지금: " + (variant == ArtVariant.None ? "없음" : Mastery.VariantName(variant)) + ")", UiFactory.Gold, () => { OnMastery(art); OpenDetail(art); }, 30);
            UiFactory.Button("Close", card.transform, new Vector2(0.30f, 0.015f), new Vector2(0.70f, 0.09f), "닫기", UiFactory.InkSoft, CloseDetail, 34);
            LandscapeMenu.Inspector(card.rectTransform);
        }

        private void OnEnable() { if (_built) ShowPage(_page); }
        private void OnDisable()
        {
            if (!_built) return;
            CloseDetail();
        }

        private void RefreshDots()
        {
            var save = GameManager.Instance.Save;
            int[] counts = { Discovery.NewArts(save).Count, Discovery.NewInner(save).Count };
            for (int i = 0; i < counts.Length; i++)
            {
                _pageDots[i].SetActive(counts[i] > 0);
                _pageDotTexts[i].text = counts[i].ToString();
            }
        }

        /// <param name="seen">true when the page is really being looked at: its red dot is cleared.</param>
        private void ShowPage(int page, bool seen = true)
        {
            _page = page;
            CloseDetail();
            var save = GameManager.Instance.Save;
            if (seen && page == 0)
            {
                _newArtIds.Clear();
                foreach (var a in Discovery.NewArts(save)) _newArtIds.Add(a.Id);
                Discovery.MarkArtsSeen(save);
                GameManager.Instance.SaveNow();
            }
            else if (seen && page == 1)
            {
                _inner.NewIds.Clear();
                foreach (var a in Discovery.NewInner(save)) _inner.NewIds.Add(a.Id);
                Discovery.MarkInnerSeen(save);
                GameManager.Instance.SaveNow();
            }
            RefreshDots();
            _artsRoot.gameObject.SetActive(page == 0);
            _detail.transform.parent.Find("InspectorPlaceholder").gameObject.SetActive(page == 0);
            _inner.Hide();
            if (page == 0) Refresh();
            else if (page == 1) _inner.Open();
            for (int i = 0; i < _pageTabs.Length; i++) _pageTabs[i].color = i == page ? new Color(0.56f, 0.44f, 0.22f, 1f) : UiFactory.InkSoft;
        }

        private void OnPressed(ArtData art)
        {
            var save = GameManager.Instance.Save;
            if (!save.LearnedArts.Contains(art.Id))
            {
                if (!art.IsUnlocked(save) || save.Doo < art.LearnCost) return;
                save.Doo -= art.LearnCost;
                save.LearnedArts.Add(art.Id);
                string learnedReplaced;
                Progression.EquipArt(save, art, out learnedReplaced);
                _hint.text = art.Name + " 습득!";
            }
            else if (save.EquippedArts.Contains(art.Id))
            {
                save.EquippedArts.Remove(art.Id);
                _hint.text = art.Name + " 장착 해제";
            }
            else
            {
                string replaced;
                if (Progression.EquipArt(save, art, out replaced))
                {
                    var old = replaced != null ? EarlyGameContent.FindArt(replaced) : null;
                    _hint.text = art.Name + " 장착" + (old != null ? " (필살기 " + old.Name + " 해제)" : "");
                }
                else _hint.text = "장착 한도(" + Progression.MaxEquippedArts + "개) 초과 — 먼저 하나를 해제하세요.";
            }
            GameManager.Instance.SaveNow();
            Refresh();
        }

        /// <summary>Icon colour by kind of art: 비기 gold, area purple, evade/guard blue, heal green, stun teal, otherwise red.</summary>
        private static Color IconColor(ArtData a)
        {
            if (a.SpiritCost > 0) return new Color(0.62f, 0.44f, 0.12f, 1f);
            if (a.AllEnemies) return new Color(0.38f, 0.22f, 0.55f, 1f);
            if (a.Effect == SkillEffect.Evade || a.Effect == SkillEffect.GuardParty) return new Color(0.18f, 0.34f, 0.58f, 1f);
            if (a.Effect == SkillEffect.HealAlly) return new Color(0.20f, 0.46f, 0.30f, 1f);
            if (a.Effect == SkillEffect.Stun) return new Color(0.16f, 0.46f, 0.50f, 1f);
            return new Color(0.55f, 0.18f, 0.18f, 1f);
        }

        private static string Stars(int level)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < Mastery.Thresholds.Length; i++) sb.Append(i <= level ? "★" : "☆");
            return sb.ToString();
        }

        private void OnMastery(ArtData art)
        {
            var save = GameManager.Instance.Save;
            if (!save.LearnedArts.Contains(art.Id)) return;
            if (Mastery.Level(save, art.Id) < Mastery.VariantLevel)
            {
                _hint.text = art.Name + ": " + Mastery.LevelNames[Mastery.VariantLevel] + "(" + Mastery.Thresholds[Mastery.VariantLevel] + "회 사용)에 변초가 열립니다.";
            }
            else if (Mastery.AvailableVariants(art).Count == 0)
            {
                _hint.text = art.Name + "은(는) 변초가 없는 무공입니다.";
            }
            else
            {
                var v = Mastery.CycleVariant(save, art);
                _hint.text = art.Name + " — " + Mastery.VariantHint(v);
                GameManager.Instance.SaveNow();
            }
            Refresh();
        }

        private static readonly Color ChipPower = new Color(0.50f, 0.20f, 0.17f, 1f), ChipArea = new Color(0.36f, 0.22f, 0.50f, 1f),
            ChipSupport = new Color(0.18f, 0.38f, 0.42f, 1f), ChipStatus = new Color(0.55f, 0.36f, 0.12f, 1f),
            ChipReach = new Color(0.17f, 0.30f, 0.50f, 1f), ChipQi = new Color(0.20f, 0.27f, 0.36f, 1f),
            ChipSpirit = new Color(0.52f, 0.42f, 0.16f, 1f), ChipCooldown = new Color(0.26f, 0.28f, 0.30f, 1f);

        /// <summary>Wrapped badges for the effect (power, hits, area, status), followed by reach, cost and cooldown. Mastery and 변초 are applied when learned.</summary>
        private static void BuildChips(RectTransform parent, ArtData a, SkillSpec spec)
        {
            const float gap = 8f, line = 50f;
            float x = 0f, y = 0f;
            System.Action<string, Color> add = (text, color) =>
            {
                float width = UiFactory.Chip(parent, x, y, text, color);
                var chip = (RectTransform)parent.GetChild(parent.childCount - 1);
                if (x > 0f && x + width > parent.rect.width)
                {
                    x = 0f; y -= line;
                    chip.anchoredPosition = new Vector2(x, y);
                }
                x += width + gap;
            };
            if (a.Effect == SkillEffect.Evade) add("회피 " + Mathf.RoundToInt(spec.EvadeChance * 100f) + "%", ChipSupport);
            else if (a.Effect == SkillEffect.GuardParty) add("파티 방어", ChipSupport);
            else if (a.Effect == SkillEffect.HealAlly) add("치유 " + Mathf.RoundToInt(spec.HealPct * 100f) + "%", ChipSupport);
            else
            {
                add("위력 ×" + spec.Power.ToString("0.0#"), ChipPower);
                if (spec.Hits > 1) add(spec.Hits + "타", ChipPower);
                if (spec.AllEnemies) add("적 전체", ChipArea);
                if (a.Effect == SkillEffect.Stun) add("점혈", ChipSupport);
            }
            if (a.Inflict != StatusKind.None) add(Statuses.Name(a.Inflict) + " " + Mathf.RoundToInt(a.InflictChance * 100f) + "%", ChipStatus);

            x = 0f; y -= line;
            System.Action<string, Color> add2 = add;
            add2(SkillSpec.ReachText(a.EffectiveRange), ChipReach);
            if (a.SpiritCost > 0) add2("기세 " + a.SpiritCost, ChipSpirit);
            else add2("내공 " + spec.QiCost, ChipQi);
            if (spec.Cooldown > 0) add2("재사용 " + spec.Cooldown + "턴", ChipCooldown);
        }

        /// <summary>The equipped arts as four slots plus the 필살기 slot; tapping a filled slot opens that art.</summary>
        private void RefreshSlots(SaveData save)
        {
            LandscapeMenu.Clear(_slotBar);
            var equipped = save.EquippedArts.Select(EarlyGameContent.FindArt).Where(a => a != null).ToList();
            var normal = equipped.Where(a => !Progression.IsUltimate(a)).Take(Progression.MaxEquippedArts).ToList();
            var ultimate = equipped.FirstOrDefault(Progression.IsUltimate);
            int slots = Progression.MaxEquippedArts + 1;
            const float gap = .012f;
            float w = (1f - gap * (slots - 1)) / slots;
            for (int i = 0; i < slots; i++)
            {
                bool ult = i == slots - 1;
                var art = ult ? ultimate : i < normal.Count ? normal[i] : null;
                float x0 = i * (w + gap);
                var slot = UiFactory.Panel("Slot" + i, _slotBar, new Vector2(x0, 0f), new Vector2(x0 + w, 1f), UiFactory.Ink);
                UiFactory.Frame(slot.transform, ult ? UiFactory.Gold : UiFactory.GoldDim, 2f);
                if (art != null)
                {
                    var tap = slot.gameObject.AddComponent<Button>();
                    tap.targetGraphic = slot;
                    tap.transition = Selectable.Transition.None;
                    var target = art;
                    tap.onClick.AddListener(() => OpenDetail(target));
                    var picture = ArtLibrary.Art(art.Id);
                    var icon = UiFactory.Panel("Icon", slot.transform, new Vector2(.04f, .12f), new Vector2(.32f, .88f), picture != null ? Color.white : IconColor(art));
                    icon.sprite = picture;
                    icon.preserveAspect = true;
                    if (picture == null) UiFactory.Label("Glyph", icon.transform, Vector2.zero, Vector2.one, art.Name.Substring(0, 1), 30, UiFactory.PaperText);
                }
                string name = art != null ? art.Name : ult ? "필살기 빈 칸" : "빈 칸";
                var label = UiFactory.Body(UiFactory.Label("Name", slot.transform, new Vector2(art != null ? .35f : .05f, .05f), new Vector2(.97f, .95f),
                    (ult ? "<color=#D4B87A>필살</color>\n" : "") + name, 22, art != null ? UiFactory.PaperText : UiFactory.Dim, art != null ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter, FontStyle.Normal), 1.05f);
                label.resizeTextForBestFit = true;   // long names shrink inside the fixed slot instead of spilling
                label.resizeTextMinSize = 16;
            }
        }

        private void Refresh()
        {
            if (_page != 0) return;   // an embedded panel redraws itself
            var save = GameManager.Instance.Save;
            Canvas.ForceUpdateCanvases();
            RefreshSlots(save);
            _header.text = "무공 · 장착 " + Progression.EquippedCount(save, false) + "/" + Progression.MaxEquippedArts + " · 필살 " + Progression.EquippedCount(save, true) + "/" + Progression.MaxEquippedUltimates + " · " + save.Doo.ToString("N0") + "은자";

            foreach (var r in _rows)
            {
                var a = r.Art;
                bool learned = save.LearnedArts.Contains(a.Id);
                bool equipped = save.EquippedArts.Contains(a.Id);
                bool unlocked = a.IsUnlocked(save);

                int mLevel = Mastery.Level(save, a.Id);
                var variant = Mastery.VariantOf(save, a.Id);
                var picture = ArtLibrary.Art(a.Id);
                r.Icon.sprite = picture;
                r.Icon.preserveAspect = true;
                r.IconGlyph.text = picture != null ? "" : a.Name.Substring(0, 1);
                r.Icon.color = picture != null ? ((learned || unlocked) ? Color.white : new Color(0.35f, 0.35f, 0.38f, 1f))
                    : (learned || unlocked) ? IconColor(a) : new Color(0.2f, 0.2f, 0.22f, 1f);
                r.Title.text = a.Name + (variant != ArtVariant.None ? "(" + Mastery.VariantName(variant) + ")" : "")
                    + (learned ? "  <size=24><color=#D4B87A>" + Stars(mLevel) + " " + Mastery.LevelNames[mLevel] + "</color></size>" : "");
                r.Tag.text = equipped ? "장착 중" : _newArtIds.Contains(a.Id) && !learned ? "<color=#FF6A5A>새 무공</color>" : "";
                r.Tag.transform.parent.gameObject.SetActive(r.Tag.text.Length > 0);
                bool open = learned || unlocked;
                r.Detail.text = open ? "" : a.UnlockText + "하면 열립니다";
                LandscapeMenu.Clear(r.Chips);
                if (open) BuildChips(r.Chips, a, learned ? Mastery.Apply(save, a) : a.ToSpec());

                if (learned)
                {
                    r.ButtonLabel.text = equipped ? "해제" : "장착";
                    r.ButtonImage.color = equipped ? UiFactory.InkSoft : UiFactory.Red;
                    r.Button.interactable = true;
                }
                else if (!unlocked)
                {
                    r.ButtonLabel.text = "잠김";
                    r.ButtonImage.color = UiFactory.InkSoft;
                    r.Button.interactable = false;
                }
                else
                {
                    bool can = save.Doo >= a.LearnCost;
                    r.ButtonLabel.text = "습득 " + a.LearnCost.ToString("N0") + "은자";
                    r.ButtonImage.color = can ? UiFactory.Red : UiFactory.InkSoft;
                    r.Button.interactable = can;
                }
            }
        }
    }
}
