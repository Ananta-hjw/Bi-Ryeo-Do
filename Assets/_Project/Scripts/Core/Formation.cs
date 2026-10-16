using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// 진형: the party stands in a 앞줄 (front row, at most 2) and a 뒷줄 (back row, at most 2).
    /// Enemies' single-target attacks hit the front row first and only reach the back row once the front has fallen;
    /// the back row takes less damage but also deals a little less, the front row hits a little harder.
    /// The player's choice is stored as the ids standing in the back row (<see cref="SaveData.BackRowIds"/>);
    /// without a custom choice an automatic formation puts the protagonist and the tankiest companions in front.
    /// </summary>
    public static class Formation
    {
        public const int MaxFront = 2, MaxBack = 2;
        public const string HeroId = "player";

        /// <summary>Damage taken by a back-row unit (single-target hits; there are no enemy area attacks).</summary>
        public const float BackTakenFactor = 0.75f;
        /// <summary>Damage dealt by the front row / back row.</summary>
        public const float FrontDealtFactor = 1.10f, BackDealtFactor = 0.90f;

        /// <summary>Front-line priority: 0 hero, 1 guards (태을천강벽), 2 fighters, 3 healers (stay back).</summary>
        private static int Priority(string id)
        {
            if (id == HeroId) return 0;
            int p = 2;
            foreach (var a in EarlyGameContent.Arts)
            {
                if (a.OwnerId != id) continue;
                if (a.Effect == SkillEffect.GuardParty) p = Math.Min(p, 1);
                if (a.Effect == SkillEffect.HealAlly) return 3;
            }
            return p;
        }

        public static int FrontCountFor(int n) { return n <= MaxFront ? n : MaxFront; }

        /// <summary>The automatic formation: ids standing in the back row.</summary>
        public static HashSet<string> AutoBack(IList<string> ids)
        {
            var order = new List<string>(ids);
            // stable sort by priority (List.Sort is not stable)
            var sorted = new List<string>();
            for (int p = 0; p <= 3; p++) foreach (var id in order) if (Priority(id) == p) sorted.Add(id);
            var back = new HashSet<string>();
            for (int i = FrontCountFor(sorted.Count); i < sorted.Count; i++) back.Add(sorted[i]);
            return back;
        }

        /// <summary>True when <paramref name="back"/> leaves 1-2 in front and at most 2 behind.</summary>
        public static bool IsValid(int n, int backCount)
        {
            int front = n - backCount;
            return front >= 1 && front <= MaxFront && backCount <= MaxBack;
        }

        /// <summary>The back-row ids for these party ids: the saved custom formation when it still fits the party, otherwise the automatic one.</summary>
        public static HashSet<string> BackSet(SaveData save, IList<string> ids)
        {
            if (save != null && save.FormationCustom)
            {
                var back = new HashSet<string>();
                foreach (var id in ids) if (save.BackRowIds.Contains(id)) back.Add(id);
                if (IsValid(ids.Count, back.Count)) return back;
            }
            return AutoBack(ids);
        }

        /// <summary>Sets <see cref="Combatant.BackRow"/> on the party (the protagonist first in the list).</summary>
        public static void Apply(IList<Combatant> party, SaveData save)
        {
            var ids = new List<string>();
            foreach (var m in party) ids.Add(m.Id);
            var back = BackSet(save, ids);
            foreach (var m in party) m.BackRow = back.Contains(m.Id);
        }

        /// <summary>
        /// Moves one member to the other row. When that row is already full the member swaps places with whoever fits the
        /// row he leaves best (the most front-minded back member / the most back-minded front member), so a full 2+2 party can be rearranged.
        /// </summary>
        public static bool Move(SaveData save, IList<string> ids, string id, out string error)
        {
            error = null;
            var back = BackSet(save, ids);
            if (back.Contains(id))
            {
                back.Remove(id);
                if (ids.Count - back.Count > MaxFront)
                {
                    string other = null;
                    foreach (var m in ids) if (m != id && !back.Contains(m) && (other == null || Priority(m) > Priority(other))) other = m;
                    if (other != null) back.Add(other);
                }
            }
            else
            {
                back.Add(id);
                if (back.Count > MaxBack)
                {
                    string other = null;
                    foreach (var m in ids) if (m != id && back.Contains(m) && (other == null || Priority(m) < Priority(other))) other = m;
                    if (other != null) back.Remove(other);
                }
            }
            if (!IsValid(ids.Count, back.Count))
            {
                error = "앞줄에는 한 명 이상 있어야 합니다.";
                return false;
            }
            save.FormationCustom = true;
            save.BackRowIds = new List<string>(back);
            return true;
        }

        public static void Reset(SaveData save)
        {
            save.FormationCustom = false;
            save.BackRowIds = new List<string>();
        }

        public static string RowName(bool back) { return back ? "뒷줄" : "앞줄"; }
    }
}
