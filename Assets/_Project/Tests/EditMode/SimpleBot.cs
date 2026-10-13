using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;

namespace BiRyeoDo.Tests
{
    /// <summary>A reasonable (not perfect) player used to check that stages are winnable. Mirrors what a person would do.</summary>
    public static class SimpleBot
    {
        public static PlayerCommand Choose(InteractiveBattle b, List<SkillSpec> equipped)
        {
            var alive = b.Enemies.Where(e => e.IsAlive).ToList();
            var target = alive.OrderBy(e => e.Hp).First();
            if (alive.Any(e => e.Charging))
            {
                var evade = equipped.FirstOrDefault(s => s.Effect == SkillEffect.Evade && b.CanUse(s));
                return evade != null ? PlayerCommand.UseSkill(evade, null) : PlayerCommand.Guard();
            }
            var best = equipped.Where(s => s.Effect == SkillEffect.None && b.CanUse(s))
                .OrderByDescending(s => s.Power * s.Hits * (s.AllEnemies ? alive.Count : 1)).FirstOrDefault();
            if (best != null) return PlayerCommand.UseSkill(best, target);
            if (b.Player.Qi < 20 && b.Player.Hp < b.Player.Stats.MaxHp * 0.8) return PlayerCommand.Meditate();
            return PlayerCommand.UseSkill(SkillSpec.BasicAttack(), target);
        }

        public static List<SkillSpec> BestLoadout(int stageNumber)
        {
            var learned = EarlyGameContent.Arts.Where(a => a.UnlockStage <= stageNumber).ToList();
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

        public static bool ClearStage(StageData st, int seed)
        {
            var specs = BestLoadout(st.Number);
            var player = new Combatant("p", "비류연", Team.Player, Progression.StatsFor(st.RecommendedLevel, 1), specs);
            foreach (var enc in st.Encounters)
            {
                var enemies = enc.Select((id, i) => EarlyGameContent.FindEnemy(id).CreateCombatant(id + i)).ToList();
                var battle = new InteractiveBattle(player, enemies, new SeededRandom(seed * 100 + st.Number));
                battle.Begin();
                for (int guard = 0; !battle.IsOver && guard < 2000; guard++) battle.PlayerAct(Choose(battle, specs));
                if (!battle.PlayerWon) return false;
                player.RestBetweenFights(0.5f);
            }
            return true;
        }
    }
}
