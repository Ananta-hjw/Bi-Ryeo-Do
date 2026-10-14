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
        public float HealPct;
        public long LearnCost;
        public string OwnerId;   // null = a martial art the protagonist can learn; otherwise a companion's fixed art

        public SkillSpec ToSpec()
        {
            return new SkillSpec
            {
                Id = Id, Name = Name, Description = Description, Power = Power, Hits = Hits, QiCost = QiCost,
                SpiritCost = SpiritCost, Cooldown = Cooldown, AllEnemies = AllEnemies, Effect = Effect, HealPct = HealPct
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
            c.IsBoss = IsBoss;
            c.Moves = Moves != null && Moves.Length > 0 ? new List<EnemyMove>(Moves) : new List<EnemyMove> { EnemyMove.Plain };
            return c;
        }

    }

    /// <summary>A party member who joins at <see cref="JoinStage"/>. Stats follow the protagonist's level; arts are fixed.</summary>
    public sealed class CompanionData
    {
        public string Id, Name, Role;
        public int JoinStage, SpeedAdd;
        public float HpMul = 1f, AtkMul = 1f, DefMul = 1f;

        public Combatant CreateCombatant(int level, int life)
        {
            var b = Progression.StatsFor(level, life);
            var stats = new CombatStats
            {
                MaxHp = (int)Math.Round(b.MaxHp * HpMul),
                MaxQi = b.MaxQi,
                Attack = (int)Math.Round(b.Attack * AtkMul),
                Defense = (int)Math.Round(b.Defense * DefMul),
                Speed = b.Speed + SpeedAdd,
                CritRate = 0.02f,
                CritDamage = 1.5f
            };
            var skills = new List<SkillSpec>();
            foreach (var a in EarlyGameContent.Arts) if (a.OwnerId == Id) skills.Add(a.ToSpec());
            return new Combatant(Id, Name, Team.Player, stats, skills);
        }
    }

    /// <summary>A short story-only scene (no fight). Plays before a stage's Intro until the stage is first cleared.</summary>
    public sealed class StoryScene
    {
        public string Title;
        public string[] Lines;
    }

    public sealed class StageData
    {
        public int Number;
        public string Name;
        /// <summary>Optional story scenes shown before Intro, only while the stage has not been cleared yet (bridges the volumes' plot between fights).</summary>
        public StoryScene[] Prologue;
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
            // Companions' fixed arts (names are descriptive unless the original text names the technique)
            new ArtData { Id = "xr_twin", Name = "쌍검 연격", Description = "쌍둥이 검으로 두 번 벤다.", Power = 1.5f, Hits = 2, QiCost = 18, Cooldown = 1, OwnerId = "hyo_ryong" },
            new ArtData { Id = "xr_barrier", Name = "태을천강벽", Description = "검막을 펼쳐 모두를 방어 태세로 만든다.", Effect = SkillEffect.GuardParty, QiCost = 30, Cooldown = 4, OwnerId = "hyo_ryong" },
            new ArtData { Id = "jh_slash", Name = "검", Description = "정석적인 검격.", Power = 1.9f, QiCost = 14, Cooldown = 1, OwnerId = "jang_hong" },
            new ArtData { Id = "jh_stun", Name = "점혈", Description = "혈도를 짚어 상대를 굳힌다. 기를 모으는 중이면 끊는다.", Power = 0.8f, Effect = SkillEffect.Stun, QiCost = 20, Cooldown = 3, OwnerId = "jang_hong" },
            new ArtData { Id = "yj_slash", Name = "화산의 검", Description = "화산파의 기본 검격. 매화검법은 알레르기 때문에 쓰지 못한다.", Power = 2.2f, QiCost = 16, Cooldown = 1, OwnerId = "yun_junho" },
            new ArtData { Id = "yj_heal", Name = "자하신공", Description = "화산의 자색 기운으로 가장 다친 동료를 치유한다.", Effect = SkillEffect.HealAlly, HealPct = 0.35f, QiCost = 28, Cooldown = 3, OwnerId = "yun_junho" },
        };

        public static readonly CompanionData[] Companions =
        {
            new CompanionData { Id = "hyo_ryong", Name = "효룡", Role = "쌍검, 검막으로 파티를 지킨다", JoinStage = 7, HpMul = 1.0f, AtkMul = 1.0f, DefMul = 1.0f, SpeedAdd = 4 },
            new CompanionData { Id = "jang_hong", Name = "장홍", Role = "점혈로 적의 강타를 끊는다", JoinStage = 7, HpMul = 1.1f, AtkMul = 0.85f, DefMul = 1.1f, SpeedAdd = 0 },
            new CompanionData { Id = "yun_junho", Name = "윤준호", Role = "자하신공으로 동료를 치유한다", JoinStage = 8, HpMul = 0.9f, AtkMul = 0.95f, DefMul = 0.85f, SpeedAdd = 2 },
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
            // Stage 7: 검혼관 기관 노방 (2권)
            new EnemyData { Id = "trap_arrow", Name = "기관 화살대", Level = 19, HpMul = 1.20f, AtkMul = 2.79f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("화살 연사", 0.6f, 3), EnemyMove.Attack("화살", 1.0f) } },
            new EnemyData { Id = "trap_needle", Name = "기관 비침대", Level = 19, HpMul = 1.20f, AtkMul = 2.79f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("비침", 0.8f, 2), EnemyMove.Wind("비침을 장전한다"), EnemyMove.Attack("비침 난사", 0.9f, 4) } },
            new EnemyData { Id = "trap_log", Name = "기관 통나무", Level = 20, HpMul = 1.80f, AtkMul = 3.11f, SpeedAdd = -8,
                Moves = new[] { EnemyMove.Wind("통나무가 흔들린다"), EnemyMove.Attack("통나무 휘두르기", 2.4f) } },
            new EnemyData { Id = "ho_cheongang", Name = "호아장 대사형 호천강", Level = 21, IsBoss = true, HpMul = 2.70f, AtkMul = 3.11f,
                Moves = new[] { EnemyMove.Attack("맹호삼십육검", 0.7f, 3), EnemyMove.Attack("패왕노호검", 1.3f), EnemyMove.Wind("패왕노호검을 치켜든다"), EnemyMove.Attack("패왕노호검 일격", 3.0f) } },
            // Stage 8: 천무식당 (3권)
            new EnemyData { Id = "jongri_hak", Name = "종리학", Level = 21, HpMul = 1.35f, AtkMul = 3.22f,
                Moves = new[] { EnemyMove.Attack("매화검법", 0.8f, 2), EnemyMove.Attack("매화검법", 1.2f) } },
            new EnemyData { Id = "chu_gayeon", Name = "추가연", Level = 23, IsBoss = true, HpMul = 2.55f, AtkMul = 3.39f,
                Moves = new[] { EnemyMove.Attack("매화검법", 0.8f, 3), EnemyMove.Attack("매화검법", 1.2f), EnemyMove.Wind("검을 모은다"), EnemyMove.Attack("매화만개", 3.0f) } },
            // Stage 9: 운향정 (3권)
            new EnemyData { Id = "gu_chang", Name = "일도파쇄 구창", Level = 23, HpMul = 1.50f, AtkMul = 4.83f,
                Moves = new[] { EnemyMove.Attack("도", 1.1f), EnemyMove.Wind("도를 치켜든다"), EnemyMove.Attack("일도파쇄", 2.6f) } },
            new EnemyData { Id = "so_cheongun", Name = "청풍검 소천군", Level = 24, HpMul = 1.43f, AtkMul = 4.83f,
                Moves = new[] { EnemyMove.Attack("청성검", 0.7f, 3), EnemyMove.Attack("청성검", 1.3f), EnemyMove.Wind("무형지기를 끌어올린다"), EnemyMove.Attack("검풍", 2.6f) } },
            new EnemyData { Id = "wiji_cheon", Name = "선풍검룡 위지천", Level = 26, IsBoss = true, HpMul = 2.85f, AtkMul = 5.08f,
                Moves = new[] { EnemyMove.Attack("절풍검기변", 0.7f, 3), EnemyMove.Attack("절풍검기변", 1.2f), EnemyMove.Wind("검강을 끌어올린다"), EnemyMove.Attack("천원삼절검격", 1.5f, 3) } },
            // Stage 10: 백향관 (4권)
            new EnemyData { Id = "sunyeo", Name = "독한녀 십이선녀", Level = 25, HpMul = 1.5f, AtkMul = 4.52f,
                Moves = new[] { EnemyMove.Attack("독침", 0.7f, 3), EnemyMove.Attack("난향장", 1.2f), EnemyMove.Wind("독향을 퍼뜨린다"), EnemyMove.Attack("절독난무", 2.6f) } },
            new EnemyData { Id = "na_yerin", Name = "빙백봉 나예린", Level = 27, IsBoss = true, HpMul = 3.0f, AtkMul = 4.70f,
                Moves = new[] { EnemyMove.Attack("백뢰낙화", 0.7f, 3), EnemyMove.Attack("섬뢰관일", 1.3f), EnemyMove.Wind("백광검뢰를 모은다"), EnemyMove.Attack("참천무뢰", 3.0f) } },
            // Stage 11: 승급시험 (4권)
            new EnemyData { Id = "ha_sein", Name = "철검비룡 하세인", Level = 27, HpMul = 1.6f, AtkMul = 3.92f,
                Moves = new[] { EnemyMove.Attack("철비룡검", 1.0f), EnemyMove.Wind("검기를 맺는다"), EnemyMove.Attack("징계의 일검", 2.6f) } },
            new EnemyData { Id = "gang_hyeonchu", Name = "영취검 강현추", Level = 28, HpMul = 1.5f, AtkMul = 3.92f,
                Moves = new[] { EnemyMove.Attack("무산검", 0.8f, 2), EnemyMove.Attack("무산검", 1.2f) } },
            new EnemyData { Id = "dan_daepung", Name = "철검비룡 단대풍", Level = 29, IsBoss = true, HpMul = 3.0f, AtkMul = 4.14f,
                Moves = new[] { EnemyMove.Attack("점창검", 0.7f, 3), EnemyMove.Attack("점창검", 1.2f), EnemyMove.Wind("살기를 끌어올린다"), EnemyMove.Attack("살기 어린 일검", 3.0f) } },
            // Stage 12: 삼성무제 본선 (5권)
            new EnemyData { Id = "dan_pyeong", Name = "쾌환검 단평", Level = 29, HpMul = 1.5f, AtkMul = 4.35f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("남해 36검", 0.6f, 3), EnemyMove.Attack("쾌검살초", 1.3f) } },
            new EnemyData { Id = "eonga_yeong", Name = "파풍창 언가영", Level = 30, HpMul = 1.6f, AtkMul = 3.25f,
                Moves = new[] { EnemyMove.Attack("언가묵창", 1.1f), EnemyMove.Wind("창을 거둔다"), EnemyMove.Attack("파풍창", 2.6f) } },
            new EnemyData { Id = "jeon_okgi", Name = "팔비신검 전옥기", Level = 31, IsBoss = true, HpMul = 3.0f, AtkMul = 4.49f,
                Moves = new[] { EnemyMove.Attack("팔비검", 0.6f, 4), EnemyMove.Attack("점창검", 1.2f), EnemyMove.Wind("검을 모은다"), EnemyMove.Attack("팔비신검", 3.0f) } },
            // Stage 13: 삼성대전 결승 (5권)
            new EnemyData { Id = "dang_munceon", Name = "독랄수 당문천", Level = 31, HpMul = 1.7f, AtkMul = 4.86f,
                Moves = new[] { EnemyMove.Attack("삼환비선", 0.7f, 3), EnemyMove.Attack("묵편", 1.2f), EnemyMove.Wind("독을 푼다"), EnemyMove.Attack("독편 난무", 2.6f) } },
            new EnemyData { Id = "wiji_cheon_baek", Name = "선풍검룡 위지천(백일연무)", Level = 33, IsBoss = true, HpMul = 3.4f, AtkMul = 5.08f,
                Moves = new[] { EnemyMove.Attack("선풍우뢰", 0.7f, 3), EnemyMove.Attack("선풍참혼", 1.3f), EnemyMove.Wind("검강을 끌어올린다"), EnemyMove.Attack("천풍마뢰참", 3.2f) } },
            // Stage 14: 무당산 고립 (7권)
            new EnemyData { Id = "chohon_gwigeom", Name = "초혼오귀검 귀영살검", Level = 32, HpMul = 1.5f, AtkMul = 2.86f,
                Moves = new[] { EnemyMove.Attack("암습검", 0.8f, 2), EnemyMove.Attack("소도", 1.2f) } },
            new EnemyData { Id = "myeongbu_jang", Name = "명부오귀장 명염도 추항", Level = 33, HpMul = 1.7f, AtkMul = 2.86f,
                Moves = new[] { EnemyMove.Attack("명부도", 1.0f), EnemyMove.Wind("도를 높이 든다"), EnemyMove.Attack("명염참", 2.6f) } },
            new EnemyData { Id = "gal_hyobong", Name = "혈류도 갈효봉(폭주)", Level = 34, HpMul = 3.2f, AtkMul = 3.05f,
                Moves = new[] { EnemyMove.Attack("광무쌍도", 0.7f, 3), EnemyMove.Attack("혈풍무", 1.3f), EnemyMove.Wind("혈광을 일으킨다"), EnemyMove.Attack("굉천혈류패왕십자인", 3.2f) } },
            new EnemyData { Id = "cheonsal", Name = "천살 초혼검", Level = 35, IsBoss = true, HpMul = 3.0f, AtkMul = 3.05f,
                Moves = new[] { EnemyMove.Attack("초혼귀령검", 0.7f, 3), EnemyMove.Attack("소혼붕렬", 1.3f), EnemyMove.Wind("초혼령을 울린다"), EnemyMove.Attack("귀령압혼", 3.0f) } },
            new EnemyData { Id = "jisal", Name = "지살 명왕도", Level = 35, IsBoss = true, HpMul = 3.0f, AtkMul = 3.05f,
                Moves = new[] { EnemyMove.Attack("명왕도", 1.0f), EnemyMove.Attack("명왕도", 1.2f), EnemyMove.Wind("웃는 얼굴이 굳는다"), EnemyMove.Attack("명왕참", 2.8f) } },
            // Stage 15: 철각비마대 (8권, 회상)
            new EnemyData { Id = "sadae_jilpung", Name = "사대질풍 철기병", Level = 35, HpMul = 0.96f, AtkMul = 2.67f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("철쇄선풍", 0.8f, 2), EnemyMove.Attack("거창", 1.2f) } },
            new EnemyData { Id = "cheolbi_gyeok", Name = "철비십이격 창기병", Level = 36, HpMul = 1.02f, AtkMul = 2.67f,
                Moves = new[] { EnemyMove.Attack("십자투창", 0.7f, 3), EnemyMove.Wind("창을 허리에 건다"), EnemyMove.Attack("기창충격", 2.6f) } },
            new EnemyData { Id = "wi_musang", Name = "철각비마 위무상", Level = 36, HpMul = 1.2f, AtkMul = 2.67f,
                Moves = new[] { EnemyMove.Attack("삼단질풍격", 0.7f, 3), EnemyMove.Attack("거창돌격", 1.3f) } },
            new EnemyData { Id = "gu_cheonhak", Name = "질풍묵흔 구천학", Level = 37, IsBoss = true, HpMul = 1.92f, AtkMul = 2.73f,
                Moves = new[] { EnemyMove.Attack("허공격상", 0.7f, 3), EnemyMove.Attack("파랑분쇄격", 1.3f), EnemyMove.Wind("창을 머리 위로 든다"), EnemyMove.Attack("무영인", 3.2f) } },
            // Stage 16: 저잣거리 습격 (8권)
            new EnemyData { Id = "baegin_jagaek", Name = "백인회 변장 자객", Level = 36, HpMul = 1.4f, AtkMul = 2.4f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("암기", 0.6f, 3), EnemyMove.Attack("암습", 1.3f) } },
            new EnemyData { Id = "ssanggwi", Name = "이인일체 쌍귀 형제", Level = 37, HpMul = 2.0f, AtkMul = 2.45f,
                Moves = new[] { EnemyMove.Attack("쌍도", 1.0f), EnemyMove.Wind("배 속에서 손이 튀어나온다"), EnemyMove.Attack("이인일체 비수", 2.8f) } },
            new EnemyData { Id = "baek_tanjeo", Name = "백인회주 관살 백탄저", Level = 37, IsBoss = true, HpMul = 3.0f, AtkMul = 2.45f,
                Moves = new[] { EnemyMove.Attack("독검", 0.7f, 3), EnemyMove.Attack("극독 일섬", 1.3f), EnemyMove.Wind("독검을 뽑아 든다"), EnemyMove.Attack("맹독 난자", 3.0f) } },
            // Stage 17: 마하령과 군웅회 (9권)
            new EnemyData { Id = "sinung_daewon", Name = "신응대 대원", Level = 38, HpMul = 1.6f, AtkMul = 2.84f,
                Moves = new[] { EnemyMove.Attack("철벽진", 0.8f, 2), EnemyMove.Attack("도격", 1.2f) } },
            new EnemyData { Id = "pae_hyeokseong", Name = "광풍맹호도 팽혁성", Level = 39, HpMul = 2.0f, AtkMul = 2.84f,
                Moves = new[] { EnemyMove.Attack("오호단문도", 1.0f), EnemyMove.Wind("맹호출림의 자세"), EnemyMove.Attack("맹호출림", 2.8f) } },
            new EnemyData { Id = "ma_haryeong", Name = "철옥잠 마하령", Level = 40, IsBoss = true, HpMul = 3.2f, AtkMul = 2.89f,
                Moves = new[] { EnemyMove.Attack("표류무상도", 0.7f, 3), EnemyMove.Attack("철옥참", 1.3f), EnemyMove.Wind("천축유가신공을 끌어올린다"), EnemyMove.Attack("표류무상참", 3.2f) } },
            // Stage 18: 태극의 인재 (9권)
            new EnemyData { Id = "yong_cheonmyeong", Name = "창천룡 용천명", Level = 40, HpMul = 2.5f, AtkMul = 5.59f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("백룡검", 0.7f, 3), EnemyMove.Attack("달마검", 1.3f), EnemyMove.Wind("반야신공을 일으킨다"), EnemyMove.Attack("여래일검", 3.0f) } },
            new EnemyData { Id = "gwan_cheolsu", Name = "빙검 관철수", Level = 42, IsBoss = true, HpMul = 3.4f, AtkMul = 5.7f,
                Moves = new[] { EnemyMove.Attack("빙령수혼", 0.7f, 3), EnemyMove.Attack("백빙신침", 1.3f), EnemyMove.Wind("한빙지기를 끌어올린다"), EnemyMove.Attack("백빙신정", 3.4f) } },
        };

        // Items. Names come from volumes 1-3; effects and numbers are placeholders.
        public static readonly ItemData[] Items =
        {
            new ItemData { Id = "bear_gall", Name = "웅담", Kind = ItemKind.Consumable, HealPct = 0.40f, Description = "곰의 쓸개. 가장 다친 동료의 체력을 40% 회복한다." },
            new ItemData { Id = "okruju", Name = "옥루주", Kind = ItemKind.Consumable, QiRestore = 60, SpiritGain = 25, Description = "사천제일주. 내공 60과 기세 25를 얻는다." },
            new ItemData { Id = "ginseng", Name = "인형설삼", Kind = ItemKind.Consumable, HealPct = 1.0f, QiRestore = 999, Description = "사람 모양의 영약 산삼. 가장 다친 동료를 완전히 회복하고 내공을 가득 채운다." },
            new ItemData { Id = "leather", Name = "가죽", Kind = ItemKind.Material, SellValue = 70, Description = "사냥한 짐승의 가죽. 팔면 두가 된다." },
            new ItemData { Id = "laundry_bat", Name = "강철 빨래방망이", Kind = ItemKind.Weapon, AtkPct = 0.08f, Description = "수련용 강철 방망이(50근). 무기라기엔 빨래용이다. 공격 +8%." },
            new ItemData { Id = "jin_biroedo", Name = "진비뢰도", Kind = ItemKind.Weapon, AtkPct = 0.22f, CritAdd = 0.04f, Description = "봉뢰함에 든 진짜 비뢰도 열 자루. 공격 +22%, 치명타 +4%." },
            new ItemData { Id = "mukryonghwan", Name = "묵룡환", Kind = ItemKind.Accessory, AtkPct = 0.10f, DefPct = 0.10f, SpeedAdd = -6, Description = "사부가 11세부터 채운 증량 족쇄. 몸은 무거워지지만 단련된다. 공격·방어 +10%, 속도 -6." },
            new ItemData { Id = "roegeum", Name = "뇌금 묵뢰", Kind = ItemKind.Accessory, QiPct = 0.30f, HpPct = 0.05f, Description = "뇌령사 현을 쓴 검은 칠현금. 내공 +30%, 체력 +5%." },
            new ItemData { Id = "claw_necklace", Name = "발톱 목걸이", Kind = ItemKind.Accessory, CritAdd = 0.05f, SpeedAdd = 2, Description = "사냥한 짐승의 발톱을 꿴 목걸이. 치명타 +5%, 속도 +2." },
            new ItemData { Id = "ilgeup_yeongdan", Name = "일급영단", Kind = ItemKind.Consumable, HealPct = 1.0f, QiRestore = 999, SpiritGain = 100, Description = "삼성무제 우승 부상. 가장 다친 동료를 완전히 회복하고 내공을 채우며 기세를 100까지 끌어올린다." },
            new ItemData { Id = "seungryong_pae", Name = "승룡패", Kind = ItemKind.Key, Description = "녹옥으로 된 추천패. 천무학관 특별전형 입관의 증표." },
        };

        public static readonly DropData[] Drops =
        {
            new DropData { EnemyId = "tiger", ItemId = "leather", Chance = 0.60f },
            new DropData { EnemyId = "tiger", ItemId = "claw_necklace", Chance = 0.04f },
            new DropData { EnemyId = "bear", ItemId = "bear_gall", Chance = 0.35f },
            new DropData { EnemyId = "bear", ItemId = "leather", Chance = 0.50f },
            new DropData { EnemyId = "bear", ItemId = "ginseng", Chance = 0.04f },
            new DropData { EnemyId = "white_tiger", ItemId = "leather", Chance = 1.0f, Count = 3 },
            new DropData { EnemyId = "white_tiger", ItemId = "bear_gall", Chance = 0.50f },
            new DropData { EnemyId = "biaosa", ItemId = "okruju", Chance = 0.12f },
            new DropData { EnemyId = "gang_janghan", ItemId = "okruju", Chance = 0.60f },
            new DropData { EnemyId = "hogol_bandit", ItemId = "okruju", Chance = 0.15f },
            new DropData { EnemyId = "paeho_jung_gwang", ItemId = "bear_gall", Chance = 0.60f },
            new DropData { EnemyId = "yonggol_bandit", ItemId = "okruju", Chance = 0.15f },
            new DropData { EnemyId = "paeryong_wooak", ItemId = "ginseng", Chance = 0.30f },
            new DropData { EnemyId = "yeomdo", ItemId = "ginseng", Chance = 0.35f },
            new DropData { EnemyId = "ho_cheonsang", ItemId = "bear_gall", Chance = 0.60f },
            new DropData { EnemyId = "ho_cheongang", ItemId = "okruju", Chance = 0.60f },
            new DropData { EnemyId = "chu_gayeon", ItemId = "ginseng", Chance = 0.30f },
            new DropData { EnemyId = "wiji_cheon", ItemId = "ginseng", Chance = 0.50f },
            new DropData { EnemyId = "na_yerin", ItemId = "okruju", Chance = 0.50f },
            new DropData { EnemyId = "dan_daepung", ItemId = "bear_gall", Chance = 0.60f },
            new DropData { EnemyId = "jeon_okgi", ItemId = "ginseng", Chance = 0.30f },
            new DropData { EnemyId = "wiji_cheon_baek", ItemId = "ginseng", Chance = 0.60f },
            new DropData { EnemyId = "gal_hyobong", ItemId = "ginseng", Chance = 0.40f },
            new DropData { EnemyId = "cheonsal", ItemId = "okruju", Chance = 0.60f },
            new DropData { EnemyId = "jisal", ItemId = "bear_gall", Chance = 0.60f },
            new DropData { EnemyId = "gu_cheonhak", ItemId = "ginseng", Chance = 0.50f },
            new DropData { EnemyId = "baek_tanjeo", ItemId = "okruju", Chance = 0.60f },
            new DropData { EnemyId = "ma_haryeong", ItemId = "ginseng", Chance = 0.50f },
            new DropData { EnemyId = "gwan_cheolsu", ItemId = "ilgeup_yeongdan", Chance = 0.20f },
        };

        /// <summary>One-time rewards for clearing a stage for the first time.</summary>
        public static readonly Dictionary<int, ItemStack[]> StageRewards = new Dictionary<int, ItemStack[]>
        {
            { 1, new[] { new ItemStack("bear_gall", 2) } },
            { 2, new[] { new ItemStack("okruju", 2) } },
            { 3, new[] { new ItemStack("ginseng", 1) } },
            { 4, new[] { new ItemStack("bear_gall", 3) } },
            { 5, new[] { new ItemStack("jin_biroedo", 1), new ItemStack("roegeum", 1) } },   // 봉뢰함과 뇌금을 들고 가출
            { 6, new[] { new ItemStack("seungryong_pae", 1), new ItemStack("okruju", 2) } },
            { 7, new[] { new ItemStack("ginseng", 1) } },
            { 8, new[] { new ItemStack("bear_gall", 3) } },
            { 9, new[] { new ItemStack("ginseng", 2) } },
            { 10, new[] { new ItemStack("okruju", 3) } },
            { 11, new[] { new ItemStack("ginseng", 1), new ItemStack("bear_gall", 2) } },
            { 12, new[] { new ItemStack("ginseng", 2) } },
            { 13, new[] { new ItemStack("ilgeup_yeongdan", 1), new ItemStack("ginseng", 2) } },   // 삼성무제 우승 부상
            { 14, new[] { new ItemStack("ginseng", 2), new ItemStack("okruju", 2) } },
            { 15, new[] { new ItemStack("ginseng", 2), new ItemStack("bear_gall", 3) } },
            { 16, new[] { new ItemStack("ginseng", 2), new ItemStack("okruju", 3) } },
            { 17, new[] { new ItemStack("ilgeup_yeongdan", 1), new ItemStack("bear_gall", 3) } },
            { 18, new[] { new ItemStack("ilgeup_yeongdan", 2), new ItemStack("ginseng", 3) } },   // 태극의 인재 시험
        };

        /// <summary>What the protagonist starts with.</summary>
        public static readonly ItemStack[] StartKit =
        {
            new ItemStack("mukryonghwan", 1), new ItemStack("laundry_bat", 1), new ItemStack("bear_gall", 2), new ItemStack("okruju", 1),
        };

        public static ItemData FindItem(string id)
        {
            foreach (var i in Items) if (i.Id == id) return i;
            return null;
        }

        public static readonly StageData[] Stages =
        {
            new StageData { Number = 1, Name = "아미산 숲", RecommendedLevel = 1, DooTotal = 300,
                Intro = new[] { "아미산 중턱 사부의 모옥. 장작 패기와 빨래로 위장된 수련이 몇 해째 이어지고 있다.", "숲에는 호랑이와 곰이 얼쩡거린다. 가죽과 웅담은 사부 몰래 챙기는 짭짤한 부수입이다.", "다만 숲 깊은 곳에는 백호가 있다고 한다." },
                Outro = new[] { "백호를 쓰러뜨렸다. 사부에게는 비밀로 해야 할 두둑한 수확이다." },
                Encounters = new[] { new[] { "tiger" }, new[] { "bear" }, new[] { "tiger", "tiger" }, new[] { "white_tiger" } } },
            new StageData { Number = 2, Name = "중양표국", RecommendedLevel = 4, DooTotal = 1400,
                Prologue = new[]
                {
                    new StoryScene { Title = "진조운의 유품", Lines = new[] {
                        "열아홉 살 늦봄. 비류연은 폭포 밑에서 수중 수련을 하다 바위를 던졌다. 하필 그때 산을 오르던 사람이 있었다.",
                        "천무학관이 초빙한 교관 철담비환 진조운. 강호 백대고수급이었던 그는 말 한마디 못 하고 세상을 떠났다.",
                        "비류연은 시신 곁에서 서찰과 인피면구, 황금 열 냥을 챙겼다. \"내가 진조운이 되면 되겠네.\"",
                    } },
                    new StoryScene { Title = "아미산 합숙", Lines = new[] {
                        "천무아미수련원에 천무학관 주작단 열여섯 명이 도착했다. 남궁상, 현운, 진령, 노학, 당철영... 구파와 팔대세가의 후기지수들이다.",
                        "'노사부'로 변장한 비류연이 시킨 수련은 단순했다. 자신이 사부 밑에서 하던 그대로 장작을 패고, 빨래를 하고, 밥을 짓는 것.",
                        "반발하던 개방의 노학이 가장 먼저 삼복구타권법의 시범 대상이 되었다. 그날부터 아무도 노사부의 말에 토를 달지 않았다.",
                        "다섯 달 뒤, 주작단의 팔다리에는 묵환이 채워졌고 눈빛에는 두려움과 존경이 섞였다. 이별을 앞둔 밤, 남궁상은 진령과 입을 맞췄다고 한다.",
                    } },
                },
                Intro = new[] { "합숙훈련 중 당철영이 강장한 일행에게 집단구타를 당했다.", "비류연은 주작단 열다섯과 함께 중양표국으로 쳐들어간다. 목적은 두둑한 보상이다." },
                Outro = new[] { "국주 장우양까지 제압하고 거액의 보상을 받아냈다. 표국은 소문이 윤색된 덕에 오히려 호황을 맞는다고 한다." },
                Encounters = new[] { new[] { "biaosa" }, new[] { "biaosa", "biaosa" }, new[] { "gang_janghan", "biaosa" } } },
            new StageData { Number = 3, Name = "호골채 매복", RecommendedLevel = 7, DooTotal = 1600,
                Intro = new[] { "중양표국 사상 최대의 남창행 표행. 표물은 극비 고려청자 세 점이다.", "사천 경계 산길에서 녹림 72채 중 호골채 오백이 매복했다. 비류연이 앞길을 정리한다. (게임 연출)" },
                Outro = new[] { "호골채를 물리쳤다. 그런데 산적들은 극비 표물을 어떻게 알았을까?" },
                Encounters = new[] { new[] { "hogol_bandit", "hogol_bandit" }, new[] { "hogol_bandit", "hogol_bandit", "hogol_bandit" }, new[] { "paeho_jung_gwang", "hogol_bandit" } } },
            new StageData { Number = 4, Name = "용골채 매복", RecommendedLevel = 10, DooTotal = 2300,
                Intro = new[] { "호골채에 이어 용골채 천 명이 길을 막았다.", "표사 사상자는 한 명도 내서는 안 된다." },
                Outro = new[] { "표행은 무사히 이어진다. 주작단은 핏빛으로 물든 옷으로 남창 천무학관 정문에 도착한다.", "이별식에서 사제들을 떠나보낸 비류연은 홀로 아미산으로 돌아간다. 모옥에는 사부가 기다리고 있다." },
                Encounters = new[] { new[] { "yonggol_bandit", "yonggol_bandit" }, new[] { "yonggol_bandit", "yonggol_bandit", "yonggol_bandit" }, new[] { "paeryong_wooak", "yonggol_bandit" } } },
            new StageData { Number = 5, Name = "주주 윤하루", RecommendedLevel = 13, DooTotal = 4700,
                Prologue = new[]
                {
                    new StoryScene { Title = "모옥의 마지막 겨울", Lines = new[] {
                        "주작단을 보낸 지 한 달 남짓. 모옥의 점심상에서 사부가 수염 '백아'를 쓰다듬으며 눈을 가늘게 떴다.",
                        "사부: \"류연아, 요즘 밤마다 어디를 쏘다니느냐. 은자는 어디서 났고?\"",
                        "비류연은 옥루주 한 병을 슬며시 내밀었다. 사부의 의심은 술 한 잔에 녹아내렸다.",
                        "하지만 비류연의 마음은 정해져 있었다. 사부가 말하는 '뇌신의 힘'에는 아직 손이 닿지 않는다. 이 산에서는 더 배울 것이 없다.",
                        "새해 원단 새벽, 비류연은 사부 명의로 노후 연금을 들어 둔 뒤 봉뢰함과 뇌금 묵뢰를 짊어지고 모옥을 나섰다. 지금까지 쓰던 비뢰도는 연습용 가짜였다. 진짜 열 자루는 봉뢰함 안에 있다.",
                        "산 아래에서 중양표국의 남창행 표행과 마주쳤다. 비류연은 그 행렬 끝에 슬쩍 합류했다.",
                    } },
                },
                Intro = new[] { "새해 원단, 비류연은 사부의 봉뢰함과 뇌금을 들고 가출했다.", "주주의 객잔 윤하루에 사천제일 도객 염도가 나타났다. 그의 이름을 입에 담는 것은 금기다." },
                Outro = new[] { "여반장 한 방. 염도는 비류연의 '제자'가 되어 짐을 지고 따라나선다." },
                Encounters = new[] { new[] { "jang_ugang" }, new[] { "yeomdo" } } },
            new StageData { Number = 6, Name = "남창 호아장", RecommendedLevel = 16, DooTotal = 3000,
                Prologue = new[]
                {
                    new StoryScene { Title = "접수 마감", Lines = new[] {
                        "표행을 따라 남창에 도착한 비류연과 염도. 그런데 천무학관 승천무제(입관시험) 접수는 한 달 전에 마감되었고, 응시자는 만 스물다섯 살까지다.",
                        "비류연은 남창 정보조직 순풍당의 주인 경순이 나중해를 찾아가 '쌍낭이'라는 별명을 입에 올렸다. 금기를 건드린 대가로 나중해는 해법을 내놓았다.",
                        "염도는 학관이 모시고 싶어 하는 무사부로, 비류연은 추천패 승룡패를 쥔 특별전형으로 들어가면 된다. 문제는 승룡패가 호아장에 있다는 것이다.",
                    } },
                },
                Intro = new[] { "천무학관에 들어가려면 승룡패가 필요하다.", "염도는 무사부로, 비류연은 특별전형으로 입관한다. 호아장이 쥔 승룡패를 빼앗으러 간다." },
                Outro = new[] { "후견인 빙검 관철수가 나타났다. 염도와 일검을 나눈 끝에 승룡패가 넘어온다." },
                Encounters = new[] { new[] { "hoajang_disciple", "hoajang_disciple" }, new[] { "hoajang_disciple", "hoajang_disciple", "hoajang_disciple" }, new[] { "ho_cheonsang", "hoajang_disciple" } } },
            new StageData { Number = 7, Name = "천무학관 검혼관", RecommendedLevel = 19, DooTotal = 3500,
                Prologue = new[]
                {
                    new StoryScene { Title = "특별전형", Lines = new[] {
                        "학관은 염도를 무사부로 맞이했다. 비류연은 승룡패를 내밀고 특별전형을 치렀다.",
                        "시건방진 응시생 네 명이 시비를 걸었다. 비류연은 그들을 일어나지 못하게 만든 채 수석으로 합격했다.",
                        "대가로 비류연은 '요주의 인물'로 분류되었다. 본인은 별로 신경 쓰지 않았다.",
                    } },
                },
                Intro = new[] { "입관식 날, 지붕에서 낮잠을 자다 효룡과 친구가 되었다. 기숙사 검혼관으로 가는 길에 신입 장홍이 따라붙는다.", "효룡과 장홍이 함께한다. 기숙사 앞 기관 노방에서 호아장의 호천강이 복수의 칼을 뽑았다." },
                Outro = new[] { "호천강의 패왕노호검이 반 토막 났다. 기관이 3단계까지 가동되어 은자 1500냥의 피해가 났지만 정당방위로 무죄다.", "방 배정 결과, 701호의 룸메이트는 승천무제 수석 칠절신검 모용휘. 먼지 하나 못 참는 결벽증 천재다.", "비류연은 이 깔끔한 룸메이트를 어떻게 골려 줄지 벌써 궁리하기 시작했다. 일행은 저녁을 먹으러 공동식당으로 향한다." },
                Encounters = new[] { new[] { "trap_arrow" }, new[] { "trap_needle", "trap_arrow" }, new[] { "trap_log", "trap_needle" }, new[] { "ho_cheongang" } } },
            new StageData { Number = 8, Name = "천무식당", RecommendedLevel = 21, DooTotal = 3500,
                Intro = new[] { "첫 저녁, 천무식당. 화산파의 윤준호가 선배들에게 조롱을 당하고 있다. 매화검법도 칠매검도 쓰지 못한다는 이유다.", "비류연은 식판을 쥐었다. 윤준호가 일행에 합류한다. (싸움은 게임 연출)" },
                Outro = new[] { "모용휘가 나타나 싸움을 말렸다. 종리학과 추가연은 이를 갈며 물러났다.", "윤준호가 털어놓았다. 칠매동에서 검보를 얻어 수련하다 매화 향기를 보고 두드러기가 난 뒤로, 매화검법은 물론 칠매검도 쓸 수 없다고.", "효룡과 장홍이 말했다. 그것은 병이 아니라 검기검화 너머, 검향의 경지에 올랐다는 증거라고.", "비류연이 처방을 내렸다. \"알레르기를 고치려 하지 마. 검향을 넘어서는 이미지를 그려 봐. 약보다 낫다.\" 윤준호는 얼떨떨한 얼굴로 고개를 끄덕였다." },
                Encounters = new[] { new[] { "jongri_hak" }, new[] { "jongri_hak", "jongri_hak" }, new[] { "chu_gayeon", "jongri_hak" } } },
            new StageData { Number = 9, Name = "천무학관 운향정", RecommendedLevel = 23, DooTotal = 4000,
                Prologue = new[]
                {
                    new StoryScene { Title = "수강신청과 우뢰", Lines = new[] {
                        "수강편람은 베개만큼 두껍다. 비류연이 고른 전공은 음공. 사문에서 가져온 묵금을 쓰기 위해서고, 효룡도 따라 들었다. 담당은 천음선자 홍란이다.",
                        "천응방 분타에서 30명이 넘게 실패한 푸른 깃털의 고려산 해동청을 비류연은 순순히 길들였다. 이름은 '우뢰'. 전서응이 생겼다.",
                    } },
                    new StoryScene { Title = "무도총론", Lines = new[] {
                        "무당의 옥현진인이 백 년 전 이야기를 꺼냈다. 흑백을 가리지 않고 짓밟은 절대자 위천무, 천겁혈신.",
                        "백도의 무림맹과 흑도의 흑천맹이 연합했지만 크게 패했다. 태극신군 무신 혁월린과 패천도 무신마 갈중혁이 끝까지 맞섰고, 혈신은 부상을 입고 종적을 감췄다.",
                        "이후 혁월린은 천무삼성과 천무학관을, 갈중혁은 마천각을 세웠다. 비류연은 천겁령도 위천무도 몰라서 숙제로 백 년사 조사를 받았다.",
                    } },
                    new StoryScene { Title = "애소저회", Lines = new[] {
                        "동호회 거리에서 구정회의 그림자 검혼회와 도백회가 으르렁거렸다. 그 틈에서 3학년 진성곤 임성진이 곤으로 둘을 갈라놓고 비류연 일행을 끌고 갔다.",
                        "간판은 애소저회. 천관 미인들의 정보를 수집하는 동호회다. '정보가 돈이 된다'는 말에 비류연은 그 자리에서 가입했고, 효룡·장홍·윤준호도 반강제로 따라 들어갔다.",
                    } },
                    new StoryScene { Title = "운향정의 소녀", Lines = new[] {
                        "학관을 둘러보던 비류연은 오검룡 이상만 드는 정자 운향정에서 걸음을 멈췄다. 밤하늘을 담은 듯한 눈을 한 소녀가 홀로 금을 타고 있었다.",
                        "소녀의 곡이 끝나자 비류연은 자기도 모르게 다가갔다. 그리고 허락도 없이 그녀의 입술을 훔쳤다.",
                        "소녀의 검이 번쩍였다. 비류연은 한 걸음 물러났고, 검기는 그의 앞섶만 갈랐다. 소녀는 평소와 달리 눈앞의 남자가 무슨 생각을 하는지 읽을 수 없었다.",
                        "(훗날 비류연은 그녀가 천하제일미로 불리는 빙백봉 나예린이라는 것을 알게 된다. 지금은 이름도 묻지 못했다.)",
                    } },
                },
                Intro = new[] { "수석으로 입관한 천무학관. 오검룡 이상만 드는 정자 운향정. 방금 일어난 일을 곱씹을 틈도 없이 인기척이 몰려온다.", "소녀의 호위대장 선풍검룡 위지천이 칼을 뽑았다. 길을 막는 선배들이 먼저 달려든다. (게임 연출)" },
                Outro = new[] { "선풍뇌절 한 줄기가 위지천의 검을 쳐냈다. 손목은 남겨 두었다.", "소녀는 어느새 사라지고 없었다. 이름조차 묻지 못한 것이 못내 아쉽다. 사람들이 몰려오기 전에 자리를 뜬다.", "며칠 뒤 학관 정문. 3개월 과제를 마친 청룡단과 주작단 열여섯이 반 각 차로 귀환했다. 비류연은 제자들을 다시 철저히 단련시키기로 마음먹는다." },
                Encounters = new[] { new[] { "gu_chang" }, new[] { "so_cheongun" }, new[] { "gu_chang", "so_cheongun" }, new[] { "wiji_cheon" } } },
            new StageData { Number = 10, Name = "백향관 침투", RecommendedLevel = 25, DooTotal = 4200,
                Prologue = new[]
                {
                    new StoryScene { Title = "빙백봉의 정체", Lines = new[] {
                        "운향정 사건 이후 일주일. 위지천이 빙봉영화수호대를 소집해 나예린의 초상화를 내리고 비류연의 수배 초상화를 걸어 검기로 찢었다. 모두의 공적이 된 것이다.",
                        "비류연은 애소저회 비연태에게서 소녀의 정체를 알아냈다. 천하제일미 빙백봉 나예린. 무림맹주 나백천의 손녀이자 검후 이옥상의 의발전인이다.",
                        "\"포기할 마음은 없는데요. 오히려 해볼 마음이 드네.\" 비류연이 씩 웃었다.",
                    } },
                    new StoryScene { Title = "음율관의 홍란", Lines = new[] {
                        "방음이 완비된 음율관 3층. 천음선자 홍란의 음공 수업에는 비류연도, 음공과 검기의 흐름을 비교하러 온 모용휘도, 효룡도 앉아 있다.",
                        "홍란이 묵금 연주를 청했지만 비류연은 거절했다. 홍란이 살짝 건드린 묵금의 뇌령사 현에 손끝이 베여 피가 맺혔다. 홍란은 이날부터 이 1학년을 눈여겨보게 된다.",
                    } },
                    new StoryScene { Title = "새벽의 나예린", Lines = new[] {
                        "새벽, 나예린은 풀잎의 이슬 한 방울을 검기로 세 갈래로 가른다. 사저 독고령이 찾아와 위지천을 방패막이로 둔 일을 두고 말을 건넨다.",
                        "나예린: \"지켜야 할 약속이 있어요. 다음엔 실수하지 않겠다고 약속했어요.\" 운향정에서 자신의 용안을 벗어난 남자에게 한 말이다.",
                        "같은 날 이진설이 새 기술 쌍검이연십이참격을 독고령에게 시험하려 했지만 나예린이 대신 나섰다. 스물네 줄기 검기가 모두 빗나가고 나예린의 검끝이 이진설의 목뒤에 닿았다. \"큰 기술은 허점도 커요.\"",
                    } },
                    new StoryScene { Title = "추일태의 내기", Lines = new[] {
                        "구정회 상부의 명을 받은 일은무영 추일태가 비류연에게 은신 내기를 걸었다. 금남의 성지, 여성 전용 기숙사 백향관에 잠입해 정해진 물건을 가져오는 것이다.",
                        "내기 돈은 은자 이백 냥. \"돈이 적게 걸린 내기는 안 한다\"던 비류연이 이를 마다할 리 없다. 목표는 나예린의 방, 301호.",
                    } },
                },
                Intro = new[] { "신월야. 백향관을 두른 난화백무진이 어둠 속에서 움직이기 시작한다. 30년간 백여 번 뚫으려 했지만 한 번도 성공하지 못한 곳이다.", "독한녀라 불리는 절독난향십이선녀가 3교대로 길목을 지킨다. 복면을 쓴 비류연은 봉황무로 그림자를 탄다. (동료들은 밖에서 망을 본다. 게임 연출)" },
                Outro = new[] { "301호 문이 열렸다. 무기도 없이 달려온 나예린이 허공섭물로 애검 봉루를 쥐고 백광검뢰를 날린다.", "비류연은 방에 쓰러져 있던 진령에게 뇌령사를 감아 둔 터였다. 괴뢰처럼 일어선 진령이 나예린의 손을 묶는 틈에, 분뢰수와 검기가 부딪치는 폭풍 속으로 빠져나간다.", "백 장 밖에서 뇌령사를 당기자 목표물이 손으로 날아왔다. 내기는 비류연의 승리다.", "그런데 이상하다. 같은 밤 검후처의 비상종이 울렸다. 그곳에 닿은 또 다른 그림자는 비류연이 아니었다.", "같은 시각 검혼관 옆 숲. 어둠 속에서 무흔일호가 무릎을 꿇었다. \"죄송합니다, 삼공자님. 실패했습니다.\" 그 앞에 서 있던 사람은 효룡이었다.", "효룡: \"백향관이 만만치 않은 곳이란 건 알고 있었어요. 남에게서 훔친 무공으로 이겨 봐야 치욕일 뿐입니다. 전 처음부터 대사형의 계획에 반대였습니다.\" 효룡은 당분간 연락을 끊으라 이르고 어둠 속으로 사라지는 부하를 보냈다." },
                Encounters = new[] { new[] { "sunyeo" }, new[] { "sunyeo", "sunyeo" }, new[] { "na_yerin" } } },
            new StageData { Number = 11, Name = "승급시험", RecommendedLevel = 27, DooTotal = 4600,
                Prologue = new[]
                {
                    new StoryScene { Title = "삼성제 약속", Lines = new[] {
                        "다음 날 나예린이 애소저회를 직접 찾아와 비류연의 목에 검을 겨눴다. \"어제 백향관에 침입한 사람이 소협인가요?\" 비류연은 \"글쎄요\"라고 웃었다.",
                        "나예린: \"올해 삼성무제에서 우승해 보세요. 그러면 당신의 말을 믿어드리죠.\"",
                        "비류연은 삼성제가 뭔지도 모른 채 \"우승하면 저를 다른 눈으로 봐 주세요\"라고 답했다. 같은 때 검존 공손일취도 모용휘에게 검성전 우승을 요구했다.",
                    } },
                    new StoryScene { Title = "대사형의 귀환", Lines = new[] {
                        "비류연은 염도를 찾아가 '주작단은 내 제자니 네 사제'라고 폭탄선언을 했다. 단, 주작단에게는 비밀이다.",
                        "다음 날 연무장. 인피면구 없이 나타난 비류연이 \"내가 너희들의 대사형이다\"라고 선언했다. 달려든 노학은 삼복구타권법에 날아갔고, 묵룡환을 본 주작단은 일제히 포권했다.",
                        "염도가 능청스럽게 보증을 섰다. 비류연의 빚은 점점 늘어난다.",
                    } },
                    new StoryScene { Title = "모용휘의 서명", Lines = new[] {
                        "비류연이 모용휘에게 붉은 연서 서른두 통을 건넸다. 모용휘는 세 쪽도 안 읽고 속독으로 전부 훑고 내려놓았다.",
                        "\"그 여자들을 떼어 줄까?\" 비류연이 내민 종이에 모용휘는 내용도 보지 않고 서명했다. 그것은 애소저회 가입신청서였다. 동호회 지원금은 회원 수에 비례하니 유령회원이라도 쓸모가 있다.",
                    } },
                    new StoryScene { Title = "문일기의 안법 수업", Lines = new[] {
                        "화산비천응 문일기 노사의 검술 강론. \"안법이 사 할이다.\" 눈밑까지 내려온 비류연의 앞머리가 지적을 받았다.",
                        "비류연: \"제 머리카락이 방해될 정도의 상대를 아직 못 만났거든요. 사부님이 눈동자를 가리고 다니라고도 했고요.\"",
                        "문일기가 회풍무류검의 일부를 갑자기 뿜었다. 비류연은 \"마흔여덟 변화네요. 실초 열둘에 허초 서른여섯.\" 하고 맞혔다. 문일기는 입을 다물지 못했다.",
                    } },
                    new StoryScene { Title = "윤준호의 칠매검", Lines = new[] {
                        "두 달째 윤준호는 비류연의 소개서를 들고 염도에게 매일 얻어맞는다. 충격요법이라는 이름의 특훈이다. 주작단 열여섯도 같은 연무장에서 구르는 사이다.",
                        "어느 날 딴생각을 하던 염도가 무심결에 절초 염천풍을 뿜자 윤준호는 얼어붙은 채 화산 수련동 이후 처음으로 칠매검을 휘둘렀다. 매화향과 홍광의 검기가 염도의 도기를 흩었다.",
                        "곧 두드러기가 번져 검을 떨어뜨렸지만, 염도는 처음으로 윤준호를 다르게 보았다. \"너 정말 검향지경이었냐?\"",
                    } },
                    new StoryScene { Title = "복도의 소동", Lines = new[] {
                        "수업 사이 복도에서 비류연의 등에 멘 뇌금 묵뢰가 청룡단 하세인과 부딪혔다. 하세인이 빼든 철비룡을 비류연은 맨손으로 잡았다.",
                        "하세인이 물러난 뒤 비류연은 가볍게 손가락을 접었다. 하세인의 허리띠가 끊기고 복도 저편에서 강유란의 비명이 울렸다.",
                        "마침 지나던 검존 공손일취가 모용휘를 꾸짖고 조부 모용정천을 깎아내리자 모용휘가 반발했다. 공손일취: \"올해 삼성제 검성전에서 우승한다면 네 말을 믿어 주마.\" 모용휘는 \"하겠습니다\"라고 답했다.",
                        "공손일취의 가슴에는 백 년 전의 어두운 기억이 얹혀 있다. 천겁멸살의 대회전 직전 아미산 근처에서 입은 상처 때문에 그는 그 자리에 서지 못했다.",
                    } },
                },
                Intro = new[] { "삼성제에 나가려면 오검룡 이상이 되어야 한다. 승급시험은 같은 등급 두 명을 이기거나 삼백 초 동안 비기면 되고, 실력만 되면 등급을 건너뛸 수 있다.", "비류연은 사물함에 쌓인 빙봉수호대의 비무첩 가운데 복도에서 시비를 건 청룡단 하세인과, 소매에 백호수를 단 단대풍·강현추를 상대로 골랐다." },
                Outro = new[] { "단대풍의 살기 어린 일검은 비류연의 정수리 한 치 앞에서 보이지 않는 막에 튕겨 나갔다. 일권에 흉곽이 부서진 단대풍은 쓰러졌고, 강현추는 기권했다.", "비류연은 육검룡이 되었고, 모용휘도 곤륜쌍검의 도발을 은하섬광으로 물리치고 오검룡이 되었다. 둘 다 삼성제 참가 자격을 얻었다.", "도백회의 군웅팔가회 회의실. 회주대리 천기룡 단목우가 입을 열었다. \"모용휘를 반드시 우리 쪽으로 끌어와야 합니다. 이번에야말로 십 년 숙원을 풀 때입니다.\"", "구정회에서는 모용휘를 견제할 연수합벽회를 보낼 준비를 한다. 두 세력 모두 천무학관의 새 별에 눈을 돌리기 시작했다." },
                Encounters = new[] { new[] { "ha_sein" }, new[] { "gang_hyeonchu" }, new[] { "dan_daepung", "gang_hyeonchu" } } },
            new StageData { Number = 12, Name = "삼성무제 본선", RecommendedLevel = 29, DooTotal = 5200,
                Prologue = new[]
                {
                    new StoryScene { Title = "삼성무제 개막", Lines = new[] {
                        "제30회 천무삼성무제 개막식. 검성 모용정천, 도성 하후식, 검후 이옥상이 참관석에 앉았다. 관주 마진가가 대회를 선언하고 삼절검 청흔이 관도 대표로 선서했다.",
                        "삼성무제는 반년에 걸쳐 열린다. 검성전·도성전·검후전·삼성대전의 결승 여덟 명은 마지막에 몰아서 싸운다. 우승자에게는 일급영단과 전대 고수의 무공 심득이 주어진다.",
                        "비류연이 등에 멘 것은 뇌금 묵뢰. 음공을 배운 지 반년이다. 장홍이 눈을 감았다.",
                    } },
                    new StoryScene { Title = "나 소저, 올 줄 알았어요", Lines = new[] {
                        "첫 공식전. 비류연은 해남파 쾌환검 단평을 앞에 두고 관중석의 나예린에게 손을 흔들었다. 비무장이 질투의 야유로 가득 찼다.",
                        "\"여긴 바보들이 많네요.\" 비류연이 중얼거렸다.",
                    } },
                },
                Intro = new[] { "음공이 비무대에 어울리지 않는다는 사실을 비류연은 첫판에서 깨달았다. 거리가 너무 가깝다. 그래서 묵금을 들고 직접 때리기로 했다.", "천음선자 홍란은 음문의 명예가 땅에 떨어졌다며 이를 갈았다. 별명은 '운수대통 격타금'이 된다." },
                Outro = new[] { "전옥기가 송풍루에서 비류연을 험담했다는 사실을 효룡이 알고 있었다. 비류연은 뇌령사 현으로 콧등을 베고, '충격요법'으로 이빨까지 부러뜨려 장외로 날려 보냈다.", "비류연은 연승을 이어 간다. 아무도 이 1학년의 음공을 본 적이 없다." },
                Encounters = new[] { new[] { "dan_pyeong" }, new[] { "eonga_yeong", "dan_pyeong" }, new[] { "jeon_okgi" } } },
            new StageData { Number = 13, Name = "삼성대전 결승", RecommendedLevel = 31, DooTotal = 6000,
                Prologue = new[]
                {
                    new StoryScene { Title = "안목품평회", Lines = new[] {
                        "학관이 묵인하는 내기판 '안목품평회'. 주작단은 한 달 생활비를 가죽 주머니에 담아 금영호에게 맡겼다. 전부 비류연에게 건다.",
                        "염도도 자기 돈을 던졌다. \"무조건 이겨야 해. 얼음땡이 제자들보다 잘해야 한다.\" 금영호는 도광서와 삼고구배를 건 내기까지 했다.",
                    } },
                    new StoryScene { Title = "세 개의 무승부", Lines = new[] {
                        "검후전 결승은 나예린과 빙검의 딸 관설지의 무승부로 끝났다. 시합 뒤 염도가 관설지의 어깨를 잡고 외쳤다. \"소운!\" 관설지의 어머니 이름은 혁소운이었다.",
                        "도성전은 청성 비전 도법 청류흔을 쓴 유엽성과 신도문 하윤명의 무승부. 검성전은 모용휘 대 청흔. 은하류 개벽검과 삼정태극검혜의 무극검이 부딪쳐 둘 다 쓰러졌다.",
                        "세 부문이 한꺼번에 무승부가 된 것은 48년 만이다. 무승부자는 종합 결승에 나가지 못한다. 길이 열렸다.",
                    } },
                },
                Intro = new[] { "준결승에서 비류연은 사천당문 독랄수 당문천과 마주쳤다. 독을 쓰면 안 된다는 규칙은 없다.", "결승 상대는 백일연무를 마치고 돌아온 선풍검룡 위지천. 그는 비류연에게 설욕하기 위해 검을 갈았다." },
                Outro = new[] { "비류연은 운향정에서 나예린이 켠 곡을 연주했다. 위지천의 마음에 틈이 났다. 선풍우뢰, 선풍참혼, 청성의 금지 살초 천풍마뢰참도 보이지 않는 막과 끊기지 않는 뇌령사에 막혔다.", "비뢰도 오의 뢰광류하곡. 왼손에서 은빛 뇌령사 세 줄기가 뻗었다. 위지천의 검이 떨어졌다. 관중석에서 공손일취가 벌떡 일어섰다.", "다른 부문이 모두 무승부여서 비류연은 삼성무제 종합 우승까지 차지했다. 어부지리라는 소문이 돌았다.", "나예린은 약속대로 백향관 의심을 사과했다. 사과 선물로 비류연이 두 번째 입맞춤을 훔쳤다. 이번에는 그녀도 저항하지 않았다.", "그날 밤 효룡은 무흔일호에게 비류연의 사문을 조사하라고 지시했다. 때가 다가오고 있다." },
                Encounters = new[] { new[] { "eonga_yeong", "dang_munceon" }, new[] { "wiji_cheon_baek" } } },
            new StageData { Number = 14, Name = "무당산 고립", RecommendedLevel = 33, DooTotal = 6500,
                Prologue = new[]
                {
                    new StoryScene { Title = "은하류 개벽검 대 굉천혈영도법", Lines = new[] {
                        "무당산 합숙지. 모용휘의 은하류 개벽검과 갈효봉의 굉천혈영도법이 백오십 합을 넘겨 부딪친다. 싸움 도중 효룡이 \"회선십자인이 뒤에서 온다!\"라고 외쳐 청흔의 의심을 산다.",
                        "비류연은 구경만 한다. \"돌아오지 않는 메아리와 회수 없는 투자는 싫어요.\" 날아든 천살의 암습 검기는 남궁상을 던져 피하게 했을 뿐이다.",
                        "비기 대 비기. 모용휘가 피를 토하고 갈효봉은 초혼섭령술의 금제가 깨져 폭주한다. 천지쌍살은 천라지망으로 합숙지를 고립시키고 물러난다."
                    } },
                    new StoryScene { Title = "사면초가를 연주하라", Lines = new[] {
                        "염도의 \"곧 무당파에서 지원이 온다\"는 거짓말이었다. 비류연은 달빛 아래 묵금을 켠다. \"저쪽에서 초가를 안 불어 주니 이쪽에서라도 연주해야죠.\" 나예린이 금음에 이끌려 찾아온다.",
                        "\"한 명도 죽지 않을 때까지 굴려요.\" 비류연의 주문으로 이레의 특별 강화 훈련이 시작된다. 내공 없이 오 장 밖 항아리의 물을 뜻으로 움직여라.",
                        "정작 비류연은 나무 그늘에서 낮잠을 자다가, 노사부와 산적으로 변장해 초혼오귀검과 명부오귀장의 전낭을 털고 옷까지 벗겨 돌려보낸다."
                    } },
                },
                Intro = new[] { "이레째 해가 질 무렵 천지쌍살의 수하들이 다시 쳐들어온다. 항아리를 터뜨린 일곱 명을 비롯해 천관도들은 처음 싸울 때와 다르다.", "마령신단을 먹은 갈효봉이 다시 폭주한다. 천살과 지살이 직접 나선다." },
                Outro = new[] { "비류연이 손가락질로 천살의 초혼령과 애검과 오른팔을 떨어뜨렸고, 염도는 지살의 오른팔을 베었다. 쌍살은 \"그분께서 용서하지 않으실 것이다\"라는 말만 남기고 도망친다.","비류연의 심뢰가 갈효봉의 마음을 베자 정신이 돌아왔다. 하지만 심맥이 끊어진 몸은 되살릴 수 없었다. 갈효봉이 스스로 일어나 동생에게 말한다. \"너의 공부를 보여 다오.\"","효룡의 검이 형의 목숨을 거두었다. 유언은 \"겁, 하늘의 겁을 조심……\". 효룡은 피의 맹세를 한다.","한편 흑천맹주는 철각비마대를 학관으로 보냈고, 마천각의 대공자는 \"아룡, 오지 마라\"라고 혼잣말을 한다." },
                Encounters = new[] { new[] { "chohon_gwigeom", "chohon_gwigeom", "myeongbu_jang" }, new[] { "gal_hyobong" }, new[] { "cheonsal", "jisal" } } },
            new StageData { Number = 15, Name = "철각비마대", RecommendedLevel = 35, DooTotal = 7000,
                Prologue = new[]
                {
                    new StoryScene { Title = "진혼제", Lines = new[] {
                        "천무학관 직속 첩보조 스무 명이 몰살당해 진혼제가 열린다. 마진가는 분노를 누르고 의식을 주관한다.",
                        "오색 비단으로 꾸민 나예린이 진혼검무를 춘다. 장내가 숨을 멈추었다가 \"천무학관 만세!\" 함성으로 터진다.",
                        "비류연은 뾰로통하다. \"너무 예뻐서 남과 같이 보기 아깝잖아요.\""
                    } },
                },
                Intro = new[] { "무당산 이후의 일이다. 흑천맹의 철각비마대가 갈효봉의 죽음을 묻겠다며 학관으로 달려온다.", "길목에 한 청년이 서 있다. \"돌아가면 되잖아요?\" 비류연이 묵룡환 두 개를 풀자 눈이 황금빛으로 변한다." },
                Outro = new[] { "사대질풍의 철쇄선풍은 천지이분에 갈라졌고, 철비십이격의 투창은 진야에 흘렀다. 부대주 위무상은 염도의 칼을 앞에 두고 구천학이 던진 창에 목숨을 건졌다.","구천학이 내기를 걸었다. \"일기승부.\" 비류연은 허공격상과 무영인을 모두 피하고 묵성에 올라타 목에 비뢰도를 겨눈다. \"졌다.\"","철각비마대는 삼십 년 만에 말머리를 돌렸다. 목격한 사람은 모두 입을 다물었다. 거짓말쟁이가 되기 싫었기 때문이다." },
                Encounters = new[] { new[] { "sadae_jilpung", "sadae_jilpung", "sadae_jilpung" }, new[] { "cheolbi_gyeok", "cheolbi_gyeok", "wi_musang" }, new[] { "gu_cheonhak" } } },
            new StageData { Number = 16, Name = "저잣거리 습격", RecommendedLevel = 37, DooTotal = 7500,
                Prologue = new[]
                {
                    new StoryScene { Title = "흑천맹의 사신", Lines = new[] {
                        "검마 초월이 흑천맹주의 서신을 들고 학관을 찾는다. 요구는 100년 전 합의문에 따른 진상조사. 마진가는 받아들인다.",
                        "학관 앞에서 비류연과 어깨를 부딪친 초월이 검을 뽑았으나 머리카락 세 가닥만 날렸다. 눈을 본 초월이 경악하고, 효룡이 \"제 친구입니다\"라 전음하자 물러난다."
                    } },
                    new StoryScene { Title = "사중화 은설란", Lines = new[] {
                        "조사관은 갈효봉의 정혼자 사중화 은설란. 쾌활한 겉모습 뒤에 정체를 알 수 없는 눈이 있다.",
                        "호위는 모용휘, 그리고 나예린. 은설란이 나예린의 손을 끌고 옷을 사러 남창 저잣거리로 나선다."
                    } },
                },
                Intro = new[] { "번화가 한복판. 노인, 아낙, 문사, 농부, 노부부…… 지나가는 모두가 자객이다.", "오성루에서 술을 마시던 비류연이 맹수의 움직임을 눈치채고 염도의 돈으로 계산을 치르게 한 뒤 달려온다." },
                Outro = new[] { "백인회주의 손목이 잘렸고, 나예린의 한상옥령신검이 쌍귀 형제를 베었다. 은설란은 비명 한 번 지르지 않고 두 사람의 실력을 가늠했다.","\"도와준 사람은 감사를 받을 자격이 있죠.\" 비류연은 세 번째 입맞춤을 훔친다. 나예린은 이번에도 검을 뽑지 못했다. \"기회를 놓쳤을 뿐입니다.\"","이틀 뒤 비류연은 은설란의 세 번째 호위가 되었다. 조사관의 요청이라 했다." },
                Encounters = new[] { new[] { "baegin_jagaek", "baegin_jagaek", "baegin_jagaek" }, new[] { "ssanggwi", "baegin_jagaek" }, new[] { "baek_tanjeo" } } },
            new StageData { Number = 17, Name = "마하령의 분노", RecommendedLevel = 39, DooTotal = 8000,
                Prologue = new[]
                {
                    new StoryScene { Title = "화산지회 후보 선발", Lines = new[] {
                        "5년마다 정사가 겨루는 화산규약지회. 올해는 100주년이다. 마진가는 신분이 불확실한 비류연과 효룡의 서류에도 합격 도장을 찍는다.",
                        "후보는 백검조(천익검 늑기한)와 흑검조(마검자 고약한)로 나뉜다. 은설란까지 수강생으로 앉은 흑검조 교실에서 고약한이 말한다. \"활검은 위선이다.\""
                    } },
                    new StoryScene { Title = "순풍루의 밤", Lines = new[] {
                        "사흘 전 밤 비류연은 몰래 기숙사를 빠져나가 정보상 나대이의 주루 순풍루를 찾았다. 현상금 은 닷 냥에 눈이 멀어 소문의 미녀를 뒤쫓았다.",
                        "천축대승유가신공. 몸이 네 배로 부풀었다가 다시 날씬해지는 모습을 엿보고 말았다. \"뚱땡이 소저!\" 비류연은 칼바람을 피해 달아났다."
                    } },
                },
                Intro = new[] { "군웅회주 철옥잠 마하령이 비류연에게 뺨을 맞고도 사과를 거부한다. 신응대가 포위하고 팽혁성이 칼을 뽑는다.", "비류연은 마하령의 손목을 쥔 채 놓지 않는다. \"놓아주세요, 부탁합니다. 이 말을 하기 전에는 안 놓아요.\"" },
                Outro = new[] { "비류연은 마하령을 방패처럼 세워 팽혁성의 칼끝을 틀게 했다. 구정회주 창천룡 용천명이 나타났다. 마하령은 어린 시절 \"뚱땡이!\"라 불렀던 그를 거부했다.","용천명의 백룡검이 번개처럼 뽑혔다. 비류연은 잔상만 남기고 피했으나 옷이 갈라졌다. 마하령의 허리띠가 끊어지고 사과 요구는 계속된다.","그때 늑기한과 고약한이 나타나 서로 으르렁거렸고, 둘은 중간 평가에서 승부를 보기로 했다." },
                Encounters = new[] { new[] { "sinung_daewon", "sinung_daewon", "sinung_daewon" }, new[] { "pae_hyeokseong", "sinung_daewon" }, new[] { "ma_haryeong" } } },
            new StageData { Number = 18, Name = "태극의 인재", RecommendedLevel = 41, DooTotal = 8500,
                Prologue = new[]
                {
                    new StoryScene { Title = "혁월린의 유언", Lines = new[] {
                        "20년 전, 온몸에 거미줄 같은 상처를 입은 태극신문 무신 혁월린이 제자 둘을 불렀다. 염도와 빙검에게 남긴 유언은 이랬다.",
                        "\"염백과 빙혼. 정과 반을 아우를 인재를 찾아라. 신도 홍염과 신검 빙루가 길을 인도할 것이다. 별의 기운을 가진 자다.\" 흉수의 이름은 끝내 말하지 않았다."
                    } },
                    new StoryScene { Title = "싸우려면 돈을 내라", Lines = new[] {
                        "비류연은 도전자가 늘자 소문을 퍼뜨렸다. \"싸우려면 대전료를 내라. 염도를 이길 실력이 아니면 돈만 날린다.\"",
                        "소문은 염도의 숙적 빙검의 귀에 들어갔다. 비류연이 염도를 이겼다는 헛소문이라면 빙검은 직접 확인해야 했다."
                    } },
                },
                Intro = new[] { "총 노사 빙검 관철수가 나타난다. \"조건이 맞으면 싸우겠다는 말이 사실이냐?\" 비류연은 \"제가 이기면 부탁 두 가지를 들어주세요\"라 답한다.", "그 전에 용천명이 비류연의 검이 장식품인지 확인하겠다며 길을 막는다." },
                Outro = new[] { "빙령수혼과 백빙신첨과 백빙신정이 모두 봉황무에 빗나갔다. \"이제야 좀 춥군요.\"","염도가 신원 보증인으로 나타나자 빙검은 말을 잃었다. 비류연이 오른팔의 묵룡환까지 벗자 빙검이 비장절초의 기수식을 잡는다.","\"네가 태극을 하나로 합칠 인재인지 이 일검으로 확인하겠다!\" \"난 그런 사람 아니라니깐요!\" 천지를 집어삼킬 듯한 흰빛이 터져 나왔다. (10권에서 계속)" },
                Encounters = new[] { new[] { "yong_cheonmyeong" }, new[] { "gwan_cheolsu" } } },
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

        public static CompanionData FindCompanion(string id)
        {
            foreach (var c in Companions) if (c.Id == id) return c;
            return null;
        }

        /// <summary>Companions travelling with the protagonist in the given stage (those who have joined by then).</summary>
        public static List<CompanionData> CompanionsFor(int stageNumber)
        {
            var list = new List<CompanionData>();
            foreach (var c in Companions) if (c.JoinStage <= stageNumber) list.Add(c);
            return list;
        }

        public static StageData FindStage(int number)
        {
            foreach (var s in Stages) if (s.Number == number) return s;
            return null;
        }
    }
}
