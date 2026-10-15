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
        public const float PowerPerLevel = 0.10f;
        public const float JointBasePower = 1.1f;

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

        /// <summary>The 합공 skill for a companion at a bond level (null below 친분).</summary>
        public static SkillSpec JointSpec(CompanionData companion, int level)
        {
            if (companion == null || level < 1 || string.IsNullOrEmpty(companion.JointName)) return null;
            return new SkillSpec
            {
                Id = "joint", Name = companion.JointName, Power = JointBasePower * companion.JointPower * (1f + PowerPerLevel * (level - 1)),
                Effect = companion.JointEffect, HealPct = 0.20f
            };
        }
    }
}
