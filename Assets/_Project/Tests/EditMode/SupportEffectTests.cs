using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class SupportEffectTests
    {
        private static Combatant Ally(string id, int hp = 1000)
        {
            return new Combatant(id, id, Team.Player, new CombatStats { MaxHp = hp, MaxQi = 500, Attack = 100, Defense = 50, Speed = 100, CritRate = 0f, CritDamage = 1.5f },
                new List<SkillSpec>());
        }

        private static Combatant Foe(int attack = 300, float power = 1f, int speed = 60)
        {
            var e = new Combatant("foe", "foe", Team.Enemy, new CombatStats { MaxHp = 100000, MaxQi = 0, Attack = attack, Defense = 0, Speed = speed, CritRate = 0f, CritDamage = 1.5f }, null);
            e.Moves = new List<EnemyMove> { EnemyMove.Attack("일격", power) };
            return e;
        }

        private static SkillSpec Art(SkillEffect effect, float heal = 0f) { return new SkillSpec { Id = "s_" + effect, Name = effect.ToString(), Effect = effect, HealPct = heal }; }

        /// <summary>Lets the party act with <paramref name="first"/> doing <paramref name="opening"/>, everyone else guarding, until the foe has acted once.</summary>
        private static List<BattleEvent> Round(InteractiveBattle b, Combatant first, PlayerCommand opening)
        {
            var events = b.Begin();
            bool done = false;
            for (int guard = 0; guard < 20 && !b.IsOver; guard++)
            {
                var cmd = !done && b.Current == first ? opening : PlayerCommand.Guard();
                if (b.Current == first) done = true;
                int before = b.EnemyActions;
                events.AddRange(b.PlayerAct(cmd));
                if (done && b.EnemyActions > before) break;
            }
            return events;
        }

        [Test]
        public void EachCompanionOwnsADifferentSupportEffect()
        {
            var owners = new Dictionary<SkillEffect, HashSet<string>>();
            foreach (var art in EarlyGameContent.Arts.Where(a => a.OwnerId != null && SupportEffects.IsSupport(a.Effect)))
            {
                if (!owners.ContainsKey(art.Effect)) owners[art.Effect] = new HashSet<string>();
                owners[art.Effect].Add(art.OwnerId);
            }
            foreach (var kv in owners) Assert.AreEqual(1, kv.Value.Count, kv.Key + " is shared by " + string.Join(",", kv.Value));
            Assert.AreEqual("hyo_ryong", owners[SkillEffect.GuardParty].Single());
            Assert.AreEqual("jang_hong", owners[SkillEffect.Stealth].Single());
            Assert.AreEqual("yun_junho", owners[SkillEffect.HealParty].Single());
            Assert.AreEqual("im_seongjin", owners[SkillEffect.Cover].Single());
            Assert.AreEqual("mo_yonghwi", owners[SkillEffect.Focus].Single());
            Assert.AreEqual(SkillEffect.None, EarlyGameContent.FindCompanion("na_yerin").JointEffect, "백혼합벽 chills instead of stunning (장홍 keeps 점혈)");
            Assert.AreEqual(StatusKind.Chill, EarlyGameContent.FindCompanion("na_yerin").JointInflict);
            Assert.AreEqual(StatusKind.Bleed, EarlyGameContent.Arts.Single(a => a.Id == "xr_ilgyeokbung").Inflict, "내상 belongs to 임성진");
            foreach (var c in EarlyGameContent.Companions)
                Assert.LessOrEqual(c.CreateCombatant(70).Skills.Count(s => s.Id != "basic" && s.SpiritCost <= 0), Progression.MaxEquippedArts + 1, c.Id);
        }

        [Test]
        public void PartyGuardCutsThirtyPercentForTheCastersNextThreeTurns()
        {
            var caster = Ally("guard"); var other = Ally("other");
            var b = new InteractiveBattle(new[] { caster, other }, new[] { Foe() }, new SeededRandom(1));
            b.Begin();
            while (b.Current != caster) b.PlayerAct(PlayerCommand.Meditate());
            b.PlayerAct(PlayerCommand.UseSkill(Art(SkillEffect.GuardParty), null));
            Assert.AreEqual(InteractiveBattle.PartyGuardDamageFactor, b.GuardFactorFor(other), 1e-6);
            int casterTurns = 0;
            for (int i = 0; i < 40 && casterTurns < 3; i++)
            {
                if (b.Current == caster) casterTurns++;
                if (casterTurns < 3) Assert.IsTrue(b.IsGuarded(other), "still up on the caster's turn " + casterTurns);
                b.PlayerAct(PlayerCommand.Meditate());
            }
            Assert.AreEqual(3, casterTurns);
            Assert.IsFalse(b.IsGuarded(other), "gone at the third turn");
            Assert.AreEqual(0.7f, InteractiveBattle.PartyGuardDamageFactor, 1e-6, "half the cut of a guard (x0.4)");
        }

        [Test]
        public void FocusAddsOneHundredThirtyPercentAndTwentyPercentTaken()
        {
            var a = Ally("focus");
            var foe = Foe();
            var b = new InteractiveBattle(new[] { a }, new[] { foe }, new SeededRandom(2));
            b.Begin();
            var plain = b.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), foe)).First(e => e.ActorId == "focus" && e.Kind == EventKind.Attack).Damage;
            var taken = b.PlayerAct(PlayerCommand.UseSkill(Art(SkillEffect.Focus), null)).Where(e => e.TargetId == "focus" && e.Kind == EventKind.Attack).Sum(e => e.Damage);
            Assert.IsTrue(a.Focused);
            var focused = b.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), foe)).First(e => e.ActorId == "focus" && e.Kind == EventKind.Attack).Damage;
            Assert.AreEqual(plain * InteractiveBattle.FocusDamageFactor, focused, plain * 0.02 + 1);
            Assert.IsFalse(a.Focused, "spent on the attack");
            var unfocused = Foe(); var c = Ally("plain");
            var b2 = new InteractiveBattle(new[] { c }, new[] { unfocused }, new SeededRandom(2));
            b2.Begin();
            b2.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), unfocused));
            int normalTaken = b2.PlayerAct(PlayerCommand.Meditate()).Where(e => e.TargetId == "plain" && e.Kind == EventKind.Attack).Sum(e => e.Damage);
            Assert.AreEqual(normalTaken * InteractiveBattle.FocusTakenFactor, taken, normalTaken * 0.03 + 1);
        }

        [Test]
        public void CoverStealthWardHealAndSpiritDoWhatTheySay()
        {
            var tank = Ally("tank"); var thief = Ally("thief"); var front = Ally("front");
            var b = new InteractiveBattle(new[] { front, tank, thief }, new[] { Foe() }, new SeededRandom(3));
            var events = Round(b, tank, PlayerCommand.UseSkill(Art(SkillEffect.Cover), null));
            var hits = events.Where(e => e.ActorId == "foe" && e.Kind == EventKind.Attack && e.Damage > 0).ToList();
            Assert.IsTrue(hits.Count > 0 && hits.Last().TargetId == "tank", "the blow comes to the covering member");
            Assert.AreEqual(0.7f, InteractiveBattle.CoverDamageFactor, 1e-6, "원의 곤: the covering member takes 30% less");

            var b2 = new InteractiveBattle(new[] { thief, Ally("mate") }, new[] { Foe(speed: 400) }, new SeededRandom(4));   // a quick foe acts before the thief comes round
            var ev2 = Round(b2, thief, PlayerCommand.UseSkill(Art(SkillEffect.Stealth), null));
            int cast = ev2.FindIndex(e => e.ActorId == "thief" && e.Tag == "은신");
            Assert.GreaterOrEqual(cast, 0);
            var after = ev2.Skip(cast + 1).Where(e => e.ActorId == "foe" && e.Kind == EventKind.Attack).ToList();
            Assert.IsTrue(after.Count > 0 && after.All(e => e.TargetId != "thief"), "hidden from the foe");
            Assert.IsTrue(thief.StealthCrit, "the next attack will be a sure critical");

            var healer = Ally("healer"); var hurt = Ally("hurt"); hurt.Hp = 500; healer.Hp = 600;
            var b3 = new InteractiveBattle(new[] { healer, hurt }, new[] { Foe(1) }, new SeededRandom(5));
            b3.Begin();
            while (b3.Current != healer) b3.PlayerAct(PlayerCommand.Guard());
            var heal = b3.PlayerAct(PlayerCommand.UseSkill(Art(SkillEffect.HealParty, 0.15f), null));
            Assert.AreEqual(2, heal.Count(e => e.Kind == EventKind.Heal && e.Heal == 150), "both healed by 15% of max HP");

            var leader = Ally("leader"); var mate = Ally("mate2");
            var b4 = new InteractiveBattle(new[] { leader, mate }, new[] { Foe(1) }, new SeededRandom(6));
            b4.Begin();
            while (b4.Current != leader) b4.PlayerAct(PlayerCommand.Guard());
            int before = mate.Spirit;
            b4.PlayerAct(PlayerCommand.UseSkill(Art(SkillEffect.PartySpirit), null));
            Assert.GreaterOrEqual(mate.Spirit, before + InteractiveBattle.PartySpiritGain);
            while (b4.Current != leader) b4.PlayerAct(PlayerCommand.Guard());
            b4.PlayerAct(PlayerCommand.UseSkill(Art(SkillEffect.Ward), null));
            Assert.AreEqual(InteractiveBattle.WardTurns, mate.WardTurns > 0 ? InteractiveBattle.WardTurns : 0);
        }

        [Test]
        public void CounterStanceStrikesBackAndChillCutsDamageByTenPercent()
        {
            var sang = Ally("sang");
            var foe = Foe();
            var b = new InteractiveBattle(new[] { sang }, new[] { foe }, new SeededRandom(7));
            var events = Round(b, sang, PlayerCommand.UseSkill(Art(SkillEffect.CounterStance), null));
            Assert.IsTrue(events.Any(e => e.ActorId == "sang" && e.TargetId == "foe" && e.SkillId == "counter_stance" && e.Damage > 0), "struck back");

            var cold = Ally("cold"); var warm = Ally("warm");
            var f1 = Foe(); var f2 = Foe();
            cold.AddStatus(StatusKind.Chill, 5);
            var b1 = new InteractiveBattle(new[] { cold }, new[] { f1 }, new SeededRandom(8)); b1.Begin();
            var b2 = new InteractiveBattle(new[] { warm }, new[] { f2 }, new SeededRandom(8)); b2.Begin();
            int c = b1.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), f1)).First(e => e.ActorId == "cold").Damage;
            int w = b2.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), f2)).First(e => e.ActorId == "warm").Damage;
            Assert.AreEqual(w * InteractiveBattle.ChillAttackFactor, c, 1.5);
            StringAssert.Contains("공격 -10%", Statuses.Describe(StatusKind.Chill));
        }

        [Test]
        public void SnakeFightsFastStarIsSeomroechangAtTheFirstOpenedMouth()
        {
            var stage = EarlyGameContent.FindStage(20);
            Assert.IsTrue(Showdowns.IsWindow(stage, 2));
            Assert.AreEqual(7, StageStars.Evaluate(stage, 2, 999, false, 0), "however long it took, the first chance taken is 신속");
            Assert.AreEqual(3, StageStars.Evaluate(stage, 2, 1, false, 1), "a missed mouth loses it");
            StringAssert.Contains("첫 기회에", StageStars.FastText(stage, 2, 10));
            Assert.AreEqual(StageStars.Evaluate(stage.Number, 0, 5, false, 0), StageStars.Evaluate(stage, 0, 5, false, 0), "other fights keep the action count");
        }
    }
}
