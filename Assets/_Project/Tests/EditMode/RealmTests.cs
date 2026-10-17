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
        public void StrongestEnemySoFarIsTheFirstStepOfChojeoljeong()
        {
            var boss = EarlyGameContent.FindEnemy("bi_daegongja").CreateCombatant("b");
            Assert.AreEqual(Realm.AnchorStep, Realm.StepOf(Realm.Power(boss.Stats)));
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
            // Trial opponents (검후 and the 은가면, who are 천무삼성) stand above him on purpose.
            foreach (var e in EarlyGameContent.Enemies)
            {
                if (e == bi || e.HpMul > 20f || e.Id.StartsWith("mask_") || !Realm.HasRealm(e)) continue;
                Assert.Greater(biPower, Realm.Power(e.CreateCombatant(e.Id).Stats), e.Id);
            }
            Assert.AreEqual("초절정 초입", Realm.ForEnemy(bi));
            Assert.AreEqual("", Realm.ForEnemy(EarlyGameContent.FindEnemy("tiger")));
            foreach (var id in new[] { "tiger", "white_tiger", "mukrin_hyeolmang", "cave_viper" }) Assert.IsNotNull(EarlyGameContent.FindEnemy(id), id);
        }
    }
}
