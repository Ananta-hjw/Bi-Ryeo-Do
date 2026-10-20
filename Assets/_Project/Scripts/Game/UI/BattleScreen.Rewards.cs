using System.Collections;
using System.Collections.Generic;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    public partial class BattleScreen
    {
        private RectTransform _fightRewards;
        private bool _rewardsDone;

        private void HideFightRewards()
        {
            if (_fightRewards != null) _fightRewards.gameObject.SetActive(false);
        }

        private static Text RewardText(string name, Transform parent, Vector2 min, Vector2 max,
            string value, int size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
        {
            return UiFactory.Body(UiFactory.Label(name, parent, min, max, value, size, color, align), 1.2f);
        }

        private IEnumerator ShowFightRewards(StageData stage, int fight, int mask, int newStars, long exp, long doo,
            List<ItemStack> drops, string reputation, int levels)
        {
            SetCommandsVisible(false);
            _banner.text = "";
            _panel.SetActive(false);
            if (_fightRewards != null) { _fightRewards.gameObject.SetActive(false); Destroy(_fightRewards.gameObject); }
            _fightRewards = UiFactory.Rect("FightRewards", (RectTransform)_fx.parent, Vector2.zero, Vector2.one);
            UiFactory.Panel("Dim", _fightRewards, Vector2.zero, Vector2.one, new Color(.015f, .025f, .035f, .97f)).raycastTarget = true;
            var card = UiFactory.Panel("RewardCard", _fightRewards, new Vector2(.04f, .07f), new Vector2(.96f, .94f), UiFactory.Ink);
            UiFactory.Frame(card.transform, UiFactory.GoldDim, 3f);
            RewardText("Title", card.transform, new Vector2(.06f, .91f), new Vector2(.94f, .98f), "승전 기록", 48, UiFactory.Gold, TextAnchor.MiddleCenter);
            RewardText("Fight", card.transform, new Vector2(.06f, .85f), new Vector2(.94f, .91f),
                stage.Name + " · 전투 " + (fight + 1), 28, UiFactory.PaperText, TextAnchor.MiddleCenter);

            var stars = UiFactory.Panel("Stars", card.transform, new Vector2(.05f, .65f), new Vector2(.95f, .84f), HudInk);
            if (mask >= 0)
            {
                RewardText("StarCount", stars.transform, new Vector2(.04f, .50f), new Vector2(.96f, .98f),
                    StarGlyphs(mask) + "   " + StageStars.Count(mask) + " / 3", 58, UiFactory.Gold, TextAnchor.MiddleCenter);
                string[] criteria = { "승리", "전원 생존", "신속 " + _fightActions + "/" + StageStars.FastLimit(stage.Number, fight, stage.Difficulty) + "행동" };
                for (int i = 0; i < 3; i++)
                    RewardText("Criterion" + i, stars.transform, new Vector2(.02f + i * .33f, .25f), new Vector2(.32f + i * .33f, .52f),
                        ((mask & (1 << i)) != 0 ? "✓ " : "· ") + criteria[i], 25,
                        (mask & (1 << i)) != 0 ? UiFactory.PaperText : UiFactory.Dim, TextAnchor.MiddleCenter);
                RewardText("NewStars", stars.transform, new Vector2(.04f, .02f), new Vector2(.96f, .27f),
                    newStars > 0 ? "새로 획득한 별  +" + newStars : "이번 전투의 별점입니다 · 최고 기록은 유지됩니다", 25,
                    newStars > 0 ? UiFactory.Gold : UiFactory.Dim, TextAnchor.MiddleCenter);
            }
            else RewardText("Victory", stars.transform, Vector2.zero, Vector2.one, "전투 승리", 42, UiFactory.Gold, TextAnchor.MiddleCenter);

            RewardValue(card.transform, .05f, .49f, "획득 경험치", "+" + exp.ToString("N0"), QiBlue);
            RewardValue(card.transform, .51f, .95f, "획득 두", "+" + doo.ToString("N0"), UiFactory.Gold);
            RewardText("LootTitle", card.transform, new Vector2(.06f, .49f), new Vector2(.94f, .55f), "획득 물품", 30, UiFactory.Gold);
            var content = CodexPanel.Scroller(card.rectTransform, .15f, .49f);
            int rowIndex = 0;
            foreach (var stack in drops)
            {
                var item = EarlyGameContent.FindItem(stack.Id);
                if (item == null || stack.Count <= 0) continue;
                var row = CodexPanel.Band(content, rowIndex++);
                row.sizeDelta = new Vector2(0f, 112f);
                var icon = UiFactory.Panel("Icon", row, new Vector2(.025f, .12f), new Vector2(.16f, .88f), Color.white);
                var glyph = UiFactory.Label("Glyph", icon.transform, Vector2.zero, Vector2.one, "", 40, UiFactory.Gold);
                ItemArt.Apply(icon, glyph, item);
                RewardText("ItemName", row, new Vector2(.19f, .08f), new Vector2(.81f, .92f), UiFactory.GradedName(item), 30, UiFactory.PaperText);
                RewardText("Amount", row, new Vector2(.82f, .08f), new Vector2(.97f, .92f), "×" + stack.Count.ToString("N0"), 36, UiFactory.Gold, TextAnchor.MiddleRight);
            }
            if (rowIndex == 0)
            {
                var empty = CodexPanel.Band(content, rowIndex++);
                RewardText("NoItems", empty, new Vector2(.04f, .10f), new Vector2(.96f, .90f), "이번 전투에서 획득한 물품이 없습니다.", 28, UiFactory.Dim);
            }
            if (!string.IsNullOrEmpty(reputation) || levels > 0)
            {
                var row = CodexPanel.Band(content, rowIndex++);
                RewardText("OtherRewards", row, new Vector2(.04f, .08f), new Vector2(.96f, .92f),
                    (levels > 0 ? "레벨 상승  Lv." + GameManager.Instance.Save.Level + "\n" : "") + reputation, 28, UiFactory.PaperText);
            }
            UiFactory.Button("Continue", card.transform, new Vector2(.20f, .025f), new Vector2(.80f, .12f), "확인 · 계속", UiFactory.Red, () => _rewardsDone = true, 34);
            _rewardsDone = false;
            _fightRewards.SetAsLastSibling();
            try { while (!_rewardsDone && !_fled) yield return null; }
            finally { HideFightRewards(); }
            StartCoroutine(GuardTaps());
        }

        private static void RewardValue(Transform parent, float left, float right, string title, string value, Color color)
        {
            var band = UiFactory.Panel("RewardValue", parent, new Vector2(left, .56f), new Vector2(right, .65f), HudInk);
            RewardText("Label", band.transform, new Vector2(.06f, .57f), new Vector2(.94f, .97f), title, 24, UiFactory.Dim);
            RewardText("Value", band.transform, new Vector2(.06f, .02f), new Vector2(.94f, .60f), value, 44, color, TextAnchor.MiddleRight);
        }
    }
}
