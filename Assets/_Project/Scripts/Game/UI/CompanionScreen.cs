using System;
using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Shared landscape workspace for the protagonist and joined companions.</summary>
    public sealed class CompanionScreen : MonoBehaviour
    {
        private static readonly string[] Pages = { "상태", "무공", "심법", "인연" };
        private RectTransform _page, _identity, _picker, _tabs, _message;
        private string _owner = Inventory.Hero, _toast;
        private int _tab;
        private Action _openArts, _openInner;
        private bool _built, _resetScroll = true;
        private SaveData Save { get { return GameManager.Instance.Save; } }
        private CompanionData Companion { get { return EarlyGameContent.FindCompanion(_owner); } }
        private string OwnerName { get { return _owner == Inventory.Hero ? "비류연" : Companion.Name; } }

        public void Build(RectTransform root, Action openArts, Action openInner = null)
        {
            _openArts = openArts;
            _openInner = openInner ?? openArts;
            UiFactory.Panel("Background", root, Vector2.zero, Vector2.one, new Color(.02f, .045f, .03f, .10f));
            _identity = UiFactory.Rect("Identity", root, new Vector2(.02f, .04f), new Vector2(.255f, .965f));
            _tabs = UiFactory.Rect("Pages", root, new Vector2(.275f, .865f), new Vector2(.98f, .965f));
            _page = UiFactory.Rect("Page", root, new Vector2(.275f, .085f), new Vector2(.98f, .85f));
            _message = UiFactory.Rect("Message", root, new Vector2(.28f, .015f), new Vector2(.975f, .075f));
            _picker = UiFactory.Rect("CharacterPicker", root, Vector2.zero, Vector2.one);
            _picker.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, .85f);
            _picker.gameObject.SetActive(false);
            _built = true;
            Refresh();
        }

        private void OnEnable() { if (_built) Refresh(); }
        private void OnDisable() { if (_picker != null) _picker.gameObject.SetActive(false); }
        private static void Clear(RectTransform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                child.SetActive(false);
                child.name = "Retired_" + child.name;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        private static Text Text(Transform parent, string name, float x, float y, float right, float top, string value, int size = 28, Color? color = null)
        {
            return UiFactory.Body(UiFactory.Label(name, parent, new Vector2(x, y), new Vector2(right, top), value, size, color ?? UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal));
        }

        private static Text Reading(Transform parent, string name, float x, float y, float right, float top, string value, int size = 28)
        {
            return UiFactory.ScrollBody(Text(parent, name, x, y, right, top, value, size), size);
        }

        private static Button Button(Transform parent, string name, float x, float y, float right, float top, string value, Action action, bool enabled = true)
        {
            var button = UiFactory.Button(name, parent, new Vector2(x, y), new Vector2(right, top), value, enabled ? UiFactory.Red : UiFactory.InkSoft, () => action(), 28);
            button.interactable = enabled;
            return button;
        }

        public void Refresh()
        {
            if (!_built || GameManager.Instance == null) return;
            if (_owner != Inventory.Hero && (Companion == null || Companion.JoinStage > Save.HighestUnlockedStage))
            {
                _owner = Inventory.Hero;
            }
            var oldList = _page.Find("List/Viewport");
            float position = !_resetScroll && oldList != null ? oldList.GetComponent<ScrollRect>().verticalNormalizedPosition : 1f;
            _picker.gameObject.SetActive(false);
            Clear(_identity); Clear(_tabs); Clear(_page); Clear(_message);
            BuildIdentity();
            for (int i = 0; i < Pages.Length; i++)
            {
                int page = i;
                float w = 1f / Pages.Length;
                var button = Button(_tabs, "Page" + i, i * w, 0f, (i + 1) * w - .01f, 1f, Pages[i], () => { _tab = page; _resetScroll = true; _toast = null; Refresh(); });
                button.GetComponent<Image>().color = i == _tab ? UiFactory.Red : UiFactory.InkSoft;
                if (i == _tab) UiFactory.Frame(button.transform, UiFactory.Gold, 2f);
            }
            switch (_tab)
            {
                case 0: BuildStatus(); break;
                case 1: BuildArts(); break;
                case 2: BuildInner(); break;
                case 3: BuildBonds(); break;
            }
            Canvas.ForceUpdateCanvases();
            var newList = _page.Find("List/Viewport");
            if (newList != null)
            {
                var scroll = newList.GetComponent<ScrollRect>();
                var layout = scroll.content.GetComponent<ReadingList>();
                if (layout != null) layout.Reflow();
                Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition = position;
            }
            _resetScroll = false;
            Reading(_message, "Feedback", 0f, 0f, 1f, 1f, _toast ?? (_tab == 1 ? "현재 전투에서 사용하는 무공만 표시합니다." : _tab == 2 ? "장착한 심법은 늘 적용됩니다." : "인물을 바꿔도 선택한 탭을 유지합니다."), 26).color = UiFactory.Gold;
        }

        private void OpenCharacterPicker()
        {
            CharacterPicker.Open(_picker, Save, _owner, owner =>
            {
                _owner = owner; _resetScroll = true; _toast = null; Refresh();
            });
        }

        /// <summary>Share of a full-length standing picture, from its top, that the profile shows: head to about mid-thigh.</summary>
        private const float StandingCrop = .63f;

        private static void Portrait(Transform parent, string name, float x, float y, float right, float top)
        {
            var standing = ArtLibrary.StandingPortrait(name);
            if (standing != null)
            {
                // Mask the frame and stretch the picture so its top StandingCrop fills the frame's height; the legs fall below the mask.
                // The rect is kept wider than the picture so preserveAspect fits it by height and centres it.
                var frame = UiFactory.Rect("Portrait", parent, new Vector2(x, y), new Vector2(right, top));
                frame.gameObject.AddComponent<RectMask2D>();
                var crop = UiFactory.Panel("Picture", frame, new Vector2(-.5f, 1f - 1f / StandingCrop), new Vector2(1.5f, 1f), Color.white);
                crop.sprite = standing; crop.preserveAspect = true; crop.raycastTarget = false;
                return;
            }
            var picture = UiFactory.Panel("Portrait", parent, new Vector2(x, y), new Vector2(right, top), Color.white);
            picture.sprite = ArtLibrary.CharacterPortrait(name); picture.preserveAspect = true;
            if (picture.sprite == null) { picture.color = Color.clear; Text(picture.transform, "Glyph", 0f, 0f, 1f, 1f, name.Substring(0, 1), 50, UiFactory.Gold); }
        }

        private void BuildIdentity()
        {
            UiFactory.Frame(_identity, UiFactory.GoldDim, 2f);
            Portrait(_identity, OwnerName, .03f, .40f, .97f, .98f);
            var change = UiFactory.Button("ChangeCharacter", _identity, new Vector2(.04f, .25f), new Vector2(.96f, .39f), "", Color.clear, OpenCharacterPicker, 28);
            Text(change.transform, "Name", .02f, .03f, .84f, .97f, OwnerName, 42, UiFactory.Gold);
            UiFactory.Label("Arrow", change.transform, new Vector2(.87f, .05f), new Vector2(.99f, .95f), "▾", 28, UiFactory.Gold);
            UiFactory.Panel("Underline", change.transform, new Vector2(.02f, 0f), new Vector2(.98f, .02f), UiFactory.GoldDim);
            bool hero = _owner == Inventory.Hero;
            int level = hero ? Save.Level : CompanionGrowth.LevelOf(Save, _owner);
            string realm = hero ? Realm.Label(Realm.StepOf(Realm.Power(InnerArts.HeroStatsBeforeRealm(Save)))) : StatusSheet.AllyRealm(Save, Companion);
            var fielded = EarlyGameContent.SelectCompanionsFor(Math.Max(1, Save.HighestUnlockedStage), Save);
            string deployment = hero ? "주인공" : fielded.Any(c => c.Id == _owner) ? "출전 편성" : "대기 중";
            Reading(_identity, "Summary", .06f, .035f, .94f, .24f, "Lv." + level + " · " + (hero ? Promotion.RankName(Promotion.EffectiveRank(Save)) : Companion.Role) + " · " + deployment + "\n" + realm, 26);
        }

        private void BuildStatus()
        {
            double power = _owner == Inventory.Hero
                ? Realm.Power(InnerArts.HeroStatsBeforeRealm(Save))
                : CompanionGrowth.Create(Save, Companion).RealmPowerBeforeBonus;
            int step = Realm.StepOf(power);
            var realm = UiFactory.Panel("Realm", _page, new Vector2(0f, .71f), new Vector2(1f, 1f), UiFactory.Ink).rectTransform;
            UiFactory.Frame(realm, UiFactory.GoldDim, 2f);
            Text(realm, "Name", .025f, .68f, .49f, .97f, "무공 경지 · " + Realm.Label(step), 30, UiFactory.Gold);
            Text(realm, "Power", .52f, .68f, .975f, .97f, "종합 수치 " + Math.Round(power).ToString("N0"), 30, UiFactory.PaperText);
            string remaining = Realm.RemainingText(power);
            Text(realm, "Remaining", .025f, .28f, .975f, .65f,
                remaining.Length > 0 ? remaining : "최고 경지에 도달했습니다.", 26, UiFactory.PaperText);
            var progress = UiFactory.MakeBar("Progress", realm, new Vector2(.025f, .07f), new Vector2(.975f, .19f), UiFactory.InkSoft, UiFactory.Gold);
            progress.SetPct((float)Realm.ProgressToNext(power));

            var lines = _owner == Inventory.Hero ? StatusSheet.Hero(Save) : StatusSheet.Ally(Save, Companion);
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i]; int column = i % 2, row = i / 2;
                var panel = UiFactory.Panel("Stat" + i, _page, new Vector2(column * .51f, .54f - row * .155f), new Vector2(column * .51f + .49f, .68f - row * .155f), UiFactory.Ink);
                Text(panel.transform, "Label", .04f, .52f, .96f, .97f, line.Label, 26, UiFactory.Dim);
                Text(panel.transform, "Value", .04f, .04f, .58f, .56f, line.Value, 34, UiFactory.Gold);
                Text(panel.transform, "Bonus", .60f, .04f, .97f, .56f, line.Bonus, 26, UiFactory.Jade);
            }
            if (_owner != Inventory.Hero)
            {
                var stats = CompanionGrowth.Create(Save, Companion).Stats;
                Reading(_page, "Critical", 0f, .005f, 1f, .20f, "치명타율 " + (stats.CritRate * 100f).ToString("0.#") + "% · 치명타 피해 x" + stats.CritDamage.ToString("0.##") + "\n장비·인연·수련을 반영한 전투 능력치입니다.");
            }
        }

        private RectTransform List(RectTransform parent, float x, float y, float right, float top, int count, float height = 180f)
        {
            var host = UiFactory.Rect("List", parent, new Vector2(x, y), new Vector2(right, top));
            var content = CodexPanel.Scroller(host, 0f, 1f);
            content.sizeDelta = new Vector2(0f, 12f + count * (height + 12f));
            return content;
        }

        private static RectTransform Row(RectTransform content, int index, float height = 180f)
        {
            var row = UiFactory.Panel("Row" + index, content, new Vector2(.015f, 1f), new Vector2(.965f, 1f), UiFactory.Ink).rectTransform;
            row.pivot = new Vector2(.5f, 1f); row.sizeDelta = new Vector2(0f, height);
            row.anchoredPosition = new Vector2(0f, -12f - index * (height + 12f));
            row.GetComponent<Image>().raycastTarget = true;
            UiFactory.Frame(row, UiFactory.GoldDim, 1f);
            ReadingList.Register(row, height, 12f);
            return row;
        }

        private void BuildArts()
        {
            bool hero = _owner == Inventory.Hero;
            var unit = hero ? Formation.BuildHero(Save) : CompanionGrowth.Create(Save, Companion);
            var arts = EarlyGameContent.Arts.Where(art => (hero ? art.OwnerId == null : art.OwnerId == _owner)
                && unit.Skills.Any(skill => skill.Id == art.Id)).ToList();
            var summary = UiFactory.Panel("ArtsSummary", _page, new Vector2(0f, .82f), Vector2.one, UiFactory.Ink).rectTransform;
            Text(summary, "Heading", .03f, .47f, hero ? .84f : .97f, .98f, (hero ? "장착 중인 무공" : "현재 사용 무공") + " · " + arts.Count + "개", 32, UiFactory.Gold);
            Text(summary, "Note", .03f, .03f, hero ? .84f : .97f, .46f, hero ? "전투에 적용되는 효과와 소모 자원을 표시합니다." : "동료 무공은 레벨·수련·여정 진행에 따라 적용됩니다.", 24, UiFactory.Dim);
            if (hero) SummaryButton(summary, "ArtsMenu", _openArts);
            // Two cards per row, using the whole page below the summary bar
            var list = List(_page, 0f, 0f, 1f, .80f, (arts.Count + 1) / 2, 300f);
            for (int i = 0; i < arts.Count; i++)
            {
                var art = arts[i]; var spec = unit.Skills.First(skill => skill.Id == art.Id);
                var row = Card(list, i, 300f);
                var icon = UiFactory.Panel("Icon", row, new Vector2(.03f, .80f), new Vector2(.03f, .80f), Color.white);
                icon.rectTransform.pivot = new Vector2(0f, .5f);
                icon.rectTransform.sizeDelta = new Vector2(100f, 100f);
                icon.sprite = ArtLibrary.Art(art.Id); icon.preserveAspect = true;
                if (icon.sprite == null)
                {
                    icon.color = Color.clear;
                    UiFactory.Label("Glyph", icon.transform, Vector2.zero, Vector2.one, art.Name.Substring(0, 1), 36, UiFactory.Gold);
                }
                Text(row, "Name", .25f, .76f, .80f, .97f, art.Name, 32, UiFactory.Gold);
                Text(row, "Kind", .80f, .76f, .97f, .97f, Progression.IsUltimate(art) ? "필살기" : "무공", 24, UiFactory.Dim);
                string resources = "내공 " + spec.QiCost + (spec.SpiritCost > 0 ? " · 기세 " + spec.SpiritCost : "")
                    + (spec.Cooldown > 0 ? " · 재사용 " + spec.Cooldown + "턴" : " · 재사용 없음");
                if (hero) resources += " · 숙련 " + Mastery.LevelNames[Mastery.Level(Save, art.Id)];
                Text(row, "Resources", .25f, .56f, .97f, .74f, resources, 24, UiFactory.PaperText);
                string effect = SupportEffects.Describe(spec) != null ? SupportEffects.Describe(spec)
                    : spec.Effect == SkillEffect.HealAlly ? "아군 체력 " + Math.Round(spec.HealPct * 100f) + "% 회복"
                    : spec.Effect == SkillEffect.Evade ? "회피 확률 " + Math.Round(spec.EvadeChance * 100f) + "%"
                    : spec.Effect == SkillEffect.Cleanse ? "아군 상태 이상 해제"
                    : "위력 ×" + spec.Power.ToString("0.##") + " · " + Math.Max(1, spec.Hits) + "타 · " + (spec.AllEnemies ? "적 전체" : "적 1명");
                Text(row, "Effect", .04f, .35f, .97f, .54f, effect, 26, UiFactory.Jade);
                Text(row, "Description", .04f, .04f, .97f, .33f, art.Description, 24, UiFactory.PaperText);
            }
            if (arts.Count == 0) Text(_page, "Empty", .04f, .40f, .96f, .70f, "현재 장착 중인 무공이 없습니다.", 30, UiFactory.Dim);
        }

        /// <summary>심법 page: the inner arts 비류연 has equipped, as two cards per row like the 무공 page (companions have none).</summary>
        private void BuildInner()
        {
            if (_owner != Inventory.Hero)
            {
                Reading(_page, "Empty", .05f, .3f, .95f, .7f, "심법은 비류연만 익히고 장착합니다.");
                return;
            }
            var equipped = Save.EquippedInner.Select(InnerArts.Find).Where(a => a != null).ToList();
            var summary = UiFactory.Panel("InnerSummary", _page, new Vector2(0f, .82f), Vector2.one, UiFactory.Ink).rectTransform;
            Text(summary, "Heading", .03f, .47f, .84f, .98f, "장착 중인 심법 · " + equipped.Count + " / " + InnerArts.SlotCount(Save) + "칸", 32, UiFactory.Gold);
            Text(summary, "Note", .03f, .03f, .84f, .46f, "초록은 얻는 효과, 빨강은 잃는 효과입니다.", 24, UiFactory.Dim);
            SummaryButton(summary, "InnerMenu", _openInner);
            var list = List(_page, 0f, 0f, 1f, .80f, (equipped.Count + 1) / 2, 300f);
            for (int i = 0; i < equipped.Count; i++)
            {
                var a = equipped[i];
                int level = InnerArts.Level(Save, a.Id);
                var card = Card(list, i, 300f);
                var icon = UiFactory.Panel("Icon", card, new Vector2(.03f, .80f), new Vector2(.03f, .80f), Color.white);
                icon.rectTransform.pivot = new Vector2(0f, .5f);
                icon.rectTransform.sizeDelta = new Vector2(100f, 100f);
                icon.sprite = ArtLibrary.InnerArt(a.Id); icon.preserveAspect = true;
                if (icon.sprite == null)
                {
                    icon.color = Color.clear;
                    UiFactory.Glyph("fortune", icon.transform, Vector2.zero, Vector2.one, UiFactory.Gold);
                }
                Text(card, "Name", .25f, .76f, .97f, .97f, a.Name + (level > 0 ? "  +" + level : ""), 32, UiFactory.Gold);
                Text(card, "Effect", .25f, .40f, .97f, .74f, InnerArtPanel.Colored(a, level, "\n"), 24, UiFactory.PaperText);
                Text(card, "Description", .04f, .04f, .97f, .38f, a.Description, 24, UiFactory.PaperText);
            }
            if (equipped.Count == 0) Text(_page, "Empty", .04f, .40f, .96f, .70f, "현재 장착 중인 심법이 없습니다.", 30, UiFactory.Dim);
        }

        /// <summary>Small 「변경」 button at the right end of an equipped-arts summary bar; opens the 무공 tab on the matching page.</summary>
        private static void SummaryButton(RectTransform summary, string name, Action action)
        {
            var button = UiFactory.Button(name, summary, new Vector2(.86f, .2f), new Vector2(.98f, .8f), "변경", UiFactory.Red, () => action(), 26);
            UiFactory.Frame(button.transform, UiFactory.GoldDim, 1f);
        }

        /// <summary>Card <paramref name="index"/> of a two-column list: rows of two, each row measured as one so a long card grows its row.</summary>
        private static RectTransform Card(RectTransform content, int index, float height)
        {
            int rowIndex = index / 2;
            var row = content.Find("Row" + rowIndex) as RectTransform;
            if (row == null)
            {
                row = UiFactory.Rect("Row" + rowIndex, content, new Vector2(.015f, 1f), new Vector2(.965f, 1f));
                row.pivot = new Vector2(.5f, 1f); row.sizeDelta = new Vector2(0f, height);
                row.anchoredPosition = new Vector2(0f, -12f - rowIndex * (height + 12f));
                ReadingList.Register(row, height, 12f);
            }
            float x0 = index % 2 == 0 ? 0f : .51f;
            var card = UiFactory.Panel("Card" + index, row, new Vector2(x0, 0f), new Vector2(x0 + .49f, 1f), UiFactory.Ink).rectTransform;
            card.GetComponent<Image>().raycastTarget = true;
            UiFactory.Frame(card, UiFactory.GoldDim, 1f);
            return card;
        }

        private void BuildBonds()
        {
            var companions = _owner == Inventory.Hero ? EarlyGameContent.Companions.Where(c => c.JoinStage <= Save.HighestUnlockedStage).ToList() : new List<CompanionData> { Companion };
            var list = List(_page, 0f, 0f, 1f, 1f, companions.Count, 260f);
            for (int i = 0; i < companions.Count; i++)
            {
                var companion = companions[i]; var row = Row(list, i, 260f);
                int level = Bonds.Level(Save, companion.Id), points = Bonds.Points(Save, companion.Id);
                string next = level >= Bonds.Thresholds.Length - 1 ? "최고 인연 단계" : "다음 단계까지 " + (Bonds.Thresholds[level + 1] - points) + "점";
                Text(row, "Name", .04f, .74f, .96f, .98f, companion.Name + " · " + Bonds.LevelNames[level] + " · " + points + "점", 32, UiFactory.Gold);
                Text(row, "Benefits", .04f, .04f, .67f, .72f, Bonds.BonusText(level) + "\n" + next
                    + "\n합공: " + (string.IsNullOrEmpty(companion.JointName) ? "없음" : companion.JointName + (level < 1 ? " · 친분부터 사용 가능" : " · 사용 가능"))
                    + "\n수련 Lv." + CompanionTraining.LevelOf(Save, companion.Id) + " / " + CompanionTraining.Cap(Save, companion.Id), 28);
                int training = CompanionTraining.LevelOf(Save, companion.Id), cap = CompanionTraining.Cap(Save, companion.Id);
                long cost = CompanionTraining.NextCost(Save, companion.Id);
                string label = training >= CompanionTraining.MaxLevel ? "수련 완료" : training >= cap ? "인연 성장 필요" : "동료 수련\n" + cost.ToString("N0") + "은자";
                Button(row, "Train", .70f, .12f, .97f, .68f, label, () =>
                {
                    string error;
                    bool success = CompanionTraining.Train(Save, companion.Id, out error);
                    Complete(success, error, "동료를 수련했습니다.");
                }, training < cap && training < CompanionTraining.MaxLevel && Save.Doo >= cost);
            }
            if (companions.Count == 0) Reading(_page, "Empty", .05f, .2f, .95f, .8f, "동료가 합류하면 인연과 합공 효과를 확인할 수 있습니다.");
        }

        private void Complete(bool success, string error, string message)
        {
            if (success) { GameManager.Instance.SaveNow(); SoundManager.Sfx("click"); }
            _toast = success ? message : error;
            Refresh();
        }
    }
}
