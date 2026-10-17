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
    /// 대공자 비 (the strongest enemy so far) lands on the first step of 초절정. Pure stat flavour: nothing in battle reads it.
    /// </summary>
    public static class Realm
    {
        public const double BasePower = 27.8;    // a level 1 protagonist
        public const double AnchorPower = 572.0; // 대공자 비 (about 572.7) = the anchor
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
        public static int StepOf(SaveData save) { return StepOf(Power(InnerArts.HeroStats(save))); }

        /// <summary>"초절정 초입 (상급)".</summary>
        public static string Label(int step)
        {
            var s = Steps[step];
            return s.Name + " (" + s.Group + ")";
        }

        /// <summary>The next step's name and the power still missing, or null at the top.</summary>
        public static string NextText(double power)
        {
            int step = StepOf(power);
            if (step + 1 >= Steps.Count) return "";
            return Steps[step + 1].Name + "까지 종합 수치 " + Math.Ceiling(LowerBound(step + 1) - power).ToString("N0") + " 남음";
        }

        public static string Describe(SaveData save)
        {
            double p = Power(InnerArts.HeroStats(save));
            return Label(StepOf(p)) + "  ·  종합 수치 " + Math.Round(p).ToString("N0");
        }
    }
}
