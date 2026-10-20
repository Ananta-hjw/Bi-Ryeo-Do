namespace BiRyeoDo.Core
{
    /// <summary>Item rarity, from common to mythic; 초월 (Transcendent) is above mythic, never drops from 뽑기 and is earned only through special conditions (see Ritual). Drives 뽑기 odds and stat budgets.</summary>
    public enum ItemGrade { Normal, Fine, Rare, Epic, Legendary, Mythic, Transcendent }

    public static class Grades
    {
        public static readonly string[] Names = { "일반", "고급", "희귀", "영웅", "전설", "신화", "초월" };

        /// <summary>Chance of each grade in one pull: 61 / 20 / 10 / 5.5 / 2.8 / 0.7 percent (초월 is not pullable, so it has no entry).</summary>
        public static readonly double[] Rates = { 0.61, 0.20, 0.10, 0.055, 0.028, 0.007 };


        public static string NameOf(ItemGrade grade) { return Names[(int)grade]; }
    }
}
