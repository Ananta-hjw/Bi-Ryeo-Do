using System;
using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>
    /// 상태창 overlay: the protagonist's stats (gear included), equipment, equipped arts and the fighting party.
    /// Laid out in a scrolling list with fixed pixel row heights and fixed font sizes, so every row lines up and stays readable.
    /// </summary>
    public sealed class StatusPanel : OverlayPanel
    {
        private static readonly Color Green = new Color(0.45f, 0.85f, 0.5f, 1f);
        private const float Gap = 20f, SectionH = 88f;

        private RectTransform _content;
        private float _y;
        private RectTransform _picker;
        private string _equipmentMessage;
        private static readonly ItemKind[] SlotKinds = { ItemKind.Weapon, ItemKind.Armor, ItemKind.Pants, ItemKind.Boots, ItemKind.Accessory, ItemKind.Accessory };
        private static readonly string[] SlotNames = { "무기", "상의", "하의", "신발", "장신구 1", "장신구 2" };

        public StatusPanel(RectTransform root, Action onChange) : base(root, onChange) { }

        public void Open()
        {
            var save = GameManager.Instance.Save;
            Begin();
            UiFactory.Label("Title", Root, new Vector2(0.05f, 0.935f), new Vector2(0.95f, 0.995f), "상태창", 44, UiFactory.Gold);
            _content = CodexPanel.Scroller(Root, 0.075f, 0.93f);
            _y = Gap;

            // Header: portrait + name, level, rank, exp
            var head = Block(230f);
            var pic = UiFactory.Panel("Pic", head, new Vector2(0.02f, 0.06f), new Vector2(0.20f, 0.94f), ArtLibrary.TintFor("비류연"));
            var sprite = ArtLibrary.Player();
            if (sprite != null) { pic.sprite = sprite; pic.color = Color.white; pic.preserveAspect = true; }
            else UiFactory.Body(UiFactory.Label("Glyph", pic.transform, Vector2.zero, Vector2.one, "비", 56, UiFactory.PaperText));
            long need = Progression.ExpToNext(save.Level);
            Text(head, 0.23f, 0.97f, 0.50f, 0.96f, "비류연  Lv." + save.Level + "  ·  " + Promotion.RankName(Promotion.EffectiveRank(save)), 38, UiFactory.Gold);
            Text(head, 0.23f, 0.97f, 0.06f, 0.50f, "경험 " + save.Exp.ToString("N0") + " / " + need.ToString("N0") + "\n두 " + save.Doo.ToString("N0"), 28, UiFactory.PaperText, TextAnchor.UpperLeft);

            // Gear
            Section(GearTitle(save));
            GearCard("무기", StatusSheet.Weapon(save), 0);
            GearCard("상의", StatusSheet.Armor(save), 1);
            GearCard("하의", StatusSheet.Pants(save), 2);
            GearCard("신발", StatusSheet.Boots(save), 3);

            for (int i = 0; i < Inventory.AccessorySlots; i++) GearCard("장신구 " + (i + 1), Gear.ItemInSlot(save, Inventory.Hero, 4 + i), 4 + i);

            // 세트 효과: every set with a worn piece, what it gives now and what the next piece adds
            Section("장비 세트 효과");
            var setStatus = Gear.SetStatus(save.EquippedGear);
            if (setStatus.Count == 0)
            {
                var none = Block(72f);
                Text(none, 0.04f, 0.97f, 0f, 1f, "착용한 세트 장비가 없습니다. 같은 세트를 2개 이상 맞추면 효과가 붙습니다.", 24, UiFactory.Dim, TextAnchor.MiddleLeft);
            }
            foreach (var entry in setStatus)
            {
                int activeLines = entry[1].Split('\n').Length;
                float h = 64f + activeLines * 34f + (entry[2].Length > 0 ? 38f : 0f);
                var block = Block(h);
                Text(block, 0.04f, 0.97f, 1f - 60f / h, 1f, entry[0], 30, UiFactory.Gold, TextAnchor.MiddleLeft);
                float yTop = 1f - 62f / h;
                float bodyBottom = (entry[2].Length > 0 ? 38f : 6f) / h;
                Text(block, 0.04f, 0.97f, bodyBottom, yTop, entry[1], 24, UiFactory.PaperText, TextAnchor.UpperLeft);
                if (entry[2].Length > 0) Text(block, 0.04f, 0.97f, 0.02f, bodyBottom, entry[2], 22, UiFactory.Dim, TextAnchor.MiddleLeft);
            }

            // 무공 경지: composite stat power placed on the realm ladder (체력·공격·방어·속도, gear included)
            Section("무공 경지");
            double power = Realm.Power(InnerArts.HeroStatsBeforeRealm(save));
            int realmStep = Realm.StepOf(power);
            var realmCard = Block(280f);
            Text(realmCard, 0.04f, 0.96f, 0.66f, 0.98f, Realm.Label(realmStep), 40, UiFactory.Gold, TextAnchor.MiddleLeft);
            Text(realmCard, 0.04f, 0.96f, 0.50f, 0.66f, "종합 수치 " + Math.Round(power).ToString("N0"), 28, UiFactory.PaperText, TextAnchor.MiddleLeft);
            string remaining = Realm.RemainingText(power);
            var nextBar = UiFactory.MakeBar("NextBar", realmCard.transform, new Vector2(0.04f, 0.36f), new Vector2(0.96f, 0.46f), new Color(0f, 0f, 0f, 0.7f), UiFactory.Gold);
            nextBar.SetPct((float)Realm.ProgressToNext(power));
            Text(realmCard, 0.04f, 0.96f, 0.02f, 0.34f, remaining.Length > 0 ? remaining : "더 오를 경지가 없습니다", 24, remaining.Length > 0 ? UiFactory.PaperText : UiFactory.Gold, TextAnchor.MiddleLeft);
            var realmNote = Block(72f);
            Text(realmNote, 0.04f, 0.97f, 0f, 1f, "체력·공격력·방어력·속도를 합친 수치(장비·심법 반영)로 정한 경지입니다. 경지가 한 칸 오를 때마다 모든 능력치가 " + Mathf.RoundToInt(Realm.BonusPerStep * 100f) + "%씩 오릅니다(지금 +" + Mathf.RoundToInt(Realm.Bonus(realmStep) * 100f) + "%).", 22, UiFactory.Dim, TextAnchor.MiddleLeft);

            // Stats: two columns of equal rows
            Section("능력치");
            var stats = StatusSheet.Hero(save);
            for (int i = 0; i < stats.Count; i += 2)
            {
                var row = Place(136f, false);
                StatCell(row, stats[i], 0f, 0.495f);
                if (i + 1 < stats.Count) StatCell(row, stats[i + 1], 0.505f, 1f);
                _y += 136f + Gap;
            }

            // Arts
            Section("장착 무공");
            var artLines = StatusSheet.ArtLines(save);
            if (StatusSheet.InnerLine(save).Length > 0) artLines.Add(StatusSheet.InnerLine(save));
            if (artLines.Count == 0) artLines.Add("장착한 무공이 없습니다");
            foreach (var line in artLines)
            {
                var block = Block(72f);
                Text(block, 0.04f, 0.97f, 0f, 1f, line, 28, UiFactory.PaperText, TextAnchor.MiddleLeft);
            }

            // Party
            var party = EarlyGameContent.Companions.Where(c => c.JoinStage <= save.HighestUnlockedStage).ToList();
            if (party.Count > 0)
            {
                Section("동료 상태 · 장비");
                foreach (var c in party)
                {
                    var l = StatusSheet.Ally(save, c);
                    var block = Block(400f);
                    Text(block, 0.04f, 0.97f, 0.76f, 0.98f, c.Name + "  Lv." + CompanionGrowth.LevelOf(save, c.Id) + "   " + StatusSheet.AllyRealm(save, c), 32, UiFactory.Gold, TextAnchor.MiddleLeft);
                    Text(block, 0.04f, 0.97f, 0.48f, 0.76f, "체력 " + l[0].Value + "    내공 " + l[1].Value + "\n공격 " + l[2].Value + "    방어 " + l[3].Value, 28, UiFactory.PaperText, TextAnchor.MiddleLeft);
                    Text(block, 0.04f, 0.97f, 0.22f, 0.48f, StatusSheet.GearNames(save, c.Id), 22, UiFactory.Dim, TextAnchor.MiddleLeft);
                    string owner = c.Id;
                    UiFactory.Button("ManageGear", block, new Vector2(.04f, .02f), new Vector2(.96f, .20f), "장비 장착 · 해제", UiFactory.InkSoft, () => OpenEquipmentPicker(owner, 0), 32);
                }
            }

            _content.sizeDelta = new Vector2(0f, _y + Gap);
            UiFactory.Button("Close", Root, new Vector2(0.25f, 0.005f), new Vector2(0.75f, 0.075f), "닫기", UiFactory.InkSoft, () => { Root.gameObject.SetActive(false); OnChange(); }, 34);
        }

        // ------------------------------------------------------------------ layout helpers (pixel rows inside the scroll content)

        /// <summary>A framed row of the given height at the current offset; advances the offset.</summary>
        private RectTransform Block(float h)
        {
            var rt = Place(h, true);
            _y += h + Gap;
            return rt;
        }

        private RectTransform Place(float h, bool framed)
        {
            var rt = UiFactory.Rect("Row", _content, new Vector2(0.04f, 1f), new Vector2(0.96f, 1f));
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -_y);
            rt.sizeDelta = new Vector2(0f, h);
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = framed ? UiFactory.Ink : new Color(0f, 0f, 0f, 0f);
            bg.raycastTarget = true;   // lets a drag that starts on a row still scroll
            if (framed) UiFactory.Frame(rt, UiFactory.GoldDim, 2f);
            ReadingList.Register(rt, h, Gap);
            return rt;
        }

        private void Section(string title)
        {
            var rt = Place(SectionH, false);
            Text(rt, 0.02f, 1f, 0f, 1f, title, 34, UiFactory.Gold, TextAnchor.MiddleLeft);
            _y += SectionH + Gap * 0.5f;
        }

        /// <summary>A fixed-size, wrapping text (no shrink-to-fit) inside a row.</summary>
        private static Text Text(RectTransform parent, float x0, float x1, float y0, float y1, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            return UiFactory.Body(UiFactory.Label("Text", parent, new Vector2(x0, y0), new Vector2(x1, y1), text, Mathf.Max(32, size), color, anchor, FontStyle.Normal), 1.2f);
        }

        private static void StatCell(RectTransform row, StatLine line, float x0, float x1)
        {
            var cell = UiFactory.Panel("Stat", row, new Vector2(x0, 0f), new Vector2(x1, 1f), UiFactory.Ink);
            UiFactory.Frame(cell.transform, UiFactory.GoldDim, 2f);
            Text(cell.rectTransform, 0.05f, 0.40f, 0f, 1f, line.Label, 28, UiFactory.Dim);
            Text(cell.rectTransform, 0.40f, 0.95f, 0.45f, 1f, line.Value, 34, UiFactory.PaperText, TextAnchor.MiddleRight);
            if (!string.IsNullOrEmpty(line.Bonus))
                Text(cell.rectTransform, 0.40f, 0.95f, 0f, 0.45f, "장비 " + line.Bonus, 22, Green, TextAnchor.MiddleRight);
        }

        private void GearCard(string slot, ItemData item, int slotIndex)
        {
            var save = GameManager.Instance.Save;
            bool has = item != null;
            var card = Block(has ? 220f : 120f);
            var tap = card.gameObject.AddComponent<Button>();
            tap.targetGraphic = card.GetComponent<Image>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() => OpenEquipmentPicker(Inventory.Hero, slotIndex));
            UiFactory.Frame(card, has ? UiFactory.GoldDim : new Color(0.25f, 0.25f, 0.28f, 1f), 2f);
            string name = has ? UiFactory.GradedName(item) + " · " + Gear.StarsText(save, item.Id) + (Gear.LevelFor(save, Inventory.Hero, item.Id) > 0 ? " +" + Gear.LevelFor(save, Inventory.Hero, item.Id) : "") : "비어 있음 · 눌러서 장착";
            Text(card, 0.04f, 0.26f, has ? 0.58f : 0f, 1f, slot, 26, UiFactory.Dim);
            Text(card, 0.27f, has ? 0.83f : 0.97f, has ? 0.58f : 0f, 1f, name, 32, has ? UiFactory.PaperText : UiFactory.InkSoft);
            if (has)
            {
                Text(card, 0.04f, 0.83f, 0.04f, 0.58f, StatusSheet.EffectText(item, Gear.LevelFor(save, Inventory.Hero, item.Id), Gear.Stars(save, item.Id)), 24, UiFactory.PaperText, TextAnchor.UpperLeft);
                var pic = UiFactory.Panel("Picture", card, new Vector2(0.85f, 0.08f), new Vector2(0.98f, 0.92f), Color.white);
                var glyph = UiFactory.Label("Glyph", pic.transform, Vector2.zero, Vector2.one, "", 40, UiFactory.PaperText);
                ItemArt.Apply(pic, glyph, item);
            }
        }

        private void OpenEquipmentPicker(string owner, int slotIndex)
        {
            if (_picker != null) { _picker.gameObject.SetActive(false); UnityEngine.Object.Destroy(_picker.gameObject); }
            _picker = UiFactory.Rect("EquipmentPicker", Root, Vector2.zero, Vector2.one);
            UiFactory.Panel("Background", _picker, Vector2.zero, Vector2.one, UiFactory.Parchment).raycastTarget = true;
            var save = GameManager.Instance.Save;
            var kind = SlotKinds[slotIndex];
            int slot = slotIndex == 5 ? 1 : 0;
            var current = Inventory.InSlot(save, owner, kind, slot);
            var owners = new List<string> { Inventory.Hero };
            owners.AddRange(EarlyGameContent.Companions.Where(c => c.JoinStage <= save.HighestUnlockedStage).Select(c => c.Id));
            for (int i = 0; i < owners.Count; i++)
            {
                string id = owners[i];
                string name = id == Inventory.Hero ? "비류연" : EarlyGameContent.FindCompanion(id).Name;
                float left = .04f + (i % 3) * .31f;
                float top = .99f - (i / 3) * .05f;
                UiFactory.Button("Owner" + i, _picker, new Vector2(left, top - .045f), new Vector2(left + .30f, top), name,
                    id == owner ? UiFactory.Red : UiFactory.InkSoft, () => { _equipmentMessage = null; OpenEquipmentPicker(id, slotIndex); }, 30);
            }
            for (int i = 0; i < SlotNames.Length; i++)
            {
                int selected = i;
                float left = .04f + (i % 3) * .31f;
                float top = .88f - (i / 3) * .055f;
                UiFactory.Button("Slot" + i, _picker, new Vector2(left, top - .05f), new Vector2(left + .30f, top), SlotNames[i],
                    i == slotIndex ? UiFactory.Red : UiFactory.InkSoft, () => { _equipmentMessage = null; OpenEquipmentPicker(owner, selected); }, 30);
            }
            string currentName = current != null ? UiFactory.GradedName(current) + " · " + Gear.StarsText(save, current.Id) : "비어 있음";
            UiFactory.ScrollBody(UiFactory.Label("Current", _picker, new Vector2(.05f, .70f), new Vector2(.72f, .77f), "현재 " + SlotNames[slotIndex] + "\n" + currentName, 32, UiFactory.PaperText, TextAnchor.MiddleLeft), 32);
            var remove = UiFactory.Button("Unequip", _picker, new Vector2(.74f, .705f), new Vector2(.95f, .765f), "해제", UiFactory.InkSoft, () =>
            {
                if (current == null) return;
                Inventory.UnequipFor(save, owner, current.Id);
                GameManager.Instance.SaveNow();
                _equipmentMessage = current.Name + "을(를) 해제했습니다.";
                OpenEquipmentPicker(owner, slotIndex);
            }, 32);
            remove.interactable = current != null;
            string summary = current != null ? StatusSheet.EffectText(current, Gear.LevelFor(save, owner, current.Id), Gear.Stars(save, current.Id)) : "아래에서 이 부위에 장착할 장비를 선택합니다.";
            UiFactory.ScrollBody(UiFactory.Label("CurrentEffect", _picker, new Vector2(.05f, .64f), new Vector2(.95f, .70f), summary, 30, UiFactory.Dim), 30);
            UiFactory.ScrollBody(UiFactory.Label("Message", _picker, new Vector2(.05f, .50f), new Vector2(.95f, .55f), _equipmentMessage ?? "착용 중인 장비는 강조 표시됩니다. 다른 인물의 장비는 먼저 해제합니다.", 30, UiFactory.Gold), 30);
            var activeSets = Gear.SetLines(Inventory.GearOf(save, owner));
            UiFactory.ScrollBody(UiFactory.Label("CurrentSets", _picker, new Vector2(.05f, .55f), new Vector2(.95f, .64f),
                "현재 세트 효과\n" + (activeSets.Count > 0 ? string.Join("\n", activeSets) : "적용 중인 세트 효과가 없습니다."), 28, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal), 28);
            int stored = Gear.SlotLevel(save, owner, slotIndex);
            int cap = current != null ? Gear.Cap(save, current.Id) : 10;
            int next = stored + 1;
            bool unlocked = current != null && Gear.IsGradeUnlocked(save, owner, slotIndex, current.Grade);
            long unlockCost = current != null ? Gear.GradeUnlockCost(save, owner, slotIndex, current.Grade) : 0;
            var material = Gear.MaterialForLevel(next);
            string upgradeInfo = SlotNames[slotIndex] + " 강화 +" + stored + " · 적용 +" + Gear.AppliedSlotLevel(save, owner, slotIndex, current)
                + " / 상한 +" + cap + "\n" + material.Name + " " + Inventory.Count(save, material.Id) + "개 · " + save.Doo.ToString("N0") + "두"
                + "\n" + Grades.NameOf(Gear.UnlockedGrade(save, owner, slotIndex)) + "까지 개방 · 교체해도 강화 유지"
                + (current != null && !unlocked ? "\n미개방: 기본·돌파 효과만 적용됩니다." : "");
            UiFactory.ScrollBody(UiFactory.Label("UpgradeInfo", _picker, new Vector2(.05f, .10f), new Vector2(.54f, .25f), upgradeInfo, 30, UiFactory.PaperText), 30);
            string upgradeLabel = current == null ? "장비를 선택해 주세요." : !unlocked ? Grades.NameOf(current.Grade) + " 등급 개방\n" + unlockCost.ToString("N0") + "두\n하위 등급도 함께 개방"
                : stored >= Gear.MaxLevel ? "최대 +100" : stored >= cap ? "한계돌파 필요"
                : "부위 강화 +" + next + "\n" + material.Name + " " + Gear.StoneCost(next) + "개\n" + Gear.DooCost(next).ToString("N0") + "두 · 확정 성공";
            var upgrade = UiFactory.Button("EnhanceSlot", _picker, new Vector2(.56f, .11f), new Vector2(.95f, .24f), upgradeLabel, UiFactory.Red, () =>
            {
                string error;
                bool success = !unlocked ? Gear.UnlockSlotGrade(save, owner, slotIndex, out error) : Gear.EnhanceSlot(save, owner, slotIndex, out error);
                _equipmentMessage = success ? (!unlocked ? Grades.NameOf(current.Grade) + " 등급을 개방했습니다." : SlotNames[slotIndex] + " 강화 +" + Gear.SlotLevel(save, owner, slotIndex) + " 성공!") : error;
                if (success) { SoundManager.Sfx("levelup"); GameManager.Instance.SaveNow(); }
                OpenEquipmentPicker(owner, slotIndex);
            }, 30);
            upgrade.interactable = current != null && (!unlocked ? save.Doo >= unlockCost : Gear.CanEnhanceSlot(save, owner, slotIndex));
            var content = CodexPanel.Scroller(_picker, .265f, .495f);
            var candidates = save.Items.Select(stack => EarlyGameContent.FindItem(stack.Id))
                .Where(item => item != null && item.IsGear && item.Kind == kind && Inventory.Count(save, item.Id) > 0
                    && (owner == Inventory.Hero || !Gear.IsHeroOnly(item.Id)))
                .OrderByDescending(item => current != null && item.Id == current.Id).ThenByDescending(item => item.Grade).ThenBy(item => item.Name).ToList();
            for (int i = 0; i < candidates.Count; i++)
            {
                var item = candidates[i];
                bool worn = current != null && current.Id == item.Id;
                bool otherSlot = !worn && Inventory.GearOf(save, owner).Contains(item.Id);
                bool available = !worn && !otherSlot && Inventory.Free(save, item.Id) > 0;
                var row = CodexPanel.Band(content, i);
                if (worn) row.GetComponent<Image>().color = new Color(.15f, .28f, .25f, 1f);
                string state = worn ? "착용 중" : otherSlot ? "다른 칸 착용 중" : !available ? "다른 인물 착용 중" : "보유 " + Inventory.Count(save, item.Id) + "개";
                var set = Gear.SetOf(item.Id);
                var previewGear = new List<string>(Inventory.GearOf(save, owner));
                if (current != null) previewGear.Remove(current.Id);
                if (!previewGear.Contains(item.Id)) previewGear.Add(item.Id);
                var afterSets = Gear.SetLines(previewGear);
                string setPreview = "장착 후 세트 효과\n" + (afterSets.Count > 0 ? string.Join("\n", afterSets) : "세트 효과 없음");
                if (set != null)
                {
                    int pieces = Gear.WornPieces(set, previewGear);
                    var nextTier = set.Tiers.FirstOrDefault(t => t.Pieces > pieces);
                    if (nextTier != null) setPreview += "\n다음 " + nextTier.Pieces + "개: " + Gear.TierText(nextTier);
                }
                UiFactory.Body(UiFactory.Label("Name", row, new Vector2(.04f, .73f), new Vector2(.72f, .98f), UiFactory.GradedName(item) + " · " + Gear.StarsText(save, item.Id) + " · " + state, 32, worn ? UiFactory.Gold : UiFactory.PaperText, TextAnchor.MiddleLeft));
                UiFactory.Body(UiFactory.Label("Effect", row, new Vector2(.04f, .44f), new Vector2(.72f, .73f), "장착 시: " + StatusSheet.EffectText(item, Gear.AppliedSlotLevel(save, owner, slotIndex, item), Gear.Stars(save, item.Id))
                    + (!Gear.IsGradeUnlocked(save, owner, slotIndex, item.Grade) ? "\n개방 " + Gear.GradeUnlockCost(save, owner, slotIndex, item.Grade).ToString("N0") + "두" : ""), 28, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal));
                UiFactory.Body(UiFactory.Label("SetPreview", row, new Vector2(.04f, .03f), new Vector2(.96f, .44f), setPreview, 27, UiFactory.Gold, TextAnchor.UpperLeft, FontStyle.Normal));
                var equip = UiFactory.Button("Equip", row, new Vector2(.75f, .49f), new Vector2(.96f, .96f), worn ? "착용 중" : "장착", available ? UiFactory.Red : UiFactory.InkSoft, () =>
                {
                    string error;
                    bool success = Inventory.EquipInSlot(save, owner, kind, slot, item.Id, out error);
                    _equipmentMessage = success ? item.Name + "을(를) 장착했습니다." : error;
                    if (success) GameManager.Instance.SaveNow();
                    OpenEquipmentPicker(owner, slotIndex);
                }, 30);
                equip.interactable = available;
            }
            if (candidates.Count == 0) UiFactory.Label("Empty", content.parent, new Vector2(.05f, .35f), new Vector2(.95f, .65f), "이 부위에 장착할 수 있는 장비가 없습니다.", 32, UiFactory.Dim);
            UiFactory.Button("Back", _picker, new Vector2(.25f, .01f), new Vector2(.75f, .085f), "상태창으로", UiFactory.InkSoft, () => { _equipmentMessage = null; Open(); OnChange(); }, 34);
        }

        private static string GearTitle(SaveData save) { return "착용 장비 · 부위를 눌러 변경"; }
    }
}
