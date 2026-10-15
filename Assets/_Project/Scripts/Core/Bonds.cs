using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>Bond points with one companion. Public fields for JsonUtility.</summary>
    [Serializable]
    public sealed class BondEntry
    {
        public string Id;
        public int Points;
    }

    /// <summary>
    /// 인연: fighting beside a companion builds a bond (+1 per cleared stage fought together, more from events). From 친분 on the
    /// protagonist can launch a 합공 with that companion; each bond level adds +10% to its power.
    /// </summary>
    public static class Bonds
    {
        public static readonly int[] Thresholds = { 0, 3, 8, 15 };
        public static readonly string[] LevelNames = { "서먹", "친분", "신뢰", "의기투합" };
        public const float PowerPerLevel = 0.15f;
        public const float JointBasePower = 2.0f;
        public const float JointReferenceFactor = 0.8f;   // each strike is 80% of the protagonist's strongest art, so 합공 keeps up with later arts

        public static int LevelOf(int points)
        {
            int level = 0;
            for (int i = 0; i < Thresholds.Length; i++) if (points >= Thresholds[i]) level = i;
            return level;
        }

        public static BondEntry Find(SaveData save, string companionId)
        {
            foreach (var e in save.Bonds) if (e.Id == companionId) return e;
            return null;
        }

        public static int Points(SaveData save, string companionId) { var e = Find(save, companionId); return e != null ? e.Points : 0; }
        public static int Level(SaveData save, string companionId) { return LevelOf(Points(save, companionId)); }

        /// <summary>Adds bond points; returns the new level when it just rose, otherwise -1.</summary>
        public static int Add(SaveData save, string companionId, int points)
        {
            if (points <= 0) return -1;
            var e = Find(save, companionId);
            if (e == null) { e = new BondEntry { Id = companionId }; save.Bonds.Add(e); }
            int before = LevelOf(e.Points);
            e.Points += points;
            int after = LevelOf(e.Points);
            return after > before ? after : -1;
        }

        /// <summary>Strongest single-skill power (power x hits) among a member's non-ultimate attack arts; 0 when there are none.</summary>
        public static float ReferencePower(System.Collections.Generic.IEnumerable<SkillSpec> skills)
        {
            float best = 0f;
            foreach (var sk in skills)
            {
                if (sk.SpiritCost > 0 || sk.Id == "basic") continue;
                if (sk.Effect != SkillEffect.None && sk.Effect != SkillEffect.Stun) continue;
                best = Math.Max(best, sk.Power * Math.Max(1, sk.Hits));
            }
            return best;
        }

        /// <summary>The 합공 skill for a companion at a bond level (null below 친분). <paramref name="referencePower"/> scales it with the protagonist's best art.</summary>
        public static SkillSpec JointSpec(CompanionData companion, int level, float referencePower = 0f)
        {
            if (companion == null || level < 1 || string.IsNullOrEmpty(companion.JointName)) return null;
            float basePower = Math.Max(JointBasePower, referencePower * JointReferenceFactor);
            return new SkillSpec
            {
                Id = "joint", Name = companion.JointName, Power = basePower * companion.JointPower * (1f + PowerPerLevel * (level - 1)),
                Effect = companion.JointEffect, HealPct = 0.20f
            };
        }
    }
}
