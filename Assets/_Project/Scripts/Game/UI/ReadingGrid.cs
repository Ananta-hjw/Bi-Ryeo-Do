using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Groups cards into measured rows so wrapped text grows the entire row without breaking columns.</summary>
    public sealed class ReadingGrid
    {
        private readonly RectTransform _content;
        private readonly int _columns;
        private readonly float _height;
        private RectTransform _row;
        private int _column, _rowIndex;
        private float _offset = Gap;
        private const float Gap = 16f;

        public ReadingGrid(RectTransform content, int columns, float minimumHeight = 280f)
        {
            _content = content;
            _columns = Mathf.Clamp(columns, 2, 3);
            _height = minimumHeight;
        }

        private RectTransform NewRow(float height)
        {
            var row = UiFactory.Rect("Row" + _rowIndex++, _content, new Vector2(.025f, 1f), new Vector2(.975f, 1f));
            row.pivot = new Vector2(.5f, 1f);
            row.sizeDelta = new Vector2(0f, height);
            row.anchoredPosition = new Vector2(0f, -_offset);
            ReadingList.Register(row, height, Gap);
            _offset += height + Gap;
            _content.sizeDelta = new Vector2(0f, _offset);
            return row;
        }

        public void Heading(string text)
        {
            _column = 0;
            var row = NewRow(90f);
            UiFactory.Label("Heading", row, Vector2.zero, Vector2.one, text, 32, UiFactory.Gold, TextAnchor.MiddleLeft);
        }

        public RectTransform Add(string name)
        {
            int column = _column;
            if (column == 0) _row = NewRow(_height);
            var card = UiFactory.Rect(name, _row, new Vector2(column / (float)_columns, 0f), new Vector2((column + 1f) / _columns, 1f));
            card.offsetMin = new Vector2(column == 0 ? 0f : Gap / 2f, 0f);
            card.offsetMax = new Vector2(column == _columns - 1 ? 0f : -Gap / 2f, 0f);
            card.gameObject.AddComponent<Image>().color = UiFactory.Ink;
            UiFactory.Frame(card, UiFactory.GoldDim, 2f);
            _column = (column + 1) % _columns;
            return card;
        }
    }
}
