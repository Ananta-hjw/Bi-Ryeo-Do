using System;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>동료 수련 overlay: train each companion who has joined with 두 (capped by the bond).</summary>
    public sealed class CompanionPanel : OverlayPanel
    {

        public CompanionPanel(RectTransform root, Action onChange) : base(root, onChange)
        {
        }

        public void Open(string toast = null)
        {
            var save = GameManager.Instance.Save;
            Begin();
            if (!Tutorial.IsDone(save, "training") && HelpPanel.Instance != null) HelpPanel.Instance.Show("training", true);

            UiFactory.Label("Title", Root, new Vector2(0.05f, 0.92f), new Vector2(0.95f, 0.99f), "동료 수련 · " + save.Doo.ToString("N0") + "은자", 40, UiFactory.Gold);
            UiFactory.ScrollBody(UiFactory.Label("Hint", Root, new Vector2(0.05f, 0.845f), new Vector2(0.95f, 0.92f), toast ?? "수련 단계마다 체력·공격·방어 +4%. 인연이 깊을수록 더 높이 수련할 수 있습니다 (서먹 3 · 친분 5 · 신뢰 8 · 의기투합 10).", 30, UiFactory.PaperText, TextAnchor.MiddleCenter, FontStyle.Normal), 30);

            var content = CodexPanel.Scroller(Root, 0.09f, 0.84f);
            int n = 0;
            foreach (var c in EarlyGameContent.CompanionsFor(Math.Max(1, save.HighestUnlockedStage)))
            {
                var band = CodexPanel.Band(content, n++);
                int level = CompanionTraining.LevelOf(save, c.Id), cap = CompanionTraining.Cap(save, c.Id), bond = Bonds.Level(save, c.Id);
                long cost = CompanionTraining.NextCost(save, c.Id);
                UiFactory.Label("Name", band, new Vector2(0.04f, 0.58f), new Vector2(0.66f, 0.97f), c.Name + "  수련 Lv." + level + "/" + cap, 32, level >= CompanionTraining.MaxLevel ? UiFactory.Gold : UiFactory.PaperText, TextAnchor.MiddleLeft);
                string next = CompanionTraining.NextMilestone(c.Id, level);
                UiFactory.Label("Info", band, new Vector2(0.04f, 0.30f), new Vector2(0.66f, 0.58f),
                    "능력 +" + Math.Round((CompanionTraining.StatMultiplier(level) - 1f) * 100f) + "%  ·  인연 " + Bonds.LevelNames[bond], 30, UiFactory.PaperText, TextAnchor.MiddleLeft, FontStyle.Normal);
                UiFactory.Label("Next", band, new Vector2(0.04f, 0.02f), new Vector2(0.66f, 0.30f), next.Length > 0 ? "다음: " + next : "모든 단계 달성 (" + CompanionTraining.AwakeningText(c.Id) + ")", 30, UiFactory.Dim, TextAnchor.MiddleLeft, FontStyle.Normal);
                var companion = c;
                bool maxed = level >= CompanionTraining.MaxLevel, capped = level >= cap;
                bool can = !maxed && !capped && save.Doo >= cost;
                string label = maxed ? "최고 단계" : capped ? "인연 필요" : "수련\n" + cost.ToString("N0") + "은자";
                var btn = UiFactory.Button("Train", band, new Vector2(0.69f, 0.12f), new Vector2(0.97f, 0.88f), label, can ? UiFactory.Red : UiFactory.InkSoft, () => Train(companion), 24);
                btn.interactable = can;
            }
            if (n == 0) UiFactory.Label("None", Root, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.7f), "아직 함께하는 동료가 없습니다.", 34, UiFactory.PaperText);
            SetListHeight(content, n);
            AddClose();
        }

        private void Train(CompanionData c)
        {
            var save = GameManager.Instance.Save;
            string error;
            if (CompanionTraining.Train(save, c.Id, out error))
            {
                GameManager.Instance.SaveNow();
                SoundManager.Sfx("levelup");
                int level = CompanionTraining.LevelOf(save, c.Id);
                string extra = level == CompanionTraining.ArtBoostLevel || level == CompanionTraining.MainArtLevel || level == CompanionTraining.AwakenLevel ? "  새 효과 해금!" : "";
                Open(c.Name + " 수련 Lv." + level + extra);
            }
            else Open(error);
        }
    }
}
