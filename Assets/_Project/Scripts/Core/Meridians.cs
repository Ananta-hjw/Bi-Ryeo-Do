using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>One of the eight extraordinary meridians: four small steps (혈자리 1-4) and a fifth that completes it.</summary>
    public sealed class MeridianData
    {
        public string Id, Name;
        public int Index;          // 0..7, the order they open in
        public int UnlockStage;    // 일반 stage that must be cleared
        public string StepText;    // what each of the first four points gives
        public string FinishText;  // what completing the meridian adds
    }

    /// <summary>
    /// 기경팔맥 타통: a lasting growth board for 비류연. Each meridian opens with 일반 stage progress (8, 16 … 64) and has five points;
    /// the first four cost 은자 and give a small stat, the fifth also costs 심득석 and adds a battle effect. 임맥+독맥 complete is 소주천,
    /// all eight is 대주천. No point can fail. The meridian names are general wuxia terms; the effects, gates and numbers are game design
    /// (the source volumes do not describe them). Pure upside: the balance bot never opens a point.
    /// </summary>
    public static class Meridians
    {
        public const int Points = 5;              // points per meridian
        public const int StatPoints = Points - 1; // the points that give the per-point stat
        public const int OpenEvery = 8;           // 일반 stage gap between meridians

        public static readonly MeridianData[] All =
        {
            M("im", "임맥", 0, "내공 +5%", "매 차례 내공 회복 +2"),
            M("dok", "독맥", 1, "체력 +4%", "운기할 때 체력 회복 +5%"),
            M("chung", "충맥", 2, "공격 +2.5%", "치명타 피해 +15%p"),
            M("dae", "대맥", 3, "방어 +3%", "방어 명령 피해 x0.4 → x0.35"),
            M("yangyu", "양유맥", 4, "속도 +1", "전투 시작 행동 게이지 +20%"),
            M("eumyu", "음유맥", 5, "상태 이상 걸릴 확률 -4%", "상태 이상 지속 -1턴 (최소 1턴)"),
            M("yanggyo", "양교맥", 6, "가한 기세 +1", "전투 시작 기세 +20"),
            M("eumgyo", "음교맥", 7, "치명타 +1%", "받는 회복 +10%"),
        };

        private static MeridianData M(string id, string name, int index, string step, string finish)
        {
            return new MeridianData { Id = id, Name = name, Index = index, UnlockStage = OpenEvery * (index + 1), StepText = step, FinishText = finish };
        }

        // per-point and completion values
        public const float QiPerPoint = .05f, HpPerPoint = .04f, AtkPerPoint = .025f, DefPerPoint = .03f, ResistPerPoint = .04f, CritPerPoint = .01f;
        public const int SpeedPerPoint = 1, SpiritPerPoint = 1;
        public const int QiRegenFinish = 2, StartSpiritFinish = 20;
        public const float MeditateHealFinish = .05f, CritDamageFinish = .15f, GuardCutFinish = .05f, HealTakenFinish = .10f;
        public const int StartGaugeFinish = InteractiveBattle.GaugeThreshold / 5;   // 20% of the gauge
        public const int StatusTurnCutFinish = 1;
        public const float SmallCircuitBonus = .03f;     // 소주천: 체력·내공·공격·방어 +3%
        public const float GrandCircuitUltimate = .15f;  // 대주천: 필살기 위력 +15%

        public static MeridianData Find(string id)
        {
            foreach (var m in All) if (m.Id == id) return m;
            return null;
        }

        /// <summary>Points opened in a meridian (0..5), read from "id:n" entries of <see cref="SaveData.Meridians"/>.</summary>
        public static int Level(SaveData save, string id)
        {
            if (save == null || save.Meridians == null) return 0;
            string prefix = id + ":";
            foreach (var e in save.Meridians)
            {
                int n;
                if (e != null && e.StartsWith(prefix, StringComparison.Ordinal) && int.TryParse(e.Substring(prefix.Length), out n)) return Math.Max(0, Math.Min(Points, n));
            }
            return 0;
        }

        private static void SetLevel(SaveData save, string id, int level)
        {
            if (save.Meridians == null) save.Meridians = new List<string>();
            save.Meridians.RemoveAll(e => e != null && e.StartsWith(id + ":", StringComparison.Ordinal));
            save.Meridians.Add(id + ":" + level);
        }

        public static bool IsOpen(SaveData save, MeridianData m) { return m != null && save.ClearedStage >= m.UnlockStage; }
        public static bool IsComplete(SaveData save, string id) { return Level(save, id) >= Points; }
        public static string UnlockText(MeridianData m) { return "일반 스테이지 " + m.UnlockStage + " 돌파"; }

        public static int CompletedCount(SaveData save)
        {
            int n = 0;
            foreach (var m in All) if (IsComplete(save, m.Id)) n++;
            return n;
        }

        public static int TotalPoints(SaveData save)
        {
            int n = 0;
            foreach (var m in All) n += Level(save, m.Id);
            return n;
        }

        public static bool SmallCircuit(SaveData save) { return IsComplete(save, "im") && IsComplete(save, "dok"); }
        public static bool GrandCircuit(SaveData save) { return CompletedCount(save) >= All.Length; }

        /// <summary>은자 for opening point <paramref name="point"/> (1..5) of a meridian: 1,500 for the first point of 임맥 up to 23,400 for the last of 음교맥.</summary>
        public static long DooCost(MeridianData m, int point)
        {
            if (m == null || point < 1 || point > Points) return -1;
            double v = 1500.0 * (1.0 + 0.6 * m.Index) * (1.0 + 0.5 * (point - 1));
            return (long)Math.Round(v / 100.0) * 100;
        }

        /// <summary>심득석 for the completing point (2 for 임맥 … 5 for 음교맥; 28 for all eight); 0 for the first four.</summary>
        public static int StoneCost(MeridianData m, int point) { return m != null && point == Points ? 2 + m.Index / 2 : 0; }

        /// <summary>Opens the next point of a meridian. Returns false with a reason when it is closed, complete or not affordable.</summary>
        public static bool Open(SaveData save, string id, out string error)
        {
            error = null;
            var m = Find(id);
            if (m == null) { error = "알 수 없는 경맥입니다."; return false; }
            if (!IsOpen(save, m)) { error = UnlockText(m) + " 후 열립니다."; return false; }
            int next = Level(save, id) + 1;
            if (next > Points) { error = "이미 타통한 경맥입니다."; return false; }
            long doo = DooCost(m, next);
            int stones = StoneCost(m, next);
            if (save.Doo < doo) { error = "은자가 모자랍니다. (" + doo.ToString("N0") + "은자 필요)"; return false; }
            if (stones > 0 && InnerArts.Stones(save) < stones) { error = "심득석이 모자랍니다. (" + stones + "개 필요)"; return false; }
            if (stones > 0 && !Inventory.Remove(save, InnerArts.StoneId, stones)) { error = "심득석이 모자랍니다. (" + stones + "개 필요)"; return false; }
            save.Doo -= doo;
            SetLevel(save, id, next);
            return true;
        }

        /// <summary>Meridians that opened but have not been looked at yet (red dot on the 경맥 page).</summary>
        public static List<MeridianData> NewlyOpened(SaveData save)
        {
            var list = new List<MeridianData>();
            foreach (var m in All)
                if (IsOpen(save, m) && Level(save, m.Id) == 0 && !save.SeenMeridians.Contains(m.Id)) list.Add(m);
            return list;
        }

        public static void MarkSeen(SaveData save) { foreach (var m in NewlyOpened(save)) save.SeenMeridians.Add(m.Id); }

        // ------------------------------------------------------------------ effects

        private static int StatLevel(SaveData save, string id) { return Math.Min(StatPoints, Level(save, id)); }

        /// <summary>The stat part (after 천무학관, before 심법): per-point bonuses, 충맥's 치명타 피해, 소주천.</summary>
        public static CombatStats Apply(CombatStats stats, SaveData save)
        {
            if (save == null || save.Meridians == null || save.Meridians.Count == 0) return stats;
            float hp = HpPerPoint * StatLevel(save, "dok");
            float qi = QiPerPoint * StatLevel(save, "im");
            float atk = AtkPerPoint * StatLevel(save, "chung");
            float def = DefPerPoint * StatLevel(save, "dae");
            if (SmallCircuit(save)) { hp += SmallCircuitBonus; qi += SmallCircuitBonus; atk += SmallCircuitBonus; def += SmallCircuitBonus; }
            stats.MaxHp = (int)Math.Round(stats.MaxHp * (1f + hp));
            stats.MaxQi = (int)Math.Round(stats.MaxQi * (1f + qi));
            stats.Attack = (int)Math.Round(stats.Attack * (1f + atk));
            stats.Defense = (int)Math.Round(stats.Defense * (1f + def));
            stats.Speed += SpeedPerPoint * StatLevel(save, "yangyu");
            stats.CritRate = Math.Min(1f, stats.CritRate + CritPerPoint * StatLevel(save, "eumgyo"));
            if (IsComplete(save, "chung")) stats.CritDamage += CritDamageFinish;
            return stats;
        }

        /// <summary>The in-fight part, on the protagonist's combatant: 기세, 상태 이상 resistance and every completion effect but 충맥's.</summary>
        public static void ApplyPerks(Combatant hero, SaveData save)
        {
            if (hero == null || save == null || save.Meridians == null || save.Meridians.Count == 0) return;
            hero.StatusResist += ResistPerPoint * StatLevel(save, "eumyu");
            hero.SpiritDealtBonus += SpiritPerPoint * StatLevel(save, "yanggyo");
            if (IsComplete(save, "im")) hero.QiRegenBonus += QiRegenFinish;
            if (IsComplete(save, "dok")) hero.MeditateHealBonus += MeditateHealFinish;
            if (IsComplete(save, "dae")) hero.GuardCut += GuardCutFinish;
            if (IsComplete(save, "yangyu")) hero.StartGauge = Math.Max(hero.StartGauge, StartGaugeFinish);
            if (IsComplete(save, "eumyu")) hero.StatusTurnCut += StatusTurnCutFinish;
            if (IsComplete(save, "yanggyo")) hero.StartSpirit += StartSpiritFinish;
            if (IsComplete(save, "eumgyo")) hero.HealTakenBonus += HealTakenFinish;
            if (GrandCircuit(save)) hero.UltimateDamageBonus += GrandCircuitUltimate;
        }

        /// <summary>What the meridian gives now, e.g. "내공 +15% (3/4)", plus its completion effect once complete.</summary>
        public static string CurrentText(SaveData save, MeridianData m)
        {
            int n = Level(save, m.Id);
            if (n == 0) return "효과 없음";
            string text = StepText(m, Math.Min(StatPoints, n));
            if (n >= Points) text += "\n완성: " + m.FinishText;
            return text;
        }

        /// <summary>The summed per-point bonus at <paramref name="points"/> points, "내공 +15%".</summary>
        public static string StepText(MeridianData m, int points)
        {
            points = Math.Max(0, Math.Min(StatPoints, points));
            switch (m.Id)
            {
                case "im": return "내공 +" + Pct(QiPerPoint * points);
                case "dok": return "체력 +" + Pct(HpPerPoint * points);
                case "chung": return "공격 +" + Pct(AtkPerPoint * points);
                case "dae": return "방어 +" + Pct(DefPerPoint * points);
                case "yangyu": return "속도 +" + SpeedPerPoint * points;
                case "eumyu": return "상태 이상 걸릴 확률 -" + Pct(ResistPerPoint * points);
                case "yanggyo": return "가한 기세 +" + SpiritPerPoint * points;
                case "eumgyo": return "치명타 +" + Pct(CritPerPoint * points);
            }
            return "";
        }

        private static string Pct(float v) { return (Math.Round(v * 1000f) / 10f).ToString(System.Globalization.CultureInfo.InvariantCulture) + "%"; }

        /// <summary>Lines for the 상태창: one per meridian with points, then 소주천/대주천.</summary>
        public static List<string> SummaryLines(SaveData save)
        {
            var lines = new List<string>();
            foreach (var m in All)
            {
                int n = Level(save, m.Id);
                if (n > 0) lines.Add(m.Name + " " + n + "/" + Points + "  " + CurrentText(save, m).Replace("\n", "  "));
            }
            if (SmallCircuit(save)) lines.Add("소주천: 체력·내공·공격·방어 +" + Pct(SmallCircuitBonus));
            if (GrandCircuit(save)) lines.Add("대주천: 필살기 위력 +" + Pct(GrandCircuitUltimate));
            return lines;
        }
    }
}
