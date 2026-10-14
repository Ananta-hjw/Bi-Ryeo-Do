using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;

namespace BiRyeoDo.Tests
{
    /// <summary>A reasonable (not perfect) player used to check that stages are winnable. Mirrors what a person would do.</summary>
    public static class SimpleBot
    {
        /// <summary>Decision for <see cref="InteractiveBattle.Current"/>: stop wind-ups, protect whoever is targeted, heal, then hit hardest.</summary>
        public static PlayerCommand Choose(InteractiveBattle b)
        {
            var me = b.Current;
            var alive = b.Enemies.Where(e => e.IsAlive).ToList();
            var target = alive.OrderBy(e => e.Hp).First();
            var skills = me.Skills.Where(s => s.Id != "basic" && b.CanUse(s)).ToList();
            var charging = alive.Where(e => e.Charging).ToList();

            if (charging.Count > 0)
            {
                var stun = skills.FirstOrDefault(s => s.Effect == SkillEffect.Stun);
                if (stun != null) return PlayerCommand.UseSkill(stun, charging[0]);
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

            var heal = skills.FirstOrDefault(s => s.Effect == SkillEffect.HealAlly);
            if (heal != null && b.Party.Any(m => m.IsAlive && m.Hp < m.Stats.MaxHp * 0.55f)) return PlayerCommand.UseSkill(heal, null);

            var best = skills.Where(s => s.Effect == SkillEffect.None || s.Effect == SkillEffect.Stun)
                .Where(s => s.Effect != SkillEffect.Stun)
                .OrderByDescending(s => s.Power * s.Hits * (s.AllEnemies ? alive.Count : 1)).FirstOrDefault();
            if (best != null) return PlayerCommand.UseSkill(best, target);
            if (me.Qi < 20 && me.Hp < me.Stats.MaxHp * 0.8) return PlayerCommand.Meditate();
            return PlayerCommand.UseSkill(SkillSpec.BasicAttack(), target);
        }

        public static List<SkillSpec> BestLoadout(int stageNumber)
        {
            var learned = EarlyGameContent.Arts.Where(a => a.OwnerId == null && a.UnlockStage <= stageNumber).ToList();
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

        public static List<Combatant> BuildParty(int stageNumber, int level, int life)
        {
            var party = new List<Combatant> { new Combatant("p", "비류연", Team.Player, Progression.StatsFor(level, life), BestLoadout(stageNumber)) };
            foreach (var c in EarlyGameContent.SelectCompanions(stageNumber, null)) party.Add(c.CreateCombatant(level, life));
            return party;
        }

        public static bool ClearStage(StageData st, int seed)
        {
            var party = BuildParty(st.Number, st.RecommendedLevel, 1);
            for (int k = 0; k < st.Encounters.Length; k++)
            {
                var enemies = st.Encounters[k].Select((id, i) => EarlyGameContent.FindEnemy(id).CreateCombatant(id + i)).ToList();
                var battle = new InteractiveBattle(party, enemies, new SeededRandom(seed * 100 + st.Number)) { Trial = st.TrialFor(k) };
                battle.Begin();
                for (int guard = 0; !battle.IsOver && guard < 3000; guard++) battle.PlayerAct(Choose(battle));
                if (!battle.PlayerWon) return false;
                foreach (var m in party) m.RestBetweenFights(0.5f);
            }
            return true;
        }
    }
}
