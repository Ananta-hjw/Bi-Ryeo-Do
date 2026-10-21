using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class RangedAttackTests
    {
        private static CombatStats Stats(int speed)
        {
            return new CombatStats { MaxHp = 100000, MaxQi = 1000, Attack = 10, Defense = 10, Speed = speed, CritDamage = 1.5f };
        }

        [TestCase("bido_throw", true)]
        [TestCase("bi_roe_do", true)]
        [TestCase("goengroe", true)]
        [TestCase("seomroe", false)]
        [TestCase("isj_iljeom", false)]
        public void CatalogAndMasteryPreserveExplicitAttackPresentationInsteadOfInferringItFromReach(string id, bool ranged)
        {
            var art = EarlyGameContent.Arts.First(a => a.Id == id);
            Assert.AreEqual(ranged, art.ToSpec().Ranged);
            Assert.AreEqual(ranged, Mastery.Apply(new SaveData(), art).Ranged);
            Assert.IsFalse(SkillSpec.BasicAttack().Ranged);
        }

        [TestCase("trap_arrow", "화살 연사")]
        [TestCase("trap_arrow", "화살")]
        [TestCase("trap_needle", "비침")]
        [TestCase("trap_needle", "비침 난사")]
        [TestCase("hyeol_gung", "혈마전")]
        [TestCase("hyeol_gung", "혈야구성")]
        [TestCase("mu_jeong", "무언독 암기")]
        [TestCase("jongri_chu_night", "일곱 창 투척")]
        public void ProjectileEnemyMovesAreExplicitlyRanged(string enemyId, string moveName)
        {
            var enemy = EarlyGameContent.Enemies.First(e => e.Id == enemyId);
            Assert.IsTrue(enemy.Moves.First(m => m.Name == moveName).Ranged);
        }

        [Test]
        public void RangedFlagDoesNotChangeSnipingOrTurnMeleeAmbushesIntoProjectiles()
        {
            var ranged = EnemyMove.Attack("shot", 1f).FromDistance();
            Assert.IsTrue(ranged.Ranged);
            Assert.IsFalse(ranged.Snipe);
            var ambush = EnemyMove.Attack("암습", 1f).Sniping();
            Assert.IsTrue(ambush.Snipe);
            Assert.IsFalse(ambush.Ranged);
            foreach (var enemy in EarlyGameContent.Enemies)
                foreach (var move in enemy.Moves.Where(m => m.Name == "암습")) Assert.IsFalse(move.Ranged, enemy.Id);
        }

        private static List<BattleEvent> PlayExchange(bool ranged)
        {
            var skill = new SkillSpec { Id = "test", Name = "test", Power = 1f, Hits = 3, QiCost = 10, Ranged = ranged };
            var hero = new Combatant("p", "hero", Team.Player, Stats(100), new[] { skill });
            var enemy = new Combatant("e", "enemy", Team.Enemy, Stats(50), null)
            {
                Moves = new List<EnemyMove> { new EnemyMove { Name = "reply", Power = 1f, Hits = 2, Ranged = ranged } }
            };
            var battle = new InteractiveBattle(hero, new[] { enemy }, new SeededRandom(1));
            battle.Begin();
            enemy.Gauge = InteractiveBattle.GaugeThreshold;
            return battle.PlayerAct(PlayerCommand.UseSkill(skill, enemy));
        }

        [Test]
        public void EveryHitCarriesRangedMetadataWithoutChangingDamageResourcesOrTurnOrder()
        {
            var melee = PlayExchange(false);
            var ranged = PlayExchange(true);
            var hits = ranged.Where(e => e.Kind == EventKind.Attack).ToList();
            Assert.AreEqual(3, hits.Count(e => e.ActorId == "p"));
            Assert.AreEqual(2, hits.Count(e => e.ActorId == "e"));
            Assert.IsTrue(hits.All(e => e.Ranged));
            CollectionAssert.AreEqual(
                melee.Select(e => new { e.Kind, e.ActorId, e.TargetId, e.SkillId, e.Damage, e.IsCrit, e.HitIndex, e.HitCount, e.TargetHpAfter, e.ActorQiAfter, e.ActorSpiritAfter }),
                ranged.Select(e => new { e.Kind, e.ActorId, e.TargetId, e.SkillId, e.Damage, e.IsCrit, e.HitIndex, e.HitCount, e.TargetHpAfter, e.ActorQiAfter, e.ActorSpiritAfter }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AwakeningCopiesTheOriginalHeavyMovesPresentationIntoEveryHit(bool ranged)
        {
            var heavy = new EnemyMove { Name = "heavy", Power = 2f, Hits = 3, Ranged = ranged };
            var moves = new List<EnemyMove> { EnemyMove.Wind("wind"), heavy };
            var awakening = BossPhases.CreateAwakening(moves, 1);
            Assert.AreEqual(ranged, awakening.Ranged);
            Assert.AreEqual(3, awakening.Hits);
            Assert.AreEqual(.7f, awakening.DamageFactor, .00001f);
            var hero = new Combatant("p", "hero", Team.Player, Stats(100), null);
            var enemy = new Combatant("e", "enemy", Team.Enemy, Stats(50), null)
            {
                Moves = moves, AwakeningStrike = awakening
            };
            var battle = new InteractiveBattle(hero, new[] { enemy }, new SeededRandom(1));
            battle.Begin();
            enemy.Gauge = InteractiveBattle.GaugeThreshold;
            var hits = battle.PlayerAct(PlayerCommand.Guard()).Where(e => e.Kind == EventKind.Attack && e.ActorId == "e").ToList();
            Assert.AreEqual(3, hits.Count);
            Assert.IsTrue(hits.All(e => e.Ranged == ranged && e.Awakening));
            Assert.AreEqual(ranged, heavy.Ranged, "creating an awakening must not mutate the source move");
        }
    }
}
