using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class InnerArtTests
    {
        private static CombatStats Base()
        {
            return new CombatStats { MaxHp = 1000, MaxQi = 100, Attack = 100, Defense = 100, Speed = 100, CritRate = 0.05f, CritDamage = 1.5f };
        }

        [Test]
        public void IdsAreUniqueAndUnlockWithinStageRange()
        {
            Assert.AreEqual(InnerArts.All.Length, InnerArts.All.Select(a => a.Id).Distinct().Count());
            int max = EarlyGameContent.Stages.Max(s => s.Number);
            foreach (var a in InnerArts.All)
            {
                Assert.LessOrEqual(a.UnlockStage, max, a.Id);
                Assert.IsTrue(InnerArts.EffectText(a).Length > 0, a.Id);
            }
            Assert.AreEqual(0, InnerArts.Find("breath").LearnCost);
        }

        [Test]
        public void LearningNeedsStageAndDoo()
        {
            var save = new SaveData { ClearedStage = 0, Doo = 100000 };
            string err;
            Assert.IsFalse(InnerArts.Learn(save, "tiger", out err));   // stage 5 not reached
            save.ClearedStage = 5;
            save.Doo = 10;
            Assert.IsFalse(InnerArts.Learn(save, "tiger", out err));   // too poor
            save.Doo = 5000;
            Assert.IsTrue(InnerArts.Learn(save, "tiger", out err));
            Assert.AreEqual(5000 - InnerArts.Find("tiger").LearnCost, save.Doo);
            Assert.IsFalse(InnerArts.Learn(save, "tiger", out err));   // already learned
        }

        [Test]
        public void SlotsGrowWithProgressAndLimitEquipping()
        {
            var save = new SaveData { ClearedStage = 5, Doo = 100000 };
            string err;
            Assert.AreEqual(1, InnerArts.SlotCount(save));
            InnerArts.Learn(save, "breath", out err);
            InnerArts.Learn(save, "vigor", out err);
            Assert.IsTrue(InnerArts.Equip(save, "breath", out err));
            Assert.IsFalse(InnerArts.Equip(save, "vigor", out err));
            save.ClearedStage = 10;
            Assert.AreEqual(2, InnerArts.SlotCount(save));
            Assert.IsTrue(InnerArts.Equip(save, "vigor", out err));
            save.ClearedStage = 20;
            Assert.AreEqual(3, InnerArts.SlotCount(save));
            InnerArts.Unequip(save, "vigor");
            Assert.AreEqual(1, save.EquippedInner.Count);
            Assert.IsFalse(InnerArts.Equip(save, "tiger", out err));   // not learned
        }

        [Test]
        public void StatsAreTradedAndStack()
        {
            var tiger = InnerArts.Apply(Base(), new[] { "tiger" });
            Assert.AreEqual(110, tiger.Attack);
            Assert.AreEqual(96, tiger.Defense);
            var fury = InnerArts.Apply(Base(), new[] { "fury" });
            Assert.AreEqual(920, fury.MaxHp);
            Assert.AreEqual(1.9f, fury.CritDamage, 0.001f);
            Assert.AreEqual(0.09f, fury.CritRate, 0.001f);
            var both = InnerArts.Apply(Base(), new[] { "tiger", "vigor" });
            Assert.AreEqual(110, both.Attack);
            Assert.AreEqual(1100, both.MaxHp);
            Assert.AreEqual(100, both.Defense);   // -4% + 4%
            Assert.AreEqual(Base().Attack, InnerArts.Apply(Base(), new string[0]).Attack);
        }

        [Test]
        public void PerksReachTheCombatant()
        {
            var hero = new Combatant("player", "비류연", Team.Player, Base(), null);
            InnerArts.ApplyPerks(hero, new[] { "spirit", "life", "breath" });
            Assert.AreEqual(4, hero.SpiritDealtBonus);
            Assert.AreEqual(3, hero.SpiritTakenBonus);
            Assert.AreEqual(20, hero.StartSpirit);
            Assert.AreEqual(0.08f, hero.Lifesteal, 0.0001f);
            Assert.AreEqual(2, hero.QiRegenBonus);
        }

        [Test]
        public void StartSpiritAndLifestealWorkInBattle()
        {
            var stats = new CombatStats { MaxHp = 500, MaxQi = 100, Attack = 100, Defense = 10, Speed = 200, CritRate = 0f, CritDamage = 1.5f };
            var foeStats = new CombatStats { MaxHp = 2000, MaxQi = 0, Attack = 1, Defense = 0, Speed = 1, CritRate = 0f, CritDamage = 1.5f };
            var hero = new Combatant("player", "비류연", Team.Player, stats, null) { StartSpirit = 30, Lifesteal = 0.5f, SpiritDealtBonus = 5 };
            var foe = new Combatant("foe", "적", Team.Enemy, foeStats, null);
            var battle = new InteractiveBattle(new[] { hero }, new[] { foe }, new SeededRandom(1));
            battle.Begin();
            Assert.AreEqual(30, hero.Spirit);
            hero.Hp = 100;
            battle.PlayerAct(PlayerCommand.UseSkill(hero.Skills[0], foe));
            Assert.Greater(hero.Hp, 100);
            Assert.GreaterOrEqual(hero.Spirit, 30 + InteractiveBattle.SpiritOnDealt + 5);
        }

        [Test]
        public void StatusSheetIncludesInnerArts()
        {
            var save = new SaveData { Level = 10, ClearedStage = 5, Doo = 100000 };
            string err;
            int before = int.Parse(StatusSheet.Hero(save)[2].Value.Replace(",", ""));
            InnerArts.Learn(save, "tiger", out err);
            InnerArts.Equip(save, "tiger", out err);
            int after = int.Parse(StatusSheet.Hero(save)[2].Value.Replace(",", ""));
            Assert.Greater(after, before);
            Assert.AreEqual(after, InnerArts.HeroStats(save).Attack);
            StringAssert.Contains("맹호심법", StatusSheet.InnerLine(save));
        }
    }
}
