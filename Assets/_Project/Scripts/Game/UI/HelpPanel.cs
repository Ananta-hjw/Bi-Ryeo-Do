using System;
using System.Collections;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>
    /// Modal help card shown above every screen: a few pages of a <see cref="TutorialTopic"/>, advanced by tapping the button.
    /// Used for first-time hints (marked done when finished) and for the "도움말" list in the settings.
    /// </summary>
    public class HelpPanel : MonoBehaviour
    {
        public static HelpPanel Instance { get; private set; }

        private GameObject _root;
        private Text _title, _body, _pageLabel, _buttonLabel;
        private bool _tapped;

        public void Build(RectTransform parent)
        {
            Instance = this;
            var rect = UiFactory.Rect("Help", parent, Vector2.zero, Vector2.one);
            _root = rect.gameObject;
            var dim = rect.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);   // blocks taps on whatever is underneath

            var card = UiFactory.Panel("Card", rect, new Vector2(.12f, .10f), new Vector2(.88f, .90f), UiFactory.ModalInk);
            UiFactory.Frame(card.transform, UiFactory.Gold, 3f);
            _title = UiFactory.ScrollBody(UiFactory.Label("Title", card.transform, new Vector2(.045f, .56f), new Vector2(.29f, .94f), "", 38, UiFactory.Gold), 38);
            UiFactory.Panel("Line", card.transform, new Vector2(.31f, .08f), new Vector2(.312f, .94f), UiFactory.GoldDim);
            _body = UiFactory.Body(UiFactory.ScrollBody(UiFactory.Label("Body", card.transform, new Vector2(.34f, .08f), new Vector2(.95f, .94f), "", 36, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal), 36), 1.35f);
            _pageLabel = UiFactory.Label("Page", card.transform, new Vector2(.045f, .40f), new Vector2(.29f, .50f), "", 28, UiFactory.Dim, TextAnchor.MiddleLeft);
            var btn = UiFactory.Button("Next", card.transform, new Vector2(.045f, .10f), new Vector2(.29f, .30f), "다음", UiFactory.Red, () => _tapped = true, 36);
            _buttonLabel = btn.GetComponentInChildren<Text>();
            _root.SetActive(false);
        }

        /// <summary>Shows a topic and waits until the player has read every page. <paramref name="markDone"/> records it as seen.
        /// The automatic first showing (markDone) uses the short pages; opening it on request (? buttons, help list) uses the fuller ones.</summary>
        public IEnumerator ShowAndWait(string topicId, bool markDone)
        {
            var topic = Tutorial.Find(topicId);
            if (topic == null) yield break;
            var pages = topic.PagesFor(!markDone);
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            _title.text = topic.Title;
            for (int i = 0; i < pages.Length; i++)
            {
                _body.text = pages[i];
                _pageLabel.text = (i + 1) + " / " + pages.Length;
                _buttonLabel.text = i == pages.Length - 1 ? "알겠어요" : "다음";
                _tapped = false;
                while (!_tapped) yield return null;
            }
            _root.SetActive(false);
            if (markDone)
            {
                Tutorial.MarkDone(GameManager.Instance.Save, topicId);
                GameManager.Instance.SaveNow();
            }
        }

        /// <summary>Fire-and-forget version for menus.</summary>
        public void Show(string topicId, bool markDone = false)
        {
            if (_root.activeSelf) return;
            StartCoroutine(ShowAndWait(topicId, markDone));
        }

        /// <summary>First-time hint: shows the topic only if it has not been seen yet.</summary>
        public IEnumerator ShowOnce(string topicId)
        {
            if (Tutorial.IsDone(GameManager.Instance.Save, topicId)) yield break;
            yield return ShowAndWait(topicId, true);
        }

        public bool IsOpen { get { return _root != null && _root.activeSelf; } }
    }
}
