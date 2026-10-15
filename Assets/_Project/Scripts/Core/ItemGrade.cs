namespace BiRyeoDo.Core
{
    /// <summary>Item rarity, from common to mythic. Drives 뽑기 odds, stat budgets and the 분해 refund of duplicates.</summary>
    public enum ItemGrade { Normal, Fine, Rare, Epic, Legendary, Mythic }

    public static class Grades
    {
        public static readonly string[] Names = { "일반", "고급", "희귀", "영웅", "전설", "신화" };

        /// <summary>Chance of each grade in one pull: 60 / 20 / 10 / 6 / 3 / 1 percent.</summary>
        public static readonly double[] Rates = { 0.60, 0.20, 0.10, 0.06, 0.03, 0.01 };

        /// <summary>두 gained when a duplicate piece of gear is pulled (it is dismantled instead of stacking).</summary>
        public static readonly long[] DismantleDoo = { 100, 300, 800, 2000, 5000, 12000 };

        public static string NameOf(ItemGrade grade) { return Names[(int)grade]; }
    }
}
