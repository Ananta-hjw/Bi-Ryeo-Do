namespace BiRyeoDo.Core
{
    public enum SkillEffect { None, Evade }

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
        public bool AllEnemies;    // hits every living enemy instead of the chosen one
        public SkillEffect Effect; // Evade: dodge every hit until the next own turn

        public static SkillSpec BasicAttack()
        {
            return new SkillSpec { Id = "basic", Name = "기본 공격", Power = 1f, QiCost = 0, Cooldown = 0 };
        }
    }
}
