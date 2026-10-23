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
            UiFactory.ScrollBody(UiFactory.Label("Hint", Root, new Vector2(0.05f, 0.845f), new Vector2(0.95f, 0.92f),
                "지금까지의 이야기를 인물과 사정 중심으로 다시 읽습니다. 돌파한 스테이지와 다음 스테이지가 열려 있습니다.", 30, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 30);

            var content = CodexPanel.Scroller(Root, .16f, 0.84f);
            for (int i = 0; i < readable.Count; i++)
            {
                var stage = EarlyGameContent.Stages[readable[i] - 1];
                var band = CodexPanel.Band(content, i);
                bool cleared = save.ClearedStage >= stage.Number;
                UiFactory.Label("Name", band, new Vector2(0.04f, 0.54f), new Vector2(0.38f, 0.97f), stage.Number + ". " + stage.Name, 28, UiFactory.PaperText, TextAnchor.MiddleLeft);
                UiFactory.Label("Info", band, new Vector2(0.04f, 0.04f), new Vector2(0.38f, 0.54f), cleared ? "돌파한 이야기" : "다음 이야기", 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                var s = stage;
                UiFactory.Button("Before", band, new Vector2(0.40f, 0.14f), new Vector2(0.53f, 0.86f), "배경", UiFactory.InkSoft, () => Play(s.Name + " — 배경", StoryBriefings.Briefing(s.Number)), 24);
                var between = StoryBriefings.Interlude(s.Number);
                var fun = UiFactory.Button("Fun", band, new Vector2(0.545f, 0.14f), new Vector2(0.675f, 0.86f), "막간", UiFactory.InkSoft, () => Play("막간 — " + between.Title, between.Lines), 24);
                fun.interactable = between != null;
                var body = UiFactory.Button("Body", band, new Vector2(.69f, .14f), new Vector2(.815f, .86f), "본문", UiFactory.InkSoft,
                    () => Play(s.Name + " — 본문", StoryEpisodes.Replay(s)), 24);
                body.interactable = cleared;
                var after = UiFactory.Button("After", band, new Vector2(0.82f, 0.14f), new Vector2(0.97f, 0.86f), "그 뒤", UiFactory.InkSoft, () => Play(s.Name + " — 그 뒤", StoryBriefings.Aftermath(s.Number)), 24);
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
