using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    public enum SoldierRole { None, Warrior, Archer, Shield }

    /// <summary>
    /// Unnamed rank-and-file soldiers (2026-11) get a stat profile on top of their scaled values: 궁수형 (archers, throwers, back-line assassins)
    /// hit harder but are frail, 방패·철갑형 (shields, armour, blockade lines, heavy axemen) hit a little softer but last longer, 일반 무사 stay as they are.
    /// Applied in <see cref="EnemyData.BuildStatsBeforeRealm"/> by enemy id, so every difficulty and 의뢰 copy (same id) carries it;
    /// 무한 비무대 clones keep it through <see cref="EnemyData.RoleOf"/>. Named characters, beasts, traps and survival walls are not listed.
    /// </summary>
    public static class SoldierRoles
    {
        public const float ArcherAtk = 1.15f, ArcherHp = 0.85f, ArcherDef = 0.85f;
        public const float ShieldAtk = 0.92f, ShieldHp = 1.15f, ShieldDef = 1.15f;

        private static readonly Dictionary<string, SoldierRole> Table = Build();

        private static Dictionary<string, SoldierRole> Build()
        {
            var t = new Dictionary<string, SoldierRole>();
            foreach (var id in new[]
            {
                "hogol_archer", "yonggol_archer", "baegin_jagaek", "hyeolma_dae", "hyeolma_dae_b", "jungwon_pyosa",
                "amryong_salsu", "amyeong_member", "salgwi_daewon", "cheolbi_gyeok"
            }) t[id] = SoldierRole.Archer;
            foreach (var id in new[]
            {
                "hogol_axe", "yonggol_axe", "sadae_jilpung", "sinung_daewon", "armored_wolf", "sura_screen", "hwapyeong_breakout",
                "donwang_guard", "academy_alarm_guard", "first_unit", "north_route_guard"
            }) t[id] = SoldierRole.Shield;
            foreach (var id in new[]
            {
                "biaosa", "hogol_bandit", "yonggol_bandit", "jungyang_pyosa", "hoajang_disciple", "hoajang_senior", "hwasan_jeja",
                "dobaek_member", "geomhon_member", "cheongryong_member", "amheon_guard", "jeogung_bandit", "bikwae_ssangchang",
                "heukrang_bandit", "hwapyeong_soldier", "mulsung_daewon", "salryong_daewon", "ipgwan_dojeonja", "donwang_hobeop",
                "third_dog_squad", "exit_dog_squad"
            }) t[id] = SoldierRole.Warrior;
            return t;
        }

        public static SoldierRole Of(string enemyId)
        {
            SoldierRole r;
            return enemyId != null && Table.TryGetValue(enemyId, out r) ? r : SoldierRole.None;
        }

        public static IEnumerable<KeyValuePair<string, SoldierRole>> All { get { return Table; } }

        public static void Factors(SoldierRole role, out float hp, out float atk, out float def)
        {
            if (role == SoldierRole.Archer) { hp = ArcherHp; atk = ArcherAtk; def = ArcherDef; }
            else if (role == SoldierRole.Shield) { hp = ShieldHp; atk = ShieldAtk; def = ShieldDef; }
            else { hp = atk = def = 1f; }
        }

        public static string Name(SoldierRole role)
        {
            return role == SoldierRole.Archer ? "궁수형" : role == SoldierRole.Shield ? "방패·철갑형" : role == SoldierRole.Warrior ? "일반 무사" : "";
        }
    }
}
