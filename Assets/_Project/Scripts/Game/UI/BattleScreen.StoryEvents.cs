using System.Collections;
using BiRyeoDo.Core;
using UnityEngine;

namespace BiRyeoDo.Game.UI
{
    public partial class BattleScreen
    {
        private IEnumerator ShowChoiceEvent(StoryChoiceEvent ev)
        {
            ClearCards();
            yield return ShowStory(ev.Title, ev.Opening);
            if (_fled) yield break;

            SetCommandsVisible(false);
            _panel.SetActive(false);
            var root = UiFactory.Rect("StoryChoices", (RectTransform)_fx.parent, Vector2.zero, Vector2.one);
            UiFactory.Panel("Dim", root, Vector2.zero, Vector2.one, new Color(.015f, .025f, .035f, .97f)).raycastTarget = true;
            var card = UiFactory.Panel("Card", root, new Vector2(.08f, .10f), new Vector2(.92f, .90f), UiFactory.Ink);
            UiFactory.Frame(card.transform, UiFactory.GoldDim, 3f);
            UiFactory.Body(UiFactory.Label("Title", card.transform, new Vector2(.06f, .79f), new Vector2(.94f, .95f), ev.Title, 40, UiFactory.Gold));
            UiFactory.ScrollBody(UiFactory.Label("Prompt", card.transform, new Vector2(.06f, .56f), new Vector2(.94f, .78f), ev.Prompt, 32, UiFactory.PaperText), 32);
            int selected = -1;
            float step = .48f / ev.Choices.Length;
            for (int i = 0; i < ev.Choices.Length; i++)
            {
                int index = i;
                float top = .53f - i * step;
                UiFactory.Button("Choice" + i, card.transform, new Vector2(.06f, top - step + .03f), new Vector2(.94f, top),
                    ev.Choices[i].Label, UiFactory.InkSoft, () => { if (selected < 0) selected = index; }, 30);
            }
            while (selected < 0 && !_fled) yield return null;
            root.gameObject.SetActive(false);
            Destroy(root.gameObject);
            if (_fled) yield break;
            yield return GuardTaps();
            yield return ShowStory(ev.Title, StoryChoiceEvents.Outcome(ev, selected));
            if (!_fled) yield return ShowStory(ev.Title, ev.Ending);
        }
    }
}
