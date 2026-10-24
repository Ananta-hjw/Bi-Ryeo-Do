using System;
using System.Collections.Generic;
using System.Linq;

namespace BiRyeoDo.Core
{
    /// <summary>Deterministic, role-aware loadout search over owned free copies and the selected person's gear.</summary>
    public static class EquipmentRecommendation
    {
        private const int BeamWidth = 64, ExactLimit = 60000;

        private sealed class Candidate
        {
            public string[] Slots;
            public double Score;
            public int Changes, Filled;
        }

        private sealed class Context
        {
            public SaveData Save;
            public string Owner;
            public CompanionData Companion;
            public int Level, Bond, Training;
            public Combatant Reference;
            public string[] Original;
            public List<string>[] Options;
            public CombatStats BaseStats;
            public Dictionary<string, GearEffects>[] Effects;
            public Dictionary<string, int> ItemSets;
            public SetTier[][] SetBonuses;
            public int[] SetCounts;
            public bool Healer, Protector;

            public CombatStats Stats(string[] slots)
            {
                var total = new GearEffects();
                Array.Clear(SetCounts, 0, SetCounts.Length);
                for (int i = 0; i < 6; i++)
                {
                    if (slots[i] == null) continue;
                    var effect = Effects[i][slots[i]];
                    total.HpPct += effect.HpPct; total.QiPct += effect.QiPct; total.AtkPct += effect.AtkPct; total.DefPct += effect.DefPct;
                    total.SpeedAdd += effect.SpeedAdd; total.CritAdd += effect.CritAdd; total.CritDmgAdd += effect.CritDmgAdd; total.StartSpirit += effect.StartSpirit;
                    int set; if (ItemSets.TryGetValue(slots[i], out set)) SetCounts[set]++;
                }
                for (int i = 0; i < SetCounts.Length; i++)
                {
                    var bonus = SetBonuses[i][SetCounts[i]];
                    total.HpPct += bonus.HpPct; total.QiPct += bonus.QiPct; total.AtkPct += bonus.AtkPct; total.DefPct += bonus.DefPct;
                    total.SpeedAdd += bonus.SpeedAdd; total.CritAdd += bonus.CritAdd;
                }
                var stats = Inventory.ApplyGearEffects(BaseStats, total);
                if (Companion == null) stats = InnerArts.Apply(Promotion.ApplyRankBonus(stats, Promotion.EffectiveRank(Save)), Save.EquippedInner, Save.InnerLevels);
                return Realm.ApplyBonus(stats, Realm.StepOf(Realm.Power(stats)));
            }

            public Candidate Evaluate(string[] slots)
            {
                var stats = Stats(slots);
                const double turns = 8;
                double ordinaryWeight = 0, cost = 0;
                foreach (var skill in Reference.Skills)
                {
                    if (skill.SpiritCost > 0) continue;
                    double weight = 1.0 / (1 + Math.Max(0, skill.Cooldown));
                    ordinaryWeight += weight; cost += weight * skill.QiCost;
                }
                double coverage = Math.Min(1, (stats.MaxQi + turns * (InteractiveBattle.QiRegenPerTurn + Reference.QiRegenBonus))
                    / Math.Max(1, turns * cost / Math.Max(1, ordinaryWeight)));
                double value = 0, weights = 0;
                foreach (var skill in Reference.Skills)
                {
                    double weight = 1.0 / (1 + Math.Max(0, skill.Cooldown));
                    if (skill.QiCost > 0) weight *= coverage * Math.Min(1, (double)stats.MaxQi / skill.QiCost);
                    if (skill.SpiritCost > 0)
                        weight *= Math.Min(1, (stats.StartSpirit + Reference.StartSpirit - Reference.Stats.StartSpirit
                            + turns * (InteractiveBattle.SpiritOnDealt + Reference.SpiritDealtBonus)) / Math.Max(1, turns * skill.SpiritCost));
                    double benefit = 0;
                    if (InteractiveBattle.NeedsTarget(skill))
                    {
                        double crit = Math.Max(0, Math.Min(1, stats.CritRate));
                        benefit = (BattleEngine.CalcDamage(stats, Reference.Stats, skill, false) * (1 - crit)
                            + BattleEngine.CalcDamage(stats, Reference.Stats, skill, true) * crit) * Math.Max(1, skill.Hits);
                        if (skill.AllEnemies) benefit *= 1.8;
                        else if (skill.Range >= 3) benefit *= 1.08;
                        if (skill.Effect == SkillEffect.Stun) benefit += Reference.Stats.MaxHp * .08;
                        if (skill.Inflict != StatusKind.None) benefit += skill.InflictChance * Reference.Stats.MaxHp * .035;
                    }
                    else if (skill.Effect == SkillEffect.HealAlly || skill.Effect == SkillEffect.Cleanse)
                        benefit = Reference.Stats.MaxHp * (skill.HealPct * 1.6 + (skill.Effect == SkillEffect.Cleanse ? .08 : 0));
                    else if (skill.Effect == SkillEffect.GuardParty) benefit = Reference.Stats.MaxHp * .35;
                    else if (skill.Effect == SkillEffect.Evade) benefit = Reference.Stats.MaxHp * .16 * skill.EvadeChance;
                    value += weight * benefit;
                    // Qi pressure reduces skill contribution; do not renormalize it back up when resources run dry.
                    weights += 1.0 / (1 + Math.Max(0, skill.Cooldown));
                }
                double output = Math.Max(1, value / Math.Max(1, weights)) * Math.Max(1, stats.Speed) / 100.0;
                double survival = Math.Max(1, stats.MaxHp) * (BattleEngine.DefenseConstant + Math.Max(0, stats.EffDefense)) / BattleEngine.DefenseConstant;
                double score = Math.Log(output) + (Protector ? 1.05 : Healer ? .85 : .55) * Math.Log(survival);
                int changes = 0;
                for (int i = 0; i < 6; i++) if (slots[i] != Original[i]) changes++;
                return new Candidate { Slots = (string[])slots.Clone(), Score = score, Changes = changes, Filled = slots.Count(id => id != null) };
            }
        }

        private static Context Build(SaveData save, string owner)
        {
            if (save == null) return null;
            owner = string.IsNullOrEmpty(owner) ? Inventory.Hero : owner;
            var companion = owner == Inventory.Hero ? null : EarlyGameContent.FindCompanion(owner);
            if (owner != Inventory.Hero && (companion == null || companion.JoinStage > save.HighestUnlockedStage)) return null;
            Gear.SyncSlots(save, owner);
            int level = companion == null ? save.Level : CompanionGrowth.LevelOf(save, owner);
            int bond = companion == null ? 0 : Bonds.Level(save, owner);
            int training = companion == null ? 0 : CompanionTraining.LevelOf(save, owner);
            Combatant reference;
            if (companion != null) reference = companion.CreateCombatant(level, bond, training);
            else
            {
                reference = new Combatant(owner, "비류연", Team.Player,
                    InnerArts.Apply(Promotion.ApplyRankBonus(Progression.HeroStatsFor(level), Promotion.EffectiveRank(save)), save.EquippedInner, save.InnerLevels), null, true);
                var actual = Formation.BuildHero(save);
                reference.Skills.Clear(); reference.Skills.AddRange(actual.Skills);
                InnerArts.ApplyPerks(reference, save.EquippedInner, save.InnerLevels);
            }
            var context = new Context
            {
                Save = save, Owner = owner, Companion = companion, Level = level, Bond = bond, Training = training,
                Reference = reference, Original = new string[6], Options = new List<string>[6], Effects = new Dictionary<string, GearEffects>[6],
                BaseStats = companion == null ? Progression.HeroStatsFor(level) : companion.StatsBeforeGear(level, bond, training),
                ItemSets = new Dictionary<string, int>(), SetCounts = new int[Gear.Sets.Length], SetBonuses = new SetTier[Gear.Sets.Length][],
                Healer = reference.Skills.Any(s => s.Effect == SkillEffect.HealAlly || s.Effect == SkillEffect.Cleanse),
                Protector = reference.Skills.Any(s => s.Effect == SkillEffect.GuardParty)
            };
            for (int i = 0; i < Gear.Sets.Length; i++)
            {
                context.SetBonuses[i] = new SetTier[7];
                for (int n = 0; n <= 6; n++) context.SetBonuses[i][n] = Gear.ActiveBonus(Gear.Sets[i], n);
                foreach (string id in Gear.Sets[i].Items) context.ItemSets[id] = i;
            }
            var worn = Inventory.GearOf(save, owner);
            var available = save.Items.Where(s => s.Count > 0).Select(s => EarlyGameContent.FindItem(s.Id))
                .Where(item => item != null && item.IsGear && (companion == null || !Gear.IsHeroOnly(item.Id))
                    && (worn.Contains(item.Id) || Inventory.Free(save, item.Id) > 0)).Distinct().OrderBy(item => item.Id, StringComparer.Ordinal).ToList();
            for (int slot = 0; slot < 6; slot++)
            {
                context.Original[slot] = Gear.ItemInSlot(save, owner, slot)?.Id;
                context.Options[slot] = new List<string> { null };
                context.Effects[slot] = new Dictionary<string, GearEffects>();
                foreach (var item in available)
                {
                    if (item.Kind != Gear.KindForSlot(slot)) continue;
                    context.Options[slot].Add(item.Id);
                    context.Effects[slot][item.Id] = Gear.EffectsFor(item,
                        Gear.AppliedSlotLevel(save, owner, slot, item), Gear.Stars(save, item.Id));
                }
            }
            return context;
        }

        private static int Compare(Candidate a, Candidate b)
        {
            if (Math.Abs(a.Score - b.Score) > .000000001) return b.Score.CompareTo(a.Score);
            int filled = b.Filled.CompareTo(a.Filled);
            if (filled != 0) return filled;
            int changes = a.Changes.CompareTo(b.Changes);
            if (changes != 0) return changes;
            for (int i = 0; i < 6; i++)
            {
                int order = StringComparer.Ordinal.Compare(a.Slots[i], b.Slots[i]);
                if (order != 0) return order;
            }
            return 0;
        }

        private static bool Legal(string[] slots, int slot, string id)
        {
            if (id == null) return true;
            for (int i = 0; i < slots.Length; i++) if (i != slot && slots[i] == id) return false;
            return true;
        }

        private static Candidate Search(Context c)
        {
            var original = c.Evaluate(c.Original);
            var best = original;
            long combinations = 1;
            foreach (var options in c.Options) { combinations *= options.Count; if (combinations > ExactLimit) break; }
            if (combinations <= ExactLimit)
            {
                void Visit(string[] slots, int slot)
                {
                    if (slot == 6) { var candidate = c.Evaluate(slots); if (Compare(candidate, best) < 0) best = candidate; return; }
                    foreach (string id in c.Options[slot])
                    {
                        if (!Legal(slots, slot, id)) continue;
                        slots[slot] = id; Visit(slots, slot + 1); slots[slot] = null;
                    }
                }
                Visit(new string[6], 0);
                return best;
            }
            var beam = new List<Candidate> { c.Evaluate(new string[6]) };
            for (int slot = 0; slot < 6; slot++)
            {
                var next = new List<Candidate>();
                foreach (var partial in beam)
                    foreach (string id in c.Options[slot])
                    {
                        if (!Legal(partial.Slots, slot, id)) continue;
                        var slots = (string[])partial.Slots.Clone(); slots[slot] = id;
                        next.Add(c.Evaluate(slots));
                    }
                next.Sort(Compare);
                beam = next.Take(BeamWidth).ToList();
            }
            foreach (var candidate in beam) if (Compare(candidate, best) < 0) best = candidate;
            // Whole-set seeds keep weak individual pieces alive until their 2..6-piece thresholds activate.
            var seeds = new List<Candidate> { best, original };
            foreach (var set in Gear.Sets)
            {
                var slots = new string[6];
                for (int slot = 0; slot < 6; slot++)
                    foreach (string id in c.Options[slot])
                        if (id != null && Array.IndexOf(set.Items, id) >= 0 && Legal(slots, slot, id))
                        {
                            var trial = (string[])slots.Clone(); trial[slot] = id;
                            if (slots[slot] == null || Compare(c.Evaluate(trial), c.Evaluate(slots)) < 0) slots[slot] = id;
                        }
                if (slots.Any(id => id != null)) seeds.Add(c.Evaluate(slots));
            }
            // Improve complete outfits, including accessory swaps and accessory set thresholds.
            foreach (var seed in seeds)
            {
                var local = seed;
                for (int round = 0; round < 2; round++)
                {
                    var before = local;
                    for (int slot = 0; slot < 6; slot++)
                        foreach (string id in c.Options[slot])
                        {
                            if (!Legal(local.Slots, slot, id)) continue;
                            var slots = (string[])local.Slots.Clone(); slots[slot] = id;
                            var candidate = c.Evaluate(slots);
                            if (Compare(candidate, local) < 0) local = candidate;
                        }
                    foreach (string a in c.Options[4])
                        foreach (string b in c.Options[5])
                        {
                            if (a != null && a == b) continue;
                            var slots = (string[])local.Slots.Clone(); slots[4] = a; slots[5] = b;
                            var candidate = c.Evaluate(slots);
                            if (Compare(candidate, local) < 0) local = candidate;
                        }
                    if (Compare(local, before) == 0) break;
                }
                if (Compare(local, best) < 0) best = local;
            }
            return best;
        }

        /// <summary>Six slot ids, without changing worn gear or paying for enhancements/unlocks. Null means empty.</summary>
        public static string[] Suggest(SaveData save, string owner)
        {
            var context = Build(save, owner);
            return context == null ? null : Search(context).Slots;
        }

        /// <summary>Revalidates and applies one complete recommendation; other people and inventory counts stay intact.</summary>
        public static bool Equip(SaveData save, string owner, out int changed, out string error)
        {
            changed = 0; error = null;
            var context = Build(save, owner);
            if (context == null) { error = "장비를 추천할 수 없는 인물입니다."; return false; }
            var plan = Search(context).Slots;
            var worn = Inventory.GearOf(save, context.Owner);
            for (int i = 0; i < 6; i++)
            {
                if (plan[i] != context.Original[i]) changed++;
                if (plan[i] == null) continue;
                var item = EarlyGameContent.FindItem(plan[i]);
                if (item == null || item.Kind != Gear.KindForSlot(i) || (context.Companion != null && Gear.IsHeroOnly(item.Id))
                    || Inventory.Count(save, item.Id) <= 0 || (!worn.Contains(item.Id) && Inventory.Free(save, item.Id) <= 0) || !Legal(plan, i, item.Id))
                { error = "장비 보유 상태가 변경되었습니다. 다시 추천해 주세요."; changed = 0; return false; }
            }
            worn.Clear();
            foreach (string id in plan) if (id != null) worn.Add(id);
            for (int i = 0; i < 6; i++) Gear.AssignSlot(save, context.Owner, i, plan[i]);
            Gear.SyncSlots(save, context.Owner);
            return true;
        }
    }
}
