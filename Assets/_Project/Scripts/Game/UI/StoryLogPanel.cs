using System;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;

namespace BiRyeoDo.Game.UI
{
    /// <summary>이야기 줄거리 overlay: the plain-language recap of every stage reached so far, to read again at any time (no rewards).</summary>
    public sealed class StoryLogPanel : OverlayPanel
    {
        private readonly Action<string, string[], Action> _play;

        public StoryLogPanel(RectTransform root, Action onChange, Action<string, string[], Action> play) : base(root, onChange)
        {
            _play = play;
        }

        public void Open()
        {
            var save = GameManager.Instance.Save;
            Begin();
            var readable = StoryBriefings.Readable(save);
            UiFactory.Label("Title", Root, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "이야기 줄거리", 40, UiFactory.Gold);
            UiFactory.Label("Hint", Root, new Vector2(0.05f, 0.845f), new Vector2(0.95f, 0.92f),
                "지금까지의 이야기를 인물과 사정 중심으로 다시 읽습니다. 돌파한 스테이지와 다음 스테이지가 열려 있습니다.", 22, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal);

            var content = CodexPanel.Scroller(Root, 0.09f, 0.84f);
            for (int i = 0; i < readable.Count; i++)
            {
                var stage = EarlyGameContent.Stages[readable[i] - 1];
                var band = CodexPanel.Band(content, i);
                bool cleared = save.ClearedStage >= stage.Number;
                UiFactory.Label("Name", band, new Vector2(0.04f, 0.54f), new Vector2(0.60f, 0.97f), stage.Number + ". " + stage.Name, 30, UiFactory.PaperText, TextAnchor.MiddleLeft);
                UiFactory.Label("Info", band, new Vector2(0.04f, 0.04f), new Vector2(0.60f, 0.54f), cleared ? "돌파한 이야기" : "다음 이야기", 20, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                var s = stage;
                UiFactory.Button("Before", band, new Vector2(0.62f, 0.14f), new Vector2(0.79f, 0.86f), "배경", UiFactory.InkSoft, () => Play(s.Name + " — 배경", StoryBriefings.Briefing(s.Number)), 26);
                var after = UiFactory.Button("After", band, new Vector2(0.81f, 0.14f), new Vector2(0.97f, 0.86f), "그 뒤", UiFactory.InkSoft, () => Play(s.Name + " — 그 뒤", StoryBriefings.Aftermath(s.Number)), 26);
                after.interactable = cleared && StoryBriefings.Aftermath(s.Number) != null;
            }
            SetListHeight(content, readable.Count);
            AddClose();
        }

        private void Play(string title, string[] lines)
        {
            if (_play == null || lines == null) return;
            Root.gameObject.SetActive(false);
            _play(title, lines, Open);
        }
    }
}
