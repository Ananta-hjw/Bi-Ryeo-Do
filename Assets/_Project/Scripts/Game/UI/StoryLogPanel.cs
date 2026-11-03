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
        private readonly Action<string, StoryScene[], Action> _play;

        public StoryLogPanel(RectTransform root, Action onChange, Action<string, StoryScene[], Action> play) : base(root, onChange)
        {
            _play = play;
        }

        public void Open()
        {
            var save = GameManager.Instance.Save;
            if (RecordUpdates.Acknowledge(save, RecordUpdates.StoryPage) && Application.isPlaying) GameManager.Instance.SaveNow();
            Begin();
            var readable = StoryBriefings.Readable(save);
            UiFactory.Label("Title", Root, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "이야기 줄거리", 40, UiFactory.Gold);
            UiFactory.ScrollBody(UiFactory.Label("Hint", Root, new Vector2(0.05f, 0.845f), new Vector2(0.95f, 0.92f),
                "돌파한 이야기를 다시 읽습니다. 전체 이야기는 스테이지 진행 순서대로 이어집니다.", 30, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 30);

            var content = CodexPanel.Scroller(Root, .02f, 0.84f);
            if (readable.Count == 0)
                UiFactory.Label("Empty", Root, new Vector2(.08f, .36f), new Vector2(.92f, .62f),
                    "아직 돌파한 스테이지가 없습니다.\n스테이지를 돌파하면 이야기가 열립니다.", 30, UiFactory.Dim);
            var grid = new ReadingGrid(content, 2, 260f);
            for (int i = 0; i < readable.Count; i++)
            {
                var stage = EarlyGameContent.Stages[readable[i] - 1];
                var band = grid.Add("Story" + i);
                bool cleared = save.ClearedStage >= stage.Number;
                UiFactory.Label("Name", band, new Vector2(0.04f, 0.64f), new Vector2(0.96f, 0.96f), stage.Number + ". " + stage.Name, 28, UiFactory.PaperText, TextAnchor.MiddleLeft);
                if (EarlyStory.Supports(stage))
                {
                    UiFactory.Label("Info", band, new Vector2(.04f, .37f), new Vector2(.96f, .61f),
                        "도입부터 후일담까지 · 진행 순서대로", 26, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                    var chapter = stage;
                    UiFactory.Button("FullStory", band, new Vector2(.04f, .04f), new Vector2(.96f, .30f),
                        "전체 이야기", UiFactory.InkSoft, () => PlayChapter(chapter), 26);
                    continue;
                }
                UiFactory.Label("Info", band, new Vector2(0.04f, 0.37f), new Vector2(0.96f, 0.61f), cleared ? "돌파한 이야기" : "다음 이야기", 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                var s = stage;
                UiFactory.Button("Before", band, new Vector2(0.04f, 0.04f), new Vector2(0.255f, 0.30f), "배경", UiFactory.InkSoft, () => Play(s.Name + " — 배경", new[] { EarlyStory.Background(s) ?? new StoryScene { Lines = StoryBriefings.Briefing(s.Number), HeroArtKey = s.IntroHeroArtKey } }), 24);
                var between = StoryBriefings.Interlude(s.Number);
                var fun = UiFactory.Button("Fun", band, new Vector2(0.275f, 0.04f), new Vector2(0.49f, 0.30f), "막간", UiFactory.InkSoft, () => Play("막간 — " + between.Title, new[] { between }), 24);
                fun.interactable = between != null;
                var body = UiFactory.Button("Body", band, new Vector2(.51f, .04f), new Vector2(.725f, .30f), "본문", UiFactory.InkSoft,
                    () => Play(s.Name + " — 본문", StoryEpisodes.ReplayScenes(s)), 24);
                body.interactable = cleared;
                var after = UiFactory.Button("After", band, new Vector2(0.745f, 0.04f), new Vector2(0.96f, 0.30f), "그 뒤", UiFactory.InkSoft, () => Play(s.Name + " — 그 뒤", new[] { new StoryScene { Lines = StoryBriefings.Aftermath(s.Number), HeroArtKey = s.OutroHeroArtKey } }), 24);
                after.interactable = cleared && StoryBriefings.Aftermath(s.Number) != null;
            }
            AddClose();
        }

        private void PlayChapter(StageData stage)
        {
            if (stage == null || !StoryBriefings.CanRead(GameManager.Instance.Save, stage.Number)) return;
            Play(stage.Number + ". " + stage.Name + " — 전체 이야기", StoryEpisodes.ReplayScenes(stage));
        }

        private void Play(string title, StoryScene[] lines)
        {
            if (_play == null || lines == null) return;
            Root.gameObject.SetActive(false);
            _play(title, lines, Open);
        }
    }
}
