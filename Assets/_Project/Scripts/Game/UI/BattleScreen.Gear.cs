using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>The worn-gear strip of the ally/enemy info window (six square slots: icon, grade frame, enhancement) and the item detail it opens.</summary>
    public partial class BattleScreen
    {
        private const float GearSlotSize = 96f, GearSlotGap = 12f;
        private RectTransform _tipGear;
        private GameObject _gearDetailRoot;
        private Image _gearDetailIcon, _gearDetailFrame;
        private Text _gearDetailGlyph, _gearDetailName, _gearDetailInfo, _gearDetailBody;

        /// <summary>Six slots in slot order (weapon, 상의, 하의, 신발, two accessories); a null entry is an empty slot.</summary>
        private sealed class WornPiece
        {
            public ItemData Item;
            public int Level, Stars;
        }

        private void BuildTipGear(Transform card)
        {
            _tipGear = UiFactory.Rect("Gear", card, new Vector2(0.05f, 0.85f), new Vector2(0.05f, 0.85f));
            _tipGear.pivot = new Vector2(0f, 1f);
            _tipGear.sizeDelta = new Vector2(6 * GearSlotSize + 5 * GearSlotGap, GearSlotSize);
            _tipGear.gameObject.SetActive(false);
            BuildGearDetail((RectTransform)_tipRoot.transform.parent);
        }

        /// <summary>What a party member wears now (the same snapshot the battle uses), in slot order.</summary>
        private static List<WornPiece> AllyGear(Combatant unit)
        {
            var save = GameManager.Instance.Save;
            string owner = unit.Id == Formation.HeroId ? Inventory.Hero : unit.Id;
            var levels = Gear.LevelsFor(save, owner);
            var list = new List<WornPiece>();
            for (int i = 0; i < 6; i++)
            {
                var item = Gear.ItemInSlot(save, owner, i);
                if (item == null || !Inventory.GearOf(save, owner).Contains(item.Id)) { list.Add(null); continue; }
                var entry = levels.Find(x => x.Id == item.Id);
                list.Add(new WornPiece { Item = item, Level = entry != null ? entry.Level : 0, Stars = entry != null ? entry.Stars : Gear.Stars(save, item.Id) });
            }
            return list;
        }

        /// <summary>What an enemy wears (story stages); null when it wears nothing.</summary>
        private static List<WornPiece> EnemyGear(Combatant unit)
        {
            if (unit.Gear == null || unit.Gear.Count == 0) return null;
            var list = new List<WornPiece>();
            foreach (var g in unit.Gear)
            {
                var item = EarlyGameContent.FindItem(g.Id);
                list.Add(item == null ? null : new WornPiece { Item = item, Level = g.Level, Stars = g.Stars });
            }
            while (list.Count < 6) list.Add(null);
            return list;
        }

        /// <summary>Fills the strip (or hides it) and moves the body below it.</summary>
        private void SetTipGear(List<WornPiece> gear)
        {
            if (_tipGear == null) return;
            LandscapeMenu.Clear(_tipGear);
            bool show = gear != null;
            _tipGear.gameObject.SetActive(show);
            var viewport = UiFactory.ReadingViewport(_tipBody);
            viewport.offsetMax = new Vector2(viewport.offsetMax.x, show ? -(GearSlotSize + 18f) : 0f);
            if (!show) return;
            for (int i = 0; i < gear.Count && i < 6; i++)
            {
                var piece = gear[i];
                var slot = UiFactory.Rect("Slot" + i, _tipGear, new Vector2(0f, 1f), new Vector2(0f, 1f));
                slot.pivot = new Vector2(0f, 1f);
                slot.sizeDelta = new Vector2(GearSlotSize, GearSlotSize);
                slot.anchoredPosition = new Vector2(i * (GearSlotSize + GearSlotGap), 0f);
                var bg = slot.gameObject.AddComponent<Image>();
                bg.color = piece == null ? new Color(.06f, .07f, .08f, .9f) : new Color(.10f, .11f, .13f, .96f);
                bg.raycastTarget = piece != null;
                UiFactory.Frame(slot, piece == null ? new Color(.30f, .31f, .33f, 1f) : UiFactory.GradeColor(piece.Item.Grade), piece == null ? 2f : 4f);
                if (piece == null) continue;
                var icon = UiFactory.Panel("Icon", slot, new Vector2(.08f, .08f), new Vector2(.92f, .92f), Color.white);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                ItemArt.Apply(icon, UiFactory.Label("Glyph", icon.transform, Vector2.zero, Vector2.one, "", 30, UiFactory.PaperText), piece.Item);
                if (piece.Level > 0)
                {
                    var lv = UiFactory.Label("Level", slot, new Vector2(.30f, .02f), new Vector2(.97f, .34f), "+" + piece.Level, 22, UiFactory.Gold, TextAnchor.LowerRight);
                    lv.raycastTarget = false;
                    var shadow = lv.gameObject.AddComponent<Outline>();
                    shadow.effectColor = new Color(0f, 0f, 0f, .9f);
                }
                if (piece.Stars > 0)
                {
                    var st = UiFactory.Label("Stars", slot, new Vector2(.04f, .66f), new Vector2(.80f, .98f), "★" + piece.Stars, 18, new Color(1f, .86f, .35f, 1f), TextAnchor.UpperLeft);
                    st.raycastTarget = false;
                }
                var button = slot.gameObject.AddComponent<Button>();
                button.targetGraphic = bg;
                var captured = piece;
                button.onClick.AddListener(() => ShowGearDetail(captured));
            }
        }

        private void BuildGearDetail(RectTransform root)
        {
            var rt = UiFactory.Rect("GearDetail", root, Vector2.zero, Vector2.one);
            _gearDetailRoot = rt.gameObject;
            var dim = rt.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.55f);
            var close = rt.gameObject.AddComponent<Button>();
            close.targetGraphic = dim;
            close.transition = Selectable.Transition.None;
            close.onClick.AddListener(CloseGearDetail);
            var card = UiFactory.Panel("Card", rt, new Vector2(0.20f, 0.14f), new Vector2(0.80f, 0.86f), UiFactory.ModalInk);
            card.raycastTarget = true;
            var cardTap = card.gameObject.AddComponent<Button>();
            cardTap.targetGraphic = card;
            cardTap.transition = Selectable.Transition.None;
            UiFactory.Frame(card.transform, UiFactory.Gold, 3f);
            var iconBox = UiFactory.Panel("IconBox", card.transform, new Vector2(0.05f, 0.70f), new Vector2(0.21f, 0.95f), new Color(.10f, .11f, .13f, .96f));
            _gearDetailFrame = iconBox;
            _gearDetailIcon = UiFactory.Panel("Icon", iconBox.transform, new Vector2(.08f, .08f), new Vector2(.92f, .92f), Color.white);
            _gearDetailIcon.preserveAspect = true;
            _gearDetailGlyph = UiFactory.Label("Glyph", _gearDetailIcon.transform, Vector2.zero, Vector2.one, "", 40, UiFactory.PaperText);
            _gearDetailName = UiFactory.Label("Name", card.transform, new Vector2(0.25f, 0.84f), new Vector2(0.95f, 0.95f), "", 34, UiFactory.PaperText, TextAnchor.MiddleLeft);
            _gearDetailInfo = UiFactory.Label("Info", card.transform, new Vector2(0.25f, 0.71f), new Vector2(0.95f, 0.83f), "", 24, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
            _gearDetailBody = UiFactory.ScrollBody(UiFactory.Label("Body", card.transform, new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.67f), "", 26, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal), 30);
            _gearDetailBody.lineSpacing = 1.2f;
            UiFactory.BackButton(card.transform, new Vector2(.30f, .02f), new Vector2(.70f, .15f), CloseGearDetail);
            _gearDetailRoot.SetActive(false);
        }

        private void CloseGearDetail()
        {
            if (_gearDetailRoot != null) _gearDetailRoot.SetActive(false);
            StartCoroutine(GuardTaps(.3f));
        }

        /// <summary>Tap on a worn piece: its grade, slot, enhancement and stars, the effects at that level, breakthrough options, set and description.</summary>
        private void ShowGearDetail(WornPiece piece)
        {
            if (_gearDetailRoot == null || piece == null) return;
            var item = piece.Item;
            foreach (Transform child in _gearDetailFrame.transform) if (child.name == "Frame") Destroy(child.gameObject);
            UiFactory.Frame(_gearDetailFrame.transform, UiFactory.GradeColor(item.Grade), 4f);
            ItemArt.Apply(_gearDetailIcon, _gearDetailGlyph, item);
            _gearDetailName.text = UiFactory.GradedName(item);
            _gearDetailInfo.text = Grades.NameOf(item.Grade) + " · " + Salvage.SlotLabel(item.Kind) + "  ·  강화 +" + piece.Level + "  ·  " + (piece.Stars > 0 ? piece.Stars + "성" : "미돌파");
            var lines = new List<string>();
            lines.Add(UiFactory.GearEffectText(item, piece.Level, piece.Stars));
            string traits = UiFactory.GearTraitsText(item, piece.Stars);
            if (!string.IsNullOrEmpty(traits)) lines.Add(traits);
            var set = Gear.SetOf(item.Id);
            if (set != null) lines.Add("세트: " + set.Name);
            if (!string.IsNullOrEmpty(item.Description)) lines.Add(item.Description);
            _gearDetailBody.text = string.Join("\n\n", lines);
            _gearDetailRoot.transform.SetAsLastSibling();
            _gearDetailRoot.SetActive(true);
            SoundManager.Sfx("ui_click");
        }
    }
}
