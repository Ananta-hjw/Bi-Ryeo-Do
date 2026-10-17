using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>Plain-data martial art entry.</summary>
    public sealed class ArtData
    {
        public string Id, Name, Description;
        public float Power = 1f;
        public int Hits = 1, QiCost, SpiritCost, Cooldown, UnlockStage, UnlockDiff;   // learnable once the stage is reached (UnlockDiff 0) or cleared on that difficulty
        public bool AllEnemies;
        public int Range;       // enemy lines reached (1 = the front line only, 2+ = any line); 0 = by kind (see EffectiveRange)
        public SkillEffect Effect;
        public float HealPct;
        public long LearnCost;
        public string OwnerId;   // null = a martial art the protagonist can learn; otherwise a companion's fixed art

        /// <summary>
        /// How far the art reaches: an explicit <see cref="Range"/>, otherwise 3 for area arts and 비기, 1 for everything else (the front line only)
        /// (the basic attack reaches 1).
        /// </summary>
        public int EffectiveRange
        {
            get
            {
                if (Range > 0) return Range;
                if (AllEnemies || SpiritCost > 0) return 3;
                return 1;
            }
        }

        public bool IsUnlocked(SaveData save) { return StageStars.Reached(save, UnlockStage, UnlockDiff); }
        public string UnlockText { get { return StageStars.ReachText(UnlockStage, UnlockDiff); } }

        public SkillSpec ToSpec()
        {
            return new SkillSpec
            {
                Id = Id, Name = Name, Description = Description, Power = Power, Hits = Hits, QiCost = QiCost,
                SpiritCost = SpiritCost, Cooldown = Cooldown, AllEnemies = AllEnemies, Range = EffectiveRange, Effect = Effect, HealPct = HealPct
            };
        }
    }

    /// <summary>Plain-data enemy. Stats derive from the player curve at <see cref="Level"/> so difficulty tracks Progression.</summary>
    public sealed class EnemyData
    {
        public string Id, Name;
        public string ArtId;         // optional: share another enemy's artwork (balance clones)
        public string ArtKey { get { return string.IsNullOrEmpty(ArtId) ? Id : ArtId; } }
        public bool IsBoss;
        public float RetreatHpPct;   // >0: story boss that escapes at this HP fraction instead of dying
        public int Level;
        public float HpMul = 1f, AtkMul = 1f, DefMul = 1f;
        public int SpeedAdd;
        public int Range = 1;       // how many tiles away its attacks reach
        public EnemyMove[] Moves;

        /// <summary>A copy with HP and attack multiplied (harder stage difficulties).</summary>
        public EnemyData Scaled(float hp, float atk)
        {
            var c = (EnemyData)MemberwiseClone();
            c.HpMul = HpMul * hp;
            c.AtkMul = AtkMul * atk;
            return c;
        }

        public CombatStats BuildStats()
        {
            var b = Progression.StatsFor(Level);
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
            c.RetreatHpPct = RetreatHpPct;
            c.Range = Range;
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
        public string JointName;                 // 합공 with the protagonist
        public SkillEffect JointEffect;          // None, Stun, GuardParty or HealAlly riding on the joint strike
        public float JointPower = 1f;

        /// <summary>Companions' own arts hit and heal harder than the listed numbers (they were too weak next to the protagonist).</summary>
        public const float ArtPowerFactor = 1.4f;

        public Combatant CreateCombatant(int level, int bondLevel = 0, int trainLevel = 0)
        {
            var b = Progression.AllyStatsFor(level);
            float bond = Bonds.StatMultiplier(bondLevel) * CompanionTraining.StatMultiplier(trainLevel);
            var stats = new CombatStats
            {
                MaxHp = (int)Math.Round(b.MaxHp * HpMul * bond),
                MaxQi = b.MaxQi,
                Attack = (int)Math.Round(b.Attack * AtkMul * bond),
                Defense = (int)Math.Round(b.Defense * DefMul * bond),
                Speed = b.Speed + SpeedAdd,
                CritRate = 0.02f + Bonds.CritBonus(bondLevel),
                CritDamage = 1.5f
            };
            var skills = new List<SkillSpec>();
            foreach (var a in EarlyGameContent.Arts)
            {
                if (a.OwnerId != Id) continue;
                var spec = a.ToSpec();
                spec.Power *= ArtPowerFactor;
                spec.HealPct *= ArtPowerFactor;
                skills.Add(spec);
            }
            CompanionTraining.ApplyToSkills(Id, skills, trainLevel);
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
        /// <summary>Optional special rules, parallel to <see cref="Encounters"/> (null or a null entry = a normal fight).</summary>
        public TrialRule[] Trials;
        public TrialRule TrialFor(int encounter) { return Trials != null && encounter < Trials.Length ? Trials[encounter] : null; }

        public int RecommendedLevel;
        public long DooTotal;   // 두 for clearing the stage once (split by enemy weight)
        public bool IsTower;    // a 무한 비무대 floor (see Tower): no stage progress, own rewards
        public int TowerFloor;
        public bool IsQuest;    // a 의뢰 built from an earlier stage's fight (see Quests): no stage progress, own exp and rewards
        public string QuestId;
        public int Difficulty;  // 0 일반, 1 어려움, 2 극악 (see StageStars); set on a copy made by WithDifficulty

        public StageData WithDifficulty(int difficulty)
        {
            var c = (StageData)MemberwiseClone();
            c.Difficulty = Math.Max(0, Math.Min(StageStars.DifficultyCount - 1, difficulty));
            return c;
        }
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
            new ArtData { Id = StarterArtId, Range = 4, Name = "비도 던지기", Description = "비뢰도의 기초. 비도를 날려 적을 맞힌다.", Power = 1.3f, Cooldown = 0 },
            new ArtData { Id = "samboku_chobok", Name = "삼복구타권법 초복", Description = "사부가 창안한 권법. 초복은 근육을 노린다.", Power = 2.2f, QiCost = 14, Cooldown = 1, LearnCost = 0, UnlockStage = 1 },
            new ArtData { Id = "bunroesu", Name = "분뢰수", Description = "손을 강철처럼 만들어 내려친다.", Power = 3.0f, QiCost = 22, Cooldown = 2, LearnCost = 220, UnlockStage = 2 },
            new ArtData { Id = "samcheopyeong", Name = "삼첩영", Description = "봉황무의 잔상 셋. 다음 차례까지 모든 공격을 피한다.", QiCost = 20, Cooldown = 3, Effect = SkillEffect.Evade, LearnCost = 400, UnlockStage = 3 },
            new ArtData { Id = "bi_roe_do", Range = 4, Name = "비뢰도", Description = "비도 세 자루를 연달아 날린다.", Power = 1.7f, Hits = 3, QiCost = 32, Cooldown = 3, LearnCost = 900, UnlockStage = 3 },
            new ArtData { Id = "yeobanjang", Name = "여반장", Description = "손바닥을 뒤집는 회전력으로 일격에 제압하는 일장법.", Power = 5.2f, QiCost = 38, Cooldown = 3, LearnCost = 2200, UnlockStage = 5 },
            new ArtData { Id = "roegwang_ryuhagok", Name = "뢰광류하곡", Description = "여섯 자루를 동시에 운용해 적 전체를 벤다.", Power = 2.6f, AllEnemies = true, QiCost = 46, Cooldown = 4, LearnCost = 4500, UnlockStage = 6 },
            new ArtData { Id = "seonpung_roejeol", Name = "선풍뇌절", Description = "비기. 기세를 모두 쏟아 은빛 섬광으로 적을 쓰러뜨린다.", Power = 9.5f, SpiritCost = 100, LearnCost = 1500, UnlockStage = 4 },
            // Learned in volumes 7-9 (names from the original text)
            new ArtData { Id = "cheongeun_roe", Name = "뇌격타 천근뢰", Description = "천 근의 무게를 실은 뇌전의 일격. 철각비마대의 창을 꺾은 기술.", Power = 6.4f, QiCost = 44, Cooldown = 3, LearnCost = 7000, UnlockStage = 10 },
            new ArtData { Id = "cheonji_ibun", Name = "참단절영의 장 천지이분", Description = "손날로 말과 기수를 한꺼번에 가르는 절삭의 장.", Power = 7.8f, QiCost = 52, Cooldown = 4, LearnCost = 11000, UnlockStage = 15 },
            new ArtData { Id = "bongik_bisang", Name = "사신무 봉익비상", Description = "봉황이 날개를 펴듯 사방으로 쏟아내는 일격. 적 전체를 친다.", Power = 3.6f, AllEnemies = true, QiCost = 58, Cooldown = 4, LearnCost = 15000, UnlockStage = 19 },
            new ArtData { Id = "simroe", Name = "단심무형의 장 심뢰", Description = "비기. 마음을 베는 검기의 오의가 적의 중심을 꿰뚫는다.", Power = 13.0f, SpiritCost = 100, LearnCost = 9000, UnlockStage = 22 },
            // Learned in volumes 10-12 (names from the original text)
            new ArtData { Id = "saengsa_seomroe", Name = "생사결 섬뢰창", Description = "비뢰도 오의 검기. 뇌전의 창이 거대한 영물의 비늘을 꿰뚫는다.", Power = 8.8f, QiCost = 60, Cooldown = 4, LearnCost = 16000, UnlockStage = 27 },
            new ArtData { Id = "eunseompung", Name = "은검풍인의 장 은섬풍", Description = "검기가 은빛 바람이 되어 사방의 적을 찢는다. 적 전체를 친다.", Power = 4.2f, AllEnemies = true, QiCost = 66, Cooldown = 4, LearnCost = 24000, UnlockStage = 5, UnlockDiff = 1 },
            new ArtData { Id = "goengroe", Name = "굉천광뢰의 장 굉뢰", Description = "작은 은빛 물방울이 도와 무기를 박살내는 비뢰도의 오의.", Power = 10.5f, QiCost = 74, Cooldown = 4, LearnCost = 30000, UnlockStage = 12, UnlockDiff = 1 },
            new ArtData { Id = "seomroe", Range = 4, Name = "섬뢰", Description = "비기. 빛을 뒤쫓는 쾌. 눈 깜짝할 새 미간을 꿰뚫는다.", Power = 17.0f, SpiritCost = 100, LearnCost = 22000, UnlockStage = 18, UnlockDiff = 1 },
            // Learned in volumes 13-16
            new ArtData { Id = "jikji_jilju", Name = "직지질주", Description = "봉황무의 오의. 절벽을 평지처럼 달리며 적을 세 번 벤다.", Power = 3.6f, Hits = 3, QiCost = 70, Cooldown = 4, LearnCost = 34000, UnlockStage = 24, UnlockDiff = 1 },
            new ArtData { Id = "pungsin", Name = "풍신(風神)", Description = "비기. 열 자루 비뢰도와 뇌령사가 나선을 그리며 용권풍이 되어 적 전체를 휩쓴다.", Power = 9.5f, AllEnemies = true, SpiritCost = 100, LearnCost = 44000, UnlockStage = 30, UnlockDiff = 1 },
            // Companions' fixed arts (names are descriptive unless the original text names the technique)
            new ArtData { Id = "xr_twin", Name = "쌍검 연격", Description = "쌍둥이 검으로 두 번 벤다.", Power = 1.5f, Hits = 2, QiCost = 18, Cooldown = 1, OwnerId = "hyo_ryong" },
            new ArtData { Id = "xr_barrier", Name = "태을천강벽", Description = "검막을 펼쳐 모두를 방어 태세로 만든다.", Effect = SkillEffect.GuardParty, QiCost = 30, Cooldown = 4, OwnerId = "hyo_ryong" },
            new ArtData { Id = "jh_slash", Name = "검", Description = "정석적인 검격.", Power = 1.9f, QiCost = 14, Cooldown = 1, OwnerId = "jang_hong" },
            new ArtData { Id = "jh_stun", Name = "점혈", Description = "혈도를 짚어 상대를 굳힌다. 기를 모으는 중이면 끊는다.", Power = 0.8f, Effect = SkillEffect.Stun, QiCost = 20, Cooldown = 3, OwnerId = "jang_hong" },
            new ArtData { Id = "yj_slash", Name = "화산의 검", Description = "화산파의 기본 검격. 매화검법은 알레르기 때문에 쓰지 못한다.", Power = 2.2f, QiCost = 16, Cooldown = 1, OwnerId = "yun_junho" },
            new ArtData { Id = "yj_heal", Name = "자하신공", Description = "화산의 자색 기운으로 가장 다친 동료를 치유한다.", Effect = SkillEffect.HealAlly, HealPct = 0.35f, QiCost = 28, Cooldown = 3, OwnerId = "yun_junho" },
            new ArtData { Id = "isj_gon", Name = "진성십이곤", Description = "녹림 진성곤의 곤법. 방어는 원, 공격은 점, 힘의 이동은 나선.", Power = 2.0f, QiCost = 16, Cooldown = 1, OwnerId = "im_seongjin" },
            new ArtData { Id = "isj_seonggwang", Name = "성광일시", Description = "곤을 별처럼 흩뿌려 네 번 찌른다.", Power = 0.9f, Hits = 4, QiCost = 30, Cooldown = 3, OwnerId = "im_seongjin" },
            new ArtData { Id = "isj_iljeom", Range = 4, Name = "일점필중", Description = "진성십이곤 최종오의. 한 줄기 섬광이 되어 한 점을 꿰뚫는다.", Power = 5.0f, QiCost = 45, Cooldown = 4, OwnerId = "im_seongjin" },
            new ArtData { Id = "ny_baekhon", Name = "백혼검뢰천검식", Description = "나예린의 공격 검법. 흰 검뢰가 한 줄기로 내리꽂힌다.", Power = 2.4f, QiCost = 16, Cooldown = 1, OwnerId = "na_yerin" },
            new ArtData { Id = "ny_hansang", Name = "한상옥령신검", Description = "얼음처럼 차가운 검기가 세 번 스친다.", Power = 1.2f, Hits = 3, QiCost = 30, Cooldown = 3, OwnerId = "na_yerin" },
            new ArtData { Id = "ny_simahn", Range = 4, Name = "심안검", Description = "용안으로 허점을 읽고 마음의 눈으로 한 점을 꿰뚫는다.", Power = 5.4f, QiCost = 42, Cooldown = 4, OwnerId = "na_yerin" },
        };

        public static readonly CompanionData[] Companions =
        {
            new CompanionData { Id = "hyo_ryong", Name = "효룡", Role = "쌍검, 검막으로 파티를 지킨다", JoinStage = 7, HpMul = 1.0f, AtkMul = 1.0f, DefMul = 1.0f, SpeedAdd = 4, JointName = "쌍검합벽", JointEffect = SkillEffect.GuardParty },
            new CompanionData { Id = "jang_hong", Name = "장홍", Role = "점혈로 적의 강타를 끊는다", JoinStage = 7, HpMul = 1.1f, AtkMul = 0.85f, DefMul = 1.1f, SpeedAdd = 0, JointName = "점혈합격", JointEffect = SkillEffect.Stun },
            new CompanionData { Id = "yun_junho", Name = "윤준호", Role = "자하신공으로 동료를 치유한다", JoinStage = 8, HpMul = 0.9f, AtkMul = 0.95f, DefMul = 0.85f, SpeedAdd = 2, JointName = "자하합격", JointEffect = SkillEffect.HealAlly },
            new CompanionData { Id = "na_yerin", Name = "나예린", Role = "빙백봉. 검기로 적을 연달아 벤다", JoinStage = 16, HpMul = 0.9f, AtkMul = 1.1f, DefMul = 0.9f, SpeedAdd = 3, JointName = "백혼합벽", JointEffect = SkillEffect.Stun, JointPower = 1.2f },
            new CompanionData { Id = "im_seongjin", Name = "임성진", Role = "곤으로 한 점을 꿰뚫는다 (굼뜨지만 묵직하다)", JoinStage = 22, HpMul = 1.15f, AtkMul = 0.9f, DefMul = 1.0f, SpeedAdd = -2, JointName = "관통합격", JointPower = 1.4f },
        };

        public static readonly EnemyData[] Enemies =
        {
            // Stage 1: 아미산 (1권)
            new EnemyData { Id = "tiger", Name = "호랑이", Level = 1, HpMul = 0.783f, AtkMul = 0.433f, SpeedAdd = 8,
                Moves = new[] { EnemyMove.Attack("물어뜯기", 1.0f), EnemyMove.Attack("할퀴기", 0.8f, 2) } },
            new EnemyData { Id = "bear", Name = "곰", Level = 1, HpMul = 1.119f, AtkMul = 0.433f, SpeedAdd = -6,
                Moves = new[] { EnemyMove.Attack("앞발치기", 1.0f), EnemyMove.Wind("몸을 일으킨다"), EnemyMove.Attack("내려치기", 2.4f) } },
            new EnemyData { Id = "white_tiger", Name = "백호", Level = 2, IsBoss = true, HpMul = 1.791f, AtkMul = 0.444f, SpeedAdd = 8,
                Moves = new[] { EnemyMove.Attack("할퀴기", 0.8f, 2), EnemyMove.Attack("물어뜯기", 1.2f), EnemyMove.Wind("몸을 낮춘다"), EnemyMove.Attack("덮치기", 2.8f) } },
            // Stage 2: 중양표국 (1권)
            new EnemyData { Id = "biaosa", Name = "표사", Level = 3, HpMul = 0.879f, AtkMul = 0.490f,
                Moves = new[] { EnemyMove.Attack("베기", 1.0f), EnemyMove.Attack("찌르기", 1.1f) } },
            new EnemyData { Id = "gang_janghan", Name = "비풍도 강장한", Level = 5, IsBoss = true, HpMul = 1.647f, AtkMul = 0.542f,
                Moves = new[] { EnemyMove.Attack("도", 1.0f), EnemyMove.Attack("도", 1.1f), EnemyMove.Wind("도를 휘두를 자세를 잡는다"), EnemyMove.Attack("내려베기", 2.6f) } },
            // Stage 3: 호골채 (1권)
            new EnemyData { Id = "hogol_bandit", Name = "호골채 산적", Level = 6, HpMul = 0.830f, AtkMul = 0.619f,
                Moves = new[] { EnemyMove.Attack("휘두르기", 1.0f), EnemyMove.Attack("베기", 1.1f) } },
            new EnemyData { Id = "paeho_jung_gwang", Name = "패호 중광", Level = 8, IsBoss = true, HpMul = 1.038f, AtkMul = 0.610f,
                Moves = new[] { EnemyMove.Attack("내려치기", 1.1f), EnemyMove.Attack("연타", 0.7f, 3), EnemyMove.Wind("기합을 모은다"), EnemyMove.Attack("일격", 2.8f) } },
            // Stage 4: 용골채 (1권)
            new EnemyData { Id = "yonggol_bandit", Name = "용골채 산적", Level = 9, HpMul = 0.800f, AtkMul = 0.613f,
                Moves = new[] { EnemyMove.Attack("휘두르기", 1.0f), EnemyMove.Attack("베기", 1.1f) } },
            new EnemyData { Id = "paeryong_wooak", Name = "패룡 우악", Level = 11, IsBoss = true, HpMul = 1.700f, AtkMul = 0.685f,
                Moves = new[] { EnemyMove.Attack("내려치기", 1.1f), EnemyMove.Attack("연타", 0.7f, 3), EnemyMove.Wind("기세를 끌어올린다"), EnemyMove.Attack("패룡격", 3.0f) } },
            // Stage 5: 윤하루 (2권)
            new EnemyData { Id = "jang_ugang", Name = "장우강", Level = 12, HpMul = 0.900f, AtkMul = 1.060f,
                Moves = new[] { EnemyMove.Attack("청성검", 1.0f), EnemyMove.Attack("청성검", 1.2f), EnemyMove.Wind("검을 거둔다"), EnemyMove.Attack("찌르기", 2.2f) } },
            new EnemyData { Id = "yeomdo", Name = "염도 곽영희", Level = 14, IsBoss = true, HpMul = 1.800f, AtkMul = 1.233f,
                Moves = new[] { EnemyMove.Attack("진홍십칠염", 0.8f, 3), EnemyMove.Attack("검염기", 1.3f), EnemyMove.Wind("화령신공을 끌어올린다"), EnemyMove.Attack("화령염천도세", 3.2f) } },
            // Stage 6: 호아장 (2권)
            new EnemyData { Id = "hoajang_disciple", Name = "호아장 제자", Level = 15, HpMul = 0.882f, AtkMul = 0.677f,
                Moves = new[] { EnemyMove.Attack("맹호비격검", 1.0f), EnemyMove.Attack("맹호비격검", 1.1f) } },
            new EnemyData { Id = "ho_cheonsang", Name = "호아장주 호천상", Level = 17, IsBoss = true, HpMul = 1.401f, AtkMul = 0.681f,
                Moves = new[] { EnemyMove.Attack("맹호비격검", 1.0f, 2), EnemyMove.Attack("맹호비격검", 1.3f), EnemyMove.Wind("검기를 모은다"), EnemyMove.Attack("맹호삼십육검", 3.0f) } },
            // Stage 7: 검혼관 기관 노방 (2권)
            new EnemyData { Id = "trap_arrow", Name = "기관 화살대", Level = 19, HpMul = 1.627f, AtkMul = 3.396f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("화살 연사", 0.6f, 3).Sniping(), EnemyMove.Attack("화살", 1.0f).Sniping() } },
            new EnemyData { Id = "trap_needle", Name = "기관 비침대", Level = 19, HpMul = 1.627f, AtkMul = 3.396f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("비침", 0.8f, 2).Sniping(), EnemyMove.Wind("비침을 장전한다"), EnemyMove.Attack("비침 난사", 0.9f, 4) } },
            new EnemyData { Id = "trap_log", Name = "기관 통나무", Level = 20, HpMul = 2.440f, AtkMul = 3.785f, SpeedAdd = -8,
                Moves = new[] { EnemyMove.Wind("통나무가 흔들린다"), EnemyMove.Attack("통나무 휘두르기", 2.4f) } },
            new EnemyData { Id = "ho_cheongang", Name = "호아장 대사형 호천강", Level = 21, IsBoss = true, HpMul = 3.660f, AtkMul = 3.785f,
                Moves = new[] { EnemyMove.Attack("맹호삼십육검", 0.7f, 3), EnemyMove.Attack("패왕노호검", 1.3f), EnemyMove.Wind("패왕노호검을 치켜든다"), EnemyMove.Attack("패왕노호검 일격", 3.0f) } },
            // Stage 8: 천무식당 (3권)
            new EnemyData { Id = "jongri_hak", Name = "종리학", Level = 21, HpMul = 2.181f, AtkMul = 3.955f,
                Moves = new[] { EnemyMove.Attack("매화검법", 0.8f, 2), EnemyMove.Attack("매화검법", 1.2f) } },
            new EnemyData { Id = "chu_gayeon", Name = "추가연", Level = 23, IsBoss = true, HpMul = 4.120f, AtkMul = 4.163f,
                Moves = new[] { EnemyMove.Attack("매화검법", 0.8f, 3), EnemyMove.Attack("매화검법", 1.2f), EnemyMove.Wind("검을 모은다"), EnemyMove.Attack("매화만개", 3.0f) } },
            // Stage 9: 운향정 (3권)
            new EnemyData { Id = "gu_chang", Name = "일도파쇄 구창", Level = 23, HpMul = 2.895f, AtkMul = 6.425f,
                Moves = new[] { EnemyMove.Attack("도", 1.1f), EnemyMove.Wind("도를 치켜든다"), EnemyMove.Attack("일도파쇄", 2.6f) } },
            new EnemyData { Id = "so_cheongun", Name = "청풍검 소천군", Level = 24, HpMul = 2.760f, AtkMul = 6.425f,
                Moves = new[] { EnemyMove.Attack("청성검", 0.7f, 3), EnemyMove.Attack("청성검", 1.3f), EnemyMove.Wind("무형지기를 끌어올린다"), EnemyMove.Attack("검풍", 2.6f) } },
            new EnemyData { Id = "wiji_cheon", Name = "선풍검룡 위지천", Level = 26, IsBoss = true, HpMul = 5.501f, AtkMul = 6.757f,
                Moves = new[] { EnemyMove.Attack("절풍검기변", 0.7f, 3), EnemyMove.Attack("절풍검기변", 1.2f), EnemyMove.Wind("검강을 끌어올린다"), EnemyMove.Attack("천원삼절검격", 1.5f, 3) } },
            // Stage 10: 백향관 (4권)
            new EnemyData { Id = "sunyeo", Name = "독한녀 십이선녀", Level = 25, HpMul = 2.838f, AtkMul = 6.010f,
                Moves = new[] { EnemyMove.Attack("독침", 0.7f, 3).Sniping(), EnemyMove.Attack("난향장", 1.2f), EnemyMove.Wind("독향을 퍼뜨린다"), EnemyMove.Attack("절독난무", 2.6f) } },
            new EnemyData { Id = "na_yerin", Name = "빙백봉 나예린", Level = 27, IsBoss = true, HpMul = 5.676f, AtkMul = 6.249f,
                Moves = new[] { EnemyMove.Attack("백뢰낙화", 0.7f, 3), EnemyMove.Attack("섬뢰관일", 1.3f), EnemyMove.Wind("백광검뢰를 모은다"), EnemyMove.Attack("참천무뢰", 3.0f) } },
            // Stage 11: 승급시험 (4권)
            new EnemyData { Id = "ha_sein", Name = "철검비룡 하세인", Level = 27, HpMul = 1.860f, AtkMul = 4.211f,
                Moves = new[] { EnemyMove.Attack("철비룡검", 1.0f), EnemyMove.Wind("검기를 맺는다"), EnemyMove.Attack("징계의 일검", 2.6f) } },
            new EnemyData { Id = "gang_hyeonchu", Name = "영취검 강현추", Level = 28, HpMul = 1.744f, AtkMul = 4.211f,
                Moves = new[] { EnemyMove.Attack("무산검", 0.8f, 2), EnemyMove.Attack("무산검", 1.2f) } },
            new EnemyData { Id = "dan_daepung", Name = "철검비룡 단대풍", Level = 29, IsBoss = true, HpMul = 3.488f, AtkMul = 4.449f,
                Moves = new[] { EnemyMove.Attack("점창검", 0.7f, 3), EnemyMove.Attack("점창검", 1.2f), EnemyMove.Wind("살기를 끌어올린다"), EnemyMove.Attack("살기 어린 일검", 3.0f) } },
            // Stage 12: 삼성무제 본선 (5권)
            new EnemyData { Id = "dan_pyeong", Name = "쾌환검 단평", Level = 29, HpMul = 2.592f, AtkMul = 6.170f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("남해 36검", 0.6f, 3), EnemyMove.Attack("쾌검살초", 1.3f) } },
            new EnemyData { Id = "eonga_yeong", Name = "파풍창 언가영", Level = 30, HpMul = 2.765f, AtkMul = 4.610f,
                Moves = new[] { EnemyMove.Attack("언가묵창", 1.1f), EnemyMove.Wind("창을 거둔다"), EnemyMove.Attack("파풍창", 2.6f) } },
            new EnemyData { Id = "jeon_okgi", Name = "팔비신검 전옥기", Level = 31, IsBoss = true, HpMul = 5.184f, AtkMul = 6.369f,
                Moves = new[] { EnemyMove.Attack("팔비검", 0.6f, 4), EnemyMove.Attack("점창검", 1.2f), EnemyMove.Wind("검을 모은다"), EnemyMove.Attack("팔비신검", 3.0f) } },
            // Stage 13: 삼성대전 결승 (5권)
            new EnemyData { Id = "dang_munceon", Name = "독랄수 당문천", Level = 31, HpMul = 3.460f, AtkMul = 6.713f,
                Moves = new[] { EnemyMove.Attack("삼환비선", 0.7f, 3), EnemyMove.Attack("묵편", 1.2f), EnemyMove.Wind("독을 푼다"), EnemyMove.Attack("독편 난무", 2.6f) } },
            new EnemyData { Id = "wiji_cheon_baek", Name = "선풍검룡 위지천(백일연무)", Level = 33, IsBoss = true, HpMul = 7.530f, AtkMul = 7.017f,
                Moves = new[] { EnemyMove.Attack("선풍우뢰", 0.7f, 3), EnemyMove.Attack("선풍참혼", 1.3f), EnemyMove.Wind("검강을 끌어올린다"), EnemyMove.Attack("천풍마뢰참", 3.2f) } },
            // Stage 14: 무당산 고립 (7권)
            new EnemyData { Id = "chohon_gwigeom", Name = "초혼오귀검 귀영살검", Level = 32, HpMul = 2.376f, AtkMul = 3.827f,
                Moves = new[] { EnemyMove.Attack("암습검", 0.8f, 2), EnemyMove.Attack("소도", 1.2f) } },
            new EnemyData { Id = "myeongbu_jang", Name = "명부오귀장 명염도 추항", Level = 33, HpMul = 2.693f, AtkMul = 3.827f,
                Moves = new[] { EnemyMove.Attack("명부도", 1.0f), EnemyMove.Wind("도를 높이 든다"), EnemyMove.Attack("명염참", 2.6f) } },
            new EnemyData { Id = "gal_hyobong", Name = "혈류도 갈효봉(폭주)", Level = 34, HpMul = 5.070f, AtkMul = 4.080f,
                Moves = new[] { EnemyMove.Attack("광무쌍도", 0.7f, 3), EnemyMove.Attack("혈풍무", 1.3f), EnemyMove.Wind("혈광을 일으킨다"), EnemyMove.Attack("굉천혈류패왕십자인", 3.2f) } },
            new EnemyData { Id = "cheonsal", Name = "천살 초혼검", Level = 35, IsBoss = true, HpMul = 4.753f, AtkMul = 4.080f,
                Moves = new[] { EnemyMove.Attack("초혼귀령검", 0.7f, 3), EnemyMove.Attack("소혼붕렬", 1.3f), EnemyMove.Wind("초혼령을 울린다"), EnemyMove.Attack("귀령압혼", 3.0f) } },
            new EnemyData { Id = "jisal", Name = "지살 명왕도", Level = 35, IsBoss = true, HpMul = 4.753f, AtkMul = 4.080f,
                Moves = new[] { EnemyMove.Attack("명왕도", 1.0f), EnemyMove.Attack("명왕도", 1.2f), EnemyMove.Wind("웃는 얼굴이 굳는다"), EnemyMove.Attack("명왕참", 2.8f) } },
            // Stage 15: 철각비마대 (8권, 회상)
            new EnemyData { Id = "sadae_jilpung", Name = "사대질풍 철기병", Level = 35, HpMul = 1.462f, AtkMul = 3.801f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("철쇄선풍", 0.8f, 2), EnemyMove.Attack("거창", 1.2f) } },
            new EnemyData { Id = "cheolbi_gyeok", Name = "철비십이격 창기병", Level = 36, HpMul = 1.554f, AtkMul = 3.801f,
                Moves = new[] { EnemyMove.Attack("십자투창", 0.7f, 3), EnemyMove.Wind("창을 허리에 건다"), EnemyMove.Attack("기창충격", 2.6f) } },
            new EnemyData { Id = "wi_musang", Name = "철각비마 위무상", Level = 36, HpMul = 1.828f, AtkMul = 3.801f,
                Moves = new[] { EnemyMove.Attack("삼단질풍격", 0.7f, 3), EnemyMove.Attack("거창돌격", 1.3f) } },
            new EnemyData { Id = "gu_cheonhak", Name = "질풍묵흔 구천학", Level = 37, IsBoss = true, HpMul = 2.925f, AtkMul = 3.886f,
                Moves = new[] { EnemyMove.Attack("허공격상", 0.7f, 3), EnemyMove.Attack("파랑분쇄격", 1.3f), EnemyMove.Wind("창을 머리 위로 든다"), EnemyMove.Attack("무영인", 3.2f) } },
            // Stage 16: 저잣거리 습격 (8권)
            new EnemyData { Id = "baegin_jagaek", Name = "백인회 변장 자객", Level = 36, HpMul = 1.972f, AtkMul = 2.951f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("암기", 0.6f, 3).Sniping(), EnemyMove.Attack("암습", 1.3f).Sniping() } },
            new EnemyData { Id = "ssanggwi", Name = "이인일체 쌍귀 형제", Level = 37, HpMul = 2.818f, AtkMul = 3.013f,
                Moves = new[] { EnemyMove.Attack("쌍도", 1.0f), EnemyMove.Wind("배 속에서 손이 튀어나온다"), EnemyMove.Attack("이인일체 비수", 2.8f) } },
            new EnemyData { Id = "baek_tanjeo", Name = "백인회주 관살 백탄저", Level = 37, IsBoss = true, HpMul = 4.227f, AtkMul = 3.013f,
                Moves = new[] { EnemyMove.Attack("독검", 0.7f, 3), EnemyMove.Attack("극독 일섬", 1.3f), EnemyMove.Wind("독검을 뽑아 든다"), EnemyMove.Attack("맹독 난자", 3.0f) } },
            // Stage 17: 마하령과 군웅회 (9권)
            new EnemyData { Id = "sinung_daewon", Name = "신응대 대원", Level = 38, HpMul = 2.742f, AtkMul = 4.205f,
                Moves = new[] { EnemyMove.Attack("철벽진", 0.8f, 2), EnemyMove.Attack("도격", 1.2f) } },
            new EnemyData { Id = "pae_hyeokseong", Name = "광풍맹호도 팽혁성", Level = 39, HpMul = 3.428f, AtkMul = 4.205f,
                Moves = new[] { EnemyMove.Attack("오호단문도", 1.0f), EnemyMove.Wind("맹호출림의 자세"), EnemyMove.Attack("맹호출림", 2.8f) } },
            new EnemyData { Id = "ma_haryeong", Name = "철옥잠 마하령", Level = 40, IsBoss = true, HpMul = 5.485f, AtkMul = 4.279f,
                Moves = new[] { EnemyMove.Attack("표류무상도", 0.7f, 3), EnemyMove.Attack("철옥참", 1.3f), EnemyMove.Wind("천축유가신공을 끌어올린다"), EnemyMove.Attack("표류무상참", 3.2f) } },
            // Stage 18: 태극의 인재 (9권)
            new EnemyData { Id = "yong_cheonmyeong", Name = "창천룡 용천명", Level = 40, HpMul = 5.300f, AtkMul = 8.568f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("백룡검", 0.7f, 3), EnemyMove.Attack("달마검", 1.3f), EnemyMove.Wind("반야신공을 일으킨다"), EnemyMove.Attack("여래일검", 3.0f) } },
            new EnemyData { Id = "gwan_cheolsu", Name = "빙검 관철수", Level = 42, IsBoss = true, HpMul = 7.208f, AtkMul = 8.736f,
                Moves = new[] { EnemyMove.Attack("빙령수혼", 0.7f, 3), EnemyMove.Attack("백빙신침", 1.3f), EnemyMove.Wind("한빙지기를 끌어올린다"), EnemyMove.Attack("백빙신정", 3.4f) } },
            // Stage 19: 환마동 시험 (10권)
            new EnemyData { Id = "hwan_ying", Name = "환마동 환영", Level = 43, HpMul = 2.968f, AtkMul = 3.555f,
                Moves = new[] { EnemyMove.Attack("조롱", 0.8f, 2), EnemyMove.Attack("덮치기", 1.2f) } },
            new EnemyData { Id = "hwan_sukbu", Name = "환영 속 광풍장", Level = 44, HpMul = 4.081f, AtkMul = 3.555f,
                Moves = new[] { EnemyMove.Attack("음습한 손길", 1.0f), EnemyMove.Wind("눈을 번뜩인다"), EnemyMove.Attack("광풍장", 2.8f) } },
            new EnemyData { Id = "hwan_sabu", Name = "환영 속 사부", Level = 45, IsBoss = true, HpMul = 6.678f, AtkMul = 3.672f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("낚싯줄", 0.7f, 3), EnemyMove.Attack("뇌신의 일격", 1.3f), EnemyMove.Wind("백발이 곤두선다"), EnemyMove.Attack("풍신의 일격", 3.2f) } },
            // Stage 20: 매몰 44일 (10권)
            new EnemyData { Id = "cave_viper", Name = "동굴 독충", Level = 45, HpMul = 2.472f, AtkMul = 3.197f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("독니", 0.8f, 2), EnemyMove.Attack("휘감기", 1.2f) } },
            new EnemyData { Id = "mukrin_hyeolmang", Name = "묵린혈망", Level = 47, IsBoss = true, HpMul = 6.262f, AtkMul = 3.306f,
                Moves = new[] { EnemyMove.Attack("꼬리치기", 0.7f, 3), EnemyMove.Attack("독액", 1.3f).Sniping(), EnemyMove.Wind("바위를 녹이는 침을 모은다"), EnemyMove.Attack("묵린 강타", 3.3f) } },
            // Stage 21: 흑랑채 청혼 (11권)
            new EnemyData { Id = "heukrang_bandit", Name = "흑랑채 산적", Level = 47, HpMul = 2.914f, AtkMul = 3.540f,
                Moves = new[] { EnemyMove.Attack("흑랑조", 0.8f, 2), EnemyMove.Attack("도끼질", 1.2f) } },
            new EnemyData { Id = "lee_songhak", Name = "군자소요검 이송학", Level = 48, HpMul = 3.771f, AtkMul = 3.540f,
                Moves = new[] { EnemyMove.Attack("소요검", 0.8f, 2), EnemyMove.Wind("수비 자세를 푼다"), EnemyMove.Attack("군자일검", 2.6f) } },
            new EnemyData { Id = "im_gae", Name = "흑랑부 임개", Level = 49, IsBoss = true, HpMul = 6.170f, AtkMul = 3.642f,
                Moves = new[] { EnemyMove.Attack("흑랑십이부", 0.7f, 3), EnemyMove.Attack("쌍아격", 1.3f), EnemyMove.Wind("도끼를 치켜든다"), EnemyMove.Attack("태산양분", 3.3f) } },
            // Stage 22: 낙뢰곡 철쇄봉혼진 (11~12권)
            new EnemyData { Id = "hyeolma_dae", Name = "십이혈마대 대원", Level = 49, HpMul = 3.025f, AtkMul = 2.825f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("혈검", 0.8f, 2), EnemyMove.Attack("암습", 1.3f).Sniping() } },
            new EnemyData { Id = "hyeol_gung", Name = "9대 조장 혈궁", Level = 50, HpMul = 4.033f, AtkMul = 3.206f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("혈마전", 0.7f, 3).Sniping(), EnemyMove.Wind("혈마궁을 당긴다"), EnemyMove.Attack("혈야구성", 2.8f) } },
            new EnemyData { Id = "hyeol_swae", Name = "10대 조장 혈쇄", Level = 51, IsBoss = true, HpMul = 6.385f, AtkMul = 3.206f,
                Moves = new[] { EnemyMove.Attack("철쇄", 0.8f, 2), EnemyMove.Attack("비차륜", 1.3f), EnemyMove.Wind("철쇄봉혼진을 펼친다"), EnemyMove.Attack("혈겁쇄법 노룡분쇄", 3.3f) } },
            // Stage 23: 비사진 (12권)
            new EnemyData { Id = "bisa_snake", Name = "비사진의 독사 떼", Level = 51, HpMul = 2.693f, AtkMul = 3.131f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("독니", 0.8f, 2), EnemyMove.Attack("휘감기", 1.2f) } },
            new EnemyData { Id = "cheonghong_ssanggaksa", Name = "청홍쌍각사", Level = 52, HpMul = 3.802f, AtkMul = 3.217f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("독니", 0.7f, 3), EnemyMove.Wind("요광을 모은다"), EnemyMove.Attack("요광", 2.6f) } },
            new EnemyData { Id = "mo_saryeong", Name = "비사마군 모사령", Level = 53, IsBoss = true, HpMul = 6.179f, AtkMul = 3.217f,
                Moves = new[] { EnemyMove.Attack("만사혈장", 0.7f, 3), EnemyMove.Attack("비사진", 1.3f), EnemyMove.Wind("사충적을 입에 문다"), EnemyMove.Attack("지사진", 3.3f) } },
            // Stage 24: 비뢰쌍마 (12권)
            new EnemyData { Id = "dongbang_hak", Name = "섬뢰마검 동방학", Level = 54, HpMul = 6.398f, AtkMul = 4.545f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("붉은 검", 0.7f, 3), EnemyMove.Attack("섬뢰", 1.3f), EnemyMove.Wind("쾌검의 자세를 취한다"), EnemyMove.Attack("섬뢰일섬", 3.0f) } },
            new EnemyData { Id = "gong_pae", Name = "잔뢰마도 공패", Level = 55, IsBoss = true, HpMul = 8.957f, AtkMul = 4.545f,
                Moves = new[] { EnemyMove.Attack("잔뢰도", 0.8f, 2), EnemyMove.Attack("잔뢰삼십육패", 1.3f), EnemyMove.Wind("도를 높이 든다"), EnemyMove.Attack("잔뢰번천지", 3.4f) } },
            // Stage 25: 화평장 (13권)
            new EnemyData { Id = "hwapyeong_soldier", Name = "화평장 훈련병", Level = 55, HpMul = 3.114f, AtkMul = 3.166f,
                Moves = new[] { EnemyMove.Attack("연환검", 0.8f, 2), EnemyMove.Attack("포위 돌격", 1.2f) } },
            new EnemyData { Id = "ssangsal_hapgyeok", Name = "천지쌍살 합격술(한 팔씩)", Level = 56, HpMul = 5.245f, AtkMul = 3.166f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("초혼귀령검", 0.7f, 3), EnemyMove.Attack("명왕도", 1.3f), EnemyMove.Wind("쌍살이 서로의 빈 팔을 맞댄다"), EnemyMove.Attack("쌍살 합격", 3.0f) } },
            new EnemyData { Id = "baek_son_gosu", Name = "하얀 손의 고수", Level = 57, IsBoss = true, HpMul = 6.885f, AtkMul = 3.264f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("단월인", 0.7f, 3), EnemyMove.Attack("무형 칼날", 1.3f), EnemyMove.Wind("하얀 손을 편다"), EnemyMove.Attack("단월인 극의", 3.3f) } },
            // Stage 26: 살귀대 (14권)
            new EnemyData { Id = "salgwi_daewon", Name = "살귀대원", Level = 57, HpMul = 3.345f, AtkMul = 2.894f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("추격검", 0.8f, 2), EnemyMove.Attack("암살", 1.3f) } },
            new EnemyData { Id = "ho_yugwang", Name = "화평장주 호유광", Level = 58, IsBoss = true, HpMul = 7.024f, AtkMul = 2.894f,
                Moves = new[] { EnemyMove.Attack("살귀 지휘검", 0.7f, 3), EnemyMove.Attack("독조", 1.3f), EnemyMove.Wind("살귀대에 신호를 보낸다"), EnemyMove.Attack("살귀 일제 공격", 3.2f) } },
            // Stage 27: 마천칠걸 (14권)
            new EnemyData { Id = "jongri_chu", Name = "칠련창 종리추", Level = 59, HpMul = 3.936f, AtkMul = 4.262f,
                Moves = new[] { EnemyMove.Attack("칠성창", 0.7f, 3), EnemyMove.Wind("일곱 번째 창을 겨눈다"), EnemyMove.Attack("북두칠성 투창", 2.7f) } },
            new EnemyData { Id = "do_chuun", Name = "사갈검편 도추운", Level = 59, HpMul = 3.633f, AtkMul = 4.262f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("검편 채찍", 0.7f, 3), EnemyMove.Attack("사갈편", 1.3f) } },
            new EnemyData { Id = "oh_munchu", Name = "쇄풍겸 오문추", Level = 60, HpMul = 3.936f, AtkMul = 4.381f,
                Moves = new[] { EnemyMove.Attack("사슬낫", 0.8f, 2), EnemyMove.Wind("사슬을 휘감는다"), EnemyMove.Attack("쇄풍 난무", 2.8f) } },
            new EnemyData { Id = "baek_sayeong", Name = "사교검 백사영", Level = 60, HpMul = 3.936f, AtkMul = 4.381f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("사교검", 0.7f, 3), EnemyMove.Wind("뱀처럼 몸을 낮춘다"), EnemyMove.Attack("백사교룡살", 2.9f) } },
            new EnemyData { Id = "chu_myeong", Name = "마검익 추명", Level = 61, IsBoss = true, HpMul = 6.660f, AtkMul = 4.381f,
                Moves = new[] { EnemyMove.Attack("회선인", 0.7f, 3), EnemyMove.Attack("흑응익", 1.3f), EnemyMove.Wind("흑풍을 일으킨다"), EnemyMove.Attack("흑풍난무", 3.3f) } },
            // Stage 28: 멸성대 (15권)
            new EnemyData { Id = "mulsung_daewon", Name = "멸성대 대원", Level = 61, HpMul = 3.466f, AtkMul = 2.669f,
                Moves = new[] { EnemyMove.Attack("살인검", 0.8f, 2), EnemyMove.Attack("검익진", 1.3f) } },
            new EnemyData { Id = "i_gae_bujo", Name = "멸성대 부조장 이개", Level = 62, HpMul = 4.411f, AtkMul = 2.723f,
                Moves = new[] { EnemyMove.Attack("호위검", 0.8f, 2), EnemyMove.Wind("만근검익진을 펼친다"), EnemyMove.Attack("만근추", 2.7f) } },
            new EnemyData { Id = "sama_heun", Name = "천참마검 사마흔", Level = 63, IsBoss = true, HpMul = 7.246f, AtkMul = 2.723f,
                Moves = new[] { EnemyMove.Attack("천참검", 0.7f, 3), EnemyMove.Attack("독검", 1.3f), EnemyMove.Wind("검을 뽑아 든다"), EnemyMove.Attack("천겁 일섬", 3.4f) } },
            // Stage 29: 중토관 (16권)
            new EnemyData { Id = "suda_bokmyeon", Name = "수다쟁이 복면인", Level = 64, HpMul = 4.452f, AtkMul = 2.277f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("가벼운 검", 0.7f, 3), EnemyMove.Wind("호들갑을 떨며 몸을 낮춘다"), EnemyMove.Attack("도우미의 일격", 2.8f) } },
            new EnemyData { Id = "mu_jeong", Name = "잔무일점혈 무정", Level = 65, IsBoss = true, HpMul = 7.123f, AtkMul = 2.330f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("그림자 비수", 0.7f, 3).Sniping(), EnemyMove.Attack("무언독 암기", 1.3f).Sniping(), EnemyMove.Wind("그림자 속으로 스며든다"), EnemyMove.Attack("일점혈", 3.5f) } },
            // Stage 30: 화룡멸겁대진 (16권)
            new EnemyData { Id = "ma_chilgeol_jeoji", Name = "마천칠걸 저지조", Level = 65, HpMul = 4.233f, AtkMul = 2.339f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("연환 공격", 0.7f, 3), EnemyMove.Attack("협공", 1.3f) } },
            new EnemyData { Id = "hwaryong_hwayeom", Name = "화룡의 불꽃", Level = 66, HpMul = 5.210f, AtkMul = 2.339f,
                Moves = new[] { EnemyMove.Attack("불의 성벽", 0.8f, 2), EnemyMove.Wind("불길이 솟구친다"), EnemyMove.Attack("화룡의 숨결", 2.9f) } },
            new EnemyData { Id = "bi_daegongja", Name = "대공자 비", Level = 67, IsBoss = true, RetreatHpPct = 0.3f, HpMul = 10.180f, AtkMul = 2.860f, DefMul = 1.15f, SpeedAdd = 14,
                Moves = new[] { EnemyMove.Attack("영뢰 잔흔", 0.7f, 3), EnemyMove.Attack("검기 난무", 1.3f), EnemyMove.Wind("영뢰를 모은다"), EnemyMove.Attack("영뢰 천잔", 3.6f) } },
            // Stage 29 trials: 검후의 십초지적, 수화관 (16권)
            new EnemyData { Id = "geomhu_ioksang", Name = "검후 이옥상", Level = 66, HpMul = 60.000f, AtkMul = 4.793f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("체대", 0.8f, 2), EnemyMove.Attack("체대 일격", 1.3f), EnemyMove.Wind("검집에 손을 얹는다"), EnemyMove.Attack("해상비조천참절", 2.8f) } },
            new EnemyData { Id = "heuksaek_amhyeok", Name = "검은 암벽", Level = 63, HpMul = 4.081f, AtkMul = 0.399f,
                Moves = new[] { EnemyMove.Attack("진동", 0.5f) } },
            new EnemyData { Id = "wiji_hyeolpok", Name = "혈폭환 위지천", Level = 64, HpMul = 4.081f, AtkMul = 2.662f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("폭주 진기", 0.7f, 3), EnemyMove.Attack("혈폭 난사", 1.3f), EnemyMove.Wind("혈폭환의 기운이 끓어오른다"), EnemyMove.Attack("생명을 불사른 일격", 3.0f) } },
            // Stage 29 trial: 은가면 삼성의 습격 (16권). Unkillable; the party only has to endure.
            new EnemyData { Id = "mask_gongpo", Name = "은가면 「공포」", Level = 66, HpMul = 60.000f, AtkMul = 2.129f,
                Moves = new[] { EnemyMove.Attack("분신 일검", 0.8f, 2), EnemyMove.Attack("백발의 일검", 1.3f), EnemyMove.Wind("분신이 하나둘 늘어난다"), EnemyMove.Attack("공포의 일검", 2.8f) } },
            new EnemyData { Id = "mask_jeolmang", Name = "은가면 「절망」", Level = 66, HpMul = 60.000f, AtkMul = 2.129f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("나선장", 0.8f, 2), EnemyMove.Attack("나선 돌풍", 1.2f), EnemyMove.Wind("나선의 힘을 모은다"), EnemyMove.Attack("옷을 찢는 나선", 2.6f) } },
            new EnemyData { Id = "mask_bitan", Name = "은가면 「비탄」", Level = 66, HpMul = 60.000f, AtkMul = 2.129f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("허상 일검", 0.7f, 3), EnemyMove.Attack("나뭇가지 일검", 1.3f), EnemyMove.Wind("심정청도의 검기를 모은다"), EnemyMove.Attack("비탄의 일검", 2.8f) } },
            // Stage 28 trial: 목요관 야간 질주 (15권)
            new EnemyData { Id = "jongri_chu_night", Name = "칠련창 종리추", Level = 59, HpMul = 3.347f, AtkMul = 3.195f,
                Moves = new[] { EnemyMove.Attack("칠성창", 0.7f, 3), EnemyMove.Attack("일곱 창 투척", 1.2f) } },
            new EnemyData { Id = "do_chuun_night", Name = "사갈검편 도추운", Level = 59, HpMul = 3.151f, AtkMul = 3.195f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("검편 채찍", 0.7f, 3), EnemyMove.Attack("사갈편", 1.2f) } },
            // Balance clones: the same enemy tuned separately for a later stage (artwork is shared via ArtId)
            new EnemyData { Id = "eonga_yeong_b", ArtId = "eonga_yeong", Name = "파풍창 언가영", Level = 30, HpMul = 3.256f, AtkMul = 4.490f,
                Moves = new[] { EnemyMove.Attack("언가묵창", 1.1f), EnemyMove.Wind("창을 거둔다"), EnemyMove.Attack("파풍창", 2.6f) } },
            new EnemyData { Id = "hyeolma_dae_b", ArtId = "hyeolma_dae", Name = "십이혈마대 대원", Level = 49, HpMul = 3.839f, AtkMul = 2.788f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("혈검", 0.8f, 2), EnemyMove.Attack("암습", 1.3f).Sniping() } },
            new EnemyData { Id = "oh_munchu_b", ArtId = "oh_munchu", Name = "쇄풍겸 오문추", Level = 60, HpMul = 3.859f, AtkMul = 4.419f,
                Moves = new[] { EnemyMove.Attack("사슬낫", 0.8f, 2), EnemyMove.Wind("사슬을 휘감는다"), EnemyMove.Attack("쇄풍 난무", 2.8f) } },
            new EnemyData { Id = "baek_sayeong_b", ArtId = "baek_sayeong", Name = "사교검 백사영", Level = 60, HpMul = 3.859f, AtkMul = 4.419f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("사교검", 0.7f, 3), EnemyMove.Wind("뱀처럼 몸을 낮춘다"), EnemyMove.Attack("백사교룡살", 2.9f) } },
        };

        // Items. Names come from volumes 1-3; effects and numbers are placeholders.
        private static readonly ItemData[] BaseItems =
        {
            new ItemData { Id = "bear_gall", Grade = ItemGrade.Fine, InGacha = true, Name = "웅담", Kind = ItemKind.Consumable, HealPct = 0.40f, Description = "곰의 쓸개. 가장 다친 동료의 체력을 40% 회복한다." },
            new ItemData { Id = "okruju", Grade = ItemGrade.Fine, InGacha = true, Name = "옥루주", Kind = ItemKind.Consumable, QiRestore = 60, SpiritGain = 25, Description = "사천제일주. 내공 60과 기세 25를 얻는다." },
            new ItemData { Id = "ginseng", Grade = ItemGrade.Epic, InGacha = true, Name = "인형설삼", Kind = ItemKind.Consumable, HealPct = 1.0f, QiRestore = 999, Description = "사람 모양의 영약 산삼. 가장 다친 동료를 완전히 회복하고 내공을 가득 채운다." },
            new ItemData { Id = InnerArts.StoneId, Grade = ItemGrade.Epic, Name = "심득석", Kind = ItemKind.Material, SellValue = 0, Description = "심법의 깨달음이 응어리진 돌. 무공 탭의 심법에서 심법 하나를 강화하는 데 쓴다. 팔 수 없다." },
            new ItemData { Id = "art_scroll_5", Grade = ItemGrade.Rare, Name = "무공 수련서 (소)", Kind = ItemKind.Consumable, MasteryUses = 5, Description = "초식을 정리한 얇은 수련서. 행낭에서 쓰면 배운 무공 하나의 숙련 +5회." },
            new ItemData { Id = "art_scroll_10", Grade = ItemGrade.Epic, Name = "무공 수련서 (중)", Kind = ItemKind.Consumable, MasteryUses = 10, Description = "요결을 풀이한 수련서. 행낭에서 쓰면 배운 무공 하나의 숙련 +10회." },
            new ItemData { Id = "art_scroll_20", Grade = ItemGrade.Legendary, Name = "무공 수련서 (대)", Kind = ItemKind.Consumable, MasteryUses = 20, Description = "고수의 깨달음이 담긴 비급. 행낭에서 쓰면 배운 무공 하나의 숙련 +20회." },
            new ItemData { Id = "leather", Grade = ItemGrade.Normal, Name = "가죽", Kind = ItemKind.Material, SellValue = 70, Description = "사냥한 짐승의 가죽. 팔면 두가 된다." },
            new ItemData { Id = "laundry_bat", Grade = ItemGrade.Fine, Name = "강철 빨래방망이", Kind = ItemKind.Weapon, AtkPct = 0.08f, Description = "수련용 강철 방망이(50근). 무기라기엔 빨래용이다. 공격 +8%." },
            new ItemData { Id = "jin_biroedo", Grade = ItemGrade.Transcendent, Name = "진비뢰도", Kind = ItemKind.Weapon, AtkPct = 0.45f, CritAdd = 0.10f, SpeedAdd = 6, Description = "봉뢰함에 든 진짜 비뢰도 열 자루. 전승의식을 마친 이만이 쥘 수 있다. 공격 +45%, 치명타 +10%, 속도 +6." },
            new ItemData { Id = "mukryonghwan", Grade = ItemGrade.Rare, Name = "묵룡환", Kind = ItemKind.Accessory, AtkPct = 0.10f, DefPct = 0.10f, SpeedAdd = -6, Description = "사부가 11세부터 채운 증량 족쇄. 몸은 무거워지지만 단련된다. 공격·방어 +10%, 속도 -6." },
            new ItemData { Id = "roegeum", Grade = ItemGrade.Epic, Name = "뇌금 묵뢰", Kind = ItemKind.Accessory, QiPct = 0.30f, HpPct = 0.05f, Description = "뇌령사 현을 쓴 검은 칠현금. 내공 +30%, 체력 +5%." },
            new ItemData { Id = "claw_necklace", Grade = ItemGrade.Fine, Name = "발톱 목걸이", Kind = ItemKind.Accessory, CritAdd = 0.05f, SpeedAdd = 2, Description = "사냥한 짐승의 발톱을 꿴 목걸이. 치명타 +5%, 속도 +2." },
            new ItemData { Id = "ilgeup_yeongdan", Grade = ItemGrade.Legendary, InGacha = true, Name = "일급영단", Kind = ItemKind.Consumable, HealPct = 1.0f, QiRestore = 999, SpiritGain = 100, Description = "삼성무제 우승 부상. 가장 다친 동료를 완전히 회복하고 내공을 채우며 기세를 100까지 끌어올린다." },
            new ItemData { Id = "seungryong_pae", Grade = ItemGrade.Rare, Name = "승룡패", Kind = ItemKind.Key, Description = "녹옥으로 된 추천패. 천무학관 특별전형 입관의 증표." },
            new ItemData { Id = "mukrin_talisman", Grade = ItemGrade.Rare, Name = "묵린혈망 독아 부적", Kind = ItemKind.Accessory, DefPct = 0.12f, HpPct = 0.05f, Description = "묵린혈망의 독아를 은세공한 초승달 장신구. 뱀이 다가오지 못한다. 방어 +12%, 체력 +5%." },
            // 객잔 shop goods (game design, not from the novel): sold by the factions at higher reputation, never dropped or pulled
            new ItemData { Id = "meal_academy", Grade = ItemGrade.Normal, Name = "학관 정식", Kind = ItemKind.Consumable, HealPct = 0.50f, QiRestore = 20, Description = "학관 식당의 든든한 한 끼. 가장 다친 동료의 체력을 50% 회복하고 내공 20을 얻는다." },
            new ItemData { Id = "biaoguk_ration", Grade = ItemGrade.Normal, Name = "표국 건량", Kind = ItemKind.Consumable, HealPct = 0.35f, Description = "표행길에 챙기는 마른 식량. 체력을 35% 회복한다." },
            new ItemData { Id = "taegeuk_pill", Grade = ItemGrade.Rare, Name = "태극환", Kind = ItemKind.Consumable, HealPct = 0.80f, QiRestore = 50, Description = "무당의 단약. 가장 다친 동료의 체력을 80% 회복하고 내공 50을 얻는다." },
            new ItemData { Id = "plum_elixir", Grade = ItemGrade.Rare, Name = "매화 영단", Kind = ItemKind.Consumable, HealPct = 0.60f, QiRestore = 80, SpiritGain = 30, Description = "화산의 영단. 체력 60% 회복, 내공 80, 기세 30을 얻는다." },
            new ItemData { Id = "greenwood_liquor", Grade = ItemGrade.Fine, Name = "녹림 화주", Kind = ItemKind.Consumable, QiRestore = 100, SpiritGain = 60, Description = "독한 산채의 화주. 내공 100과 기세 60을 얻는다." },
            new ItemData { Id = "cheonyul_pae", Grade = ItemGrade.Epic, Name = "천율패", Kind = ItemKind.Key, Description = "화산규약지회 중토관의 신분 패. 조와 이름과 천율인이 새겨져 있다. 잃으면 탈락이다." },
        };

        /// <summary>Every item: the story items above plus the 뽑기 catalog.</summary>
        public static readonly ItemData[] Items = MergeItems();

        private static ItemData[] MergeItems()
        {
            var list = new List<ItemData>(BaseItems);
            list.AddRange(GachaItems.All);
            return list.ToArray();
        }

        public static readonly DropData[] Drops =
        {
            new DropData { EnemyId = "tiger", ItemId = "leather", Chance = 0.60f },
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
            new DropData { EnemyId = "hwan_sabu", ItemId = "ginseng", Chance = 0.50f },
            new DropData { EnemyId = "mukrin_hyeolmang", ItemId = "ginseng", Chance = 0.50f },
            new DropData { EnemyId = "im_gae", ItemId = "bear_gall", Chance = 0.50f },
            new DropData { EnemyId = "hyeol_gung", ItemId = "okruju", Chance = 0.50f },
            new DropData { EnemyId = "hyeol_swae", ItemId = "ginseng", Chance = 0.50f },
            new DropData { EnemyId = "mo_saryeong", ItemId = "ginseng", Chance = 0.50f },
            new DropData { EnemyId = "dongbang_hak", ItemId = "okruju", Chance = 0.50f },
            new DropData { EnemyId = "gong_pae", ItemId = "ilgeup_yeongdan", Chance = 0.20f },
            new DropData { EnemyId = "baek_son_gosu", ItemId = "ginseng", Chance = 0.50f },
            new DropData { EnemyId = "ho_yugwang", ItemId = "ginseng", Chance = 0.50f },
            new DropData { EnemyId = "chu_myeong", ItemId = "okruju", Chance = 0.50f },
            new DropData { EnemyId = "sama_heun", ItemId = "ilgeup_yeongdan", Chance = 0.20f },
            new DropData { EnemyId = "mu_jeong", ItemId = "ginseng", Chance = 0.50f },
            new DropData { EnemyId = "bi_daegongja", ItemId = "ilgeup_yeongdan", Chance = 0.20f },
        };

        /// <summary>One-time rewards for clearing a stage for the first time.</summary>
        public static readonly Dictionary<int, ItemStack[]> StageRewards = new Dictionary<int, ItemStack[]>
        {
            { 1, new[] { new ItemStack("bear_gall", 2) } },
            { 2, new[] { new ItemStack("okruju", 2) } },
            { 3, new[] { new ItemStack("ginseng", 1) } },
            { 4, new[] { new ItemStack("bear_gall", 3) } },
            { 5, new[] { new ItemStack("ginseng", 1), new ItemStack("okruju", 2) } },   // 봉뢰함과 뇌금은 시작 때 지닌 것으로 친다(진비뢰도 등은 이제 뽑기)
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
            { 19, new[] { new ItemStack("ginseng", 2), new ItemStack("okruju", 3) } },
            { 20, new[] { new ItemStack("ginseng", 3) } },   // 묵린혈망의 독아 부적은 이제 뽑기
            { 21, new[] { new ItemStack("ginseng", 2), new ItemStack("bear_gall", 3) } },
            { 22, new[] { new ItemStack("ilgeup_yeongdan", 1), new ItemStack("ginseng", 2) } },
            { 23, new[] { new ItemStack("ginseng", 3), new ItemStack("okruju", 3) } },
            { 24, new[] { new ItemStack("ilgeup_yeongdan", 2), new ItemStack("ginseng", 3) } },
            { 25, new[] { new ItemStack("ginseng", 2), new ItemStack("okruju", 3) } },
            { 26, new[] { new ItemStack("ilgeup_yeongdan", 1), new ItemStack("bear_gall", 3) } },
            { 27, new[] { new ItemStack("ginseng", 3), new ItemStack("okruju", 3) } },
            { 28, new[] { new ItemStack("ilgeup_yeongdan", 2), new ItemStack("ginseng", 2) } },
            { 29, new[] { new ItemStack("cheonyul_pae", 1), new ItemStack("ginseng", 3) } },   // 중토관 천율패
            { 30, new[] { new ItemStack("ilgeup_yeongdan", 3), new ItemStack("ginseng", 3) } },   // 화산규약지회 우승
        };

        /// <summary>What the protagonist starts with.</summary>
        public static readonly ItemStack[] StartKit =
        {
            new ItemStack("mukryonghwan", 1), new ItemStack("laundry_bat", 1), new ItemStack("cloth_robe", 1), new ItemStack("bear_gall", 2), new ItemStack("okruju", 1),
        };

        public static ItemData FindItem(string id)
        {
            foreach (var i in Items) if (i.Id == id) return i;
            return null;
        }

        public static readonly StageData[] Stages =
        {
            new StageData { Number = 1, Name = "아미산 숲", RecommendedLevel = 1, DooTotal = 300,
                Intro = new[] {
                    "아미산 중턱, 사부의 모옥. 장작 패기와 빨래로 위장된 수련이 몇 해째 이어지고 있다.",
                    "사부|류연아, 오늘 땔감은 넉넉하더냐? 숲 깊은 데는 가지 말거라. 백호가 산다는 소문이 있다.",
                    "비류연|네, 사부님. (호랑이 가죽이랑 웅담은 사부님 몰래 챙겨야지.)",
                    "숲에는 호랑이와 곰이 얼쩡거린다. 가죽과 웅담은 사부 몰래 챙기는 짭짤한 부수입이다.",
                },
                Outro = new[] {
                    "비류연|백호까지 끝. 이건 사부님한테는 비밀로 해야겠다.",
                    "백호를 쓰러뜨렸다. 두둑한 수확이다.",
                },
                Encounters = new[] { new[] { "tiger" }, new[] { "bear" }, new[] { "tiger", "tiger" }, new[] { "white_tiger" } } },
            new StageData { Number = 2, Name = "중양표국", RecommendedLevel = 4, DooTotal = 1400,
                Prologue = new[]
                {
                    new StoryScene { Title = "진조운의 유품", Lines = new[] {
                            "열아홉 살 늦봄. 비류연은 폭포 밑에서 수중 수련을 하다 바위를 던졌다. 하필 그때 산을 오르던 사람이 있었다.",
                            "천무학관이 초빙한 교관 철담비환 진조운. 강호 백대고수급이었던 그는 말 한마디 못 하고 세상을 떠났다.",
                            "비류연|어… 이 사람, 사고인데. 서찰이랑 인피면구에 황금 열 냥까지. 이거 어쩌지.",
                            "비류연|내가 진조운이 되면 되겠네.",
                        } },
                    new StoryScene { Title = "아미산 합숙", Lines = new[] {
                            "천무아미수련원에 천무학관 주작단 열여섯 명이 도착했다. 남궁상, 현운, 진령, 노학, 당철영… 구파와 팔대세가의 후기지수들이다.",
                            "비류연|나는 노사부라 한다. 앞으로 다섯 달, 내 말대로 한다. 장작 패고, 빨래하고, 밥 짓는다.",
                            "노학|뭐? 우리가 이런 일이나 하려고 온 줄 아십니까!",
                            "비류연|의욕이 넘치는구나. 시범 조교는 네가 맡아라. 삼복구타권법이다.",
                            "그날부터 아무도 노사부의 말에 토를 달지 않았다.",
                            "다섯 달 뒤, 주작단의 팔다리에는 묵환이 채워졌고 눈빛에는 두려움과 존경이 섞였다. 이별을 앞둔 밤, 남궁상은 진령과 입을 맞췄다고 한다.",
                        } },
                },
                Intro = new[] {
                    "합숙훈련 중 당철영이 강장한 일행에게 집단구타를 당했다.",
                    "당철영|… 크윽, 스승님. 제가 부족했습니다.",
                    "비류연|내 제자를 건드렸으면 값을 치러야지. 가자, 중양표국으로. 목적은 두둑한 보상이다.",
                    "비류연은 주작단 열다섯과 함께 중양표국으로 쳐들어간다.",
                },
                Outro = new[] {
                    "비류연|국주님, 합의금은 이쪽으로 주시죠. 다치신 분 치료비는 따로 쳐서.",
                    "국주 장우양까지 제압하고 거액의 보상을 받아냈다.",
                    "표국은 소문이 윤색된 덕에 오히려 호황을 맞는다고 한다.",
                },
                Encounters = new[] { new[] { "biaosa" }, new[] { "biaosa", "biaosa" }, new[] { "gang_janghan", "biaosa" } } },
            new StageData { Number = 3, Name = "호골채 매복", RecommendedLevel = 7, DooTotal = 1600,
                Intro = new[] {
                    "중양표국 사상 최대의 남창행 표행. 표물은 극비 고려청자 세 점이다.",
                    "표사|사천 경계 산길입니다. 여기만 넘으면… 앗, 매복이다!",
                    "호골채 도적|여기가 표물이 지나는 길이 맞군! 녹림 72채 호골채 오백이다, 표물을 내놔라!",
                    "비류연|앞길은 내가 정리하죠. 다들 뒤에서 구경이나 하세요. (게임 연출)",
                },
                Outro = new[] {
                    "호골채를 물리쳤다.",
                    "비류연|그런데 이상하네. 이 산적들은 극비 표물을 어떻게 알고 왔지?",
                },
                Encounters = new[] { new[] { "hogol_bandit", "hogol_bandit" }, new[] { "hogol_bandit", "hogol_bandit", "hogol_bandit" }, new[] { "paeho_jung_gwang", "hogol_bandit" } } },
            new StageData { Number = 4, Name = "용골채 매복", RecommendedLevel = 10, DooTotal = 2300,
                Intro = new[] {
                    "호골채에 이어 용골채 천 명이 길을 막았다.",
                    "표사|또 산적이라니, 이번엔 천 명이오! 이대로는 표사 여럿이 다칩니다!",
                    "비류연|한 명도 다치게 하면 안 돼요. 보수가 깎이니까.",
                },
                Outro = new[] {
                    "표행은 무사히 이어진다. 주작단은 핏빛으로 물든 옷으로 남창 천무학관 정문에 도착한다.",
                    "표물은 무사히 학관에 넘어가고, 이제 사제들과 이별할 시간이 온다.",
                    "남궁상|노사부님, 정말 여기서 헤어지는 겁니까?",
                },
                Encounters = new[] { new[] { "yonggol_bandit", "yonggol_bandit" }, new[] { "yonggol_bandit", "yonggol_bandit", "yonggol_bandit" }, new[] { "paeryong_wooak", "yonggol_bandit" } } },
            new StageData { Number = 5, Name = "주주 윤하루", RecommendedLevel = 13, DooTotal = 4700,
                Prologue = new[]
                {
                    new StoryScene { Title = "모옥의 마지막 겨울", Lines = new[] {
                            "주작단을 보낸 지 한 달 남짓. 모옥의 점심상에서 사부가 수염 '백아'를 쓰다듬으며 눈을 가늘게 떴다.",
                            "사부|류연아, 요즘 밤마다 어디를 쏘다니느냐. 은자는 어디서 났고?",
                            "비류연은 옥루주 한 병을 슬며시 내밀었다. 사부의 의심은 술 한 잔에 녹아내렸다.",
                            "비류연|(사부님이 말하는 뇌신의 힘에는 아직 손이 닿지 않아. 이 산에서는 더 배울 게 없어.)",
                            "새해 원단 새벽, 비류연은 사부 명의로 노후 연금을 들어 둔 뒤 봉뢰함과 뇌금 묵뢰를 짊어지고 모옥을 나섰다. 지금까지 쓰던 비뢰도는 연습용 가짜였다. 진짜 열 자루는 봉뢰함 안에 있다.",
                            "산 아래에서 중양표국의 남창행 표행과 마주쳤다. 비류연은 그 행렬 끝에 슬쩍 합류했다.",
                        } },
                },
                Intro = new[] {
                    "새해 원단, 비류연은 사부의 봉뢰함과 뇌금을 들고 가출했다.",
                    "주주의 객잔 윤하루에 사천제일 도객 염도가 나타났다. 그의 이름을 입에 담는 것은 금기다.",
                    "손님|저, 저 사람이… 염도?! 이름을 부르면 안 된다고 했는데!",
                    "염도|누가 날 불렀느냐. 오늘은 이 몸이 기분이 아주 나쁘다.",
                },
                Outro = new[] {
                    "비류연|여반장 한 방이면 충분하죠.",
                    "염도는 비류연의 '제자'가 되어 짐을 지고 따라나선다.",
                    "염도|… 제자 되기 싫어서 도망치려 해도 소용없겠지. 짐이나 지고 가마.",
                },
                Encounters = new[] { new[] { "jang_ugang" }, new[] { "yeomdo" } } },
            new StageData { Number = 6, Name = "남창 호아장", RecommendedLevel = 16, DooTotal = 3000,
                Prologue = new[]
                {
                    new StoryScene { Title = "접수 마감", Lines = new[] {
                            "표행을 따라 남창에 도착한 비류연과 염도. 그런데 천무학관 승천무제(입관시험) 접수는 한 달 전에 마감되었고, 응시자는 만 스물다섯 살까지다.",
                            "비류연|이거 곤란하게 됐네요. 방법이 없을까요?",
                            "비류연은 남창 정보조직 순풍당의 주인 경순이 나중해를 찾아가 '쌍낭이'라는 별명을 입에 올렸다. 금기를 건드린 대가로 나중해는 해법을 내놓았다.",
                            "나중해|염도 소협은 학관이 모시고 싶어 하는 무사부로, 소협은 추천패 승룡패를 쥔 특별전형으로 들어가면 되겠소.",
                            "비류연|그럼 승룡패는 어디 있죠?",
                            "나중해|… 호아장에 있소.",
                        } },
                },
                Intro = new[] {
                    "천무학관에 들어가려면 승룡패가 필요하다.",
                    "비류연|염도 아저씨는 무사부로, 나는 특별전형으로 들어가면 돼요. 호아장이 쥔 승룡패를 가져오죠.",
                    "염도|… 가져온다는 말이 그냥 '털어 온다'는 뜻이라는 건 알고 있다.",
                },
                Outro = new[] {
                    "후견인 빙검 관철수가 나타났다. 염도와 일검을 나눈 끝에 승룡패가 넘어온다.",
                    "빙검|… 염도. 네놈이 여기서 뭘 하고 있는 것이냐.",
                    "염도|이쪽 사정이 좀 복잡하다. 일단 한 판 하고 얘기하지.",
                },
                Encounters = new[] { new[] { "hoajang_disciple", "hoajang_disciple" }, new[] { "hoajang_disciple", "hoajang_disciple", "hoajang_disciple" }, new[] { "ho_cheonsang", "hoajang_disciple" } } },
            new StageData { Number = 7, Name = "천무학관 검혼관", RecommendedLevel = 19, DooTotal = 3500,
                Prologue = new[]
                {
                    new StoryScene { Title = "특별전형", Lines = new[] {
                            "학관은 염도를 무사부로 맞이했다. 비류연은 승룡패를 내밀고 특별전형을 치렀다.",
                            "응시생|특별전형이라니, 어디서 굴러온 놈이 시건방지게!",
                            "비류연|말이 많네. 일어나서 말해요.",
                            "시건방진 응시생 네 명은 일어나지 못했고, 비류연은 수석으로 합격했다. 대가로 '요주의 인물'로 분류되었지만 본인은 별로 신경 쓰지 않았다.",
                        } },
                },
                Intro = new[] {
                    "입관식 날, 지붕에서 낮잠을 자다 효룡과 친구가 되었다. 기숙사 검혼관으로 가는 길에 신입 장홍이 따라붙는다.",
                    "효룡|비류연 소협, 같은 701호 쪽이라면 저도 같이 가요. 이쪽은 신입 장홍입니다.",
                    "장홍|잘 부탁드립니다.",
                    "기숙사 앞 기관 노방에서 호아장의 호천강이 복수의 칼을 뽑았다.",
                    "호천강|네놈이 호아장의 승룡패를 훔쳐 간 놈이냐! 오늘 여기가 네 무덤이다!",
                },
                Outro = new[] {
                    "호천강의 패왕노호검이 반 토막 났다. 기관이 3단계까지 가동되어 은자 1500냥의 피해가 났지만 정당방위로 무죄다.",
                    "방 배정 결과, 701호의 룸메이트는 승천무제 수석 칠절신검 모용휘. 먼지 하나 못 참는 결벽증 천재다.",
                    "모용휘|… 그 신발로 방에 들어오지 마라.",
                    "비류연|(이 깔끔한 사람을 어떻게 골려 줄까.)",
                    "일행은 저녁을 먹으러 공동식당으로 향한다.",
                },
                Encounters = new[] { new[] { "trap_arrow" }, new[] { "trap_needle", "trap_arrow" }, new[] { "trap_log", "trap_needle" }, new[] { "ho_cheongang" } } },
            new StageData { Number = 8, Name = "천무식당", RecommendedLevel = 21, DooTotal = 3500,
                Intro = new[] {
                    "첫 저녁, 천무식당. 화산파의 윤준호가 선배들에게 조롱을 당하고 있다. 매화검법도 칠매검도 쓰지 못한다는 이유다.",
                    "종리학|화산파 제자라는 놈이 매화검법도 못 쓴다고? 화산의 수치로군!",
                    "윤준호|… 죄송합니다. 저도 쓰고 싶은데 몸이 말을 안 들어서.",
                    "비류연|(식판을 쥐며) 같이 밥 먹을 사람이 필요하겠네. 이쪽으로 와요.",
                    "윤준호가 일행에 합류한다. (싸움은 게임 연출)",
                },
                Outro = new[] {
                    "모용휘가 나타나 싸움을 말렸다. 종리학과 추가연은 이를 갈며 물러났다.",
                    "윤준호|칠매동에서 검보를 얻어 수련하다 매화 향기를 맡았더니 두드러기가 났어요. 그 뒤로 칠매검은커녕 매화검법도 못 써요.",
                    "효룡|그건 병이 아니에요. 검기검화 너머, 검향의 경지에 올랐다는 증거예요.",
                    "장홍|맞습니다. 검향지경이 몸에 반응하는 겁니다.",
                    "비류연|알레르기를 고치려 하지 마. 검향을 넘어서는 이미지를 그려 봐. 약보다 낫다.",
                    "윤준호는 얼떨떨한 얼굴로 고개를 끄덕였다.",
                },
                Encounters = new[] { new[] { "jongri_hak" }, new[] { "jongri_hak", "jongri_hak" }, new[] { "chu_gayeon", "jongri_hak" } } },
            new StageData { Number = 9, Name = "천무학관 운향정", RecommendedLevel = 23, DooTotal = 4000,
                Prologue = new[]
                {
                    new StoryScene { Title = "수강신청과 우뢰", Lines = new[] {
                            "수강편람은 베개만큼 두껍다. 비류연이 고른 전공은 음공. 사문에서 가져온 묵금을 쓰기 위해서고, 효룡도 따라 들었다. 담당은 천음선자 홍란이다.",
                            "천응방 직원|이 푸른 깃털의 고려산 해동청은 분타 직원 30명이 넘게 실패했습니다. 포기하시는 편이…",
                            "비류연|이름은 우뢰로 하죠. 이리 와, 우뢰.",
                            "해동청은 순순히 비류연의 팔에 앉았다. 전서응이 생겼다.",
                        } },
                    new StoryScene { Title = "무도총론", Lines = new[] {
                            "무당의 옥현진인이 백 년 전 이야기를 꺼냈다. 흑백을 가리지 않고 짓밟은 절대자 위천무, 천겁혈신.",
                            "옥현진인|백도의 무림맹과 흑도의 흑천맹이 연합했지만 크게 패했다. 태극신군 무신 혁월린과 패천도 무신마 갈중혁이 끝까지 맞섰지. 혈신은 부상을 입고 종적을 감췄다.",
                            "옥현진인|이후 혁월린은 천무삼성과 천무학관을, 갈중혁은 마천각을 세웠다. 이 이야기를 모르는 자는 없을 터인데.",
                            "비류연|천겁령? 위천무? 처음 듣는데요.",
                            "비류연은 숙제로 백 년사 조사를 받았다.",
                        } },
                    new StoryScene { Title = "애소저회", Lines = new[] {
                            "동호회 거리에서 구정회의 그림자 검혼회와 팔가회·군웅회의 도백회가 으르렁거렸다. 그 틈에서 3학년 진성곤 임성진이 곤으로 둘을 갈라놓고 비류연 일행을 끌고 갔다.",
                            "임성진|여기가 우리 동호회 애소저회다. 천관 미인들의 정보를 수집하는 곳이지.",
                            "비류연|정보가 돈이 된다고요? 가입하겠습니다. 지금 당장.",
                            "효룡·장홍·윤준호도 반강제로 따라 들어갔다.",
                        } },
                    new StoryScene { Title = "운향정의 소녀", Lines = new[] {
                            "학관을 둘러보던 비류연은 오검룡 이상만 드는 정자 운향정에서 걸음을 멈췄다. 밤하늘을 담은 듯한 눈을 한 소녀가 홀로 금을 타고 있었다.",
                            "소녀의 곡이 끝나자 비류연은 자기도 모르게 다가갔다. 그리고 허락도 없이 그녀의 입술을 훔쳤다.",
                            "소녀의 검이 번쩍였다. 비류연은 한 걸음 물러났고, 검기는 그의 앞섶만 갈랐다.",
                            "비류연|(이 사람은 눈빛을 읽을 수가 없다. 뭐지, 이 느낌.)",
                            "훗날 비류연은 그녀가 천하제일미로 불리는 빙백봉 나예린이라는 것을 알게 된다. 지금은 이름도 묻지 못했다.",
                        } },
                },
                Intro = new[] {
                    "수석으로 입관한 천무학관. 오검룡 이상만 드는 정자 운향정. 방금 일어난 일을 곱씹을 틈도 없이 인기척이 몰려온다.",
                    "위지천|감히 우리 소저께 무례를 범한 놈이 너냐! 선풍검룡 위지천이 상대해 주마!",
                    "소천군|위 대장님, 저희가 먼저 길을 막겠습니다. 검혼회 청풍검 소천군이 상대해 드리지요.",
                    "구창|도백회 일도파쇄 구창도 가겠다. 웬일로 청풍검과 뜻이 맞는군.",
                    "비류연|이쪽은 소저 이름도 모르는데... 뭐, 여기서 밀리면 설 자리가 없겠네요. 한 명씩 오세요.",
                },
                Outro = new[] {
                    "선풍뇌절 한 줄기가 위지천의 검을 쳐냈다. 손목은 남겨 두었다.",
                    "위지천|… 크윽, 이 한 번으로 끝났다고 생각하지 마라.",
                    "소녀는 어느새 사라지고 없었다. 이름조차 묻지 못한 것이 못내 아쉽다. 사람들이 몰려오기 전에 자리를 뜬다.",
                    "며칠 뒤 학관 정문. 3개월 과제를 마친 청룡단과 주작단 열여섯이 반 각 차로 귀환했다.",
                    "비류연|(제자들 꼴이 흐트러졌네. 다시 철저히 단련시켜야겠어.)",
                },
                Encounters = new[] { new[] { "gu_chang" }, new[] { "so_cheongun" }, new[] { "gu_chang", "so_cheongun" }, new[] { "wiji_cheon" } } },
            new StageData { Number = 10, Name = "백향관 침투", RecommendedLevel = 25, DooTotal = 4200,
                Prologue = new[]
                {
                    new StoryScene { Title = "빙백봉의 정체", Lines = new[] {
                            "운향정 사건 이후 일주일. 위지천이 빙봉영화수호대를 소집해 나예린의 초상화를 내리고 비류연의 수배 초상화를 걸어 검기로 찢었다. 모두의 공적이 된 것이다.",
                            "비류연은 애소저회 비연태에게서 소녀의 정체를 알아냈다.",
                            "비연태|천하제일미 빙백봉 나예린. 무림맹주 나백천의 손녀이자 검후 이옥상의 의발전인이지.",
                            "비류연|포기할 마음은 없는데요. 오히려 해볼 마음이 드네.",
                        } },
                    new StoryScene { Title = "음율관의 홍란", Lines = new[] {
                            "방음이 완비된 음율관 3층. 천음선자 홍란의 음공 수업에는 비류연도, 음공과 검기의 흐름을 비교하러 온 모용휘도, 효룡도 앉아 있다.",
                            "홍란|연주를 청하마. 묵금을 들어 보거라.",
                            "비류연|사양하겠습니다.",
                            "홍란이 살짝 건드린 묵금의 뇌령사 현에 손끝이 베여 피가 맺혔다. 홍란은 이날부터 이 1학년을 눈여겨보게 된다.",
                        } },
                    new StoryScene { Title = "새벽의 나예린", Lines = new[] {
                            "새벽, 나예린은 풀잎의 이슬 한 방울을 검기로 세 갈래로 가른다. 사저 독고령이 찾아와 위지천을 방패막이로 둔 일을 두고 말을 건넨다.",
                            "나예린|지켜야 할 약속이 있어요. 다음엔 실수하지 않겠다고 약속했어요.",
                            "운향정에서 자신의 용안, 곧 마음을 읽는 신안이 읽지 못한 남자에게 한 말이다.",
                            "같은 날 이진설이 새 기술 쌍검이연십이참격을 독고령에게 시험하려 했지만 나예린이 대신 나섰다. 스물네 줄기 검기가 모두 빗나가고 나예린의 검끝이 이진설의 목뒤에 닿았다.",
                            "나예린|큰 기술은 허점도 커요.",
                        } },
                    new StoryScene { Title = "추일태의 내기", Lines = new[] {
                            "구정회 상부의 명을 받은 일은무영 추일태가 비류연에게 은신 내기를 걸었다. 금남의 성지, 여성 전용 기숙사 백향관에 잠입해 정해진 물건을 가져오는 것이다.",
                            "추일태|내기 돈은 은자 이백 냥이다. 해볼 텐가?",
                            "비류연|돈이 적게 걸린 내기는 안 한다고 했잖아요. 이백 냥이면 하죠. 목표는요?",
                            "추일태|나예린의 방, 301호.",
                        } },
                },
                Intro = new[] {
                    "신월야. 백향관을 두른 난화백무진이 어둠 속에서 움직이기 시작한다. 30년간 백여 번 뚫으려 했지만 한 번도 성공하지 못한 곳이다.",
                    "추일태|독한녀라 불리는 절독난향십이선녀가 3교대로 길목을 지켜. 건방진 신입이 해낼 수 있을까?",
                    "비류연|(복면을 쓰며) 내기는 내기죠. 은자 이백 냥이면 충분히 가치가 있어요.",
                    "복면을 쓴 비류연은 봉황무로 그림자를 탄다. 담장 밖 숲에서는 효룡·장홍·윤준호가 망을 본다.",
                    "윤준호|저, 저는 여기 있는 것만으로 이미 죄를 지은 기분이에요...",
                    "효룡|정확히는 아직 아무 죄도 짓지 않았습니다. 망만 보고 있으니까요.",
                    "장홍|코가 아립니다. 절독난향이라더니 담장 밖까지 향이 번지는군요.",
                    "비연태|(수풀에서 불쑥) 쉿! 삼십 년 숙원이다! 비류연이 성공하면 애소저회의 이름이 남는다!",
                    "효룡|...그 기록에서 저희 이름은 빼 주십시오.",
                },
                Outro = new[] {
                    "301호 문이 열렸다. 무기도 없이 달려온 나예린이 허공섭물로 애검 봉루를 쥐고 백광검뢰를 날린다.",
                    "나예린|누구죠, 당신은!",
                    "비류연은 방에 쓰러져 있던 진령에게 뇌령사를 감아 둔 터였다. 괴뢰처럼 일어선 진령이 나예린의 손을 묶는 틈에, 분뢰수와 검기가 부딪치는 폭풍 속으로 빠져나간다.",
                    "백 장 밖에서 뇌령사를 당기자 목표물이 손으로 날아왔다. 내기는 비류연의 승리다.",
                    "그런데 이상하다. 같은 밤 검후처의 비상종이 울렸다. 그곳에 닿은 또 다른 그림자는 비류연이 아니었다.",
                    "같은 시각 검혼관 옆 숲. 어둠 속에서 무흔일호가 무릎을 꿇었다.",
                    "무흔일호|죄송합니다, 삼공자님. 실패했습니다.",
                    "효룡|백향관이 만만치 않은 곳이란 건 알고 있었어요. 남에게서 훔친 무공으로 이겨 봐야 치욕일 뿐입니다. 전 처음부터 대사형의 계획에 반대였습니다.",
                    "효룡은 당분간 연락을 끊으라 이르고 어둠 속으로 사라지는 부하를 보냈다.",
                },
                Encounters = new[] { new[] { "sunyeo" }, new[] { "sunyeo", "sunyeo" }, new[] { "na_yerin" } } },
            new StageData { Number = 11, Name = "승급시험", RecommendedLevel = 27, DooTotal = 4600,
                Prologue = new[]
                {
                    new StoryScene { Title = "삼성제 약속", Lines = new[] {
                            "다음 날 나예린이 애소저회를 직접 찾아와 비류연의 목에 검을 겨눴다.",
                            "나예린|어제 백향관에 침입한 사람이 소협인가요?",
                            "비류연|글쎄요.",
                            "나예린|올해 삼성무제에서 우승해 보세요. 그러면 당신의 말을 믿어드리죠.",
                            "비류연|(삼성제가 뭔지도 모른 채) 우승하면 저를 다른 눈으로 봐 주세요.",
                            "같은 때 검존 공손일취도 모용휘에게 검성전 우승을 요구했다.",
                        } },
                    new StoryScene { Title = "대사형의 귀환", Lines = new[] {
                            "비류연은 염도를 찾아가 '주작단은 내 제자니 네 사제'라고 폭탄선언을 했다. 다섯 달 동안 인피면구를 쓴 가짜 노사부였다는 사실은 주작단에게 끝까지 비밀이다. 그래서 '만난 적 없는 대사형'으로 나서기로 했다.",
                            "다음 날 연무장. 인피면구 없이 나타난 비류연이 외쳤다.",
                            "비류연|내가 너희들의 대사형이다!",
                            "노학|뭐? 이런 애송이가 무슨… 으아악!",
                            "달려든 노학은 삼복구타권법에 날아갔고, 묵룡환을 본 주작단은 일제히 포권했다.",
                            "염도가 능청스럽게 보증을 섰다. 비류연의 빚은 점점 늘어난다.",
                        } },
                    new StoryScene { Title = "모용휘의 서명", Lines = new[] {
                            "비류연이 모용휘에게 붉은 연서 서른두 통을 건넸다. 모용휘는 세 쪽도 안 읽고 속독으로 전부 훑고 내려놓았다.",
                            "비류연|그 여자들을 떼어 줄까? 여기 서명만 해.",
                            "모용휘는 내용도 보지 않고 서명했다. 그것은 애소저회 가입신청서였다. 동호회 지원금은 회원 수에 비례하니 유령회원이라도 쓸모가 있다.",
                        } },
                    new StoryScene { Title = "문일기의 안법 수업", Lines = new[] {
                            "화산비천응 문일기 노사의 검술 강론. 안법이 사 할이라는 지적에 눈밑까지 내려온 비류연의 앞머리가 걸렸다.",
                            "문일기|머리를 걷어라. 안법이 사 할이다.",
                            "비류연|제 머리카락이 방해될 정도의 상대를 아직 못 만났거든요. 사부님이 눈동자를 가리고 다니라고도 했고요.",
                            "문일기가 회풍무류검의 일부를 갑자기 뿜었다.",
                            "비류연|마흔여덟 변화네요. 실초 열둘에 허초 서른여섯.",
                            "문일기는 입을 다물지 못했다.",
                        } },
                    new StoryScene { Title = "윤준호의 칠매검", Lines = new[] {
                            "두 달째 윤준호는 비류연의 소개서를 들고 염도에게 매일 얻어맞는다. 충격요법이라는 이름의 특훈이다. 주작단 열여섯도 같은 연무장에서 구르는 사이다.",
                            "어느 날 딴생각을 하던 염도가 무심결에 절초 염천풍을 뿜자 윤준호는 얼어붙은 채 화산 수련동 이후 처음으로 칠매검을 휘둘렀다. 매화향과 홍광의 검기가 염도의 도기를 흩었다.",
                            "곧 두드러기가 번져 검을 떨어뜨렸지만, 염도는 처음으로 윤준호를 다르게 보았다.",
                            "염도|너 정말 검향지경이었냐?",
                        } },
                    new StoryScene { Title = "복도의 소동", Lines = new[] {
                            "수업 사이 복도에서 비류연의 등에 멘 뇌금 묵뢰가 청룡단 하세인과 부딪혔다. 하세인이 빼든 철비룡을 비류연은 맨손으로 잡았다.",
                            "하세인이 물러난 뒤 비류연은 가볍게 손가락을 접었다. 하세인의 허리띠가 끊기고 복도 저편에서 강유란의 비명이 울렸다.",
                            "마침 지나던 검존 공손일취가 모용휘를 꾸짖고 조부 모용정천을 깎아내리자 모용휘가 반발했다.",
                            "공손일취|올해 삼성제 검성전에서 우승한다면 네 말을 믿어 주마.",
                            "모용휘|하겠습니다.",
                            "공손일취의 가슴에는 백 년 전의 어두운 기억이 얹혀 있다. 천겁멸살의 대회전 직전 아미산 근처에서 입은 상처 때문에 그는 그 자리에 서지 못했다.",
                        } },
                },
                Intro = new[] {
                    "삼성제에 나가려면 삼검룡 이상이 되어야 한다. 승급시험은 같은 등급 두 명을 이기거나 삼백 초 동안 비기면 되고, 실력만 되면 등급을 건너뛸 수 있다.",
                    "비류연|(사물함의 비무첩 더미를 뒤적이며) 이 중에서 고르면 되는 거죠? 복도에서 시비를 건 청룡단 하세인, 그리고 백호수를 단 단대풍과 강현추로 하죠.",
                },
                Outro = new[] {
                    "단대풍의 살기 어린 일검은 비류연의 정수리 한 치 앞에서 보이지 않는 막에 튕겨 나갔다. 일권에 흉곽이 부서진 단대풍은 쓰러졌고, 강현추는 기권했다.",
                    "비류연은 육검룡이 되었고, 모용휘도 곤륜쌍검의 도발을 은하섬광으로 물리치고 오검룡이 되었다. 둘 다 삼성제 참가 자격을 얻었다.",
                    "도백회의 군웅팔가회 회의실. 회주대리 천기룡 단목우가 입을 열었다.",
                    "단목우|모용휘를 반드시 우리 쪽으로 끌어와야 합니다. 이번에야말로 십 년 숙원을 풀 때입니다.",
                    "구정회에서는 모용휘를 견제할 연수합벽회를 보낼 준비를 한다. 두 세력 모두 천무학관의 새 별에 눈을 돌리기 시작했다.",
                },
                Encounters = new[] { new[] { "ha_sein" }, new[] { "gang_hyeonchu" }, new[] { "dan_daepung", "gang_hyeonchu" } } },
            new StageData { Number = 12, Name = "삼성무제 본선", RecommendedLevel = 29, DooTotal = 5200,
                Prologue = new[]
                {
                    new StoryScene { Title = "삼성무제 개막", Lines = new[] {
                            "제30회 천무삼성무제 개막식. 검성 모용정천, 도성 하후식, 검후 이옥상이 참관석에 앉았다. 관주 마진가가 대회를 선언하고 삼절검 청흔이 관도 대표로 선서했다.",
                            "마진가|삼성무제는 반년에 걸쳐 열린다. 검성전, 도성전, 검후전, 삼성대전의 결승 여덟 명은 마지막에 몰아서 싸운다.",
                            "마진가|우승자에게는 일급영단과 전대 고수의 무공 심득이 주어진다.",
                            "비류연이 등에 멘 것은 뇌금 묵뢰. 음공을 배운 지 반년이다.",
                            "장홍|(눈을 감으며) … 불안합니다.",
                        } },
                    new StoryScene { Title = "나 소저, 올 줄 알았어요", Lines = new[] {
                            "첫 공식전. 비류연은 해남파 쾌환검 단평을 앞에 두고 관중석의 나예린에게 손을 흔들었다. 비무장이 질투의 야유로 가득 찼다.",
                            "비류연|나 소저, 올 줄 알았어요!",
                            "비류연|여긴 바보들이 많네요.",
                        } },
                },
                Intro = new[] {
                    "비류연|음공이 비무대에 어울리지 않는다는 걸 첫판에서 깨달았어요. 거리가 너무 가깝거든요. 그러니까 묵금을 들고 직접 때리기로 했어요.",
                    "홍란|음문의 명예가 땅에 떨어졌구나! 저 녀석, 가만두지 않겠다!",
                    "별명은 '운수대통 격타금'이 된다.",
                },
                Outro = new[] {
                    "효룡|전옥기가 송풍루에서 제 앞에서 비류연 님을 험담한 값은 이걸로 치르신 셈이군요.",
                    "비류연은 뇌령사 현으로 전옥기의 콧등을 베고, '충격요법'으로 이빨까지 부러뜨려 장외로 날려 보냈다.",
                    "비류연은 연승을 이어 간다. 아무도 이 1학년의 음공을 본 적이 없다.",
                },
                Encounters = new[] { new[] { "dan_pyeong" }, new[] { "eonga_yeong", "dan_pyeong" }, new[] { "jeon_okgi" } } },
            new StageData { Number = 13, Name = "삼성대전 결승", RecommendedLevel = 31, DooTotal = 6000,
                Prologue = new[]
                {
                    new StoryScene { Title = "안목품평회", Lines = new[] {
                            "마감 직전, 주작단은 한 달 생활비를 가죽 주머니에 담아 금영호에게 맡겼다. 전부 비류연에게 건다.",
                            "염도|무조건 이겨야 해. 빙검, 그 얼음땡이의 제자들보다 잘해야 한다.",
                            "염도도 자기 돈을 던졌다.",
                        } },
                    new StoryScene { Title = "세 개의 무승부", Lines = new[] {
                            "검후전 결승은 나예린과 빙검의 딸 관설지의 무승부로 끝났다. 시합 뒤 염도가 관설지의 어깨를 잡고 외쳤다.",
                            "염도|소운!",
                            "관설지의 어머니 이름은 혁소운. 염도와 빙검의 사부의 딸로, 염도가 마음에 품었다가 빙검에게 보낸 사람이다.",
                            "도성전은 청성 비전 도법 청류흔을 쓴 유엽성과 신도문 하윤명의 무승부. 검성전은 모용휘 대 청흔. 은하류 개벽검과 삼정태극검혜의 무극검이 부딪쳐 둘 다 쓰러졌다.",
                            "세 부문이 한꺼번에 무승부가 된 것은 48년 만이다. 무승부자는 종합 결승에 나가지 못한다. 길이 열렸다.",
                        } },
                },
                Intro = new[] {
                    "준결승에서 비류연은 사천당문 독랄수 당문천과 마주쳤다.",
                    "당문천|독을 쓰면 안 된다는 규칙은 없으니까. 각오해라.",
                    "결승 상대는 백일연무를 마치고 돌아온 선풍검룡 위지천. 그는 비류연에게 설욕하기 위해 검을 갈았다.",
                    "위지천|이번에는 운향정 때와 다를 것이다, 비류연!",
                },
                Outro = new[] {
                    "비류연은 운향정에서 나예린이 켠 곡을 연주했다. 위지천의 마음에 틈이 났다. 선풍우뢰, 선풍참혼, 청성의 금지 살초 천풍마뢰참도 보이지 않는 막과 끊기지 않는 뇌령사에 막혔다.",
                    "비류연|비뢰도 오의, 뢰광류하곡.",
                    "왼손에서 은빛 뇌령사 세 줄기가 뻗었다. 위지천의 검이 떨어졌다. 관중석에서 공손일취가 벌떡 일어섰다.",
                    "다른 부문이 모두 무승부여서 비류연은 삼성무제 종합 우승까지 차지했다. 어부지리라는 소문이 돌았다.",
                    "나예린|약속대로 백향관 의심을 사과드립니다.",
                    "비류연|사과 선물로 하나만 받겠습니다.",
                    "두 번째 입맞춤이었다. 이번에는 그녀도 저항하지 않았다.",
                    "그날 밤 효룡은 무흔일호에게 비류연의 사문을 조사하라고 지시했다. 때가 다가오고 있다.",
                },
                Encounters = new[] { new[] { "eonga_yeong_b", "dang_munceon" }, new[] { "wiji_cheon_baek" } } },
            new StageData { Number = 14, Name = "무당산 고립", RecommendedLevel = 33, DooTotal = 6500,
                Prologue = new[]
                {
                    new StoryScene { Title = "은하류 개벽검 대 굉천혈영도법", Lines = new[] {
                            "무당산 합숙지에 쌍도를 쥔 괴인이 뛰어들었다. 모용휘의 은하류 개벽검과 괴인의 굉천혈영도법이 백오십 합을 넘겨 부딪친다.",
                            "청흔|저 도법은 흑도의 우상 갈중혁의 굉천혈영도법이오. 쌍도를 쓴다면 손자 갈효봉이 틀림없소.",
                            "싸움 도중 효룡이 외친다.",
                            "효룡|회선십자인이 뒤에서 온다!",
                            "청흔은 효룡을 의심한다. 비류연은 구경만 한다.",
                            "비류연|돌아오지 않는 메아리와 회수 없는 투자는 싫어요.",
                            "마천각이 부린 흑도 살수 천지쌍살 가운데 천살의 암습 검기는 남궁상을 던져 피하게 했을 뿐이다. 비기 대 비기. 모용휘가 피를 토하고 갈효봉은 초혼섭령술의 금제가 깨져 폭주한다. 천지쌍살은 천라지망으로 합숙지를 고립시키고 물러난다. 염도가 '곧 무당파에서 지원이 온다'고 외쳐 겨우 사기를 붙든다.",
                        } },
                    new StoryScene { Title = "효룡의 고민", Lines = new[] {
                            "효룡은 무흔 일호를 만나 따진다.",
                            "효룡|칠 년 전 참극 뒤 천마뢰에 갇혀 있어야 할 형이 어떻게 무당산에 나타났죠?",
                            "천지쌍살을 사적으로 부릴 사람은 마천각의 대공자뿐이다.",
                            "청흔이 효룡의 정체를 추궁하며 검기점혈을 쏘지만 비류연이 막는다.",
                            "청흔|신분이 불확실한 사람은 비류연, 효룡, 장홍 셋뿐이다.",
                            "윤준호는 이십 일 전 산속에서 해진 도복의 도사에게 왕따 이야기를 털어놓았다. 염도는 그것을 기연이라 평한다.",
                        } },
                    new StoryScene { Title = "사면초가를 연주하라", Lines = new[] {
                            "염도의 \"곧 무당파에서 지원이 온다\"는 거짓말이었다. 일행은 천라지망에 갇혔고, 비류연은 달빛 아래 묵금을 켠다.",
                            "비류연|저쪽에서 초가를 안 불어 주니 이쪽에서라도 연주해야죠.",
                            "나예린이 금음에 이끌려 찾아온다.",
                            "비류연|스스로 수습 못 하면 무슨 천무학관이에요. 염 사부님, 한 명도 죽지 않을 때까지 굴리세요. 이레 동안.",
                            "염도|...좋다. 내공 없이 오 장 밖 항아리의 물을 뜻으로 움직여라! 파문만 일으켜도 합격이다!",
                            "이레의 특별 강화 훈련이 시작된다. 염도는 방해가 될까 무진자 장로의 혈도를 짚어 재워 둔다.",
                            "정작 비류연은 나무 그늘에서 낮잠이다. 눈짓 한 번에 사과가 툭 떨어지고, 연못 물고기가 뛰어올라 그의 무릎에 안긴다.",
                            "노학|대사형은 대체 뭘 하시는 겁니까?",
                            "염도|(식은땀) 저 녀석은 가망이 없어서 감시역을 맡겼다. 신경 쓰지 마라.",
                            "그 틈에 비류연은 죽은 교관 진조운의 인피면구를 쓰고 가짜 '노사부'로 나서 천살의 수하 초혼오귀검 다섯을 털어 옷까지 벗겨 돌려보낸다. 산적으로 변장해 지살의 수하 명부오귀장도 같은 꼴로 만든다.",
                        } },
                },
                Intro = new[] {
                    "이레째 해가 질 무렵 천지쌍살의 수하들이 다시 쳐들어온다. 항아리를 터뜨린 일곱 명을 비롯해 천관도들은 처음 싸울 때와 다르다.",
                    "갈효봉|크르르… 마령신단이… 몸을 태운다…!",
                    "마령신단을 먹은 갈효봉이 다시 폭주한다. 천살과 지살이 직접 나선다.",
                },
                Outro = new[] {
                    "비류연이 손가락질로 천살의 초혼령과 애검과 오른팔을 떨어뜨렸고, 염도는 지살의 오른팔을 베었다.",
                    "천살|그분께서 용서하지 않으실 것이다…!",
                    "쌍살은 그 말만 남기고 도망친다.",
                    "비류연의 심뢰가 갈효봉의 마음을 베자 정신이 돌아왔다. 하지만 심맥이 끊어진 몸은 되살릴 수 없었다.",
                    "갈효봉|아룡아. 너의 공부를 보여 다오.",
                    "효룡의 검이 형의 목숨을 거두었다.",
                    "갈효봉|겁… 하늘의 겁을 조심……",
                    "효룡은 피의 맹세를 한다. 한편 흑천맹주는 철각비마대를 학관으로 보냈고, 마천각의 대공자는 \"아룡, 오지 마라\"라고 혼잣말을 한다.",
                },
                Encounters = new[] { new[] { "chohon_gwigeom", "chohon_gwigeom", "myeongbu_jang" }, new[] { "gal_hyobong" }, new[] { "cheonsal", "jisal" } } },
            new StageData { Number = 15, Name = "철각비마대", RecommendedLevel = 35, DooTotal = 7000,
                Prologue = new[]
                {
                    new StoryScene { Title = "진혼제", Lines = new[] {
                            "천무학관 직속 첩보조 스무 명이 몰살당해 진혼제가 열린다. 철각비마대가 이틀 거리까지 왔다는 전서가 닿았지만, 마진가는 분노를 누르고 의식을 주관한다.",
                            "오색 비단으로 꾸민 나예린이 진혼검무를 춘다. 장내가 숨을 멈추었다가 \"천무학관 만세!\" 함성으로 터진다.",
                            "비류연|(뾰로통하게) 너무 예뻐서 남과 같이 보기 아깝잖아요.",
                        } },
                },
                Intro = new[] {
                    "진혼제를 마친 이튿날 새벽, 흑천맹의 철각비마대가 갈효봉의 죽음을 묻겠다며 학관 길목까지 달려온다.",
                    "철각비마대원|갈효봉 공자의 죽음에 대해 해명하라!",
                    "길목에 한 청년이 서 있다.",
                    "비류연|돌아가면 되잖아요?",
                    "비류연이 묵룡환 두 개를 풀자 눈이 황금빛으로 변한다.",
                },
                Outro = new[] {
                    "사대질풍의 철쇄선풍은 천지이분에 갈라졌고, 철비십이격의 투창은 진야에 흘렀다. 부대주 위무상은 염도의 칼을 앞에 두고 구천학이 던진 창에 목숨을 건졌다.",
                    "구천학|일기승부를 청하겠소.",
                    "비류연은 허공격상과 무영인을 모두 피하고 묵성에 올라타 구천학의 목에 비뢰도를 겨눈다.",
                    "구천학|… 졌다.",
                    "철각비마대는 삼십 년 만에 말머리를 돌렸다. 목격한 사람은 모두 입을 다물었다. 거짓말쟁이가 되기 싫었기 때문이다.",
                },
                Encounters = new[] { new[] { "sadae_jilpung", "sadae_jilpung", "sadae_jilpung" }, new[] { "cheolbi_gyeok", "cheolbi_gyeok", "wi_musang" }, new[] { "gu_cheonhak" } } },
            new StageData { Number = 16, Name = "저잣거리 습격", RecommendedLevel = 37, DooTotal = 7500,
                Prologue = new[]
                {
                    new StoryScene { Title = "흑천맹의 사신", Lines = new[] {
                            "검마 초월이 흑천맹주의 서신을 들고 학관을 찾는다. 요구는 100년 전 합의문에 따른 진상조사. 마진가는 받아들인다.",
                            "학관 앞에서 비류연과 어깨를 부딪친 초월이 검을 뽑았으나 머리카락 세 가닥만 날렸다. 눈을 본 초월이 경악한다.",
                            "효룡|제 친구입니다.",
                            "효룡이 전음으로 말하자 초월이 물러난다.",
                        } },
                    new StoryScene { Title = "사중화 은설란", Lines = new[] {
                            "조사관은 갈효봉의 정혼자 사중화 은설란. 쾌활한 겉모습 뒤에 정체를 알 수 없는 눈이 있다.",
                            "은설란|호위는 모용 소협과 나 소저죠? 자, 같이 옷 사러 가요!",
                            "은설란이 나예린의 손을 끌고 옷을 사러 남창 저잣거리로 나선다.",
                        } },
                },
                Intro = new[] {
                    "번화가 한복판. 노인, 아낙, 문사, 농부, 노부부… 지나가는 모두가 자객이다.",
                    "오성루에서 술을 마시던 비류연이 맹수의 움직임을 눈치챘다.",
                    "비류연|염도, 계산은 네가 해라. 뭔가 큰일이 터졌다.",
                    "염도|사부! 계산은 하고 가셔야…!",
                },
                Outro = new[] {
                    "백인회주의 손목이 잘렸고, 나예린의 한상옥령신검이 쌍귀 형제를 베었다. 은설란은 비명 한 번 지르지 않고 두 사람의 실력을 가늠했다.",
                    "비류연|도와준 사람은 감사를 받을 자격이 있죠.",
                    "비류연은 세 번째 입맞춤을 훔친다. 나예린은 이번에도 검을 뽑지 못했다.",
                    "나예린|… 기회를 놓쳤을 뿐입니다.",
                    "이틀 뒤 비류연은 은설란의 세 번째 호위가 되었다. 조사관의 요청이라 했다.",
                    "호위로 나선 나예린도 일행과 함께 움직이게 되었다. (동료 합류: 나예린 — 파티는 최대 셋이라 강호 탭의 \"파티 편성\"에서 동료 셋을 고른다.)",
                },
                Encounters = new[] { new[] { "baegin_jagaek", "baegin_jagaek", "baegin_jagaek" }, new[] { "ssanggwi", "baegin_jagaek" }, new[] { "baek_tanjeo" } } },
            new StageData { Number = 17, Name = "마하령의 분노", RecommendedLevel = 39, DooTotal = 8000,
                Prologue = new[]
                {
                    new StoryScene { Title = "화산지회 후보 선발", Lines = new[] {
                            "화산규약지회 후보 선발이 한창이다. 마진가는 신분이 불확실한 비류연과 효룡의 서류에도 합격 도장을 찍는다.",
                            "후보는 백검조(천익검 늑기한)와 흑검조(마검자 고약한)로 나뉜다.",
                            "고약한|활검은 위선이다.",
                        } },
                    new StoryScene { Title = "줄 위의 비무", Lines = new[] {
                            "고약한의 수업은 가는 줄을 불규칙하게 걸고 그 위에서 비무를 하는 것이다. 견학하던 은설란이 나예린과 겨루고, 칠채윤무와 월하비연이 부딪치자 둘 다 감탄한다. 나예린이 웃는다.",
                            "효룡 대 비류연은 비류연의 승리로 끝났다.",
                            "고약한|… 비류연. 주의 대상으로 올리겠다.",
                        } },
                    new StoryScene { Title = "운향정의 눈물", Lines = new[] {
                            "은설란은 운향정에서 죽은 정혼자 갈효봉을 그리워하다 효룡과 마주친다.",
                            "효룡|… 제가 형을 죽였어요. 제 손으로.",
                            "죄책감에 효룡은 말을 못 하고 운다. 은설란이 그를 안아 주는 모습을 이진설이 보고 오해해 달아난다.",
                            "다음 날 이진설은 은설란에게 효룡을 좋아한다고 고백한다.",
                            "염도와 비류연의 제자연장결정전은 이번에도 비류연의 승리. 봉황무 오의 비경에 뒤통수를 맞은 염도는 181일 뒤를 기약한다.",
                        } },
                    new StoryScene { Title = "순풍루의 밤", Lines = new[] {
                            "사흘 전 밤 비류연은 몰래 기숙사를 빠져나가 정보상 나대이의 주루 순풍루를 찾았다. 현상금 은 닷 냥에 눈이 멀어 소문의 미녀를 뒤쫓았다.",
                            "천축대승유가신공. 몸이 네 배로 부풀었다가 다시 날씬해지는 모습을 엿보고 말았다.",
                            "비류연|뚱땡이 소저!",
                            "비류연은 칼바람을 피해 달아났다.",
                        } },
                },
                Intro = new[] {
                    "군웅회주 철옥잠 마하령이 비류연에게 뺨을 맞고도 사과를 거부한다. 신응대가 포위하고 팽혁성이 칼을 뽑는다.",
                    "팽혁성|감히 회주님께 손을 대다니, 목을 베어 주마!",
                    "비류연|놓아주세요, 부탁합니다. 이 말을 하기 전에는 안 놓아요.",
                    "비류연은 마하령의 손목을 쥔 채 놓지 않는다.",
                },
                Outro = new[] {
                    "비류연은 마하령을 방패처럼 세워 팽혁성의 칼끝을 틀게 했다. 구정회주 창천룡 용천명이 나타났다. 마하령은 어린 시절 \"뚱땡이!\"라 불렀던 그를 거부했다.",
                    "용천명의 백룡검이 번개처럼 뽑혔다. 비류연은 잔상만 남기고 피했으나 옷이 갈라졌다. 마하령의 허리띠가 끊어지고 사과 요구는 계속된다.",
                    "그때 늑기한과 고약한이 나타나 서로 으르렁거렸고, 둘은 중간 평가에서 승부를 보기로 했다.",
                    "마진가는 총 노사 빙검에게 마천각의 전력 분석을 보여 주며, 십팔 년 봉인된 환마동의 개방을 결정한다.",
                },
                Encounters = new[] { new[] { "sinung_daewon", "sinung_daewon", "sinung_daewon" }, new[] { "pae_hyeokseong", "sinung_daewon" }, new[] { "ma_haryeong" } } },
            new StageData { Number = 18, Name = "태극의 인재", RecommendedLevel = 41, DooTotal = 8500,
                Prologue = new[]
                {
                    new StoryScene { Title = "혁월린의 유언", Lines = new[] {
                            "오래전, 온몸에 거미줄 같은 상처를 입은 태극신문 무신 혁월린이 제자 둘을 불렀다. 염도와 빙검에게 남긴 유언은 이랬다.",
                            "혁월린|염백과 빙혼. 정과 반을 아우를 인재를 찾아라. 신도 홍염과 신검 빙루가 길을 인도할 것이다. 별의 기운을 가진 자다.",
                            "흉수의 이름은 끝내 말하지 않았다.",
                        } },
                    new StoryScene { Title = "싸우려면 돈을 내라", Lines = new[] {
                            "비류연은 도전자가 늘자 소문을 퍼뜨렸다.",
                            "비류연|싸우려면 대전료를 내세요. 염도를 이길 실력이 아니면 돈만 날립니다.",
                            "소문은 염도의 숙적 빙검의 귀에 들어갔다. 비류연이 염도를 이겼다는 헛소문이라면 빙검은 직접 확인해야 했다.",
                        } },
                },
                Intro = new[] {
                    "총 노사 빙검 관철수가 나타난다.",
                    "빙검|조건이 맞으면 싸우겠다는 말이 사실이냐?",
                    "비류연|제가 이기면 부탁 두 가지를 들어주세요.",
                    "그 전에 용천명이 비류연의 검이 장식품인지 확인하겠다며 길을 막는다.",
                },
                Outro = new[] {
                    "빙령수혼과 백빙신첨과 백빙신정이 모두 봉황무에 빗나갔다.",
                    "비류연|이제야 좀 춥군요.",
                    "염도가 신원 보증인으로 나타나자 빙검은 말을 잃었다. 비류연이 오른팔의 묵룡환까지 벗자 빙검이 비장절초 무영무흔비월인의 기수식을 잡는다.",
                    "빙검|네가 태극을 하나로 합칠 인재인지 이 일검으로 확인하겠다!",
                    "비류연|난 그런 사람 아니라니깐요!",
                    "눈을 감은 비류연은 홀황경으로 두 개의 얼음 칼날을 읽어 두 손가락으로 부수고, 삼복구타권법 중복의 무한연환구타로 빙검을 쓰러뜨린다.",
                    "비류연|부탁 첫째. 빙검 선생님, 제 제자가 되어 주세요.",
                    "염도|사부님!",
                    "염도가 \"사부님!\" 하고 부르자 빙검은 입을 다물 수밖에 없었다. 둘째와 셋째 부탁은 나중에 청산하기로 한다.",
                },
                Encounters = new[] { new[] { "yong_cheonmyeong" }, new[] { "gwan_cheolsu" } } },
            new StageData { Number = 19, Name = "환마동 시험", RecommendedLevel = 43, DooTotal = 9000,
                Prologue = new[]
                {
                    new StoryScene { Title = "제자가 된 사형", Lines = new[] {
                            "그렇게 비류연은 의약전 독실에서 며칠째 누워 지낸다. 수업과 호위를 피하려는 꾀병이다.",
                            "병문안을 온 나예린은 염도가 사부에게 바칠 음식 바구니를 들고 서 있는 모습을 본다. 염도와 빙검은 \"사제가 사형에게 대든다\"며 서로를 외면한다.",
                            "비류연|(콜록) 나 소저, 와 줘서 고마워요… 몸이 영 안 좋네요.",
                            "비류연은 나예린 앞에서만 병약한 척한다.",
                        } },
                    new StoryScene { Title = "환마동의 개문", Lines = new[] {
                            "전서응이 정파 각 문파로 날아간다. 화산규약지회 대표 선발전의 참가자는 모두 환마동 시험을 통과해야 한다. 현 관도 108명과 30세 이하 졸업생 213명, 모두 321명이다.",
                            "마진가|출구를 대신 찾아 줄 수 없다. 두려운 자는 나서라.",
                            "환마동은 몽환소혼향과 환영봉마진으로 각자의 가장 두려운 것을 환상으로 보여 준다. 환상에 먹히면 백치나 광인이 된다.",
                            "순번표 44번, 여덟 갈래 문 중 네 번째 문. 위지천은 정체불명의 인물에게서 받은 비황신침과 염마뢰를 품고 비류연의 뒤를 따른다.",
                            "대공자|축하 선물로 관을 보낸다.",
                        } },
                },
                Intro = new[] {
                    "환마동 안. 사람마다 다른 환상이 펼쳐진다. 윤준호는 사형제들의 조롱을, 효룡은 형 갈효봉의 환영을, 나예린은 열 살 때의 악몽을 본다.",
                    "비류연의 앞에는 사부가 나타난다.",
                    "사부|뇌신의 힘은 손에 넣었느냐? 풍신의 힘은?",
                },
                Outro = new[] {
                    "비류연|직접 시험해 보시죠.",
                    "비류연이 날린 일격에 사부의 환영이 흩어진다.",
                    "나예린|도와줘요, 비류연!",
                    "나예린이 외치자 비류연이 나타난다. 옷자락에 미리 매달아 둔 뇌령사 덕분이었다.",
                    "그러나 그때 위지천이 던진 염마뢰가 터진다. 나예린은 비류연을 구하려 뛰어들고, 환마동이 연쇄 붕괴한다. 비류연과 나예린은 흙더미 아래로 사라진다. 범인이 위지천임을 아는 사람은 아직 없다.",
                },
                Encounters = new[] { new[] { "hwan_ying", "hwan_ying", "hwan_ying" }, new[] { "hwan_sukbu", "hwan_ying" }, new[] { "hwan_sabu" } } },
            new StageData { Number = 20, Name = "매몰 44일", RecommendedLevel = 45, DooTotal = 9500,
                Prologue = new[]
                {
                    new StoryScene { Title = "흙더미 속의 두 사람", Lines = new[] {
                            "매몰 직후 나예린이 비류연의 팔꿈치 부상을 치료한다. 한상옥령이 어둠 속에서 빛나 등불이 되었다.",
                            "비류연|장우양 국주님이 준 침낭포랑 벽곡단 사십 알이 있어요. 아껴 먹으면 돼요.",
                            "나예린|…한 알 먹어 봤는데 모래를 씹는 맛이에요. 이걸 사십 알이나요?",
                            "비류연|비싼 모래니까 감사히 드세요. 국주님은 아미산 밑 중양표국의 주인이고, 날 보면 벌벌 떨죠. 참, 이 침낭포는 2인용이에요.",
                            "나예린|…저는 바닥에서 자겠어요. 검을 베고.",
                            "칠 일째 나예린이 절망하자 비류연이 몰래 가져온 천일취를 건넨다.",
                            "나예린|… 류연.",
                            "그날부터 두 사람은 서로 이름을 부르게 된다. 열흘째, 비류연이 뇌령신공 극성으로 벽을 뚫어 자연동굴로 길을 낸다. 구조대가 찾지 못한 이유다.",
                        } },
                    new StoryScene { Title = "별의 강 사이로 흐르는 음률", Lines = new[] {
                            "푸른 광물이 별처럼 빛나는 동굴에서 비류연이 처음으로 누군가를 위해 묵금을 연주한다.",
                            "나예린|… 이 곡, 처음 들어요.",
                            "나예린은 그의 어깨에 기대어 잠든다.",
                        } },
                },
                Intro = new[] {
                    "매몰 이십육 일째. 동굴 깊은 곳에서 팔백 년 가까이 산 영물 묵린혈망이 깨어난다. 검은 비늘과 바위를 녹이는 침.",
                    "나예린|저건… 사람이 상대할 수 있는 게 아니에요.",
                    "비류연|음식이 제 발로 걸어왔군요.",
                    "비류연이 묵룡환을 푼다.",
                },
                Outro = new[] {
                    "비뢰도 오의 검기 생사결 섬뢰창이 묵린혈망을 꿰뚫었다. 내단은 없었고 묵린혈망은 죽으며 출입구를 막는다. 두 사람은 그 피로 연명한다.",
                    "사십사 일째, 비류연은 벽 너머의 수맥을 감지한다.",
                    "비류연|제게 목숨을 다시 맡겨 주세요.",
                    "벽을 뚫자 나예린이 먼저 입을 맞춘다. 물에 휩쓸려 파양호로 빠져나간다.",
                    "학관은 빈 관 둘로 장례식을 치르고 있었다. 나백천이 통곡하며 천겁우를 몰살하겠다고 맹세한 직후, 비류연이 나예린과 함께 나타난다.",
                    "비류연|조의금은 많이 걷혔어요?",
                    "두 사람 모두 환마동 시험에 합격한다. 비류연은 죽은 묵린혈망의 독아로 은세공 부적 한 쌍을 만들었다. 하나는 나예린의 것이다.",
                },
                Encounters = new[] { new[] { "cave_viper", "cave_viper", "cave_viper" }, new[] { "cave_viper", "cave_viper" }, new[] { "mukrin_hyeolmang" } } },
            new StageData { Number = 21, Name = "흑랑채 청혼", RecommendedLevel = 47, DooTotal = 10000,
                Prologue = new[]
                {
                    new StoryScene { Title = "화산을 향해", Lines = new[] {
                            "후보는 역대 최다인 오십 명. 인솔자는 늑기한, 고약한, 염도, 빙검. 규칙은 동행과 호위 금지, 한 달 전까지 화산에 도착하는 것. 대공자는 길잡이로 십이혈마대를 흑천맹 몰래 보낸다.",
                            "정문에서는 나백천이 마지막까지 딸 곁의 비류연을 수상쩍게 노려본다.",
                            "나예린|아버님, 이제 그만 보내 주세요. 모두 기다리고 있어요.",
                            "비영각 신견대는 비류연의 출신을 조사하러 아미산으로 떠난다.",
                            "염도|출발이다! 낙오하는 자는 밥이 없다!",
                            "주작단 열여섯은 이를 악물고 행렬의 맨 뒤를 따른다.",
                        } },
                },
                Intro = new[] {
                    "구궁산. 녹림오패의 하나인 흑랑채주 흑랑부 임개가 나타나 나예린을 아내로 달라고 청혼한다. 비류연이 격노한다.",
                    "임개|내 아내가 되어 주시오, 소저!",
                    "비류연|… 당신, 오늘 죽고 싶은 거죠?",
                    "부채주 군자도 이송은 정의문 장로인 하북십검 군자소요검 이송학이었다. 몇 년 전 어떤 소년에게 손 한번 못 쓰고 패한 뒤 실종된 사람이다.",
                },
                Outro = new[] {
                    "염도와 빙검이 정체를 밝히자 산적들은 길을 연다. 이송학은 비류연을 알아보지 못한다.",
                    "그러나 학관 일행이 떠난 뒤 십이혈마대가 흑랑채를 몰살한다. 임개는 자폭했고 이송학은 도망친다.",
                    "십이혈마대는 학관 대표단을 범인으로 몰려 한다.",
                },
                Encounters = new[] { new[] { "heukrang_bandit", "heukrang_bandit", "heukrang_bandit" }, new[] { "lee_songhak", "heukrang_bandit" }, new[] { "im_gae" } } },
            new StageData { Number = 22, Name = "낙뢰곡 철쇄봉혼진", RecommendedLevel = 49, DooTotal = 10500,
                Prologue = new[]
                {
                    new StoryScene { Title = "녹림왕의 누명", Lines = new[] {
                            "은설란과 마부 장노, 중양표국 마차까지 붙은 대표단이 대홍산에 이르자 녹림왕 임덕성과 삼천 녹림도가 흑랑채 몰살의 혈채를 요구한다.",
                            "임덕성|흑랑채를 쓸어버린 게 네놈들이냐! 혈채를 갚아라!",
                            "임성진이 나서 아버지와 결투를 벌인다. 승부가 나지 않은 채 부자는 화해에 가까워진다.",
                            "은설란|흑랑채 현장에 남은 도흔은 학관의 것이 아니에요.",
                            "은설란이 증언하자 삼천 녹림도가 검례로 길을 연다. 대공자는 실패를 인정하고 \"최후의 방법\"을 쓰기로 한다. 십이혈마대와 \"옛분들\"을 낙뢰곡에 매복시킨다.",
                        } },
                    new StoryScene { Title = "윤준호의 칠매검", Lines = new[] {
                            "숲에서 십이혈마대 8대에게 쫓기던 이송학을 윤준호가 만난다. 도망치지 않고 맞서다 위기에 빠지자 비류연이 나타난다.",
                            "비류연|윤준호가 이긴다에 은 열 냥을 걸었어. 실망시키지 마.",
                            "윤준호는 칠매검으로 혈마대원 다섯을 쓰러뜨린다. 그의 첫 살인이었다.",
                        } },
                },
                Intro = new[] {
                    "호북과 섬서의 경계 낙뢰곡. 늑기한은 배탈을 핑계로 일행에서 빠졌다. 협곡 위에 십이혈마대가 매복해 있다.",
                    "혈궁|9대 조장 혈궁이 인사드리지. 화살비를 받아 보시게!",
                    "혈마전이 염도의 볼을 스치고, 화살비가 쏟아진다.",
                },
                Outro = new[] {
                    "혈궁이 구궁연환사로 나예린과 빙검을 노리자 비류연이 화살을 손으로 잡아 되던져 혈궁의 활을 부순다. 10대 조장 혈쇄가 만년한철 철쇄봉혼진으로 협곡을 거미줄처럼 막는다.",
                    "비류연이 쇠사슬을 타고 봉무등천으로 협곡 위에 올라 혈쇄조 열 명을 쓰러뜨리고 혈쇄의 쇠사슬을 끊는다. 아래에서 나예린이 얼음 가면이 부서지듯 외친다.",
                    "나예린|류여어언!",
                    "십이혈마대 총대주 적혈이 \"그분들\"에게 도움을 청한다.",
                    "임성진|아버지와는 화해했다. 이제 내 곤으로 여러분 곁에 서겠다.",
                    "아버지와 화해한 임성진이 곤을 들고 일행의 곁에 선다. 굼뜨지만 묵직한 곤법이다. (동료 합류: 임성진 — 파티는 최대 셋이라 강호 탭의 \"파티 편성\"에서 동료 셋을 고른다.)",
                },
                Encounters = new[] { new[] { "hyeolma_dae", "hyeolma_dae", "hyeolma_dae" }, new[] { "hyeol_gung", "hyeolma_dae" }, new[] { "hyeol_swae" } } },
            new StageData { Number = 23, Name = "비사진", RecommendedLevel = 51, DooTotal = 11000,
                Prologue = new[]
                {
                    new StoryScene { Title = "질투의 불꽃", Lines = new[] {
                            "낙뢰곡 아래에서 위지천은 자기 두 손이 붉게 보이는 듯 멍하니 서 있다. 독사가 덮치자 천룡삼우 청흔이 구한다.",
                            "청흔|정신 차려라, 위지천!",
                            "그러나 위지천은 \"비류연의 피로 물들였으면\" 하는 증오에 사로잡혀 정신의 끈이 끊어진다.",
                            "용천명|도와 드릴까요, 마 소저?",
                            "뱀 공포증이 있는 마하령이 체면을 접고 청하자 용천명은 달마여래십삼검 불광만조로 뱀을 쓸어낸다.",
                        } },
                },
                Intro = new[] {
                    "녹의 노인이 협곡 위에 선다. 비사신군, 백도에서는 비사마군이라 부르는 모사령. 백 년 전 선혈해 대혈전에서 정사연합군 삼백 명을 뱀 먹이로 만든 마인이다.",
                    "염도|뱀 땅꾼 아니냐, 이 영감!",
                    "모사령|… 이놈이 감히!",
                    "모사령이 격분해 예비 뱀 전부를 푼다. 나예린 곁에는 비류연이 준 독아 부적 덕에 뱀이 반 장 안에 접근하지 못한다.",
                },
                Outro = new[] {
                    "청흔의 삼정태극검혜와 모용휘의 은하류가 하늘의 뱀과 화살을 쓸어낸다. 모사령이 청홍쌍각사를 혈마전에 얹어 나예린을 겨누자 비류연이 대갈성으로 모사령을 멈추고 뇌령사로 혈마전을 던져 버린다.",
                    "비류연은 쌍각사를 직접 잡아 협박해 복종시킨다. 모사령의 지사진은 은검풍인의 장 은섬풍에 찢겼다.",
                    "비류연|내기를 하죠. 쌍각사를 땅에 놓았을 때 누구에게 가는지 보자고요.",
                    "쌍각사가 비류연에게 오자 모사령이 패배를 인정한다.",
                    "비류연|세 가지만 묻죠. 배후는? 근거지는? 목적은?",
                    "모사령|배후는 천겁령. 근거지는 당신들이 믿고 있는 거울의 그림자. 목적은 붉은 꽃이 만개한 화산이다.",
                },
                Encounters = new[] { new[] { "bisa_snake", "bisa_snake", "bisa_snake" }, new[] { "cheonghong_ssanggaksa", "bisa_snake" }, new[] { "mo_saryeong" } } },
            new StageData { Number = 24, Name = "비뢰쌍마", RecommendedLevel = 53, DooTotal = 12000,
                Prologue = new[]
                {
                    new StoryScene { Title = "운해 낚시", Lines = new[] {
                            "오 년 전 뇌운봉 은백평 운해암. 비류연과 사부가 구름바다에서 맹금을 낚는 수련을 한다.",
                            "비류연|사부님은 어디서 오셨어요? 젊을 때는 뭘 하셨는데요?",
                            "사부|나는 내가 누구인지도 잊었다.",
                            "사부|비뢰라는 이름은 비뢰문 후예 외엔 아무도 쓰지 못한다. 어중이들이 남용하면 주제를 가르쳐라. 이름에 대한 책임이 곧 명예다.",
                        } },
                },
                Intro = new[] {
                    "적혈의 퇴각 명령 직후, 남의노인과 갈의노인이 나타난다. 섬뢰마검 동방학과 잔뢰마도 공패, 백 년 전 천겁령 편에서 정사 양도를 휩쓴 비뢰쌍마다.",
                    "이진설을 구하려던 효룡이 동방학의 붉은 검에 이마를 베인다. 동방학은 효룡의 쌍검 검로에서 굉천혈영도를 느낀다.",
                    "두 노인이 염도와 빙검의 검한기와 검염기에서 태극신군의 흔적을 알아본다.",
                    "염도|돌아가신 사부님을 모욕하지 마라!",
                    "염도가 외치는 순간 혁월린의 죽음이 쌍마에게 누설된다.",
                },
                Outro = new[] {
                    "비류연이 무형지기 한가운데로 걸어 들어간다.",
                    "비류연|비뢰라는 이름을 쓸 자격이 있는지, 사문의 계율에 따라 심사하겠습니다.",
                    "묵룡환을 풀고 동방학과 쾌를 겨뤄 섬뢰로 미간을 꿰뚫는다.",
                    "비류연|뇌의 이름은 인정하죠.",
                    "공패가 잔뢰번천지로 덤비지만 비뢰도 오의 굉천광뢰의 장 굉뢰가 도를 박살내고, 도편이 두 눈을 찌른다. 가짜 연막 뇌탄이 터지고 적혈이 공패를 업고 사라진다.",
                    "적혈|환마동 폭발도 우리가 터뜨렸다. 우리를 건드리면 진짜 뇌탄을 던지겠다.",
                    "모사령은 풀려났다. 비류연은 압수한 사충적을 당삼에게, 사충음보를 당문혜에게 판다. 효룡은 말수가 줄고 이마에 뇌인 모양의 상처가 남았다.",
                    "화산에 도착한 대표단을 화산파 장문인 풍매검 양유중과 12장로가 맞이한다. 화음현 매화객잔에서 다음 날, 복면을 쓴 매화가면이 윤준호를 지명해 겨루고, 윤준호는 검향지경으로 붉은 매화를 피워 올린 뒤 이십사수 매화검법 매염토망을 알아보고 절한다.",
                    "윤준호|… 태사부님!",
                    "다음 날 아침, 길에서 비류연과 말이 통하던 회색 장삼의 정체불명 노인이 일행에 끼어 천무봉에 오른다. 샛길의 첫 관문에서 두 다리가 없는 노인 비공답운 종쾌가 천겁혈신과 천무봉의 비밀을 들려주고, 첫 시험 장소 \"천겁간\"을 가리킨다. (13권에서 계속)",
                },
                Encounters = new[] { new[] { "hyeolma_dae_b", "hyeolma_dae_b", "hyeolma_dae_b" }, new[] { "dongbang_hak", "hyeolma_dae_b" }, new[] { "gong_pae" } } },
            new StageData { Number = 25, Name = "화평장", RecommendedLevel = 55, DooTotal = 13000,
                Prologue = new[]
                {
                    new StoryScene { Title = "멸겁삼관", Lines = new[] {
                            "종쾌가 백 년 전 천겁혈신 위천무에게 두 다리를 잃은 이야기를 마친다. 첫 관문 혈신일보는 20장 폭의 협곡이었다.",
                            "비류연|남궁상, 네가 가라. 묵룡환을 풀고 새 머리를 밟고 건너.",
                            "남궁상이 협곡의 새 머리 위를 밟아 건너고, 염도는 은 열 냥을 걸어 177전 177패를 기록한다.",
                            "두 번째 관문 앞에는 거울 같은 검은 벽 겁흔벽이 서 있다. 비류연은 벽을 만지며 처음으로 동요하고, 나예린이 용안으로 그 흔들림을 읽는다.",
                            "세 번째 관문은 부러진 검 108자루가 꽂힌 땅이다. 묘지기 검치 섭운명 앞에서 비류연은 간합 세 치를 증명해 통과한다.",
                        } },
                    new StoryScene { Title = "홍매곡과 은설란의 납치", Lines = new[] {
                            "계절을 잊고 붉은 매화가 만개한 홍매곡에 도착한다. 마천각 대표단이 기세 싸움을 걸어 오고 검마 초월과 삿갓 쓴 사내가 모습을 드러낸다.",
                            "은설란은 중원표국이 전세 낸 풍매객잔에 잠입해 \"작전 2단계\", \"간세\", \"붉은 꽃이 만개한 화산\"이라는 대화를 엿듣다 납치된다.",
                            "비류연|규칙은 깨라고 있는 겁니다. 구출대를 꾸리죠.",
                            "비류연은 외부 접촉 금지 규칙을 깨고 구출대를 꾸린다. 나예린, 독고령, 이진설, 효룡, 모용휘, 남궁상, 진령, 장홍, 윤준호, 노학, 염도, 석류화, 개방의 오개. 개방의 마차 이동 기록이 화평장을 가리킨다.",
                        } },
                },
                Intro = new[] {
                    "깊은 밤, 마을 외곽의 오래된 장원 화평장. 구출대는 훈련된 병사들에게 포위된다. 귀빈실에는 눈이 차가운 젊은 사내가 있었지만 은설란에게서 얻을 정보가 없다는 것을 확인하고 자리를 비웠다.",
                    "천지쌍살은 매일 악몽에 시달린다. 효룡이 형 갈효봉의 죽음을 떠올리며 쌍검을 들고 천살에게 돌진한다.",
                    "염도가 효룡의 뺨을 때려 꾸짖는다.",
                    "염도|오랜 잠에서 깨어난 걸 축하한다. 이제 정신 차려라, 아룡!",
                },
                Outro = new[] {
                    "염도가 불꽃 통로를 만들어 모용휘가 은설란을 업고 먼저 탈출한다. 비류연은 천살의 요검 귀곡을 묵룡환으로 돌바닥에 박아 굴복시키지만 직접 싸우지는 않는다.",
                    "비류연|그 생명 며칠 더 맡겨두죠. 빚을 받을 당사자가 처리할 때까지.",
                    "한 팔씩 남은 쌍살이 수천 번 연습한 합격술로 두 사람을 몰아붙이는 사이, 모용휘 쪽에서 쌍살보다 위험한 존재가 느껴진다.",
                    "하얀 손|너의 목숨을 가져갈 자다.",
                    "단월인이 날았다. 모용휘는 검성의 방어 자세 극안으로 버티지만 검막이 찢어진다.",
                    "비류연|선수교체를 해도 될까요?",
                    "비류연이 모용휘 앞에 나타난다. (14권에서 계속)",
                },
                Encounters = new[] { new[] { "hwapyeong_soldier", "hwapyeong_soldier", "hwapyeong_soldier" }, new[] { "hwapyeong_soldier", "ssangsal_hapgyeok" }, new[] { "baek_son_gosu" } } },
            new StageData { Number = 26, Name = "살귀대", RecommendedLevel = 57, DooTotal = 13500,
                Prologue = new[]
                {
                    new StoryScene { Title = "시야와 심안", Lines = new[] {
                            "비류연은 모용휘를 보내고 어둠 속 괴인과 맞선다.",
                            "비류연|상대를 인지 속에 넣고 자신은 상대의 인지에서 벗어나는 것이 승리의 1단계죠.",
                            "괴인은 마천각 대공자 비였다.",
                            "대공자|나의 일초를 피하면 살려주겠다.",
                            "오의 영뢰 천잔에 비류연은 심안을 열고도 가슴을 베이고, 공간에서 사라져 피한다.",
                            "대공자|류연이라 했던가. 다시 만나면 반드시.",
                            "비류연|성혈을 외출시킨 대가는 비싸다.",
                        } },
                    new StoryScene { Title = "두 노인의 술판", Lines = new[] {
                            "화음현 매령주루에서 윤준호의 태사부 매화검선 유환권(매화가면)과 청흔의 사부인 무당 현검자가 며칠째 술을 마시고 있다.",
                            "매화검선|이번 대회의 실상을 알면 아이들이 낙담할 것이다.",
                            "현검자|그분께서 아우까지 부르셨다.",
                        } },
                },
                Intro = new[] {
                    "화평장에서 놓친 은설란을 되찾으라는 대공자의 명령으로 살귀대가 풀린다. 화평장주 호유광은 \"누구에게나 두 번의 기회\"를 받았다.",
                    "염도의 구출대가 매령주루 앞에서 살귀대에게 포위된다.",
                    "호유광|은설란을 내놓으시오. 아니면 모두 여기서 끝이다.",
                    "염도|이 정도 머릿수로 우리를 막겠다고?",
                },
                Outro = new[] {
                    "매화검선과 현검자가 살귀대를 제압하고 염도 일행을 보낸다.",
                    "매화검선|우리는 만난 적이 없는 거네?",
                    "홍매곡 입구에서 율령자 세 사람과 위지천이 구출대를 막는다. 비류연은 은설란을 \"호환을 당한 조난자\"라 우기고, 빙검이 밤새 잡아 온 대호가 증거가 된다. 위지천의 고자질은 용서하지 않기로 한다.",
                    "사흘 뒤 불탄 화평장에서 정천맹 감찰조사관 안명후와 개코 왕견이 고순도의 위험한 물품 냄새를 맡는다.",
                },
                Encounters = new[] { new[] { "salgwi_daewon", "salgwi_daewon", "salgwi_daewon" }, new[] { "salgwi_daewon", "salgwi_daewon" }, new[] { "ho_yugwang" } } },
            new StageData { Number = 27, Name = "마천칠걸", RecommendedLevel = 59, DooTotal = 14000,
                Prologue = new[]
                {
                    new StoryScene { Title = "대공자의 멸겁삼관", Lines = new[] {
                            "대공자 비가 입산기한 내에 합법적으로 시험만 치르겠다며 홀로 천무봉에 오른다.",
                            "새를 밟지 않고 혈신일보를 뛰어넘고, 겁흔벽에는 새 도강의 흔적을, 세 번째 관문에는 일검에 말라 죽은 야생화를 남긴다.",
                            "대공자|길을 안내해 주신 분께 감사드립니다.",
                            "종쾌는 그의 정중한 말에서 \"마인을 부려본 자의 위엄\"을 느낀다.",
                        } },
                    new StoryScene { Title = "깨어난 은설란", Lines = new[] {
                            "홍매곡 의무실에서 깨어난 은설란은 풍매객잔에 간 것은 기억하지만 누군가를 따라간 이유가 심령금제로 막혀 있다.",
                            "은설란|이상해요. 왜 그곳에 갔는지, 머릿속에서 지워진 것 같아요.",
                            "비류연|죽이기가 쉬운데 왜 심령금제를 걸었을까요? 은 소저가 아는 고위 인물이었을 거예요.",
                        } },
                },
                Intro = new[] {
                    "홍매곡 입구에 대공자와 비류연이 10장 거리에서 대치한다. 나예린은 그의 기운에 오한을 느끼고 비류연의 눈은 황금빛이 된다.",
                    "비류연|그놈의 기.",
                    "마천칠걸이 주군의 허락 없이 비류연을 처단하려 연환 공격을 펼친다.",
                },
                Outro = new[] {
                    "칠걸의 공격은 모두 빗나간다.",
                    "추명|봉인을 푸는 것이 허가되지 않았습니다.",
                    "은설란이 \"대공자!\"라고 부르자 대공자가 처음으로 놀라며 대답한다.",
                    "대공자|아란 소저.",
                    "그는 물러나며 비류연의 이름을 되묻는다.",
                    "비류연|댁 이름은? 철수나 영희?",
                    "대공자는 교옥에게 \"비류연의 영혼을 사로잡아 그 안의 모든 것을 알아내라\"고 명한다.",
                    "천무봉을 함께 오른 회색 옷의 노인이 단상에 올라 스스로를 운영 총책임자 혁중, 혁 노야라 소개한다. 그는 화산규약지회의 진짜 목적이 우정, 화합, 평화이며 천겁령 부활에 대비하는 것이라고 선언한다.",
                    "혁중|일주일간 흑백 혼합 공동 숙소 생활을 하고, 흑백 5:5의 조로 오행관에 도전한다.",
                    "태양관과 월음관의 소동 끝에 조 추첨이 시작된다.",
                    "비류연|나는 반드시 나예린과 한 조가 된다.",
                },
                Encounters = new[] { new[] { "jongri_chu", "do_chuun" }, new[] { "oh_munchu", "baek_sayeong" }, new[] { "chu_myeong" } } },
            new StageData { Number = 28, Name = "멸성대", RecommendedLevel = 61, DooTotal = 14500,
                Prologue = new[]
                {
                    new StoryScene { Title = "오행 제1관 목요관", Lines = new[] {
                            "화산오봉 전체를 아홉 구간으로 나눠 달리는 경공계주. 교옥은 같은 7조가 되었다고 선언하고 나예린과 마주친다. 독고령은 대공자 비와 같은 1조가 되어 당황한다.",
                            "교옥|저도 같은 7조가 되었어요. 잘 부탁드려요, 나 소저.",
                            "비류연은 윤준호에게 \"화산과 하나가 된다\"는 경공의 이치를 가르친다. 윤준호는 껍질을 깨고 마하령을 추월한다.",
                            "그러나 마천칠걸의 암수로 옥녀봉을 굴러 떨어지고, 절뚝이며 도착해 운다.",
                            "윤준호|분해요…",
                            "비류연|넌 늦지 않았어. 묵룡환은 맡아 둬. 맡겨두라고, 친구!",
                            "비류연이 달린다. 밤의 낙안봉에서 마천칠걸 종리추와 도추운이 길을 막는다. (첫 전투: 목요관)",
                        } },
                    new StoryScene { Title = "천무삼성의 여로", Lines = new[] {
                            "백의 학창의 노인이 검 대신 나뭇가지에 \"은하\"라는 이름을 붙이고 석등을 벤다. 아들에게 비싼 석등이라고 혼나고 도망치듯 길을 떠난다. 오랜 친구들과의 \"동창회\"다.",
                            "임덕성|성진아! 아비가 보러 왔다!",
                            "임성진|아, 아버지… 여기까지 어떻게.",
                            "화음객잔에는 녹림왕 임덕성과 모경이 아들 임성진을 보러 왔고, 수다스러운 중년 여인과 두 노인이 들어와 엄청난 양의 요리를 주문한다.",
                        } },
                },
                Intro = new[] {
                    "화음객잔에 거지 차림의 안명후가 도착한다. 그를 쫓던 마천각 군사 치사한의 멸성대가 객잔을 포위한다.",
                    "치사한|개코 왕견은 이미 끝났다. 이제 네 차례다, 감찰조사관.",
                    "개코 왕견의 수급이 던져진다.",
                    "멸성대원|천무삼성이라 할지라도 우리 손에 걸리면 죽음을 피하지 못한다.",
                },
                Outro = new[] {
                    "목요관의 결말. 비류연은 암습을 격파하고 대공자 비와 거의 동시에 결승선을 넘는다. 공동 1위.",
                    "비류연|그것이 자기 자신을 속이는 일일지라도?",
                    "대공자|판정에 승복한다.",
                    "청의 노인이 검지로 기류를 휘저어 멸성대원을 뒤집어 박고, 오의 나선용권풍으로 만근검익진을 박살낸다. 여인은 젓가락으로 검을 분지르고, 백의 노인은 나뭇가지로 이마를 쳐 쓰러뜨린다. 도성 하후식, 검후 이옥상, 검성 모용정천이다.",
                    "사마흔|천겁혈세 혈신재림!",
                    "사마흔은 자결하고, 멸성대 전원이 따라 죽는다. 도성은 목숨을 가볍게 버린 것에 분노한다.",
                    "독에 중독된 안명후는 삼성의 합력 내공으로 살아난다.",
                    "안명후|음모… 화산… 위험… 용…",
                    "안명후는 그 말을 남기고 쓰러진다. 임덕성과 모경이 짐꾼을 자원한다.",
                },
                Encounters = new[] { new[] { "jongri_chu_night", "do_chuun_night" }, new[] { "mulsung_daewon", "mulsung_daewon", "mulsung_daewon" }, new[] { "mulsung_daewon", "i_gae_bujo" }, new[] { "sama_heun" } },
                Trials = new[]
                {
                    TrialRule.Deadline("오행 제1관 목요관", "밤의 낙안봉. 종리추와 도추운이 길을 막는다. 대공자를 쫓으려면 30행동 안에 돌파하라!", 30)
                } },
            new StageData { Number = 29, Name = "중토관", RecommendedLevel = 63, DooTotal = 15000,
                Prologue = new[]
                {
                    new StoryScene { Title = "오행 제2관 금요관", Lines = new[] {
                            "금요관은 아홉 명이 생명줄로 몸을 묶고 절벽을 오르는 시험이다. 줄이 끊어지는 즉시 그 조는 실격한다.",
                            "마천칠걸 오문추가 노학의 줄을 자른다.",
                            "비류연|내 줄도 끊겠어. 직지질주로 달려가서 노학을 구하죠.",
                            "비류연이 자기 줄을 끊고 직지질주로 절벽을 달려 노학을 구하러 간다.",
                            "나예린|무모하게… 이제 당신이 죽든 말든 상관 안 해요!",
                            "나예린은 화를 내며 가 버린다.",
                        } },
                    new StoryScene { Title = "검후의 시험", Lines = new[] {
                            "검후 이옥상이 나예린과의 관계를 걸고 비류연에게 십 초 내기를 청한다.",
                            "이옥상|네가 십 초를 받아내면 내가 패배한 것이다. 그러면 두 사람의 관계를 천무삼성이 공인하마.",
                            "비류연|제가 당신의 제자를 사랑하게 된 것을 사과할 이유가 없습니다.",
                        } },
                    new StoryScene { Title = "수화관", Lines = new[] {
                            "위지천이 정체불명의 복면인에게서 혈폭환을 받는다.",
                            "복면인|이걸 쓰면 네가 원하는 대로 될 것이다.",
                            "수화관은 아홉 명이 내공을 합쳐 검은 암벽을 부수는 시험이다. 위지천이 자청해 비류연의 바로 뒤에 선다.",
                        } },
                },
                Intro = new[] {
                    "오행관 최종 관문 중토관. 14일 동안 자연에서 생존하고 천율패를 지켜야 한다. 7조는 동굴을 본진으로 삼는다.",
                    "혁중|천율패를 지켜 내라. 잃으면 그 조는 실격이다.",
                    "5일째 안개 낀 날, 반면 은가면을 쓴 셋(공포·절망·비탄)이 각 조를 습격해 천율패를 빼앗는다. 마천삼흉은 7조에게 거꾸로 매달린 채 일찌감치 제압되었다.",
                },
                Outro = new[] {
                    "7조는 은가면 셋의 습격을 끝까지 버텨 냈다.",
                    "은가면|오늘은 이만.",
                    "은가면들은 천율패를 빼앗지 못하고 물러난다. (원작에서는 대부분의 조가 이때 천율패를 잃고 기절한다.)",
                    "삼성각의 안명후를 노린 암살자 무정이 인형을 찌르고 모경을 해친다. 임덕성이 안명후를 업고 도망치고, 임성진이 일점필중으로 복면인을 물리친다.",
                    "7조 동굴에서 깨어난 안명후를 교옥이 \"자장가\"라며 베어 죽인다. 안명후의 손에서 비류연의 피 묻은 천율패가 나온다.",
                    "대공자|7조 안에 범인이 있습니다.",
                    "사람들은 비류연을 의심한다.",
                    "나예린, 장홍, 효룡, 윤준호, 이진설, 모용휘가 비류연을 따라 나선다.",
                    "나예린|우리는 당신을 믿어요.",
                    "비류연은 은가면에게 뇌령사 실을 달아 두었다.",
                },
                Encounters = new[] { new[] { "oh_munchu_b", "baek_sayeong_b" }, new[] { "geomhu_ioksang" }, new[] { "heuksaek_amhyeok", "wiji_hyeolpok" }, new[] { "mask_gongpo", "mask_jeolmang", "mask_bitan" }, new[] { "suda_bokmyeon", "mu_jeong" } },
                Trials = new[]
                {
                    TrialRule.Lifeline("금요관", "절벽의 마천칠걸이 줄을 노린다. 힘을 모으는 공격을 끊지 못해 세 번 맞으면 줄이 끊어진다!", 3),
                    TrialRule.Survive("검후의 십초지적", "검후의 공격을 12번 버텨라. 이길 수는 없다. 피하고 막아 살아남아라!", 12),
                    TrialRule.Harmony("수화관 내공합격", "쉬지 않고 모두가 공격을 이으면 다음 일격이 크게 강해진다. 32행동 안에 암벽과 위지천을 쓰러뜨려라!", 32),
                    TrialRule.Survive("은가면 삼성의 습격", "공포·절망·비탄 셋을 이길 수는 없다. 열두 번의 공격을 버텨 천율패를 지켜라!", 12),
                    null
                } },
new StageData { Number = 30, Name = "화룡멸겁대진", RecommendedLevel = 65, DooTotal = 16000,
                Prologue = new[]
                {
                    new StoryScene { Title = "은가면의 정체", Lines = new[] {
                            "비류연이 실을 따라가자 은가면 셋이 멧돼지를 굽고 있다. 정체는 천무삼성이었고, 그 곁에서 혁 노야가 머쓱한 얼굴로 서 있다.",
                            "혁중|저 아이들에게 압도적인 공포를 가르쳐 달라고 부탁했던 것이다.",
                            "삼성은 안명후가 살해되었다는 말에 경악한다.",
                            "비류연은 안명후의 잠꼬대 \"용린… 염우\"와 노학의 \"맛있어 보이는 곰 같은 복면인\"을 이어 주방을 추리한다.",
                            "비류연|염우는 고순도의 휘발성 기름이고, 용린은 벽력궁의 미세 화약이에요. 범인은 주방에 있어요.",
                            "홍매곡 요리사 서른여섯의 정점, 우 대숙수가 잡힌다.",
                        } },
                },
                Intro = new[] {
                    "중토관 종료 사흘 뒤 우승 발표.",
                    "대공자|오늘 용이 날아오른다. 재생을 위한 파괴, 오늘을 기해 신무림기가 시작된다.",
                    "천무전 지붕에서 비류연이 외친다.",
                    "비류연|이의 있음!",
                    "우 대숙수를 단상 앞에 던진다. 대공자가 순순히 인정한다.",
                    "대공자|안명후는 내 도구가 죽였다.",
                    "비류연|옳은 말도 옳은 행동이 따라야 가치가 있죠. 파괴를 위한 파괴는 재생이 아니에요.",
                    "대공자|고인 물은 썩고 쓰지 않는 검은 녹슨다! 화룡이 날아오르고 불의 홍수가 산을 뒤덮으리라! 재 속에서 불사조는 날아오른다! 천겁혈세 혈신재림!",
                    "화룡멸겁대진이 발동되고 단상이 폭발한다. 마천칠걸과 불의 성벽이 비류연의 길을 막는다.",
                },
                Outro = new[] {
                    "비류연의 유성호접검이 대공자를 몰아붙이지만 대공자는 삼첩영을 닮은 분신 영뢰 잔흔으로 맞서다 물러선다.",
                    "대공자|미안하지만 더 이상 놀아줄 수 없겠군. 용이 날아오를 시간이다!",
                    "이 대진은 아무도 멈출 수 없다. 대공자는 쓰러지지 않았다. 불의 성벽이 닫히는 찰나 생로가 열린다. 대공자와 수하들은 독고령을 들쳐메고 빠져나가고, 홍매곡 입구가 무너져 고립된다.",
                    "비류연|방법이 있어요. 풍신.",
                    "비류연이 삼성과 혁중의 내력을 구한다.",
                    "나예린|… 우승자가 되는 거죠?",
                    "나예린의 입맞춤을 부적으로 받은 비류연이 묵룡환을 모두 풀고 열 자루 비뢰도와 뇌령사를 나선으로 돌린다.",
                    "비류연|비뢰도 최종비전오의, 풍뢰의 장. 나선의 인, 용권승룡!",
                    "용권풍이 불꽃을 하늘로 밀어 올려 모두 꺼뜨린다. 비류연은 쓰러져 나예린의 무릎에서 쉰다. 사망 3, 중상 13, 경상 25.",
                    "원문은 대공자 비가 \"한때 은명이라 불렸던 남자\"임을 밝히고, 비류연의 사부로 보이는 백염백발의 노인이 불타는 청룡은장에서 제자를 찾아 길을 나서며 1부가 끝난다.",
                },
                Encounters = new[] { new[] { "ma_chilgeol_jeoji", "ma_chilgeol_jeoji", "ma_chilgeol_jeoji" }, new[] { "hwaryong_hwayeom", "ma_chilgeol_jeoji" }, new[] { "bi_daegongja" } } },
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

        /// <summary>Levels a first-clear-only run stays above the recommended level from stage 2 on (a small cushion).</summary>
        public const int LevelHeadroom = 1;

        /// <summary>Share of exp (not 두) paid when replaying an already cleared stage.</summary>
        public const double ReplayExpFactor = 0.5;
        public const double ReplayDooFactor = 0.5;

        /// <summary>Exp for one enemy: clearing stages in order keeps the player <see cref="LevelHeadroom"/> above each stage's recommended level (stage 1 pays the cushion up front).</summary>
        public static long ExpReward(StageData st, string enemyId)
        {
            var next = FindStage(st.Number + 1);
            int to = (next != null ? next.RecommendedLevel : st.RecommendedLevel + 3) + LevelHeadroom;
            int from = st.RecommendedLevel + (st.Number > 1 ? LevelHeadroom : 0);
            long total = CumulativeExp(to) - CumulativeExp(from);
            return (long)Math.Ceiling((double)total * RewardWeight(enemyId) / TotalWeight(st));
        }

        /// <summary>Stage 두 payouts are raised by this factor on top of <see cref="StageData.DooTotal"/> (clears, replays and quests alike).</summary>
        public const double StageDooFactor = 1.2;

        public static long DooReward(StageData st, string enemyId)
        {
            return (long)Math.Round((double)st.DooTotal * StageDooFactor * RewardWeight(enemyId) / TotalWeight(st));
        }

        private static readonly Dictionary<string, EnemyData> DynamicEnemies = new Dictionary<string, EnemyData>();

        /// <summary>Makes a generated enemy (tower floors) findable by id.</summary>
        public static void RegisterDynamicEnemy(EnemyData enemy) { DynamicEnemies[enemy.Id] = enemy; }

        public static EnemyData FindEnemy(string id)
        {
            foreach (var e in Enemies) if (e.Id == id) return e;
            EnemyData dynamicEnemy;
            return DynamicEnemies.TryGetValue(id, out dynamicEnemy) ? dynamicEnemy : null;
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

        /// <summary>Most companions that can fight at once (the protagonist plus these fills the party of four).</summary>
        public const int MaxCompanions = 3;

        /// <summary>The companions who actually fight in a stage: the player's picks that have joined, then the rest in join order, up to <see cref="MaxCompanions"/>. Benched ids are never auto-filled.</summary>
        public static List<CompanionData> SelectCompanions(int stageNumber, IList<string> preferred, IList<string> benched = null)
        {
            var available = CompanionsFor(stageNumber);
            var result = new List<CompanionData>();
            if (preferred != null)
                foreach (var id in preferred)
                {
                    var c = available.Find(x => x.Id == id);
                    if (c != null && !result.Contains(c) && result.Count < MaxCompanions) result.Add(c);
                }
            foreach (var c in available)
                if (!result.Contains(c) && result.Count < MaxCompanions && (benched == null || !benched.Contains(c.Id))) result.Add(c);
            return result;
        }

        /// <summary>Companions for a stage using the player's saved picks and benched list.</summary>
        public static List<CompanionData> SelectCompanionsFor(int stageNumber, SaveData save)
        {
            return SelectCompanions(stageNumber, save.ActiveCompanions, save.BenchedCompanions);
        }

        public static StageData FindStage(int number)
        {
            foreach (var s in Stages) if (s.Number == number) return s;
            return null;
        }
    }
}
