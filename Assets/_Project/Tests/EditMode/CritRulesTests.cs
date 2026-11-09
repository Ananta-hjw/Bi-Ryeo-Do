using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class CritRulesTests
    {
        [Test]
        public void CritUpToHalfCountsInFull()
        {
            Assert.AreEqual(.3f, CritRules.Chance(.3f), 1e-6);
            Assert.AreEqual(.5f, CritRules.Chance(.5f), 1e-6);
            Assert.AreEqual(0f, CritRules.ExtraDamage(.5f), 1e-6);
        }

        [Test]
        public void TheExcessSplitsIntoChanceAndCritDamage()
        {
            Assert.AreEqual(.6f, CritRules.Chance(.7f), 1e-6);
            Assert.AreEqual(.1f, CritRules.ExtraDamage(.7f), 1e-6);
            Assert.AreEqual(1f, CritRules.Chance(1.5f), 1e-6);
            Assert.AreEqual(.5f, CritRules.ExtraDamage(1.5f), 1e-6);
            // past 100% effective chance everything turns into crit damage
            Assert.AreEqual(1f, CritRules.Chance(1.8f), 1e-6);
            Assert.AreEqual(.8f, CritRules.ExtraDamage(1.8f), 1e-6);
            var s = new CombatStats { CritRate = .7f, CritDamage = 1.5f };
            Assert.AreEqual(1.6f, CritRules.Damage(s), 1e-6);
        }

        [Test]
        public void ConvertedCritRaisesCritHits()
        {
            var skill = new SkillSpec { Id = "x", Power = 1f, Hits = 1 };
            var target = new CombatStats { MaxHp = 1000, Defense = 0 };
            var low = new CombatStats { Attack = 3000, CritRate = .5f, CritDamage = 1.5f };
            var high = new CombatStats { Attack = 3000, CritRate = 1.0f, CritDamage = 1.5f };
            Assert.Less(BattleEngine.CalcDamage(low, target, skill, true), BattleEngine.CalcDamage(high, target, skill, true));
            Assert.AreEqual(BattleEngine.CalcDamage(low, target, skill, false), BattleEngine.CalcDamage(high, target, skill, false));
        }

        [Test]
        public void TheSheetShowsTheSumAndTheConversion()
        {
            var b = new CombatStats { CritRate = .05f, CritDamage = 1.5f };
            var f = new CombatStats { CritRate = .7f, CritDamage = 1.5f };
            var line = StatusSheet.CritLine(f, b);
            Assert.AreEqual("60%", line.Value);
            StringAssert.Contains("70", line.Note);
            StringAssert.Contains("10", StatusSheet.CritDamageLine(f).Note);
            Assert.IsNull(StatusSheet.CritLine(b, b).Note);
        }
    }
}
