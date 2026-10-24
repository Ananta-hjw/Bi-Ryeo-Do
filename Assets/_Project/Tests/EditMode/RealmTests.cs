using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BiRyeoDo.Core;

namespace BiRyeoDo.Tests
{
    public class RealmTests
    {
        [Test]
        public void LadderHasTheRequestedNamesAndSubSteps()
        {
            var names = Realm.Steps.Select(s => s.Name).ToList();
            CollectionAssert.AreEqual(new[] { "삼급", "이급", "일급", "초일급" }, names.Take(4));   // no sub steps below 상급
            Assert.AreEqual("절정 초입", names[4]);
            Assert.AreEqual("절정 완숙", names[5]);
            Assert.AreEqual("절정 극", names[6]);
            Assert.AreEqual("초절정 초입", names[Realm.AnchorStep]);
            Assert.AreEqual(7, Realm.AnchorStep);
            foreach (var realm in new[] { "화경", "현경", "생사경", "자연경", "공허경", "여의경" })
                Assert.AreEqual(3, Realm.Steps.Count(s => s.Realm == realm), realm);
            Assert.AreEqual("최상급", Realm.Steps.First(s => s.Realm == "생사경").Group);
            Assert.AreEqual("초월급", Realm.Steps.First(s => s.Realm == "공허경").Group);
            Assert.AreEqual("신급", Realm.Steps.Last().Group);
            Assert.AreEqual("여의경 극", Realm.Steps.Last().Name);
        }

        [Test]
        public void DaegongjaBiOnHardIsTheFirstStepOfChojeoljeong()
        {
            // The ladder was anchored on him (어려움 stage 30 = 초절정 초입); after the 어려움 상향 (HardExtra) he stands at or above it.
            var hard = StageStars.ScaleEnemy(EarlyGameContent.FindEnemy("bi_daegongja"), 30, 1).CreateCombatant("b");
            Assert.GreaterOrEqual(hard.RealmStep, Realm.AnchorStep);
            var normal = EarlyGameContent.FindEnemy("bi_daegongja").CreateCombatant("b");
            Assert.Less(normal.RealmStep, Realm.AnchorStep);   // on 일반 he is a step below
        }

        [Test]
        public void EarlyStagesStayInTheLowRealms()
        {
            // Stage 5 (level 13) is still 하급·중급 and stage 30 (level 65) has not reached 초절정.
            Assert.LessOrEqual(Realm.StepOf(Realm.Power(Progression.HeroStatsAtCurve(13))), 1);
            Assert.Less(Realm.StepOf(Realm.Power(Progression.HeroStatsAtCurve(65))), Realm.AnchorStep);
        }

        [Test]
        public void RisingIsReportedOnceAndNeverForFalls()
        {
            var save = new SaveData { Level = 1 };
            int from, to;
            Assert.IsFalse(Realm.PollRise(save, out from, out to));   // first call only records
            Assert.AreEqual(Realm.StepOf(save), save.RealmMax);
            save.Level = 80;
            Assert.IsTrue(Realm.PollRise(save, out from, out to));
            Assert.Greater(to, from);
            Assert.IsFalse(Realm.PollRise(save, out from, out to));   // told once
            save.Level = 10;                                          // a fall (e.g. gear off) is silent
            Assert.IsFalse(Realm.PollRise(save, out from, out to));
        }

        [Test]
        public void StepsAreMonotonicAndLevelOneStartsAtSamgeup()
        {
            Assert.AreEqual(0, Realm.StepOf(Realm.Power(Progression.HeroStatsFor(1))));
            int prev = -1;
            for (int lv = 1; lv <= 200; lv++)
            {
                int step = Realm.StepOf(Realm.Power(Progression.HeroStatsFor(lv)));
                Assert.GreaterOrEqual(step, prev);
                prev = step;
            }
            Assert.Greater(prev, 4);   // a high level hero reaches 절정
            for (int i = 1; i < Realm.Steps.Count; i++) Assert.Greater(Realm.LowerBound(i), Realm.LowerBound(i - 1));
        }

        [Test]
        public void GearRaisesTheRealm()
        {
            var save = new SaveData { Level = 40 };
            double plain = Realm.Power(InnerArts.HeroStats(save));
            save.EquippedGear.Add("jin_biroedo");
            Assert.Greater(Realm.Power(InnerArts.HeroStats(save)), plain);
            StringAssert.Contains("종합 수치", Realm.Describe(save));
        }

        [Test]
        public void DaegongjaBiIsTheStrongestFightingEnemyAndBeastsHaveNoRealm()
        {
            var bi = EarlyGameContent.FindEnemy("bi_daegongja");
            double biPower = Realm.Power(bi.CreateCombatant("b").Stats);
            // Trial opponents (검후 and the 은가면, who are 천무삼성) stand above him on purpose. He tops 1부 (stages 1-30); 2부 enemies may stand higher.
            var partTwo = new HashSet<string>(EarlyGameContent.Stages.Where(st => st.Number > StageStars.LevelLineStage).SelectMany(st => st.Encounters.SelectMany(x => x)));
            foreach (var e in EarlyGameContent.Enemies)
            {
                if (e == bi || e.HpMul > 20f || e.Id.StartsWith("mask_") || !Realm.HasRealm(e) || partTwo.Contains(e.Id)) continue;
                Assert.Greater(biPower, Realm.Power(e.CreateCombatant(e.Id).Stats), e.Id);
            }
            Assert.AreEqual("절정 완숙", Realm.ForEnemy(bi));   // 초절정 초입 only on 어려움 (일반 stages 7+ were eased)
            Assert.AreEqual("", Realm.ForEnemy(EarlyGameContent.FindEnemy("tiger")));
            foreach (var id in new[] { "tiger", "white_tiger", "mukrin_hyeolmang", "cave_viper" }) Assert.IsNotNull(EarlyGameContent.FindEnemy(id), id);
        }

        [Test]
        public void EachRealmStepRaisesEveryStatByFivePercent()
        {
            var save = new SaveData { Level = 65 };
            var before = InnerArts.HeroStatsBeforeRealm(save);
            int step = Realm.StepOf(Realm.Power(before));
            Assert.Greater(step, 0);
            var after = InnerArts.HeroStats(save);
            float m = 1f + 0.05f * step;
            Assert.AreEqual((int)System.Math.Round(before.MaxHp * m), after.MaxHp);
            Assert.AreEqual((int)System.Math.Round(before.Attack * m), after.Attack);
            Assert.AreEqual((int)System.Math.Round(before.Defense * m), after.Defense);
            Assert.AreEqual((int)System.Math.Round(before.MaxQi * m), after.MaxQi);
            Assert.AreEqual((int)System.Math.Round(before.Speed * m), after.Speed);
            Assert.AreEqual(step, Realm.StepOf(save));   // the realm is read before the bonus, so it does not feed on itself
            var fresh = new SaveData { Level = 1 };
            Assert.AreEqual(InnerArts.HeroStatsBeforeRealm(fresh).Attack, InnerArts.HeroStats(fresh).Attack);   // 삼급 has no bonus
        }

        [Test]
        public void ExtremeSwordQueenKeepsItsPreBonusRealmAndBoostsItsDefense()
        {
            var queen = EarlyGameContent.FindEnemy("geomhu_ioksang");
            var normal = StageStars.ScaleEnemy(queen, 29, 0).CreateCombatant("q").Stats;
            var hard = StageStars.ScaleEnemy(queen, 29, 1).CreateCombatant("q").Stats;
            var data = StageStars.ScaleEnemy(queen, 29, 2);
            var before = data.BuildStatsBeforeRealm();
            var unit = data.CreateCombatant("q");
            var extreme = unit.Stats;
            Assert.AreEqual(0.4, BattleEngine.DefenseConstant / (BattleEngine.DefenseConstant + before.EffDefense), 0.002);   // 극악: x0.4 (defense about 500 on the current scale)
            Assert.AreEqual((int)System.Math.Round(before.Defense * (1f + Realm.Bonus(unit.RealmStep))), extreme.Defense);
            Assert.Less(BattleEngine.DefenseConstant / (BattleEngine.DefenseConstant + extreme.EffDefense), .4);
            Assert.Greater(extreme.Defense, hard.Defense);
            Assert.Greater(hard.Defense, normal.Defense);
            Assert.AreEqual("초절정 극", Realm.Label(unit.RealmStep));
            Assert.Greater(unit.RealmPowerBeforeBonus, 2200.0);   // not pinned to one number any more: difficulty rises, the ladder stays
            Assert.Less(unit.RealmPowerBeforeBonus, 3000.0);
        }

        [Test]
        public void CompanionsHaveARealmLabel()
        {
            foreach (var c in EarlyGameContent.Companions)
            {
                var text = StatusSheet.AllyRealm(c, 20);
                StringAssert.Contains("종합", text);
                Assert.IsTrue(text.Length > 6, text);
            }
        }
    }
}
