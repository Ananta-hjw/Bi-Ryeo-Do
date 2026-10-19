using System;
using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;

namespace BiRyeoDo.Tests
{
    /// <summary>Explicit, reproducible builds; no random gear or hidden stat multipliers.</summary>
    public static class BalanceSimulation
    {
        public sealed class Result
        {
            public bool Won;
            public int Actions, FailedFight = -1;
            public double HpLeft;
        }

        // A prepared player: start-kit gear, entry mastery, free breath, no training or faction bonuses.
        // Learning costs are checked separately by the first-clear campaign; fixed-level checks isolate combat.
        /// <summary>
        /// Levels a real player has over the balance bot at the stage's recommended level, standing for the gear, 심법, mastery and items the bot ignores.
        /// The solo stages 1-6 (fought with the start kit) get none; from stage 7 on the bot is given ceil((stage - 6) / 3) more levels (stage 30: +8)
        /// so that the enemy HP boost on 일반 (<see cref="StageStars.NormalHpBoost"/>) stays clearable by it. See Docs/BALANCE.md.
        /// </summary>
        public static int EdgeLevels(int stage) { return StageStars.NormalEdgeLevels(stage); }

        /// <summary>The level the bot plays a stage at: the recommended level plus <see cref="EdgeLevels"/>.</summary>
        public static int BotLevel(StageData stage) { return stage.RecommendedPlayerLevel + EdgeLevels(stage.Number); }

        public static SaveData Starter(int stage, int level)
        {
            var save = new SaveData { Level = level, ClearedStage = stage - 1, SwordRank = stage >= Promotion.GateStage ? Promotion.GateRank : 1 };
            Inventory.EnsureStartKit(save);
            foreach (var skill in SimpleBot.BestLoadout(stage))
            {
                save.LearnedArts.Add(skill.Id);
                save.EquippedArts.Add(skill.Id);
            }
            return save;
        }

        public static List<Combatant> Party(SaveData save, StageData stage)
        {
            var party = new List<Combatant> { Formation.BuildHero(save) };
            foreach (var c in EarlyGameContent.SelectCompanionsFor(Roster.PartyStage(stage, save), save))
                party.Add(CompanionGrowth.Create(save, c));
            Formation.Apply(party, save, stage);
            return party;
        }

        public static Result Play(StageData stage, SaveData save, int seed, bool progression = false)
        {
            var party = Party(save, stage);
            var result = new Result();
            for (int k = 0; k < stage.Encounters.Length; k++)
            {
                var enemies = stage.Encounters[k].Select((id, i) =>
                {
                    var data = stage.EnemyFor(id);
                    return data.CreateCombatant(id + i);
                }).ToList();
                var battle = new InteractiveBattle(party, enemies, new SeededRandom(seed * 1000 + stage.Number * 10 + k)) { Trial = stage.TrialFor(k) };
                battle.Begin();
                for (int guard = 0; !battle.IsOver && guard < 3000; guard++)
                {
                    var actor = battle.Current;
                    var command = SimpleBot.Choose(battle);
                    battle.PlayerAct(command);
                    result.Actions++;
                    if (progression && actor.Id == Inventory.Hero && command.Kind == CommandKind.Skill && command.Skill.Id != "basic")
                        Mastery.RecordUse(save, command.Skill.Id);
                }
                if (!battle.PlayerWon) { result.FailedFight = k; return result; }
                if (progression)
                {
                    Recommend.FightReward(save, stage, k, false, out long exp, out long doo);
                    CompanionGrowth.EnsureRecords(save);
                    var leveled = new HashSet<string>();
                    if (Progression.AddExp(save, exp) > 0) leveled.Add(Inventory.Hero);
                    foreach (var m in party.Skip(1)) if (CompanionGrowth.AddExp(save, m.Id, exp) > 0) leveled.Add(m.Id);
                    CompanionGrowth.ShareWithBench(save, party.Select(m => m.Id).ToList(), exp);
                    save.Doo += doo;
                    if (leveled.Count > 0)
                    {
                        var fresh = Party(save, stage);
                        foreach (var m in fresh)
                        {
                            var old = party.First(p => p.Id == m.Id);
                            if (!leveled.Contains(m.Id))
                            {
                                m.Hp = old.IsAlive ? Math.Max(1, (int)Math.Round(m.Stats.MaxHp * (double)old.Hp / old.Stats.MaxHp)) : 0;
                                m.Qi = (int)Math.Round(m.Stats.MaxQi * (double)old.Qi / Math.Max(1, old.Stats.MaxQi));
                            }
                            m.Spirit = old.Spirit;
                        }
                        party = fresh;
                    }
                }
                if (k == stage.Encounters.Length - 1) result.HpLeft = party.Sum(p => (double)p.Hp) / party.Sum(p => (double)p.Stats.MaxHp);
                foreach (var m in party) m.RestBetweenFights();
            }
            result.Won = true;
            return result;
        }

        public static void BuyLoadout(SaveData save, int stage)
        {
            save.EquippedArts.Clear();
            foreach (var skill in SimpleBot.BestLoadout(stage))
            {
                var art = EarlyGameContent.FindArt(skill.Id);
                if (!save.LearnedArts.Contains(art.Id) && art.IsUnlocked(save) && save.Doo >= art.LearnCost)
                {
                    save.Doo -= art.LearnCost;
                    save.LearnedArts.Add(art.Id);
                }
                if (save.LearnedArts.Contains(art.Id)) save.EquippedArts.Add(art.Id);
            }
        }
    }
}
