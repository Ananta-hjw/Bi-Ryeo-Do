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
        private static readonly string[] Pages = { "상태", "장비", "수련", "무공", "인연" };
        private static readonly string[] Slots = { "무기", "상의", "하의", "신발", "장신구 1", "장신구 2" };
        private RectTransform _page, _identity, _people, _tabs, _message;
        private string _owner = Inventory.Hero, _selectedItem, _toast;
        private int _tab, _slot;
        private Action _openArts, _openParty;
        private bool _built, _resetScroll = true;
        private SaveData Save { get { return GameManager.Instance.Save; } }
        private CompanionData Companion { get { return EarlyGameContent.FindCompanion(_owner); } }
        private string OwnerName { get { return _owner == Inventory.Hero ? "비류연" : Companion.Name; } }

        public void Build(RectTransform root, Action openArts, Action openParty)
        {
            _openArts = openArts; _openParty = openParty;
            UiFactory.Panel("Background", root, Vector2.zero, Vector2.one, UiFactory.Parchment);
            _people = UiFactory.Rect("People", root, new Vector2(.02f, .855f), new Vector2(.98f, .985f));
            _identity = UiFactory.Rect("Identity", root, new Vector2(.02f, .04f), new Vector2(.255f, .835f));
            _tabs = UiFactory.Rect("Pages", root, new Vector2(.275f, .755f), new Vector2(.98f, .835f));
            _page = UiFactory.Rect("Page", root, new Vector2(.275f, .085f), new Vector2(.98f, .74f));
            _message = UiFactory.Rect("Message", root, new Vector2(.28f, .015f), new Vector2(.975f, .075f));
            _built = true;
            Refresh();
        }

        private void OnEnable() { if (_built) Refresh(); }

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
                _selectedItem = null;
            }
            var oldList = _page.Find("List/Viewport");
            float position = !_resetScroll && oldList != null ? oldList.GetComponent<ScrollRect>().verticalNormalizedPosition : 1f;
            Clear(_people); Clear(_identity); Clear(_tabs); Clear(_page); Clear(_message);
            BuildPeople(); BuildIdentity();
            for (int i = 0; i < Pages.Length; i++)
            {
                int page = i;
                var button = Button(_tabs, "Page" + i, i * .202f, 0f, i * .202f + .192f, 1f, Pages[i], () => { _tab = page; _resetScroll = true; _toast = null; Refresh(); });
                button.GetComponent<Image>().color = i == _tab ? UiFactory.Red : UiFactory.InkSoft;
                if (i == _tab) UiFactory.Frame(button.transform, UiFactory.Gold, 2f);
            }
            switch (_tab)
            {
                case 0: BuildStatus(); break;
                case 1: BuildEquipment(); break;
                case 2: BuildTraining(); break;
                case 3: BuildArts(); break;
                case 4: BuildBonds(); break;
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
            Reading(_message, "Feedback", 0f, 0f, 1f, 1f, _toast ?? "인물을 바꿔도 선택한 탭과 장비 부위를 유지합니다.", 26).color = UiFactory.Gold;
        }

        private void BuildPeople()
        {
            var owners = new List<string> { Inventory.Hero };
            owners.AddRange(EarlyGameContent.Companions.Where(c => c.JoinStage <= Save.HighestUnlockedStage).Select(c => c.Id));
            var viewport = UiFactory.Rect("Viewport", _people, Vector2.zero, Vector2.one);
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = UiFactory.Rect("Content", viewport, Vector2.zero, new Vector2(0f, 1f));
            content.pivot = new Vector2(0f, .5f);
            content.sizeDelta = new Vector2(owners.Count * 228f, 0f);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport; scroll.content = content; scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            for (int i = 0; i < owners.Count; i++)
            {
                string owner = owners[i];
                string name = owner == Inventory.Hero ? "비류연" : EarlyGameContent.FindCompanion(owner).Name;
                var button = Button(content, "Person_" + owner, 0f, .03f, 0f, .97f, "", () => { _owner = owner; _selectedItem = null; _resetScroll = true; _toast = null; Refresh(); });
                var rect = button.GetComponent<RectTransform>();
                rect.pivot = new Vector2(0f, .5f); rect.anchoredPosition = new Vector2(i * 228f, 0f); rect.sizeDelta = new Vector2(212f, 0f);
                button.GetComponent<Image>().color = owner == _owner ? new Color(.13f, .27f, .24f) : UiFactory.Ink;
                UiFactory.Frame(button.transform, owner == _owner ? UiFactory.Gold : UiFactory.GoldDim, 2f);
                Portrait(button.transform, name, .03f, .05f, .40f, .95f);
                Text(button.transform, "Name", .45f, .18f, .97f, .85f, name, 28, owner == _owner ? UiFactory.Gold : UiFactory.PaperText);
            }
            // Keep the selected person visible when the roster exceeds the screen width.
            int selected = owners.IndexOf(_owner);
            Canvas.ForceUpdateCanvases();
            float overflow = Mathf.Max(0f, content.rect.width - viewport.rect.width);
            content.anchoredPosition = new Vector2(-Mathf.Min(Mathf.Max(0f, selected * 228f - viewport.rect.width + 228f), overflow), 0f);
        }

        private static void Portrait(Transform parent, string name, float x, float y, float right, float top)
        {
            var picture = UiFactory.Panel("Portrait", parent, new Vector2(x, y), new Vector2(right, top), Color.white);
            picture.sprite = ArtLibrary.Portrait(name); picture.preserveAspect = true;
            if (picture.sprite == null) { picture.color = Color.clear; Text(picture.transform, "Glyph", 0f, 0f, 1f, 1f, name.Substring(0, 1), 50, UiFactory.Gold); }
        }

        private void BuildIdentity()
        {
            UiFactory.Frame(_identity, UiFactory.GoldDim, 2f);
            Portrait(_identity, OwnerName, .03f, .28f, .97f, .98f);
            Text(_identity, "Name", .06f, .21f, .94f, .32f, OwnerName, 42, UiFactory.Gold);
            bool hero = _owner == Inventory.Hero;
            int level = hero ? Save.Level : CompanionGrowth.LevelOf(Save, _owner);
            string realm = hero ? Realm.Label(Realm.StepOf(Realm.Power(InnerArts.HeroStatsBeforeRealm(Save)))) : StatusSheet.AllyRealm(Save, Companion);
            var fielded = EarlyGameContent.SelectCompanionsFor(Math.Max(1, Save.HighestUnlockedStage), Save);
            string deployment = hero ? "주인공" : fielded.Any(c => c.Id == _owner) ? "출전 편성" : "대기 중";
            Reading(_identity, "Summary", .06f, .105f, .94f, .21f, "Lv." + level + " · " + (hero ? Promotion.RankName(Promotion.EffectiveRank(Save)) : Companion.Role) + " · " + deployment + "\n" + realm, 26);
            Button(_identity, "Formation", .06f, .015f, .94f, .09f, "파티 편성", _openParty);
        }

        private void BuildStatus()
        {
            var lines = _owner == Inventory.Hero ? StatusSheet.Hero(Save) : StatusSheet.Ally(Save, Companion);
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i]; int column = i % 2, row = i / 2;
                var panel = UiFactory.Panel("Stat" + i, _page, new Vector2(column * .51f, .79f - row * .22f), new Vector2(column * .51f + .49f, .99f - row * .22f), UiFactory.Ink);
                Text(panel.transform, "Label", .04f, .52f, .96f, .97f, line.Label, 26, UiFactory.Dim);
                Text(panel.transform, "Value", .04f, .04f, .58f, .56f, line.Value, 34, UiFactory.Gold);
                Text(panel.transform, "Bonus", .60f, .04f, .97f, .56f, line.Bonus, 26, UiFactory.Jade);
            }
            if (_owner != Inventory.Hero)
            {
                var stats = CompanionGrowth.Create(Save, Companion).Stats;
                Reading(_page, "Critical", 0f, .05f, 1f, .31f, "치명타율 " + (stats.CritRate * 100f).ToString("0.#") + "% · 치명타 피해 x" + stats.CritDamage.ToString("0.##") + "\n장비·인연·수련을 반영한 전투 능력치입니다.");
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

        private void BuildEquipment()
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                int slot = i;
                var button = Button(_page, "Slot" + i, i * .168f, .91f, i * .168f + .158f, 1f, Slots[i], () => { _slot = slot; _selectedItem = null; _resetScroll = true; _toast = null; Refresh(); });
                button.GetComponent<Image>().color = i == _slot ? UiFactory.Red : UiFactory.InkSoft;
            }
            var current = Gear.ItemInSlot(Save, _owner, _slot);
            int stored = Gear.SlotLevel(Save, _owner, _slot), next = stored + 1;
            bool unlocked = current != null && Gear.IsGradeUnlocked(Save, _owner, _slot, current.Grade);
            var material = Gear.MaterialForLevel(next);
            string cost = stored >= Gear.MaxLevel ? "최대 강화" : "다음 강화: " + material.Name + " " + Inventory.Count(Save, material.Id) + "/" + Gear.StoneCost(next) + "개 · " + Gear.DooCost(next).ToString("N0") + "두";
            Reading(_page, "Resources", 0f, .80f, .995f, .90f, Save.Doo.ToString("N0") + "두 · " + Grades.NameOf(Gear.UnlockedGrade(Save, _owner, _slot)) + "까지 개방\n" + cost, 26);
            var setLines = Gear.SetLines(Inventory.GearOf(Save, _owner));
            Reading(_page, "Sets", 0f, .69f, .995f, .79f, "현재 세트 효과 · " + (setLines.Count > 0 ? string.Join(" / ", setLines) : "없음"), 26);

            var equipped = UiFactory.Panel("Equipped", _page, new Vector2(0f, 0f), new Vector2(.30f, .675f), UiFactory.Ink);
            UiFactory.Frame(equipped.transform, UiFactory.GoldDim, 2f);
            Text(equipped.transform, "Heading", .06f, .88f, .94f, .98f, "현재 착용 · " + Slots[_slot], 26, UiFactory.Gold);
            Reading(equipped.transform, "Name", .06f, .64f, .94f, .86f, current == null ? "비어 있음" : UiFactory.GradedName(current) + " · " + Gear.StarsText(Save, current.Id));
            Reading(equipped.transform, "Effect", .06f, .30f, .94f, .63f, current == null ? "목록에서 장비를 선택해 주세요." : UiFactory.GearEffectText(current, Gear.AppliedSlotLevel(Save, _owner, _slot, current), Gear.Stars(Save, current.Id)), 26);
            Reading(equipped.transform, "Growth", .06f, .19f, .94f, .29f, "부위 +" + stored + " · 적용 +" + (current == null ? 0 : Gear.AppliedSlotLevel(Save, _owner, _slot, current)) + "\n상한 +" + (current == null ? 10 : Gear.Cap(Save, current.Id)), 24);
            long unlockCost = current == null ? 0 : Gear.GradeUnlockCost(Save, _owner, _slot, current.Grade);
            string upgrade = current == null ? "강화" : !unlocked ? "개방 " + unlockCost.ToString("N0") + "두" : stored >= Gear.MaxLevel ? "최대 강화" : stored >= Gear.Cap(Save, current.Id) ? "한계돌파 필요" : "강화 +" + next;
            Button(equipped.transform, "Enhance", .04f, .025f, .62f, .17f, upgrade, () =>
            {
                string error;
                bool success = !unlocked ? Gear.UnlockSlotGrade(Save, _owner, _slot, out error) : Gear.EnhanceSlot(Save, _owner, _slot, out error);
                Complete(success, error, !unlocked ? "등급을 개방했습니다." : "강화했습니다.");
            }, current != null && (!unlocked ? Save.Doo >= unlockCost : Gear.CanEnhanceSlot(Save, _owner, _slot)));
            Button(equipped.transform, "Remove", .66f, .025f, .96f, .17f, "해제", () => { Inventory.UnequipFor(Save, _owner, current.Id); Complete(true, null, "장비를 해제했습니다."); }, current != null);

            var candidates = Save.Items.Select(stack => EarlyGameContent.FindItem(stack.Id))
                .Where(item => item != null && item.IsGear && item.Kind == Gear.KindForSlot(_slot) && Inventory.Count(Save, item.Id) > 0 && (_owner == Inventory.Hero || !Gear.IsHeroOnly(item.Id)))
                .Distinct().OrderByDescending(item => current != null && item.Id == current.Id).ThenByDescending(item => item.Grade).ThenBy(item => item.Name).ToList();
            if (_selectedItem == null || !candidates.Any(item => item.Id == _selectedItem)) _selectedItem = current != null ? current.Id : candidates.FirstOrDefault()?.Id;
            var list = List(_page, .315f, 0f, .65f, .675f, candidates.Count, 210f);
            for (int i = 0; i < candidates.Count; i++)
            {
                var item = candidates[i]; var row = Row(list, i, 210f);
                var tap = row.gameObject.AddComponent<Button>(); tap.targetGraphic = row.GetComponent<Image>(); tap.transition = Selectable.Transition.None;
                tap.onClick.AddListener(() => { _selectedItem = item.Id; _toast = null; Refresh(); });
                if (item.Id == _selectedItem) UiFactory.Frame(row, UiFactory.Gold, 2f);
                bool worn = current != null && item.Id == current.Id;
                Text(row, "Name", .05f, .66f, .95f, .98f, (worn ? "[착용] " : "") + UiFactory.GradedName(item) + " · " + Gear.StarsText(Save, item.Id), 26);
                Text(row, "Effect", .05f, .18f, .95f, .65f, UiFactory.GearEffectText(item, Gear.AppliedSlotLevel(Save, _owner, _slot, item), Gear.Stars(Save, item.Id)), 24);
                Text(row, "Set", .05f, .015f, .95f, .18f, Gear.SetOf(item.Id)?.Name ?? "세트 없음", 24, UiFactory.Gold);
            }
            if (candidates.Count == 0) Reading(_page, "Empty", .32f, .20f, .65f, .50f, "이 부위에 장착할 수 있는 장비가 없습니다.");
            BuildEquipmentDetail(EarlyGameContent.FindItem(_selectedItem), current);
        }

        private void BuildEquipmentDetail(ItemData item, ItemData current)
        {
            var detail = UiFactory.Panel("Detail", _page, new Vector2(.665f, 0f), new Vector2(1f, .675f), UiFactory.Ink);
            UiFactory.Frame(detail.transform, item != null ? UiFactory.GradeColor(item.Grade) : UiFactory.GoldDim, 2f);
            if (item == null) { Reading(detail.transform, "Empty", .06f, .3f, .94f, .7f, "장비를 선택하면 상세정보를 볼 수 있습니다."); return; }
            bool worn = current != null && item.Id == current.Id;
            bool otherSlot = !worn && Inventory.GearOf(Save, _owner).Contains(item.Id);
            bool available = !worn && !otherSlot && Inventory.Free(Save, item.Id) > 0;
            var icon = UiFactory.Panel("Picture", detail.transform, new Vector2(.70f, .81f), new Vector2(.95f, .98f), Color.white);
            ItemArt.Apply(icon, UiFactory.Label("Glyph", icon.transform, Vector2.zero, Vector2.one, "", 40, UiFactory.Gold), item);
            Reading(detail.transform, "Name", .05f, .81f, .68f, .98f, UiFactory.GradedName(item) + "\n" + Gear.StarsText(Save, item.Id), 26);
            string state = worn ? "이 부위에 착용 중" : otherSlot ? "같은 인물의 다른 칸에 착용 중" : !available ? "다른 인물이 착용 중" : "장착 가능";
            Reading(detail.transform, "Body", .06f, .19f, .94f, .79f,
                state + "\n\n장착 시 효과\n" + UiFactory.GearEffectText(item, Gear.AppliedSlotLevel(Save, _owner, _slot, item), Gear.Stars(Save, item.Id))
                + "\n\n세트: " + (Gear.SetOf(item.Id)?.Name ?? "없음") + "\n강화 상한 +" + Gear.Cap(Save, item.Id)
                + "\n보유 " + Inventory.Count(Save, item.Id) + "개 · 여분 " + Gear.Spares(Save, item.Id) + "개"
                + "\n\n" + UiFactory.GearTraitsText(item, Gear.Stars(Save, item.Id)) + "\n\n" + StatusSheet.Flavor(item), 26);
            Button(detail.transform, "Equip", .06f, .03f, .94f, .16f, worn ? "착용 중" : otherSlot ? "다른 칸 착용 중" : available ? "장착" : "다른 인물 착용 중", () =>
            {
                string error;
                bool success = Inventory.EquipInSlot(Save, _owner, Gear.KindForSlot(_slot), _slot == 5 ? 1 : 0, item.Id, out error);
                Complete(success, error, item.Name + "을(를) 장착했습니다.");
            }, available);
        }

        private void BuildTraining()
        {
            if (_owner == Inventory.Hero) { BuildInnerArts(); return; }
            int level = CompanionTraining.LevelOf(Save, _owner), cap = CompanionTraining.Cap(Save, _owner);
            long cost = CompanionTraining.NextCost(Save, _owner);
            Text(_page, "Level", .02f, .84f, .98f, .98f, "수련 Lv." + level + " / " + CompanionTraining.MaxLevel, 42, UiFactory.Gold);
            var bar = UiFactory.MakeBar("Progress", _page, new Vector2(.02f, .78f), new Vector2(.98f, .81f), UiFactory.Ink, UiFactory.Jade);
            bar.SetPct((float)level / CompanionTraining.MaxLevel);
            Reading(_page, "Training", .02f, .27f, .98f, .75f,
                "현재 체력·공격·방어 +" + Math.Round((CompanionTraining.StatMultiplier(level) - 1f) * 100f) + "%"
                + "\n인연 " + Bonds.LevelNames[Bonds.Level(Save, _owner)] + " · 현재 수련 한도 Lv." + cap
                + (level < CompanionTraining.MaxLevel ? "\n다음 단계: 체력·공격·방어 +4% · " + cost.ToString("N0") + "두" : "\n모든 단계를 달성했습니다.")
                + "\n\n" + CompanionTraining.MilestoneText(_owner, level), 30);
            string label = level >= CompanionTraining.MaxLevel ? "최고 단계" : level >= cap ? "인연을 더 쌓아 주세요" : Save.Doo < cost ? "두가 부족합니다 · " + cost.ToString("N0") + "두 필요" : "수련 · " + cost.ToString("N0") + "두";
            Button(_page, "Train", .20f, .06f, .80f, .23f, label, () => { string error; bool success = CompanionTraining.Train(Save, _owner, out error); Complete(success, error, "수련을 완료했습니다."); }, level < cap && level < CompanionTraining.MaxLevel && Save.Doo >= cost);
        }

        private void BuildInnerArts()
        {
            Reading(_page, "HeroGrowth", 0f, .80f, 1f, 1f, "주인공 성장 · Lv." + Save.Level + " · 경험 " + Save.Exp.ToString("N0") + "/" + Progression.ExpToNext(Save.Level).ToString("N0")
                + "\n검룡위 " + Promotion.RankName(Promotion.EffectiveRank(Save)) + " · 장착 심법 " + Save.EquippedInner.Count + "/" + InnerArts.SlotCount(Save)
                + " · 오성석 " + InnerArts.Stones(Save), 28);
            var list = List(_page, 0f, 0f, 1f, .78f, InnerArts.All.Length, 220f);
            for (int i = 0; i < InnerArts.All.Length; i++)
            {
                var art = InnerArts.All[i]; var row = Row(list, i, 220f);
                bool learned = InnerArts.IsLearned(Save, art.Id), equipped = InnerArts.IsEquipped(Save, art.Id), unlocked = InnerArts.IsUnlocked(Save, art);
                int level = InnerArts.Level(Save, art.Id);
                Text(row, "Name", .03f, .72f, .66f, .98f, art.Name + " +" + level + (equipped ? " · 장착 중" : ""), 30, UiFactory.Gold);
                Text(row, "Effects", .03f, .05f, .66f, .70f, InnerArts.EffectText(art, level) + "\n" + (unlocked ? art.Description : InnerArts.UnlockText(art)), 26);
                Button(row, "Equip", .70f, .51f, .97f, .94f, !unlocked ? "미해금" : !learned ? "습득 " + art.LearnCost.ToString("N0") + "두" : equipped ? "해제" : "장착", () =>
                {
                    string error = null; bool success;
                    if (!learned) success = InnerArts.Learn(Save, art.Id, out error);
                    else if (equipped) { InnerArts.Unequip(Save, art.Id); success = true; }
                    else success = InnerArts.Equip(Save, art.Id, out error);
                    Complete(success, error, "심법을 변경했습니다.");
                }, unlocked && (learned || Save.Doo >= art.LearnCost));
                int cost = InnerArts.NextCost(Save, art.Id);
                Button(row, "Enhance", .70f, .06f, .97f, .45f, level >= InnerArts.MaxLevel ? "최대 강화" : "강화 · 오성석 " + cost, () => { string error; bool success = InnerArts.Enhance(Save, art.Id, out error); Complete(success, error, "심법을 강화했습니다."); }, learned && level < InnerArts.MaxLevel && InnerArts.Stones(Save) >= cost);
            }
        }

        private void BuildArts()
        {
            bool hero = _owner == Inventory.Hero;
            var arts = EarlyGameContent.Arts.Where(art => hero ? art.OwnerId == null : art.OwnerId == _owner).ToList();
            var skills = hero ? null : CompanionGrowth.Create(Save, Companion).Skills;
            var list = List(_page, 0f, .11f, 1f, 1f, arts.Count, 230f);
            for (int i = 0; i < arts.Count; i++)
            {
                var art = arts[i]; var row = Row(list, i, 230f);
                bool learned = hero ? Save.LearnedArts.Contains(art.Id) : art.OwnerLevel <= CompanionGrowth.LevelOf(Save, _owner);
                var spec = hero ? (learned ? Mastery.Apply(Save, art) : art.ToSpec()) : skills.FirstOrDefault(skill => skill.Id == art.Id) ?? art.ToSpec();
                bool equipped = hero && Save.EquippedArts.Contains(art.Id);
                Text(row, "Name", .03f, .72f, .68f, .98f, art.Name + (hero ? equipped ? " · 장착 중" : learned ? " · 습득" : "" : learned ? " · 사용 가능" : " · Lv." + art.OwnerLevel + " 해금"), 30, UiFactory.Gold);
                string numbers = "위력 " + spec.Power.ToString("0.##") + "배 · " + Math.Max(1, spec.Hits) + "타 · 내공 " + spec.QiCost + " · 기세 " + spec.SpiritCost + " · 재사용 " + spec.Cooldown + "턴";
                Text(row, "Description", .03f, .04f, hero ? .68f : .97f, .70f, numbers + "\n" + art.Description + (hero && learned ? "\n숙련 " + Mastery.LevelNames[Mastery.Level(Save, art.Id)] + " · 사용 " + Mastery.Uses(Save, art.Id) + "회" : ""), 26);
                if (!hero) continue;
                bool unlocked = art.IsUnlocked(Save);
                Button(row, "Action", .71f, .48f, .97f, .94f, !unlocked && !learned ? art.UnlockText : !learned ? "습득 " + art.LearnCost.ToString("N0") + "두" : equipped ? "해제" : "장착", () => UseArt(art), learned || (unlocked && Save.Doo >= art.LearnCost));
                var variants = Mastery.AvailableVariants(art);
                Button(row, "Variant", .71f, .06f, .97f, .43f, variants.Count == 0 ? "변초 없음" : Mastery.Level(Save, art.Id) < Mastery.VariantLevel ? "변초 · 극성 필요" : "변초 · " + Mastery.VariantHint(Mastery.VariantOf(Save, art.Id)), () => { Mastery.CycleVariant(Save, art); Complete(true, null, "변초를 변경했습니다."); }, learned && Mastery.Level(Save, art.Id) >= Mastery.VariantLevel && variants.Count > 0);
            }
            Button(_page, "ArtsMenu", .25f, .005f, .75f, .09f, "무공 전체 관리 · 숙련 안내", _openArts);
        }

        private void UseArt(ArtData art)
        {
            if (!Save.LearnedArts.Contains(art.Id))
            {
                if (!art.IsUnlocked(Save) || Save.Doo < art.LearnCost) { Complete(false, "습득 조건을 확인해 주세요.", null); return; }
                Save.Doo -= art.LearnCost; Save.LearnedArts.Add(art.Id);
                string replaced; Progression.EquipArt(Save, art, out replaced);
                Complete(true, null, "무공을 습득했습니다.");
            }
            else if (Save.EquippedArts.Contains(art.Id)) { Save.EquippedArts.Remove(art.Id); Complete(true, null, "무공을 해제했습니다."); }
            else
            {
                string replaced; bool success = Progression.EquipArt(Save, art, out replaced);
                Complete(success, "무공 장착 한도입니다. 먼저 다른 무공을 해제해 주세요.", replaced != null ? "필살기를 교체했습니다." : "무공을 장착했습니다.");
            }
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
                Text(row, "Benefits", .04f, .04f, .96f, .72f, Bonds.BonusText(level) + "\n" + next
                    + "\n합공: " + (string.IsNullOrEmpty(companion.JointName) ? "없음" : companion.JointName + (level < 1 ? " · 친분부터 사용 가능" : " · 사용 가능"))
                    + "\n수련 상한 Lv." + CompanionTraining.Cap(Save, companion.Id), 28);
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
