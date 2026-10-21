namespace BiRyeoDo.Core
{
    public enum SkillEffect
    {
        None,
        Evade,      // the user dodges every hit until its next turn
        GuardParty, // every living ally guards until its next turn
        Stun,       // damages, then the target loses its next action (a charging enemy is interrupted instead of skipped on bosses)
        HealAlly,   // restores HealPct of max HP to the most wounded ally
        Cleanse     // cures every 상태 이상 of the chosen ally and restores HealPct of max HP
    }

    /// <summary>A usable martial art in battle (무공).</summary>
    public sealed class SkillSpec
    {
        public string Id;
        public string Name;
        public string Description;
        public float Power = 1f;   // damage multiplier on Attack, per hit
        public int Hits = 1;       // number of hits per use
        public int QiCost;         // 내공
        public int SpiritCost;     // 기세: 비기 consume the whole gauge (100)
        public int Cooldown;       // own turns before reuse; 0 = every turn
        public bool Ranged;       // animation only: strike from the current position, independent of reach
        public bool AllEnemies;    // hits every living enemy instead of the chosen one (front row only while a front-row enemy stands, see Formation)
        public int Range = 1;      // how many enemy lines the art reaches (1 = the front line only, 2+ = any line) (attacks and area arts; self/party effects ignore it)
        public SkillEffect Effect;
        public float HealPct;      // for HealAlly
        public StatusKind Inflict;       // 상태 이상 the art may leave on each enemy it hits
        public float InflictChance;      // chance per enemy hit (rolled once per use)
        public float EvadeChance = 1f;   // for Evade: chance that each incoming hit is dodged (mastery lowers it for the protagonist's 삼첩영)

        /// <summary>"맨 앞 줄" / "모든 줄": which of the enemy lines an art of this reach touches (a reach of 2 or more covers every line).</summary>
        public static string ReachText(int range)
        {
            return range <= 1 ? "맨 앞 줄" : "모든 줄";
        }

        public static SkillSpec BasicAttack()
        {
            return new SkillSpec { Id = "basic", Name = "기본 공격", Power = 1f, QiCost = 0, Cooldown = 0 };
        }
    }
}
