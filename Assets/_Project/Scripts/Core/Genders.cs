using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// Which default battle figure (<c>default_man</c> / <c>default_woman</c>) a character without artwork gets.
    /// Women are the characters the story analysis shows as women plus those whose name reads as a woman's (추가연, 언가영, 백사영); everyone else, including beasts and traps, is a man.
    /// </summary>
    public static class Genders
    {
        // Art keys (companion ids and EnemyData.ArtKey) of women: 나예린, 검후 이옥상, 독한녀 십이선녀, 철옥잠 마하령, 은가면 여인 셋 (story); 추가연, 언가영, 사교검 백사영 (name).
        private static readonly HashSet<string> Women = new HashSet<string>
        {
            "na_yerin", "geomhu_ioksang", "sunyeo", "ma_haryeong", "mask_gongpo", "mask_jeolmang", "mask_bitan",
            "chu_gayeon", "eonga_yeong", "baek_sayeong",
        };

        // Art keys of bandits and assassins: 산적 셋, 자객, 살수 계열(살귀대, 천살·지살, 천지쌍살, 초혼오귀검), 쌍귀, 무정, 복면인, 하얀 손의 고수.
        private static readonly HashSet<string> Assassins = new HashSet<string>
        {
            "hogol_bandit", "yonggol_bandit", "heukrang_bandit", "baegin_jagaek", "ssanggwi", "chohon_gwigeom", "cheonsal", "jisal",
            "ssangsal_hapgyeok", "salgwi_daewon", "mu_jeong", "suda_bokmyeon", "baek_son_gosu",
        };

        public static bool IsAssassin(string artKey) { return !string.IsNullOrEmpty(artKey) && Assassins.Contains(artKey); }

        /// <summary>Name of the stand-in battle figure (file name under <c>Battle/</c>): assassin types first, then woman, else man.</summary>
        public static string DefaultFigure(string artKey) { return IsAssassin(artKey) ? "default_assassin" : IsFemale(artKey) ? "default_woman" : "default_man"; }

        public static bool IsFemale(string artKey) { return !string.IsNullOrEmpty(artKey) && Women.Contains(artKey); }
    }
}
