namespace BiRyeoDo.Core
{
    /// <summary>One entry in an enemy's attack script. A Charge move does no damage but telegraphs the next one.</summary>
    public sealed class EnemyMove
    {
        public string Name;
        public float Power = 1f;
        public int Hits = 1;
        public bool Charge;

        public static readonly EnemyMove Plain = new EnemyMove { Name = "공격" };

        public static EnemyMove Attack(string name, float power, int hits = 1)
        {
            return new EnemyMove { Name = name, Power = power, Hits = hits };
        }

        public static EnemyMove Wind(string name)
        {
            return new EnemyMove { Name = name, Charge = true, Power = 0f };
        }
    }
}
