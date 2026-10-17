using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>One step of the 무공 경지 ladder.</summary>
    public sealed class RealmStep
    {
        public string Group;   // 하급 / 중급 / 상급 / 최상급 / 초월급 / 신급
        public string Realm;   // 삼급 ... 여의경
        public string Sub;     // 초입 / 완숙 / 극 for 상급 and above, "" below
        public string Name { get { return Sub.Length == 0 ? Realm : Realm + " " + Sub; } }
    }

    /// <summary>
    /// 무공 경지: the protagonist's composite stat power (geometric mean of 체력·공격력·방어력·속도, gear and 심법 included)
    /// placed on a ladder of steps. Step i starts at <see cref="BasePower"/> x Ratio^i; the ratio is fixed so that
    /// 대공자 비 on 어려움 (stage 30, the strongest enemy so far) lands on the first step of 초절정. Pure stat flavour: nothing in battle reads it.
    /// </summary>
    public static class Realm
    {
        public const double BasePower = 50.0;    // the start of 삼급's successor ladder: below this is still 삼급 (a level 1 protagonist is about 28)
        public const double AnchorPower = 951.0; // 대공자 비 on 어려움 (about 951.8) = the anchor
        public const string AnchorRealm = "초절정", AnchorSub = "초입";

        private static readonly string[] Subs = { "초입", "완숙", "극" };
        private static readonly string[][] Realms =
        {
            new[] { "하급", "삼급", "이급" },
            new[] { "중급", "일급", "초일급" },
            new[] { "상급", "절정", "초절정" },
            new[] { "최상급", "화경", "현경", "생사경" },
            new[] { "초월급", "자연경", "공허경" },
            new[] { "신급", "여의경" },
        };

        public static readonly IList<RealmStep> Steps = Build();
        public static readonly int AnchorStep = IndexOf(AnchorRealm, AnchorSub);
        public static readonly double Ratio = Math.Pow(AnchorPower / BasePower, 1.0 / AnchorStep);

        private static List<RealmStep> Build()
        {
            var list = new List<RealmStep>();
            foreach (var group in Realms)
            {
                // 상급 and everything above splits each realm into 초입 / 완숙 / 극.
                bool split = group[0] != "하급" && group[0] != "중급";
                for (int i = 1; i < group.Length; i++)
                {
                    if (!split) list.Add(new RealmStep { Group = group[0], Realm = group[i], Sub = "" });
                    else foreach (var sub in Subs) list.Add(new RealmStep { Group = group[0], Realm = group[i], Sub = sub });
                }
            }
            return list;
        }

        private static int IndexOf(string realm, string sub)
        {
            for (int i = 0; i < Steps.Count; i++) if (Steps[i].Realm == realm && Steps[i].Sub == sub) return i;
            throw new ArgumentException(realm + " " + sub);
        }

        /// <summary>Composite power: the geometric mean of 체력, 공격력, 방어력 and 속도.</summary>
        public static double Power(CombatStats s)
        {
            return Math.Pow((double)Math.Max(1, s.MaxHp) * Math.Max(1, s.Attack) * Math.Max(1, s.Defense) * Math.Max(1, s.Speed), 0.25);
        }

        /// <summary>The composite power at which a step begins.</summary>
        public static double LowerBound(int step) { return BasePower * Math.Pow(Ratio, step); }

        public static int StepOf(double power)
        {
            int step = 0;
            while (step + 1 < Steps.Count && power >= LowerBound(step + 1)) step++;
            return step;
        }

        /// <summary>The protagonist's current realm (gear, 심법 and 검룡위 included, like in battle).</summary>
        public static int StepOf(SaveData save) { return StepOf(Power(InnerArts.HeroStatsBeforeRealm(save))); }

        public const float BonusPerStep = 0.05f;

        /// <summary>Each step above 삼급 raises every stat of the protagonist by 5% (step 4 = +20%).</summary>
        public static float Bonus(int step) { return BonusPerStep * Math.Max(0, step); }

        public static CombatStats ApplyBonus(CombatStats stats, int step)
        {
            float m = 1f + Bonus(step);
            stats.MaxHp = (int)Math.Round(stats.MaxHp * m);
            stats.MaxQi = (int)Math.Round(stats.MaxQi * m);
            stats.Attack = (int)Math.Round(stats.Attack * m);
            stats.Defense = (int)Math.Round(stats.Defense * m);
            stats.Speed = Math.Max(1, (int)Math.Round(stats.Speed * m));
            return stats;
        }

        /// <summary>"초절정 초입".</summary>
        public static string Label(int step) { return Steps[step].Name; }

        /// <summary>Call now and then: returns true once when the protagonist's realm rose above the best one reached so far (nothing for a fall,
        /// e.g. after taking off gear). The first call on an older save only records the current step.</summary>
        public static bool PollRise(SaveData save, out int from, out int to)
        {
            from = to = StepOf(save);
            if (save.RealmMax < 0) { save.RealmMax = to; return false; }
            if (to <= save.RealmMax) return false;
            from = save.RealmMax;
            save.RealmMax = to;
            return true;
        }

        /// <summary>The next step's name and the power still missing, or null at the top.</summary>
        public static string NextText(double power)
        {
            int step = StepOf(power);
            if (step + 1 >= Steps.Count) return "";
            return Steps[step + 1].Name + "까지 종합 수치 " + Math.Ceiling(LowerBound(step + 1) - power).ToString("N0") + " 남음";
        }

        // Beasts, traps and objects have no 무공 경지.
        private static readonly HashSet<string> NotMartial = new HashSet<string>
        {
            "tiger", "bear", "white_tiger", "trap_arrow", "trap_needle", "trap_log", "cave_viper", "mukrin_hyeolmang",
            "bisa_snake", "cheonghong_ssanggaksa", "heuksaek_amhyeok", "hwaryong_hwayeom", "hwan_ying",
        };

        public static bool HasRealm(EnemyData enemy) { return enemy != null && !NotMartial.Contains(enemy.Id); }

        /// <summary>An enemy's realm from its own stats on the 일반 난이도 ("" for beasts, traps and objects).</summary>
        public static string ForEnemy(EnemyData enemy)
        {
            if (!HasRealm(enemy)) return "";
            return Steps[StepOf(Power(enemy.CreateCombatant(enemy.Id).Stats))].Name;
        }

        public static string Describe(SaveData save)
        {
            double p = Power(InnerArts.HeroStatsBeforeRealm(save));
            return Label(StepOf(p)) + "  ·  종합 수치 " + Math.Round(p).ToString("N0");
        }
    }
}
