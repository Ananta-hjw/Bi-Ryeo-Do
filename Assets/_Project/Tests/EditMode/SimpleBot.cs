using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;

namespace BiRyeoDo.Tests
{
    /// <summary>A reasonable (not perfect) player used to check that stages are winnable. Mirrors what a person would do.</summary>
    public static class SimpleBot
    {
        /// <summary>Decision for <see cref="InteractiveBattle.Current"/>: stop wind-ups, protect whoever is targeted, heal, then hit the weakest reachable enemy as hard as possible.</summary>
        public static PlayerCommand Choose(InteractiveBattle b)
        {
            var me = b.Current;
            var alive = b.Enemies.Where(e => e.IsAlive).ToList();
            var skills = me.Skills.Where(s => s.Id != "basic" && b.CanUse(s)).ToList();
            var charging = alive.Where(e => e.Charging).ToList();

            if (charging.Count > 0)
            {
                var stun = skills.FirstOrDefault(s => s.Effect == SkillEffect.Stun);
                if (stun != null && b.EnemiesInRange(stun.Range).Contains(charging[0])) return PlayerCommand.UseSkill(stun, charging[0]);
                bool targeted = charging.Any(e => e.ChargeTarget == me);
                if (targeted)
                {
                    var evade = skills.FirstOrDefault(s => s.Effect == SkillEffect.Evade);
                    if (evade != null) return PlayerCommand.UseSkill(evade, null);
                    var barrier = skills.FirstOrDefault(s => s.Effect == SkillEffect.GuardParty);
                    if (barrier != null) return PlayerCommand.UseSkill(barrier, null);
                    return PlayerCommand.Guard();
                }
            }

            // cure a burning or poisoned ally (what a person would do with 자하청심결)
            var cleanse = skills.FirstOrDefault(s => s.Effect == SkillEffect.Cleanse);
            var sick = b.Party.FirstOrDefault(m => m.IsAlive && (m.Has(StatusKind.Burn) || m.Has(StatusKind.Poison)));
            if (cleanse != null && sick != null) return PlayerCommand.UseSkill(cleanse, null).WithAlly(sick);

            var heal = skills.FirstOrDefault(s => s.Effect == SkillEffect.HealAlly);
            if (heal != null && b.Party.Any(m => m.IsAlive && m.Hp < m.Stats.MaxHp * 0.55f)) return PlayerCommand.UseSkill(heal, null);

            // The strongest art that reaches, aimed at the weakest enemy it reaches.
            var best = skills.Where(x => x.Effect == SkillEffect.None && b.CanReach(x))
                .OrderByDescending(x => x.Power * x.Hits * (x.AllEnemies ? b.EnemiesInRange(x.Range).Count : 1)).FirstOrDefault();
            if (best != null) return PlayerCommand.UseSkill(best, b.EnemiesInRange(best.Range).OrderBy(e => e.Hp).First());
            if (me.Qi < 20 && me.Hp < me.Stats.MaxHp * 0.8 && b.EnemiesInRange(1).Count == 0) return PlayerCommand.Meditate();
            return PlayerCommand.UseSkill(SkillSpec.BasicAttack(), b.EnemiesInRange(1).OrderBy(e => e.Hp).First());
        }

        public static List<SkillSpec> BestLoadout(int stageNumber)
        {
            var learned = EarlyGameContent.Arts.Where(a => a.OwnerId == null && a.UnlockDiff == 0 && a.UnlockStage <= stageNumber).ToList();
            var eq = new List<ArtData>();
            foreach (var id in new[] { "seonpung_roejeol", "samcheopyeong" })
            {
                var x = learned.FirstOrDefault(a => a.Id == id);
                if (x != null) eq.Add(x);
            }
            foreach (var x in learned.Where(a => !eq.Contains(a) && a.Effect == SkillEffect.None)
                .OrderByDescending(a => a.Power * a.Hits * (a.AllEnemies ? 2 : 1)))
                if (eq.Count < Progression.MaxEquippedArts) eq.Add(x);
            return eq.Select(a => a.ToSpec()).ToList();
        }

        public static List<Combatant> BuildParty(int stageNumber, int level)
        {
            return BalanceSimulation.Party(BalanceSimulation.Starter(stageNumber, level), EarlyGameContent.FindStage(stageNumber));
        }

        public static bool ClearStage(StageData st, int seed)
        {
            return BalanceSimulation.Play(st, BalanceSimulation.Starter(st.Number, st.RecommendedPlayerLevel), seed).Won;
        }
    }
}
