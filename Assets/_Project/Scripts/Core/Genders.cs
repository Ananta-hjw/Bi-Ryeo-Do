using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// Which default battle figure (<c>default_man</c> / <c>default_woman</c>) a character without artwork gets.
    /// Only characters the story analysis clearly shows as women are listed; everyone else, including beasts, traps and anyone unclear, is a man.
    /// </summary>
    public static class Genders
    {
        // Art keys (companion ids and EnemyData.ArtKey) of women: 나예린, 검후 이옥상, 독한녀 십이선녀, 철옥잠 마하령, 은가면 여인 셋.
        private static readonly HashSet<string> Women = new HashSet<string>
        {
            "na_yerin", "geomhu_ioksang", "sunyeo", "ma_haryeong", "mask_gongpo", "mask_jeolmang", "mask_bitan",
        };

        public static bool IsFemale(string artKey) { return !string.IsNullOrEmpty(artKey) && Women.Contains(artKey); }
    }
}
