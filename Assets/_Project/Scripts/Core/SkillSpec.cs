namespace BiRyeoDo.Core
{
    /// <summary>A usable martial art in battle (무공). Single-target damage for now.</summary>
    public sealed class SkillSpec
    {
        public string Id;
        public string Name;
        public float Power = 1f;   // damage multiplier on Attack
        public int QiCost;
        public int Cooldown;       // own turns before reuse; 0 = every turn

        public static SkillSpec BasicAttack()
        {
            return new SkillSpec { Id = "basic", Name = "기본 공격", Power = 1f, QiCost = 0, Cooldown = 0 };
        }
    }
}
