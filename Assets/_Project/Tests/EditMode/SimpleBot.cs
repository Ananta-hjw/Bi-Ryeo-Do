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
            // The bot wears no gear, but it does get the 경지 bonus of the realm its plain stats reach.
            var heroBase = Progression.HeroStatsFor(level);
            var hero = Realm.ApplyBonus(heroBase, Realm.StepOf(Realm.Power(heroBase)));
            var party = new List<Combatant> { new Combatant("p", "비류연", Team.Player, hero, BestLoadout(stageNumber)) };
            foreach (var c in EarlyGameContent.SelectCompanions(stageNumber, null)) party.Add(c.CreateCombatant(level));
            Formation.Apply(party, null);
            return party;
        }

        public static bool ClearStage(StageData st, int seed)
        {
            var party = BuildParty(st.Number, st.RecommendedLevel);
            for (int k = 0; k < st.Encounters.Length; k++)
            {
                var enemies = st.Encounters[k].Select((id, i) => (st.IsQuest ? Quests.Scale(st.QuestId, EarlyGameContent.FindEnemy(id)) : EarlyGameContent.FindEnemy(id)).CreateCombatant(id + i)).ToList();
                Formation.Apply(party, null);   // every fight starts from the formation tiles
                var battle = new InteractiveBattle(party, enemies, new SeededRandom(seed * 1000 + st.Number * 10 + k)) { Trial = st.TrialFor(k) };
                battle.Begin();
                for (int guard = 0; !battle.IsOver && guard < 3000; guard++) battle.PlayerAct(Choose(battle));
                if (!battle.PlayerWon) return false;
                foreach (var m in party) m.RestBetweenFights(0.5f);
            }
            return true;
        }
    }
}
