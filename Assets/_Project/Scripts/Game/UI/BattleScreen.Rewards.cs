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
            var card = CreateRewardCard("FightRewards", "승전 기록", stage.Name + " · 전투 " + (fight + 1));

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
            RewardValue(card.transform, .51f, .95f, "획득 은자", "+" + doo.ToString("N0"), UiFactory.Gold);
            RewardText("LootTitle", card.transform, new Vector2(.06f, .49f), new Vector2(.94f, .55f), "획득 물품", 30, UiFactory.Gold);
            var content = CodexPanel.Scroller(card, .15f, .49f);
            int rowIndex = 0;
            AddRewardItems(content, ref rowIndex, drops);
            if (rowIndex == 0)
                AddRewardNote(content, ref rowIndex, "획득 물품", "이번 전투에서 획득한 물품이 없습니다.");
            if (!string.IsNullOrEmpty(reputation) || levels > 0)
            {
                var row = RewardRow(content, rowIndex++, 160f);
                RewardText("OtherRewards", row, new Vector2(.04f, .08f), new Vector2(.96f, .92f),
                    (levels > 0 ? "레벨 상승  Lv." + GameManager.Instance.Save.Level + "\n" : "") + reputation, 28, UiFactory.PaperText);
            }
            yield return WaitForRewardConfirmation(card, "확인 · 계속");
        }

        private RectTransform CreateRewardCard(string rootName, string title, string subtitle)
        {
            SetCommandsVisible(false);
            _banner.text = "";
            _panel.SetActive(false);
            if (_fightRewards != null) { _fightRewards.gameObject.SetActive(false); Destroy(_fightRewards.gameObject); }
            _fightRewards = UiFactory.Rect(rootName, (RectTransform)_fx.parent, Vector2.zero, Vector2.one);
            UiFactory.Panel("Dim", _fightRewards, Vector2.zero, Vector2.one, new Color(.015f, .025f, .035f, .97f)).raycastTarget = true;
            var card = UiFactory.Panel("RewardCard", _fightRewards, new Vector2(.04f, .07f), new Vector2(.96f, .94f), UiFactory.Ink);
            UiFactory.Frame(card.transform, UiFactory.GoldDim, 3f);
            UiFactory.Panel("SummaryBacking", card.transform, new Vector2(.025f, .155f), new Vector2(.415f, .835f), HudInk);
            UiFactory.Panel("ColumnRule", card.transform, new Vector2(.421f, .17f), new Vector2(.422f, .835f), UiFactory.GoldDim);
            RewardText("Title", card.transform, new Vector2(.06f, .91f), new Vector2(.94f, .98f), title, 48, UiFactory.Gold, TextAnchor.MiddleCenter);
            RewardText("Subtitle", card.transform, new Vector2(.06f, .85f), new Vector2(.94f, .91f), subtitle, 28, UiFactory.PaperText, TextAnchor.MiddleCenter);
            return card.rectTransform;
        }

        private IEnumerator WaitForRewardConfirmation(RectTransform card, string buttonLabel)
        {
            UiFactory.Button("Continue", card, new Vector2(.20f, .025f), new Vector2(.80f, .12f), buttonLabel, UiFactory.Red, () => _rewardsDone = true, 34);
            yield return WaitForRewardChoice();
        }

        private IEnumerator WaitForRewardChoice()
        {
            _rewardsDone = false;
            LayoutRewardCard((RectTransform)_fightRewards.Find("RewardCard"));
            _fightRewards.SetAsLastSibling();
            try { while (!_rewardsDone && !_fled) yield return null; }
            finally { HideFightRewards(); }
            StartCoroutine(GuardTaps());
        }

        private static RectTransform RewardRow(RectTransform content, int index, float height)
        {
            var row = UiFactory.Rect("RewardRow" + index, content, new Vector2(.04f, 1f), new Vector2(.96f, 1f));
            row.pivot = new Vector2(.5f, 1f);
            row.sizeDelta = new Vector2(0f, height);
            row.anchoredPosition = new Vector2(0f, -(CodexPanel.RowGap + index * (height + CodexPanel.RowGap)));
            UiFactory.Panel("Background", row, Vector2.zero, Vector2.one, HudInk).raycastTarget = true;
            UiFactory.Frame(row, UiFactory.GoldDim, 1f);
            ReadingList.Register(row, height, CodexPanel.RowGap);
            return row;
        }

        private static void AddRewardItems(RectTransform content, ref int index, List<ItemStack> items)
        {
            foreach (var stack in items)
            {
                var item = EarlyGameContent.FindItem(stack.Id);
                if (item == null || stack.Count <= 0) continue;
                var row = RewardRow(content, index++, 112f);
                var icon = UiFactory.Panel("Icon", row, new Vector2(.025f, .12f), new Vector2(.16f, .88f), Color.white);
                var glyph = UiFactory.Label("Glyph", icon.transform, Vector2.zero, Vector2.one, "", 40, UiFactory.Gold);
                ItemArt.Apply(icon, glyph, item);
                RewardText("ItemName", row, new Vector2(.19f, .08f), new Vector2(.81f, .92f), UiFactory.GradedName(item), 30, UiFactory.PaperText);
                RewardText("Amount", row, new Vector2(.82f, .08f), new Vector2(.97f, .92f), "×" + stack.Count.ToString("N0"), 36, UiFactory.Gold, TextAnchor.MiddleRight);
            }
        }

        private static void AddRewardNote(RectTransform content, ref int index, string title, string body)
        {
            var row = RewardRow(content, index++, 180f);
            RewardText("Heading", row, new Vector2(.04f, .73f), new Vector2(.96f, .96f), title, 28, UiFactory.Gold);
            RewardText("Body", row, new Vector2(.04f, .05f), new Vector2(.96f, .70f), body, 28, UiFactory.PaperText, TextAnchor.UpperLeft);
        }

        private IEnumerator ShowStageRewards(StageData stage, long exp, long doo, int newStars,
            List<ItemStack> firstRewards, List<string> materials, string bonds, int chests)
        {
            var save = GameManager.Instance.Save;
            var card = CreateRewardCard("StageRewards", "스테이지 돌파", StageStars.TagFor(stage.Difficulty) + stage.Name);
            SoundManager.Sfx("clear");
            _retry = false; _leave = false;
            int totalStars = StageStars.StarsOfStage(save, stage.Number, stage.Difficulty);
            int maxStars = StageStars.MaxStarsOfStage(stage.Number);
            var stars = UiFactory.Panel("Stars", card, new Vector2(.05f, .65f), new Vector2(.95f, .84f), HudInk);
            RewardText("StarCount", stars.transform, new Vector2(.04f, .43f), new Vector2(.96f, .98f),
                "★  " + totalStars + " / " + maxStars, 58, UiFactory.Gold, TextAnchor.MiddleCenter);
            RewardText("StarScope", stars.transform, new Vector2(.04f, .24f), new Vector2(.96f, .45f), "이 난이도의 누적 최고 별 기록", 25, UiFactory.Dim, TextAnchor.MiddleCenter);
            RewardText("NewStars", stars.transform, new Vector2(.04f, .02f), new Vector2(.96f, .25f),
                newStars > 0 ? "이번 진행에서 새로 얻은 별  +" + newStars : "새로 얻은 별 없음 · 최고 기록은 유지됩니다", 28,
                newStars > 0 ? UiFactory.Gold : UiFactory.Dim, TextAnchor.MiddleCenter);
            RewardValue(card, .05f, .49f, "이번 진행 경험치", "+" + exp.ToString("N0"), QiBlue);
            RewardValue(card, .51f, .95f, "이번 진행 은자", "+" + doo.ToString("N0"), UiFactory.Gold);
            RewardText("SummaryTitle", card, new Vector2(.06f, .49f), new Vector2(.94f, .55f), "보상 및 전투 기록", 30, UiFactory.Gold);
            var content = CodexPanel.Scroller(card, .15f, .49f);
            int index = 0;
            AddRewardNote(content, ref index, firstRewards.Count > 0 ? "첫 돌파 보상 · 지급 완료" : "첫 돌파 보상",
                firstRewards.Count > 0 ? "아래 물품이 행낭에 지급되었습니다." : "이번 진행에 추가 첫 돌파 물품은 없습니다.");
            AddRewardItems(content, ref index, firstRewards);
            if (materials.Count > 0)
                AddRewardNote(content, ref index, "이번 진행 강화 재료 · 지급 완료", string.Join("\n", materials));
            if (!string.IsNullOrEmpty(bonds)) AddRewardNote(content, ref index, "인연 성장", bonds);
            if (chests > 0)
                AddRewardNote(content, ref index, "받을 수 있는 별 보상  " + chests + "개", "강호 탭에서 별 보상 상자를 확인해 주세요.");
            AddRewardNote(content, ref index, "전투별 최고 별 기록", "승리 / 전원 생존 / 신속 순서입니다.\n이번 진행 이전에 획득한 별도 포함합니다.");
            for (int fight = 0; fight < StageStars.FightCount(stage.Number); fight++)
            {
                int mask = StageStars.MaskOf(save, stage.Number, fight, stage.Difficulty);
                var row = RewardRow(content, index++, 100f);
                RewardText("Fight", row, new Vector2(.04f, .08f), new Vector2(.43f, .92f),
                    fight == StageStars.FightCount(stage.Number) - 1 ? "결전" : "전투 " + (fight + 1), 28, UiFactory.PaperText);
                RewardText("Stars", row, new Vector2(.45f, .08f), new Vector2(.96f, .92f),
                    StarGlyphs(mask) + "  " + StageStars.Count(mask) + "/3", 36, UiFactory.Gold, TextAnchor.MiddleRight);
            }
            yield return WaitForRewardConfirmation(card, "확인");
        }

        private IEnumerator ShowLevelUp(StageData stage, Dictionary<string, int> levelsBefore,
            Dictionary<string, CombatStats> statsBefore)
        {
            var save = GameManager.Instance.Save;
            var card = CreateRewardCard("LevelUpRewards", "레벨 업", stage.Name + " · 전투 후 성장 기록");
            SoundManager.Sfx("levelup");
            _retry = false; _leave = false;
            var content = CodexPanel.Scroller(card, .15f, .71f);
            int index = 0, members = 0, gains = 0;
            var fielded = new HashSet<string>();
            foreach (var unit in _party)
            {
                fielded.Add(unit.Id);
                int before;
                if (!levelsBefore.TryGetValue(unit.Id, out before)) continue;
                int after = unit.Id == Inventory.Hero ? save.Level : CompanionGrowth.LevelOf(save, unit.Id);
                if (after <= before) continue;
                CombatStats previous;
                if (!statsBefore.TryGetValue(unit.Id, out previous)) previous = unit.Stats;
                AddLevelGrowth(content, ref index, unit.Id, unit.Name, before, after, previous, unit.Stats, true);
                members++; gains += after - before;
            }
            foreach (var companion in EarlyGameContent.Companions)
            {
                int before;
                if (fielded.Contains(companion.Id) || !levelsBefore.TryGetValue(companion.Id, out before)) continue;
                int after = CompanionGrowth.LevelOf(save, companion.Id);
                if (after <= before) continue;
                AddLevelGrowth(content, ref index, companion.Id, companion.Name, before, after, default(CombatStats), default(CombatStats), false);
                members++; gains += after - before;
            }
            var summary = UiFactory.Panel("GrowthSummary", card, new Vector2(.05f, .73f), new Vector2(.95f, .84f), HudInk);
            RewardText("Members", summary.transform, new Vector2(.04f, .42f), new Vector2(.96f, .98f),
                members + "명 성장  ·  총 +" + gains + "레벨", 42, UiFactory.Gold, TextAnchor.MiddleCenter);
            RewardText("Hint", summary.transform, new Vector2(.04f, .04f), new Vector2(.96f, .43f),
                "출전 인물과 대기 동료의 성장 내역을 확인해 주세요.", 26, UiFactory.Dim, TextAnchor.MiddleCenter);
            yield return WaitForRewardConfirmation(card, "성장 확인");
        }

        private void AddLevelGrowth(RectTransform content, ref int index, string id, string name,
            int before, int after, CombatStats previous, CombatStats current, bool fielded)
        {
            var row = RewardRow(content, index++, fielded ? 400f : 220f);
            var portrait = UiFactory.Panel("Portrait", row, new Vector2(.03f, fielded ? .68f : .30f), new Vector2(.18f, .96f), Color.white);
            portrait.preserveAspect = true;
            portrait.sprite = BattleFigure(id);
            if (portrait.sprite == null)
            {
                portrait.color = Color.clear;
                RewardText("Glyph", portrait.transform, Vector2.zero, Vector2.one, FirstGlyph(name), 44, UiFactory.Gold, TextAnchor.MiddleCenter);
            }
            RewardText("Member", row, new Vector2(.22f, fielded ? .84f : .70f), new Vector2(.96f, .98f), name, 32, UiFactory.PaperText);
            RewardText("Levels", row, new Vector2(.22f, fielded ? .66f : .32f), new Vector2(.96f, fielded ? .84f : .70f),
                "Lv." + before + "  →  Lv." + after, 42, UiFactory.Gold);
            RewardText("Recovery", row, new Vector2(.04f, fielded ? .53f : .04f), new Vector2(.96f, fielded ? .66f : .29f),
                fielded ? "출전 · 체력·내공 완전 회복" : "대기 · 공유 경험치로 레벨 상승", 26, fielded ? HpGreen : UiFactory.Dim);
            if (fielded)
            {
                string[] labels = { "최대 체력", "최대 내공", "공격력", "방어력", "속도" };
                int[] oldValues = { previous.MaxHp, previous.MaxQi, previous.Attack, previous.Defense, previous.Speed };
                int[] newValues = { current.MaxHp, current.MaxQi, current.Attack, current.Defense, current.Speed };
                for (int i = 0; i < labels.Length; i++)
                {
                    float left = i % 2 == 0 ? .04f : .52f;
                    float top = .51f - (i / 2) * .16f;
                    RewardText("Stat" + i, row, new Vector2(left, top - .14f), new Vector2(left + .44f, top),
                        labels[i] + "\n" + oldValues[i].ToString("N0") + " → " + newValues[i].ToString("N0"), 28, UiFactory.PaperText);
                }
            }
            var companion = EarlyGameContent.FindCompanion(id);
            if (companion != null)
            {
                var learned = CompanionGrowth.NewArtLines(companion, before, after);
                if (learned.Count > 0) AddRewardNote(content, ref index, name + " · 새 무공 습득", string.Join("\n", learned));
            }
        }

        private IEnumerator ShowHub(StageData stage, int doneIndex, int mask, int newStars)
        {
            int next = doneIndex + 1;
            bool boss = next == stage.Encounters.Length - 1;
            bool canResume = !stage.IsQuest && !stage.IsTower;
            var card = CreateRewardCard("BetweenFights", "다음 전투 준비", StageStars.TagFor(stage.Difficulty) + stage.Name);
            var progress = UiFactory.Panel("Progress", card, new Vector2(.05f, .72f), new Vector2(.95f, .84f), HudInk);
            RewardText("Victory", progress.transform, new Vector2(.04f, .43f), new Vector2(.96f, .98f),
                "전투 " + (doneIndex + 1) + " / " + stage.Encounters.Length + " 승리", 42, UiFactory.Gold, TextAnchor.MiddleCenter);
            RewardText("Stars", progress.transform, new Vector2(.04f, .03f), new Vector2(.96f, .45f),
                mask >= 0 ? StarGlyphs(mask) + "  " + StageStars.Count(mask) + "/3" + (newStars > 0 ? "  ·  새 별 +" + newStars : "  ·  새 별 없음") : "전투 후 회복이 반영되었습니다.",
                30, UiFactory.Gold, TextAnchor.MiddleCenter);
            RewardText("NextFight", card, new Vector2(.06f, .65f), new Vector2(.94f, .71f),
                boss ? "다음은 결전 · 전투 " + (next + 1) : "다음 전투 " + (next + 1) + " / " + stage.Encounters.Length,
                34, boss ? ThreatRed : UiFactory.PaperText, TextAnchor.MiddleCenter);
            RewardText("PartyTitle", card, new Vector2(.06f, .59f), new Vector2(.94f, .65f), "파티 상태 · 전투 후 회복 반영", 28, UiFactory.Gold);
            var content = CodexPanel.Scroller(card, .23f, .59f);
            int index = 0;
            foreach (var unit in _party) AddPartyRecovery(content, index++, unit);
            var trial = stage.TrialFor(next);
            if (trial != null) AddRewardNote(content, ref index, "다음 전투 조건 · " + trial.Title, trial.Brief);
            RewardText("SaveHint", card, new Vector2(.06f, .14f), new Vector2(.94f, .22f),
                canResume ? "물러나면 진행이 저장됩니다.\n다시 도전할 때 다음 전투부터 이어집니다." : "준비가 되면 다음 전투에 도전해 주세요.\n물러나면 다음 도전은 처음부터 시작합니다.",
                26, UiFactory.Dim, TextAnchor.MiddleCenter);
            UiFactory.Button("NextFight", card, new Vector2(.05f, .025f), new Vector2(.61f, .12f),
                boss ? "결전에 도전" : "다음 전투 도전", UiFactory.Red, () => _rewardsDone = true, 32);
            UiFactory.Button("Leave", card, new Vector2(.65f, .025f), new Vector2(.95f, .12f),
                canResume ? "물러나기\n(저장)" : "물러나기", UiFactory.InkSoft, () => { _leave = true; _rewardsDone = true; }, 30);
            _leave = false; _retry = false;
            yield return WaitForRewardChoice();
        }

        private static void AddPartyRecovery(RectTransform content, int index, Combatant unit)
        {
            var row = RewardRow(content, index, 210f);
            var portrait = UiFactory.Panel("Portrait", row, new Vector2(.03f, .24f), new Vector2(.17f, .94f), Color.white);
            portrait.preserveAspect = true;
            portrait.sprite = BattleFigure(unit.Id);
            if (portrait.sprite == null)
            {
                portrait.color = Color.clear;
                RewardText("Glyph", portrait.transform, Vector2.zero, Vector2.one, FirstGlyph(unit.Name), 44, UiFactory.Gold, TextAnchor.MiddleCenter);
            }
            RewardText("Name", row, new Vector2(.20f, .73f), new Vector2(.96f, .98f),
                unit.Name + (unit.IsAlive ? "  ·  기세 " + unit.Spirit : "  ·  쓰러짐"), 30, unit.IsAlive ? UiFactory.Gold : ThreatRed);
            AddRecoveryGauge(row, "체력", .40f, unit.Hp, unit.Stats.MaxHp, HpGreen);
            AddRecoveryGauge(row, "내공", .06f, unit.Qi, unit.Stats.MaxQi, QiBlue);
        }

        private static void AddRecoveryGauge(RectTransform row, string label, float bottom, int value, int maximum, Color color)
        {
            RewardText(label + "Label", row, new Vector2(.20f, bottom), new Vector2(.33f, bottom + .32f), label, 24, UiFactory.Dim);
            var bar = UiFactory.MakeBar(label + "Bar", row, new Vector2(.35f, bottom + .07f), new Vector2(.96f, bottom + .24f), Color.black, color);
            bar.SetPct(maximum > 0 ? value / (float)maximum : 0f);
            RewardText(label + "Value", row, new Vector2(.35f, bottom), new Vector2(.96f, bottom + .32f),
                value.ToString("N0") + " / " + maximum.ToString("N0"), 28, UiFactory.PaperText, TextAnchor.MiddleCenter);
        }

        private static void RewardValue(Transform parent, float left, float right, string title, string value, Color color)
        {
            var band = UiFactory.Panel("RewardValue", parent, new Vector2(left, .56f), new Vector2(right, .65f), HudInk);
            RewardText("Label", band.transform, new Vector2(.06f, .57f), new Vector2(.94f, .97f), title, 24, UiFactory.Dim);
            RewardText("Value", band.transform, new Vector2(.06f, .02f), new Vector2(.94f, .60f), value, 44, color, TextAnchor.MiddleRight);
        }
    }
}
