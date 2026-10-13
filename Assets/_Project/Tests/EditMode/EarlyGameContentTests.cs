using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class EarlyGameContentTests
    {
        [Test]
        public void StarterArtsAreFree()
        {
            var starter = EarlyGameContent.FindArt(EarlyGameContent.StarterArtId);
            Assert.IsNotNull(starter);
            Assert.AreEqual(0, starter.LearnCost);
            Assert.AreEqual(0, starter.QiCost);
        }

        [Test]
        public void IdsAreUnique()
        {
            Assert.AreEqual(EarlyGameContent.Arts.Length, EarlyGameContent.Arts.Select(a => a.Id).Distinct().Count());
            Assert.AreEqual(EarlyGameContent.Enemies.Length, EarlyGameContent.Enemies.Select(e => e.Id).Distinct().Count());
        }

        [Test]
        public void StagesReferenceExistingEnemiesAndEndWithABoss()
        {
            var numbers = new HashSet<int>();
            foreach (var s in EarlyGameContent.Stages)
            {
                Assert.IsTrue(numbers.Add(s.Number), "duplicate stage " + s.Number);
                Assert.IsTrue(s.Encounters.Length > 0);
                Assert.IsTrue(s.Intro.Length > 0 && s.Outro.Length > 0);
                for (int i = 0; i < s.Encounters.Length; i++)
                {
                    var enc = s.Encounters[i];
                    Assert.IsTrue(enc.Length >= 1 && enc.Length <= 3, s.Name);
                    bool hasBoss = false;
                    foreach (var id in enc)
                    {
                        var e = EarlyGameContent.FindEnemy(id);
                        Assert.IsNotNull(e, id);
                        if (e.IsBoss) hasBoss = true;
                    }
                    Assert.AreEqual(i == s.Encounters.Length - 1, hasBoss, s.Name + " encounter " + i);
                }
            }
        }

        [Test]
        public void EnemyScriptsAlwaysFollowAChargeWithAnAttack()
        {
            foreach (var e in EarlyGameContent.Enemies)
            {
                Assert.IsNotNull(e.Moves, e.Id);
                Assert.IsTrue(e.Moves.Any(m => !m.Charge), e.Id + " never attacks");
                for (int i = 0; i < e.Moves.Length; i++)
                    if (e.Moves[i].Charge) Assert.IsFalse(e.Moves[(i + 1) % e.Moves.Length].Charge, e.Id + " charges twice in a row");
            }
        }

        [Test]
        public void StageRewardsPayExactlyTheNextStagesLevel()
        {
            foreach (var s in EarlyGameContent.Stages.Take(EarlyGameContent.Stages.Length - 1))
            {
                var save = new SaveData { Level = s.RecommendedLevel };
                foreach (var enc in s.Encounters) foreach (var id in enc) Progression.AddExp(save, EarlyGameContent.ExpReward(s, id));
                Assert.GreaterOrEqual(save.Level, EarlyGameContent.FindStage(s.Number + 1).RecommendedLevel, s.Name);
            }
        }

        [Test]
        public void ArtsUnlockWithinStageRange()
        {
            int max = EarlyGameContent.Stages.Max(s => s.Number);
            foreach (var a in EarlyGameContent.Arts) Assert.LessOrEqual(a.UnlockStage, max, a.Id);
        }

        [Test]
        public void EveryStageIsClearableByASimpleBotAtItsRecommendedLevel()
        {
            foreach (var st in EarlyGameContent.Stages)
            {
                int wins = 0;
                for (int seed = 1; seed <= 40; seed++)
                    if (SimpleBot.ClearStage(st, seed)) wins++;
                Assert.GreaterOrEqual(wins, 20, st.Name + " is too hard: " + wins + "/40");
            }
        }
    }
}
