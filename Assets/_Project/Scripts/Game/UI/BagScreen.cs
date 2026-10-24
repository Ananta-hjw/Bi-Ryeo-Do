using System;
using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Three-column inventory. Item actions live in the detail window.</summary>
    public class BagScreen : MonoBehaviour
    {
        private const int Columns = 7;   // compact cards (icon, name, stars, count); effects are in the inspector
        private const float RowGap = 16f;

        private sealed class Row
        {
            public GameObject Band;
            public Text Title, Detail;
            public Image Icon;
            public Text IconGlyph;
            public string ItemId;
        }

        private readonly List<Row> _rows = new List<Row>();
        private Text _header, _hint, _empty;
        private string _flash;
        private BulkDismantlePanel _bulkDismantle;
        private BulkFusePanel _bulkFuse;
        private RectTransform _content;
        private int _filter = 1;                       // 0 all, 1 gear, 2 consumables, 3 materials & keys
        private readonly Image[] _filterTabs = new Image[4];
        private int _gearFilter;                   // weapon, armor, pants, boots, accessory
        private readonly Image[] _gearTabs = new Image[5];        // underline of each 부위 sub-tab (shown on the picked one)
        private readonly Text[] _gearTabLabels = new Text[5];
        private static readonly string[] GearFilterNames = { "무기", "상의", "하의", "신발", "장신구" };
        private RectTransform _viewport;
        private GameObject _gearTabRow;
        private static readonly string[] FilterNames = { "전체", "장비", "소모품", "재료" };
        private bool _built;
        private bool _layoutDirty;
        private float _layoutWidth = -1f;
        private ScrollRect _scroll;
        private int _visibleCount;

        public void Build(RectTransform root)
        {
            if (_built) return;
            _built = true;

            _header = UiFactory.TabHeader(root, "bag", "", .54f);
            var fuseButton = UiFactory.Button("BulkFuseButton", root, new Vector2(.565f, .90f), new Vector2(.695f, .975f), "일괄 합성", new Color(0.55f, 0.36f, 0.14f, 1f), () =>
            {
                CloseDetail();
                _bulkFuse.Show();
            }, 28);
            UiFactory.CenterInHeader((RectTransform)fuseButton.transform, 72f);
            var bulkButton = UiFactory.Button("BulkDismantleButton", root, new Vector2(.71f, .90f), new Vector2(.84f, .975f), "일괄 분해", UiFactory.Red, () =>
            {
                CloseDetail();
                _bulkDismantle.Show();
            }, 28);
            UiFactory.CenterInHeader((RectTransform)bulkButton.transform, 72f);
            UiFactory.HelpButton(root, () => { if (HelpPanel.Instance != null) HelpPanel.Instance.Show("gear"); });   // right next to the 두 figure: the full 돌파·분해·강화 help
            _hint = UiFactory.ScrollBody(UiFactory.Label("Hint", root, new Vector2(.025f, .005f), new Vector2(.635f, .075f), "", 30, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal), 30);


            // Filter tabs
            for (int t = 0; t < FilterNames.Length; t++)
            {
                int tab = t;
                float x0 = .025f + t * .155f;
                var btn = UiFactory.Button("Filter" + t, root, new Vector2(x0, .75f), new Vector2(x0 + .145f, .855f), FilterNames[t], UiFactory.InkSoft, () => { _filter = tab; CloseDetail(); Refresh(); _scroll.verticalNormalizedPosition = 1f; }, 28);
                _filterTabs[t] = btn.GetComponent<Image>();
            }

            // Second row, only on the 장비 tab: weapon / 상의 / 하의 / 신발 / accessory.
            // Drawn as a slimmer, indented strip of text tabs with a gold underline, so it reads as a sub-menu of 장비, not as more filter buttons.
            var strip = UiFactory.Panel("GearTabs", root, new Vector2(.045f, .655f), new Vector2(.635f, .728f), new Color(0.06f, 0.09f, 0.10f, 0.85f));
            _gearTabRow = strip.gameObject;
            UiFactory.Panel("Rule", strip.transform, Vector2.zero, new Vector2(1f, .04f), UiFactory.GoldDim);
            for (int t = 0; t < GearFilterNames.Length; t++)
            {
                int tab = t;
                float x0 = t * .2f;
                var btn = UiFactory.Button("GearFilter" + t, strip.transform, new Vector2(x0, 0f), new Vector2(x0 + .2f, 1f), GearFilterNames[t], Color.clear, () => { _gearFilter = tab; CloseDetail(); Refresh(); _scroll.verticalNormalizedPosition = 1f; }, 26);
                _gearTabLabels[t] = btn.GetComponentInChildren<Text>();
                _gearTabs[t] = UiFactory.Panel("Underline", btn.transform, new Vector2(.18f, 0f), new Vector2(.82f, .1f), UiFactory.Gold);
                _gearTabs[t].raycastTarget = false;
            }

            // The bag scrolls: it can hold more stacks than fit on one screen.
            var viewport = UiFactory.Rect("Viewport", root, new Vector2(.01f, .085f), new Vector2(.645f, .62f));
            _viewport = viewport;
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            _content = UiFactory.Rect("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f));
            _content.pivot = new Vector2(0.5f, 1f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            _scroll = scroll;
            scroll.viewport = viewport;
            scroll.content = _content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            UiFactory.ScrollIndicator(scroll, viewport);
            _empty = UiFactory.Label("Empty", viewport, new Vector2(.06f, .4f), new Vector2(.94f, .7f), "이 분류에 보유한 아이템이 없습니다.", 32, UiFactory.Dim);

            var placeholder = UiFactory.Panel("InspectorPlaceholder", root, new Vector2(.66f, .02f), new Vector2(.985f, .855f), UiFactory.Ink);
            UiFactory.Frame(placeholder.transform, UiFactory.GoldDim, 2f);
            UiFactory.ScrollBody(UiFactory.Label("Hint", placeholder.transform, new Vector2(.06f, .30f), new Vector2(.94f, .72f), "물품을 선택해 주세요.\n\n상세 효과·한계돌파·분해·합성은 이곳에서 확인합니다.\n장착과 강화는 협객의 장비 탭에서 합니다.", 30, UiFactory.Dim), 30);
            _detail = UiFactory.Rect("ItemDetail", root, new Vector2(.66f, .02f), new Vector2(.985f, .855f));
            _detail.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);   // dims and blocks the list underneath
            _detail.gameObject.SetActive(false);
            _bulkDismantle = new BulkDismantlePanel(root, (title, body) => ShowNotice(title, body, UiFactory.Gold), Refresh);
            _bulkFuse = new BulkFusePanel(root, (title, body) => ShowNotice(title, body, UiFactory.Gold), Refresh);
            Refresh();
        }

        // ------------------------------------------------------------------ item detail window

        private RectTransform _detail;
        private string _detailId;
        private string _confirmDismantle;   // an enhanced last copy waiting for a second press of 분해

        private void OpenDetail(string itemId)
        {
            var item = string.IsNullOrEmpty(itemId) ? null : EarlyGameContent.FindItem(itemId);
            if (item == null) return;
            var save = GameManager.Instance.Save;
            if (itemId != _detailId) _confirmDismantle = null;
            _detailId = itemId;
            LandscapeMenu.Clear(_detail);
            _detail.gameObject.SetActive(true);

            bool gear = item.IsGear;
            int enh = gear ? Gear.Stars(save, item.Id) : 0;
            var card = UiFactory.Panel("Card", _detail, Vector2.zero, Vector2.one, new Color(0.05f, 0.07f, 0.11f, 0.99f));
            UiFactory.Frame(card.transform, UiFactory.GradeColor(item.Grade), 3f);
            var big = UiFactory.Panel("Picture", card.transform, new Vector2(0.66f, 0.82f), new Vector2(0.96f, 0.985f), Color.white);
            UiFactory.Frame(big.transform, UiFactory.GradeColor(item.Grade), 2f);
            var bigGlyph = UiFactory.Label("Glyph", big.transform, Vector2.zero, Vector2.one, "", 60, UiFactory.PaperText);
            ItemArt.Apply(big, bigGlyph, item);
            UiFactory.Label("Title", card.transform, new Vector2(0.05f, 0.90f), new Vector2(0.63f, 0.98f), UiFactory.GradedName(item) + (gear ? " · " + Gear.StarsText(save, item.Id) : ""), 40, UiFactory.PaperText, TextAnchor.MiddleLeft);
            string kind = KindName(item) + (gear && Gear.IsHeroOnly(item.Id) ? "  ·  " + Gear.HeroOnlyLabel : "") + (gear && Inventory.IsEquipped(save, item.Id) ? "  ·  착용 중" : "") + (gear && Inventory.WearingCompanions(save, item.Id).Count > 0 ? "  ·  동료 " + string.Join(",", Inventory.WearingCompanions(save, item.Id)) : "") + (item.Kind == ItemKind.Consumable || item.Kind == ItemKind.Material ? "  ·  보유 " + Inventory.Count(save, item.Id) + "개" : "");
            UiFactory.Label("Kind", card.transform, new Vector2(0.05f, 0.84f), new Vector2(0.63f, 0.90f), kind, 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);

            var lines = new List<string>();
            if (gear) lines.Add("강화 · 장착 · 해제는 장비 탭의 부위 칸에서 합니다.\n\n<color=#9ADB9F>" + UiFactory.GearEffectText(item, 0, enh) + "</color>" + (enh > 0 && item.Grade != ItemGrade.Transcendent ? "   (돌파 전 " + UiFactory.GearEffectText(item, 0) + ")" : ""));
            else
            {
                var fx = new List<string>();
                if (item.HealPct > 0f) fx.Add("체력 " + Mathf.RoundToInt(item.HealPct * 100f) + "% 회복");
                if (item.QiRestore > 0) fx.Add("내공 " + (item.QiRestore >= 999 ? "가득" : "+" + item.QiRestore));
                if (item.SpiritGain > 0) fx.Add("기세 +" + item.SpiritGain);
                if (item.MasteryUses > 0) fx.Add("배운 무공 하나의 숙련 +" + item.MasteryUses + "회");
                if (fx.Count > 0) lines.Add("<color=#9ADB9F>" + string.Join("  ", fx) + "</color>");
                if (item.Kind == ItemKind.Material && item.SellValue > 0) lines.Add("판매가 " + Income.Boost(item.SellValue).ToString("N0") + "두 (1개)");
            }
            string flavor = StatusSheet.Flavor(item);
            if (flavor.Length > 0) lines.Add(flavor);
            if (gear)
            {
                int spares = Gear.Spares(save, item.Id);
                lines.Add("한계돌파 " + Gear.StarsText(save, item.Id) + " / 5성 · 강화 상한 +" + Gear.Cap(save, item.Id)
                    + "\n기본 장비 효과 +" + Mathf.RoundToInt(Gear.StarBonusFor(item, enh) * 100f) + "% · 여분 " + spares + "개"
                    + (enh < Gear.MaxStars ? "\n다음: " + (enh + 1) + "성 · 상한 +" + Gear.LevelCaps[enh + 1] + " · 기본 효과 +" + Mathf.RoundToInt(Gear.StarBonusFor(item, enh + 1) * 100f) + "% (여분 1개 사용)" : "\n최대 한계돌파입니다."));
                string traits = UiFactory.GearTraitsText(item, enh);
                if (traits.Length > 0) lines.Add("돌파 추가 능력치 (강화·기본 효과 보너스 미적용)\n" + traits);
                var matInfo = Salvage.MaterialFor(item);
                if (matInfo != null) lines.Add("분해: " + matInfo.Name + " " + Salvage.MinCount[(int)item.Grade] + "~" + Salvage.MaxCount(item.Grade) + "개 (" + Grades.NameOf(item.Grade) + " " + Salvage.SlotLabel(item.Kind) + " 재료)\n" + Salvage.EnhancementMaterialFor(item).Name + " 1~5개 (각 20%)");
                var set = Gear.SetOf(item.Id);
                lines.Add(set != null ? Gear.SetDetail(set, save.EquippedGear, save) : "세트 없음");
            }
            UiFactory.ScrollBody(UiFactory.Label("Body", card.transform, new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.83f), string.Join("\n\n", lines), 28, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal), 36);

            if (gear)
            {
                bool canEnh = Gear.CanBreakthrough(save, item.Id);
                string enhLabel = enh >= Gear.MaxStars ? "최대 5성" : "한계돌파 " + (enh + 1) + "성\n(여분 1개 필요)";
                var enhBtn = UiFactory.Button("Breakthrough", card.transform, new Vector2(0.05f, 0.14f), new Vector2(0.49f, 0.24f), enhLabel, canEnh ? UiFactory.Red : UiFactory.InkSoft, () => { DoBreakthrough(item); }, 24);
                enhBtn.interactable = canEnh;
                bool canDis = Salvage.CanDismantle(save, item.Id);
                var disBtn = UiFactory.Button("Dismantle", card.transform, new Vector2(0.51f, 0.14f), new Vector2(0.95f, 0.24f), canDis ? "분해\n(재료 " + (item.Grade <= ItemGrade.Legendary ? Salvage.MinCount[(int)item.Grade] + "~" + Salvage.MaxCount(item.Grade) : "?") + "개)" : "분해 불가", canDis ? new Color(0.55f, 0.36f, 0.14f, 1f) : UiFactory.InkSoft, () => { DoDismantle(item); }, 24);
                disBtn.interactable = canDis;
            }
            else if (item.MasteryUses > 0)
            {
                UiFactory.Button("Read", card.transform, new Vector2(0.05f, 0.13f), new Vector2(0.95f, 0.22f), "무공 고르기 (숙련 +" + item.MasteryUses + "회)", UiFactory.Red, () => { DoMain(item); }, 32);
            }
            else if (Gear.NextMaterial(item.Id) != null)
            {
                var next = Gear.NextMaterial(item.Id);
                bool ready = Gear.CanFuseMaterial(save, item.Id);
                var fuseBtn = UiFactory.Button("FuseMaterial", card.transform, new Vector2(0.05f, 0.13f), new Vector2(0.95f, 0.22f),
                    "합성 " + Inventory.Count(save, item.Id) + " / 3 → " + next.Name + " 1개", ready ? UiFactory.Red : UiFactory.InkSoft, () => DoFuseMaterial(item), 28);
                fuseBtn.interactable = ready;
            }
            else if (Salvage.IsFusable(item))
            {
                int have = Inventory.Count(save, item.Id), need = Salvage.Need(item);
                var next = Grades.NameOf(item.Grade + 1);
                bool ready = have >= need;
                var fuseBtn = UiFactory.Button("Fuse", card.transform, new Vector2(0.05f, 0.13f), new Vector2(0.95f, 0.22f), "합성  " + have + " / " + need + "  →  " + next + " " + Salvage.SlotLabel(Salvage.SlotOf(item)) + " 무작위", ready ? UiFactory.Red : UiFactory.InkSoft, () => { DoFuse(item); }, 28);
                fuseBtn.interactable = ready;
            }
            else if (item.Kind == ItemKind.Material && item.SellValue > 0)
            {
                int count = Inventory.Count(save, item.Id);
                UiFactory.Button("Sell", card.transform, new Vector2(0.05f, 0.13f), new Vector2(0.95f, 0.22f), "전부 판매 +" + (Income.Boost(item.SellValue) * count).ToString("N0") + "두", UiFactory.Red, () => { DoMain(item); }, 32);
            }
            else if (item.Kind == ItemKind.Material)
            {
                var blocked = UiFactory.Button("Sell", card.transform, new Vector2(0.05f, 0.13f), new Vector2(0.95f, 0.22f), "판매 불가", new Color(0.30f, 0.30f, 0.33f, 1f), null, 32);
                blocked.interactable = false;
            }
            UiFactory.Button("Close", card.transform, new Vector2(0.30f, 0.02f), new Vector2(0.70f, 0.10f), "닫기", UiFactory.InkSoft, CloseDetail, 34);
            LandscapeMenu.Inspector(card.rectTransform);
        }

        private void CloseDetail()
        {
            _detailId = null;
            _detail.gameObject.SetActive(false);
        }

        private static string KindName(ItemData item)
        {
            switch (item.Kind)
            {
                case ItemKind.Weapon: return "무기";
                case ItemKind.Armor: return "상의";
                case ItemKind.Pants: return "하의";
                case ItemKind.Boots: return "신발";
                case ItemKind.Accessory: return "장신구";
                case ItemKind.Consumable: return item.MasteryUses > 0 ? "수련서 (행낭에서 사용)" : "소모품 (전투 중 사용)";
                case ItemKind.Material: return "재료";
                default: return "중요 물품";
            }
        }

        private void OnDisable()
        {
            if (!_built) return;
            if (_detail != null) CloseDetail();
            if (_bulkDismantle != null) _bulkDismantle.Hide();
            if (_bulkFuse != null) _bulkFuse.Hide();
            if (_notice != null) _notice.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (!_built) return;
            Refresh();
            var save = GameManager.Instance.Save;
            if (HelpPanel.Instance != null && !HelpPanel.Instance.IsOpen && Tutorial.IsDone(save, "intro") && !Tutorial.IsDone(save, "gear"))
                HelpPanel.Instance.Show("gear", true);
        }

        /// <summary>Use a training scroll or sell a material stack from item details.</summary>
        private void DoMain(ItemData item)
        {
            var save = GameManager.Instance.Save;

            if (item.MasteryUses > 0)
            {
                OpenScrollPicker(item);   // the art is chosen there; nothing else to refresh yet
                return;
            }
            else if (Gear.NextMaterial(item.Id) != null)
            {
                DoFuseMaterial(item);
                return;
            }
            else if (Salvage.IsFusable(item))
            {
                DoFuse(item);
                return;
            }
            else if (item.Kind == ItemKind.Material && item.SellValue > 0)
            {
                int count = Inventory.Count(save, item.Id);
                long gold = Inventory.Sell(save, item.Id, count);
                _flash = item.Name + " " + count + "개를 팔아 " + gold.ToString("N0") + "두를 얻었습니다.";
            }
            GameManager.Instance.SaveNow();
            Refresh();
            ReopenDetail(item);
        }

        /// <summary>Lists the learned arts; tapping one reads the 수련서 into it.</summary>
        private void OpenScrollPicker(ItemData scroll)
        {
            var save = GameManager.Instance.Save;
            _detailId = scroll.Id;
            LandscapeMenu.Clear(_detail);
            _detail.gameObject.SetActive(true);
            var card = UiFactory.Panel("Card", _detail, Vector2.zero, Vector2.one, new Color(0.05f, 0.07f, 0.11f, 0.99f));
            UiFactory.Frame(card.transform, UiFactory.GradeColor(scroll.Grade), 3f);
            UiFactory.Label("Title", card.transform, new Vector2(0.05f, 0.91f), new Vector2(0.95f, 0.99f), UiFactory.GradedName(scroll), 40, UiFactory.PaperText, TextAnchor.MiddleLeft);
            UiFactory.ScrollBody(UiFactory.Label("Hint", card.transform, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.91f), "숙련을 +" + scroll.MasteryUses + "회 올릴 무공을 하나 고르세요. (보유 " + Inventory.Count(save, scroll.Id) + "개)", 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal), 30);

            var arts = Mastery.ScrollTargets(save);
            var viewport = UiFactory.Rect("PickViewport", card.transform, new Vector2(0.03f, 0.13f), new Vector2(0.97f, 0.83f));
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UiFactory.Rect("PickContent", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f));
            content.pivot = new Vector2(0.5f, 1f);
            const float rowH = 104f, gap = 10f;
            content.sizeDelta = new Vector2(0f, arts.Count * (rowH + gap) + gap);
            var scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 40f;
            UiFactory.ScrollIndicator(scrollRect, viewport);
            for (int i = 0; i < arts.Count; i++)
            {
                var art = arts[i];
                int lv = Mastery.Level(save, art.Id);
                int toNext = Mastery.UsesToNext(save, art.Id);
                string progress = Mastery.LevelNames[lv] + "  ·  " + Mastery.Uses(save, art.Id) + "회" + (toNext >= 0 ? " (다음 단계까지 " + toNext + "회)" : " (최고 단계)");
                var rt = UiFactory.Rect("Pick" + i, content, new Vector2(0.02f, 1f), new Vector2(0.98f, 1f));
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -(gap + i * (rowH + gap)));
                rt.sizeDelta = new Vector2(0f, rowH);
                ReadingList.Register(rt, rowH, gap);
                string artId = art.Id;
                var btn = UiFactory.Button("PickBtn" + i, rt, Vector2.zero, Vector2.one, "", UiFactory.Ink, () => ApplyScroll(scroll, artId), 28);
                UiFactory.Frame(btn.transform, UiFactory.GoldDim, 2f);
                var pic = ArtLibrary.Art(art.Id);
                float textX = 0.04f;
                if (pic != null)
                {
                    var pi = UiFactory.Panel("Icon", btn.transform, new Vector2(0.02f, 0.08f), new Vector2(0.14f, 0.92f), Color.white);
                    pi.sprite = pic; pi.preserveAspect = true;
                    textX = 0.16f;
                }
                UiFactory.Label("Name", btn.transform, new Vector2(textX, 0.5f), new Vector2(0.96f, 0.98f), art.Name + (Progression.IsUltimate(art) ? "  (비기)" : ""), 32, UiFactory.PaperText, TextAnchor.MiddleLeft);
                UiFactory.Label("Progress", btn.transform, new Vector2(textX, 0.02f), new Vector2(0.96f, 0.5f), progress, 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
            }
            if (arts.Count == 0) UiFactory.Label("None", card.transform, new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.6f), "배운 무공이 없습니다.", 32, UiFactory.Dim);
            UiFactory.Button("Close", card.transform, new Vector2(0.30f, 0.02f), new Vector2(0.70f, 0.10f), "취소", UiFactory.InkSoft, () => OpenDetail(scroll.Id), 34);
        }

        private void ApplyScroll(ItemData scroll, string artId)
        {
            var save = GameManager.Instance.Save;
            bool used;
            _flash = Mastery.UseScroll(save, scroll.Id, artId, out used);
            if (used) { SoundManager.Sfx("levelup"); GameManager.Instance.SaveNow(); }
            Refresh();
            ReopenDetail(scroll);
        }

        private void ReopenDetail(ItemData item)
        {
            if (_detailId != item.Id) return;
            if (Inventory.Count(GameManager.Instance.Save, item.Id) <= 0) CloseDetail();   // e.g. the last material was sold
            else OpenDetail(item.Id);
        }

        // ------------------------------------------------------------------ result pop-up (강화 / 분해 / 합성)

        private RectTransform _notice;

        /// <summary>A small modal over the bag that says what just happened; tapping 확인 (or anywhere outside) closes it.</summary>
        private void ShowNotice(string title, string body, Color accent)
        {
            if (_notice == null)
            {
                _notice = UiFactory.Rect("Notice", transform as RectTransform, Vector2.zero, Vector2.one);
                _notice.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
                var closeBtn = _notice.gameObject.AddComponent<Button>();
                closeBtn.transition = Selectable.Transition.None;
                closeBtn.onClick.AddListener(() => _notice.gameObject.SetActive(false));
            }
            for (int i = _notice.childCount - 1; i >= 0; i--) Destroy(_notice.GetChild(i).gameObject);
            _notice.gameObject.SetActive(true);
            _notice.SetAsLastSibling();
            var card = UiFactory.Panel("Card", _notice, new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.85f), new Color(0.05f, 0.07f, 0.11f, 0.99f));
            UiFactory.Frame(card.transform, accent, 3f);
            UiFactory.Label("Title", card.transform, new Vector2(0.05f, 0.70f), new Vector2(0.95f, 0.95f), title, 40, accent);
            UiFactory.Body(UiFactory.ScrollBody(UiFactory.Label("Body", card.transform, new Vector2(0.06f, 0.30f), new Vector2(0.94f, 0.70f), body, 30, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 36));
            UiFactory.Button("Ok", card.transform, new Vector2(0.30f, 0.05f), new Vector2(0.70f, 0.26f), "확인", UiFactory.Red, () => _notice.gameObject.SetActive(false), 34);
        }

        private void DoDismantle(ItemData item)
        {
            var save = GameManager.Instance.Save;
            // Dismantling cannot be undone: always ask once. The last copy of a breakthrough piece also loses its stars (slot enhancement stays).
            string warning = Salvage.LosesEnhancement(save, item.Id)
                ? "<b>" + Gear.StarsText(save, item.Id) + "</b> 돌파 장비의 마지막 한 개입니다.\n분해하면 이 장비의 돌파 단계가 사라집니다. 부위 강화는 유지됩니다.\n\n"
                : "";
            UiFactory.Confirm(transform, "분해할까요?", UiFactory.GradedName(item) + " 1개를 분해합니다.\n" + warning + "분해한 장비는 되돌릴 수 없습니다.", "분해", () => Dismantle(item));
        }

        private void Dismantle(ItemData item)
        {
            var save = GameManager.Instance.Save;
            string error;
            _confirmDismantle = null;
            ItemStack enhancement;
            var got = Salvage.Dismantle(save, item.Id, new SeededRandom(Environment.TickCount), out enhancement, out error);
            if (got != null)
            {
                var mat = EarlyGameContent.FindItem(got.Id);
                _flash = item.Name + " 분해: " + mat.Name + " x" + got.Count + ", " + EarlyGameContent.FindItem(enhancement.Id).Name + " x" + enhancement.Count + " 획득";
                SoundManager.Sfx("win");
                GameManager.Instance.SaveNow();
                Refresh();
                ReopenDetail(item);
                ShowNotice("분해 완료", UiFactory.GradedName(item) + "을(를) 분해했습니다.\n\n<color=" + UiFactory.GradeHex(mat.Grade) + ">" + mat.Name + "</color> <b>" + got.Count + "개</b> 획득\n(보유 " + Inventory.Count(save, mat.Id) + "개)\n" + EarlyGameContent.FindItem(enhancement.Id).Name + " " + enhancement.Count + "개 획득", UiFactory.Gold);
                return;
            }
            _flash = error;
            Refresh();
            ReopenDetail(item);
        }

        private void DoFuseMaterial(ItemData material)
        {
            var save = GameManager.Instance.Save;
            string error;
            var made = Gear.FuseMaterial(save, material.Id, out error);
            _flash = made != null ? made.Name + " 1개를 합성했습니다." : error;
            if (made != null) { SoundManager.Sfx("levelup"); GameManager.Instance.SaveNow(); }
            Refresh();
            ReopenDetail(material);
            if (made != null) ShowNotice("재료 합성 완료", material.Name + " 3개를 사용해\n" + UiFactory.GradedName(made) + " 1개를 얻었습니다.", UiFactory.Gold);
        }

        private void DoFuse(ItemData material)
        {
            var save = GameManager.Instance.Save;
            string error;
            var made = Salvage.Fuse(save, material.Id, new SeededRandom(Environment.TickCount), out error);
            if (made != null)
            {
                _flash = "합성 성공! " + Grades.NameOf(made.Grade) + " " + made.Name + " 획득";
                SoundManager.Sfx(made.Grade >= ItemGrade.Legendary ? "gacha_mythic" : "levelup");
                GameManager.Instance.SaveNow();
                Refresh();
                ReopenDetail(material);
                ShowNotice("합성 성공", UiFactory.GradedName(made) + "\n" + UiFactory.GearEffectText(made) + "\n\n재료 " + Salvage.Need(material) + "개를 사용했습니다.", UiFactory.Gold);
                return;
            }
            _flash = error;
            Refresh();
            ReopenDetail(material);
        }

        private void DoBreakthrough(ItemData item)
        {
            var save = GameManager.Instance.Save;
            string error;
            if (Gear.Breakthrough(save, item.Id, out error))
            {
                int lv = Gear.Stars(save, item.Id);
                _flash = item.Name + " " + lv + "성 한계돌파!";
                SoundManager.Sfx("levelup");
                GameManager.Instance.SaveNow();
                Refresh();
                ReopenDetail(item);
                ShowNotice("한계돌파 성공", UiFactory.GradedName(item) + " <b>" + lv + "성</b>\n강화 상한 +" + Gear.Cap(save, item.Id) + "\n기본 장비 효과 +" + Mathf.RoundToInt(Gear.StarBonusFor(item, lv) * 100f) + "%\n\n" + UiFactory.GearTraitsText(item, lv) + "\n\n여분 " + Gear.Spares(save, item.Id) + "개 남음", UiFactory.Gold);
                return;
            }
            _flash = error;
            Refresh();
            ReopenDetail(item);
        }

        /// <summary>Gear first (weapon, armor, accessory), then consumables, materials and keys.</summary>
        private static int KindOrder(ItemData item)
        {
            switch (item.Kind)
            {
                case ItemKind.Weapon: return 0;
                case ItemKind.Armor: return 1;
                case ItemKind.Pants: return 2;
                case ItemKind.Boots: return 3;
                case ItemKind.Accessory: return 4;
                case ItemKind.Consumable: return 5;
                case ItemKind.Material: return 6;
                default: return 7;
            }
        }

        private bool PassesFilter(ItemData item)
        {
            switch (_filter)
            {
                case 1:
                    if (!item.IsGear) return false;
                    return (_gearFilter == 0 && item.Kind == ItemKind.Weapon) || (_gearFilter == 1 && item.Kind == ItemKind.Armor) || (_gearFilter == 2 && item.Kind == ItemKind.Pants) || (_gearFilter == 3 && item.Kind == ItemKind.Boots) || (_gearFilter == 4 && item.Kind == ItemKind.Accessory);
                case 2: return item.Kind == ItemKind.Consumable;
                case 3: return item.Kind == ItemKind.Material || item.Kind == ItemKind.Key;
                default: return true;
            }
        }

        public void Refresh()
        {
            if (!_built) return;
            var save = GameManager.Instance.Save;
            _header.text = "행낭 · " + save.Doo.ToString("N0") + "두";
            _hint.text = !string.IsNullOrEmpty(_flash) ? _flash : "";
            _flash = null;

            var stacks = save.Items
                .Where(s => EarlyGameContent.FindItem(s.Id) != null)
                .OrderBy(s => KindOrder(EarlyGameContent.FindItem(s.Id)))
                .ThenByDescending(s => (int)EarlyGameContent.FindItem(s.Id).Grade)
                .ThenBy(s => EarlyGameContent.FindItem(s.Id).Name)
                .Where(s => PassesFilter(EarlyGameContent.FindItem(s.Id)))
                .ToList();
            _gearTabRow.SetActive(_filter == 1);
            _viewport.anchorMax = new Vector2(.645f, _filter == 1 ? .645f : .735f);
            for (int t = 0; t < _gearTabs.Length; t++)
            {
                _gearTabs[t].gameObject.SetActive(t == _gearFilter);
                _gearTabLabels[t].color = t == _gearFilter ? UiFactory.Gold : UiFactory.Dim;
            }
            for (int t = 0; t < _filterTabs.Length; t++) _filterTabs[t].color = t == _filter ? new Color(0.56f, 0.44f, 0.22f, 1f) : UiFactory.InkSoft;

            _visibleCount = stacks.Count;
            _empty.gameObject.SetActive(stacks.Count == 0);
            while (_rows.Count < stacks.Count) CreateCard();
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                bool has = i < stacks.Count;
                row.Band.SetActive(has);
                row.ItemId = has ? stacks[i].Id : null;
                if (!has) continue;
                var stack = stacks[i];
                var item = EarlyGameContent.FindItem(stack.Id);
                var wearers = Inventory.WearingCompanions(save, item.Id);
                if (Inventory.IsEquipped(save, item.Id)) wearers.Insert(0, "비류연");
                ItemArt.Apply(row.Icon, row.IconGlyph, item);
                string count = stack.Count > 1 ? "×" + stack.Count : "";
                row.Title.text = UiFactory.GradedName(item)
                    + (item.IsGear ? "\n" + Gear.StarsText(save, item.Id) + (count.Length > 0 ? " " + count : "") : count.Length > 0 ? "\n" + count : "")
                    + (wearers.Count > 0 ? "\n<color=#9ADB9F>착용 중</color>" : "");
                row.Detail.text = "";
                row.Title.fontSize = 20;
                row.Title.alignment = TextAnchor.UpperCenter;
            }
            _layoutDirty = true;
        }

        /// <summary>"체력 100% 회복\n내공 가득\n기세 +100" (lines joined with <paramref name="separator"/>).</summary>
        public static string ConsumableSummary(ItemData item, string separator = "\n")
        {
            var effects = new List<string>();
            if (item.HealPct > 0f) effects.Add("체력 " + Mathf.RoundToInt(item.HealPct * 100f) + "% 회복");
            if (item.QiRestore > 0) effects.Add("내공 " + (item.QiRestore >= 999 ? "가득" : "+" + item.QiRestore));
            if (item.SpiritGain > 0) effects.Add("기세 +" + item.SpiritGain);
            if (item.Cleanse) effects.Add("상태 이상 해제");
            if (item.MasteryUses > 0) effects.Add("숙련 +" + item.MasteryUses + "회");
            return effects.Count > 0 ? string.Join(separator, effects) : "전투 소모품";
        }

        private void CreateCard()
        {
            int index = _rows.Count;
            var rt = UiFactory.Rect("Item" + index, _content, new Vector2(0f, 1f), new Vector2(0f, 1f));
            rt.pivot = new Vector2(.5f, 1f);
            var row = new Row { Band = rt.gameObject };
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = UiFactory.Ink;
            var tap = rt.gameObject.AddComponent<Button>();
            tap.targetGraphic = bg;
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => OpenDetail(row.ItemId));
            UiFactory.Frame(rt, UiFactory.GoldDim, 2f);
            var iconRect = UiFactory.Rect("Icon", rt, new Vector2(.5f, 1f), new Vector2(.5f, 1f));
            iconRect.pivot = new Vector2(.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -16f);
            iconRect.sizeDelta = new Vector2(132f, 132f);
            row.Icon = iconRect.gameObject.AddComponent<Image>();
            row.IconGlyph = UiFactory.Label("Glyph", iconRect, Vector2.zero, Vector2.one, "", 44, UiFactory.PaperText);
            row.Title = UiFactory.Body(UiFactory.Label("Name", rt, Vector2.zero, Vector2.one, "", 28, UiFactory.PaperText, TextAnchor.UpperLeft));
            row.Detail = UiFactory.Body(UiFactory.Label("Summary", rt, Vector2.zero, Vector2.one, "", 28, UiFactory.Dim, TextAnchor.UpperLeft, FontStyle.Normal));
            _rows.Add(row);
        }

        // Measure each row's cards together (3 per row, 7 on the 장비 tab) so long names and effects never overlap the next row.
        private void LateUpdate()
        {
            if (!_built || _content.rect.width < 1f) return;
            float width = _content.rect.width;
            if (!_layoutDirty && Mathf.Abs(width - _layoutWidth) < .5f) return;
            _layoutWidth = width;
            _layoutDirty = false;
            float y = RowGap;
            int columns = Columns;
            // The cards share a .92 span with .01 gaps.
            float cardFrac = (.92f - (columns - 1) * .01f) / columns;
            float step = cardFrac + .01f;
            float cardWidth = width * cardFrac;
            float icon = Mathf.Min(132f, cardWidth - 20f);
            for (int start = 0; start < _visibleCount; start += columns)
            {
                float height = 0f;
                for (int i = start; i < Mathf.Min(start + columns, _visibleCount); i++)
                {
                    var row = _rows[i];
                    var iconRect = (RectTransform)row.Icon.transform;
                    iconRect.sizeDelta = new Vector2(icon, icon);
                    const float pad = 12f;
                    float nameHeight = UiFactory.MeasureHeight(row.Title, row.Title.text, cardWidth - pad) + 8f;
                    float detailHeight = string.IsNullOrEmpty(row.Detail.text) ? 0f : UiFactory.MeasureHeight(row.Detail, row.Detail.text, cardWidth - pad) + 8f;
                    float top = 16f + icon + 8f;
                    SetTextRegion(row.Title.rectTransform, top, nameHeight, pad);
                    SetTextRegion(row.Detail.rectTransform, top + 12f + nameHeight, detailHeight, pad);
                    height = Mathf.Max(height, top + 28f + nameHeight + detailHeight);
                }
                for (int i = start; i < Mathf.Min(start + columns, _visibleCount); i++)
                {
                    var rt = (RectTransform)_rows[i].Band.transform;
                    float left = .04f + (i % columns) * step;
                    rt.anchorMin = new Vector2(left, 1f);
                    rt.anchorMax = new Vector2(left + cardFrac, 1f);
                    rt.sizeDelta = new Vector2(0f, height);
                    rt.anchoredPosition = new Vector2(0f, -y);
                }
                y += height + RowGap;
            }
            _content.sizeDelta = new Vector2(0f, y);
        }

        private static void SetTextRegion(RectTransform rect, float top, float height, float pad = 24f)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(-pad, height);
            rect.anchoredPosition = new Vector2(pad / 2f, -top);
        }
    }
}
