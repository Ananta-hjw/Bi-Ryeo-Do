using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;

namespace BiRyeoDo.Tests
{
    /// <summary>A reasonable (not perfect) player used to check that stages are winnable. Mirrors what a person would do.</summary>
    public static class SimpleBot
    {
        /// <summary>The tile (within walking range, staying allowed) from which <paramref name="enemy"/> is closest.</summary>
        private static int[] Aim(InteractiveBattle b, Combatant me, Combatant enemy)
        {
            int[] best = { me.Col, me.Row };
            int bestD = Board.Dist(me, enemy), bestM = System.Math.Abs(me.Col - enemy.Col) + System.Math.Abs(me.Row - enemy.Row);
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    if (!b.CanMoveTo(me, c, r)) continue;
                    int d = Board.Dist(c, r, enemy.Col, enemy.Row), m = System.Math.Abs(c - enemy.Col) + System.Math.Abs(r - enemy.Row);
                    if (d < bestD || (d == bestD && m < bestM)) { best = new[] { c, r }; bestD = d; bestM = m; }
                }
            return best;
        }

        private static PlayerCommand Moved(PlayerCommand cmd, InteractiveBattle b, int[] tile)
        {
            var me = b.Current;
            return tile[0] == me.Col && tile[1] == me.Row ? cmd : cmd.WithMove(tile[0], tile[1]);
        }

        /// <summary>Decision for <see cref="InteractiveBattle.Current"/>: stop wind-ups, protect whoever is targeted, heal, then walk up to the weakest enemy and hit hardest.</summary>
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
                if (stun != null)
                {
                    var tile = Aim(b, me, charging[0]);
                    if (Board.Dist(tile[0], tile[1], charging[0].Col, charging[0].Row) <= stun.Range) return Moved(PlayerCommand.UseSkill(stun, charging[0]), b, tile);
                }
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

            // Walk up to the weakest enemy, then pick the strongest art that reaches from there.
            var stand = Aim(b, me, target);
            var best = skills.Where(s => s.Effect == SkillEffect.None)
                .Where(s => s.AllEnemies ? b.EnemiesInRange(stand[0], stand[1], s.Range).Count > 0 : Board.Dist(stand[0], stand[1], target.Col, target.Row) <= s.Range)
                .OrderByDescending(s => s.Power * s.Hits * (s.AllEnemies ? b.EnemiesInRange(stand[0], stand[1], s.Range).Count : 1)).FirstOrDefault();
            if (best != null) return Moved(PlayerCommand.UseSkill(best, target), b, stand);
            if (Board.Dist(stand[0], stand[1], target.Col, target.Row) <= 1) return Moved(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), target), b, stand);
            if (me.Qi < 20 && me.Hp < me.Stats.MaxHp * 0.8) return Moved(PlayerCommand.Meditate(), b, stand);
            return Moved(PlayerCommand.Guard(), b, stand);   // could not get close enough this turn: keep walking
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
            var party = new List<Combatant> { new Combatant("p", "비류연", Team.Player, Progression.HeroStatsFor(level), BestLoadout(stageNumber)) };
            foreach (var c in EarlyGameContent.SelectCompanions(stageNumber, null)) party.Add(c.CreateCombatant(level));
            Formation.Apply(party, null);
            return party;
        }

        public static bool ClearStage(StageData st, int seed)
        {
            var party = BuildParty(st.Number, st.RecommendedLevel);
            for (int k = 0; k < st.Encounters.Length; k++)
            {
                var enemies = st.Encounters[k].Select((id, i) => EarlyGameContent.FindEnemy(id).CreateCombatant(id + i)).ToList();
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
