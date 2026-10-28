using System;
using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;

namespace BiRyeoDo.Tests
{
    /// <summary>Explicit comparison builds and combat telemetry for optional content; no random gear or consumables.</summary>
    public static class SideContentSimulation
    {
        public sealed class Result
        {
            public bool Won;
            public int Actions, EnemyActions, Deaths;
            public long DamageTaken;
            public double HpLeft;
        }

        public static SaveData Prepared(int story, int level, bool trainingGear = false)
        {
            var save = BalanceSimulation.Starter(story, level);
            if (!trainingGear) return save;
            BalanceSimulation.UseRealGear(save);
            var owners = new[] { Inventory.Hero }.Concat(EarlyGameContent.SelectCompanionsFor(story, save).Select(c => c.Id)).ToArray();
            foreach (string id in Gear.Sets[0].Items) Inventory.Add(save, id, owners.Length);
            foreach (string owner in owners)
            {
                Inventory.GearOf(save, owner).Clear();
                foreach (string id in Gear.Sets[0].Items)
                    if (!Inventory.EquipFor(save, owner, id, out string error)) throw new InvalidOperationException(error);
                foreach (var slot in save.GearSlots.Where(s => s.Owner == owner))
                { slot.Level = 10; slot.UnlockedGrade = (int)ItemGrade.Rare; }
            }
            return save;
        }

        public static Result Play(StageData stage, SaveData save, int seed, bool defend = true)
        {
            var party = BalanceSimulation.Party(save, stage);
            var enemies = stage.Encounters[0].Select((id, i) => stage.EnemyFor(id, stage.TrialFor(0)).CreateCombatant(id + i)).ToList();
            var battle = new InteractiveBattle(party, enemies, new SeededRandom(seed * 1000 + stage.Number * 10)) { Trial = stage.TrialFor(0) };
            var partyIds = new HashSet<string>(party.Select(p => p.Id));
            long taken = 0;
            void Record(IEnumerable<BattleEvent> events)
            {
                foreach (var e in events) if (e.Damage > 0 && e.TargetId != null && partyIds.Contains(e.TargetId)) taken += e.Damage;
            }
            Record(battle.Begin());
            while (!battle.IsOver) Record(battle.PlayerAct(defend ? SimpleBot.Choose(battle) : WithoutTelegraphResponse(battle)));
            return new Result { Won = battle.PlayerWon, Actions = battle.PartyActions, EnemyActions = battle.EnemyActions, DamageTaken = taken,
                Deaths = party.Count(p => !p.IsAlive), HpLeft = party.Sum(p => (double)p.Hp) / party.Sum(p => (double)p.Stats.MaxHp) };
        }

        // Same healing/cleanse/damage decisions as SimpleBot; only telegraph interruption, guarding and evasion are omitted.
        private static PlayerCommand WithoutTelegraphResponse(InteractiveBattle battle)
        {
            var actor = battle.Current;
            var skills = actor.Skills.Where(s => s.Id != "basic" && battle.CanUse(s)).ToList();
            var sick = battle.Party.FirstOrDefault(p => p.IsAlive && (p.Has(StatusKind.Poison) || p.Has(StatusKind.Burn)));
            var cleanse = skills.FirstOrDefault(s => s.Effect == SkillEffect.Cleanse);
            if (sick != null && cleanse != null) return PlayerCommand.UseSkill(cleanse, null).WithAlly(sick);
            var heal = skills.FirstOrDefault(s => s.Effect == SkillEffect.HealAlly);
            if (heal != null && battle.Party.Any(p => p.IsAlive && p.Hp < p.Stats.MaxHp * .55f)) return PlayerCommand.UseSkill(heal, null);
            var best = skills.Where(s => s.Effect == SkillEffect.None && battle.CanReach(s))
                .OrderByDescending(s => s.Power * s.Hits * (s.AllEnemies ? battle.EnemiesInRange(s.Range).Count : 1)).FirstOrDefault() ?? SkillSpec.BasicAttack();
            return PlayerCommand.UseSkill(best, battle.EnemiesInRange(best.Range).OrderBy(e => e.Hp).First());
        }
    }
}
