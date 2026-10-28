using System;
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
                if (s.Prologue != null)
                    foreach (var sc in s.Prologue) Assert.IsTrue(!string.IsNullOrEmpty(sc.Title) && sc.Lines.Length > 0, s.Name);
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
        public void CompanionsHaveArtsAndJoinAtExistingStages()
        {
            var stages = EarlyGameContent.Stages.Select(s => s.Number).ToList();
            foreach (var c in EarlyGameContent.Companions)
            {
                CollectionAssert.Contains(stages, c.JoinStage, c.Id);
                var arts = EarlyGameContent.Arts.Where(a => a.OwnerId == c.Id).ToList();
                Assert.GreaterOrEqual(arts.Count, 2, c.Id);
                var unit = c.CreateCombatant(100);   // high enough for every level-gated art
                Assert.AreEqual(Team.Player, unit.Team);
                var known = unit.Skills.Where(s => s.Id != "basic").ToList();
                // at most five ordinary arts (four buttons + the 합공 button a companion never uses) and one 기세 100 필살기
                Assert.LessOrEqual(known.Count(s => s.SpiritCost == 0), Progression.MaxEquippedArts + 1, c.Id);
                Assert.AreEqual(1, known.Count(s => s.SpiritCost > 0), c.Id);
                // a replaced art is gone, and every art names a real older art of the same companion
                foreach (var a in arts.Where(a => a.Replaces != null))
                {
                    Assert.IsTrue(arts.Any(o => o.Id == a.Replaces && o.OwnerLevel < a.OwnerLevel), a.Id);
                    Assert.IsFalse(known.Any(s => s.Id == a.Replaces), a.Replaces);
                }
                Assert.IsTrue(unit.Skills.Any(s => s.Id == "basic"), "basic attack plus arts");
            }
        }

        [Test]
        public void PartyGrowsAsCompanionsJoin()
        {
            Assert.AreEqual(0, EarlyGameContent.CompanionsFor(1).Count);
            Assert.AreEqual(2, EarlyGameContent.CompanionsFor(7).Count);
            Assert.AreEqual(3, EarlyGameContent.CompanionsFor(8).Count);
        }

        [Test]
        public void CompanionSelectionCapsAtThreeAndHonorsPicks()
        {
            // Before 나예린 joins, everyone fights.
            Assert.AreEqual(3, EarlyGameContent.SelectCompanions(15, null).Count);
            Assert.AreEqual(5, EarlyGameContent.CompanionsFor(30).Count);
            // After, only three fit: default is join order.
            var def = EarlyGameContent.SelectCompanions(25, null).Select(c => c.Id).ToList();
            CollectionAssert.AreEqual(new[] { "hyo_ryong", "jang_hong", "yun_junho" }, def);
            // Picks win, in order, and unknown or not-yet-joined ids are ignored.
            var picked = EarlyGameContent.SelectCompanions(25, new[] { "na_yerin", "nobody", "im_seongjin" }).Select(c => c.Id).ToList();
            CollectionAssert.AreEqual(new[] { "na_yerin", "im_seongjin", "hyo_ryong" }, picked);
            Assert.AreEqual(0, EarlyGameContent.SelectCompanions(1, new[] { "na_yerin" }).Count);
            Assert.AreEqual(EarlyGameContent.MaxCompanions, EarlyGameContent.SelectCompanions(30, new[] { "a", "b", "c", "d" }).Count);
        }

        [Test]
        public void ProtagonistArtsExcludeCompanionArts()
        {
            // Healing stays with 윤준호; binding arts (점혈-like Stun) appear for the protagonist only from the 극악 ladder (26권 이후).
            var hero = EarlyGameContent.Arts.Where(a => a.OwnerId == null).ToList();
            Assert.IsTrue(hero.All(a => a.Effect != SkillEffect.HealAlly && a.Effect != SkillEffect.Cleanse));
            Assert.IsTrue(hero.Where(a => a.Effect == SkillEffect.Stun).All(a => a.UnlockDiff == 2));
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
        public void StageRewardsKeepPaceWithTheRecommendedLevels()
        {
            // clearing every stage once, in order: each stage pays, the player arrives within a level of its (rounded) recommendation, and stage 30 ends one above its own
            var save = new SaveData { Level = 1 };
            foreach (var s in EarlyGameContent.Stages)
            {
                Assert.GreaterOrEqual(save.Level, s.RecommendedPlayerLevel - 1, s.Name);
                Assert.GreaterOrEqual(save.Level, (int)Math.Floor(EarlyGameContent.ExactPlayerLevel(s)), s.Name);
                long exp = 0;
                foreach (var enc in s.Encounters) foreach (var id in enc) exp += EarlyGameContent.ExpReward(s, id);
                Assert.Greater(exp, 0, s.Name + " pays no exp");
                Progression.AddExp(save, exp);
            }
            Assert.AreEqual(EarlyGameContent.Stages.Last().RecommendedPlayerLevel + 1, save.Level);
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
                int minimum = st.Number == 5 ? 28 : 34;
                Assert.GreaterOrEqual(wins, minimum, st.Name + " is too hard for the start-kit build: " + wins + "/40");
            }
        }

        [Test]
        public void BenchedCompanionsAreNotAutoFilled()
        {
            var save = new SaveData();
            // Everyone who has joined by stage 8 fits in the party (3 slots), so without a bench list nobody can be left out.
            Assert.AreEqual(3, EarlyGameContent.SelectCompanionsFor(8, save).Count);
            save.BenchedCompanions.Add("jang_hong");
            var picked = EarlyGameContent.SelectCompanionsFor(8, save);
            Assert.AreEqual(2, picked.Count);
            Assert.IsFalse(picked.Any(c => c.Id == "jang_hong"));
            // Benching everyone leaves the protagonist alone.
            save.BenchedCompanions.AddRange(new[] { "hyo_ryong", "yun_junho" });
            Assert.AreEqual(0, EarlyGameContent.SelectCompanionsFor(8, save).Count);
            // A newly joined companion is not benched, so he fills a free slot.
            save.BenchedCompanions.Clear();
            save.BenchedCompanions.AddRange(new[] { "hyo_ryong", "jang_hong" });
            var late = EarlyGameContent.SelectCompanionsFor(22, save).Select(c => c.Id).ToList();
            CollectionAssert.AreEqual(new[] { "yun_junho", "na_yerin", "im_seongjin" }, late);
        }
    }
}
