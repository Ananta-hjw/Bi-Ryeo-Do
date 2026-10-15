namespace BiRyeoDo.Core
{
    /// <summary>Item rarity, from common to mythic. Drives 뽑기 odds, stat budgets and the 분해 refund of duplicates.</summary>
    public enum ItemGrade { Normal, Fine, Rare, Epic, Legendary, Mythic }

    public static class Grades
    {
        public static readonly string[] Names = { "일반", "고급", "희귀", "영웅", "전설", "신화" };

        /// <summary>Chance of each grade in one pull: 62 / 20 / 10 / 5 / 2.5 / 0.5 percent.</summary>
        public static readonly double[] Rates = { 0.62, 0.20, 0.10, 0.05, 0.025, 0.005 };

        /// <summary>두 gained when a duplicate piece of gear is pulled (it is dismantled instead of stacking).</summary>
        public static readonly long[] DismantleDoo = { 20, 60, 160, 400, 1000, 2400 };

        public static string NameOf(ItemGrade grade) { return Names[(int)grade]; }
    }
}
