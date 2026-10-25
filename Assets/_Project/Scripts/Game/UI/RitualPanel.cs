using System;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>전승의식 overlay: the six conditions, the 초월 reward (진비뢰도) and the button that performs the ritual once everything is met.</summary>
    public sealed class RitualPanel : OverlayPanel
    {
        private static readonly string[] Scene =
        {
            "봉뢰함의 뚜껑이 소리 없이 열린다. 열 자루의 비뢰도가 한꺼번에 낮게 운다.",
            "뇌령사가 손끝에서 풀려나와, 오랫동안 미완이던 전승의 매듭이 하나씩 이어진다.",
            "마지막 한 자루까지 기운을 받아들이자, 열 자루는 비로소 하나의 주인을 알아본다."
        };

        private readonly Action<string, string[], Action> _play;

        public RitualPanel(RectTransform root, Action onChange, Action<string, string[], Action> play) : base(root, onChange)
        {
            _play = play;
        }

        public void Open(string toast = null)
        {
            var save = GameManager.Instance.Save;
            Begin();
            if (!Tutorial.IsDone(save, "ritual") && HelpPanel.Instance != null) HelpPanel.Instance.Show("ritual", true);

            UiFactory.Label("Title", Root, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "전승의식", 44, UiFactory.Gold);
            UiFactory.Body(UiFactory.ScrollBody(UiFactory.Label("Hint", Root, new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.92f),
                toast ?? "봉뢰함의 진비뢰도를 쓰려면 전승의식을 마쳐야 합니다. 아래 여섯 가지를 모두 갖추면 의식을 치를 수 있습니다. 보상은 5성 진비뢰도입니다.", 30, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 30));

            var sword = EarlyGameContent.FindItem(Ritual.RewardId);
            var conditions = Ritual.Conditions(save);
            var content = CodexPanel.Scroller(Root, .02f, .79f);
            LandscapeMenu.Place((RectTransform)content.parent, .02f, .02f, .65f, .79f);
            var grid = new ReadingGrid(content, 2, 240f);
            for (int i = 0; i < conditions.Count; i++)
            {
                var c = conditions[i];
                var band = grid.Add("Condition" + i);
                UiFactory.Label("Mark", band, new Vector2(0.03f, 0.1f), new Vector2(0.14f, 0.9f), c.Done ? "✔" : "✘", 44, c.Done ? new Color(0.45f, 0.85f, 0.5f, 1f) : UiFactory.Red);
                UiFactory.Label("Text", band, new Vector2(0.16f, 0.50f), new Vector2(0.97f, 0.96f), c.Text, 30, c.Done ? UiFactory.PaperText : UiFactory.Dim, TextAnchor.MiddleLeft);
                UiFactory.Label("Progress", band, new Vector2(0.16f, 0.04f), new Vector2(0.97f, 0.50f), "현재: " + c.Progress, 30, c.Done ? new Color(0.45f, 0.85f, 0.5f, 1f) : UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
            }

            var rewardIcon = UiFactory.Panel("RewardIcon", Root, new Vector2(.73f, .61f), new Vector2(.93f, .79f), Color.white);
            UiFactory.Frame(rewardIcon.transform, UiFactory.GradeColor(sword.Grade), 2f);
            var rewardGlyph = UiFactory.Label("Glyph", rewardIcon.transform, Vector2.zero, Vector2.one, "", 54, UiFactory.PaperText);
            ItemArt.Apply(rewardIcon, rewardGlyph, sword);
            UiFactory.Body(UiFactory.Label("Reward", Root, new Vector2(.68f, .48f), new Vector2(.97f, .60f),
                "보상  " + UiFactory.GradedName(sword) + " · 5성", 32, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal));
            UiFactory.ScrollBody(UiFactory.Label("RewardEffects", Root, new Vector2(.67f, .21f), new Vector2(.98f, .47f),
                UiFactory.GearEffectText(sword, 0, Gear.MaxStars), 30, UiFactory.PaperText, TextAnchor.UpperLeft, FontStyle.Normal), 30);

            if (save.RitualDone)
            {
                UiFactory.Button("Done", Root, new Vector2(.68f, .03f), new Vector2(.97f, .19f), "전승의식 완료", UiFactory.InkSoft, null, 34).interactable = false;
            }
            else
            {
                bool can = Ritual.CanComplete(save);
                var btn = UiFactory.Button("Start", Root, new Vector2(.68f, .03f), new Vector2(.97f, .19f), can ? "전승의식 시작" : "조건 " + Ritual.DoneCount(save) + "/" + conditions.Count, can ? UiFactory.Gold : UiFactory.InkSoft, Perform, 34);
                btn.interactable = can;
            }
            AddClose();
        }

        private void Perform()
        {
            var save = GameManager.Instance.Save;
            if (!Ritual.CanComplete(save)) return;
            Action finish = () =>
            {
                var note = Ritual.Complete(save);
                GameManager.Instance.SaveNow();
                if (note != null) SoundManager.Sfx("levelup");
                Open(note);
            };
            if (_play == null) { finish(); return; }
            Root.gameObject.SetActive(false);
            _play("전승의식", Scene, finish);
        }
    }
}
