using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class PartyGuardTests
    {
        private static CombatStats Stats(int speed, int hp = 100000)
        {
            return new CombatStats { MaxHp = hp, MaxQi = 200, Attack = 5, Defense = 0, Speed = speed, CritRate = 0f, CritDamage = 1.5f };
        }

        [Test]
        public void PartyGuardCoversEveryoneEvenAfterTheyActBeforeTheEnemyHits()
        {
            var guardSkill = new SkillSpec { Id = "wall", Name = "벽", Effect = SkillEffect.GuardParty, QiCost = 10, Cooldown = 4 };
            var caster = new Combatant("hyo_ryong", "효룡", Team.Player, Stats(300), new[] { guardSkill });
            var hero = new Combatant("player", "비류연", Team.Player, Stats(200), null);
            var ally = new Combatant("jang_hong", "장홍", Team.Player, Stats(150), null);
            var foe = new Combatant("foe", "적", Team.Enemy, Stats(40, 999999), null)
            {
                Moves = new List<EnemyMove> { EnemyMove.Attack("강타", 1f) }
            };
            var foeStats = foe.Stats;
            var battle = new InteractiveBattle(new[] { hero, caster, ally }, new[] { foe }, new SeededRandom(2));
            battle.Begin();
            Assert.AreSame(caster, battle.Current);
            var events = battle.PlayerAct(PlayerCommand.UseSkill(guardSkill, null));
            foreach (var u in new[] { hero, caster, ally }) Assert.IsTrue(battle.IsGuarded(u), u.Name + " should be guarded");

            // The others act (which used to clear their own guard) ...
            int attacksSeen = 0;
            for (int i = 0; i < 12 && battle.Current != caster && !battle.IsOver; i++)
            {
                Assert.IsTrue(battle.IsGuarded(battle.Current), "guard must survive " + battle.Current.Name + "'s own turn");
                var evs = battle.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), foe));
                foreach (var e in evs.Where(x => x.Kind == EventKind.Attack && x.ActorId == "foe" && !x.Dodged)) { attacksSeen++; Assert.IsTrue(e.Guarded, "enemy hit on " + e.TargetId + " was not guarded"); }
            }
            Assert.IsNotNull(events);
            Assert.GreaterOrEqual(attacksSeen, 0);
        }

        [Test]
        public void GuardEndsWhenTheCasterActsAgain()
        {
            var guardSkill = new SkillSpec { Id = "wall", Name = "벽", Effect = SkillEffect.GuardParty, QiCost = 10, Cooldown = 0 };
            var caster = new Combatant("hyo_ryong", "효룡", Team.Player, Stats(130), new[] { guardSkill });
            var hero = new Combatant("player", "비류연", Team.Player, Stats(100), null);
            var foe = new Combatant("foe", "적", Team.Enemy, Stats(10, 999999), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("강타", 1f) } };
            var battle = new InteractiveBattle(new[] { hero, caster }, new[] { foe }, new SeededRandom(1));
            battle.Begin();
            Assert.AreSame(caster, battle.Current);
            battle.PlayerAct(PlayerCommand.UseSkill(guardSkill, null));
            Assert.IsTrue(battle.IsGuarded(hero));
            // Let turns pass until the caster is up again; then the guard is gone.
            for (int i = 0; i < 10 && battle.Current != caster; i++) battle.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), foe));
            Assert.AreSame(caster, battle.Current);
            Assert.IsFalse(battle.IsGuarded(hero));
            Assert.IsFalse(battle.IsGuarded(caster));
        }

        [Test]
        public void ADeadCasterEndsTheGuardOnceAnAllyActs()
        {
            var guardSkill = new SkillSpec { Id = "wall", Name = "벽", Effect = SkillEffect.GuardParty, QiCost = 10, Cooldown = 0 };
            var caster = new Combatant("hyo_ryong", "효룡", Team.Player, Stats(130), new[] { guardSkill });
            var hero = new Combatant("player", "비류연", Team.Player, Stats(100), null);
            var foe = new Combatant("foe", "적", Team.Enemy, Stats(10, 999999), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("강타", 1f) } };
            var battle = new InteractiveBattle(new[] { hero, caster }, new[] { foe }, new SeededRandom(1));
            battle.Begin();
            battle.PlayerAct(PlayerCommand.UseSkill(guardSkill, null));
            caster.Hp = 0;
            // Still covered until an ally takes a turn (a hit in the same enemy phase is guarded) ...
            Assert.IsTrue(battle.IsGuarded(hero));
            // ... then it ends.
            for (int i = 0; i < 6 && !battle.IsOver; i++)
            {
                if (battle.Current == hero) { battle.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), foe)); break; }
                battle.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), foe));
            }
            Assert.IsFalse(battle.IsGuarded(hero) && !hero.IsAlive);
        }

        [Test]
        public void TheGuardOutlivesACasterWhoFallsInTheSameEnemyPhase()
        {
            var guardSkill = new SkillSpec { Id = "wall", Name = "벽", Effect = SkillEffect.GuardParty, QiCost = 10, Cooldown = 0 };
            var caster = new Combatant("hyo_ryong", "효룡", Team.Player, Stats(130, 5), new[] { guardSkill });
            var hero = new Combatant("player", "비류연", Team.Player, Stats(100), null);
            var foe = new Combatant("foe", "적", Team.Enemy, Stats(10, 999999), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("강타", 1f) } };
            var battle = new InteractiveBattle(new[] { hero, caster }, new[] { foe }, new SeededRandom(1));
            battle.Begin();
            battle.PlayerAct(PlayerCommand.UseSkill(guardSkill, null));
            caster.Hp = 0;                                   // the caster falls
            Assert.IsTrue(battle.IsGuarded(hero));           // the others are still covered for now
        }
    }
}
