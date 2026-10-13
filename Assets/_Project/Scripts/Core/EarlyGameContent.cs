using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>Plain-data martial art entry.</summary>
    public sealed class ArtData
    {
        public string Id, Name, Description;
        public float Power = 1f;
        public int Hits = 1, QiCost, SpiritCost, Cooldown, UnlockStage;
        public bool AllEnemies;
        public SkillEffect Effect;
        public long LearnCost;

        public SkillSpec ToSpec()
        {
            return new SkillSpec
            {
                Id = Id, Name = Name, Description = Description, Power = Power, Hits = Hits, QiCost = QiCost,
                SpiritCost = SpiritCost, Cooldown = Cooldown, AllEnemies = AllEnemies, Effect = Effect
            };
        }
    }

    /// <summary>Plain-data enemy. Stats derive from the player curve at <see cref="Level"/> so difficulty tracks Progression.</summary>
    public sealed class EnemyData
    {
        public string Id, Name;
        public bool IsBoss;
        public int Level;
        public float HpMul = 1f, AtkMul = 1f, DefMul = 1f;
        public int SpeedAdd;
        public EnemyMove[] Moves;

        public CombatStats BuildStats()
        {
            var b = Progression.StatsFor(Level, 1);
            return new CombatStats
            {
                MaxHp = (int)Math.Round(b.MaxHp * HpMul),
                MaxQi = 0,
                Attack = (int)Math.Round(b.Attack * AtkMul),
                Defense = (int)Math.Round(b.Defense * DefMul),
                Speed = b.Speed + SpeedAdd,
                CritRate = IsBoss ? 0.06f : 0.02f,
                CritDamage = 1.5f
            };
        }

        public Combatant CreateCombatant(string uniqueId)
        {
            var c = new Combatant(uniqueId, Name, Team.Enemy, BuildStats(), null);
            c.Moves = Moves != null && Moves.Length > 0 ? new List<EnemyMove>(Moves) : new List<EnemyMove> { EnemyMove.Plain };
            return c;
        }

    }

    public sealed class StageData
    {
        public int Number;
        public string Name;
        public string[] Intro, Outro;
        /// <summary>Fights in order; each is a group of enemy ids (1-3). The last one is the boss fight.</summary>
        public string[][] Encounters;

        public int RecommendedLevel;
        public long DooTotal;   // 두 for clearing the stage once (split by enemy weight)
    }

    /// <summary>
    /// Early-game content (stages 1-7) taken from the analyzed volumes 1-3 (see Story/GAME_MAPPING.md).
    /// Names and events are canon; moves, numbers and unlock stages are game-design placeholders.
    /// </summary>
    public static class EarlyGameContent
    {
        public const string StarterArtId = "bido_throw";

        public static readonly ArtData[] Arts =
        {
            new ArtData { Id = StarterArtId, Name = "비도 던지기", Description = "비뢰도의 기초. 비도를 날려 적을 맞힌다.", Power = 1.3f, Cooldown = 0 },
            new ArtData { Id = "samboku_chobok", Name = "삼복구타권법 초복", Description = "사부가 창안한 권법. 초복은 근육을 노린다.", Power = 2.2f, QiCost = 14, Cooldown = 1, LearnCost = 0, UnlockStage = 1 },
            new ArtData { Id = "bunroesu", Name = "분뢰수", Description = "손을 강철처럼 만들어 내려친다.", Power = 3.0f, QiCost = 22, Cooldown = 2, LearnCost = 220, UnlockStage = 2 },
            new ArtData { Id = "samcheopyeong", Name = "삼첩영", Description = "봉황무의 잔상 셋. 다음 차례까지 모든 공격을 피한다.", QiCost = 20, Cooldown = 3, Effect = SkillEffect.Evade, LearnCost = 400, UnlockStage = 3 },
            new ArtData { Id = "bi_roe_do", Name = "비뢰도", Description = "비도 세 자루를 연달아 날린다.", Power = 1.7f, Hits = 3, QiCost = 32, Cooldown = 3, LearnCost = 900, UnlockStage = 3 },
            new ArtData { Id = "yeobanjang", Name = "여반장", Description = "손바닥을 뒤집는 회전력으로 일격에 제압하는 일장법.", Power = 5.2f, QiCost = 38, Cooldown = 3, LearnCost = 2200, UnlockStage = 5 },
            new ArtData { Id = "roegwang_ryuhagok", Name = "뢰광류하곡", Description = "여섯 자루를 동시에 운용해 적 전체를 벤다.", Power = 2.6f, AllEnemies = true, QiCost = 46, Cooldown = 4, LearnCost = 4500, UnlockStage = 6 },
            new ArtData { Id = "seonpung_roejeol", Name = "선풍뇌절", Description = "비기. 기세를 모두 쏟아 은빛 섬광으로 적을 쓰러뜨린다.", Power = 9.5f, SpiritCost = 100, LearnCost = 1500, UnlockStage = 4 },
        };

        public static readonly EnemyData[] Enemies =
        {
            // Stage 1: 아미산 (1권)
            new EnemyData { Id = "tiger", Name = "호랑이", Level = 1, HpMul = 0.7f, AtkMul = 0.41f, SpeedAdd = 8,
                Moves = new[] { EnemyMove.Attack("물어뜯기", 1.0f), EnemyMove.Attack("할퀴기", 0.8f, 2) } },
            new EnemyData { Id = "bear", Name = "곰", Level = 1, HpMul = 1.0f, AtkMul = 0.41f, SpeedAdd = -6,
                Moves = new[] { EnemyMove.Attack("앞발치기", 1.0f), EnemyMove.Wind("몸을 일으킨다"), EnemyMove.Attack("내려치기", 2.4f) } },
            new EnemyData { Id = "white_tiger", Name = "백호", Level = 2, IsBoss = true, HpMul = 1.6f, AtkMul = 0.42f, SpeedAdd = 8,
                Moves = new[] { EnemyMove.Attack("할퀴기", 0.8f, 2), EnemyMove.Attack("물어뜯기", 1.2f), EnemyMove.Wind("몸을 낮춘다"), EnemyMove.Attack("덮치기", 2.8f) } },
            // Stage 2: 중양표국 (1권)
            new EnemyData { Id = "biaosa", Name = "표사", Level = 3, HpMul = 0.8f, AtkMul = 0.46f,
                Moves = new[] { EnemyMove.Attack("베기", 1.0f), EnemyMove.Attack("찌르기", 1.1f) } },
            new EnemyData { Id = "gang_janghan", Name = "비풍도 강장한", Level = 5, IsBoss = true, HpMul = 1.5f, AtkMul = 0.51f,
                Moves = new[] { EnemyMove.Attack("도", 1.0f), EnemyMove.Attack("도", 1.1f), EnemyMove.Wind("도를 휘두를 자세를 잡는다"), EnemyMove.Attack("내려베기", 2.6f) } },
            // Stage 3: 호골채 (1권)
            new EnemyData { Id = "hogol_bandit", Name = "호골채 산적", Level = 6, HpMul = 0.8f, AtkMul = 0.65f,
                Moves = new[] { EnemyMove.Attack("휘두르기", 1.0f), EnemyMove.Attack("베기", 1.1f) } },
            new EnemyData { Id = "paeho_jung_gwang", Name = "패호 중광", Level = 8, IsBoss = true, HpMul = 1.6f, AtkMul = 0.72f,
                Moves = new[] { EnemyMove.Attack("내려치기", 1.1f), EnemyMove.Attack("연타", 0.7f, 3), EnemyMove.Wind("기합을 모은다"), EnemyMove.Attack("일격", 2.8f) } },
            // Stage 4: 용골채 (1권)
            new EnemyData { Id = "yonggol_bandit", Name = "용골채 산적", Level = 9, HpMul = 0.8f, AtkMul = 0.60f,
                Moves = new[] { EnemyMove.Attack("휘두르기", 1.0f), EnemyMove.Attack("베기", 1.1f) } },
            new EnemyData { Id = "paeryong_wooak", Name = "패룡 우악", Level = 11, IsBoss = true, HpMul = 1.7f, AtkMul = 0.67f,
                Moves = new[] { EnemyMove.Attack("내려치기", 1.1f), EnemyMove.Attack("연타", 0.7f, 3), EnemyMove.Wind("기세를 끌어올린다"), EnemyMove.Attack("패룡격", 3.0f) } },
            // Stage 5: 윤하루 (2권)
            new EnemyData { Id = "jang_ugang", Name = "장우강", Level = 12, HpMul = 0.9f, AtkMul = 1.04f,
                Moves = new[] { EnemyMove.Attack("청성검", 1.0f), EnemyMove.Attack("청성검", 1.2f), EnemyMove.Wind("검을 거둔다"), EnemyMove.Attack("찌르기", 2.2f) } },
            new EnemyData { Id = "yeomdo", Name = "염도 곽영희", Level = 14, IsBoss = true, HpMul = 1.8f, AtkMul = 1.21f,
                Moves = new[] { EnemyMove.Attack("진홍십칠염", 0.8f, 3), EnemyMove.Attack("검염기", 1.3f), EnemyMove.Wind("화령신공을 끌어올린다"), EnemyMove.Attack("화령염천도세", 3.2f) } },
            // Stage 6: 호아장 (2권)
            new EnemyData { Id = "hoajang_disciple", Name = "호아장 제자", Level = 15, HpMul = 0.85f, AtkMul = 0.74f,
                Moves = new[] { EnemyMove.Attack("맹호비격검", 1.0f), EnemyMove.Attack("맹호비격검", 1.1f) } },
            new EnemyData { Id = "ho_cheonsang", Name = "호아장주 호천상", Level = 17, IsBoss = true, HpMul = 1.7f, AtkMul = 0.86f,
                Moves = new[] { EnemyMove.Attack("맹호비격검", 1.0f, 2), EnemyMove.Attack("맹호비격검", 1.3f), EnemyMove.Wind("검기를 모은다"), EnemyMove.Attack("맹호삼십육검", 3.0f) } },
            // Stage 7: 운향정 (3권)
            new EnemyData { Id = "jongri_hak", Name = "종리학", Level = 18, HpMul = 0.9f, AtkMul = 0.84f,
                Moves = new[] { EnemyMove.Attack("매화검법", 1.0f, 2), EnemyMove.Attack("매화검법", 1.2f) } },
            new EnemyData { Id = "gu_chang", Name = "일도파쇄 구창", Level = 19, HpMul = 1.0f, AtkMul = 0.88f,
                Moves = new[] { EnemyMove.Attack("도", 1.1f), EnemyMove.Wind("도를 치켜든다"), EnemyMove.Attack("일도파쇄", 2.6f) } },
            new EnemyData { Id = "wiji_cheon", Name = "선풍검룡 위지천", Level = 21, IsBoss = true, HpMul = 1.9f, AtkMul = 0.97f,
                Moves = new[] { EnemyMove.Attack("절풍검기변", 0.8f, 3), EnemyMove.Attack("절풍검기변", 1.2f), EnemyMove.Wind("검강을 끌어올린다"), EnemyMove.Attack("천원삼절검격", 1.5f, 3) } },
        };

        public static readonly StageData[] Stages =
        {
            new StageData { Number = 1, Name = "아미산 숲", RecommendedLevel = 1, DooTotal = 300,
                Intro = new[] { "아미산 중턱 사부의 모옥. 장작 패기와 빨래로 위장된 수련이 몇 해째 이어지고 있다.", "숲에는 호랑이와 곰이 얼쩡거린다. 가죽과 웅담은 사부 몰래 챙기는 짭짤한 부수입이다.", "다만 숲 깊은 곳에는 백호가 있다고 한다." },
                Outro = new[] { "백호를 쓰러뜨렸다. 사부에게는 비밀로 해야 할 두둑한 수확이다." },
                Encounters = new[] { new[] { "tiger" }, new[] { "bear" }, new[] { "tiger", "tiger" }, new[] { "white_tiger" } } },
            new StageData { Number = 2, Name = "중양표국", RecommendedLevel = 4, DooTotal = 1400,
                Intro = new[] { "합숙훈련 중 당철영이 강장한 일행에게 집단구타를 당했다.", "비류연은 주작단 열다섯과 함께 중양표국으로 쳐들어간다. 목적은 두둑한 보상이다." },
                Outro = new[] { "국주 장우양까지 제압하고 거액의 보상을 받아냈다. 표국은 소문이 윤색된 덕에 오히려 호황을 맞는다고 한다." },
                Encounters = new[] { new[] { "biaosa" }, new[] { "biaosa", "biaosa" }, new[] { "gang_janghan", "biaosa" } } },
            new StageData { Number = 3, Name = "호골채 매복", RecommendedLevel = 7, DooTotal = 1600,
                Intro = new[] { "중양표국 사상 최대의 남창행 표행. 표물은 극비 고려청자 세 점이다.", "사천 경계 산길에서 녹림 72채 중 호골채 오백이 매복했다. 비류연이 앞길을 정리한다. (게임 연출)" },
                Outro = new[] { "호골채를 물리쳤다. 그런데 산적들은 극비 표물을 어떻게 알았을까?" },
                Encounters = new[] { new[] { "hogol_bandit", "hogol_bandit" }, new[] { "hogol_bandit", "hogol_bandit", "hogol_bandit" }, new[] { "paeho_jung_gwang", "hogol_bandit" } } },
            new StageData { Number = 4, Name = "용골채 매복", RecommendedLevel = 10, DooTotal = 2300,
                Intro = new[] { "호골채에 이어 용골채 천 명이 길을 막았다.", "표사 사상자는 한 명도 내서는 안 된다." },
                Outro = new[] { "표행은 무사히 이어진다. 주작단은 핏빛으로 물든 옷으로 남창 천무학관 정문에 도착한다." },
                Encounters = new[] { new[] { "yonggol_bandit", "yonggol_bandit" }, new[] { "yonggol_bandit", "yonggol_bandit", "yonggol_bandit" }, new[] { "paeryong_wooak", "yonggol_bandit" } } },
            new StageData { Number = 5, Name = "주주 윤하루", RecommendedLevel = 13, DooTotal = 4700,
                Intro = new[] { "새해 원단, 비류연은 사부의 봉뢰함과 뇌금을 들고 가출했다.", "주주의 객잔 윤하루에 사천제일 도객 염도가 나타났다. 그의 이름을 입에 담는 것은 금기다." },
                Outro = new[] { "여반장 한 방. 염도는 비류연의 '제자'가 되어 짐을 지고 따라나선다." },
                Encounters = new[] { new[] { "jang_ugang" }, new[] { "yeomdo" } } },
            new StageData { Number = 6, Name = "남창 호아장", RecommendedLevel = 16, DooTotal = 3000,
                Intro = new[] { "천무학관에 들어가려면 승룡패가 필요하다.", "염도는 무사부로, 비류연은 특별전형으로 입관한다. 호아장이 쥔 승룡패를 빼앗으러 간다." },
                Outro = new[] { "후견인 빙검 관철수가 나타났다. 염도와 일검을 나눈 끝에 승룡패가 넘어온다." },
                Encounters = new[] { new[] { "hoajang_disciple", "hoajang_disciple" }, new[] { "hoajang_disciple", "hoajang_disciple", "hoajang_disciple" }, new[] { "ho_cheonsang", "hoajang_disciple" } } },
            new StageData { Number = 7, Name = "천무학관 운향정", RecommendedLevel = 20, DooTotal = 3000,
                Intro = new[] { "수석으로 입관한 천무학관. 오검룡 이상만 드는 정자 운향정에서 밤하늘 같은 눈의 소녀와 마주쳤다.", "마음을 빼앗긴 사이, 칠검룡 위지천이 칼을 뽑았다." },
                Outro = new[] { "선풍뇌절 한 줄기가 위지천의 검을 쳐냈다. 손목은 남겨 두었다. 사람들이 몰려오기 전에 자리를 뜬다." },
                Encounters = new[] { new[] { "jongri_hak" }, new[] { "gu_chang" }, new[] { "jongri_hak", "gu_chang" }, new[] { "wiji_cheon" } } },
        };

        private static long CumulativeExp(int level)
        {
            long sum = 0;
            for (int l = 1; l < level; l++) sum += Progression.ExpToNext(l);
            return sum;
        }

        private static int RewardWeight(string enemyId)
        {
            var e = FindEnemy(enemyId);
            return e != null && e.IsBoss ? 3 : 1;
        }

        private static int TotalWeight(StageData st)
        {
            int w = 0;
            foreach (var enc in st.Encounters) foreach (var id in enc) w += RewardWeight(id);
            return w;
        }

        /// <summary>Exp for one enemy: the stage pays out exactly the levels between its recommended level and the next stage's.</summary>
        public static long ExpReward(StageData st, string enemyId)
        {
            var next = FindStage(st.Number + 1);
            int to = next != null ? next.RecommendedLevel : st.RecommendedLevel + 3;
            long total = CumulativeExp(to) - CumulativeExp(st.RecommendedLevel);
            return (long)Math.Ceiling((double)total * RewardWeight(enemyId) / TotalWeight(st));
        }

        public static long DooReward(StageData st, string enemyId)
        {
            return (long)Math.Round((double)st.DooTotal * RewardWeight(enemyId) / TotalWeight(st));
        }

        public static EnemyData FindEnemy(string id)
        {
            foreach (var e in Enemies) if (e.Id == id) return e;
            return null;
        }

        public static ArtData FindArt(string id)
        {
            foreach (var a in Arts) if (a.Id == id) return a;
            return null;
        }

        public static StageData FindStage(int number)
        {
            foreach (var s in Stages) if (s.Number == number) return s;
            return null;
        }
    }
}
