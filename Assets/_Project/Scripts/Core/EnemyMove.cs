namespace BiRyeoDo.Core
{
    /// <summary>One entry in an enemy's attack script. A Charge move does no damage but telegraphs the next one.</summary>
    public sealed class EnemyMove
    {
        public string Name;
        public float Power = 1f;
        public int Hits = 1;
        public bool Charge;
        public bool Awakening;    // one phase-transition strike, outside the normal script
        public float DamageFactor = 1f;   // scales damage without changing the original move's resource cost
        public bool Ranged;       // presentation only; does not change targeting or damage
        public bool Snipe;   // 저격: aims at the farthest party member within reach instead of the nearest
        public StatusKind Inflict;   // 상태 이상 the move may leave when it lands (from its name, see Statuses.ForMove)

        public EnemyMove FromDistance() { Ranged = true; return this; }

        public EnemyMove Sniping() { Snipe = true; return this; }

        public static readonly EnemyMove Plain = new EnemyMove { Name = "공격" };

        public static EnemyMove Attack(string name, float power, int hits = 1)
        {
            return new EnemyMove { Name = name, Power = power, Hits = hits, Inflict = Statuses.ForMove(name) };
        }

        public static EnemyMove Wind(string name)
        {
            return new EnemyMove { Name = name, Charge = true, Power = 0f };
        }
    }
}
