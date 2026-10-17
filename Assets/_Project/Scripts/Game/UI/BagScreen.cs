using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>행낭 탭. Shows the bag: equip gear (one weapon, two accessories), sell materials. Consumables are used from the battle screen.</summary>
    public class BagScreen : MonoBehaviour
    {
        private const int MaxRows = 60;
        private const float RowH = 180f, RowGap = 10f;   // gear rows stack 장착/해제 over 강화 as two big buttons

        private sealed class Row
        {
            public GameObject Band;
            public Text Title, Detail, ButtonLabel, EnhLabel;
            public Button Button, EnhButton;
            public Image ButtonImage, Icon;
            public Text IconGlyph;
            public string ItemId;
        }

        private readonly List<Row> _rows = new List<Row>();
        private Text _header, _gear, _hint;
        private string _flash;
        private RectTransform _content;
        private int _filter;                       // 0 all, 1 gear, 2 consumables, 3 materials & keys
        private readonly Image[] _filterTabs = new Image[4];
        private int _gearFilter;                   // within 장비: 0 all, 1 weapon, 2 armor, 3 accessory
        private readonly Image[] _gearTabs = new Image[4];
        private static readonly string[] GearFilterNames = { "전체", "무기", "방어구", "장신구" };
        private RectTransform _viewport;
        private GameObject _gearTabRow;
        private static readonly string[] FilterNames = { "전체", "장비", "소모품", "재료" };
        private bool _built;

        public void Build(RectTransform root)
        {
            if (_built) return;
            _built = true;

            UiFactory.Frame(UiFactory.Panel("HeaderBand", root, new Vector2(0.04f, 0.925f), new Vector2(0.96f, 0.985f), UiFactory.Ink).transform, UiFactory.GoldDim, 2f);
            _header = UiFactory.Label("Header", root, new Vector2(0.06f, 0.925f), new Vector2(0.94f, 0.985f), "", 42, UiFactory.Gold);
            _gear = UiFactory.Label("Gear", root, new Vector2(0.05f, 0.875f), new Vector2(0.95f, 0.922f), "", 28, UiFactory.Dim, TextAnchor.MiddleCenter);
            _hint = UiFactory.Label("Hint", root, new Vector2(0.05f, 0.840f), new Vector2(0.95f, 0.875f), "", 24, UiFactory.Dim, TextAnchor.MiddleCenter, FontStyle.Normal);

            // Filter tabs
            for (int t = 0; t < FilterNames.Length; t++)
            {
                int tab = t;
                float x0 = 0.04f + t * 0.24f;
                var btn = UiFactory.Button("Filter" + t, root, new Vector2(x0, 0.778f), new Vector2(x0 + 0.225f, 0.832f), FilterNames[t], UiFactory.InkSoft, () => { _filter = tab; Refresh(); }, 28);
                _filterTabs[t] = btn.GetComponent<Image>();
            }

            // Second row, only on the 장비 tab: weapon / armor / accessory
            _gearTabRow = UiFactory.Rect("GearTabs", root, new Vector2(0f, 0.728f), new Vector2(1f, 0.768f)).gameObject;
            for (int t = 0; t < GearFilterNames.Length; t++)
            {
                int tab = t;
                float x0 = 0.10f + t * 0.20f;   // smaller and indented so they read as sub-tabs of 장비
                var btn = UiFactory.Button("GearFilter" + t, _gearTabRow.transform, new Vector2(x0, 0.06f), new Vector2(x0 + 0.185f, 0.94f), GearFilterNames[t], UiFactory.InkSoft, () => { _gearFilter = tab; Refresh(); }, 20);
                _gearTabs[t] = btn.GetComponent<Image>();
            }

            // The bag scrolls: it can hold more stacks than fit on one screen.
            var viewport = UiFactory.Rect("Viewport", root, Vector2.zero, new Vector2(1f, 0.772f));
            _viewport = viewport;
            viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            _content = UiFactory.Rect("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f));
            _content.pivot = new Vector2(0.5f, 1f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = _content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            for (int i = 0; i < MaxRows; i++)
            {
                var row = new Row();
                var rt = UiFactory.Rect("Row" + i, _content, new Vector2(0.04f, 1f), new Vector2(0.96f, 1f));
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -(RowGap + i * (RowH + RowGap)));
                rt.sizeDelta = new Vector2(0f, RowH);
                row.Band = rt.gameObject;
                var bg = rt.gameObject.AddComponent<Image>();
                bg.color = UiFactory.Ink;
                bg.raycastTarget = true;   // lets a drag that starts on a row still scroll
                int tapIndex = i;
                var tap = rt.gameObject.AddComponent<Button>();   // tapping the row itself opens the detail window
                tap.targetGraphic = bg;
                tap.transition = Selectable.Transition.None;
                tap.onClick.AddListener(() => OpenDetail(_rows[tapIndex].ItemId));
                UiFactory.Frame(rt, UiFactory.GoldDim, 2f);
                row.Icon = UiFactory.Panel("Icon", rt, new Vector2(0.025f, 0.14f), new Vector2(0.125f, 0.86f), new Color(0.3f, 0.3f, 0.3f, 1f));
                UiFactory.Frame(row.Icon.transform, UiFactory.Gold, 2f);
                row.IconGlyph = UiFactory.Label("Glyph", row.Icon.transform, Vector2.zero, Vector2.one, "", 44, UiFactory.PaperText);
                row.Title = UiFactory.Label("Title", rt, new Vector2(0.14f, 0.50f), new Vector2(0.66f, 0.98f), "", 32, UiFactory.PaperText, TextAnchor.MiddleLeft);
                row.Detail = UiFactory.Label("Detail", rt, new Vector2(0.14f, 0.02f), new Vector2(0.66f, 0.50f), "", 20, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                int index = i;
                row.Button = UiFactory.Button("Btn" + i, rt, new Vector2(0.68f, 0.14f), new Vector2(0.97f, 0.86f), "", UiFactory.Red, () => OnPressed(index), 28);
                row.ButtonImage = row.Button.GetComponent<Image>();
                row.ButtonLabel = row.Button.GetComponentInChildren<Text>();
                row.EnhButton = UiFactory.Button("Enh" + i, rt, new Vector2(0.68f, 0.05f), new Vector2(0.97f, 0.46f), "", UiFactory.InkSoft, () => OnEnhance(index), 22);
                row.EnhLabel = row.EnhButton.GetComponentInChildren<Text>();
                _rows.Add(row);
            }

            _detail = UiFactory.Rect("ItemDetail", root, Vector2.zero, Vector2.one);
            _detail.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);   // dims and blocks the list underneath
            _detail.gameObject.SetActive(false);
            Refresh();
        }

        // ------------------------------------------------------------------ item detail window

        private RectTransform _detail;
        private string _detailId;

        private void OpenDetail(string itemId)
        {
            var item = string.IsNullOrEmpty(itemId) ? null : EarlyGameContent.FindItem(itemId);
            if (item == null) return;
            var save = GameManager.Instance.Save;
            _detailId = itemId;
            for (int i = _detail.childCount - 1; i >= 0; i--) Destroy(_detail.GetChild(i).gameObject);
            _detail.gameObject.SetActive(true);

            bool gear = item.IsGear;
            int enh = gear ? Gear.Level(save, item.Id) : 0;
            var card = UiFactory.Panel("Card", _detail, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.90f), new Color(0.05f, 0.07f, 0.11f, 0.99f));
            UiFactory.Frame(card.transform, UiFactory.GradeColor(item.Grade), 3f);
            var big = UiFactory.Panel("Picture", card.transform, new Vector2(0.74f, 0.855f), new Vector2(0.95f, 0.985f), Color.white);
            UiFactory.Frame(big.transform, UiFactory.GradeColor(item.Grade), 2f);
            var bigGlyph = UiFactory.Label("Glyph", big.transform, Vector2.zero, Vector2.one, "", 60, UiFactory.PaperText);
            ItemArt.Apply(big, bigGlyph, item);
            UiFactory.Label("Title", card.transform, new Vector2(0.05f, 0.90f), new Vector2(0.72f, 0.98f), UiFactory.GradedName(item) + (enh > 0 ? " +" + enh : ""), 40, UiFactory.PaperText, TextAnchor.MiddleLeft);
            string kind = KindName(item) + (gear && Inventory.IsEquipped(save, item.Id) ? "  ·  착용 중" : "") + (item.Kind == ItemKind.Consumable || item.Kind == ItemKind.Material ? "  ·  보유 " + Inventory.Count(save, item.Id) + "개" : "");
            UiFactory.Label("Kind", card.transform, new Vector2(0.05f, 0.84f), new Vector2(0.72f, 0.90f), kind, 24, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);

            var lines = new List<string>();
            if (gear) lines.Add("<color=#9ADB9F>" + StatusSheet.EffectText(item, enh) + "</color>" + (enh > 0 ? "   (기본 " + StatusSheet.EffectText(item, 0) + ")" : ""));
            else
            {
                var fx = new List<string>();
                if (item.HealPct > 0f) fx.Add("체력 " + Mathf.RoundToInt(item.HealPct * 100f) + "% 회복");
                if (item.QiRestore > 0) fx.Add("내공 " + (item.QiRestore >= 999 ? "가득" : "+" + item.QiRestore));
                if (item.SpiritGain > 0) fx.Add("기세 +" + item.SpiritGain);
                if (item.MasteryUses > 0) fx.Add("배운 무공 하나의 숙련 +" + item.MasteryUses + "회");
                if (fx.Count > 0) lines.Add("<color=#9ADB9F>" + string.Join("  ", fx) + "</color>");
                if (item.Kind == ItemKind.Material && item.SellValue > 0) lines.Add("판매가 " + item.SellValue.ToString("N0") + "두 (1개)");
            }
            string flavor = StatusSheet.Flavor(item);
            if (flavor.Length > 0) lines.Add(flavor);
            if (gear)
            {
                long cost = Gear.NextCost(save, item.Id);
                lines.Add("강화 +" + enh + " / +" + Gear.MaxLevel + (cost >= 0 ? "   다음 강화 " + cost.ToString("N0") + "두 (능력 +" + Mathf.RoundToInt(Gear.PerLevel * 100f) + "%)" : "   (최대)"));
                var set = Gear.SetOf(item.Id);
                lines.Add(set != null ? Gear.SetDetail(set, save.EquippedGear, save) : "세트 없음");
            }
            UiFactory.Label("Body", card.transform, new Vector2(0.06f, gear ? 0.37f : 0.24f), new Vector2(0.94f, 0.83f), string.Join("\n\n", lines), 28, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal);

            if (gear)
            {
                bool eq = Inventory.IsEquipped(save, item.Id);
                UiFactory.Button("Equip", card.transform, new Vector2(0.05f, 0.255f), new Vector2(0.95f, 0.355f), eq ? "해제" : "장착", eq ? UiFactory.InkSoft : UiFactory.Red, () => { DoMain(item); }, 34);
                long cost = Gear.NextCost(save, item.Id);
                var enhBtn = UiFactory.Button("Enhance", card.transform, new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.24f), cost < 0 ? "최대 강화" : "강화  " + cost.ToString("N0") + "두", cost >= 0 && save.Doo >= cost ? UiFactory.Red : UiFactory.InkSoft, () => { DoEnhance(item); }, 30);
                enhBtn.interactable = cost >= 0;
            }
            else if (item.MasteryUses > 0)
            {
                UiFactory.Button("Read", card.transform, new Vector2(0.05f, 0.13f), new Vector2(0.95f, 0.22f), "무공 고르기 (숙련 +" + item.MasteryUses + "회)", UiFactory.Red, () => { DoMain(item); }, 32);
            }
            else if (item.Kind == ItemKind.Material && item.SellValue > 0)
            {
                int count = Inventory.Count(save, item.Id);
                UiFactory.Button("Sell", card.transform, new Vector2(0.05f, 0.13f), new Vector2(0.95f, 0.22f), "전부 판매 +" + (item.SellValue * count).ToString("N0") + "두", UiFactory.Red, () => { DoMain(item); }, 32);
            }
            else if (item.Kind == ItemKind.Material)
            {
                var blocked = UiFactory.Button("Sell", card.transform, new Vector2(0.05f, 0.13f), new Vector2(0.95f, 0.22f), "판매 불가", new Color(0.30f, 0.30f, 0.33f, 1f), null, 32);
                blocked.interactable = false;
            }
            UiFactory.Button("Close", card.transform, new Vector2(0.30f, 0.02f), new Vector2(0.70f, 0.10f), "닫기", UiFactory.InkSoft, CloseDetail, 34);
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
                case ItemKind.Armor: return "방어구";
                case ItemKind.Accessory: return "장신구";
                case ItemKind.Consumable: return item.MasteryUses > 0 ? "수련서 (행낭에서 사용)" : "소모품 (전투 중 사용)";
                case ItemKind.Material: return "재료";
                default: return "중요 물품";
            }
        }

        private void OnDisable() { if (_built && _detail != null) CloseDetail(); }

        private void OnEnable()
        {
            if (!_built) return;
            Refresh();
            var save = GameManager.Instance.Save;
            if (HelpPanel.Instance != null && !HelpPanel.Instance.IsOpen && Tutorial.IsDone(save, "intro") && !Tutorial.IsDone(save, "gear"))
                HelpPanel.Instance.Show("gear", true);
        }

        private void OnPressed(int index)
        {
            var row = _rows[index];
            if (string.IsNullOrEmpty(row.ItemId)) return;
            var item = EarlyGameContent.FindItem(row.ItemId);
            if (item != null) DoMain(item);
        }

        /// <summary>Equip / unequip gear or sell a material stack (from the list button or the detail window).</summary>
        private void DoMain(ItemData item)
        {
            var save = GameManager.Instance.Save;

            if (item.IsGear)
            {
                if (Inventory.IsEquipped(save, item.Id))
                {
                    Inventory.Unequip(save, item.Id);
                    _flash = item.Name + " 해제";
                }
                else
                {
                    string error;
                    _flash = Inventory.Equip(save, item.Id, out error) ? item.Name + " 장착" : error;
                }
            }
            else if (item.MasteryUses > 0)
            {
                OpenScrollPicker(item);   // the art is chosen there; nothing else to refresh yet
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
            for (int i = _detail.childCount - 1; i >= 0; i--) Destroy(_detail.GetChild(i).gameObject);
            _detail.gameObject.SetActive(true);
            var card = UiFactory.Panel("Card", _detail, new Vector2(0.05f, 0.08f), new Vector2(0.95f, 0.90f), new Color(0.05f, 0.07f, 0.11f, 0.99f));
            UiFactory.Frame(card.transform, UiFactory.GradeColor(scroll.Grade), 3f);
            UiFactory.Label("Title", card.transform, new Vector2(0.05f, 0.91f), new Vector2(0.95f, 0.99f), UiFactory.GradedName(scroll), 40, UiFactory.PaperText, TextAnchor.MiddleLeft);
            UiFactory.Label("Hint", card.transform, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.91f), "숙련을 +" + scroll.MasteryUses + "회 올릴 무공을 하나 고르세요. (보유 " + Inventory.Count(save, scroll.Id) + "개)", 24, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);

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
                string artId = art.Id;
                var btn = UiFactory.Button("PickBtn" + i, rt, Vector2.zero, Vector2.one, "", UiFactory.Ink, () => ApplyScroll(scroll, artId), 28);
                UiFactory.Frame(btn.transform, UiFactory.GoldDim, 2f);
                UiFactory.Label("Name", btn.transform, new Vector2(0.04f, 0.5f), new Vector2(0.96f, 0.98f), art.Name + (Progression.IsUltimate(art) ? "  (비기)" : ""), 32, UiFactory.PaperText, TextAnchor.MiddleLeft);
                UiFactory.Label("Progress", btn.transform, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.5f), progress, 22, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
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

        private void OnEnhance(int index)
        {
            var row = _rows[index];
            if (string.IsNullOrEmpty(row.ItemId)) return;
            var item = EarlyGameContent.FindItem(row.ItemId);
            if (item != null && item.IsGear) DoEnhance(item);
        }

        private void DoEnhance(ItemData item)
        {
            var save = GameManager.Instance.Save;
            string error;
            if (Gear.Enhance(save, item.Id, out error))
            {
                _flash = item.Name + " +" + Gear.Level(save, item.Id) + " 강화 성공!";
                SoundManager.Sfx("levelup");
                GameManager.Instance.SaveNow();
            }
            else _flash = error;
            Refresh();
            ReopenDetail(item);
        }

        private static string GearLine(SaveData save)
        {
            string weapon = "없음", armor = "없음";
            var acc = new List<string>();
            foreach (var id in save.EquippedGear)
            {
                var it = EarlyGameContent.FindItem(id);
                if (it == null) continue;
                if (it.Kind == ItemKind.Weapon) weapon = it.Name; else if (it.Kind == ItemKind.Armor) armor = it.Name; else acc.Add(it.Name);
            }
            return "무기: " + weapon + "  ·  방어구: " + armor + "  ·  장신구: " + (acc.Count == 0 ? "없음" : string.Join(", ", acc)) + " (" + acc.Count + "/" + Inventory.AccessorySlots + ")";
        }

        /// <summary>Gear first (weapon, armor, accessory), then consumables, materials and keys.</summary>
        private static int KindOrder(ItemData item)
        {
            switch (item.Kind)
            {
                case ItemKind.Weapon: return 0;
                case ItemKind.Armor: return 1;
                case ItemKind.Accessory: return 2;
                case ItemKind.Consumable: return 3;
                case ItemKind.Material: return 4;
                default: return 5;
            }
        }

        private bool PassesFilter(ItemData item)
        {
            switch (_filter)
            {
                case 1:
                    if (!item.IsGear) return false;
                    return _gearFilter == 0 || (_gearFilter == 1 && item.Kind == ItemKind.Weapon) || (_gearFilter == 2 && item.Kind == ItemKind.Armor) || (_gearFilter == 3 && item.Kind == ItemKind.Accessory);
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
            _gear.text = GearLine(save);
            var setLines = Gear.SetLines(save.EquippedGear);
            _hint.text = !string.IsNullOrEmpty(_flash) ? _flash : setLines.Count > 0 ? string.Join("  |  ", setLines) : "장비는 비류연만 착용합니다. 같은 세트를 맞추면 효과가 붙습니다.";
            _flash = null;

            var stacks = save.Items
                .Where(s => EarlyGameContent.FindItem(s.Id) != null)
                .OrderBy(s => KindOrder(EarlyGameContent.FindItem(s.Id)))
                .ThenByDescending(s => (int)EarlyGameContent.FindItem(s.Id).Grade)
                .ThenBy(s => EarlyGameContent.FindItem(s.Id).Name)
                .Where(s => PassesFilter(EarlyGameContent.FindItem(s.Id)))
                .ToList();
            _gearTabRow.SetActive(_filter == 1);
            _viewport.anchorMax = new Vector2(1f, _filter == 1 ? 0.722f : 0.772f);
            for (int t = 0; t < _gearTabs.Length; t++) _gearTabs[t].color = t == _gearFilter ? new Color(0.30f, 0.48f, 0.44f, 1f) : new Color(0.12f, 0.15f, 0.20f, 0.92f);
            for (int t = 0; t < _filterTabs.Length; t++) _filterTabs[t].color = t == _filter ? new Color(0.56f, 0.44f, 0.22f, 1f) : UiFactory.InkSoft;

            _content.sizeDelta = new Vector2(0f, Mathf.Min(stacks.Count, MaxRows) * (RowH + RowGap) + RowGap);
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                bool has = i < stacks.Count;
                row.Band.SetActive(has);
                row.ItemId = has ? stacks[i].Id : null;
                if (!has) continue;

                var stack = stacks[i];
                var item = EarlyGameContent.FindItem(stack.Id);
                bool equipped = Inventory.IsEquipped(save, item.Id);
                ItemArt.Apply(row.Icon, row.IconGlyph, item);
                int enh = item.IsGear ? Gear.Level(save, item.Id) : 0;
                var set = item.IsGear ? Gear.SetOf(item.Id) : null;
                row.Title.text = UiFactory.GradedName(item) + (enh > 0 ? " +" + enh : "") + (stack.Count > 1 ? " x" + stack.Count : "") + (equipped ? "  [착용]" : "");
                row.Detail.text = item.IsGear ? StatusSheet.EffectText(item, enh) + (set != null ? "  ·  [" + set.Name + " 세트]" : "") + (StatusSheet.Flavor(item).Length > 0 ? "  —  " + StatusSheet.Flavor(item) : "") : item.Description;
                row.EnhButton.gameObject.SetActive(item.IsGear);
                var main = row.Button.GetComponent<RectTransform>();
                main.anchorMin = item.IsGear ? new Vector2(0.68f, 0.54f) : new Vector2(0.68f, 0.14f);
                main.anchorMax = item.IsGear ? new Vector2(0.97f, 0.95f) : new Vector2(0.97f, 0.86f);

                if (item.IsGear)
                {
                    long cost = Gear.NextCost(save, item.Id);
                    row.EnhLabel.text = cost < 0 ? "최대 강화" : "강화 +" + (enh + 1) + "  " + cost.ToString("N0") + "두";
                    row.EnhButton.interactable = cost >= 0;
                    row.EnhButton.GetComponent<Image>().color = cost >= 0 && save.Doo >= cost ? UiFactory.Red : UiFactory.InkSoft;
                    row.ButtonLabel.text = equipped ? "해제" : "장착";
                    row.ButtonImage.color = equipped ? UiFactory.InkSoft : UiFactory.Red;
                    row.Button.interactable = true;
                }
                else if (item.Kind == ItemKind.Material && item.SellValue > 0)
                {
                    row.ButtonLabel.text = "판매 +" + (item.SellValue * stack.Count).ToString("N0") + "두";
                    row.ButtonImage.color = UiFactory.Red;
                    row.Button.interactable = true;
                }
                else if (item.Kind == ItemKind.Material)
                {
                    row.ButtonLabel.text = "판매 불가";   // e.g. 심득석: only used in the 심법 page
                    row.ButtonImage.color = new Color(0.30f, 0.30f, 0.33f, 1f);
                    row.Button.interactable = false;
                }
                else if (item.MasteryUses > 0)
                {
                    row.ButtonLabel.text = "사용 +" + item.MasteryUses;
                    row.ButtonImage.color = UiFactory.Red;
                    row.Button.interactable = true;
                }
                else
                {
                    row.ButtonLabel.text = item.Kind == ItemKind.Consumable ? "전투 중 사용" : "보관";
                    row.ButtonImage.color = UiFactory.InkSoft;
                    row.Button.interactable = false;
                }
            }
        }
    }
}
