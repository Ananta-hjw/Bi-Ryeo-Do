using System;
using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Shared joined-character chooser; selection only changes the viewing context.</summary>
    internal static class CharacterPicker
    {
        public static void Open(RectTransform root, SaveData save, string selected, Action<string> choose)
        {
            LandscapeMenu.Clear(root);
            root.gameObject.SetActive(true);
            root.SetAsLastSibling();
            var card = UiFactory.Panel("Card", root, new Vector2(.12f, .06f), new Vector2(.88f, .94f), UiFactory.Ink).rectTransform;
            UiFactory.Frame(card, UiFactory.GoldDim, 2f);
            string selectedName = selected == Inventory.Hero ? "비류연" : EarlyGameContent.FindCompanion(selected).Name;
            UiFactory.Body(UiFactory.Label("Title", card, new Vector2(.04f, .86f), new Vector2(.96f, .98f), "인물 변경 · 현재 " + selectedName, 34, UiFactory.Gold, TextAnchor.MiddleLeft));
            var content = CodexPanel.Scroller(card, .16f, .84f);
            var owners = new List<string> { Inventory.Hero };
            owners.AddRange(EarlyGameContent.Companions.Where(c => c.JoinStage <= save.HighestUnlockedStage).Select(c => c.Id));
            RectTransform row = null;
            for (int i = 0; i < owners.Count; i++)
            {
                if (i % 3 == 0)
                {
                    row = UiFactory.Rect("Row" + i / 3, content, new Vector2(.025f, 1f), new Vector2(.975f, 1f));
                    row.pivot = new Vector2(.5f, 1f); row.sizeDelta = new Vector2(0f, 160f);
                    ReadingList.Register(row, 160f, 16f);
                }
                string owner = owners[i];
                string name = owner == Inventory.Hero ? "비류연" : EarlyGameContent.FindCompanion(owner).Name;
                float x = i % 3 * .338f;
                var button = UiFactory.Button("Person_" + owner, row, new Vector2(x, 0f), new Vector2(x + .324f, 1f), "",
                    owner == selected ? UiFactory.Jade : UiFactory.InkSoft, () =>
                    {
                        root.gameObject.SetActive(false);
                        choose(owner);
                    }, 28);
                UiFactory.Frame(button.transform, owner == selected ? UiFactory.Gold : UiFactory.GoldDim, 2f);
                var portrait = UiFactory.Panel("Portrait", button.transform, new Vector2(.03f, .08f), new Vector2(.36f, .92f), Color.white);
                portrait.sprite = ArtLibrary.Portrait(name); portrait.preserveAspect = true;
                if (portrait.sprite == null)
                {
                    portrait.color = Color.clear;
                    UiFactory.Label("Glyph", portrait.transform, Vector2.zero, Vector2.one, name.Substring(0, 1), 42, UiFactory.Gold);
                }
                UiFactory.Body(UiFactory.Label("Name", button.transform, new Vector2(.40f, .36f), new Vector2(.97f, .91f), name, 28,
                    owner == selected ? UiFactory.Gold : UiFactory.PaperText, TextAnchor.MiddleLeft));
                UiFactory.Body(UiFactory.Label("Selected", button.transform, new Vector2(.40f, .06f), new Vector2(.97f, .33f), owner == selected ? "현재 선택" : "선택하기", 22, UiFactory.Dim, TextAnchor.MiddleLeft));
            }
            UiFactory.Button("Close", card, new Vector2(.30f, .015f), new Vector2(.70f, .14f), "닫기", UiFactory.InkSoft, () => root.gameObject.SetActive(false), 28);
            Canvas.ForceUpdateCanvases();
            content.GetComponent<ReadingList>()?.Reflow();
        }
    }
}
