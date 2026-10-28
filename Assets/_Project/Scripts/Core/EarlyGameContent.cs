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
        public bool Ranged;       // presentation only: projectile attacks stay at their starting position
        public bool AllEnemies;
        public int Range;       // enemy lines reached (1 = the front line only, 2+ = any line); 0 = by kind (see EffectiveRange)
        public SkillEffect Effect;
        public float HealPct;
        public StatusKind Inflict;      // 상태 이상 the art may leave (see Statuses)
        public float InflictChance;
        public long LearnCost;
        public string OwnerId;   // null = a martial art the protagonist can learn; otherwise a companion's fixed art
        public int OwnerLevel;   // companion arts: the companion's own level at which the art is learned (0 = known from joining)
        public string Replaces;  // companion arts: the older art of the same line this one takes over once learned (its 수련 각성 carries over)

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
                SpiritCost = SpiritCost, Cooldown = Cooldown, Ranged = Ranged, AllEnemies = AllEnemies, Range = EffectiveRange, Effect = Effect, HealPct = HealPct,
                Inflict = Inflict, InflictChance = InflictChance
            };
        }
    }

    /// <summary>일반 enemies only swing; 정예 and 보스 also carry 내공 and 기세 (see <see cref="EnemyData.QiPool"/> and InteractiveBattle.EnemyAct).</summary>
    public enum EnemyRank { Normal, Elite, Boss }

    /// <summary>Plain-data enemy. Stats derive from the player curve at <see cref="Level"/> so difficulty tracks Progression.</summary>
    public sealed class EnemyData
    {
        public string Id, Name;
        public string ArtId;         // optional: share another enemy's artwork (balance clones)
        public string ArtKey { get { return string.IsNullOrEmpty(ArtId) ? Id : ArtId; } }
        /// <summary>The level to show the player: the enemy's curve <see cref="Level"/> on the player level scale (curve 65 = level 25), so it reads next to the protagonist's own level.</summary>
        public int DisplayLevel { get { return Progression.PlayerLevelAt(Level); } }
        public bool IsBoss;
        public float RetreatHpPct;   // >0: story boss that escapes at this HP fraction instead of dying
        public int Level;
        public float HpMul = 1f, AtkMul = 1f, DefMul = 1f;
        public int SpeedAdd;
        public int Range = 1;       // how many tiles away its attacks reach
        public EnemyMove[] Moves;
        /// <summary>Gear the enemy wears (story stages, <see cref="GearLoadout.ForEnemies"/>), applied like a player's; null = none.</summary>
        public List<GearLevel> Gear;

        /// <summary>A copy wearing <paramref name="gear"/>.</summary>
        public EnemyData WithGear(List<GearLevel> gear)
        {
            var c = (EnemyData)MemberwiseClone();
            c.Gear = gear;
            return c;
        }

        /// <summary>
        /// 보스 = <see cref="IsBoss"/>. 정예 = a fighter with a real script (3+ moves) who is not a trap, beast-less object or the like (<see cref="Realm.HasRealm"/>).
        /// Everything else is 일반 (henchmen with a two-move script, traps).
        /// </summary>
        public EnemyRank Rank
        {
            get
            {
                if (IsBoss) return EnemyRank.Boss;
                if (Moves != null && Moves.Length >= 3 && Realm.HasRealm(this)) return EnemyRank.Elite;
                return EnemyRank.Normal;
            }
        }

        public static string RankName(EnemyRank rank) { return rank == EnemyRank.Boss ? "보스" : rank == EnemyRank.Elite ? "정예" : "일반"; }

        /// <summary>내공 an enemy of this rank starts with (0 for 일반).</summary>
        public const int EliteQi = 30, BossQi = 70;
        public int QiPool { get { var r = Rank; return r == EnemyRank.Boss ? BossQi : r == EnemyRank.Elite ? EliteQi : 0; } }

        /// <summary>A copy with HP and attack multiplied (harder stage difficulties).</summary>
        public EnemyData Scaled(float hp, float atk, float def = 1f, int levelAdd = 0)
        {
            var c = (EnemyData)MemberwiseClone();
            c.Level = Level + levelAdd;
            c.HpMul = HpMul * hp;
            c.AtkMul = AtkMul * atk;
            c.DefMul = DefMul * def;
            return c;
        }

        public CombatStats BuildStats()
        {
            var before = BuildStatsBeforeRealm();
            return Realm.HasRealm(this) ? Realm.ApplyBonus(before, Realm.StepOf(Realm.Power(before))) : before;
        }

        /// <summary>Difficulty and enemy modifiers applied, before the realm multiplier.</summary>
        public CombatStats BuildStatsBeforeRealm()
        {
            var b = Progression.StatsFor(Level);
            int attack = Math.Max(1, (int)Math.Round(b.Attack * AtkMul)), defense = Math.Max(1, (int)Math.Round(b.Defense * DefMul));
            // Shown attack and defense are re-balanced against each other (their product stays the same) so that an enemy reads like a party member:
            // the fight values are attack x AtkUnit and defense x DefUnit, exactly what they were before.
            double s = Math.Sqrt((double)attack / (defense * DisplayRatio));
            int shownAttack = Math.Max(1, (int)Math.Round(attack / s)), shownDefense = Math.Max(1, (int)Math.Round(defense * s));
            var stats = new CombatStats
            {
                MaxHp = (int)Math.Round(b.MaxHp * HpMul),
                MaxQi = QiPool,
                Attack = shownAttack,
                Defense = shownDefense,
                AtkUnit = (float)attack / shownAttack,
                DefUnit = (float)defense / shownDefense,
                Speed = b.Speed + SpeedAdd,
                CritRate = IsBoss ? 0.06f : 0.02f,
                CritDamage = 1.5f
            };
            if (Gear != null && Gear.Count > 0) stats = Inventory.ApplyGear(stats, GearLoadout.Ids(Gear), Gear);
            return stats;
        }

        /// <summary>Shown attack / defense of every enemy (about what the protagonist's own curve gives: 1.4).</summary>
        public const double DisplayRatio = 1.4;

        public Combatant CreateCombatant(string uniqueId)
        {
            var c = new Combatant(uniqueId, Name, Team.Enemy, BuildStatsBeforeRealm(), null, Realm.HasRealm(this));
            c.IsBoss = IsBoss;
            c.Rank = Rank;
            c.RetreatHpPct = RetreatHpPct;
            c.Range = Range;
            c.Moves = Moves != null && Moves.Length > 0 ? new List<EnemyMove>(Moves) : new List<EnemyMove> { EnemyMove.Plain };
            c.Phases = BossPhases.For(this);
            c.Gear = Gear;
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

        /// <summary>Unboosted companion stats, shared by combat and loadout previews.</summary>
        public CombatStats StatsBeforeGear(int level, int bondLevel = 0, int trainLevel = 0)
        {
            var b = Progression.AllyStatsFor(level);
            float bond = Bonds.StatMultiplier(bondLevel) * CompanionTraining.StatMultiplier(trainLevel);
            return new CombatStats
            {
                MaxHp = (int)Math.Round(b.MaxHp * HpMul * bond),
                MaxQi = b.MaxQi,
                Attack = (int)Math.Round(b.Attack * AtkMul * bond),
                Defense = (int)Math.Round(b.Defense * DefMul * bond),
                Speed = b.Speed + SpeedAdd,
                CritRate = 0.02f + Bonds.CritBonus(bondLevel),
                CritDamage = 1.5f
            };
        }

        public Combatant CreateCombatant(int level, int bondLevel = 0, int trainLevel = 0, IEnumerable<string> gear = null, IList<GearLevel> gearLevels = null)
        {
            var stats = StatsBeforeGear(level, bondLevel, trainLevel);
            if (gear != null) stats = Inventory.ApplyGear(stats, gear, gearLevels);
            return CreateCombatant(level, trainLevel, stats);
        }

        /// <summary>The companion with the given stats before the 경지 bonus (gear already applied) and its arts at this level and training.</summary>
        public Combatant CreateCombatant(int level, int trainLevel, CombatStats stats)
        {
            var skills = new List<SkillSpec>();
            var replaced = new HashSet<string>();
            foreach (var a in EarlyGameContent.Arts)
                if (a.OwnerId == Id && a.OwnerLevel <= level && !string.IsNullOrEmpty(a.Replaces)) replaced.Add(a.Replaces);
            foreach (var a in EarlyGameContent.Arts)
            {
                if (a.OwnerId != Id || a.OwnerLevel > level) continue;   // some arts are only learned at a level
                if (replaced.Contains(a.Id)) continue;   // a newer art of the same line took over
                var spec = a.ToSpec();
                spec.Power *= ArtPowerFactor;
                spec.HealPct *= ArtPowerFactor;
                skills.Add(spec);
            }
            CompanionTraining.ApplyToSkills(Id, skills, trainLevel);
            return new Combatant(Id, Name, Team.Player, stats, skills, true);
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
        public int RewardWeightBudget;   // original reward denominator; added fights pay extra instead of diluting existing rewards
        /// <summary>Optional story scenes shown before Intro, only while the stage has not been cleared yet (bridges the volumes' plot between fights).</summary>
        public StoryScene[] Prologue;
        public string[] Intro, Outro;
        /// <summary>Fights in order; each is a group of enemy ids (1-3). The last one is the boss fight.</summary>
        public string[][] Encounters;
        /// <summary>Optional special rules, parallel to <see cref="Encounters"/> (null or a null entry = a normal fight).</summary>
        public TrialRule[] Trials;
        public TrialRule TrialFor(int encounter) { return Trials != null && encounter < Trials.Length ? Trials[encounter] : null; }

        public int RecommendedLevel;   // the enemy stat-curve level of the stage (1..65); the player level to bring is RecommendedPlayerLevel
        public int RecommendedPlayerLevel { get { return Progression.PlayerLevelAt(RecommendedLevel); } }
        public long DooTotal;   // base stage budget; expanded stages pay additional shares via RewardWeightBudget
        public bool IsTower;    // a 무한 비무대 floor (see Tower): no stage progress, own rewards
        public int TowerFloor;
        public bool IsQuest;    // a 의뢰 built from an earlier stage's fight (see Quests): no stage progress, own exp and rewards
        public string QuestId;
        public int Difficulty;  // 0 일반, 1 어려움, 2 극악 (see StageStars); set on a copy made by WithDifficulty

        /// <summary>Enemy data for this workflow, shared by combat, previews and formation planning. Pass the fight's trial so a 버티기 fight keeps full enemy HP.</summary>
        public EnemyData EnemyFor(string id, TrialRule trial = null)
        {
            var enemy = EarlyGameContent.FindEnemy(id);
            if (enemy == null || IsTower) return enemy;
            return IsQuest ? Quests.Scale(QuestId, enemy) : StageStars.StoryEnemy(enemy, Number, Difficulty, trial);
        }

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
            new ArtData { Id = StarterArtId, Ranged = true, Range = 4, Name = "비도 던지기", Description = "비뢰도의 기초. 비도를 날려 적을 맞힌다.", Power = 1.3f, Cooldown = 0 },
            new ArtData { Id = "samboku_chobok", Name = "삼복구타권법 초복", Description = "사부가 창안한 권법. 초복은 근육을 노린다.", Power = 2.2f, QiCost = 14, Cooldown = 1, LearnCost = 0, UnlockStage = 1 },
            new ArtData { Id = "bunroesu", Name = "분뢰수", Description = "손을 강철처럼 만들어 내려친다.", Inflict = StatusKind.InnerWound, InflictChance = 0.4f, Power = 3.0f, QiCost = 22, Cooldown = 2, LearnCost = 220, UnlockStage = 2 },
            new ArtData { Id = "samcheopyeong", Name = "삼첩영", Description = "봉황무의 잔상 셋. 다음 차례까지 모든 공격을 피한다.", QiCost = 20, Cooldown = 3, Effect = SkillEffect.Evade, LearnCost = 400, UnlockStage = 3 },
            new ArtData { Id = "bi_roe_do", Ranged = true, Range = 4, Name = "비뢰도", Description = "비도 세 자루를 연달아 날린다.", Inflict = StatusKind.Bleed, InflictChance = 0.4f, Power = 1.7f, Hits = 3, QiCost = 32, Cooldown = 3, LearnCost = 900, UnlockStage = 3 },
            new ArtData { Id = "yeobanjang", Name = "여반장", Description = "손바닥을 뒤집는 회전력으로 일격에 제압하는 일장법.", Power = 5.2f, QiCost = 38, Cooldown = 3, LearnCost = 2200, UnlockStage = 5 },
            new ArtData { Id = "roegwang_ryuhagok", Name = "뢰광류하곡", Description = "여섯 자루를 동시에 운용해 적 전체를 벤다.", Inflict = StatusKind.Bleed, InflictChance = 0.25f, Power = 2.6f, AllEnemies = true, QiCost = 46, Cooldown = 4, LearnCost = 4500, UnlockStage = 6 },
            new ArtData { Id = "seonpung_roejeol", Name = "선풍뇌절", Description = "비기. 기세를 모두 쏟아 은빛 섬광으로 적을 쓰러뜨린다.", Power = 9.5f, SpiritCost = 100, LearnCost = 1500, UnlockStage = 4 },
            // Learned in volumes 7-9 (names from the original text)
            new ArtData { Id = "cheongeun_roe", Name = "뇌격타 천근뢰", Description = "천 근의 무게를 실은 뇌전의 일격. 철각비마대의 창을 꺾은 기술.", Inflict = StatusKind.Burn, InflictChance = 0.5f, Power = 6.4f, QiCost = 67, Cooldown = 3, LearnCost = 7000, UnlockStage = 10 },
            new ArtData { Id = "cheonji_ibun", Name = "참단절영의 장 천지이분", Description = "손날로 말과 기수를 한꺼번에 가르는 절삭의 장.", Power = 7.8f, QiCost = 94, Cooldown = 4, LearnCost = 11000, UnlockStage = 15 },
            new ArtData { Id = "bongik_bisang", Name = "사신무 봉익비상", Description = "봉황이 날개를 펴듯 사방으로 쏟아내는 일격. 적 전체를 친다.", Power = 3.6f, AllEnemies = true, QiCost = 110, Cooldown = 4, LearnCost = 15000, UnlockStage = 19 },
            new ArtData { Id = "simroe", Name = "단심무형의 장 심뢰", Description = "비기. 마음을 베는 검기의 오의가 적의 중심을 꿰뚫는다.", Power = 13.0f, SpiritCost = 100, LearnCost = 9000, UnlockStage = 22 },
            // Learned in volumes 10-12 (names from the original text)
            new ArtData { Id = "saengsa_seomroe", Name = "생사결 섬뢰창", Description = "비뢰도 오의 검기. 뇌전의 창이 거대한 영물의 비늘을 꿰뚫는다.", Power = 8.8f, QiCost = 152, Cooldown = 4, LearnCost = 16000, UnlockStage = 27 },
            new ArtData { Id = "eunseompung", Name = "은검풍인의 장 은섬풍", Description = "검기가 은빛 바람이 되어 사방의 적을 찢는다. 적 전체를 친다.", Power = 4.2f, AllEnemies = true, QiCost = 212, Cooldown = 4, LearnCost = 24000, UnlockStage = 5, UnlockDiff = 1 },
            new ArtData { Id = "goengroe", Ranged = true, Name = "굉천광뢰의 장 굉뢰", Description = "작은 은빛 물방울이 도와 무기를 박살내는 비뢰도의 오의.", Power = 10.5f, QiCost = 233, Cooldown = 4, LearnCost = 30000, UnlockStage = 12, UnlockDiff = 1 },
            new ArtData { Id = "seomroe", Range = 4, Name = "섬뢰", Description = "비기. 빛을 뒤쫓는 쾌. 눈 깜짝할 새 미간을 꿰뚫는다.", Power = 17.0f, SpiritCost = 100, LearnCost = 22000, UnlockStage = 18, UnlockDiff = 1 },
            // Learned in volumes 13-16
            new ArtData { Id = "jikji_jilju", Name = "직지질주", Description = "봉황무의 오의. 절벽을 평지처럼 달리며 적을 세 번 벤다.", Power = 3.6f, Hits = 3, QiCost = 278, Cooldown = 4, LearnCost = 34000, UnlockStage = 24, UnlockDiff = 1 },
            new ArtData { Id = "pungsin", Name = "풍신(風神)", Description = "비기. 열 자루 비뢰도와 뇌령사가 나선을 그리며 용권풍이 되어 적 전체를 휩쓴다.", Power = 12.35f, AllEnemies = true, SpiritCost = 100, LearnCost = 44000, UnlockStage = 30, UnlockDiff = 1 },
            // Volumes 17-29 (names from the original text; 연비 is 비류연 in disguise, 21권). 19권 opens on 일반 33, 21-23권 on 어려움, 23-29권 on 극악.
            new ArtData { Id = "jeokjunghwa", Name = "변역의 장 적중화", Description = "비뢰도 상급오의. 상대의 느린 손을 세 번 앞질러 친다.", Power = 3.0f, Hits = 3, QiCost = 160, Cooldown = 4, LearnCost = 18000, UnlockStage = 33 },
            new ArtData { Id = "palhwa_nanmu", Name = "초절기교의 장 팔화난무", Description = "비뢰도 오의. 여덟 송이 꽃이 흩날리듯 여덟 번 몰아친다.", Power = 1.25f, Hits = 8, QiCost = 150, Cooldown = 4, LearnCost = 20000, UnlockStage = 2, UnlockDiff = 1 },
            new ArtData { Id = "ssangseom_ilgyeokroe", Ranged = true, Range = 4, Name = "검뢰사살의 장 충각 쌍섬일격뢰", Description = "비도 두 자루에 모든 힘을 모아 한 점에 꽂는다.", Inflict = StatusKind.Bleed, InflictChance = 0.5f, Power = 5.5f, Hits = 2, QiCost = 180, Cooldown = 4, LearnCost = 26000, UnlockStage = 8, UnlockDiff = 1 },
            new ArtData { Id = "yeomyeong_cheonan", Name = "영사심결 오의 여명천안", Description = "눈이 황금빛으로 물들며 모든 수가 보인다. 다음 차례까지 모든 공격을 피한다.", Effect = SkillEffect.Evade, QiCost = 120, Cooldown = 3, LearnCost = 28000, UnlockStage = 15, UnlockDiff = 1 },
            new ArtData { Id = "sanmak", Name = "산막", Description = "연비의 즉흥 우산 막. 일행 모두를 방어 태세로 만든다.", Effect = SkillEffect.GuardParty, QiCost = 140, Cooldown = 4, LearnCost = 30000, UnlockStage = 21, UnlockDiff = 1 },
            new ArtData { Id = "paego_makpaego", Name = "삼복구타산 궁극 말살기 패고패고막패고", Description = "우산으로 쉬지 않고 열두 번 패고 또 팬다.", Power = 1.0f, Hits = 12, QiCost = 270, Cooldown = 4, LearnCost = 36000, UnlockStage = 27, UnlockDiff = 1 },
            new ArtData { Id = "seomjeon_ilsi", Name = "봉황무 오의 섬전일시", Description = "흙파도를 정면으로 가르고 번개처럼 파고드는 일격.", Power = 12.5f, QiCost = 300, Cooldown = 4, LearnCost = 40000, UnlockStage = 33, UnlockDiff = 1 },
            new ArtData { Id = "cheonji_mugyeon", Name = "삼복구타봉법 말복 천지무견", Description = "천하무구를 고쳐 만든 봉법. 하늘도 땅도 보이지 않게 여섯 번 내려친다.", Power = 2.2f, Hits = 6, QiCost = 310, Cooldown = 4, LearnCost = 42000, UnlockStage = 36, UnlockDiff = 1 },
            new ArtData { Id = "hyeoncheon_hoecheon", Name = "현천은린 회천", Description = "현천은린을 돌려 막아 내며 둘레의 적을 두 번 벤다.", Inflict = StatusKind.Bleed, InflictChance = 0.3f, Power = 2.4f, Hits = 2, AllEnemies = true, QiCost = 230, Cooldown = 4, LearnCost = 46000, UnlockStage = 2, UnlockDiff = 2 },
            new ArtData { Id = "hyeoncheon_powol", Name = "현천은린 오의 현천포월", Description = "비기. 수백 줄기 검은 번개가 달을 감싸듯 적 전체를 덮친다.", Power = 14.0f, AllEnemies = true, SpiritCost = 100, LearnCost = 48000, UnlockStage = 4, UnlockDiff = 2 },
            new ArtData { Id = "hyeoncheon_secheom", Ranged = true, Range = 4, Name = "현천은린 세침", Description = "우산살에 숨긴 가는 침을 여덟 번 쏜다.", Inflict = StatusKind.Bleed, InflictChance = 0.4f, Power = 0.9f, Hits = 8, QiCost = 160, Cooldown = 3, LearnCost = 44000, UnlockStage = 6, UnlockDiff = 2 },
            new ArtData { Id = "hoejeop", Ranged = true, Range = 4, Name = "회접", Description = "손으로 펴 만든 쇠 원반을 나비처럼 날린다.", Power = 6.5f, QiCost = 150, Cooldown = 2, LearnCost = 46000, UnlockStage = 8, UnlockDiff = 2 },
            new ArtData { Id = "mukroesal", Name = "무명검 오의 묵뢰살", Description = "검은 번개를 두른 일격. 심도 천외일도를 꺾은 오의.", Power = 15.0f, QiCost = 330, Cooldown = 4, LearnCost = 50000, UnlockStage = 10, UnlockDiff = 2 },
            new ArtData { Id = "samboku_jungbok", Name = "삼복구타권법 중복", Description = "아구창창 주구장창. 아홉 번 쉬지 않고 두들긴다.", Power = 1.4f, Hits = 9, QiCost = 260, Cooldown = 3, LearnCost = 52000, UnlockStage = 12, UnlockDiff = 2 },
            new ArtData { Id = "jumang_pobak", Range = 4, Name = "괴뢰의 장 주망포박", Description = "뇌령사로 그물을 쳐 상대를 묶는다. 기를 모으는 중이면 끊는다.", Power = 2.0f, Effect = SkillEffect.Stun, QiCost = 200, Cooldown = 4, LearnCost = 52000, UnlockStage = 13, UnlockDiff = 2 },
            new ArtData { Id = "gyeokgongjang", Ranged = true, Range = 4, Name = "격공장", Description = "허공을 격해 떨어진 적을 치는 장력.", Power = 7.5f, QiCost = 170, Cooldown = 2, LearnCost = 54000, UnlockStage = 14, UnlockDiff = 2 },
            new ArtData { Id = "nakroe_yeong", Name = "그림자 숨기기의 장 낙뢰영", Description = "봉황무 오의. 그림자 속으로 몸을 숨겨 다음 차례까지 모든 공격을 피한다.", Effect = SkillEffect.Evade, QiCost = 160, Cooldown = 3, LearnCost = 54000, UnlockStage = 15, UnlockDiff = 2 },
            new ArtData { Id = "noeseom_ilsi", Name = "일점집중의 장 뇌섬일시", Description = "모든 힘을 한 점에 모아 갑옷 속까지 꿰뚫는다.", Power = 16.5f, QiCost = 350, Cooldown = 4, LearnCost = 58000, UnlockStage = 17, UnlockDiff = 2 },
            new ArtData { Id = "ujunggeo_buljeom", Name = "봉황무 비전극상오의 우중거 불점의", Description = "빗속을 걸어도 옷이 젖지 않는다. 다음 차례까지 모든 공격을 피한다.", Effect = SkillEffect.Evade, QiCost = 180, Cooldown = 2, LearnCost = 58000, UnlockStage = 18, UnlockDiff = 2 },
            new ArtData { Id = "simroe_yeolpa", Name = "단심무형의 장 심뢰열파", Description = "마음을 베는 검기를 사방으로 퍼뜨려 적 전체의 마음을 흔든다.", Inflict = StatusKind.InnerWound, InflictChance = 0.5f, Power = 5.5f, AllEnemies = true, QiCost = 320, Cooldown = 4, LearnCost = 60000, UnlockStage = 19, UnlockDiff = 2 },
            new ArtData { Id = "palseom_noegwang", Name = "환영분신의 장 팔섬 뇌광굉타", Description = "여덟 분신이 한꺼번에 뇌광을 내리친다.", Power = 2.1f, Hits = 8, QiCost = 360, Cooldown = 4, LearnCost = 62000, UnlockStage = 21, UnlockDiff = 2 },
            new ArtData { Id = "pyeonsu_pungsin", Range = 4, Name = "편수 풍신", Description = "비기. 한 손으로 다섯 자루 비뢰도를 풀어 물기둥째 적 전체를 쓸어 버린다.", Power = 15.0f, AllEnemies = true, SpiritCost = 100, LearnCost = 66000, UnlockStage = 22, UnlockDiff = 2 },
            new ArtData { Id = "simroe_cheonhyeongin", Name = "심뢰 극 천형인", Description = "비기. 단심무형의 장의 끝. 하늘의 형벌을 새기는 일격.", Power = 26.0f, SpiritCost = 100, LearnCost = 70000, UnlockStage = 24, UnlockDiff = 2 },
            new ArtData { Id = "noejeol_daneum", Range = 4, Name = "벽력단절의 장 뇌절단음", Description = "비뢰도 비전 오의. 소리까지 끊어 내는 벽력으로 상대를 굳힌다. 기를 모으는 중이면 끊는다.", Power = 6.0f, Effect = SkillEffect.Stun, QiCost = 280, Cooldown = 4, LearnCost = 70000, UnlockStage = 25, UnlockDiff = 2 },
            new ArtData { Id = "noemangok", Name = "뇌망옥", Description = "뇌전의 그물 감옥이 적 전체를 일순 얼어붙게 한다.", Inflict = StatusKind.Chill, InflictChance = 0.6f, Power = 4.5f, AllEnemies = true, QiCost = 330, Cooldown = 4, LearnCost = 72000, UnlockStage = 26, UnlockDiff = 2 },
            new ArtData { Id = "samnyeon_guta", Name = "돌아온 말복 천지무견 삼년구타", Description = "삼 년 치를 한 번에 몰아 열두 번 두들긴다.", Power = 1.6f, Hits = 12, QiCost = 400, Cooldown = 4, LearnCost = 76000, UnlockStage = 28, UnlockDiff = 2 },
            new ArtData { Id = "sineunsul_yeongsik", Name = "신은술 영식", Description = "그림자를 먹는 술법을 고쳐 만든 은신. 다음 차례까지 모든 공격을 피한다.", Effect = SkillEffect.Evade, QiCost = 200, Cooldown = 2, LearnCost = 78000, UnlockStage = 30, UnlockDiff = 2 },
            new ArtData { Id = "umin_gunmu", Name = "괴뢰의 장 꼭두각시 춤 우민군무 불신지옥", Description = "적을 서로 이간해 꼭두각시처럼 춤추게 한다. 적 전체가 굳는다.", Power = 3.0f, AllEnemies = true, Effect = SkillEffect.Stun, QiCost = 380, Cooldown = 5, LearnCost = 80000, UnlockStage = 32, UnlockDiff = 2 },
            new ArtData { Id = "hwaryong_nanryu", Name = "용권승천의 장 화룡난류", Description = "비기. 풍신에 불을 실어 화룡의 꼬리와 열풍진, 두 머리의 불길이 적 전체를 휩쓴다.", Inflict = StatusKind.Burn, InflictChance = 0.6f, Power = 17.0f, AllEnemies = true, SpiritCost = 100, LearnCost = 88000, UnlockStage = 34, UnlockDiff = 2 },
            new ArtData { Id = "ssangryong_gwicheon", Name = "뇌화열풍인 쌍룡귀천", Description = "비기. 벽력의 장 심득 오의. 벼락과 화룡과 질풍이 두 마리 용이 되어 하늘로 돌아간다.", Power = 20.0f, AllEnemies = true, SpiritCost = 100, LearnCost = 100000, UnlockStage = 36, UnlockDiff = 2 },
            // Companions' fixed arts (names are descriptive unless the original text names the technique)
            new ArtData { Id = "xr_twin", Name = "쌍검 연격", Description = "쌍둥이 검으로 두 번 벤다.", Power = 1.5f, Hits = 2, QiCost = 18, Cooldown = 1, OwnerId = "hyo_ryong" },
            new ArtData { Id = "xr_barrier", Name = "태을천강벽", Description = "검막을 펼쳐 모두를 방어 태세로 만든다.", Effect = SkillEffect.GuardParty, QiCost = 30, Cooldown = 4, OwnerId = "hyo_ryong" },
            new ArtData { Id = "jh_slash", Name = "검", Description = "정석적인 검격.", Power = 1.9f, QiCost = 14, Cooldown = 1, OwnerId = "jang_hong" },
            new ArtData { Id = "jh_stun", Name = "점혈", Description = "혈도를 짚어 상대를 굳힌다. 기를 모으는 중이면 끊는다.", Power = 0.8f, Effect = SkillEffect.Stun, QiCost = 20, Cooldown = 3, OwnerId = "jang_hong" },
            new ArtData { Id = "yj_slash", Name = "화산의 검", Description = "화산파의 기본 검격. 매화검법은 알레르기 때문에 쓰지 못한다.", Power = 2.2f, QiCost = 16, Cooldown = 1, OwnerId = "yun_junho" },
            new ArtData { Id = "yj_heal", Name = "자하신공", Description = "화산의 자색 기운으로 가장 다친 동료를 치유한다.", Effect = SkillEffect.HealAlly, HealPct = 0.35f, QiCost = 28, Cooldown = 3, OwnerId = "yun_junho" },
            // 자하청심결: game addition (not named in the story) so the party has a way to cure 상태 이상 besides 운기 and items
            new ArtData { Id = "yj_cleanse", Name = "자하청심결", Description = "자하신공의 맑은 기운으로 동료 한 명의 상태 이상을 모두 씻어 내고 조금 치유한다.", Effect = SkillEffect.Cleanse, HealPct = 0.10f, QiCost = 22, Cooldown = 3, OwnerId = "yun_junho", OwnerLevel = 15 },
            new ArtData { Id = "isj_gon", Name = "진성십이곤", Description = "녹림 진성곤의 곤법. 방어는 원, 공격은 점, 힘의 이동은 나선.", Power = 2.0f, QiCost = 16, Cooldown = 1, OwnerId = "im_seongjin" },
            new ArtData { Id = "isj_seonggwang", Name = "성광일시", Description = "곤을 별처럼 흩뿌려 네 번 찌른다.", Power = 0.9f, Hits = 4, QiCost = 30, Cooldown = 3, OwnerId = "im_seongjin" },
            new ArtData { Id = "isj_iljeom", Range = 4, Name = "일점필중", Description = "진성십이곤 최종오의. 한 줄기 섬광이 되어 한 점을 꿰뚫는다.", Power = 5.0f, QiCost = 45, Cooldown = 4, OwnerId = "im_seongjin" },
            new ArtData { Id = "ny_baekhon", Name = "백혼검뢰천검식", Description = "나예린의 공격 검법. 흰 검뢰가 한 줄기로 내리꽂힌다.", Power = 2.4f, QiCost = 16, Cooldown = 1, OwnerId = "na_yerin" },
            new ArtData { Id = "ny_hansang", Name = "한상옥령신검", Description = "얼음처럼 차가운 검기가 세 번 스친다.", Inflict = StatusKind.Chill, InflictChance = 0.6f, Power = 1.2f, Hits = 3, QiCost = 30, Cooldown = 3, OwnerId = "na_yerin" },
            new ArtData { Id = "ny_simahn", Range = 4, Name = "심안검", Description = "용안으로 허점을 읽고 마음의 눈으로 한 점을 꿰뚫는다.", Power = 5.4f, QiCost = 42, Cooldown = 4, OwnerId = "na_yerin" },
            // Companion arts learned at later levels (25-29권 names; 원의 곤·나선곤·십이연격·무영 일격 are game names). A newer art of the same line replaces the older one.
            new ArtData { Id = "xr_hyeolran", Name = "쌍검혈란", Description = "쌍검이 핏빛으로 흐드러져 적 전체를 두 번 벤다.", Inflict = StatusKind.Bleed, InflictChance = 0.3f, Power = 1.0f, Hits = 2, AllEnemies = true, QiCost = 90, Cooldown = 3, OwnerId = "hyo_ryong", OwnerLevel = 18 },
            new ArtData { Id = "xr_ilgyeokbung", Name = "패왕권 일격붕", Description = "쌍검을 거두고 패왕권으로 한 번에 무너뜨린다.", Inflict = StatusKind.InnerWound, InflictChance = 0.5f, Power = 6.0f, QiCost = 180, Cooldown = 4, OwnerId = "hyo_ryong", OwnerLevel = 26 },
            new ArtData { Id = "xr_sipja", Name = "무쌍십자검", Description = "을진무쌍검법. 두 자루가 십자로 엇갈려 벤다.", Inflict = StatusKind.Bleed, InflictChance = 0.4f, Power = 2.0f, Hits = 2, QiCost = 110, Cooldown = 1, OwnerId = "hyo_ryong", OwnerLevel = 36, Replaces = "xr_twin" },
            new ArtData { Id = "xr_bijo", Ranged = true, Range = 4, Name = "비조쌍월인", Description = "효룡의 오의. 쌍검을 두 줄기 달처럼 던져 뒷줄까지 꿰뚫는다.", Inflict = StatusKind.Bleed, InflictChance = 0.6f, Power = 3.6f, Hits = 2, QiCost = 300, Cooldown = 4, OwnerId = "hyo_ryong", OwnerLevel = 48, Replaces = "xr_ilgyeokbung" },
            new ArtData { Id = "xr_cheonoe", Name = "천외일도", Description = "비기. 쌍붕도 중천으로 혈류십이도에 없는 열세 번째 도를 벤다.", Power = 11.0f, SpiritCost = 100, OwnerId = "hyo_ryong", OwnerLevel = 60 },
            new ArtData { Id = "jh_mukyeong", Range = 4, Name = "영사삭 묵영포박", Description = "그림자 같은 영사삭으로 뒷줄의 적까지 묶는다. 기를 모으는 중이면 끊는다.", Power = 1.0f, Effect = SkillEffect.Stun, QiCost = 100, Cooldown = 4, OwnerId = "jang_hong", OwnerLevel = 18 },
            new ArtData { Id = "jh_sineun", Name = "신은술", Description = "무영대장의 그림자 술법. 다음 차례까지 모든 공격을 피한다.", Effect = SkillEffect.Evade, QiCost = 120, Cooldown = 3, OwnerId = "jang_hong", OwnerLevel = 32 },
            new ArtData { Id = "jh_muyeong", Range = 4, Name = "무영 일격", Description = "비기. 그림자 속에서 나타나 단 한 번에 숨통을 끊는다.", Power = 11.0f, SpiritCost = 100, OwnerId = "jang_hong", OwnerLevel = 50 },
            new ArtData { Id = "yj_chilmae", Name = "칠매검", Description = "칠매동에서 얻은 선조의 검보. 매화 일곱 송이처럼 일곱 번 찌른다.", Power = 0.45f, Hits = 7, QiCost = 40, Cooldown = 1, OwnerId = "yun_junho", OwnerLevel = 12, Replaces = "yj_slash" },
            new ArtData { Id = "yj_maeyeom", Name = "매염토망", Description = "이십사수 매화검법 최강의 방어 초식. 일행 모두를 방어 태세로 만든다.", Effect = SkillEffect.GuardParty, QiCost = 130, Cooldown = 4, OwnerId = "yun_junho", OwnerLevel = 30 },
            new ArtData { Id = "yj_maehwa", Name = "이십사수 매화검법", Description = "비기. 두드러기를 이겨 낸 매화검. 매화 향이 적 전체를 네 번 휘감는다.", Power = 1.6f, Hits = 4, AllEnemies = true, SpiritCost = 100, OwnerId = "yun_junho", OwnerLevel = 50 },
            new ArtData { Id = "ny_biseol", Name = "비설보", Description = "검후가 전한 흩날리는 눈의 분신 보법. 다음 차례까지 모든 공격을 피한다.", Effect = SkillEffect.Evade, QiCost = 90, Cooldown = 3, OwnerId = "na_yerin", OwnerLevel = 20 },
            new ArtData { Id = "ny_soan", Name = "소안검", Description = "웃는 눈처럼 가볍고 빠른 검.", Power = 3.0f, QiCost = 50, Cooldown = 1, OwnerId = "na_yerin", OwnerLevel = 24, Replaces = "ny_baekhon" },
            new ArtData { Id = "ny_hyeollan", Name = "현란무답", Description = "한상옥령신검 비설보 오의. 눈보라 속 춤이 적 전체를 세 번 벤다.", Inflict = StatusKind.Chill, InflictChance = 0.3f, Power = 1.1f, Hits = 3, AllEnemies = true, QiCost = 170, Cooldown = 3, OwnerId = "na_yerin", OwnerLevel = 28 },
            new ArtData { Id = "ny_geommak", Name = "검막", Description = "검으로 막을 쳐 일행 모두를 방어 태세로 만든다.", Effect = SkillEffect.GuardParty, QiCost = 160, Cooldown = 4, OwnerId = "na_yerin", OwnerLevel = 34, Replaces = "ny_biseol" },
            new ArtData { Id = "ny_bihong", Name = "비홍검", Description = "붉은 기러기가 날듯 두 번 벤다.", Power = 2.0f, Hits = 2, QiCost = 90, Cooldown = 1, OwnerId = "na_yerin", OwnerLevel = 40, Replaces = "ny_soan" },
            new ArtData { Id = "ny_hanbing", Name = "한빙섬옥", Description = "한상옥령신검 오의(통곡오의). 정교함과 빠름이 맞물린 네 번의 냉기.", Inflict = StatusKind.Chill, InflictChance = 1.0f, Power = 1.6f, Hits = 4, QiCost = 260, Cooldown = 3, OwnerId = "na_yerin", OwnerLevel = 46, Replaces = "ny_hansang" },
            new ArtData { Id = "ny_nanik", Name = "빙백봉무 난익", Description = "한상옥령신검. 얼음 봉황의 날개가 적 전체를 세 번 휩쓴다.", Inflict = StatusKind.Chill, InflictChance = 0.4f, Power = 1.5f, Hits = 3, AllEnemies = true, QiCost = 300, Cooldown = 4, OwnerId = "na_yerin", OwnerLevel = 52, Replaces = "ny_hyeollan" },
            new ArtData { Id = "ny_bihonggwanun", Range = 4, Name = "폭풍비상 비홍관운", Description = "검각 신법. 폭풍처럼 솟구쳐 구름을 꿰뚫는 일격.", Power = 8.5f, QiCost = 360, Cooldown = 4, OwnerId = "na_yerin", OwnerLevel = 58, Replaces = "ny_simahn" },
            new ArtData { Id = "ny_haesang", Name = "해상비조천참절", Description = "비기. 검후의 비전오의. 천 마리 바닷새를 베듯 적 전체를 다섯 번 벤다.", Power = 1.6f, Hits = 5, AllEnemies = true, SpiritCost = 100, OwnerId = "na_yerin", OwnerLevel = 64 },
            new ArtData { Id = "isj_bunggon", Range = 1, Name = "붕곤", Description = "아버지의 패도와 맞선 곤. 적의 앞줄 전체를 무너뜨린다.", Power = 2.2f, AllEnemies = true, QiCost = 160, Cooldown = 3, OwnerId = "im_seongjin", OwnerLevel = 26 },
            new ArtData { Id = "isj_won", Name = "원(圓)의 곤", Description = "방어는 원. 곤을 크게 돌려 일행 모두를 방어 태세로 만든다.", Effect = SkillEffect.GuardParty, QiCost = 170, Cooldown = 4, OwnerId = "im_seongjin", OwnerLevel = 36 },
            new ArtData { Id = "isj_naseon", Name = "나선곤", Description = "힘의 이동은 나선. 비틀어 넣은 곤이 두 번 파고든다.", Inflict = StatusKind.InnerWound, InflictChance = 0.5f, Power = 1.9f, Hits = 2, QiCost = 120, Cooldown = 1, OwnerId = "im_seongjin", OwnerLevel = 46, Replaces = "isj_gon" },
            new ArtData { Id = "isj_sibi", Name = "진성십이곤 · 십이연격", Description = "비기. 진성십이곤 열두 초식을 쉬지 않고 몰아친다.", Power = 0.9f, Hits = 12, SpiritCost = 100, OwnerId = "im_seongjin", OwnerLevel = 60 },
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
            new EnemyData { Id = "tiger", Name = "호랑이", Level = 1, HpMul = 0.797f, AtkMul = 0.436f, SpeedAdd = 8,
                Moves = new[] { EnemyMove.Attack("물어뜯기", 1.0f), EnemyMove.Attack("할퀴기", 0.8f, 2) } },
            new EnemyData { Id = "bear", Name = "곰", Level = 1, HpMul = 1.139f, AtkMul = 0.436f, SpeedAdd = -6,
                Moves = new[] { EnemyMove.Attack("앞발치기", 1.0f), EnemyMove.Wind("몸을 일으킨다"), EnemyMove.Attack("내려치기", 2.4f) } },
            new EnemyData { Id = "white_tiger", Name = "백호", Level = 2, IsBoss = true, HpMul = 1.75f, AtkMul = 0.447f, SpeedAdd = 8,
                Moves = new[] { EnemyMove.Attack("할퀴기", 0.8f, 2), EnemyMove.Attack("물어뜯기", 1.2f), EnemyMove.Wind("몸을 낮춘다"), EnemyMove.Attack("덮치기", 2.8f) } },
            // Stage 2: 중양표국 (1권)
            new EnemyData { Id = "biaosa", Name = "표사", Level = 3, HpMul = 0.878f, AtkMul = 0.490f,
                Moves = new[] { EnemyMove.Attack("베기", 1.0f), EnemyMove.Attack("찌르기", 1.1f) } },
            new EnemyData { Id = "gang_janghan", Name = "비풍도 강장한", Level = 5, HpMul = 1.645f, AtkMul = 0.542f,
                Moves = new[] { EnemyMove.Attack("도", 1.0f), EnemyMove.Attack("도", 1.1f), EnemyMove.Wind("도를 휘두를 자세를 잡는다"), EnemyMove.Attack("내려베기", 2.6f) } },
            // Stage 3: 호골채 (1권)
            new EnemyData { Id = "hogol_bandit", Name = "호골채 산적", Level = 6, HpMul = 0.829f, AtkMul = 0.619f,
                Moves = new[] { EnemyMove.Attack("휘두르기", 1.0f), EnemyMove.Attack("베기", 1.1f) } },
            new EnemyData { Id = "paeho_jung_gwang", Name = "패호 중광", Level = 8, IsBoss = true, HpMul = 1.3f, AtkMul = 0.609f,
                Moves = new[] { EnemyMove.Attack("내려치기", 1.1f), EnemyMove.Attack("연타", 0.7f, 3), EnemyMove.Wind("기합을 모은다"), EnemyMove.Attack("일격", 2.8f) } },
            // Stage 4: 용골채 (1권)
            new EnemyData { Id = "yonggol_bandit", Name = "용골채 산적", Level = 9, HpMul = 1.013f, AtkMul = 0.678f,
                Moves = new[] { EnemyMove.Attack("휘두르기", 1.0f), EnemyMove.Attack("베기", 1.1f) } },
            new EnemyData { Id = "paeryong_wooak", Name = "패룡 우악", Level = 11, IsBoss = true, HpMul = 2.153f, AtkMul = 0.758f,
                Moves = new[] { EnemyMove.Attack("내려치기", 1.1f), EnemyMove.Attack("연타", 0.7f, 3), EnemyMove.Wind("기세를 끌어올린다"), EnemyMove.Attack("패룡격", 3.0f) } },
            // Stage 5: 윤하루 (2권)
            new EnemyData { Id = "jang_ugang", Name = "장우강", Level = 12, HpMul = 1.333f, AtkMul = 1.254f,
                Moves = new[] { EnemyMove.Attack("청성검", 1.0f), EnemyMove.Attack("청성검", 1.2f), EnemyMove.Wind("검을 거둔다"), EnemyMove.Attack("찌르기", 2.2f) } },
            new EnemyData { Id = "yeomdo", Name = "염도 곽영희", Level = 14, IsBoss = true, HpMul = 3.3f, AtkMul = 1.55f,
                Moves = new[] { EnemyMove.Attack("진홍십칠염", 0.8f, 3), EnemyMove.Attack("검염기", 1.3f), EnemyMove.Wind("화령신공을 끌어올린다"), EnemyMove.Attack("화령염천도세", 3.2f) } },
            // Stage 6: 호아장 (2권)
            new EnemyData { Id = "hoajang_disciple", Name = "호아장 제자", Level = 15, HpMul = 1.383f, AtkMul = 0.821f,
                Moves = new[] { EnemyMove.Attack("맹호비격검", 1.0f), EnemyMove.Attack("맹호비격검", 1.1f) } },
            new EnemyData { Id = "ho_cheonsang", Name = "호아장주 호천상", Level = 17, IsBoss = true, HpMul = 2.195f, AtkMul = 0.825f,
                Moves = new[] { EnemyMove.Attack("맹호비격검", 1.0f, 2), EnemyMove.Attack("맹호비격검", 1.3f), EnemyMove.Wind("검기를 모은다"), EnemyMove.Attack("맹호삼십육검", 3.0f) } },
            new EnemyData { Id = "deung_yeoun", Name = "쾌창 등여운", ArtId = "biaosa", Level = 5, HpMul = 0.78f, AtkMul = 0.38f,
                Moves = new[] { EnemyMove.Attack("쾌창", 1.05f, 1), EnemyMove.Attack("연쇄 찌르기", 0.55f, 2) } },
            new EnemyData { Id = "deung_yeoho", Name = "섬연창 등여호", ArtId = "biaosa", Level = 5, HpMul = 0.78f, AtkMul = 0.38f,
                Moves = new[] { EnemyMove.Attack("섬연창", 0.6f, 2), EnemyMove.Attack("오단연쇄격", 0.4f, 3) } },
            new EnemyData { Id = "jang_uyang", Name = "십팔검 장우양", ArtId = "gang_janghan", Level = 6, HpMul = 1.5f, AtkMul = 0.49f, IsBoss = true,
                Moves = new[] { EnemyMove.Attack("난화검", 0.7f, 2), EnemyMove.Attack("난화수", 1.2f, 1), EnemyMove.Wind("난화검을 펼친다"), EnemyMove.Attack("난화검", 2.4f) } },
            new EnemyData { Id = "jo_yeonil", Name = "무영각 조연일", ArtId = "dan_pyeong", Level = 29, HpMul = 1.95f, AtkMul = 4.4f,
                Moves = new[] { EnemyMove.Attack("무영각", 0.65f, 2), EnemyMove.Attack("발차기", 1.2f, 1) } },
            new EnemyData { Id = "jin_pae", Name = "용아조 진패", ArtId = "dan_pyeong", Level = 29, HpMul = 2.0f, AtkMul = 4.4f,
                Moves = new[] { EnemyMove.Attack("용아조", 0.7f, 2), EnemyMove.Attack("파고들기", 1.2f, 1) } },
            new EnemyData { Id = "ilyeon", Name = "소림 일연", ArtId = "dan_pyeong", Level = 30, HpMul = 2.15f, AtkMul = 4.2f,
                Moves = new[] { EnemyMove.Attack("권격", 1.15f, 1), EnemyMove.Attack("연환 타격", 0.65f, 2) } },
            new EnemyData { Id = "admit_jeomchang", Name = "점창파 응시생", ArtId = "dan_pyeong", Level = 18, HpMul = 1.1f, AtkMul = 0.8f,
                Moves = new[] { EnemyMove.Attack("검격", 1.0f, 1), EnemyMove.Attack("연속 베기", 0.6f, 2) } },
            new EnemyData { Id = "admit_hwasan", Name = "화산파 응시생", ArtId = "jongri_hak", Level = 18, HpMul = 1.15f, AtkMul = 0.82f,
                Moves = new[] { EnemyMove.Attack("검격", 1.0f, 1), EnemyMove.Attack("검격", 0.65f, 2) } },
            new EnemyData { Id = "admit_geumbok", Name = "금복산장 응시생", ArtId = "biaosa", Level = 19, HpMul = 1.15f, AtkMul = 0.82f,
                Moves = new[] { EnemyMove.Attack("일격", 1.2f, 1), EnemyMove.Attack("연속 타격", 0.65f, 2) } },
            new EnemyData { Id = "admit_eonga", Name = "진주언가 응시생", ArtId = "eonga_yeong", Level = 19, HpMul = 1.2f, AtkMul = 0.85f,
                Moves = new[] { EnemyMove.Attack("창격", 1.2f, 1), EnemyMove.Attack("창격", 0.65f, 2) } },
            new EnemyData { Id = "no_jusaeng", Name = "곤륜쌍검 노주생", ArtId = "ha_sein", Level = 27, HpMul = 1.0f, AtkMul = 1.1f,
                Moves = new[] { EnemyMove.Attack("양의합벽검진", 0.65f, 2), EnemyMove.Attack("검격", 1.1f, 1) } },
            new EnemyData { Id = "chae_hagun", Name = "곤륜쌍검 채하군", ArtId = "ha_sein", Level = 27, HpMul = 1.0f, AtkMul = 1.1f,
                Moves = new[] { EnemyMove.Attack("양의합벽검진", 1.1f, 1), EnemyMove.Attack("검격", 0.65f, 2) } },
            new EnemyData { Id = "oh_muhyeong", Name = "형산파 오문형", ArtId = "dan_pyeong", Level = 27, HpMul = 1.25f, AtkMul = 1.3f,
                Moves = new[] { EnemyMove.Attack("검격", 1.15f, 1), EnemyMove.Attack("검격", 0.65f, 2) } },
            new EnemyData { Id = "jang_daepyeong", Name = "천웅보 장대평", ArtId = "biaosa", Level = 27, HpMul = 1.35f, AtkMul = 1.3f,
                Moves = new[] { EnemyMove.Attack("일격", 1.2f, 1), EnemyMove.Attack("연환 타격", 0.65f, 2) } },
            new EnemyData { Id = "gwan_seolji_story", Name = "청설옥검녀 관설지", ArtId = "default_woman", Level = 31, HpMul = 8.0f, AtkMul = 0.9f,
                Moves = new[] { EnemyMove.Attack("빙령수류검", 0.7f, 2), EnemyMove.Attack("한빙벽", 1.1f, 1) } },
            new EnemyData { Id = "cheong_heun_story", Name = "삼절검 청흔", ArtId = "ha_sein", Level = 31, HpMul = 8.0f, AtkMul = 0.9f,
                Moves = new[] { EnemyMove.Attack("삼환회선비검", 0.65f, 3), EnemyMove.Wind("네 번째 검을 펼친다"), EnemyMove.Attack("무극검", 2.1f, 1) } },
            // Stage 7: 검혼관 기관 노방 (2권)
            new EnemyData { Id = "trap_arrow", Name = "기관 화살대", Level = 19, HpMul = 1.403f, AtkMul = 3.188f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("화살 연사", 0.6f, 3).FromDistance().Sniping(), EnemyMove.Attack("화살", 1.0f).FromDistance().Sniping() } },
            new EnemyData { Id = "trap_needle", Name = "기관 비침대", Level = 19, HpMul = 1.403f, AtkMul = 3.188f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("비침", 0.8f, 2).FromDistance().Sniping(), EnemyMove.Wind("비침을 장전한다"), EnemyMove.Attack("비침 난사", 0.9f, 4).FromDistance() } },
            new EnemyData { Id = "trap_log", Name = "기관 통나무", Level = 20, HpMul = 2.104f, AtkMul = 3.552f, SpeedAdd = -8,
                Moves = new[] { EnemyMove.Wind("통나무가 흔들린다"), EnemyMove.Attack("통나무 휘두르기", 2.4f) } },
            new EnemyData { Id = "ho_cheongang", Name = "호아장 대사형 호천강", Level = 21, IsBoss = true, HpMul = 3.157f, AtkMul = 3.552f,
                Moves = new[] { EnemyMove.Attack("맹호삼십육검", 0.7f, 3), EnemyMove.Attack("패왕노호검", 1.3f), EnemyMove.Wind("패왕노호검을 치켜든다"), EnemyMove.Attack("패왕노호검 일격", 3.0f) } },
            // Stage 8: 천무식당 (3권)
            new EnemyData { Id = "jongri_hak", Name = "종리학", Level = 21, HpMul = 1.562f, AtkMul = 3.428f,
                Moves = new[] { EnemyMove.Attack("매화검법", 0.8f, 2), EnemyMove.Attack("매화검법", 1.2f) } },
            new EnemyData { Id = "chu_gayeon", Name = "추가연", Level = 23, IsBoss = true, HpMul = 2.950f, AtkMul = 3.608f,
                Moves = new[] { EnemyMove.Attack("매화검법", 0.8f, 3), EnemyMove.Attack("매화검법", 1.2f), EnemyMove.Wind("검을 모은다"), EnemyMove.Attack("매화만개", 3.0f) } },
            // Stage 9: 운향정 (3권)
            new EnemyData { Id = "gu_chang", Name = "일도파쇄 구창", Level = 23, HpMul = 2.230f, AtkMul = 5.745f,
                Moves = new[] { EnemyMove.Attack("도", 1.1f), EnemyMove.Wind("도를 치켜든다"), EnemyMove.Attack("일도파쇄", 2.6f) } },
            new EnemyData { Id = "so_cheongun", Name = "청풍검 소천군", Level = 24, HpMul = 2.127f, AtkMul = 5.745f,
                Moves = new[] { EnemyMove.Attack("청성검", 0.7f, 3), EnemyMove.Attack("청성검", 1.3f), EnemyMove.Wind("무형지기를 끌어올린다"), EnemyMove.Attack("검풍", 2.6f) } },
            new EnemyData { Id = "wiji_cheon", Name = "선풍검룡 위지천", Level = 26, IsBoss = true, HpMul = 4.238f, AtkMul = 4.834f,
                Moves = new[] { EnemyMove.Attack("절풍검기변", 0.7f, 3), EnemyMove.Attack("절풍검기변", 1.2f), EnemyMove.Wind("검강을 끌어올린다"), EnemyMove.Attack("천원삼절검격", 1.5f, 3) } },
            // Stage 10: 백향관 (4권)
            new EnemyData { Id = "sunyeo", Name = "독한녀 십이선녀", Level = 25, HpMul = 2.228f, AtkMul = 5.418f,
                Moves = new[] { EnemyMove.Attack("독침", 0.7f, 3).FromDistance().Sniping(), EnemyMove.Attack("난향장", 1.2f), EnemyMove.Wind("독향을 퍼뜨린다"), EnemyMove.Attack("절독난무", 2.6f) } },
            new EnemyData { Id = "na_yerin", Name = "빙백봉 나예린", Level = 27, IsBoss = true, HpMul = 4.456f, AtkMul = 5.633f,
                Moves = new[] { EnemyMove.Attack("백뢰낙화", 0.7f, 3), EnemyMove.Attack("섬뢰관일", 1.3f), EnemyMove.Wind("백광검뢰를 모은다"), EnemyMove.Attack("참천무뢰", 3.0f) } },
            // Stage 11: 승급시험 (4권)
            new EnemyData { Id = "ha_sein", Name = "철검비룡 하세인", Level = 27, HpMul = 1.765f, AtkMul = 4.118f,
                Moves = new[] { EnemyMove.Attack("철비룡검", 1.0f), EnemyMove.Wind("검기를 맺는다"), EnemyMove.Attack("징계의 일검", 2.6f) } },
            new EnemyData { Id = "gang_hyeonchu", Name = "영취검 강현추", Level = 28, HpMul = 1.655f, AtkMul = 4.118f,
                Moves = new[] { EnemyMove.Attack("무산검", 0.8f, 2), EnemyMove.Attack("무산검", 1.2f) } },
            new EnemyData { Id = "dan_daepung", Name = "백호단 단대풍", Level = 29, IsBoss = true, HpMul = 4.250f, AtkMul = 5.000f,
                Moves = new[] { EnemyMove.Attack("점창검", 0.7f, 3), EnemyMove.Attack("점창검", 1.2f), EnemyMove.Wind("살기를 끌어올린다"), EnemyMove.Attack("살기 어린 일검", 3.0f) } },
            // Stage 12: 삼성무제 본선 (5권)
            new EnemyData { Id = "dan_pyeong", Name = "쾌환검 단평", Level = 29, HpMul = 2.035f, AtkMul = 5.562f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("남해 36검", 0.6f, 3), EnemyMove.Attack("쾌검살초", 1.3f) } },
            new EnemyData { Id = "eonga_yeong", Name = "파풍창 언가영", Level = 30, HpMul = 2.172f, AtkMul = 4.156f,
                Moves = new[] { EnemyMove.Attack("언가묵창", 1.1f), EnemyMove.Wind("창을 거둔다"), EnemyMove.Attack("파풍창", 2.6f) } },
            new EnemyData { Id = "jeon_okgi", Name = "팔비신검 전옥기", Level = 31, IsBoss = true, HpMul = 4.070f, AtkMul = 5.742f,
                Moves = new[] { EnemyMove.Attack("팔비검", 0.6f, 4), EnemyMove.Attack("점창검", 1.2f), EnemyMove.Wind("검을 모은다"), EnemyMove.Attack("팔비신검", 3.0f) } },
            // Stage 13: 삼성대전 결승 (5권)
            new EnemyData { Id = "dang_munceon", Name = "독랄수 당문천", Level = 31, HpMul = 2.827f, AtkMul = 6.156f,
                Moves = new[] { EnemyMove.Attack("삼환비선", 0.7f, 3), EnemyMove.Attack("묵편", 1.2f), EnemyMove.Wind("독을 푼다"), EnemyMove.Attack("독편 난무", 2.6f) } },
            new EnemyData { Id = "wiji_cheon_baek", Name = "선풍검룡 위지천(백일연무)", Level = 33, IsBoss = true, HpMul = 6.153f, AtkMul = 6.435f,
                Moves = new[] { EnemyMove.Attack("선풍우뢰", 0.7f, 3), EnemyMove.Attack("선풍참혼", 1.3f), EnemyMove.Wind("검강을 끌어올린다"), EnemyMove.Attack("천풍마뢰참", 3.2f) } },
            // Additional volume 6-10 encounters; alternate ArtId values reuse existing battle figures.
            new EnemyData { Id = "maeng_yeonho", ArtId = "jongri_hak", Name = "청룡단주 맹연호", Level = 32, HpMul = 3.0f, AtkMul = 3.3f,
                Moves = new[] { EnemyMove.Attack("매화검", 0.7f, 3), EnemyMove.Wind("매화만천의 검세를 펼친다"), EnemyMove.Attack("매화만천", 2.6f) } },
            new EnemyData { Id = "amryong_salsu", ArtId = "assassin", Name = "암룡대 수중 살수", Level = 32, HpMul = 1.25f, AtkMul = 3.0f,
                Moves = new[] { EnemyMove.Attack("수중 비수", 0.75f, 2), EnemyMove.Attack("암습", 1.15f) } },
            new EnemyData { Id = "mak_jeok", ArtId = "paeho_jung_gwang", Name = "적웅산도 막적", Level = 32, HpMul = 3.1f, AtkMul = 3.2f, DefMul = 1.1f,
                Moves = new[] { EnemyMove.Attack("산도", 1.0f), EnemyMove.Wind("도를 치켜든다"), EnemyMove.Attack("적웅산도", 2.5f) } },
            new EnemyData { Id = "heuk_sang", ArtId = "assassin", Name = "암혼비영대 흑상", Level = 33, HpMul = 3.1f, AtkMul = 3.45f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("암영검", 0.65f, 3), EnemyMove.Wind("그림자 속에서 검을 고른다"), EnemyMove.Attack("흑살도", 2.6f) } },
            new EnemyData { Id = "bikwae_ssangchang", ArtId = "sadae_jilpung", Name = "비쾌쌍창 선봉", Level = 34, HpMul = 1.15f, AtkMul = 3.0f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("선봉 돌격", 1.0f), EnemyMove.Attack("쌍창", 0.6f, 2) } },
            new EnemyData { Id = "pae_yugyeong", ArtId = "pae_hyeokseong", Name = "단옥도 팽유경", Level = 38, HpMul = 3.0f, AtkMul = 3.65f,
                Moves = new[] { EnemyMove.Attack("단옥도", 1.05f), EnemyMove.Wind("호위의 칼을 세운다"), EnemyMove.Attack("단옥도 일격", 2.6f) } },
            new EnemyData { Id = "hyo_ryong_rope", ArtId = "hyo_ryong", Name = "효룡 · 줄 위 비무", Level = 41, HpMul = 3.6f, AtkMul = 5.6f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("쌍검", 0.8f, 2), EnemyMove.Wind("줄 위에서 쌍검을 교차한다"), EnemyMove.Attack("쌍검 합격", 1.6f, 2) } },
            new EnemyData { Id = "hwan_geomseong", ArtId = "hwan_sabu", Name = "환영 속 검성", Level = 43, HpMul = 3.0f, AtkMul = 3.0f,
                Moves = new[] { EnemyMove.Attack("나뭇가지", 1.1f), EnemyMove.Wind("검성의 기세가 다가온다"), EnemyMove.Attack("환영의 검", 2.0f) } },
            new EnemyData { Id = "hwan_gal_hyobong", ArtId = "gal_hyobong", Name = "환영 속 갈효봉", Level = 43, HpMul = 3.0f, AtkMul = 3.0f,
                Moves = new[] { EnemyMove.Attack("죄책감의 쌍도", 0.7f, 2), EnemyMove.Attack("형의 환영", 1.1f) } },
            // Stage 14: 무당산 고립 (7권)
            new EnemyData { Id = "chohon_gwigeom", Name = "초혼오귀검 귀영살검", Level = 32, HpMul = 2.011f, AtkMul = 3.563f,
                Moves = new[] { EnemyMove.Attack("암습검", 0.8f, 2), EnemyMove.Attack("소도", 1.2f) } },
            new EnemyData { Id = "myeongbu_jang", Name = "명부오귀장 명염도 추항", Level = 33, HpMul = 2.280f, AtkMul = 3.563f,
                Moves = new[] { EnemyMove.Attack("명부도", 1.0f), EnemyMove.Wind("도를 높이 든다"), EnemyMove.Attack("명염참", 2.6f) } },
            new EnemyData { Id = "gal_hyobong", Name = "혈류도 갈효봉(폭주)", Level = 34, HpMul = 4.291f, AtkMul = 3.799f,
                Moves = new[] { EnemyMove.Attack("광무쌍도", 0.7f, 3), EnemyMove.Attack("혈풍무", 1.3f), EnemyMove.Wind("혈광을 일으킨다"), EnemyMove.Attack("굉천혈류패왕십자인", 3.2f) } },
            new EnemyData { Id = "cheonsal", Name = "천살 초혼검", Level = 35, IsBoss = true, HpMul = 4.023f, AtkMul = 3.799f,
                Moves = new[] { EnemyMove.Attack("초혼귀령검", 0.7f, 3), EnemyMove.Attack("소혼붕렬", 1.3f), EnemyMove.Wind("초혼령을 울린다"), EnemyMove.Attack("귀령압혼", 3.0f) } },
            new EnemyData { Id = "jisal", Name = "지살 명왕도", Level = 35, IsBoss = true, HpMul = 4.023f, AtkMul = 3.799f,
                Moves = new[] { EnemyMove.Attack("명왕도", 1.0f), EnemyMove.Attack("명왕도", 1.2f), EnemyMove.Wind("웃는 얼굴이 굳는다"), EnemyMove.Attack("명왕참", 2.8f) } },
            // Stage 15: 철각비마대 (8권, 회상)
            new EnemyData { Id = "sadae_jilpung", Name = "사대질풍 철기병", Level = 35, HpMul = 1.335f, AtkMul = 3.656f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("철쇄선풍", 0.8f, 2), EnemyMove.Attack("거창", 1.2f) } },
            new EnemyData { Id = "cheolbi_gyeok", Name = "철비십이격 창기병", Level = 36, HpMul = 1.419f, AtkMul = 3.656f,
                Moves = new[] { EnemyMove.Attack("십자투창", 0.7f, 3), EnemyMove.Wind("창을 허리에 건다"), EnemyMove.Attack("기창충격", 2.6f) } },
            new EnemyData { Id = "wi_musang", Name = "철각비마 위무상", Level = 36, HpMul = 1.669f, AtkMul = 3.656f,
                Moves = new[] { EnemyMove.Attack("삼단질풍격", 0.7f, 3), EnemyMove.Attack("거창돌격", 1.3f) } },
            new EnemyData { Id = "gu_cheonhak", Name = "질풍묵흔 구천학", Level = 37, IsBoss = true, HpMul = 2.671f, AtkMul = 3.738f,
                Moves = new[] { EnemyMove.Attack("허공격상", 0.7f, 3), EnemyMove.Attack("파랑분쇄격", 1.3f), EnemyMove.Wind("창을 머리 위로 든다"), EnemyMove.Attack("무영인", 3.2f) } },
            // Stage 16: 저잣거리 습격 (8권)
            new EnemyData { Id = "baegin_jagaek", Name = "백인회 변장 자객", Level = 36, HpMul = 1.733f, AtkMul = 2.792f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("암기", 0.6f, 3).FromDistance().Sniping(), EnemyMove.Attack("암습", 1.3f).Sniping() } },
            new EnemyData { Id = "ssanggwi", Name = "이인일체 쌍귀 형제", Level = 37, HpMul = 2.477f, AtkMul = 2.851f,
                Moves = new[] { EnemyMove.Attack("쌍도", 1.0f), EnemyMove.Wind("배 속에서 손이 튀어나온다"), EnemyMove.Attack("이인일체 비수", 2.8f) } },
            new EnemyData { Id = "baek_tanjeo", Name = "백인회주 관살 백탄저", Level = 37, IsBoss = true, HpMul = 3.715f, AtkMul = 2.851f,
                Moves = new[] { EnemyMove.Attack("독검", 0.7f, 3), EnemyMove.Attack("극독 일섬", 1.3f), EnemyMove.Wind("독검을 뽑아 든다"), EnemyMove.Attack("맹독 난자", 3.0f) } },
            // Stage 17: 마하령과 군웅회 (9권)
            new EnemyData { Id = "sinung_daewon", Name = "신응대 대원", Level = 38, HpMul = 2.152f, AtkMul = 3.791f,
                Moves = new[] { EnemyMove.Attack("철벽진", 0.8f, 2), EnemyMove.Attack("도격", 1.2f) } },
            new EnemyData { Id = "pae_hyeokseong", Name = "광풍맹호도 팽혁성", Level = 39, IsBoss = true, HpMul = 4.3f, AtkMul = 3.858f,
                Moves = new[] { EnemyMove.Attack("오호단문도", 1.0f), EnemyMove.Wind("맹호출림의 자세"), EnemyMove.Attack("맹호출림", 2.8f) } },
            new EnemyData { Id = "ma_haryeong", Name = "철옥잠 마하령", Level = 40, IsBoss = true, HpMul = 4.305f, AtkMul = 3.858f,
                Moves = new[] { EnemyMove.Attack("표류무상도", 0.7f, 3), EnemyMove.Attack("철옥참", 1.3f), EnemyMove.Wind("천축유가신공을 끌어올린다"), EnemyMove.Attack("표류무상참", 3.2f) } },
            // Stage 18: 태극의 인재 (9권)
            new EnemyData { Id = "yong_cheonmyeong", Name = "창천룡 용천명", Level = 40, HpMul = 4.246f, AtkMul = 7.792f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("백룡검", 0.7f, 3), EnemyMove.Attack("백룡검 변초", 1.3f), EnemyMove.Wind("반야신공을 일으킨다"), EnemyMove.Attack("백룡검 일섬", 3.0f) } },
            new EnemyData { Id = "gwan_cheolsu", Name = "빙검 관철수", Level = 42, IsBoss = true, HpMul = 5.775f, AtkMul = 7.945f,
                Moves = new[] { EnemyMove.Attack("빙령수혼", 0.7f, 3), EnemyMove.Attack("백빙신첨", 1.3f), EnemyMove.Wind("한빙지기를 끌어올린다"), EnemyMove.Attack("백빙신정", 3.4f) } },
            // Stage 19: 환마동 시험 (10권)
            new EnemyData { Id = "hwan_ying", Name = "환마동 환영", Level = 43, HpMul = 2.418f, AtkMul = 3.257f,
                Moves = new[] { EnemyMove.Attack("조롱", 0.8f, 2), EnemyMove.Attack("덮치기", 1.2f) } },
            new EnemyData { Id = "hwan_sukbu", Name = "환영 속 광풍장", Level = 44, HpMul = 3.326f, AtkMul = 3.257f,
                Moves = new[] { EnemyMove.Attack("음습한 손길", 1.0f), EnemyMove.Wind("눈을 번뜩인다"), EnemyMove.Attack("광풍장", 2.8f) } },
            new EnemyData { Id = "hwan_sabu", Name = "환영 속 사부", Level = 45, IsBoss = true, HpMul = 5.441f, AtkMul = 3.363f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("낚싯줄", 0.7f, 3), EnemyMove.Attack("뇌신의 일격", 1.3f), EnemyMove.Wind("백발이 곤두선다"), EnemyMove.Attack("풍신의 일격", 3.2f) } },
            // Stage 20: 매몰 44일 (10권)
            new EnemyData { Id = "cave_viper", Name = "동굴 독충", Level = 45, HpMul = 2.346f, AtkMul = 3.127f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("독니", 0.8f, 2), EnemyMove.Attack("휘감기", 1.2f) } },
            new EnemyData { Id = "mukrin_hyeolmang", Name = "묵린혈망", Level = 47, IsBoss = true, HpMul = 5.942f, AtkMul = 3.233f,
                Moves = new[] { EnemyMove.Attack("꼬리치기", 0.7f, 3), EnemyMove.Attack("독액", 1.3f).FromDistance().Sniping(), EnemyMove.Wind("바위를 녹이는 침을 모은다"), EnemyMove.Attack("묵린 강타", 3.3f) } },
            // Stage 21: 흑랑채 청혼 (11권)
            new EnemyData { Id = "heukrang_bandit", Name = "흑랑채 산적", Level = 47, HpMul = 2.331f, AtkMul = 3.218f,
                Moves = new[] { EnemyMove.Attack("흑랑조", 0.8f, 2), EnemyMove.Attack("도끼질", 1.2f) } },
            new EnemyData { Id = "lee_songhak", Name = "군자소요검 이송학", Level = 48, HpMul = 3.017f, AtkMul = 3.218f,
                Moves = new[] { EnemyMove.Attack("소요검", 0.8f, 2), EnemyMove.Wind("수비 자세를 푼다"), EnemyMove.Attack("군자일검", 2.6f) } },
            new EnemyData { Id = "im_gae", Name = "흑랑부 임개", Level = 49, IsBoss = true, HpMul = 4.936f, AtkMul = 3.310f,
                Moves = new[] { EnemyMove.Attack("흑랑십이부", 0.7f, 3), EnemyMove.Attack("쌍아격", 1.3f), EnemyMove.Wind("도끼를 치켜든다"), EnemyMove.Attack("태산양분", 3.3f) } },
            // Stage 22: 낙뢰곡 철쇄봉혼진 (11~12권)
            new EnemyData { Id = "hyeolma_dae", Name = "십이혈마대 대원", Level = 49, HpMul = 2.420f, AtkMul = 2.568f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("혈검", 0.8f, 2), EnemyMove.Attack("암습", 1.3f).Sniping() } },
            new EnemyData { Id = "hyeol_gung", Name = "9대 조장 혈궁", Level = 50, HpMul = 3.226f, AtkMul = 2.914f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("혈마전", 0.7f, 3).FromDistance().Sniping(), EnemyMove.Wind("혈마궁을 당긴다"), EnemyMove.Attack("혈야구성", 2.8f).FromDistance() } },
            new EnemyData { Id = "hyeol_swae", Name = "10대 조장 혈쇄", Level = 51, IsBoss = true, HpMul = 5.109f, AtkMul = 2.914f,
                Moves = new[] { EnemyMove.Attack("철쇄", 0.8f, 2), EnemyMove.Attack("비차륜", 1.3f), EnemyMove.Wind("철쇄봉혼진을 펼친다"), EnemyMove.Attack("혈겁쇄법 노룡분쇄", 3.3f) } },
            // Stage 23: 비사진 (12권)
            new EnemyData { Id = "bisa_snake", Name = "비사진의 독사 떼", Level = 51, HpMul = 2.237f, AtkMul = 2.892f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("독니", 0.8f, 2), EnemyMove.Attack("휘감기", 1.2f) } },
            new EnemyData { Id = "cheonghong_ssanggaksa", Name = "청홍쌍각사", Level = 52, HpMul = 3.158f, AtkMul = 2.970f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("독니", 0.7f, 3), EnemyMove.Wind("요광을 모은다"), EnemyMove.Attack("요광", 2.6f) } },
            new EnemyData { Id = "mo_saryeong", Name = "비사마군 모사령", Level = 53, IsBoss = true, HpMul = 5.131f, AtkMul = 2.970f,
                Moves = new[] { EnemyMove.Attack("만사혈장", 0.7f, 3), EnemyMove.Attack("비사진", 1.3f), EnemyMove.Wind("사충적을 입에 문다"), EnemyMove.Attack("지사진", 3.3f) } },
            // Stage 24: 비뢰쌍마 (12권)
            new EnemyData { Id = "dongbang_hak", Name = "섬뢰마검 동방학", Level = 54, HpMul = 5.313f, AtkMul = 4.197f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("붉은 검", 0.7f, 3), EnemyMove.Attack("섬뢰", 1.3f), EnemyMove.Wind("쾌검의 자세를 취한다"), EnemyMove.Attack("섬뢰일섬", 3.0f) } },
            new EnemyData { Id = "gong_pae", Name = "잔뢰마도 공패", Level = 55, IsBoss = true, HpMul = 7.438f, AtkMul = 4.197f,
                Moves = new[] { EnemyMove.Attack("잔뢰도", 0.8f, 2), EnemyMove.Attack("잔뢰삼십육패", 1.3f), EnemyMove.Wind("도를 높이 든다"), EnemyMove.Attack("잔뢰번천지", 3.4f) } },
            // Stage 25: 화평장 (13권)
            new EnemyData { Id = "hwapyeong_soldier", Name = "화평장 훈련병", Level = 55, HpMul = 2.686f, AtkMul = 2.971f,
                Moves = new[] { EnemyMove.Attack("연환검", 0.8f, 2), EnemyMove.Attack("포위 돌격", 1.2f) } },
            new EnemyData { Id = "ssangsal_hapgyeok", Name = "천지쌍살 합격술(한 팔씩)", Level = 56, HpMul = 4.523f, AtkMul = 2.971f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("초혼귀령검", 0.7f, 3), EnemyMove.Attack("명왕도", 1.3f), EnemyMove.Wind("쌍살이 서로의 빈 팔을 맞댄다"), EnemyMove.Attack("쌍살 합격", 3.0f) } },
            new EnemyData { Id = "baek_son_gosu", Name = "하얀 손의 고수", Level = 57, IsBoss = true, HpMul = 5.937f, AtkMul = 3.063f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("단월인", 0.7f, 3), EnemyMove.Attack("무형 칼날", 1.3f), EnemyMove.Wind("하얀 손을 편다"), EnemyMove.Attack("단월인 극의", 3.3f) } },
            // Stage 26: 살귀대 (14권)
            new EnemyData { Id = "salgwi_daewon", Name = "살귀대원", Level = 57, HpMul = 2.778f, AtkMul = 2.672f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("추격검", 0.8f, 2), EnemyMove.Attack("암살", 1.3f) } },
            new EnemyData { Id = "ho_yugwang", Name = "화평장주 호유광", Level = 58, IsBoss = true, HpMul = 5.834f, AtkMul = 2.672f,
                Moves = new[] { EnemyMove.Attack("살귀 지휘검", 0.7f, 3), EnemyMove.Attack("독조", 1.3f), EnemyMove.Wind("살귀대에 신호를 보낸다"), EnemyMove.Attack("살귀 일제 공격", 3.2f) } },
            // Stage 27: 마천칠걸 (14권)
            new EnemyData { Id = "jongri_chu", Name = "칠련창 종리추", Level = 59, HpMul = 3.735f, AtkMul = 4.167f,
                Moves = new[] { EnemyMove.Attack("칠성창", 0.7f, 3), EnemyMove.Wind("일곱 번째 창을 겨눈다"), EnemyMove.Attack("북두칠성 투창", 2.7f) } },
            new EnemyData { Id = "do_chuun", Name = "사갈검편 도추운", Level = 59, HpMul = 3.447f, AtkMul = 4.167f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("검편 채찍", 0.7f, 3), EnemyMove.Attack("사갈편", 1.3f) } },
            new EnemyData { Id = "oh_munchu", Name = "쇄풍겸 오문추", Level = 60, HpMul = 3.735f, AtkMul = 4.283f,
                Moves = new[] { EnemyMove.Attack("사슬낫", 0.8f, 2), EnemyMove.Wind("사슬을 휘감는다"), EnemyMove.Attack("쇄풍 난무", 2.8f) } },
            new EnemyData { Id = "baek_sayeong", Name = "사교검 백사영", Level = 60, HpMul = 3.735f, AtkMul = 4.283f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("사교검", 0.7f, 3), EnemyMove.Wind("뱀처럼 몸을 낮춘다"), EnemyMove.Attack("백사교룡살", 2.9f) } },
            new EnemyData { Id = "chu_myeong", Name = "마검익 추명", Level = 61, IsBoss = true, HpMul = 6.320f, AtkMul = 4.283f,
                Moves = new[] { EnemyMove.Attack("회선인", 0.7f, 3), EnemyMove.Attack("흑응익", 1.3f), EnemyMove.Wind("흑풍을 일으킨다"), EnemyMove.Attack("흑풍난무", 3.3f) } },
            // Stage 28: 멸성대 (15권)
            new EnemyData { Id = "mulsung_daewon", Name = "멸성대 대원", Level = 61, HpMul = 2.879f, AtkMul = 2.464f,
                Moves = new[] { EnemyMove.Attack("살인검", 0.8f, 2), EnemyMove.Attack("검익진", 1.3f) } },
            new EnemyData { Id = "i_gae_bujo", Name = "멸성대 부조장 이개", Level = 62, HpMul = 3.663f, AtkMul = 2.514f,
                Moves = new[] { EnemyMove.Attack("호위검", 0.8f, 2), EnemyMove.Wind("만근검익진을 펼친다"), EnemyMove.Attack("만근추", 2.7f) } },
            new EnemyData { Id = "sama_heun", Name = "천참마검 사마흔", Level = 63, IsBoss = true, HpMul = 6.018f, AtkMul = 2.514f,
                Moves = new[] { EnemyMove.Attack("천참검", 0.7f, 3), EnemyMove.Attack("독검", 1.3f), EnemyMove.Wind("검을 뽑아 든다"), EnemyMove.Attack("천겁 일섬", 3.4f) } },
            // Stage 29: 중토관 (16권)
            new EnemyData { Id = "suda_bokmyeon", Name = "수다쟁이 복면인", Level = 64, HpMul = 4.476f, AtkMul = 2.282f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("가벼운 검", 0.7f, 3), EnemyMove.Wind("호들갑을 떨며 몸을 낮춘다"), EnemyMove.Attack("도우미의 일격", 2.8f) } },
            new EnemyData { Id = "mu_jeong", Name = "잔무일점혈 무정", Level = 65, IsBoss = true, HpMul = 7.161f, AtkMul = 2.335f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("그림자 비수", 0.7f, 3).FromDistance().Sniping(), EnemyMove.Attack("무언독 암기", 1.3f).FromDistance().Sniping(), EnemyMove.Wind("그림자 속으로 스며든다"), EnemyMove.Attack("일점혈", 3.5f) } },
            // Stage 30: 화룡멸겁대진 (16권)
            new EnemyData { Id = "ma_chilgeol_jeoji", Name = "마천칠걸 저지조", Level = 65, HpMul = 3.459f, AtkMul = 2.145f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("연환 공격", 0.7f, 3), EnemyMove.Attack("협공", 1.3f) } },
            new EnemyData { Id = "hwaryong_hwayeom", Name = "화룡의 불꽃", Level = 66, HpMul = 4.257f, AtkMul = 2.145f,
                Moves = new[] { EnemyMove.Attack("불의 성벽", 0.8f, 2).FromDistance(), EnemyMove.Wind("불길이 솟구친다"), EnemyMove.Attack("화룡의 숨결", 2.9f).FromDistance() } },
            new EnemyData { Id = "bi_daegongja", Name = "대공자 비", Level = 67, IsBoss = true, RetreatHpPct = 0.3f, HpMul = 7.492f, AtkMul = 2.593f, DefMul = 1.15f, SpeedAdd = 14,
                Moves = new[] { EnemyMove.Attack("영뢰 잔흔", 0.7f, 3), EnemyMove.Attack("검기 난무", 1.3f), EnemyMove.Wind("영뢰를 모은다"), EnemyMove.Attack("영뢰 천잔", 3.6f) } },
            // Stage 29 trials: 검후의 십초지적, 수화관 (16권)
            new EnemyData { Id = "geomhu_ioksang", Name = "검후 이옥상", Level = 66, HpMul = 45.426f, AtkMul = 4.804f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("체대", 0.8f, 2), EnemyMove.Attack("체대 일격", 1.3f), EnemyMove.Wind("검집에 손을 얹는다"), EnemyMove.Attack("해상비조천참절", 2.8f) } },
            new EnemyData { Id = "heuksaek_amhyeok", Name = "검은 암벽", Level = 63, HpMul = 4.103f, AtkMul = 0.400f,
                Moves = new[] { EnemyMove.Attack("진동", 0.5f) } },
            new EnemyData { Id = "wiji_hyeolpok", Name = "혈폭환 위지천", Level = 64, HpMul = 4.103f, AtkMul = 2.668f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("폭주 진기", 0.7f, 3), EnemyMove.Attack("혈폭 난사", 1.3f), EnemyMove.Wind("혈폭환의 기운이 끓어오른다"), EnemyMove.Attack("생명을 불사른 일격", 3.0f) } },
            // Stage 29 trial: 은가면 삼성의 습격 (16권). Unkillable; the party only has to endure.
            new EnemyData { Id = "mask_gongpo", Name = "은가면 「공포」", Level = 66, HpMul = 45.426f, AtkMul = 2.133f,
                Moves = new[] { EnemyMove.Attack("분신 일검", 0.8f, 2), EnemyMove.Attack("백발의 일검", 1.3f), EnemyMove.Wind("분신이 하나둘 늘어난다"), EnemyMove.Attack("공포의 일검", 2.8f) } },
            new EnemyData { Id = "mask_jeolmang", Name = "은가면 「절망」", Level = 66, HpMul = 45.426f, AtkMul = 2.133f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("나선장", 0.8f, 2), EnemyMove.Attack("나선 돌풍", 1.2f), EnemyMove.Wind("나선의 힘을 모은다"), EnemyMove.Attack("옷을 찢는 나선", 2.6f) } },
            new EnemyData { Id = "mask_bitan", Name = "은가면 「비탄」", Level = 66, HpMul = 45.426f, AtkMul = 2.133f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("허상 일검", 0.7f, 3), EnemyMove.Attack("나뭇가지 일검", 1.3f), EnemyMove.Wind("심정청도의 검기를 모은다"), EnemyMove.Attack("비탄의 일검", 2.8f) } },
            // Stage 28 trial: 목요관 야간 질주 (15권)
            new EnemyData { Id = "jongri_chu_night", Name = "칠련창 종리추", Level = 59, HpMul = 2.779f, AtkMul = 2.951f,
                Moves = new[] { EnemyMove.Attack("칠성창", 0.7f, 3), EnemyMove.Attack("일곱 창 투척", 1.2f).FromDistance() } },
            new EnemyData { Id = "do_chuun_night", Name = "사갈검편 도추운", Level = 59, HpMul = 2.617f, AtkMul = 2.951f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("검편 채찍", 0.7f, 3), EnemyMove.Attack("사갈편", 1.2f) } },
            // Balance clones: the same enemy tuned separately for a later stage (artwork is shared via ArtId)
            new EnemyData { Id = "eonga_yeong_b", ArtId = "eonga_yeong", Name = "파풍창 언가영", Level = 30, HpMul = 2.660f, AtkMul = 4.117f,
                Moves = new[] { EnemyMove.Attack("언가묵창", 1.1f), EnemyMove.Wind("창을 거둔다"), EnemyMove.Attack("파풍창", 2.6f) } },
            new EnemyData { Id = "hyeolma_dae_b", ArtId = "hyeolma_dae", Name = "십이혈마대 대원", Level = 49, HpMul = 3.188f, AtkMul = 2.575f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("혈검", 0.8f, 2), EnemyMove.Attack("암습", 1.3f).Sniping() } },
            new EnemyData { Id = "oh_munchu_b", ArtId = "oh_munchu", Name = "쇄풍겸 오문추", Level = 60, HpMul = 3.880f, AtkMul = 4.429f,
                Moves = new[] { EnemyMove.Attack("사슬낫", 0.8f, 2), EnemyMove.Wind("사슬을 휘감는다"), EnemyMove.Attack("쇄풍 난무", 2.8f) } },
            new EnemyData { Id = "baek_sayeong_b", ArtId = "baek_sayeong", Name = "사교검 백사영", Level = 60, HpMul = 3.880f, AtkMul = 4.429f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("사교검", 0.7f, 3), EnemyMove.Wind("뱀처럼 몸을 낮춘다"), EnemyMove.Attack("백사교룡살", 2.9f) } },
            // Stages 31-32: 청룡은장의 밤, 승천무제 시험관 (17-18권). The 살룡대 fights, the 황금 완장 bout and the training are 게임 연출 (in the novel 비류연 is not there / 남궁상 fights alone).
            new EnemyData { Id = "salryong_daewon", Name = "살룡대 복면 무사", Level = 66, HpMul = 2.100f, AtkMul = 3.400f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("복면 도격", 0.8f, 2), EnemyMove.Attack("협살", 1.3f) } },
            new EnemyData { Id = "cham_yeong", Name = "살룡대 참영", Level = 66, HpMul = 2.500f, AtkMul = 4.000f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("그림자 베기", 0.7f, 3), EnemyMove.Wind("그림자를 겹친다"), EnemyMove.Attack("참영도", 2.8f) } },
            new EnemyData { Id = "gyeok_un", Name = "살룡대 격운", Level = 66, HpMul = 2.500f, AtkMul = 4.000f,
                Moves = new[] { EnemyMove.Attack("격운장", 0.8f, 2), EnemyMove.Wind("구름 같은 장력을 모은다"), EnemyMove.Attack("격운파", 2.7f) } },
            new EnemyData { Id = "hyeol_pyo", Name = "살룡대 수석조장 혈표", Level = 67, IsBoss = true, HpMul = 3.500f, AtkMul = 3.300f, SpeedAdd = 8,
                Moves = new[] { EnemyMove.Attack("혈조", 0.7f, 3), EnemyMove.Attack("목 꺾기", 1.3f), EnemyMove.Wind("살기를 끌어올린다"), EnemyMove.Attack("혈표 일섬", 3.4f) } },
            new EnemyData { Id = "yu_ran", Name = "아미 제자 유란", Level = 66, HpMul = 2.000f, AtkMul = 2.335f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("연화검법", 0.7f, 3), EnemyMove.Attack("연화 일섬", 1.3f) } },
            new EnemyData { Id = "yu_unbi", Name = "점창 유운비", Level = 66, HpMul = 2.000f, AtkMul = 2.335f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("사일검법", 0.7f, 3), EnemyMove.Wind("찌르기를 노린다"), EnemyMove.Attack("사일 찌르기", 2.6f) } },
            new EnemyData { Id = "gwan_cheolsu_eogeom", ArtId = "gwan_cheolsu", Name = "빙검 관철수 (특훈)", Level = 67, IsBoss = true, HpMul = 45.426f, AtkMul = 4.600f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("이기어검", 0.8f, 2).FromDistance(), EnemyMove.Attack("어검분영술", 1.3f).FromDistance(), EnemyMove.Wind("검을 허공에 띄운다"), EnemyMove.Attack("비검 일섬", 2.8f).FromDistance() } },
            // Stage 33: 무극검과 누명 (19권). 이시건 is only endured (in the novel he apologizes and leaves); the patrol brawl and the 청흔 duel are fought by the party (in the novel 비류연 alone).
            new EnemyData { Id = "i_sigeon", Name = "마천각 이공자 이시건", Level = 69, HpMul = 45.426f, AtkMul = 4.500f, SpeedAdd = 8,
                Moves = new[] { EnemyMove.Attack("손가락 검기", 0.8f, 2).FromDistance(), EnemyMove.Attack("자운", 1.3f), EnemyMove.Wind("자운암풍이 소용돌이친다"), EnemyMove.Attack("광풍난주", 2.8f) } },
            new EnemyData { Id = "ipgwan_dojeonja", Name = "입관 희망자", Level = 68, HpMul = 2.100f, AtkMul = 3.700f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("명문 검법", 0.8f, 2), EnemyMove.Attack("완장 노리기", 1.3f) } },
            new EnemyData { Id = "chimbaetneun_ja", Name = "침 뱉은 도전자", Level = 68, HpMul = 2.500f, AtkMul = 4.250f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("시비", 0.7f, 3), EnemyMove.Wind("침을 뱉는다"), EnemyMove.Attack("악다구니 일격", 2.7f) } },
            // Stages 34-36: 20권. 비류연 is in prison for most of the volume, so these fights are 게임 연출 (the story text says who fought in the novel).
            new EnemyData { Id = "jungwon_pyosa", Name = "중원표국 표사", Level = 70, HpMul = 2.100f, AtkMul = 3.400f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("표창", 0.8f, 2).FromDistance(), EnemyMove.Attack("박도 내려치기", 1.3f) } },
            new EnemyData { Id = "o_yeong", Name = "오가 형제 오영", Level = 70, HpMul = 2.500f, AtkMul = 3.700f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("단도술", 0.7f, 3), EnemyMove.Wind("단도를 거꾸로 쥔다"), EnemyMove.Attack("쌍단도 교차", 2.7f) } },
            new EnemyData { Id = "o_gi", Name = "오가 형제 오기", Level = 70, HpMul = 2.500f, AtkMul = 3.700f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("단도술", 0.7f, 3), EnemyMove.Wind("형과 등을 맞댄다"), EnemyMove.Attack("쌍단도 교차", 2.7f) } },
            new EnemyData { Id = "yun_ijeong", Name = "풍마도 윤이정", Level = 70, HpMul = 3.000f, AtkMul = 3.700f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("풍마도법", 0.8f, 2), EnemyMove.Wind("삼현조화의 도막을 세운다"), EnemyMove.Attack("풍마귀혼", 2.8f) } },
            new EnemyData { Id = "i_sigeon_jungwon", ArtId = "i_sigeon", Name = "마천각 이공자 이시건", Level = 71, IsBoss = true, RetreatHpPct = 0.3f, HpMul = 6.500f, AtkMul = 3.500f, SpeedAdd = 10,
                Moves = new[] { EnemyMove.Attack("주박인", 0.7f, 3), EnemyMove.Attack("사검 그물", 1.3f), EnemyMove.Wind("열 근 철토시를 벗는다"), EnemyMove.Attack("삭풍참살인", 3.5f) } },
            new EnemyData { Id = "i_sigeon_bait", ArtId = "i_sigeon", Name = "자운 암풍 이시건", Level = 71, HpMul = 45.426f, AtkMul = 4.600f, SpeedAdd = 10,
                Moves = new[] { EnemyMove.Attack("질풍참살", 0.8f, 2), EnemyMove.Attack("사검 그물", 1.3f), EnemyMove.Wind("흑응의 기세를 모은다"), EnemyMove.Attack("흑응비섬", 2.8f) } },
            new EnemyData { Id = "sipsamhyeol", Name = "십삼혈 살인광", Level = 71, HpMul = 1.900f, AtkMul = 2.600f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("혈설십삼식", 0.8f, 2), EnemyMove.Attack("살인 취미", 1.3f) } },
            new EnemyData { Id = "il_hyeol", Name = "십삼혈의 우두머리 일혈", Level = 72, IsBoss = true, HpMul = 3.800f, AtkMul = 2.600f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("혈설십삼식", 0.7f, 3), EnemyMove.Attack("핏빛 손톱", 1.3f), EnemyMove.Wind("혈왕살을 끌어올린다"), EnemyMove.Attack("혈왕살", 3.5f) } },
            new EnemyData { Id = "geomjon_gongson", Name = "검존 공손일취", Level = 72, HpMul = 45.426f, AtkMul = 4.600f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("노검존의 일검", 0.8f, 2), EnemyMove.Attack("분노의 검", 1.3f), EnemyMove.Wind("손자의 이름에 검을 뽑는다"), EnemyMove.Attack("지존검법", 2.8f) } },
            new EnemyData { Id = "jin_soryeong", Name = "아미신녀 진소령", Level = 72, IsBoss = true, RetreatHpPct = 0.2f, HpMul = 7.500f, AtkMul = 5.200f, SpeedAdd = 8,
                Moves = new[] { EnemyMove.Attack("이분영", 0.8f, 2).FromDistance(), EnemyMove.Attack("사련화영", 0.6f, 4).FromDistance(), EnemyMove.Wind("옥현이 허공에 떠오른다"), EnemyMove.Attack("팔련화린 난화만천", 0.45f, 8).FromDistance() } },
            new EnemyData { Id = "cheong_heun_mugeuk", ArtId = "ha_sein", Name = "삼절검 청흔", Level = 70, IsBoss = true, HpMul = 7.161f, AtkMul = 4.250f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("삼환회선비검", 0.7f, 3), EnemyMove.Attack("태극검", 1.3f), EnemyMove.Wind("삼정태극검혜를 펼친다"), EnemyMove.Attack("무극검 일관", 3.5f) } },
        };

        // Items. Names come from volumes 1-3; effects and numbers are placeholders.
        private static readonly ItemData[] BaseItems =
        {
            new ItemData { Id = "bear_gall", Grade = ItemGrade.Fine, InGacha = true, Name = "웅담", Kind = ItemKind.Consumable, HealPct = 0.40f, Description = "곰의 쓸개. 가장 다친 동료의 체력을 40% 회복한다." },
            new ItemData { Id = "okruju", Grade = ItemGrade.Fine, InGacha = true, Name = "옥루주", Kind = ItemKind.Consumable, QiRestore = 60, SpiritGain = 25, Description = "사천제일주. 내공 60과 기세 25를 얻는다." },
            new ItemData { Id = "ginseng", Grade = ItemGrade.Epic, InGacha = true, Name = "인형설삼", Kind = ItemKind.Consumable, HealPct = 1.0f, QiRestore = 999, Description = "사람 모양의 영약 산삼. 가장 다친 동료를 완전히 회복하고 내공을 가득 채운다." },
            new ItemData { Id = InnerArts.StoneId, Grade = ItemGrade.Epic, Name = "심득석", Kind = ItemKind.Material, SellValue = 0, Description = "심법의 깨달음이 응어리진 돌. 무공 탭의 심법에서 심법 하나를 강화하는 데 쓴다. 팔 수 없다." },
            new ItemData { Id = "art_scroll_5", Grade = ItemGrade.Rare, Name = "무공 수련서 (소)", Kind = ItemKind.Consumable, MasteryUses = 5, Description = "초식을 정리한 얇은 수련서. 행낭에서 쓰면 배운 무공 하나의 숙련 +5회(필살기 +1회)." },
            new ItemData { Id = "art_scroll_10", Grade = ItemGrade.Epic, Name = "무공 수련서 (중)", Kind = ItemKind.Consumable, MasteryUses = 10, Description = "요결을 풀이한 수련서. 행낭에서 쓰면 배운 무공 하나의 숙련 +10회(필살기 +2회)." },
            new ItemData { Id = "art_scroll_20", Grade = ItemGrade.Legendary, Name = "무공 수련서 (대)", Kind = ItemKind.Consumable, MasteryUses = 20, Description = "고수의 깨달음이 담긴 비급. 행낭에서 쓰면 배운 무공 하나의 숙련 +20회(필살기 +4회)." },
            new ItemData { Id = "leather", Grade = ItemGrade.Normal, Name = "가죽", Kind = ItemKind.Material, SellValue = 105, Description = "사냥한 짐승의 가죽. 팔면 은자가 된다." },
            new ItemData { Id = "laundry_bat", Grade = ItemGrade.Fine, Name = "강철 빨래방망이", Kind = ItemKind.Weapon, AtkPct = 0.08f, Description = "수련용 강철 방망이(50근). 무기라기엔 빨래용이다. 공격 +8%." },
            new ItemData { Id = "jin_biroedo", Grade = ItemGrade.Transcendent, Name = "진비뢰도", Kind = ItemKind.Weapon, QiPct = 0.083f, DefPct = 0.774f, AtkPct = 2.04f, CritAdd = 0.51f, SpeedAdd = 34, HpPct = 0.51f, CritDmgAdd = 0.85f, Description = "봉뢰함에 든 진짜 비뢰도 열 자루. 전승의식을 마친 이만이 쥘 수 있다. 현재 5성·무강화 기준 체력 +78.46%, 내공 +12.77%, 공격 +313.85%, 방어 +119.08%, 속도 +52.31, 치명타 +58.85%, 치명타 피해 +130.77%." },
            new ItemData { Id = "mukryonghwan", Grade = ItemGrade.Rare, Name = "묵룡환", Kind = ItemKind.Accessory, AtkPct = 0.10f, DefPct = 0.10f, SpeedAdd = -6, Description = "사부가 11세부터 채운 증량 족쇄. 몸은 무거워지지만 단련된다. 공격·방어 +10%, 속도 -6." },
            new ItemData { Id = "roegeum", Grade = ItemGrade.Mythic, Name = "뇌금 묵뢰", Kind = ItemKind.Accessory, QiPct = 0.089f, AtkPct = 0.34f, CritAdd = 0.17f, HpPct = 0.17f, SpeedAdd = 10, Description = "뇌령사 현을 쓴 검은 칠현금. 비뢰의 신화 장신구로, 울릴 때마다 번개가 깃든다. 내공 +8.9%, 공격 +34%, 치명타 +17%, 체력 +17%, 속도 +10" },
            new ItemData { Id = "claw_necklace", Grade = ItemGrade.Fine, Name = "발톱 목걸이", Kind = ItemKind.Accessory, CritAdd = 0.05f, SpeedAdd = 2, Description = "사냥한 짐승의 발톱을 꿴 목걸이. 치명타 +5%, 속도 +2." },
            new ItemData { Id = "ilgeup_yeongdan", Grade = ItemGrade.Legendary, InGacha = true, Name = "일급영단", Kind = ItemKind.Consumable, HealPct = 1.0f, QiRestore = 999, SpiritGain = 100, Description = "삼성무제 우승 부상. 가장 다친 동료를 완전히 회복하고 내공을 채우며 기세를 100까지 끌어올린다." },
            new ItemData { Id = "seungryong_pae", Grade = ItemGrade.Rare, Name = "승룡패", Kind = ItemKind.Key, Description = "녹옥으로 된 추천패. 천무학관 특별전형 입관의 증표." },
            new ItemData { Id = "mukrin_talisman", Grade = ItemGrade.Rare, Name = "묵린혈망 독아 부적", Kind = ItemKind.Accessory, DefPct = 0.12f, HpPct = 0.05f, Description = "묵린혈망의 독아를 은세공한 초승달 장신구. 뱀이 다가오지 못한다. 방어 +12%, 체력 +5%." },
            // 객잔 shop goods (game design, not from the novel): sold by the factions at higher reputation, never dropped or pulled
            new ItemData { Id = "meal_academy", Grade = ItemGrade.Normal, Name = "학관 정식", Kind = ItemKind.Consumable, HealPct = 0.50f, QiRestore = 20, Description = "학관 식당의 든든한 한 끼. 가장 다친 동료의 체력을 50% 회복하고 내공 20을 얻는다." },
            new ItemData { Id = "biaoguk_ration", Grade = ItemGrade.Normal, Name = "표국 건량", Kind = ItemKind.Consumable, HealPct = 0.35f, Description = "표행길에 챙기는 마른 식량. 체력을 35% 회복한다." },
            new ItemData { Id = "taegeuk_pill", Grade = ItemGrade.Rare, Name = "태극환", Kind = ItemKind.Consumable, HealPct = 0.80f, QiRestore = 50, Description = "무당의 단약. 가장 다친 동료의 체력을 80% 회복하고 내공 50을 얻는다." },
            new ItemData { Id = "plum_elixir", Grade = ItemGrade.Rare, Name = "매화 영단", Kind = ItemKind.Consumable, HealPct = 0.60f, QiRestore = 80, SpiritGain = 30, Description = "화산의 영단. 체력 60% 회복, 내공 80, 기세 30을 얻는다." },
            new ItemData { Id = "antidote", Grade = ItemGrade.Fine, Name = "해독 영약", Kind = ItemKind.Consumable, Cleanse = true, Description = "독기와 열기, 한기와 내상을 씻어 내는 영약. 고른 동료의 상태 이상을 모두 푼다." },
            new ItemData { Id = "greenwood_liquor", Grade = ItemGrade.Fine, Name = "녹림 화주", Kind = ItemKind.Consumable, QiRestore = 100, SpiritGain = 60, Description = "독한 산채의 화주. 내공 100과 기세 60을 얻는다." },
            new ItemData { Id = "cheonyul_pae", Grade = ItemGrade.Epic, Name = "천율패", Kind = ItemKind.Key, Description = "화산규약지회 중토관의 신분 패. 조와 이름과 천율인이 새겨져 있다. 잃으면 탈락이다." },
        };

        /// <summary>Every item: the story items above plus the 뽑기 catalog.</summary>
        public static readonly ItemData[] Items = MergeItems();

        private static ItemData[] MergeItems()
        {
            var list = new List<ItemData>(BaseItems);
            list.AddRange(GachaItems.All);
            list.AddRange(GachaItemsMore.All);
            list.AddRange(GearCatalogExpansion.All);
            list.AddRange(Salvage.Materials);
            list.AddRange(Gear.Materials);
            return list.ToArray();
        }

        public static readonly DropData[] Drops =
        {
            new DropData { EnemyId = "tiger", ItemId = "leather", Chance = 0.60f },
            // 해독 영약 from the poison users
            new DropData { EnemyId = "sunyeo", ItemId = "antidote", Chance = 0.40f },
            new DropData { EnemyId = "dang_munceon", ItemId = "antidote", Chance = 0.40f },
            new DropData { EnemyId = "cave_viper", ItemId = "antidote", Chance = 0.25f },
            new DropData { EnemyId = "bisa_snake", ItemId = "antidote", Chance = 0.25f },
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
            new DropData { EnemyId = "hyeol_pyo", ItemId = "ginseng", Chance = 0.50f },
            new DropData { EnemyId = "cheong_heun_mugeuk", ItemId = "ilgeup_yeongdan", Chance = 0.20f },
            new DropData { EnemyId = "i_sigeon_jungwon", ItemId = "ginseng", Chance = 0.50f },
            new DropData { EnemyId = "il_hyeol", ItemId = "ginseng", Chance = 0.50f },
            new DropData { EnemyId = "jin_soryeong", ItemId = "ilgeup_yeongdan", Chance = 0.20f },
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
            { 31, new[] { new ItemStack("ginseng", 3), new ItemStack("bear_gall", 3) } },   // 청룡은장의 밤
            { 32, new[] { new ItemStack("ilgeup_yeongdan", 1), new ItemStack("okruju", 3) } },   // 승천무제 시험관
            { 33, new[] { new ItemStack("ilgeup_yeongdan", 2), new ItemStack("ginseng", 2) } },   // 무극검과 누명
            { 34, new[] { new ItemStack("ginseng", 3), new ItemStack("okruju", 3) } },   // 흰 봉황
            { 35, new[] { new ItemStack("ilgeup_yeongdan", 1), new ItemStack("ginseng", 3) } },   // 전설의 포효
            { 36, new[] { new ItemStack("ilgeup_yeongdan", 2), new ItemStack("ginseng", 3) } },   // 운명의 비무
        };

        /// <summary>What the protagonist starts with.</summary>
        public static readonly ItemStack[] StartKit =
        {
            new ItemStack("mukryonghwan", 1), new ItemStack("laundry_bat", 1), new ItemStack("cloth_robe", 1), new ItemStack("train_pants", 1), new ItemStack("straw_sandals", 1), new ItemStack("bear_gall", 2), new ItemStack("okruju", 1), new ItemStack(Gear.StoneId, 5),
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
                    "아미산의 숲은 비류연에게 수련장이자 사부 몰래 돈을 모으는 일터였다.",
                    "사부|장작과 빨래를 끝내거라. 손에 밴 일을 버리고 무공만 얻겠다는 생각은 말고.",
                    "비류연|알겠습니다. 나물도 캐 오죠. 조금 늦어도 찾지 마세요.",
                    "무거운 묵룡환을 찬 채 산을 오르내리던 날들. 호랑이 가죽과 웅담은 그가 따로 챙긴 부수입이었다.",
                },
                Outro = new[] {
                    "남궁상은 진령을 감싸다 등에 깊은 발톱 자국을 입었다. 비류연이 꾸민 만남은 뜻밖의 백호 습격으로 끝났다.",
                    "진령|저 때문에 다치셨잖아요. 움직이지 마세요.",
                    "남궁상|진 소저가 무사하면 됐소.",
                    "두 사람의 관계는 달라졌지만, 등에는 평생 지워지지 않을 흉터가 남았다. 수련원에는 아직 끝내야 할 합숙이 있었다.",
                },
                Encounters = new[] { new[] { "tiger" }, new[] { "bear" }, new[] { "tiger", "tiger" }, new[] { "white_tiger" } } },
            new StageData { Number = 2, RewardWeightBudget = 7, Name = "중양표국", RecommendedLevel = 4, DooTotal = 1400,
                Prologue = new[]
                {
                    new StoryScene { Title = "연장된 합숙", Lines = new[] {
                            "처음 약속된 합숙은 석 달이었다. 주작단은 첫날부터 손목과 발목에 묵환을 차고 식사 준비와 빨래, 장작 패기에 매달렸다.",
                            "노학이 반항하자 노사부는 삼복구타권법으로 답했다. 그 뒤로 수련원에서는 불평도 목소리를 낮춰야 했다.",
                            "수상립과 수상보, 물속에서 병기 휘두르기까지 이어졌다. 주작단은 끝을 보고 돌아가겠다며 학관에 편지를 보냈다.",
                            "예정된 석 달은 다섯 달로 늘어났다. 노사부의 정체를 모르는 제자들도 자신들이 달라지고 있음을 느꼈다.",
                        } },
                },
                Intro = new[] {
                    "그런 합숙 도중, 당철영이 중양표국 대표두 강장한 일행에게 집단구타를 당해 돌아왔다.",
                    "당철영|스승님… 제가 부족했습니다.",
                    "비류연|내 제자를 저 꼴로 만들고 그냥 넘어갈 셈이냐. 가자. 사과도, 치료비도 받아야겠다.",
                    "다친 당철영을 남겨 두고, 노사부와 주작단 열다섯 명이 중양표국으로 향했다.",
                },
                Outro = new[] {
                    "강장한과 등여운·등여호 형제를 넘어 국주 장우양까지 제압했다. 중양표국의 소란이 마침내 멎었다.",
                    "장우양|보상은 드리겠습니다. 제발 그만하십시오, 노사부님!",
                    "비류연|치료비를 빼먹으면 곤란하지. 제자 몸에 난 멍부터 세어 볼까?",
                    "보상금을 받아낸 뒤 합숙도 마무리에 접어들었다. 떠나기 전날 밤, 남궁상과 진령은 서로의 마음을 확인했다.",
                },
                Encounters = new[] { new[] { "biaosa" }, new[] { "biaosa", "biaosa" }, new[] { "gang_janghan" }, new[] { "deung_yeoun", "deung_yeoho" }, new[] { "jang_uyang" } } },
            new StageData { Number = 3, Name = "호골채 매복", RecommendedLevel = 7, DooTotal = 1600,
                Intro = new[] {
                    "노사부는 사천에서 주작단과 이별했다. 제자들의 귀향 여비를 아끼려고 붙여 준 일자리는 중양표국의 남창행 표행 호위였다.",
                    "남궁상|이제부터는 우리 힘으로 돌아간다. 표사와 표물부터 지켜라.",
                    "호골채 도적|여기는 호골채의 길이다! 표물을 내놓아라!",
                    "사천 경계 산길에 호골채 오백 명이 매복해 있었다. 이야기는 주작단이 돌아가는 길을 따라간다.",
                },
                Outro = new[] {
                    "화산 출신 조천우가 호골채 두목 패호 중광을 제압하고, 주작단이 산적들을 물리쳤다.",
                    "표사|우리 쪽에는 사상자가 없습니다! 소협들 덕분입니다.",
                    "노학|그런데 극비 표물을 어찌 알고 기다렸을까요. 단서가 없으니 답답하군요.",
                    "주작단은 표행을 정비해 다시 길을 나섰다. 노사부는 이 자리에 없었다.",
                },
                Encounters = new[] { new[] { "hogol_bandit", "hogol_bandit" }, new[] { "hogol_bandit", "hogol_bandit", "hogol_bandit" }, new[] { "paeho_jung_gwang", "hogol_bandit" } } },
            new StageData { Number = 4, Name = "용골채 매복", RecommendedLevel = 10, DooTotal = 2300,
                Intro = new[] {
                    "주작단이 호위하는 표행 앞에 이번에는 용골채 천 명이 길을 막았다. 노사부 없이 치러야 할 두 번째 매복이었다.",
                    "패룡 우악|고려청자만 내놓으면 길을 열어 주마!",
                    "남궁상|표사들에게 붙지 못하게 막아라. 지난번처럼 각자 맡은 자리를 지켜!",
                    "당철영|지난번 맞은 값은 받아 왔습니다. 이번에는 제가 먼저 보답하죠.",
                },
                Outro = new[] {
                    "당철영이 던진 돌멩이에 용골채주 패룡 우악이 쓰러졌다. 두 번의 매복을 넘기도록 표사 사상자는 한 명도 없었다.",
                    "표물은 성 하나와 맞먹는다는 고려청자 세 점이었다. 산적들이 그 비밀을 안 경위는 끝내 밝혀지지 않았다.",
                    "사천을 떠난 지 일곱 달 만에 주작단이 천무학관 정문으로 돌아왔다. 피로 물든 옷이 그동안의 일을 대신 말해 주었다.",
                    "그들이 돌아오기 전, 비류연은 이미 사천의 모옥에서 사부의 점심상을 차리고 있었다.",
                },
                Encounters = new[] { new[] { "yonggol_bandit", "yonggol_bandit" }, new[] { "yonggol_bandit", "yonggol_bandit", "yonggol_bandit" }, new[] { "paeryong_wooak", "yonggol_bandit" } } },
            new StageData { Number = 5, Name = "주주 윤하루", RecommendedLevel = 13, DooTotal = 4700,
                Prologue = new[]
                {
                    new StoryScene { Title = "주작단이 떠난 이튿날", Lines = new[] {
                            "시간을 잠시 되돌린다. 주작단을 표행에 보낸 이튿날, 아미산 모옥에서 사부와 제자가 점심상을 마주했다.",
                            "사부|그동안 밖에서 무슨 짓을 하고 다녔느냐. 그 주머니는 또 뭐고?",
                            "비류연|사례를 좀 받았을 뿐이죠. 옥루주도 준비했습니다, 사부님.",
                            "비류연은 합숙과 보상금 이야기를 술병 뒤로 감췄다. 제자들이 남창에 도착하기까지는 아직 한 달 반가량 남아 있었다.",
                        } },
                    new StoryScene { Title = "스무 살의 가출", Lines = new[] {
                            "겨울 계곡의 소용돌이 수련에서도 마지막 힘에는 닿지 못했다. 사부가 말한 뇌신의 힘은 아직 멀었다.",
                            "비류연|(그렇다고 평생 이 산에서 사부님 수발만 들 수는 없지. 제자 녀석들은 잘 지내려나.)",
                            "새해 원단, 스무 살이 된 비류연은 편지와 사부 명의의 노후 연금을 남기고 모옥을 떠났다.",
                            "짐에는 사부 몰래 가져온 봉뢰함과 뇌금 묵뢰가 있었다. 봉뢰함에는 진비뢰도 열 자루가 들어 있었다.",
                            "계승의식은 끝났지만 전승의식은 미완이었다. 이전까지 쓰던 비도는 연습용이었다.",
                            "이번에는 맨얼굴로 중양표국을 찾아가 노사부 명의의 서찰을 내밀었다. 장우양은 그가 노사부 본인인 줄 꿈에도 몰랐다.",
                        } },
                },
                Intro = new[] {
                    "새로운 남창행 표행이 주주의 객잔 윤하루에 묵었다. 이번에는 주작단이 아니라 맨얼굴의 비류연이 동행했다.",
                    "장우강|아버님이 받아 주셨다고 네가 대단한 표사라도 된 줄 아느냐?",
                    "비류연|소국주님은 말씀이 많으시네요. 호위 일보다 그쪽이 더 바쁘신가 봐요.",
                    "오만한 소국주와 낯선 호위 표사 사이의 기싸움이 팽팽해졌다.",
                },
                Outro = new[] {
                    "비류연은 염도의 공격을 피해 묵룡환 하나를 벗었다. 여반장 한 번에 승부가 났다.",
                    "비류연|내기에서 졌으면 약속은 지켜야죠. 이제 제자답게 짐부터 드세요.",
                    "염도|이 몸을 제자로 부려 먹겠다니…!",
                    "사천제일 도객은 뇌금 묵뢰를 짊어지고 비류연을 따라나섰다. 누가 스승인지 남들에게 설명하기는 곤란한 동행이었다.",
                },
                Encounters = new[] { new[] { "jang_ugang" }, new[] { "yeomdo" } } },
            new StageData { Number = 6, Name = "남창 호아장", RecommendedLevel = 16, DooTotal = 3000,
                Prologue = new[]
                {
                    new StoryScene { Title = "마감된 입관시험", Lines = new[] {
                            "남창에 도착했지만 승천무제 접수는 한 달 전에 끝나 있었다. 응시 연령도 만 스물다섯까지였다.",
                            "비류연은 순풍당의 정보상 나중해에게 다른 방법을 캐물었다.",
                            "나중해|염도 대협은 학관에서 초빙하는 무사부로, 소협은 승룡패를 가진 추천자로 특별전형을 치르면 되오.",
                            "비류연|그 추천패는 어디서 구하죠?",
                            "나중해|호아장에 있소. 그 뒤를 봐주는 분이 있다는 것도 알아 두시오.",
                        } },
                },
                Intro = new[] {
                    "호아장의 장주 호천상에게는 천무학관 특별전형 추천패인 승룡패가 있었다.",
                    "비류연|승룡패를 좀 빌리러 왔습니다. 얘기부터 들어 주시죠.",
                    "호천상|남의 문중에 들이닥쳐 내놓으라니, 그게 부탁하는 태도냐!",
                    "대화가 틀어지자 호아장 제자들이 길을 막았다. 염도가 든 홍염도 어딘가에 응하듯 울기 시작했다.",
                },
                Outro = new[] {
                    "호아장의 후견인 빙검 관철수가 모습을 드러냈다. 홍염이 울었던 까닭은 그의 검 빙루였다.",
                    "빙검|염도, 네가 여기는 웬일이냐.",
                    "염도|네놈이야말로… 오래간만이군.",
                    "동문 사형제가 일검을 교환했다. 빙검이 나선 뒤 호아장은 끝내 승룡패를 넘겼다.",
                    "비류연은 추천패를 챙겼다. 두 고수 사이의 오래된 감정까지 풀린 것은 아니었다.",
                },
                Encounters = new[] { new[] { "hoajang_disciple", "hoajang_disciple" }, new[] { "hoajang_disciple", "hoajang_disciple", "hoajang_disciple" }, new[] { "ho_cheonsang", "hoajang_disciple" } } },
            new StageData { Number = 7, RewardWeightBudget = 8, Name = "특별전형과 검혼관", RecommendedLevel = 19, DooTotal = 3500,
                Intro = new[] {
                    "염도는 무사부로 영입되었다. 승룡패를 내민 비류연에게는 특별전형이 남아 있었다.",
                    "응시생|추천패 한 장으로 우리 앞을 지나가겠다는 거냐!",
                    "네 응시생과의 승부를 넘어야 학관에 들어갈 수 있다. 합격 뒤에는 검혼관의 첫날이 기다린다.",
                },
                Outro = new[] {
                    "기관이 삼 단계까지 작동해 검림이 솟았다. 비류연은 봉황무로 빠져나와 호천강의 패왕노호검을 반 토막 냈다.",
                    "사감 강하윤은 은자 천오백 냥의 피해를 따졌다. 정당방위가 인정된 비류연 일행은 벌을 면했다.",
                    "강하윤|기관 작동법은 누구에게 배웠느냐, 호천강? 그 둘이 누구냐!",
                    "호천강은 끝내 배후를 말하지 않았다.",
                    "칠백일 호에 배정된 비류연의 룸메이트는 모용휘였다. 효룡·장홍과 윤준호까지 모이자 일행은 천무식당으로 향했다.",
                },
                Encounters = new[] { new[] { "admit_jeomchang" }, new[] { "admit_hwasan" }, new[] { "admit_geumbok" }, new[] { "admit_eonga" }, new[] { "trap_arrow" }, new[] { "trap_needle", "trap_arrow" }, new[] { "trap_log", "trap_needle" }, new[] { "ho_cheongang" } } },
            new StageData { Number = 8, Name = "천무식당", RecommendedLevel = 21, DooTotal = 3500,
                Intro = new[] {
                    "천무식당의 메뉴는 천무정식 하나뿐이었다. 식사를 하려던 일행 앞에서 종리학과 추가연이 윤준호를 조롱했다.",
                    "종리학|화산 제자라는 놈이 매화검법도 제대로 못 쓰느냐?",
                    "장홍|동문을 그런 식으로 몰아세우는 것이 선배의 도리입니까?",
                    "비류연은 식판 모서리를 쥐었다. 검이 오간다면 어디가 먼저 열릴지, 날 선 기세 속의 공방이 머릿속에 그려졌다.",
                },
                Outro = new[] {
                    "비류연이 식판을 휘두르기 직전, 모용휘가 나섰다. 종리학과 추가연은 그의 모란 문양을 보고 물러났다.",
                    "그 뒤 들은 윤준호의 사연은 단순한 재주 부족과는 달랐다. 칠매동에서 얻은 칠매검을 익히다 스스로 만들어 낸 매화 향기에 쓰러진 것이었다.",
                    "효룡|검기검화 너머, 검향의 경지에 닿은 겁니다. 흔한 일이 아니에요.",
                    "윤준호|그 뒤로는 매화검법도 쓸 수 없어요. 몸을 약으로 바꾸는 건 싫고요.",
                    "비류연|그럼 알레르기부터 고치려 하지 마. 검향을 넘어서는 이미지를 그려 봐.",
                    "장홍|못 배워서 막힌 게 아니라 너무 멀리 나아가서 막혔다는 것이군요.",
                    "윤준호의 수련은 이제 시작이었다. 이날의 처방으로 검법을 곧장 되찾은 것은 아니었다.",
                },
                Encounters = new[] { new[] { "jongri_hak" }, new[] { "jongri_hak", "jongri_hak" }, new[] { "chu_gayeon", "jongri_hak" } } },
            new StageData { Number = 9, Name = "천무학관 운향정", RecommendedLevel = 23, DooTotal = 4000,
                Prologue = new[]
                {
                    new StoryScene { Title = "수강신청과 우뢰", Lines = new[] {
                            "수강편람은 베개만큼 두꺼웠다. 필수과목을 고르고도 전공을 정해야 했다.",
                            "비류연|사문에서 가져온 묵금이 있으니 음공을 들어야겠어. 제자에게 도를 배우러 갈 수는 없잖아.",
                            "효룡도 음공을 함께 신청했다. 금 과정의 서평 노사와 음공 입문의 천음선자 홍란은 서로 다른 수업을 맡았다.",
                            "천응방 분타에서는 서른 명 넘게 길들이지 못했다는 푸른 해동청이 비류연의 팔에 순순히 앉았다.",
                            "비류연은 열한 냥 오십 문 오십 푼으로 값을 깎아 매를 사고 우뢰라는 이름을 붙였다.",
                        } },
                    new StoryScene { Title = "처음 듣는 백 년사", Lines = new[] {
                            "무도총론 첫 시간, 옥현진인은 백 년 전의 천겁혈세를 이야기했다.",
                            "옥현진인|천겁혈신 위천무는 흑백을 가리지 않았다. 무림맹과 흑천맹이 연합하고도 패했지.",
                            "옥현진인|혁월린과 갈중혁이 끝까지 맞섰다. 혈신이 종적을 감춘 뒤, 천무학관과 마천각이 세워졌다.",
                            "옥현진인|화산규약지회는 두 곳이 다시 싸우지 않도록 마련한 약속이다. 처음 듣는다는 말은 아니겠지?",
                            "비류연|천겁령도 위천무도 처음 듣는데요. 저희 사문 역사에는 그런 이야기가 없었어요.",
                            "옥현진인|천무삼성과 그 뒤 백 년사를 조사해 오너라.",
                            "태극신군이라는 호칭은 어딘가 낯익었지만 비류연은 그 까닭을 깊이 따지지 않았다.",
                        } },
                },
                Intro = new[] {
                    "수업 밖에서도 해야 할 일이 있었다. 모든 관도는 동호회에 한 곳 이상 가입해야 했다.",
                    "장홍|검혼회와 도백회가 서로 신입을 끌어가려고 야단이더군요.",
                    "비류연|회비만 비싸지 않으면 되지. 일단 가 보자.",
                    "그날 동호회 거리에서 본 기세의 대치가, 운향정에서 벌어질 진짜 싸움보다 먼저였다.",
                },
                Outro = new[] {
                    "위지천이 천원삼절검격을 연달아 펼쳤다. 비류연은 봉황무의 삼첩영으로 피하고 선풍뇌절을 날렸다.",
                    "학관에 들어온 뒤 처음 드러낸 비뢰도가 위지천의 오른손을 다치게 하고 검을 쳐냈다.",
                    "비류연|(이름을 묻지 못했네. 그 소저를 다시 볼 수 있을까.)",
                    "같은 시각 비영각의 보고를 받은 백무영은 호아장 사건과 비류연의 관계를 확인하고, 추일태에게 제재를 맡겼다.",
                    "이후 학관 정문에는 석 달 과제를 마친 청룡단과 주작단이 반 각 차로 돌아왔다. 사천 합숙 뒤의 첫 귀환과는 다른 길에서 돌아온 참이었다.",
                    "비류연|(제자 녀석들, 돌아왔구나. 정신교육부터 다시 시켜야겠네.)",
                },
                Encounters = new[] { new[] { "gu_chang" }, new[] { "so_cheongun" }, new[] { "gu_chang", "so_cheongun" }, new[] { "wiji_cheon" } } },
            new StageData { Number = 10, Name = "백향관 침투", RecommendedLevel = 25, DooTotal = 4200,
                Prologue = new[]
                {
                    new StoryScene { Title = "빙백봉의 정체", Lines = new[] {
                            "운향정 사건 이후 일주일. 위지천이 빙봉영화수호대를 소집해 나예린의 초상화를 내리고 비류연의 수배 초상화를 걸어 검기로 찢었다. 모두의 공적이 된 것이다.",
                            "비류연은 애소저회 비연태에게서 소녀의 정체를 알아냈다.",
                            "비연태|천하제일미 빙백봉 나예린. 무림맹주 나백천의 딸이자 검후 이옥상의 의발전인이지.",
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
                            "나예린|다음에는 같은 실수를 하지 않겠어요.",
                            "용안으로도 읽을 수 없었던 낯선 남자의 행동이 마음에 남아 있었다.",
                            "같은 날 이진설이 새 기술 쌍검이연십이참격을 독고령에게 시험하려 했지만 나예린이 대신 나섰다. 스물네 줄기 검기가 모두 빗나가고 나예린의 검끝이 이진설의 목뒤에 닿았다.",
                            "나예린|큰 기술은 허점도 커요.",
                        } },
                    new StoryScene { Title = "추일태의 내기", Lines = new[] {
                            "구정회 상부의 명을 받은 일은무영 추일태가 비류연에게 은신 내기를 걸었다. 금남의 성지, 여성 전용 기숙사 백향관에 잠입해 수놓은 옷을 가져오는 것이다. 비연태가 증인을 맡았다.",
                            "추일태|내기 돈은 은자 이백 냥이다. 해볼 텐가?",
                            "비류연|돈이 적게 걸린 내기는 안 한다고 했잖아요. 이백 냥이면 하죠. 목표는요?",
                            "추일태|네 목표는 301호 나예린의 옷이다. 나는 독고령의 것을 가져오마.",
                        } },
                },
                Intro = new[] {
                    "달이 숨은 밤, 비류연은 복면을 쓰고 백향관으로 향했다. 난화백무진과 절독난향십이선녀의 경계를 넘어야 했다.",
                    "비류연|(추일태는 독고령, 나는 나예린의 옷. 은자 이백 냥짜리 내기라지만 만만하지 않겠네.)",
                    "그는 봉황무로 그림자를 탔다. 동료들을 망꾼으로 세우지 않은 단독 잠입이었다.",
                },
                Outro = new[] {
                    "비류연은 쓰러진 진령에게 감아 둔 뇌령사를 움직였다. 괴뢰처럼 일어난 진령이 나예린을 붙잡는 틈에 분뢰수로 검기를 뚫고 빠져나갔다.",
                    "백 장 밖에서 뇌령사를 당기자 목표물이 손에 들어왔다. 그러나 검후처에 침입한 또 다른 그림자의 목적은 내기와 달랐다.",
                    "같은 밤 검혼관 옆 숲. 무흔일호가 효룡 앞에 무릎을 꿇었다.",
                    "무흔일호|실패했습니다, 삼공자님.",
                    "효룡은 마천각 삼공자였다. 형인 일공자의 계획과 달리, 훔친 무공으로 승리를 얻는 일에는 반대했다.",
                    "효룡|감시가 있을 겁니다. 당분간 연락을 끊으세요.",
                    "두 침입 사건은 겹쳐 보였지만, 비류연과 무흔일호가 함께 움직인 것은 아니었다.",
                },
                Encounters = new[] { new[] { "sunyeo" }, new[] { "sunyeo", "sunyeo" }, new[] { "na_yerin" } } },
            new StageData { Number = 11, RewardWeightBudget = 3, Name = "승급시험", RecommendedLevel = 27, DooTotal = 4600,
                Prologue = new[]
                {
                    new StoryScene { Title = "삼성제 약속", Lines = new[] {
                            "다음 날 나예린이 애소저회를 직접 찾아와 비류연의 목에 검을 겨눴다.",
                            "나예린|어제 백향관에 침입한 사람이 소협인가요?",
                            "비류연|글쎄요.",
                            "나예린|올해 삼성무제에서 우승해 보세요. 그러면 당신의 말을 믿어드리죠.",
                            "비류연|(삼성제가 뭔지도 모른 채) 우승하면 저를 다른 눈으로 봐 주세요.",
                            "비류연은 자신이 약속한 삼성무제가 어떤 대회인지 뒤늦게 알아보기 시작했다.",
                        } },
                    new StoryScene { Title = "대사형의 귀환", Lines = new[] {
                            "비류연은 염도를 찾아가 '주작단은 내 제자니 네 사제'라고 폭탄선언을 했다. 다섯 달 동안 인피면구를 쓴 가짜 노사부였다는 사실은 주작단에게 끝까지 비밀이다. 그래서 '만난 적 없는 대사형'으로 나서기로 했다.",
                            "다음 날 연무장. 인피면구 없이 나타난 비류연이 외쳤다.",
                            "비류연|내가 너희들의 대사형이다!",
                            "노학|뭐? 이런 애송이가 무슨… 으아악!",
                            "달려든 노학은 삼복구타권법에 날아갔고, 묵룡환을 본 주작단은 일제히 포권했다.",
                            "염도가 능청스럽게 보증을 섰다. 주작단은 눈앞의 대사형이 합숙 때의 노사부인 줄 끝내 몰랐다.",
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
                    "승급시험 당일, 곤륜쌍검이 모용휘를 도발했다. 먼저 두 검의 합격을 넘어야 했다.",
                    "이후 모용휘의 정식 상대 오문형과 장대평, 비류연의 상대 단대풍 순으로 승부가 이어진다.",
                    "동료 중심 사건의 전투는 현재 육성한 편성 파티로 재현합니다. 이야기 속 승급 당사자는 원작대로 유지됩니다.",
                },
                Outro = new[] {
                    "정식 승급시험. 단대풍은 십여 차례 검을 내리쳤지만 비류연 앞의 보이지 않는 막을 넘지 못했다. 이어진 일권에 단대풍이 쓰러졌다.",
                    "그 광경을 본 강현추는 싸우지 않고 기권했다. 두 번째 승부는 열리지 않았고, 비류연의 육검룡 승급이 확정되었다.",
                    "비류연은 육검룡이 되었다. 모용휘도 곤륜쌍검의 시비를 물리친 뒤 오문형·장대평과의 정식 시험을 거쳐 오검룡에 올랐다.",
                    "군웅팔가회는 모용휘를 영입하려 했고, 구정회는 그를 견제했다. 삼성무제를 앞두고 두 세력의 신경전이 깊어졌다.",
                },
                Encounters = new[] { new[] { "no_jusaeng", "chae_hagun" }, new[] { "oh_muhyeong" }, new[] { "jang_daepyeong" }, new[] { "dan_daepung" } } },
            new StageData { Number = 12, RewardWeightBudget = 6, Name = "삼성무제 본선", RecommendedLevel = 29, DooTotal = 5200,
                Prologue = new[]
                {
                    new StoryScene { Title = "삼성무제 개막", Lines = new[] {
                            "제30회 천무삼성무제 개막식. 검성 모용정천, 도성 하후식, 검후 이옥상이 참관석에 앉았다. 관주 마진가가 대회를 선언하고 삼절검 청흔이 관도 대표로 선서했다.",
                            "마진가|삼성무제는 반년에 걸쳐 열린다. 검성전, 도성전, 검후전, 삼성대전의 결승 여덟 명은 마지막에 몰아서 싸운다.",
                            "마진가|우승자에게는 일급영단과 전대 고수의 무공 심득이 주어진다.",
                            "비류연이 등에 멘 것은 뇌금 묵뢰. 음공을 배운 지 반년이다.",
                            "장홍|(눈을 감으며) … 불안합니다.",
                        } },
                },
                Intro = new[] {
                    "우뢰가 전한 초대를 받아 나예린이 독고령과 함께 관중석에 앉았다. 약속을 지켜보는 동시에 백향관 침입자의 실력을 확인하려는 관전이었다.",
                    "비류연|나 소저, 올 줄 알았어요!",
                    "첫 상대는 빙봉수호대 소속 해남파 쾌환검 단평. 묵금을 든 신입에게 야유가 쏟아졌다.",
                },
                Outro = new[] {
                    "효룡|전옥기가 송풍루에서 제 앞에서 비류연 님을 험담한 값은 이걸로 치르신 셈이군요.",
                    "비류연은 뇌령사 현으로 전옥기의 콧등을 베고, '충격요법'으로 이빨까지 부러뜨려 장외로 날려 보냈다.",
                    "비류연은 연승을 이어 간다. 아무도 이 1학년의 음공을 본 적이 없다.",
                },
                Encounters = new[] { new[] { "dan_pyeong" }, new[] { "jo_yeonil" }, new[] { "jin_pae" }, new[] { "ilyeon" }, new[] { "eonga_yeong" }, new[] { "jeon_okgi" } } },
            new StageData { Number = 13, RewardWeightBudget = 4, Name = "삼성대전 결승", RecommendedLevel = 31, DooTotal = 6000,
                Prologue = new[]
                {
                    new StoryScene { Title = "안목품평회", Lines = new[] {
                            "마감 직전, 주작단은 한 달 생활비를 가죽 주머니에 담아 금영호에게 맡겼다. 전부 비류연에게 건다.",
                            "염도|무조건 이겨야 해. 빙검, 그 얼음땡이의 제자들보다 잘해야 한다.",
                            "염도도 자기 돈을 던졌다.",
                        } },
                },
                Intro = new[] {
                    "결승으로 가는 길에는 사천당문의 독랄수 당문천이 남아 있었다. 당철영은 사촌 형의 독을 미리 경고했다.",
                    "비류연|(독에만 기대고 있는 상대라면, 그 계산부터 무너뜨려야겠지.)",
                    "당문천전 뒤 검후전과 검성전의 공방을 거쳐, 비류연과 위지천의 마지막 승부로 이어진다.",
                },
                Outro = new[] {
                    "운향정에서 나예린이 연주했던 곡이 묵금에서 흘렀다. 위지천은 질투에 흔들렸고, 끊긴 곡의 충격에 피를 토했다.",
                    "선풍우뢰와 선풍참혼, 금지 살초 천풍마뢰참까지 보이지 않는 막과 끊기지 않는 뇌령사에 막혔다.",
                    "비류연|비뢰도 오의, 뢰광류하곡.",
                    "왼손에서 은빛 뇌령사 세 줄기가 뻗었다. 위지천이 무너지고, 공손일취는 그 수법에 경악했다. 그가 떠올린 과거의 진상은 아직 드러나지 않았다.",
                    "다른 세 부문이 모두 무승부여서 비류연은 삼성대전 우승에 이어 종합 우승까지 차지했다.",
                    "나예린|검후처에 침입한 사람이 소협이라는 의심은 거두겠습니다.",
                    "비류연은 사과를 빌미로 다시 입맞춤했다. 나예린은 여전히 그의 마음을 읽지 못했고, 뽑은 검도 그에게 닿지 않았다.",
                    "금영호는 도광서에게 삼고구배를 받고 배당금을 챙겼지만 절반 이상을 비류연에게 내주었다.",
                    "한 달 뒤 염도는 연말 청룡단전을 앞두고 주작단을 다시 몰아붙였다. 한편 무흔각의 비류연 사문 조사 결과는 불명이었다.",
                    "효룡은 청흔·모용휘·비류연, 그리고 장홍을 주시했다. 다가오는 때의 정체는 아직 일행에게 알려지지 않았다.",
                },
                Encounters = new[] { new[] { "dang_munceon" }, new[] { "gwan_seolji_story" }, new[] { "cheong_heun_story" }, new[] { "wiji_cheon_baek" } },
                Trials = new[] { null, TrialRule.Survive("검후전 결승", "관설지의 공격을 12행동 동안 견뎌 주세요.", 12), TrialRule.Survive("검성전 결승", "청흔의 검을 12행동 동안 견뎌 주세요.", 12), null } },
            new StageData { Number = 14, RewardWeightBudget = 10, Name = "무당산 고립", RecommendedLevel = 33, DooTotal = 6500,
                Prologue = StoryMidgame.Prologues[14],
                Intro = StoryMidgame.Intros[14],
                Outro = StoryMidgame.Outros[14],
                Encounters = new[] { new[] { "maeng_yeonho" }, new[] { "amryong_salsu", "amryong_salsu", "amryong_salsu" }, new[] { "mak_jeok" }, new[] { "heuk_sang" }, new[] { "gal_hyobong" }, new[] { "chohon_gwigeom", "chohon_gwigeom", "myeongbu_jang" }, new[] { "gal_hyobong" }, new[] { "cheonsal", "jisal" } },
                Trials = new[] { null, null, null, null, TrialRule.Survive("갈효봉의 첫 공방", "갈효봉의 공격 8행동을 버텨 주세요. 방어와 운기를 활용할 수 있습니다.", 8), null, null, null } },
            new StageData { Number = 15, RewardWeightBudget = 9, Name = "철각비마대", RecommendedLevel = 35, DooTotal = 7000,
                Prologue = StoryMidgame.Prologues[15],
                Intro = StoryMidgame.Intros[15],
                Outro = StoryMidgame.Outros[15],
                Encounters = new[] { new[] { "bikwae_ssangchang", "bikwae_ssangchang" }, new[] { "sadae_jilpung", "sadae_jilpung", "sadae_jilpung" }, new[] { "cheolbi_gyeok", "cheolbi_gyeok", "wi_musang" }, new[] { "gu_cheonhak" } } },
            new StageData { Number = 16, Name = "저잣거리 습격", RecommendedLevel = 37, DooTotal = 7500,
                Prologue = StoryMidgame.Prologues[16],
                Intro = StoryMidgame.Intros[16],
                Outro = StoryMidgame.Outros[16],
                Encounters = new[] { new[] { "baegin_jagaek", "baegin_jagaek", "baegin_jagaek" }, new[] { "ssanggwi", "baegin_jagaek" }, new[] { "baek_tanjeo" } } },
            new StageData { Number = 17, RewardWeightBudget = 7, Name = "마하령의 분노", RecommendedLevel = 39, DooTotal = 8000,
                Prologue = StoryMidgame.Prologues[17],
                Intro = StoryMidgame.Intros[17],
                Outro = StoryMidgame.Outros[17],
                Encounters = new[] { new[] { "pae_yugyeong" }, new[] { "sinung_daewon", "sinung_daewon", "sinung_daewon" }, new[] { "pae_hyeokseong" } } },
            new StageData { Number = 18, RewardWeightBudget = 4, Name = "태극의 인재", RecommendedLevel = 41, DooTotal = 8500,
                Prologue = StoryMidgame.Prologues[18],
                Intro = StoryMidgame.Intros[18],
                Outro = StoryMidgame.Outros[18],
                Encounters = new[] { new[] { "yong_cheonmyeong" }, new[] { "hyo_ryong_rope" }, new[] { "gwan_cheolsu" } } },
            new StageData { Number = 19, RewardWeightBudget = 8, Name = "환마동 시험", RecommendedLevel = 43, DooTotal = 9000,
                Prologue = StoryMidgame.Prologues[19],
                Intro = StoryMidgame.Intros[19],
                Outro = StoryMidgame.Outros[19],
                Encounters = new[] { new[] { "hwan_ying", "hwan_ying", "hwan_ying" }, new[] { "hwan_geomseong" }, new[] { "hwan_gal_hyobong" }, new[] { "hwan_sukbu", "hwan_ying" }, new[] { "hwan_sabu" } } },
            new StageData { Number = 20, Name = "매몰 44일", RecommendedLevel = 45, DooTotal = 9500,
                Prologue = StoryMidgame.Prologues[20],
                Intro = StoryMidgame.Intros[20],
                Outro = StoryMidgame.Outros[20],
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
                    TrialRule.Lifeline("금요관", "절벽의 마천칠걸이 줄을 노린다. 힘을 모은 일격에 세 번 맞으면 줄이 끊어진다. 점혈로 끊거나 피하라!", 3),
                    TrialRule.Survive("검후의 십초지적", "검후의 공격을 12번 버텨라. 이길 수는 없다. 피하고 막아 살아남아라!", 12),
                    TrialRule.Harmony("수화관 내공합격", "모두가 한 번씩 쉬지 않고 공격을 이으면 다음 일격이 2.5배가 된다. 32행동 안에 암벽과 위지천을 쓰러뜨려라!", 32),
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
            // 2부 (17-18권). The 살룡대 fights (stage 31) and the 황금 완장 / training bouts (stage 32) are 게임 연출; the notes in the story text say so.
            new StageData { Number = 31, Name = "청룡은장의 밤", RecommendedLevel = 67, DooTotal = 16200,
                Prologue = new[]
                {
                    new StoryScene { Title = "등가의 잣대", Lines = new[] {
                            "홍매겁화로부터 수개월이 지난 초봄. 혁중은 염도·빙검의 사부가 이십 년 전 세상을 떠났음을 확인한다. 매듭지을 일이 있다며 나갔다가 가슴에 거미줄 같은 혈선을 안고 돌아왔다고 한다.",
                            "혁중|자네들이 뽑아낸 것을 보겠다.",
                            "혁중이 검지 하나를 치켜든다. 두 사람은 합격술로 기의 장벽을 뚫고 돌진했으나, 둘 다 기절했다.",
                            "의약전 이인용 특실. 두 사람은 서로 등을 돌리고 누워 있다.",
                            "빙검|빙염양의귀원합격이었다!",
                            "염도|염빙양의귀원합격이다!",
                            "비류연|그 할배는 멀쩡해요. 그래도 두 사람 합격술은 훌륭했어요.",
                        } },
                    new StoryScene { Title = "청부", Lines = new[] {
                            "모용휘|죽으라 하면 죽고, 살라 하면 살겠습니다.",
                            "혁중|검성을 죽여라! 방법은 맡기겠다.",
                            "검성은 모용휘의 친조부이자 우상이다. 모용휘는 숨을 고르며 가까스로 진정한다.",
                            "문가에서 엿듣던 비류연이 혁중과 내기를 건다. 비류연은 죽일 수 있다에, 혁중은 죽이지 못한다에 건다.",
                        } },
                    new StoryScene { Title = "회담", Lines = new[] {
                            "학관주 마진가는 대공자와 마천칠걸의 행방을 쫓느라 쉬지 못한다. 책상에는 정례 행사의 예상 명부가 놓여 있다.",
                            "빙검|중원표국에 대해 어떻게 생각하십니까.",
                            "중원표국은 강호 물류의 칠 할을 쥐고 정천맹 십대원로 표왕의 비호를 받는다. 빙검은 사천의 중양표국을 은밀히 밀어 견제하자고 하고, 마진가가 동의한다.",
                            "빙검|구체적인 안은 유능한 녀석이 며칠 안에 올릴 겁니다.",
                        } },
                    new StoryScene { Title = "몽환산장", Lines = new[] {
                            "안개 속의 몽환산장. 한 여인이 검은 두건의 공포에게 쫓기는 악몽에서 깨어난다.",
                            "몽무|영령 아가씨!",
                            "기억은 흐릿하고, 밤마다 상처 자리가 화끈거린다. 장주는 한 달 뒤의 시험에 합격해 정혼자인 그분의 힘이 되라고 한다.",
                            "영령|그것만이 제가 이 세상에 생을 부여받은 유일한 의미입니다.",
                            "장주는 시비 몽무·환무를 붙여 보낸 뒤, 냉혹한 얼굴로 대계를 그르치면 안 된다고 이른다.",
                        } },
                },
                Intro = new[] {
                    "같은 무렵, 사천 아미산 영역의 청룡은장. 복면 집단 살룡대가 밤에 은장을 덮친다.",
                    "장주 유재룡은 아들 유경영과 딸 유선아에게 금낭 둘과 황금빛 열쇠 황룡시, 옥패를 건넨다.",
                    "유재룡|남창의 천무학관으로 가서 철권 마진가를 찾아라. 아미로 가는 길은 막혔을 것이다.",
                    "아이들은 옷장 속의 진짜 통로로 빠져나간다.",
                    "혈표|원한은 없다. 열쇠가 필요할 뿐이다.",
                    "원작에서 살룡대의 서열 이위 참영과 삼위 격운은 유재룡의 가짜 통로 함정에 쓰러집니다. 이 싸움은 현재 편성 파티로 재현합니다.",
                },
                Outro = new[] {
                    "하얀 질풍이 지나가고, 혈표가 목이 뜯겨 쓰러진다. 아미산의 백호 백무후가 팔섬풍을 거느리고 나타나 노인에게 재롱을 부린다.",
                    "사부|하양아. 뒤에서 따라오너라.",
                    "노인은 자신을 무사무려 무처무복 무종무도한 사람이라고만 밝힌다.",
                    "잡화 수레에 숨어 있던 유경영과 유선아가 나온다. 장우양이 남매를 보호하기로 하고, 한 달 뒤 표행은 무사히 남창에 닿는다.",
                },
                Encounters = new[] { new[] { "salryong_daewon", "cham_yeong", "gyeok_un" }, new[] { "salryong_daewon", "hyeol_pyo", "salryong_daewon" } } },
            new StageData { Number = 32, Name = "승천무제 시험관", RecommendedLevel = 68, DooTotal = 16600,
                Prologue = new[]
                {
                    new StoryScene { Title = "어둠 속의 지배자", Lines = new[] {
                            "멸겁 두 자가 쓰인 검은 장막 앞. 대공자 비가 화룡이 하늘로 솟아 화공이 불발했음을 보고하고, 예상 못한 변수로 비류연의 이름을 올린다.",
                            "지배자|이 무림에서 누구도 두렵지 않으나 단 한 명, 그 노인만은 예외다.",
                            "지배자|열쇠는 어찌 되었느냐.",
                            "처분은 근신이다. 나오는 길에 사제 이시건이 새 비기 자운으로 비의 오른 소매를 터뜨린다.",
                            "이시건|대공자라는 칭호, 반드시 떼어 내겠습니다.",
                            "이시건이 이번 작전에서 데려온 여자를 캐묻자 비가 살기를 뿜는다.",
                            "대공자|그녀는 죽었다.",
                            "이시건은 번천계를 맡아 십삼혈을 거느리고 남창의 천무학관으로 향한다.",
                        } },
                    new StoryScene { Title = "발탁", Lines = new[] {
                            "윤준호|승천무제 시험관으로 발탁되셨대요! 고독한 무사부님이 알려 주셨어요!",
                            "비류연|무보수 막노동이잖아. 거절하러 가야지.",
                            "마진가는 지난 백 년 동안 거절한 사람은 비류연뿐이라고 한다.",
                            "비류연|천원지방이라 했죠. 둥근 걸 주세요. 천장지구, 많을수록 좋고요.",
                            "마진가가 은자를 내기로 하고 거래가 성립한다. 그리고 다른 노사들에게 말한다.",
                            "마진가|새 규칙은 시험관 학생들에게 일부러 알리지 않았소. 이미 시험은 시작되었소.",
                        } },
                    new StoryScene { Title = "련! 련! 련!", Lines = new[] {
                            "남궁상이 산에서 세 번째로 도망치다가 하류에서 현운과 주작단에게 붙잡힌다. 마지막으로 진령이 앞을 막는다.",
                            "남궁상|당신마저…",
                            "비류연은 벌 대신 칭찬을 하고, 진령을 앞세워 겁쟁이와 혼인할 여자는 없다고 넌지시 이른다. 남궁상은 스스로 수련을 계속하기로 한다.",
                            "비류연|일련일천타다.",
                        } },
                },
                Intro = new[] {
                    "지옥 훈련 일촌간파를 마친 남궁상과 비류연이 오른팔에 황금 완장을 차고 남창 시내로 나선다.",
                    "제101회 입관 시험 요강: 입관 희망자는 황금 완장 시험관에게 비무를 청할 수 있고, 완장을 가진 입관 희망자는 1·2차 시험이 면제된다.",
                    "아미의 유란과 점창의 유운비가 두 사람 앞을 막아선다.",
                    "비류연|오 초 안에 못 이기면 완장 둘 다 넘겨 드리죠.",
                    "원작에서는 남궁상이 혼자 삼 초 만에 두 사람을 제압합니다. 이 싸움은 현재 편성 파티로 재현합니다.",
                },
                Outro = new[] {
                    "날아드는 검을 피해 다니는 특훈은 보름 동안 이어진다.",
                    "한편 남창. 불안해 표국을 몰래 빠져나온 남매 앞에 중원표국 대표두 윤이정이 나타난다. 아버지 유재룡의 의형제다.",
                    "윤이정|나와 함께 가자. 중원표국이 방패가 되어 줄 것이다.",
                    "진소령이 윤이정의 소매를 벤다. 유경영은 숙부의 오른팔에 용이 휘감은 듯한 새 상처를 본다. 반년 전에는 없던 상처다.",
                    "유경영은 숙부의 손을 거절한다.",
                    "진소령|내 착각인가. 장심에 음유한 내공이 있었던 듯한데.",
                    "같은 밤 비류연은 식은땀을 흘리며 깨지만, 꿈의 내용은 기억하지 못한다.",
                },
                Encounters = new[] { new[] { "yu_ran", "yu_unbi" }, new[] { "gwan_cheolsu_eogeom" } },
                Trials = new[]
                {
                    TrialRule.Deadline("황금 완장", "유란과 유운비가 황금 완장을 노린다. 30행동 안에 두 도전자를 제압하라!", 30),
                    TrialRule.Survive("이기어검 특훈", "빙검을 이길 필요는 없다. 허공에서 날아드는 검을 12번 버텨라!", 12),
                } },
            new StageData { Number = 33, Name = "무극검과 누명", RecommendedLevel = 69, DooTotal = 17000,
                Prologue = new[]
                {
                    new StoryScene { Title = "백 년의 숙원", Lines = new[] {
                            "공손세가 가주 공손경운은 자신이 모란, 곧 모용가 검의 벽을 넘지 못했다고 고백하고 아들에게 가주의 보검 극예를 넘긴다.",
                            "공손절휘는 열아홉, 십 년을 지존검법에 바쳤다. 그는 승천무제에서 모란의 후계자를 꺾겠다고 맹세한다.",
                            "사흘 동안 모용휘를 쫓아 스무 번의 대결을 엿보지만, 모용휘는 도전자를 모두 주먹 한 방에 끝낸다. 시선을 눈치챈 모용휘가 기를 날려 경고한다.",
                            "모용휘|(주먹부터 나가는 걸 보니, 나도 비류연 공에게 물들었군.)",
                        } },
                    new StoryScene { Title = "빈대떡을 부치는 법", Lines = new[] {
                            "마천각 사신 이시건이 학관주 마진가를 만난다. 반쪽 부를 맞추니 활과 화살 문양의 대대가 들어맞는다.",
                            "군사 손문경이 마천각이 화산 사건을 덮으려 한다고 몰아세운다. 마진가는 빈대떡을 뒤집으려면 요리사가 필요하다고 말한다.",
                            "마진가는 사천멸겁을 꺼낸다. 확인된 죽음은 남천과 동천뿐, 서천은 중상만 입었고 최고수 북천은 행방불명이다.",
                            "이시건은 속으로 동요하고, 학관이 마련한 숙소를 거절한다.",
                            "손문경|기밀은 사신 앞에서만 흘렸습니다. 이 정보가 다른 데서 나오면, 통로는 저 사람입니다.",
                        } },
                },
                Intro = new[] {
                    "금란주루. 겨울 내내 학관에 머문 은설란이 나예린을 끌고 와 독주 여섯 병을 비운다.",
                    "은설란|남자들은 바보예요. 그 사람도 자기 이상만 쫓다가 말 한마디 없이 죽었어요.",
                    "은설란이 쓰러진 사이, 마천각 이공자 이시건이 손가락 짓만으로 빙봉영화수호대 팔호위를 쓰러뜨리고 나예린에게 다가온다.",
                    "나예린|거절하겠어요.",
                    "위층에서 검성·도성에게 밥을 얻어먹던 비류연이 젓가락을 던진다.",
                },
                Outro = new[] {
                    "비류연이 오른팔의 묵룡환 하나를 풀고, 느린 손을 세 검 사이로 밀어 넣는다. 비뢰도 상급오의 변역의 장, 적중화. 청흔이 피를 토하며 패배를 인정한다.",
                    "백무영의 깃발 신호에 무원대 서른과 구정회 칠십여 명이 검진을 펼친다. 나예린이 검을 뽑으려 하자 비류연이 말린다.",
                    "비류연|예린답지 않아요. 나를 믿어 줘요.",
                    "나예린|믿을게요.",
                    "비류연은 수갑을 차고 순순히 연행된다. 홀로 남은 나예린은 스스로에게 맹세한다.",
                    "나예린|류연도, 령 언니도 내 손으로 되찾겠어.",
                    "학관의 독신 남자들이 흰 머리띠에 살 자를 쓰고 「즉살 비류연」 현수막을 든다. 이틀 만에 상소가 산처럼 쌓인다.",
                    "감옥으로 면회 온 장홍에게 비류연은 대리인으로 남궁상을 고른다. 그리고 남궁상에게 말한다.",
                    "비류연|날 위해 죽어 줘야겠다.",
                    "다섯 번째 밤. 화산파 매화 옷에 황금 완장을 찬 시험관을 노리던 공손절휘 앞에서 시험관이 돌아서며 뇌전 같은 쾌검을 날린다.",
                    "남궁상|남궁세가의 남궁상이다.",
                    "며칠 뒤 중양표국. 유은성이 달려와 진소령에게 비무 상대 남궁상이 피살당했다고 전한다.",
                },
                Encounters = new[] { new[] { "i_sigeon" }, new[] { "ipgwan_dojeonja", "chimbaetneun_ja", "ipgwan_dojeonja" }, new[] { "cheong_heun_mugeuk" } },
                Trials = new[]
                {
                    TrialRule.Survive("금란주루의 광풍난주", "사신 이시건을 쓰러뜨릴 수는 없다. 자운암풍의 공격을 10번 버텨라!", 10),
                } },
            new StageData { Number = 34, Name = "흰 봉황", RecommendedLevel = 70, DooTotal = 17400,
                Prologue = new[]
                {
                    new StoryScene { Title = "통곡", Lines = new[] {
                            "천무학관이 남궁상의 장례로 들끓는다. 진령이 빈 관 앞에서 운다. 비류연이 미리 준 고춧가루와 양파 주머니 덕분이다.",
                            "주작단은 무원대의 검시를 몸으로 막아선다.",
                            "감옥으로 찾아온 장홍에게 비류연이 작전을 털어놓는다. 죽었다가 부활하는 작전이고, 시신 역할은 아는 곳에 숨겨 두었다.",
                        } },
                    new StoryScene { Title = "두 번의 방문", Lines = new[] {
                            "나예린이 처음으로 이진설에게 도움을 청한다. 이진설은 남자 기숙사 검혼각에서 효룡을 끌어낸다.",
                            "나예린은 용안으로 장홍을 압박해, 이시건이 중원표국 남창지국에 있다는 것을 알아낸다.",
                        } },
                },
                Intro = new[] {
                    "흰 봉황이 빈 산 위를 난다. 나예린이 애검 빙령의 검강으로 중원표국 남창지국의 대문을 열두 조각으로 베고 들어간다.",
                    "이진설과 효룡이 표사들을 막는다. 오가 형제 오영과 오기가 앞을 가로막는다.",
                    "원작에서 이 싸움은 나예린·이진설·효룡의 몫입니다. 비류연은 감옥에 있습니다. 이 싸움은 현재 편성 파티로 재현합니다.",
                },
                Outro = new[] {
                    "나예린은 증언을 위해 목을 베지 않는다. 섬광탄이 터지고, 이시건과 윤이정이 달아난다. 이시건의 뺨에도 나예린의 상처가 남는다.",
                    "감옥. 나예린이 실패했다고 하자 비류연이 고개를 젓는다.",
                    "비류연|스스로 움직였잖아요. 그럼 성공이에요. 그 대신 손수 만든 음식 좀 넣어 주세요.",
                    "이튿날 밤. 가죽끈으로 묶인 남궁상과 공손절휘가 중양표국 담을 넘다 진소령과 유은성에게 붙잡힌다.",
                    "진소령|유령이 담을 넘다니. 베어도 되겠구나.",
                    "장우양이 두 사람을 손님으로 맞는다. 공손절휘는 모용휘를 이기려고 황금 완장만 노렸을 뿐, 사람을 죽인 적이 없다.",
                    "진소령은 공손절휘와 싸울 때 남궁상이 시간이 느려지는 감각에 들어갔다는 말을 듣고, 그것을 심안의 경지라 본다.",
                },
                Encounters = new[] { new[] { "jungwon_pyosa", "o_yeong", "o_gi" }, new[] { "yun_ijeong", "i_sigeon_jungwon" } } },
            new StageData { Number = 35, Name = "전설의 포효", RecommendedLevel = 71, DooTotal = 17800,
                Prologue = new[]
                {
                    new StoryScene { Title = "범죄를 촉탁하는 밤", Lines = new[] {
                            "감옥의 비류연이 나예린이 만든 도시락을 먹는다. 써는 솜씨는 완벽하고, 불 조절은 실패했다.",
                            "비류연|홍 아저씨, 이거 나 소저가 직접 만든 거예요.",
                            "장홍은 나예린이 만든 음식이라는 데 경악한다.",
                            "비류연|미끼를 잡은 다음 날 진범이 다시 나올 확률이 팔 할이에요. 안 나오면 만만한 학생을 하나 더 습격해서, 하루도 거르지 않는다고 믿게 하세요.",
                        } },
                    new StoryScene { Title = "채무 이행", Lines = new[] {
                            "그물 역할을 할 사람이 하나 더 필요하다. 비류연에게 빚진 모용휘다.",
                            "비류연|선불로 갚으시죠.",
                            "모용휘|외상으로 하겠습니다.",
                            "결국 모용휘는 몸으로 갚게 된다. 피풍의를 쓴 미끼 역할이다.",
                        } },
                },
                Intro = new[] {
                    "황금 완장 순찰자로 위장한 미끼를 이시건이 노린다. 자운 암풍 오의, 살식 질풍참살. 미끼는 피풍의를 쓴 모용휘였다.",
                    "남궁상이 비류연이 가르친 대로 보지 말고 느끼며 뇌전검법 구명절초 성막밀밀로 막는다. 이시건은 자신이 함정에 걸렸음을 안다.",
                    "이시건|화산지회 우승자라는 상징만 필요했을 뿐, 누구라도 상관없었다.",
                    "원작에서 이시건을 막는 것은 남궁상·모용휘·공손절휘, 그리고 진소령과 유은성입니다. 이 싸움은 현재 편성 파티로 재현합니다.",
                },
                Outro = new[] {
                    "남매의 방. 숙부 행세를 하던 윤이정의 오른 팔뚝에서 유경영은 가문 비전 용린폭의 상처를 본다.",
                    "유경영|당신이… 아버지의 원수야.",
                    "윤이정이 칼을 드는 순간 포효가 남창 전체에 울린다. 나무 위의 황금빛 두 눈, 백무후가 문을 부수고 윤이정을 앞발로 찢어 이시건에게 내던진다.",
                    "나무를 방패로 삼았는데도 이시건의 왼쪽 얼굴 대부분이 발톱에 찢긴다. 그는 남은 십삼혈과 빈사의 윤이정을 차례로 던져 시간을 번다.",
                    "사부|잠 좀 자자.",
                    "노인이 던진 목침이 백무후의 콧등을 맞히고, 그 틈에 이시건만 달아난다.",
                    "지하 감옥의 비류연도 익숙한 소리에 눈을 뜬다.",
                    "비류연|사천에 사는 놈이 여기 있을 리가 없는데…",
                    "전설이 된 새벽 뒤로 중양표국에는 계약이 쏟아진다. 노사부를 깨우러 들어간 유은성은 목침에 맞아 기절한다.",
                },
                Encounters = new[] { new[] { "i_sigeon_bait" }, new[] { "sipsamhyeol", "sipsamhyeol", "sipsamhyeol" }, new[] { "sipsamhyeol", "il_hyeol", "sipsamhyeol" } },
                Trials = new[]
                {
                    TrialRule.Survive("미끼 작전", "이시건을 붙잡아 두어라. 유은성의 검이 닿을 때까지 자운 암풍의 공격을 10번 버텨라!", 10),
                } },
            new StageData { Number = 36, Name = "운명의 비무", RecommendedLevel = 72, DooTotal = 18200,
                Prologue = new[]
                {
                    new StoryScene { Title = "소년, 천무학관에 서다", Lines = new[] {
                            "진소령이 남매를 은사 마진가에게 데려간다. 유경영은 아버지의 유언대로 한 번도 열지 않은 비단 주머니를 건넨다.",
                            "마진가|이것은 강호의 운명을 가를 물건이다. 네 아버지는 강호를 지키다 돌아가셨다.",
                            "유경영|제 소원은 청룡은장을 다시 세우는 것입니다.",
                            "마진가는 중원표국의 금강십이벽이 연루된 이상 표물을 중양표국으로 나눠 맡기겠다고 약속한다.",
                        } },
                    new StoryScene { Title = "영령, 접수하다", Lines = new[] {
                            "동정호의 섬 자죽도. 뱃사람들이 용왕성이라 부르는 마천각에 몽환산장의 영령이 시비 몽무·환무와 함께 입관하러 온다.",
                            "교관 옥유경이 그녀를 영입하려 하고, 접수처의 흑의서생이 면접을 본다.",
                            "흑의서생|은명이라 하오.",
                            "영령은 그 이름에 까닭 모르게 가슴이 아파 온다.",
                        } },
                },
                Intro = new[] {
                    "백무영이 감옥의 비류연을 풀어 관주 집무실로 데려간다.",
                    "비류연|스스로 운명의 주인이 된 자는 두려워하지 않아요.",
                    "집무실에는 마진가·진소령·유은성, 그리고 검존 공손일취가 있다. 검존은 물의를 일으킨 비류연을 시험관에서 빼겠다고 한다.",
                    "비류연이 손자 이야기를 꺼내자, 손자가 납치되어 죽은 줄 안 검존이 검을 휘두른다.",
                    "원작에서 비류연은 머리카락 몇 올만 잘린 채 피해 관주석에 앉아 있습니다. 이 싸움은 현재 편성 파티로 재현합니다.",
                },
                Outro = new[] {
                    "날아간 애검 섬뢰가 진소령의 목덜미 뒤에서 멈춘다. 남궁상의 첫 이기어검이다. 남궁상은 그대로 쓰러진다.",
                    "진소령|진령을 잘 부탁한다. 이제 고모라 불러도 좋다.",
                    "빙검|이기어검을 아는 가장 좋은 방법은 직접 익히는 것이다.",
                    "비류연|스무 해 동안 쌓은 한계 하나를 넘었을 뿐이에요.",
                    "승천무제 본선. 공손절휘는 모용휘에게 지고 울고, 금사·은사 매화가 수놓인 검은 양산의 현의여인이 손수건을 건넨다. 그 여인은 남궁상의 열 초를 우산을 든 채 모두 피하고 합격한다.",
                    "비류연은 사절단 명단에서 빠진다. 그는 금고에서 빛바랜 작은 상자를 꺼내고, 거울 앞에서 앞머리를 든다.",
                    "비류연|오랜만이다.",
                    "가위 소리가 난다. 출발하는 사절단을 천무삼성이 망루에서 배웅한다.",
                    "흑마를 탄 현의여인이 나예린 곁에 다가온다. 나예린은 그 마음을 읽지 못한다. 여인의 눈이 햇빛에 호안석처럼 금빛으로 바뀐다.",
                    "현의여인|오랜만이에요, 린.",
                    "나예린|연비!",
                },
                Encounters = new[] { new[] { "geomjon_gongson" }, new[] { "jin_soryeong" } },
                Trials = new[]
                {
                    TrialRule.Survive("검존의 오해", "검존을 쓰러뜨릴 일이 아니다. 남궁상이 공손절휘를 데려올 때까지 검을 6번 버텨라!", 6),
                } },
        };

        /// <summary>The player level a stage asks for before rounding (<see cref="StageData.RecommendedPlayerLevel"/> rounds it).</summary>
        public static double ExactPlayerLevel(StageData st) { return 1.0 + Math.Max(1, st.RecommendedLevel) / Progression.CurvePerPlayerLevel; }

        /// <summary>Exp from level 1 to a fractional level (part of the last level's bar for the fraction).</summary>
        private static long CumulativeExpAt(double level)
        {
            int whole = (int)Math.Floor(level);
            return CumulativeExp(whole) + (long)Math.Round((level - whole) * Progression.ExpToNext(whole));
        }

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

        private static int RewardDenominator(StageData st)
        {
            if (!st.IsQuest && !st.IsTower && st.RewardWeightBudget > 0) return st.RewardWeightBudget;
            int w = 0;
            foreach (var enc in st.Encounters) foreach (var id in enc) w += RewardWeight(id);
            return w;
        }

        /// <summary>Baseline level cushion used to calculate first-clear experience.</summary>
        public const int LevelHeadroom = 0;

        /// <summary>Share of exp (not 두) paid when replaying an already cleared stage.</summary>
        public const double ReplayExpFactor = 0.5;
        public const double ReplayDooFactor = 0.5;

        /// <summary>
        /// Exp for one enemy, based on the level span between neighboring stages and the original reward weight budget.
        /// The span runs between unrounded player levels (<see cref="ExactPlayerLevel"/>), so two stages that round to the same recommended level still both pay;
        /// expanded story stages add rewards at the original per-weight rate, so they can grow beyond this baseline.
        /// </summary>
        public static long ExpReward(StageData st, string enemyId)
        {
            var next = FindStage(st.Number + 1);
            // the last stage has no next one: it spans the usual two curve steps, so it pays like the others and still ends at its recommended level + 1
            double to = (next != null ? ExactPlayerLevel(next) : ExactPlayerLevel(st) + 2.0 / Progression.CurvePerPlayerLevel) + LevelHeadroom;
            double from = st.Number > 1 ? ExactPlayerLevel(st) + LevelHeadroom : st.RecommendedPlayerLevel;
            long total = Math.Max(0L, CumulativeExpAt(to) - CumulativeExpAt(from));
            return (long)Math.Ceiling((double)total * RewardWeight(enemyId) / RewardDenominator(st));
        }

        /// <summary>Stage 두 payouts are raised by this factor on top of <see cref="StageData.DooTotal"/> (clears, replays and quests alike).</summary>
        public const double StageDooFactor = 1.8;   // x1.5 more 두 from fights than before (was 1.2)

        public static long DooReward(StageData st, string enemyId)
        {
            return (long)Math.Round((double)st.DooTotal * StageDooFactor * RewardWeight(enemyId) / RewardDenominator(st));
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
