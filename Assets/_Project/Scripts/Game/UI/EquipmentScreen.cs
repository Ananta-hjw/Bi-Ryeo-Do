using System;
using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Equipment workspace: battle figure and worn slots on the left, inventory and actions on the right.</summary>
    public sealed class EquipmentScreen : MonoBehaviour
    {
        private static readonly string[] SlotNames = { "무기", "상의", "하의", "신발", "장신구 1", "장신구 2" };
        private RectTransform _picker, _workspace, _modal;
        private string _owner = Inventory.Hero, _selectedId, _feedback;
        private int _slot;
        private bool _built, _resetScroll = true;
        private SaveData Save { get { return GameManager.Instance.Save; } }
        private bool Hero { get { return _owner == Inventory.Hero; } }
        private CompanionData Companion { get { return EarlyGameContent.FindCompanion(_owner); } }
        private string OwnerName { get { return Hero ? "비류연" : Companion.Name; } }

        public void Build(RectTransform root)
        {
            if (_built) return;
            UiFactory.Panel("Background", root, Vector2.zero, Vector2.one, new Color(.02f, .045f, .03f, .10f));
            _workspace = UiFactory.Rect("Workspace", root, new Vector2(.02f, .03f), new Vector2(.98f, .97f));
            _modal = UiFactory.Rect("EquipmentInfo", root, Vector2.zero, Vector2.one);
            _modal.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, .85f);
            _modal.gameObject.SetActive(false);
            _picker = UiFactory.Rect("CharacterPicker", root, Vector2.zero, Vector2.one);
            _picker.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, .85f);
            _picker.gameObject.SetActive(false);
            _built = true;
            Refresh();
        }

        public void SelectOwner(string owner)
        {
            _owner = owner; _selectedId = null; _feedback = null; _resetScroll = true;
            if (_built) Refresh();
        }

        private void OnEnable() { if (_built) Refresh(); }
        private void OnDisable()
        {
            if (_modal != null) _modal.gameObject.SetActive(false);
            if (_picker != null) _picker.gameObject.SetActive(false);
        }

        private static Text Label(Transform parent, string name, float left, float bottom, float right, float top, string value, int size = 26, Color? color = null)
        {
            return UiFactory.Body(UiFactory.Label(name, parent, new Vector2(left, bottom), new Vector2(right, top), value, size,
                color ?? UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal));
        }

        private static Button Action(Transform parent, string name, float left, float bottom, float right, float top, string label, UnityEngine.Events.UnityAction action, bool enabled = true)
        {
            var button = UiFactory.Button(name, parent, new Vector2(left, bottom), new Vector2(right, top), label,
                enabled ? UiFactory.Red : UiFactory.InkSoft, action, 28);
            button.interactable = enabled;
            return button;
        }

        public void Refresh()
        {
            if (!_built || GameManager.Instance == null) return;
            if (!Hero && (Companion == null || Companion.JoinStage > Save.HighestUnlockedStage)) _owner = Inventory.Hero;
            var oldScroll = _workspace.Find("Inventory/Viewport");
            float position = !_resetScroll && oldScroll != null ? oldScroll.GetComponent<ScrollRect>().verticalNormalizedPosition : 1f;
            LandscapeMenu.Clear(_workspace);
            _picker.gameObject.SetActive(false);
            _modal.gameObject.SetActive(false);
            BuildInventory(); BuildFigure();
            Canvas.ForceUpdateCanvases();
            var scroll = _workspace.Find("Inventory/Viewport").GetComponent<ScrollRect>();
            scroll.content.GetComponent<ReadingList>()?.Reflow();
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = position;
            _resetScroll = false;
        }

        private void OpenCharacterPicker()
        {
            CharacterPicker.Open(_picker, Save, _owner, SelectOwner);
        }

        private void BuildFigure()
        {
            var left = UiFactory.Panel("Character", _workspace, Vector2.zero, new Vector2(.385f, 1f), UiFactory.Ink).rectTransform;
            UiFactory.Frame(left, UiFactory.GoldDim, 2f);
            int level = Hero ? Save.Level : CompanionGrowth.LevelOf(Save, _owner);
            var change = UiFactory.Button("ChangeCharacter", left, new Vector2(.025f, .90f), new Vector2(.59f, .99f), "", Color.clear, OpenCharacterPicker, 28);
            Label(change.transform, "Name", .02f, .02f, .85f, .98f, OwnerName + " · Lv." + level, 28, UiFactory.Gold);
            UiFactory.Label("Arrow", change.transform, new Vector2(.87f, .05f), new Vector2(.99f, .95f), "▾", 28, UiFactory.Gold);
            UiFactory.Panel("Underline", change.transform, new Vector2(.02f, 0f), new Vector2(.98f, .02f), UiFactory.GoldDim);
            Label(left, "Doo", .03f, .84f, .59f, .90f, Save.Doo.ToString("N0") + "은자", 24);
            Action(left, "Sets", .61f, .85f, .97f, .985f, "세트 효과", OpenSets);
            Action(left, "Recommend", .29f, .755f, .71f, .83f, "추천 장비", EquipRecommended);
            var figure = UiFactory.Panel("BattleFigure", left, new Vector2(.235f, .14f), new Vector2(.765f, .74f), Color.white);
            // Use only files under Art/Battle, including the battle fallback when a figure is missing.
            figure.sprite = ArtLibrary.SdFigure(_owner);
            figure.preserveAspect = true;
            if (figure.sprite == null) { figure.color = Color.clear; Label(figure.transform, "Fallback", 0f, 0f, 1f, 1f, OwnerName, 34); }
            for (int slot = 0; slot < 6; slot++)
            {
                int selected = slot;
                float x = slot < 3 ? .02f : .76f, top = .82f - slot % 3 * .24f;
                var item = Gear.ItemInSlot(Save, _owner, slot);
                var button = Action(left, "WearSlot" + slot, x, top - .20f, x + .22f, top, "", () =>
                {
                    _slot = selected; _selectedId = Gear.ItemInSlot(Save, _owner, selected)?.Id;
                    _feedback = null; _resetScroll = true; Refresh();
                });
                button.GetComponent<Image>().color = _slot == slot ? UiFactory.Jade : UiFactory.Ink;
                UiFactory.Frame(button.transform, _slot == slot ? UiFactory.Gold : item == null ? UiFactory.GoldDim : UiFactory.GradeColor(item.Grade), _slot == slot ? 4f : 2f);
                if (_slot == slot) SelectionMarker(button.transform, "선택 부위");
                if (item != null)
                {
                    var icon = UiFactory.Panel("Icon", button.transform, new Vector2(.10f, .32f), new Vector2(.90f, .79f), Color.white);
                    ItemArt.Apply(icon, UiFactory.Label("Glyph", icon.transform, Vector2.zero, Vector2.one, "", 36, UiFactory.Gold), item);
                }
                else Label(button.transform, "Empty", .05f, .38f, .95f, .86f, "미착용", 22, UiFactory.Dim);
                UiFactory.Label("Slot", button.transform, new Vector2(.03f, .025f), new Vector2(.97f, .30f),
                    SlotNames[slot] + (item == null ? "" : " +" + Gear.AppliedSlotLevel(Save, _owner, slot, item)), 22, UiFactory.PaperText);
            }
            var stats = Hero ? InnerArts.HeroStats(Save) : CompanionGrowth.Create(Save, Companion).Stats;
            UiFactory.ScrollBody(Label(left, "Stats", .025f, .005f, .975f, .125f,
                "체력 " + stats.MaxHp.ToString("N0") + " · 내공 " + stats.MaxQi.ToString("N0")
                + "\n공격 " + stats.Attack.ToString("N0") + " · 방어 " + stats.Defense.ToString("N0") + " · 속도 " + stats.Speed, 24), 24);
        }

        private void BuildInventory()
        {
            var right = UiFactory.Rect("Inventory", _workspace, new Vector2(.405f, 0f), Vector2.one);
            var groups = SlotNames;
            for (int i = 0; i < groups.Length; i++)
            {
                int group = i;
                var button = Action(right, "Group" + i, i / 6f, .88f, (i + 1) / 6f - .008f, 1f, groups[i], () =>
                {
                    _slot = group;
                    _selectedId = null; _feedback = null; _resetScroll = true; Refresh();
                });
                button.GetComponent<Image>().color = i == _slot ? UiFactory.Red : UiFactory.InkSoft;
            }
            var worn = Inventory.GearOf(Save, _owner);
            var items = Save.Items.Select(stack => EarlyGameContent.FindItem(stack.Id))
                .Where(item => item != null && item.IsGear && item.Kind == Gear.KindForSlot(_slot) && Inventory.Count(Save, item.Id) > 0 && (Hero || !Gear.IsHeroOnly(item.Id)))
                .Distinct().OrderBy(item => !worn.Contains(item.Id) && Inventory.Free(Save, item.Id) <= 0)
                .ThenByDescending(item => worn.Contains(item.Id)).ThenByDescending(item => item.Grade).ThenBy(item => item.Name).ToList();
            var current = Gear.ItemInSlot(Save, _owner, _slot);
            if (!items.Any(item => item.Id == _selectedId)) _selectedId = current?.Id ?? items.FirstOrDefault(item => item.Kind == Gear.KindForSlot(_slot))?.Id;
            var selected = items.FirstOrDefault(item => item.Id == _selectedId);
            Label(right, "Hint", .02f, .80f, .98f, .87f, SlotNames[_slot] + " · 보유 " + items.Count + "종 · " + (_feedback ?? "선택한 장비의 효과와 비용을 아래에서 확인할 수 있습니다."), 22, UiFactory.Gold);
            var content = CodexPanel.Scroller(right, .35f, .79f);
            RectTransform row = null;
            for (int i = 0; i < items.Count; i++)
            {
                if (i % 7 == 0)
                {
                    int index = i / 7;
                    row = UiFactory.Rect("Row" + index, content, new Vector2(.01f, 1f), new Vector2(.99f, 1f));
                    row.pivot = new Vector2(.5f, 1f); row.sizeDelta = new Vector2(0f, 300f);
                    row.anchoredPosition = new Vector2(0f, -14f - index * 314f);
                    ReadingList.Register(row, 300f, 14f);
                }
                var item = items[i];
                float x = i % 7 / 7f;
                var card = UiFactory.Panel("Item_" + item.Id, row, new Vector2(x, 0f), new Vector2(x + 1f / 7f - .008f, 1f), item.Id == _selectedId ? UiFactory.InkSoft : UiFactory.Ink).rectTransform;
                card.GetComponent<Image>().raycastTarget = true;
                var tap = card.gameObject.AddComponent<Button>(); tap.targetGraphic = card.GetComponent<Image>(); tap.transition = Selectable.Transition.None;
                tap.onClick.AddListener(() =>
                {
                    _selectedId = item.Id; _feedback = null; Refresh();
                });
                UiFactory.Frame(card, item.Id == _selectedId ? UiFactory.Gold : UiFactory.GradeColor(item.Grade), item.Id == _selectedId ? 4f : 2f);
                var icon = UiFactory.TopIcon(card, 88f, 34f);
                ItemArt.Apply(icon, UiFactory.Label("Glyph", icon.transform, Vector2.zero, Vector2.one, "", 36, UiFactory.Gold), item);
                Label(card, "Name", .06f, .28f, .94f, .53f, item.Name, 22, UiFactory.GradeColor(item.Grade));
                var wearers = Inventory.WearerNames(Save, item.Id, _owner);
                if (worn.Contains(item.Id)) wearers.Add(OwnerName);
                string ownership = wearers.Count == 0 ? Gear.StarsText(Save, item.Id)
                    : "\"" + wearers[0] + "\"" + (wearers.Count > 1 ? " 외 " + (wearers.Count - 1) + "명" : "") + "\n장착 중";
                Label(card, "State", .04f, .015f, .96f, .27f, ownership, 20, wearers.Count > 0 ? UiFactory.Jade : UiFactory.Dim);
                if (item.Id == _selectedId) SelectionMarker(card, "선택 장비");
            }
            if (items.Count == 0) Label(right, "Empty", .05f, .40f, .95f, .68f, "보유한 장비가 없습니다.", 30, UiFactory.Dim);
            BuildActions(right, selected);
        }

        private static void SelectionMarker(Transform parent, string caption)
        {
            var band = UiFactory.Panel("Selected", parent, new Vector2(.02f, 1f), new Vector2(.98f, 1f), UiFactory.Gold);
            band.rectTransform.pivot = new Vector2(.5f, 1f);
            band.rectTransform.sizeDelta = new Vector2(0f, 28f);
            band.rectTransform.anchoredPosition = new Vector2(0f, -2f);
            UiFactory.Label("Caption", band.transform, Vector2.zero, Vector2.one, caption, 18, UiFactory.Ink, TextAnchor.MiddleCenter);
            band.raycastTarget = false;
        }

        private string SelectionHint(ItemData item, int count)
        {
            if (item == null) return "해당 부위의 장비를 획득하면 여기에 표시됩니다.";
            bool worn = Gear.ItemInSlot(Save, _owner, _slot)?.Id == item.Id;
            if (!worn) return "선택한 장비는 아래 버튼으로 장착할 수 있습니다.";
            if (!Gear.IsGradeUnlocked(Save, _owner, _slot, item.Grade)) return "등급 개방 비용 " + Gear.GradeUnlockCost(Save, _owner, _slot, item.Grade).ToString("N0") + "은자";
            int next = Gear.SlotLevel(Save, _owner, _slot) + 1;
            if (next > Gear.MaxLevel) return "최대 강화에 도달했습니다.";
            if (next > Gear.Cap(Save, item.Id)) return "현재 강화 상한입니다. 한계돌파로 상한을 높일 수 있습니다.";
            var stone = Gear.MaterialForLevel(next);
            return "다음 강화: " + stone.Name + " " + Inventory.Count(Save, stone.Id) + "/" + Gear.StoneCost(next) + "개 · " + Gear.DooCost(next).ToString("N0") + "은자";
        }

        private void BuildActions(RectTransform parent, ItemData item)
        {
            var panel = UiFactory.Panel("Selection", parent, Vector2.zero, new Vector2(1f, .33f), UiFactory.Ink).rectTransform;
            bool worn = item != null && Gear.ItemInSlot(Save, _owner, _slot)?.Id == item.Id;
            bool otherSlot = item != null && !worn && Inventory.GearOf(Save, _owner).Contains(item.Id);
            bool available = item != null && !worn && !otherSlot && Inventory.Free(Save, item.Id) > 0;
            Label(panel, "Name", .025f, .75f, .635f, .98f, item == null ? "장비를 선택해 주세요" : UiFactory.GradedName(item) + " · " + Gear.StarsText(Save, item.Id), 28, UiFactory.Gold);
            var others = Inventory.WearerNames(Save, item?.Id, _owner);
            UiFactory.ScrollBody(Label(panel, "State", .025f, .54f, .635f, .74f, item == null ? "선택한 장비의 조작 버튼이 여기에 표시됩니다."
                : SlotNames[_slot] + " · " + (worn ? "착용 중" : otherSlot ? "다른 장신구 칸에 착용 중" : available ? "장착 가능" : string.Join(" · ", others.Select(name => "\"" + name + "\"")) + " 장착 중")
                  + " · 적용 +" + Gear.AppliedSlotLevel(Save, _owner, _slot, item) + " / 상한 +" + Gear.Cap(Save, item.Id), 24), 24);
            UiFactory.ScrollBody(Label(panel, "Effects", .025f, .35f, .635f, .53f,
                item == null ? "" : UiFactory.GearEffectText(item, Gear.AppliedSlotLevel(Save, _owner, _slot, item), Gear.Stars(Save, item.Id))
                    + " · 세트: " + (Gear.SetOf(item.Id)?.Name ?? "없음"), 22), 22);
            var costPanel = UiFactory.Panel("Costs", panel, new Vector2(.66f, .35f), new Vector2(.985f, .98f), UiFactory.InkSoft).rectTransform;
            UiFactory.Frame(costPanel, UiFactory.GoldDim, 2f);
            string costText;
            bool affordable = true;
            if (item == null) costText = "개방 · 강화 비용\n장비를 선택해 주세요.";
            else if (!Gear.IsGradeUnlocked(Save, _owner, _slot, item.Grade))
            {
                long unlock = Gear.GradeUnlockCost(Save, _owner, _slot, item.Grade);
                affordable = Save.Doo >= unlock;
                costText = Grades.NameOf(item.Grade) + " 등급 개방\n필요 " + unlock.ToString("N0") + "은자\n보유 " + Save.Doo.ToString("N0") + "은자";
            }
            else
            {
                int next = Gear.SlotLevel(Save, _owner, _slot) + 1;
                if (next > Gear.MaxLevel) costText = "최대 강화 +" + Gear.MaxLevel;
                else if (next > Gear.Cap(Save, item.Id)) costText = "강화 상한 +" + Gear.Cap(Save, item.Id) + "\n한계돌파가 필요합니다.";
                else
                {
                    var material = Gear.MaterialForLevel(next);
                    int need = Gear.StoneCost(next), have = Inventory.Count(Save, material.Id);
                    long silver = Gear.DooCost(next);
                    affordable = have >= need && Save.Doo >= silver;
                    costText = "강화 +" + next + " (보유/필요)\n" + material.Name + " " + have.ToString("N0") + "/" + need + "개\n은자 " + Save.Doo.ToString("N0") + "/" + silver.ToString("N0");
                }
            }
            if (item != null && !worn) costText += "\n장착 후 진행할 수 있습니다.";
            UiFactory.ScrollBody(Label(costPanel, "Text", .05f, .04f, .95f, .96f, costText, 22, affordable ? UiFactory.Gold : new Color(1f, .55f, .45f)), 22);
            Action(panel, "Equip", .015f, .025f, .245f, .33f, "장착", () =>
            {
                string error;
                Complete(Inventory.EquipInSlot(Save, _owner, Gear.KindForSlot(_slot), _slot == 5 ? 1 : 0, item.Id, out error), error, "장비를 장착했습니다.");
            }, available);
            Action(panel, "Remove", .26f, .025f, .49f, .33f, "해제", () =>
            {
                Inventory.UnequipFor(Save, _owner, item.Id); Complete(true, null, "장비를 해제했습니다.");
            }, worn);
            Action(panel, "Details", .505f, .025f, .735f, .33f, "상세", () => OpenDetails(item), item != null);
            bool unlocked = item != null && Gear.IsGradeUnlocked(Save, _owner, _slot, item.Grade);
            long cost = item == null ? 0 : Gear.GradeUnlockCost(Save, _owner, _slot, item.Grade);
            int level = Gear.SlotLevel(Save, _owner, _slot);
            string enhance = !worn ? "강화" : !unlocked ? "개방" : level >= Gear.MaxLevel ? "최대 강화" : level >= Gear.Cap(Save, item.Id) ? "돌파 필요" : "강화 +" + (level + 1);
            Action(panel, "Enhance", .75f, .025f, .98f, .33f, enhance, () =>
            {
                string error;
                bool success = unlocked ? Gear.EnhanceSlot(Save, _owner, _slot, out error) : Gear.UnlockSlotGrade(Save, _owner, _slot, out error);
                Complete(success, error, unlocked ? "강화했습니다." : "등급을 개방했습니다.");
            }, worn && (unlocked ? Gear.CanEnhanceSlot(Save, _owner, _slot) : Save.Doo >= cost));
        }

        private void EquipRecommended()
        {
            string error;
            int changed;
            if (!EquipmentRecommendation.Equip(Save, _owner, out changed, out error))
            { _feedback = error; Refresh(); return; }
            _selectedId = null;
            if (changed == 0) { _feedback = "현재 장비가 추천 조합입니다."; Refresh(); return; }
            Complete(true, null, "추천 장비 " + changed + "부위를 장착했습니다.");
        }

        private void Complete(bool success, string error, string message)
        {
            if (success) { GameManager.Instance.SaveNow(); SoundManager.Sfx("click"); _resetScroll = true; }
            _feedback = success ? message : error; Refresh();
        }

        private void OpenDetails(ItemData item)
        {
            string text = UiFactory.GearEffectText(item, Gear.AppliedSlotLevel(Save, _owner, _slot, item), Gear.Stars(Save, item.Id))
                + "\n\n돌파 추가 효과\n" + UiFactory.GearTraitsText(item, Gear.Stars(Save, item.Id))
                + "\n\n장착 중: " + (Inventory.WearerNames(Save, item.Id).Count == 0 ? "없음" : string.Join(" · ", Inventory.WearerNames(Save, item.Id)))
                + "\n\n세트: " + (Gear.SetOf(item.Id)?.Name ?? "없음") + "\n보유 " + Inventory.Count(Save, item.Id) + "개 · 여분 " + Gear.Spares(Save, item.Id) + "개"
                + "\n부위 강화 +" + Gear.SlotLevel(Save, _owner, _slot) + " · 적용 +" + Gear.AppliedSlotLevel(Save, _owner, _slot, item) + " · 상한 +" + Gear.Cap(Save, item.Id)
                + "\n\n" + SelectionHint(item, 0) + "\n\n" + StatusSheet.Flavor(item);
            ShowInfo(UiFactory.GradedName(item) + " · " + Gear.StarsText(Save, item.Id), text);
        }

        private void OpenSets()
        {
            var sets = Gear.SetStatus(Inventory.GearOf(Save, _owner));
            ShowInfo(OwnerName + " · 세트 효과", sets.Count == 0 ? "착용 중인 세트 장비가 없습니다." : string.Join("\n\n", sets.Select(set => string.Join("\n", set))));
        }

        private void ShowInfo(string title, string text)
        {
            LandscapeMenu.Clear(_modal); _modal.gameObject.SetActive(true); _modal.SetAsLastSibling();
            var card = UiFactory.Panel("Card", _modal, new Vector2(.15f, .06f), new Vector2(.85f, .94f), UiFactory.Ink).rectTransform;
            UiFactory.Frame(card, UiFactory.GoldDim, 2f);
            Label(card, "Title", .04f, .85f, .96f, .98f, title, 34, UiFactory.Gold);
            UiFactory.ScrollBody(Label(card, "Body", .04f, .17f, .96f, .83f, text, 28), 28);
            UiFactory.BackButton(card, new Vector2(.30f, .015f), new Vector2(.70f, .145f), () => _modal.gameObject.SetActive(false));
        }
    }
}
