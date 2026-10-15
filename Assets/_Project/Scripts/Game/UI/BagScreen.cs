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
        private const float RowH = 130f, RowGap = 10f;

        private sealed class Row
        {
            public GameObject Band;
            public Text Title, Detail, ButtonLabel;
            public Button Button;
            public Image ButtonImage;
            public string ItemId;
        }

        private readonly List<Row> _rows = new List<Row>();
        private Text _header, _gear, _hint;
        private RectTransform _content;
        private bool _built;

        public void Build(RectTransform root)
        {
            if (_built) return;
            _built = true;

            UiFactory.Panel("HeaderBand", root, new Vector2(0.04f, 0.925f), new Vector2(0.96f, 0.985f), UiFactory.Ink);
            _header = UiFactory.Label("Header", root, new Vector2(0.06f, 0.925f), new Vector2(0.94f, 0.985f), "", 42, UiFactory.Gold);
            _gear = UiFactory.Label("Gear", root, new Vector2(0.05f, 0.875f), new Vector2(0.95f, 0.922f), "", 28, UiFactory.Ink, TextAnchor.MiddleCenter);
            _hint = UiFactory.Label("Hint", root, new Vector2(0.05f, 0.840f), new Vector2(0.95f, 0.875f), "", 24, UiFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);

            // The bag scrolls: it can hold more stacks than fit on one screen.
            var viewport = UiFactory.Rect("Viewport", root, Vector2.zero, new Vector2(1f, 0.835f));
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
                row.Title = UiFactory.Label("Title", rt, new Vector2(0.03f, 0.50f), new Vector2(0.66f, 0.98f), "", 32, UiFactory.PaperText, TextAnchor.MiddleLeft);
                row.Detail = UiFactory.Label("Detail", rt, new Vector2(0.03f, 0.02f), new Vector2(0.66f, 0.50f), "", 20, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                int index = i;
                row.Button = UiFactory.Button("Btn" + i, rt, new Vector2(0.68f, 0.12f), new Vector2(0.97f, 0.88f), "", UiFactory.Red, () => OnPressed(index), 26);
                row.ButtonImage = row.Button.GetComponent<Image>();
                row.ButtonLabel = row.Button.GetComponentInChildren<Text>();
                _rows.Add(row);
            }
            Refresh();
        }

        private void OnEnable() { if (_built) Refresh(); }

        private void OnPressed(int index)
        {
            var row = _rows[index];
            if (string.IsNullOrEmpty(row.ItemId)) return;
            var save = GameManager.Instance.Save;
            var item = EarlyGameContent.FindItem(row.ItemId);
            if (item == null) return;

            if (item.IsGear)
            {
                if (Inventory.IsEquipped(save, item.Id))
                {
                    Inventory.Unequip(save, item.Id);
                    _hint.text = item.Name + " 해제";
                }
                else
                {
                    string error;
                    _hint.text = Inventory.Equip(save, item.Id, out error) ? item.Name + " 장착" : error;
                }
            }
            else if (item.Kind == ItemKind.Material)
            {
                int count = Inventory.Count(save, item.Id);
                long gold = Inventory.Sell(save, item.Id, count);
                _hint.text = item.Name + " " + count + "개를 팔아 " + gold.ToString("N0") + "두를 얻었다.";
            }
            GameManager.Instance.SaveNow();
            Refresh();
        }

        private static string GearLine(SaveData save)
        {
            string weapon = "없음";
            var acc = new List<string>();
            foreach (var id in save.EquippedGear)
            {
                var it = EarlyGameContent.FindItem(id);
                if (it == null) continue;
                if (it.Kind == ItemKind.Weapon) weapon = it.Name; else acc.Add(it.Name);
            }
            return "무기: " + weapon + "  ·  장신구: " + (acc.Count == 0 ? "없음" : string.Join(", ", acc)) + " (" + acc.Count + "/" + Inventory.AccessorySlots + ")";
        }

        public void Refresh()
        {
            if (!_built) return;
            var save = GameManager.Instance.Save;
            _header.text = "행낭 · " + save.Doo.ToString("N0") + "두";
            _gear.text = GearLine(save);
            if (string.IsNullOrEmpty(_hint.text)) _hint.text = "장비는 비류연만 착용한다. 소모품은 전투 중 '아이템'으로 쓴다.";

            var stacks = save.Items
                .Where(s => EarlyGameContent.FindItem(s.Id) != null)
                .OrderBy(s => (int)EarlyGameContent.FindItem(s.Id).Kind)
                .ThenBy(s => EarlyGameContent.FindItem(s.Id).Name)
                .ToList();

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
                row.Title.text = item.Name + (stack.Count > 1 ? " x" + stack.Count : "") + (equipped ? "  [착용]" : "");
                row.Detail.text = item.Description;

                if (item.IsGear)
                {
                    row.ButtonLabel.text = equipped ? "해제" : "장착";
                    row.ButtonImage.color = equipped ? UiFactory.InkSoft : UiFactory.Red;
                    row.Button.interactable = true;
                }
                else if (item.Kind == ItemKind.Material)
                {
                    row.ButtonLabel.text = "판매 +" + (item.SellValue * stack.Count).ToString("N0") + "두";
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
