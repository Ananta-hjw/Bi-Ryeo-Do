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
        public int OwnerStage;   // companion art becomes available after this story stage is cleared
        public int OwnerLevel;   // companion arts: the companion's own level at which the art is learned (0 = known from joining)
        public string Replaces;  // companion arts: the older art of the same line this one takes over once learned (its 수련 각성 carries over)
        public string FormOf;    // 무공 변화 (EarlyGameContent.ArtForms): the protagonist art this technique grows out of
        public int FormTier;     // the 숙련 step (Mastery.LevelNames index) at which the art turns into this form; that step's gate is the form's unlock stage

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
        public string ArtId;         // optional: canonical artwork of the same character/opponent, never an unrelated stand-in
        public string ArtKey { get { return string.IsNullOrEmpty(ArtId) ? Id : ArtId; } }
        /// <summary>The level to show the player: the enemy's curve <see cref="Level"/> on the player level scale (curve 65 = level 25), so it reads next to the protagonist's own level.</summary>
        public int DisplayLevel { get { return Progression.PlayerLevelAt(Level); } }
        public bool IsBoss;
        public bool StoryOnly;   // authored scene opponents are excluded from random tower encounters
        public float RetreatHpPct;   // >0: story boss that escapes at this HP fraction instead of dying
        public bool HoldsAtOneHp;    // story boss that never drops below 1 HP: the blow that would fell it makes it leave instead (says RetreatLines)
        public string[] RetreatLines;   // "화자|대사" lines said as it leaves
        public int FixedHp;          // >0: exact max HP regardless of level and difficulty (a scripted obstacle, e.g. 화룡멸겁대진)
        /// <summary>A nameless follower (궁수, 도부수, 회원 ...) that fills out a story fight; never picked for the 무한 비무대 pool.</summary>
        public bool IsMinion;
        public int Level;
        public float HpMul = 1f, AtkMul = 1f, DefMul = 1f;
        public int SpeedAdd;
        public int Range = 1;       // how many tiles away its attacks reach
        public EnemyMove[] Moves;
        /// <summary>Gear the enemy wears (story stages, <see cref="GearLoadout.ForEnemies"/>), applied like a player's; null = none.</summary>
        public List<GearLevel> Gear;
        private List<GearLevel> _naturalBonus;

        /// <summary>A copy wearing <paramref name="gear"/>.</summary>
        public EnemyData WithGear(List<GearLevel> gear)
        {
            var c = (EnemyData)MemberwiseClone();
            c.Gear = gear;
            c._naturalBonus = null;
            return c;
        }

        /// <summary>A creature's equivalent combat bonuses without visible human equipment.</summary>
        public EnemyData WithNaturalBonus(List<GearLevel> profile)
        {
            var c = (EnemyData)MemberwiseClone();
            c.Gear = null;
            c._naturalBonus = profile;
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
            var bonuses = Gear ?? _naturalBonus;
            if (bonuses != null && bonuses.Count > 0) stats = Inventory.ApplyGear(stats, GearLoadout.Ids(bonuses), bonuses);
            return stats;
        }

        /// <summary>Shown attack / defense of every enemy (about what the protagonist's own curve gives: 1.4).</summary>
        public const double DisplayRatio = 1.4;

        public Combatant CreateCombatant(string uniqueId)
        {
            var stats = BuildStatsBeforeRealm();
            if (FixedHp > 0) stats.MaxHp = FixedHp;
            var c = new Combatant(uniqueId, Name, Team.Enemy, stats, null, FixedHp <= 0 && Realm.HasRealm(this));
            c.IsBoss = IsBoss;
            c.Rank = Rank;
            c.RetreatHpPct = RetreatHpPct;
            c.HoldsAtOneHp = HoldsAtOneHp;
            c.RetreatLines = RetreatLines;
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
        public double RewardScale = 1.0; // redistributes a shortened chapter's experience/currency budget across retained fights
        /// <summary>The fights' line-ups before 전투 인원 확장: exp, 두 and drops follow these, so the followers added later pay nothing extra (null = <see cref="Encounters"/>).</summary>
        public string[][] RewardEncounters;
        /// <summary>Optional story scenes shown before Intro, only while the stage has not been cleared yet (bridges the volumes' plot between fights).</summary>
        public StoryScene[] Prologue;
        public string[] Intro, Outro;
        /// <summary>Fights in order; each is a group of enemy ids (1-3). Trials may follow a boss.</summary>
        public string[][] Encounters;
        /// <summary>Optional fixed perspectives per encounter. Null uses the player's regular party; quests and towers always use that party.</summary>
        public string[][] StoryActors;
        /// <summary>Optional special rules, parallel to <see cref="Encounters"/> (null or a null entry = a normal fight).</summary>
        public TrialRule[] Trials;
        /// <summary>Optional choice events, parallel to Encounters. An event slot has no enemies.</summary>
        public StoryChoiceEvent[] Events;
        public StoryChoiceEvent EventFor(int encounter)
        {
            return !IsQuest && !IsTower && Events != null && encounter >= 0 && encounter < Events.Length ? Events[encounter] : null;
        }
        public TrialRule TrialFor(int encounter) { return Trials != null && encounter < Trials.Length ? Trials[encounter] : null; }

        public int RecommendedLevel;   // the enemy stat-curve level of the stage; the player level to bring is RecommendedPlayerLevel
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
            if (IsQuest) return Quests.Scale(QuestId, enemy);
            var scaled = StageStars.StoryEnemy(enemy, Number, Difficulty, trial);
            return FireDragonFinale.Is(this) && id == FireDragonFinale.BiId ? FireDragonFinale.LastStand(scaled) : scaled;
        }

        public StageData WithDifficulty(int difficulty)
        {
            var c = (StageData)MemberwiseClone();
            c.Difficulty = Math.Max(0, Math.Min(StageStars.DifficultyCount - 1, difficulty));
            if (c.Number == FireDragonFinale.Stage && !c.IsQuest && !c.IsTower && c.Difficulty > 0) FireDragonFinale.UseLegacy(c);
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

        /// <summary>
        /// 무공 변화: an art of a line turns into the next technique of the original text once its 숙련 reaches <see cref="ArtData.FormTier"/>,
        /// the step's gate (the form's unlock stage and the level expected there, <see cref="Mastery.TierGate"/>) is met (<see cref="Mastery.CurrentForm"/>).
        /// Forms are never learned, listed or equipped on their own; the base art keeps its id, slot and 숙련.
        /// </summary>
        public static readonly ArtData[] ArtForms =
        {
            new ArtData { Id = "yeomyeong_cheonan", Name = "영사심결 오의 여명천안", Description = "눈이 황금빛으로 물들며 모든 수가 보인다. 다음 차례까지 모든 공격을 피한다.", Effect = SkillEffect.Evade, QiCost = 40, Cooldown = 3, UnlockStage = 10, FormOf = "samcheopyeong", FormTier = 1 },
            new ArtData { Id = "nakroe_yeong", Name = "그림자 숨기기의 장 낙뢰영", Description = "봉황무 오의. 그림자 속으로 몸을 숨겨 다음 차례까지 모든 공격을 피한다.", Effect = SkillEffect.Evade, QiCost = 100, Cooldown = 3, UnlockStage = 12, UnlockDiff = 1, FormOf = "samcheopyeong", FormTier = 2 },
            new ArtData { Id = "ujunggeo_buljeom", Name = "봉황무 비전극상오의 우중거 불점의", Description = "빗속을 걸어도 옷이 젖지 않는다. 다음 차례까지 모든 공격을 피한다.", Effect = SkillEffect.Evade, QiCost = 140, Cooldown = 2, UnlockStage = 18, UnlockDiff = 1, FormOf = "samcheopyeong", FormTier = 3 },
            new ArtData { Id = "sineunsul_yeongsik", Name = "신은술 영식", Description = "그림자를 먹는 술법을 고쳐 만든 은신. 다음 차례까지 모든 공격을 피한다.", Effect = SkillEffect.Evade, QiCost = 160, Cooldown = 2, UnlockStage = 30, UnlockDiff = 1, FormOf = "samcheopyeong", FormTier = 4 },
            new ArtData { Id = "samboku_jungbok", Name = "삼복구타권법 중복", Description = "아구창창 주구장창. 아홉 번 쉬지 않고 두들긴다.", Power = 0.5f, Hits = 9, QiCost = 28, Cooldown = 2, UnlockStage = 9, FormOf = "samboku_chobok", FormTier = 2 },
            new ArtData { Id = "simroe_cheonhyeongin", Name = "심뢰 극 천형인", Description = "비기. 단심무형의 장의 끝. 하늘의 형벌을 새기는 일격.", Power = 22.0f, SpiritCost = 100, UnlockStage = 24, UnlockDiff = 1, FormOf = "simroe", FormTier = 4 },
            new ArtData { Id = "pyeonsu_pungsin", Range = 4, Name = "편수 풍신", Description = "비기. 한 손으로 다섯 자루 비뢰도를 풀어 물기둥째 적 전체를 쓸어 버린다.", Power = 15.0f, AllEnemies = true, SpiritCost = 100, UnlockStage = 38, UnlockDiff = 1, FormOf = "pungsin", FormTier = 2 },
            new ArtData { Id = "hwaryong_nanryu", Name = "풍신 화룡난류", Description = "비기. 풍신 용권승천의 장. 풍신에 불을 실어 화룡의 꼬리와 열풍진, 두 머리의 불길이 적 전체를 휩쓴다.", Inflict = StatusKind.Burn, InflictChance = 0.6f, Power = 17.0f, AllEnemies = true, SpiritCost = 100, UnlockStage = 60, UnlockDiff = 1, FormOf = "pungsin", FormTier = 4 },
            new ArtData { Id = "samnyeon_guta", Name = "돌아온 말복 천지무견 삼년구타", Description = "삼 년 치를 한 번에 몰아 열두 번 두들긴다.", Power = 1.6f, Hits = 12, QiCost = 330, Cooldown = 4, UnlockStage = 61, FormOf = "cheonji_mugyeon", FormTier = 3 },
            new ArtData { Id = "paego_makpaego", Name = "삼복구타산 궁극 말살기 패고패고막패고", Description = "삼복구타의 궁극 말살기. 쉬지 않고 열두 번 패고 또 팬다.", Power = 1.8f, Hits = 12, QiCost = 380, Cooldown = 4, UnlockStage = 5, UnlockDiff = 1, FormOf = "cheonji_mugyeon", FormTier = 4 },
            new ArtData { Id = "gyeokgongjang", Ranged = true, Range = 4, Name = "격공장", Description = "허공을 격해 떨어진 적을 치는 장력.", Power = 7.5f, QiCost = 170, Cooldown = 2, UnlockStage = 56, FormOf = "hoejeop", FormTier = 2 },
        };

        public static List<ArtData> FormsOf(string baseId)
        {
            var list = new List<ArtData>();
            foreach (var f in ArtForms) if (f.FormOf == baseId) list.Add(f);
            list.Sort((a, b) => a.FormTier.CompareTo(b.FormTier));
            return list;
        }

        public static readonly ArtData[] Arts =
        {
            new ArtData { Id = StarterArtId, Ranged = true, Range = 4, Name = "비도 던지기", Description = "비뢰도의 기초. 비도를 날려 적을 맞힌다.", Power = 1.3f, Cooldown = 0 },
            new ArtData { Id = "samboku_chobok", Name = "삼복구타권법 초복", Description = "사부가 창안한 권법. 초복은 근육을 노린다.", Power = 2.2f, QiCost = 14, Cooldown = 1, LearnCost = 0, UnlockStage = 1 },
            new ArtData { Id = "bunroesu", Name = "분뢰수", Description = "손을 강철처럼 만들어 내려친다.", Inflict = StatusKind.InnerWound, InflictChance = 0.4f, Power = 3.0f, QiCost = 22, Cooldown = 2, LearnCost = 220, UnlockStage = 2 },
            new ArtData { Id = "samcheopyeong", Name = "삼첩영", Description = "봉황무의 잔상 셋. 다음 차례까지 모든 공격을 피한다.", QiCost = 20, Cooldown = 3, Effect = SkillEffect.Evade, LearnCost = 400, UnlockStage = 3 },
            new ArtData { Id = "bi_roe_do", Ranged = true, Range = 4, Name = "비뢰도", Description = "비도 세 자루를 연달아 날린다.", Inflict = StatusKind.Bleed, InflictChance = 0.4f, Power = 1.7f, Hits = 3, QiCost = 32, Cooldown = 3, LearnCost = 900, UnlockStage = 3 },
            new ArtData { Id = "roegwang_ryuhagok", Name = "뢰광류하곡", Description = "여섯 자루를 동시에 운용해 적 전체를 벤다.", Inflict = StatusKind.Bleed, InflictChance = 0.25f, Power = 2.6f, AllEnemies = true, QiCost = 46, Cooldown = 4, LearnCost = 4500, UnlockStage = 6 },
            new ArtData { Id = "seonpung_roejeol", Name = "선풍뇌절", Description = "비기. 기세를 모두 쏟아 은빛 섬광으로 적을 쓰러뜨린다.", Power = 9.5f, SpiritCost = 100, LearnCost = 1500, UnlockStage = 4 },
            // Learned in volumes 7-9 (names from the original text)
            new ArtData { Id = "cheongeun_roe", Name = "뇌격타 천근뢰", Description = "천 근의 무게를 실은 뇌전의 일격. 철각비마대의 창을 꺾은 기술.", Inflict = StatusKind.Burn, InflictChance = 0.5f, Power = 6.4f, QiCost = 67, Cooldown = 3, LearnCost = 7000, UnlockStage = 10 },
            new ArtData { Id = "bongik_bisang", Name = "사신무 봉익비상", Description = "봉황이 날개를 펴듯 사방으로 쏟아내는 일격. 적 전체를 친다.", Power = 3.6f, AllEnemies = true, QiCost = 110, Cooldown = 4, LearnCost = 15000, UnlockStage = 15 },
            new ArtData { Id = "simroe", Name = "단심무형의 장 심뢰", Description = "비기. 마음을 베는 검기의 오의가 적의 중심을 꿰뚫는다.", Power = 13.0f, SpiritCost = 100, LearnCost = 9000, UnlockStage = 20 },
            // Learned in volumes 10-12 (names from the original text)
            new ArtData { Id = "goengroe", Ranged = true, Name = "굉천광뢰의 장 굉뢰", Description = "작은 은빛 물방울이 도와 무기를 박살내는 비뢰도의 오의.", Power = 10.5f, QiCost = 233, Cooldown = 4, LearnCost = 30000, UnlockStage = 27 },
            // Volumes 13-29 (names from the original text; 연비 is 비류연 in disguise, 21권). 일반 35-66 in story order, then 어려움 10 쌍룡귀천 and 어려움 30 풍신, the last art; techniques of an existing line are its forms (ArtForms).
            new ArtData { Id = "cheonji_mugyeon", Name = "삼복구타봉법 말복 천지무견", Description = "천하무구를 고쳐 만든 봉법. 하늘도 땅도 보이지 않게 여섯 번 내려친다.", Power = 2.2f, Hits = 6, QiCost = 310, Cooldown = 4, LearnCost = 42000, UnlockStage = 35 },
            new ArtData { Id = "hyeoncheon_powol", Name = "현천은린 오의 현천포월", Description = "비기. 수백 줄기 검은 번개가 달을 감싸듯 적 전체를 덮친다.", Power = 14.0f, AllEnemies = true, SpiritCost = 100, LearnCost = 48000, UnlockStage = 42 },
            new ArtData { Id = "hoejeop", Ranged = true, Range = 4, Name = "회접", Description = "손으로 펴 만든 쇠 원반을 나비처럼 날린다.", Power = 6.5f, QiCost = 150, Cooldown = 2, LearnCost = 46000, UnlockStage = 48 },
            new ArtData { Id = "mukroesal", Name = "무명검 오의 묵뢰살", Description = "검은 번개를 두른 일격. 심도 천외일도를 꺾은 오의.", Power = 15.0f, QiCost = 330, Cooldown = 4, LearnCost = 50000, UnlockStage = 54 },
            new ArtData { Id = "jumang_pobak", Range = 4, Name = "괴뢰의 장 주망포박", Description = "뇌령사로 그물을 쳐 상대를 묶는다. 기를 모으는 중이면 끊는다.", Power = 2.0f, Effect = SkillEffect.Stun, QiCost = 200, Cooldown = 4, LearnCost = 52000, UnlockStage = 60 },
            new ArtData { Id = "umin_gunmu", Name = "괴뢰의 장 꼭두각시 춤 우민군무 불신지옥", Description = "적을 서로 이간해 꼭두각시처럼 춤추게 한다. 적 전체가 굳는다.", Power = 3.0f, AllEnemies = true, Effect = SkillEffect.Stun, QiCost = 380, Cooldown = 5, LearnCost = 80000, UnlockStage = 66 },
            new ArtData { Id = "ssangryong_gwicheon", Name = "뇌화열풍인 쌍룡귀천", Description = "비기. 벽력의 장 심득 오의. 벼락과 화룡과 질풍이 두 마리 용이 되어 하늘로 돌아간다.", Power = 20.0f, AllEnemies = true, SpiritCost = 100, LearnCost = 100000, UnlockStage = 10, UnlockDiff = 1 },
            new ArtData { Id = "pungsin", Name = "풍신(風神)", Description = "비기. 열 자루 비뢰도와 뇌령사가 나선을 그리며 용권풍이 되어 적 전체를 휩쓴다.", Power = 12.35f, AllEnemies = true, SpiritCost = 100, LearnCost = 44000, UnlockStage = 30, UnlockDiff = 1 },
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
            new ArtData { Id = "ny_biseol", Name = "비설보", Description = "검후가 전한 흩날리는 눈의 분신 보법. 다음 차례까지 모든 공격을 피한다.", Effect = SkillEffect.Evade, QiCost = 90, Cooldown = 3, OwnerId = "na_yerin", OwnerStage = 32, OwnerLevel = 20 },
            new ArtData { Id = "ny_soan", Name = "소안검", Description = "웃는 눈처럼 가볍고 빠른 검.", Power = 3.0f, QiCost = 50, Cooldown = 1, OwnerId = "na_yerin", OwnerLevel = 24, Replaces = "ny_baekhon" },
            new ArtData { Id = "ny_hyeollan", Name = "현란무답", Description = "한상옥령신검 비설보 오의. 눈보라 속 춤이 적 전체를 세 번 벤다.", Inflict = StatusKind.Chill, InflictChance = 0.3f, Power = 1.1f, Hits = 3, AllEnemies = true, QiCost = 170, Cooldown = 3, OwnerId = "na_yerin", OwnerLevel = 28 },
            new ArtData { Id = "ny_geommak", Name = "검막", Description = "검으로 막을 쳐 일행 모두를 방어 태세로 만든다.", Effect = SkillEffect.GuardParty, QiCost = 160, Cooldown = 4, OwnerId = "na_yerin", OwnerStage = 32, OwnerLevel = 34, Replaces = "ny_biseol" },
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
            new EnemyData { Id = "hogol_archer", Name = "호골채 궁수", Level = 6, HpMul = 0.66f, AtkMul = 0.619f, IsMinion = true,
                Moves = new[] { EnemyMove.Attack("활쏘기", 1.0f).FromDistance().Sniping(), EnemyMove.Attack("연사", 0.55f, 2).FromDistance().Sniping() } },
            new EnemyData { Id = "hogol_axe", Name = "호골채 도부수", Level = 6, HpMul = 1.08f, AtkMul = 0.619f, IsMinion = true,
                Moves = new[] { EnemyMove.Attack("도끼질", 1.0f), EnemyMove.Wind("도끼를 치켜든다"), EnemyMove.Attack("내려찍기", 2.2f) } },
            new EnemyData { Id = "paeho_jung_gwang", Name = "패호 중광", Level = 8, IsBoss = true, HpMul = 1.3f, AtkMul = 0.609f,
                Moves = new[] { EnemyMove.Attack("내려치기", 1.1f), EnemyMove.Attack("연타", 0.7f, 3), EnemyMove.Wind("기합을 모은다"), EnemyMove.Attack("일격", 2.8f) } },
            // Stage 4: 용골채 (1권)
            new EnemyData { Id = "yonggol_bandit", Name = "용골채 산적", Level = 9, HpMul = 1.013f, AtkMul = 0.678f,
                Moves = new[] { EnemyMove.Attack("휘두르기", 1.0f), EnemyMove.Attack("베기", 1.1f) } },
            new EnemyData { Id = "yonggol_archer", Name = "용골채 궁수", Level = 9, HpMul = 0.81f, AtkMul = 0.678f, IsMinion = true,
                Moves = new[] { EnemyMove.Attack("활쏘기", 1.0f).FromDistance().Sniping(), EnemyMove.Attack("연사", 0.55f, 2).FromDistance().Sniping() } },
            new EnemyData { Id = "yonggol_axe", Name = "용골채 도부수", Level = 9, HpMul = 1.32f, AtkMul = 0.678f, IsMinion = true,
                Moves = new[] { EnemyMove.Attack("도끼질", 1.0f), EnemyMove.Wind("도끼를 치켜든다"), EnemyMove.Attack("내려찍기", 2.2f) } },
            new EnemyData { Id = "paeryong_wooak", Name = "패룡 우악", Level = 11, IsBoss = true, HpMul = 2.153f, AtkMul = 0.758f,
                Moves = new[] { EnemyMove.Attack("내려치기", 1.1f), EnemyMove.Attack("연타", 0.7f, 3), EnemyMove.Wind("기세를 끌어올린다"), EnemyMove.Attack("패룡격", 3.0f) } },
            // Stage 5: 윤하루 (2권)
            new EnemyData { Id = "jang_ugang", Name = "장우강", Level = 12, HpMul = 1.333f, AtkMul = 1.254f,
                Moves = new[] { EnemyMove.Attack("청성검", 1.0f), EnemyMove.Attack("청성검", 1.2f), EnemyMove.Wind("검을 거둔다"), EnemyMove.Attack("찌르기", 2.2f) } },
            new EnemyData { Id = "jungyang_pyosa", Name = "중양표국 표사", Level = 12, HpMul = 1.2f, AtkMul = 0.75f, IsMinion = true,
                Moves = new[] { EnemyMove.Attack("베기", 1.0f), EnemyMove.Attack("찌르기", 1.1f) } },
            new EnemyData { Id = "yeomdo", Name = "염도 곽영희", Level = 14, IsBoss = true, HpMul = 3.3f, AtkMul = 1.55f,
                Moves = new[] { EnemyMove.Attack("진홍십칠염", 0.8f, 3), EnemyMove.Attack("검염기", 1.3f), EnemyMove.Wind("화령신공을 끌어올린다"), EnemyMove.Attack("화령염천도세", 3.2f) } },
            // Stage 6: 호아장 (2권)
            new EnemyData { Id = "hoajang_disciple", Name = "호아장 제자", Level = 15, HpMul = 1.383f, AtkMul = 0.821f,
                Moves = new[] { EnemyMove.Attack("맹호비격검", 1.0f), EnemyMove.Attack("맹호비격검", 1.1f) } },
            new EnemyData { Id = "hoajang_senior", Name = "호아장 고참 제자", Level = 15, HpMul = 1.73f, AtkMul = 0.86f, IsMinion = true,
                Moves = new[] { EnemyMove.Attack("맹호비격검", 1.0f), EnemyMove.Wind("검기를 모은다"), EnemyMove.Attack("맹호비격검", 2.2f) } },
            new EnemyData { Id = "ho_cheonsang", Name = "호아장주 호천상", Level = 17, IsBoss = true, HpMul = 2.195f, AtkMul = 0.825f,
                Moves = new[] { EnemyMove.Attack("맹호비격검", 1.0f, 2), EnemyMove.Attack("맹호비격검", 1.3f), EnemyMove.Wind("검기를 모은다"), EnemyMove.Attack("맹호삼십육검", 3.0f) } },
            new EnemyData { Id = "deung_yeoun", Name = "쾌창 등여운", Level = 5, HpMul = 0.78f, AtkMul = 0.38f,
                Moves = new[] { EnemyMove.Attack("쾌창", 1.05f, 1), EnemyMove.Attack("연쇄 찌르기", 0.55f, 2) } },
            new EnemyData { Id = "deung_yeoho", Name = "섬연창 등여호", Level = 5, HpMul = 0.78f, AtkMul = 0.38f,
                Moves = new[] { EnemyMove.Attack("섬연창", 0.6f, 2), EnemyMove.Attack("오단연쇄격", 0.4f, 3) } },
            new EnemyData { Id = "jang_uyang", Name = "십팔검 장우양", ArtId = "jang_uyang", Level = 6, HpMul = 1.5f, AtkMul = 0.49f, IsBoss = true,
                Moves = new[] { EnemyMove.Attack("난화검", 0.7f, 2), EnemyMove.Attack("난화수", 1.2f, 1), EnemyMove.Wind("난화검을 펼친다"), EnemyMove.Attack("난화검", 2.4f) } },
            new EnemyData { Id = "jo_yeonil", Name = "무영각 조연일", Level = 29, HpMul = 1.95f, AtkMul = 4.4f,
                Moves = new[] { EnemyMove.Attack("무영각", 0.65f, 2), EnemyMove.Attack("발차기", 1.2f, 1) } },
            new EnemyData { Id = "jin_pae", Name = "용아조 진패", Level = 29, HpMul = 2.0f, AtkMul = 4.4f,
                Moves = new[] { EnemyMove.Attack("용아조", 0.7f, 2), EnemyMove.Attack("파고들기", 1.2f, 1) } },
            new EnemyData { Id = "ilyeon", Name = "소림 일연", Level = 30, HpMul = 2.15f, AtkMul = 4.2f,
                Moves = new[] { EnemyMove.Attack("권격", 1.15f, 1), EnemyMove.Attack("연환 타격", 0.65f, 2) } },
            new EnemyData { Id = "admit_jeomchang", Name = "점창파 응시생", Level = 18, HpMul = 1.1f, AtkMul = 0.8f,
                Moves = new[] { EnemyMove.Attack("검격", 1.0f, 1), EnemyMove.Attack("연속 베기", 0.6f, 2) } },
            new EnemyData { Id = "admit_hwasan", Name = "화산파 응시생", Level = 18, HpMul = 1.15f, AtkMul = 0.82f,
                Moves = new[] { EnemyMove.Attack("검격", 1.0f, 1), EnemyMove.Attack("검격", 0.65f, 2) } },
            new EnemyData { Id = "admit_geumbok", Name = "금복산장 응시생", Level = 19, HpMul = 1.15f, AtkMul = 0.82f,
                Moves = new[] { EnemyMove.Attack("일격", 1.2f, 1), EnemyMove.Attack("연속 타격", 0.65f, 2) } },
            new EnemyData { Id = "admit_eonga", Name = "진주언가 응시생", Level = 19, HpMul = 1.2f, AtkMul = 0.85f,
                Moves = new[] { EnemyMove.Attack("창격", 1.2f, 1), EnemyMove.Attack("창격", 0.65f, 2) } },
            new EnemyData { Id = "no_jusaeng", Name = "곤륜쌍검 노주생", Level = 27, HpMul = 1.0f, AtkMul = 1.1f,
                Moves = new[] { EnemyMove.Attack("양의합벽검진", 0.65f, 2), EnemyMove.Attack("검격", 1.1f, 1) } },
            new EnemyData { Id = "chae_hagun", Name = "곤륜쌍검 채하군", Level = 27, HpMul = 1.0f, AtkMul = 1.1f,
                Moves = new[] { EnemyMove.Attack("양의합벽검진", 1.1f, 1), EnemyMove.Attack("검격", 0.65f, 2) } },
            new EnemyData { Id = "oh_muhyeong", Name = "형산파 오문형", Level = 27, HpMul = 1.25f, AtkMul = 1.3f,
                Moves = new[] { EnemyMove.Attack("검격", 1.15f, 1), EnemyMove.Attack("검격", 0.65f, 2) } },
            new EnemyData { Id = "jang_daepyeong", Name = "천웅보 장대평", Level = 27, HpMul = 1.35f, AtkMul = 1.3f,
                Moves = new[] { EnemyMove.Attack("일격", 1.2f, 1), EnemyMove.Attack("연환 타격", 0.65f, 2) } },
            new EnemyData { Id = "gwan_seolji_story", Name = "청설옥검녀 관설지", Level = 31, HpMul = 8.0f, AtkMul = 0.9f,
                Moves = new[] { EnemyMove.Attack("빙령수류검", 0.7f, 2), EnemyMove.Attack("한빙벽", 1.1f, 1) } },
            new EnemyData { Id = "cheong_heun_story", Name = "삼절검 청흔", ArtId = "cheong_heun", Level = 31, HpMul = 8.0f, AtkMul = 0.9f,
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
            new EnemyData { Id = "hwasan_jeja", Name = "화산파 제자", Level = 21, HpMul = 1.25f, AtkMul = 3.0f, IsMinion = true,
                Moves = new[] { EnemyMove.Attack("매화검법", 0.8f, 2), EnemyMove.Attack("매화검법", 1.1f) } },
            new EnemyData { Id = "chu_gayeon", Name = "추가연", Level = 23, IsBoss = true, HpMul = 2.950f, AtkMul = 3.608f,
                Moves = new[] { EnemyMove.Attack("매화검법", 0.8f, 3), EnemyMove.Attack("매화검법", 1.2f), EnemyMove.Wind("검을 모은다"), EnemyMove.Attack("매화만개", 3.0f) } },
            // Stage 9: 운향정 (3권)
            new EnemyData { Id = "gu_chang", Name = "일도파쇄 구창", Level = 23, HpMul = 2.230f, AtkMul = 5.745f,
                Moves = new[] { EnemyMove.Attack("도", 1.1f), EnemyMove.Wind("도를 치켜든다"), EnemyMove.Attack("일도파쇄", 2.6f) } },
            new EnemyData { Id = "so_cheongun", Name = "청풍검 소천군", Level = 24, HpMul = 2.127f, AtkMul = 5.745f,
                Moves = new[] { EnemyMove.Attack("청성검", 0.7f, 3), EnemyMove.Attack("청성검", 1.3f), EnemyMove.Wind("무형지기를 끌어올린다"), EnemyMove.Attack("검풍", 2.6f) } },
            new EnemyData { Id = "dobaek_member", Name = "도백회 회원", Level = 23, HpMul = 1.56f, AtkMul = 4.6f, IsMinion = true,
                Moves = new[] { EnemyMove.Attack("도", 1.0f), EnemyMove.Attack("도", 1.1f) } },
            new EnemyData { Id = "geomhon_member", Name = "검혼회 회원", Level = 23, HpMul = 1.49f, AtkMul = 4.6f, IsMinion = true,
                Moves = new[] { EnemyMove.Attack("검", 0.7f, 2), EnemyMove.Attack("검", 1.1f) } },
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
            new EnemyData { Id = "maeng_yeonho", Name = "청룡단주 맹연호", Level = 32, HpMul = 3.0f, AtkMul = 3.3f,
                Moves = new[] { EnemyMove.Attack("매화검", 0.7f, 3), EnemyMove.Wind("매화만천의 검세를 펼친다"), EnemyMove.Attack("매화만천", 2.6f) } },
            new EnemyData { Id = "cheongryong_member", Name = "청룡단원", Level = 32, HpMul = 1.25f, AtkMul = 3.0f, IsMinion = true,
                Moves = new[] { EnemyMove.Attack("검격", 0.7f, 2), EnemyMove.Attack("검격", 1.1f) } },
            new EnemyData { Id = "amheon_guard", Name = "암혼비영대 검진 살수", Level = 33, HpMul = 1.5f, AtkMul = 3.0f,
                Moves = new[] { EnemyMove.Attack("연환검", .85f, 2), EnemyMove.Wind("검진을 좁힌다"), EnemyMove.Attack("합격 일격", 2.3f) } },
            new EnemyData { Id = "amryong_salsu", Name = "암룡대 수중 살수", Level = 32, HpMul = 1.25f, AtkMul = 3.0f,
                Moves = new[] { EnemyMove.Attack("수중 비수", 0.75f, 2), EnemyMove.Attack("암습", 1.15f) } },
            new EnemyData { Id = "mak_jeok", Name = "적웅산도 막적", Level = 32, HpMul = 3.1f, AtkMul = 3.2f, DefMul = 1.1f,
                Moves = new[] { EnemyMove.Attack("산도", 1.0f), EnemyMove.Wind("도를 치켜든다"), EnemyMove.Attack("적웅산도", 2.5f) } },
            new EnemyData { Id = "jeogung_bandit", Name = "적웅채 산적", Level = 32, HpMul = 1.3f, AtkMul = 3.0f, IsMinion = true,
                Moves = new[] { EnemyMove.Attack("휘두르기", 1.0f), EnemyMove.Attack("베기", 1.1f) } },
            new EnemyData { Id = "heuk_sang", Name = "암혼비영대 흑상", Level = 33, HpMul = 3.1f, AtkMul = 3.45f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("암영검", 0.65f, 3), EnemyMove.Wind("그림자 속에서 검을 고른다"), EnemyMove.Attack("흑살도", 2.6f) } },
            new EnemyData { Id = "amyeong_member", Name = "암영조 무사", Level = 33, HpMul = 1.3f, AtkMul = 3.1f, IsMinion = true,
                Moves = new[] { EnemyMove.Attack("암영검", 0.65f, 2), EnemyMove.Attack("암습", 1.15f) } },
            new EnemyData { Id = "bikwae_ssangchang", Name = "비쾌쌍창 선봉", Level = 34, HpMul = 1.15f, AtkMul = 3.0f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("선봉 돌격", 1.0f), EnemyMove.Attack("쌍창", 0.6f, 2) } },
            new EnemyData { Id = "pae_yugyeong", RetreatHpPct = .2f, Name = "단옥도 팽유경", Level = 38, HpMul = 3.0f, AtkMul = 3.65f,
                Moves = new[] { EnemyMove.Attack("단옥도", 1.05f), EnemyMove.Wind("호위의 칼을 세운다"), EnemyMove.Attack("단옥도 일격", 2.6f) } },
            new EnemyData { Id = "hyo_ryong_rope", RetreatHpPct = .2f, ArtId = "hyo_ryong", Name = "효룡 · 줄 위 비무", Level = 41, HpMul = 3.6f, AtkMul = 5.6f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("쌍검", 0.8f, 2), EnemyMove.Wind("줄 위에서 쌍검을 교차한다"), EnemyMove.Attack("쌍검 합격", 1.6f, 2) } },
            new EnemyData { Id = "hwan_geomseong", ArtId = "geom_seong", Name = "환영 속 검성", Level = 43, HpMul = 3.0f, AtkMul = 3.0f,
                Moves = new[] { EnemyMove.Attack("나뭇가지", 1.1f), EnemyMove.Wind("검성의 기세가 다가온다"), EnemyMove.Attack("환영의 검", 2.0f) } },
            new EnemyData { Id = "hwan_gal_hyobong", ArtId = "gal_hyobong", Name = "환영 속 갈효봉", Level = 43, HpMul = 3.0f, AtkMul = 3.0f,
                Moves = new[] { EnemyMove.Attack("죄책감의 쌍도", 0.7f, 2), EnemyMove.Attack("형의 환영", 1.1f) } },
            // Stage 14: 무당산 고립 (7권)
            new EnemyData { Id = "chohon_gwigeom", RetreatHpPct = .15f, Name = "초혼오귀검 귀영살검", Level = 32, HpMul = 2.011f, AtkMul = 3.563f,
                Moves = new[] { EnemyMove.Attack("암습검", 0.8f, 2), EnemyMove.Attack("소도", 1.2f) } },
            new EnemyData { Id = "myeongbu_jang", RetreatHpPct = .15f, Name = "명부오귀장 명염도 추항", Level = 33, HpMul = 2.280f, AtkMul = 3.563f,
                Moves = new[] { EnemyMove.Attack("명부도", 1.0f), EnemyMove.Wind("도를 높이 든다"), EnemyMove.Attack("명염참", 2.6f) } },
            new EnemyData { Id = "gal_hyobong", Name = "혈류도 갈효봉(폭주)", Level = 34, HpMul = 4.291f, AtkMul = 3.799f,
                Moves = new[] { EnemyMove.Attack("광무쌍도", 0.7f, 3), EnemyMove.Attack("혈풍무", 1.3f), EnemyMove.Wind("혈광을 일으킨다"), EnemyMove.Attack("굉천혈류패왕십자인", 3.2f) } },
            new EnemyData { Id = "cheonsal", RetreatHpPct = .15f, Name = "천살 초혼검", Level = 35, IsBoss = true, HpMul = 4.023f, AtkMul = 3.799f,
                Moves = new[] { EnemyMove.Attack("초혼귀령검", 0.7f, 3), EnemyMove.Attack("소혼붕렬", 1.3f), EnemyMove.Wind("초혼령을 울린다"), EnemyMove.Attack("귀령압혼", 3.0f) } },
            new EnemyData { Id = "jisal", RetreatHpPct = .15f, Name = "지살 명왕도", Level = 35, IsBoss = true, HpMul = 4.023f, AtkMul = 3.799f,
                Moves = new[] { EnemyMove.Attack("명왕도", 1.0f), EnemyMove.Attack("명왕도", 1.2f), EnemyMove.Wind("웃는 얼굴이 굳는다"), EnemyMove.Attack("명왕참", 2.8f) } },
            // Stage 15: 철각비마대 (8권, 회상)
            new EnemyData { Id = "sadae_jilpung", Name = "사대질풍 철기병", Level = 35, HpMul = 1.335f, AtkMul = 3.656f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("철쇄선풍", 0.8f, 2), EnemyMove.Attack("거창", 1.2f) } },
            new EnemyData { Id = "cheolbi_gyeok", Name = "철비십이격 창기병", Level = 36, HpMul = 1.419f, AtkMul = 3.656f,
                Moves = new[] { EnemyMove.Attack("십자투창", 0.7f, 3), EnemyMove.Wind("창을 허리에 건다"), EnemyMove.Attack("기창충격", 2.6f) } },
            new EnemyData { Id = "wi_musang", RetreatHpPct = .2f, Name = "철각비마 위무상", Level = 36, HpMul = 1.669f, AtkMul = 3.656f,
                Moves = new[] { EnemyMove.Attack("삼단질풍격", 0.7f, 3), EnemyMove.Attack("거창돌격", 1.3f) } },
            new EnemyData { Id = "gu_cheonhak", RetreatHpPct = .2f, Name = "질풍묵흔 구천학", Level = 37, IsBoss = true, HpMul = 2.671f, AtkMul = 3.738f,
                Moves = new[] { EnemyMove.Attack("허공격상", 0.7f, 3), EnemyMove.Attack("파랑분쇄격", 1.3f), EnemyMove.Wind("창을 머리 위로 든다"), EnemyMove.Attack("무영인", 3.2f) } },
            // Stage 16: 저잣거리 습격 (8권)
            new EnemyData { Id = "baegin_jagaek", Name = "백인회 변장 자객", Level = 36, HpMul = 1.733f, AtkMul = 2.792f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("암기", 0.6f, 3).FromDistance().Sniping(), EnemyMove.Attack("암습", 1.3f).Sniping() } },
            new EnemyData { Id = "ssanggwi", Name = "이인일체 쌍귀 형제", Level = 37, HpMul = 2.477f, AtkMul = 2.851f,
                Moves = new[] { EnemyMove.Attack("쌍도", 1.0f), EnemyMove.Wind("배 속에서 손이 튀어나온다"), EnemyMove.Attack("이인일체 비수", 2.8f) } },
            new EnemyData { Id = "baek_tanjeo", RetreatHpPct = .2f, Name = "백인회주 관살 백탄저", Level = 37, IsBoss = true, HpMul = 3.715f, AtkMul = 2.851f,
                Moves = new[] { EnemyMove.Attack("독검", 0.7f, 3), EnemyMove.Attack("극독 일섬", 1.3f), EnemyMove.Wind("독검을 뽑아 든다"), EnemyMove.Attack("맹독 난자", 3.0f) } },
            // Stage 17: 마하령과 군웅회 (9권)
            new EnemyData { Id = "sinung_daewon", Name = "신응대 대원", Level = 38, HpMul = 2.152f, AtkMul = 3.791f,
                Moves = new[] { EnemyMove.Attack("철벽진", 0.8f, 2), EnemyMove.Attack("도격", 1.2f) } },
            new EnemyData { Id = "pae_hyeokseong", RetreatHpPct = .2f, Name = "광풍맹호도 팽혁성", Level = 39, IsBoss = true, HpMul = 4.3f, AtkMul = 3.858f,
                Moves = new[] { EnemyMove.Attack("오호단문도", 1.0f), EnemyMove.Wind("맹호출림의 자세"), EnemyMove.Attack("맹호출림", 2.8f) } },
            new EnemyData { Id = "ma_haryeong", Name = "철옥잠 마하령", Level = 40, IsBoss = true, HpMul = 4.305f, AtkMul = 3.858f,
                Moves = new[] { EnemyMove.Attack("표류무상도", 0.7f, 3), EnemyMove.Attack("철옥참", 1.3f), EnemyMove.Wind("천축유가신공을 끌어올린다"), EnemyMove.Attack("표류무상참", 3.2f) } },
            // Stage 18: 태극의 인재 (9권)
            new EnemyData { Id = "yong_cheonmyeong", Name = "창천룡 용천명", Level = 40, HpMul = 4.246f, AtkMul = 7.792f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("백룡검", 0.7f, 3), EnemyMove.Attack("백룡검 변초", 1.3f), EnemyMove.Wind("반야신공을 일으킨다"), EnemyMove.Attack("백룡검 일섬", 3.0f) } },
            new EnemyData { Id = "gwan_cheolsu_trial", ArtId = "gwan_cheolsu", Name = "빙검 · 북극칠성인", Level = 42, HpMul = 5.775f, AtkMul = 7.945f,
                Moves = new[] { EnemyMove.Attack("빙령수혼", .7f, 3), EnemyMove.Wind("북극칠성인의 검한기를 모은다"), EnemyMove.Attack("북극칠성인", 2.7f) } },
            new EnemyData { Id = "gwan_cheolsu", Name = "빙검 관철수", Level = 42, IsBoss = true, HpMul = 5.775f, AtkMul = 7.945f,
                Moves = new[] { EnemyMove.Attack("빙령수혼", 0.7f, 3), EnemyMove.Attack("백빙신첨", 1.3f), EnemyMove.Wind("한빙지기를 끌어올린다"), EnemyMove.Attack("백빙신정", 3.4f) } },
            // Stage 19: 환마동 시험 (10권)
            new EnemyData { Id = "hwan_ying", Name = "환마동 환영", Level = 43, HpMul = 2.418f, AtkMul = 3.257f,
                Moves = new[] { EnemyMove.Attack("조롱", 0.8f, 2), EnemyMove.Attack("덮치기", 1.2f) } },
            new EnemyData { Id = "hwan_family_duty", ArtId = "im_deokseong", Name = "환영 속 아버지의 기대", Level = 43, HpMul = 3.0f, AtkMul = 3.0f,
                Moves = new[] { EnemyMove.Attack("후계의 무게", .8f, 2), EnemyMove.Wind("기대가 길을 막는다"), EnemyMove.Attack("도적의 길", 2.1f) } },
            new EnemyData { Id = "hwan_nameless_blade", Name = "환영 속 이름 없는 검", Level = 43, HpMul = 3.0f, AtkMul = 3.0f,
                Moves = new[] { EnemyMove.Attack("지워지지 않는 기억", 1.0f), EnemyMove.Wind("기억 속 검이 다가온다"), EnemyMove.Attack("악몽의 검", 2.1f) } },
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
            new EnemyData { Id = "im_deokseong_story", RetreatHpPct = .15f, ArtId = "im_deokseong", Name = "녹림왕 임덕성", Level = 49, HpMul = 3.4f, AtkMul = 3.1f,
                Moves = new[] { EnemyMove.Attack("부자의 공방", .8f, 2), EnemyMove.Wind("큰 도끼를 들어 올린다"), EnemyMove.Attack("녹림왕의 도끼", 2.5f) } },
            new EnemyData { Id = "hyeol_chang", Name = "혈창", Level = 52, HpMul = 2.2f, AtkMul = 3.0f,
                Moves = new[] { EnemyMove.Attack("가로막는 창", .8f, 2), EnemyMove.Wind("창끝을 겨눈다"), EnemyMove.Attack("혈창의 찌르기", 2.5f) } },
            new EnemyData { Id = "maehwa_mask_trial", RetreatHpPct = .15f, Name = "매화가면의 시험", Level = 53, HpMul = 3.8f, AtkMul = 3.4f,
                Moves = new[] { EnemyMove.Attack("매화의 문답", .8f, 2), EnemyMove.Wind("매화검의 검로를 펼친다"), EnemyMove.Attack("매염토망", 2.5f) } },
            new EnemyData { Id = "cheonsal_hwapyeong", ArtId = "cheonsal", Name = "외팔 천살", Level = 56, HpMul = 2.5f, AtkMul = 2.97f, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("귀곡의 합격", .8f, 2), EnemyMove.Wind("남은 왼팔로 검을 든다"), EnemyMove.Attack("천지합살", 2.5f) } },
            new EnemyData { Id = "jisal_hwapyeong", ArtId = "jisal", Name = "외팔 지살", Level = 56, HpMul = 2.5f, AtkMul = 2.97f, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("합격의 빈틈", .8f, 2), EnemyMove.Wind("남은 오른팔로 도를 든다"), EnemyMove.Attack("천지합살", 2.5f) } },
            new EnemyData { Id = "heukrang_bandit", Name = "흑랑채 산적", Level = 47, HpMul = 2.331f, AtkMul = 3.218f,
                Moves = new[] { EnemyMove.Attack("흑랑조", 0.8f, 2), EnemyMove.Attack("도끼질", 1.2f) } },
            new EnemyData { Id = "lee_songhak", RetreatHpPct = .15f, IsBoss = true, Name = "군자소요검 이송학", Level = 48, HpMul = 3.017f, AtkMul = 3.218f,
                Moves = new[] { EnemyMove.Attack("소요검", 0.8f, 2), EnemyMove.Wind("수비 자세를 푼다"), EnemyMove.Attack("군자일검", 2.6f) } },
            new EnemyData { Id = "im_gae", RetreatHpPct = .15f, Name = "흑랑부 임개", Level = 49, IsBoss = true, HpMul = 4.936f, AtkMul = 3.310f,
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
            new EnemyData { Id = "cheonghong_ssanggaksa", RetreatHpPct = .15f, Name = "청홍쌍각사", Level = 52, HpMul = 3.158f, AtkMul = 2.970f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("독니", 0.7f, 3), EnemyMove.Wind("요광을 모은다"), EnemyMove.Attack("요광", 2.6f) } },
            new EnemyData { Id = "mo_saryeong", RetreatHpPct = .15f, Name = "비사마군 모사령", Level = 53, IsBoss = true, HpMul = 5.131f, AtkMul = 2.970f,
                Moves = new[] { EnemyMove.Attack("만사혈장", 0.7f, 3), EnemyMove.Attack("비사진", 1.3f), EnemyMove.Wind("사충적을 입에 문다"), EnemyMove.Attack("지사진", 3.3f) } },
            // Stage 24: 비뢰쌍마 (12권)
            new EnemyData { Id = "dongbang_hak", Name = "섬뢰마검 동방학", Level = 54, HpMul = 5.313f, AtkMul = 4.197f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("붉은 검", 0.7f, 3), EnemyMove.Attack("섬뢰", 1.3f), EnemyMove.Wind("쾌검의 자세를 취한다"), EnemyMove.Attack("섬뢰일섬", 3.0f) } },
            new EnemyData { Id = "gong_pae", RetreatHpPct = .15f, Name = "잔뢰마도 공패", Level = 55, IsBoss = true, HpMul = 7.438f, AtkMul = 4.197f,
                Moves = new[] { EnemyMove.Attack("잔뢰도", 0.8f, 2), EnemyMove.Attack("잔뢰삼십육패", 1.3f), EnemyMove.Wind("도를 높이 든다"), EnemyMove.Attack("잔뢰번천지", 3.4f) } },
            // Stage 25: 화평장 (13권)
            new EnemyData { Id = "hwapyeong_soldier", Name = "화평장 훈련병", Level = 55, HpMul = 2.686f, AtkMul = 2.971f,
                Moves = new[] { EnemyMove.Attack("연환검", 0.8f, 2), EnemyMove.Attack("포위 돌격", 1.2f) } },
            new EnemyData { Id = "ssangsal_hapgyeok", Name = "천지쌍살 합격술(한 팔씩)", Level = 56, HpMul = 4.523f, AtkMul = 2.971f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("초혼귀령검", 0.7f, 3), EnemyMove.Attack("명왕도", 1.3f), EnemyMove.Wind("쌍살이 서로의 빈 팔을 맞댄다"), EnemyMove.Attack("쌍살 합격", 3.0f) } },
            new EnemyData { Id = "baek_son_gosu", RetreatHpPct = .15f, Name = "하얀 손의 고수", Level = 57, IsBoss = true, HpMul = 5.937f, AtkMul = 3.063f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("단월인", 0.7f, 3), EnemyMove.Attack("무형 칼날", 1.3f), EnemyMove.Wind("하얀 손을 편다"), EnemyMove.Attack("단월인 극의", 3.3f) } },
            new EnemyData { Id = "sura_screen", Name = "십이수라쇄성진 무사", Level = 57, HpMul = 2.2f, AtkMul = 2.6f, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("퇴로 봉쇄", .8f, 2), EnemyMove.Wind("검진을 좁힌다"), EnemyMove.Attack("연계 검격", 2.5f) } },
            new EnemyData { Id = "hwapyeong_breakout", Name = "화평장 봉쇄 무사", Level = 57, HpMul = 2f, AtkMul = 2.1f, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("퇴로 봉쇄검", .8f, 2), EnemyMove.Wind("동료와 검을 맞춘다"), EnemyMove.Attack("봉쇄 일격", 2.5f) } },
            new EnemyData { Id = "bi_first_clash", ArtId = "bi_daegongja", Name = "어둠 속의 고수", Level = 58, HpMul = 10f, AtkMul = 2.7f, SpeedAdd = 4, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("휘선의 변화", .8f, 2), EnemyMove.Wind("영뢰의 궤도를 바꾼다"), EnemyMove.Attack("영뢰 천잔", 2.8f) } },
            new EnemyData { Id = "salhon_sipgwi", Name = "살귀대장 살혼 십귀", Level = 58, IsBoss = true, HpMul = 5.834f, AtkMul = 2.672f, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("추격 지휘", .7f, 3), EnemyMove.Attack("포위검", 1.3f), EnemyMove.Wind("살귀대의 포위를 좁힌다"), EnemyMove.Attack("추격대 합격", 3.2f) } },
            new EnemyData { Id = "mu_jeong_public", ArtId = "mu_jeong", Name = "은형의 무정", Level = 60, HpMul = 4f, AtkMul = 3.8f, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("은신검", .7f, 3), EnemyMove.Wind("그림자 속에 몸을 숨긴다"), EnemyMove.Attack("기척 없는 찌르기", 2.7f) } },
            new EnemyData { Id = "gyook_public", ArtId = "gyo_ok", Name = "혈심란 교옥", Level = 60, HpMul = 4f, AtkMul = 3.8f, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("오색 투침", .7f, 3).FromDistance().Sniping(), EnemyMove.Wind("실바늘을 펼친다"), EnemyMove.Attack("혈심란의 침", 2.7f).FromDistance() } },
            new EnemyData { Id = "cliff_rescue", Name = "절벽의 낙하와 기류", Level = 63, HpMul = 30f, AtkMul = 1.5f, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("낙하의 압력", .8f, 2), EnemyMove.Wind("날카로운 기류가 손끝을 가른다"), EnemyMove.Attack("낙하 충격", 2.4f) } },
            new EnemyData { Id = "macheon_samhyung", Name = "마천삼흉", Level = 63, HpMul = 2.2f, AtkMul = 2.3f, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("거점 습격", .8f, 2), EnemyMove.Wind("흉기를 치켜든다"), EnemyMove.Attack("난폭한 일격", 2.5f) } },
            new EnemyData { Id = "witness_pursuer", ArtId = "suda_bokmyeon", Name = "증인을 쫓는 복면인", Level = 65, IsBoss = true, HpMul = 6f, AtkMul = 2.335f, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("추격검", .7f, 3), EnemyMove.Attack("검은 장갑", 1.3f), EnemyMove.Wind("호위의 틈을 노린다"), EnemyMove.Attack("증인 말살", 3f) } },
            new EnemyData { Id = "hwaryong_rescue", ArtId = "hwaryong_hwayeom", Name = "꺼지지 않는 화룡", Level = 66, HpMul = 40f, AtkMul = 1.5f, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("솟구치는 열기", .8f, 2).FromDistance(), EnemyMove.Wind("마지막 폭발의 압력이 밀려온다"), EnemyMove.Attack("화룡의 압력", 2.5f).FromDistance() } },
            // Stage 26: 살귀대 (14권)
            new EnemyData { Id = "salgwi_daewon", Name = "살귀대원", Level = 57, HpMul = 2.778f, AtkMul = 2.672f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("추격검", 0.8f, 2), EnemyMove.Attack("암살", 1.3f) } },
            new EnemyData { Id = "ho_yugwang", Name = "화평장주 호유광", Level = 58, IsBoss = true, HpMul = 5.834f, AtkMul = 2.672f,
                Moves = new[] { EnemyMove.Attack("살귀 지휘검", 0.7f, 3), EnemyMove.Attack("독조", 1.3f), EnemyMove.Wind("살귀대에 신호를 보낸다"), EnemyMove.Attack("살귀 일제 공격", 3.2f) } },
            // Stage 27: 마천칠걸 (14권)
            new EnemyData { Id = "jongri_chu", RetreatHpPct = .15f, Name = "칠련창 종리추", Level = 59, HpMul = 3.735f, AtkMul = 4.167f,
                Moves = new[] { EnemyMove.Attack("칠성창", 0.7f, 3), EnemyMove.Wind("일곱 번째 창을 겨눈다"), EnemyMove.Attack("북두칠성 투창", 2.7f) } },
            new EnemyData { Id = "do_chuun", RetreatHpPct = .15f, Name = "사갈검편 도추운", Level = 59, HpMul = 3.447f, AtkMul = 4.167f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("검편 채찍", 0.7f, 3), EnemyMove.Attack("사갈편", 1.3f) } },
            new EnemyData { Id = "oh_munchu", RetreatHpPct = .15f, Name = "쇄풍겸 오문추", Level = 60, HpMul = 3.735f, AtkMul = 4.283f,
                Moves = new[] { EnemyMove.Attack("사슬낫", 0.8f, 2), EnemyMove.Wind("사슬을 휘감는다"), EnemyMove.Attack("쇄풍 난무", 2.8f) } },
            new EnemyData { Id = "baek_sayeong", RetreatHpPct = .15f, Name = "사교검 백사영", Level = 60, HpMul = 3.735f, AtkMul = 4.283f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("사교검", 0.7f, 3), EnemyMove.Wind("뱀처럼 몸을 낮춘다"), EnemyMove.Attack("백사교룡살", 2.9f) } },
            new EnemyData { Id = "chu_myeong", RetreatHpPct = .15f, Name = "마검익 추명", Level = 61, IsBoss = true, HpMul = 6.320f, AtkMul = 4.283f,
                Moves = new[] { EnemyMove.Attack("회선인", 0.7f, 3), EnemyMove.Attack("흑응익", 1.3f), EnemyMove.Wind("흑풍을 일으킨다"), EnemyMove.Attack("흑풍난무", 3.3f) } },
            // Stage 28: 멸성대 (15권)
            new EnemyData { Id = "mulsung_daewon", Name = "멸성대 대원", Level = 61, HpMul = 2.879f, AtkMul = 2.464f,
                Moves = new[] { EnemyMove.Attack("살인검", 0.8f, 2), EnemyMove.Attack("검익진", 1.3f) } },
            new EnemyData { Id = "i_gae_bujo", Name = "멸성대 부조장 이개", Level = 62, HpMul = 3.663f, AtkMul = 2.514f,
                Moves = new[] { EnemyMove.Attack("호위검", 0.8f, 2), EnemyMove.Wind("만근검익진을 펼친다"), EnemyMove.Attack("만근추", 2.7f) } },
            new EnemyData { Id = "sama_heun", Name = "천참마검 사마흔", Level = 63, IsBoss = true, HpMul = 6.018f, AtkMul = 2.514f,
                Moves = new[] { EnemyMove.Attack("천참검", 0.7f, 3), EnemyMove.Attack("독검", 1.3f), EnemyMove.Wind("검을 뽑아 든다"), EnemyMove.Attack("천겁 일섬", 3.4f) } },
            // Stage 29: 중토관 (16권)
            new EnemyData { Id = "suda_bokmyeon", RetreatHpPct = .15f, Name = "수다쟁이 복면인", Level = 64, HpMul = 4.476f, AtkMul = 2.282f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("가벼운 검", 0.7f, 3), EnemyMove.Wind("호들갑을 떨며 몸을 낮춘다"), EnemyMove.Attack("도우미의 일격", 2.8f) } },
            new EnemyData { Id = "mu_jeong", RetreatHpPct = .15f, Name = "잔무일점혈 무정", Level = 65, IsBoss = true, HpMul = 7.161f, AtkMul = 2.335f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("그림자 비수", 0.7f, 3).FromDistance().Sniping(), EnemyMove.Attack("무언독 암기", 1.3f).FromDistance().Sniping(), EnemyMove.Wind("그림자 속으로 스며든다"), EnemyMove.Attack("일점혈", 3.5f) } },
            // Stage 30: 화룡멸겁대진 (16권)
            new EnemyData { Id = "ma_chilgeol_jeoji", RetreatHpPct = .15f, Name = "마천칠걸 저지조", Level = 65, HpMul = 3.459f, AtkMul = 2.145f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("연환 공격", 0.7f, 3), EnemyMove.Attack("협공", 1.3f) } },
            new EnemyData { Id = FireDragonFinale.JinId, ArtId = "hwaryong_hwayeom", Name = "화룡멸겁대진", Level = 66, StoryOnly = true, FixedHp = 999999,
                Moves = new[] { EnemyMove.Attack(InteractiveBattle.FinaleBlowName, 1f).FromDistance() } },
            new EnemyData { Id = "hwaryong_hwayeom", Name = "화룡의 불꽃", Level = 66, HpMul = 4.257f, AtkMul = 2.145f,
                Moves = new[] { EnemyMove.Attack("불의 성벽", 0.8f, 2).FromDistance(), EnemyMove.Wind("불길이 솟구친다"), EnemyMove.Attack("화룡의 숨결", 2.9f).FromDistance() } },
            new EnemyData { Id = "bi_daegongja", Name = "대공자 비", Level = 67, IsBoss = true, RetreatHpPct = 0.3f, HpMul = 7.492f, AtkMul = 2.593f, DefMul = 1.15f, SpeedAdd = 14,
                Moves = new[] { EnemyMove.Attack("삼중 잔영", 0.7f, 3), EnemyMove.Attack("검기 난무", 1.3f), EnemyMove.Wind("영뢰를 모은다"), EnemyMove.Attack("영뢰 잔흔", 3.6f) } },
            // Stage 29 trials: 검후의 십초지적, 수화관 (16권)
            new EnemyData { Id = "geomhu_ioksang", RetreatHpPct = .15f, Name = "검후 이옥상", Level = 66, HpMul = 45.426f, AtkMul = 4.804f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("체대", 0.8f, 2), EnemyMove.Attack("체대 일격", 1.3f), EnemyMove.Wind("검집에 손을 얹는다"), EnemyMove.Attack("해상비조천참절", 2.8f) } },
            new EnemyData { Id = "heuksaek_amhyeok", Name = "검은 암벽", Level = 63, HpMul = 4.103f, AtkMul = 0.400f,
                Moves = new[] { EnemyMove.Attack("진동", 0.5f) } },
            new EnemyData { Id = "wiji_hyeolpok", ArtId = "wiji_cheon", RetreatHpPct = .15f, Name = "혈폭환 위지천", Level = 64, HpMul = 4.103f, AtkMul = 2.668f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("폭주 진기", 0.7f, 3), EnemyMove.Attack("혈폭 난사", 1.3f), EnemyMove.Wind("혈폭환의 기운이 끓어오른다"), EnemyMove.Attack("생명을 불사른 일격", 3.0f) } },
            // Stage 29 trial: 은가면 삼성의 습격 (16권). Unkillable; the party only has to endure.
            new EnemyData { Id = "mask_gongpo", RetreatHpPct = .15f, Name = "은가면 「공포」", Level = 66, HpMul = 45.426f, AtkMul = 2.133f,
                Moves = new[] { EnemyMove.Attack("분신 일검", 0.8f, 2), EnemyMove.Attack("백발의 일검", 1.3f), EnemyMove.Wind("분신이 하나둘 늘어난다"), EnemyMove.Attack("공포의 일검", 2.8f) } },
            new EnemyData { Id = "mask_jeolmang", RetreatHpPct = .15f, Name = "은가면 「절망」", Level = 66, HpMul = 45.426f, AtkMul = 2.133f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("나선장", 0.8f, 2), EnemyMove.Attack("나선 돌풍", 1.2f), EnemyMove.Wind("나선의 힘을 모은다"), EnemyMove.Attack("옷을 찢는 나선", 2.6f) } },
            new EnemyData { Id = "mask_bitan", RetreatHpPct = .15f, Name = "은가면 「비탄」", Level = 66, HpMul = 45.426f, AtkMul = 2.133f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("허상 일검", 0.7f, 3), EnemyMove.Attack("나뭇가지 일검", 1.3f), EnemyMove.Wind("심정청도의 검기를 모은다"), EnemyMove.Attack("비탄의 일검", 2.8f) } },
            // Stage 28 trial: 목요관 야간 질주 (15권)
            new EnemyData { Id = "jongri_chu_night", RetreatHpPct = .15f, Name = "칠련창 종리추", Level = 59, HpMul = 2.779f, AtkMul = 2.951f,
                Moves = new[] { EnemyMove.Attack("칠성창", 0.7f, 3), EnemyMove.Attack("일곱 창 투척", 1.2f).FromDistance() } },
            new EnemyData { Id = "do_chuun_night", RetreatHpPct = .15f, Name = "사갈검편 도추운", Level = 59, HpMul = 2.617f, AtkMul = 2.951f, SpeedAdd = 4,
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
            new EnemyData { Id = "cheongryong_escape", Name = "가짜 통로의 기관", Level = 66, HpMul = 45f, AtkMul = 1.2f,
                Moves = new[] { EnemyMove.Attack("기관의 진동", .7f), EnemyMove.Wind("가짜 통로의 문이 열린다"), EnemyMove.Attack("무너지는 석벽", 1.8f) } },
            new EnemyData { Id = "salryong_yun", ArtId = "yun_ijeong", Name = "살룡", Level = 67, IsBoss = true, HpMul = 30f, AtkMul = 1.8f, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("참룡해체", .8f, 2), EnemyMove.Wind("청룡검의 빈틈을 노린다"), EnemyMove.Attack("청룡낙지", 2.4f) } },
            new EnemyData { Id = "caravan_ambush", Name = "산길의 매복 기척", Level = 67, HpMul = 45f, AtkMul = 1f,
                Moves = new[] { EnemyMove.Wind("수풀에서 살기가 번진다"), EnemyMove.Attack("가로막힌 길", .8f) } },
            new EnemyData { Id = "ichon_training", Name = "일촌간파의 비도", Level = 67, HpMul = 45f, AtkMul = 1.2f,
                Moves = new[] { EnemyMove.Attack("이어지는 비도", .6f, 2).FromDistance(), EnemyMove.Wind("다음 비도가 날아온다"), EnemyMove.Attack("일곱 비도의 틈", 1.8f).FromDistance() } },
            new EnemyData { Id = "gwan_eogeom_first", ArtId = "gwan_cheolsu", Name = "빙검 · 이기어검의 첫 검", Level = 67, HpMul = 45f, AtkMul = 1.5f, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("이기어검", .8f, 2).FromDistance(), EnemyMove.Wind("검영이 둘로 나뉜다"), EnemyMove.Attack("어검분영술", 2.2f).FromDistance() } },
            new EnemyData { Id = "gongson_ambush", Name = "황금 완장을 노리는 검객", Level = 70, HpMul = 30f, AtkMul = 1.8f, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("야습의 검", .8f, 2), EnemyMove.Wind("지존검의 기세를 모은다"), EnemyMove.Attack("지존검법", 2.4f) } },
            new EnemyData { Id = "i_sigeon_arrow", ArtId = "i_sigeon", Name = "흑응비섬의 이시건", Level = 71, HpMul = 30f, AtkMul = 1.8f, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("자운의 검", .8f, 2), EnemyMove.Wind("흑응비섬을 준비한다"), EnemyMove.Attack("흑응비섬", 2.5f) } },
            new EnemyData { Id = "closed_room_threat", Name = "차음막 안의 위협", Level = 71, HpMul = 45f, AtkMul = .8f,
                Moves = new[] { EnemyMove.Wind("숙부의 목소리가 차갑게 바뀐다"), EnemyMove.Attack("문 앞을 덮는 살기", .6f) } },
            new EnemyData { Id = "jin_soryeong_two", ArtId = "jin_soryeong", Name = "진소령 · 이분영", Level = 72, HpMul = 45f, AtkMul = 1.5f, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("이분영", .8f, 2).FromDistance(), EnemyMove.Wind("옥현의 검영이 갈라진다"), EnemyMove.Attack("비상련화", 2.4f).FromDistance() } },
            new EnemyData { Id = "jin_soryeong_four", ArtId = "jin_soryeong", Name = "진소령 · 사분영", Level = 72, HpMul = 45f, AtkMul = 1.7f, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("사련화영", .6f, 4).FromDistance(), EnemyMove.Wind("사방으로 검영이 퍼진다"), EnemyMove.Attack("사분영의 검", 2.4f).FromDistance() } },
            // Stages 31-32: escort defense, Namgung's solo bout and limited training (volumes 17-18).
            new EnemyData { Id = "salryong_daewon", Name = "살룡대 복면 무사", Level = 66, HpMul = 2.100f, AtkMul = 3.400f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("복면 도격", 0.8f, 2), EnemyMove.Attack("협살", 1.3f) } },
            new EnemyData { Id = "cham_yeong", Name = "살룡대 참영", Level = 66, HpMul = 2.500f, AtkMul = 4.000f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("그림자 베기", 0.7f, 3), EnemyMove.Wind("그림자를 겹친다"), EnemyMove.Attack("참영도", 2.8f) } },
            new EnemyData { Id = "gyeok_un", Name = "살룡대 격운", Level = 66, HpMul = 2.500f, AtkMul = 4.000f,
                Moves = new[] { EnemyMove.Attack("격운장", 0.8f, 2), EnemyMove.Wind("구름 같은 장력을 모은다"), EnemyMove.Attack("격운파", 2.7f) } },
            new EnemyData { Id = "hyeol_pyo", Name = "살룡대 수석조장 혈표", Level = 67, IsBoss = true, HpMul = 3.500f, AtkMul = 3.300f, SpeedAdd = 8,
                Moves = new[] { EnemyMove.Attack("혈조", 0.7f, 3), EnemyMove.Attack("목 꺾기", 1.3f), EnemyMove.Wind("살기를 끌어올린다"), EnemyMove.Attack("혈표 일섬", 3.4f) } },
            new EnemyData { Id = "yu_ran", RetreatHpPct = .15f, Name = "아미 제자 유란", Level = 66, HpMul = 1.100f, AtkMul = 1.200f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("연화검법", 0.7f, 3), EnemyMove.Attack("연화 일섬", 1.3f) } },
            new EnemyData { Id = "yu_unbi", RetreatHpPct = .15f, Name = "점창 유운비", Level = 66, HpMul = 1.100f, AtkMul = 1.200f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("사일검법", 0.7f, 3), EnemyMove.Wind("찌르기를 노린다"), EnemyMove.Attack("사일 찌르기", 2.6f) } },
            new EnemyData { Id = "gwan_cheolsu_eogeom", RetreatHpPct = .15f, ArtId = "gwan_cheolsu", Name = "빙검 관철수 (특훈)", Level = 67, IsBoss = true, HpMul = 45.426f, AtkMul = 1.800f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("이기어검", 0.8f, 2).FromDistance(), EnemyMove.Attack("어검분영술", 1.3f).FromDistance(), EnemyMove.Wind("검을 허공에 띄운다"), EnemyMove.Attack("비검 일섬", 2.8f).FromDistance() } },
            // Stage 33: Biryueon's solo encounters, then Namgung's armband ambush (volume 19).
            new EnemyData { Id = "i_sigeon", RetreatHpPct = .15f, Name = "마천각 이공자 이시건", Level = 69, HpMul = 45.426f, AtkMul = 2.100f, SpeedAdd = 8,
                Moves = new[] { EnemyMove.Attack("손가락 검기", 0.8f, 2).FromDistance(), EnemyMove.Attack("자운", 1.3f), EnemyMove.Wind("자운암풍이 소용돌이친다"), EnemyMove.Attack("광풍난주", 2.8f) } },
            new EnemyData { Id = "ipgwan_dojeonja", RetreatHpPct = .15f, Name = "입관 희망자", Level = 68, HpMul = 2.100f, AtkMul = 3.700f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("명문 검법", 0.8f, 2), EnemyMove.Attack("완장 노리기", 1.3f) } },
            new EnemyData { Id = "chimbaetneun_ja", RetreatHpPct = .15f, Name = "침 뱉은 도전자", Level = 68, HpMul = 1.500f, AtkMul = 1.600f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("시비", 0.7f, 3), EnemyMove.Wind("침을 뱉는다"), EnemyMove.Attack("악다구니 일격", 2.7f) } },
            // Stages 34-36: fixed perspectives follow each encounter's actual participants (volume 20).
            new EnemyData { Id = "jungwon_pyosa", Name = "중원표국 표사", Level = 70, HpMul = 2.100f, AtkMul = 3.400f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("표창", 0.8f, 2).FromDistance(), EnemyMove.Attack("박도 내려치기", 1.3f) } },
            new EnemyData { Id = "o_yeong", Name = "오가 형제 오영", Level = 70, HpMul = 2.500f, AtkMul = 3.700f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("단도술", 0.7f, 3), EnemyMove.Wind("단도를 거꾸로 쥔다"), EnemyMove.Attack("쌍단도 교차", 2.7f) } },
            new EnemyData { Id = "o_gi", Name = "오가 형제 오기", Level = 70, HpMul = 2.500f, AtkMul = 3.700f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("단도술", 0.7f, 3), EnemyMove.Wind("형과 등을 맞댄다"), EnemyMove.Attack("쌍단도 교차", 2.7f) } },
            new EnemyData { Id = "yun_ijeong", RetreatHpPct = .15f, Name = "풍마도 윤이정", Level = 70, HpMul = 3.000f, AtkMul = 3.700f, SpeedAdd = 2,
                Moves = new[] { EnemyMove.Attack("풍마도법", 0.8f, 2), EnemyMove.Wind("삼현조화의 도막을 세운다"), EnemyMove.Attack("풍마귀혼", 2.8f) } },
            new EnemyData { Id = "i_sigeon_jungwon", ArtId = "i_sigeon", Name = "마천각 이공자 이시건", Level = 71, IsBoss = true, RetreatHpPct = 0.3f, HpMul = 6.500f, AtkMul = 3.500f, SpeedAdd = 10,
                Moves = new[] { EnemyMove.Attack("주박인", 0.7f, 3), EnemyMove.Attack("사검 그물", 1.3f), EnemyMove.Wind("열 근 철토시를 벗는다"), EnemyMove.Attack("삭풍참살인", 3.5f) } },
            new EnemyData { Id = "i_sigeon_bait", RetreatHpPct = .15f, ArtId = "i_sigeon", Name = "자운 암풍 이시건", Level = 71, HpMul = 45.426f, AtkMul = 4.600f, SpeedAdd = 10,
                Moves = new[] { EnemyMove.Attack("질풍참살", 0.8f, 2), EnemyMove.Attack("사검 그물", 1.3f), EnemyMove.Wind("흑응의 기세를 모은다"), EnemyMove.Attack("흑응비섬", 2.8f) } },
            new EnemyData { Id = "sipsamhyeol", Name = "십삼혈 살인광", Level = 71, HpMul = 45f, AtkMul = 2.600f, SpeedAdd = 4,
                Moves = new[] { EnemyMove.Attack("혈설십삼식", 0.8f, 2), EnemyMove.Attack("살인 취미", 1.3f) } },
            new EnemyData { Id = "il_hyeol", Name = "십삼혈의 우두머리 일혈", Level = 72, IsBoss = true, HpMul = 45f, AtkMul = 2.600f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("혈설십삼식", 0.7f, 3), EnemyMove.Attack("핏빛 손톱", 1.3f), EnemyMove.Wind("혈왕살을 끌어올린다"), EnemyMove.Attack("혈왕살", 3.5f) } },
            new EnemyData { Id = "geomjon_gongson", RetreatHpPct = .15f, Name = "검존 공손일취", Level = 72, HpMul = 45.426f, AtkMul = 1.800f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("노검존의 일검", 0.8f, 2), EnemyMove.Attack("분노의 검", 1.3f), EnemyMove.Wind("손자의 이름에 검을 뽑는다"), EnemyMove.Attack("지존검법", 2.8f) } },
            new EnemyData { Id = "jin_soryeong", Name = "아미신녀 진소령", Level = 72, IsBoss = true, RetreatHpPct = 0.2f, HpMul = 45.426f, AtkMul = 1.800f, SpeedAdd = 8,
                Moves = new[] { EnemyMove.Attack("이분영", 0.8f, 2).FromDistance(), EnemyMove.Attack("사련화영", 0.6f, 4).FromDistance(), EnemyMove.Wind("옥현이 허공에 떠오른다"), EnemyMove.Attack("팔련화린 난화만천", 0.45f, 8).FromDistance() } },
            new EnemyData { Id = "cheong_heun_mugeuk", RetreatHpPct = .15f, ArtId = "cheong_heun", Name = "삼절검 청흔", Level = 70, IsBoss = true, HpMul = 2.200f, AtkMul = .900f, SpeedAdd = 6,
                Moves = new[] { EnemyMove.Attack("삼환회선비검", 0.7f, 3), EnemyMove.Attack("태극검", 1.3f), EnemyMove.Wind("삼정태극검혜를 펼친다"), EnemyMove.Attack("무극검 일관", 3.5f) } },
            new EnemyData { Id = "baekgyo_scales", Name = "백교 · 단단한 비늘", ArtId = "baekgyo", Level = 30, HpMul = 45f, AtkMul = 0.8f, IsBoss = false, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("꼬리치기", .8f), EnemyMove.Wind("둥지를 향해 몸을 튼다"), EnemyMove.Attack("비늘의 압박", 1.8f) } },
            new EnemyData { Id = "baekgyo_opening", Name = "백교 · 관통의 틈", ArtId = "baekgyo", Level = 30, HpMul = 0.8f, AtkMul = 0.8f, IsBoss = true, RetreatHpPct = .2f,
                Moves = new[] { EnemyMove.Attack("물어뜯기", .7f), EnemyMove.Wind("빈 눈을 감싸며 몸을 낮춘다"), EnemyMove.Attack("거대한 꼬리", 1.6f) } },
            new EnemyData { Id = "ok_yugyeong_reunion", Name = "옥유경 · 혈풍란", ArtId = "ok_yugyeong", Level = 76, HpMul = 45f, AtkMul = 1.3f, IsBoss = false, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("혈풍란", .25f, 7).FromDistance(), EnemyMove.Wind("붉은 촉이 방향을 바꾼다"), EnemyMove.Attack("일곱 촉의 공세", 2f).FromDistance() } },
            new EnemyData { Id = "gongson_thirteenth", Name = "공손절휘 · 대장 선출전", ArtId = "gongson_jeolhwi", Level = 76, HpMul = 1.1f, AtkMul = 1.2f, IsBoss = false, RetreatHpPct = .2f,
                Moves = new[] { EnemyMove.Attack("지존검법", .7f, 2), EnemyMove.Wind("극예를 고쳐 쥔다"), EnemyMove.Attack("정면의 일검", 2.2f) } },
            new EnemyData { Id = "yong_cheongmyeong_vote", Name = "용천명 · 대장 선출전", ArtId = "yong_cheonmyeong", Level = 76, HpMul = 1.8f, AtkMul = 1.1f, IsBoss = true, RetreatHpPct = .2f,
                Moves = new[] { EnemyMove.Attack("달마십삼검", .6f, 3), EnemyMove.Wind("녹옥여래검을 바로 세운다"), EnemyMove.Attack("정석의 검로", 2.2f) } },
            new EnemyData { Id = "donwang_guard", Name = "돈왕의 계단 호위", Level = 78, HpMul = 0.7f, AtkMul = 1.1f, IsBoss = false, RetreatHpPct = .2f,
                Moves = new[] { EnemyMove.Attack("계단 봉쇄", .8f, 2), EnemyMove.Attack("호위 도격", 1.3f) } },
            new EnemyData { Id = "georyeoksin", Name = "금종조 거력신", ArtId = "georyeoksin", Level = 78, HpMul = 1.4f, AtkMul = 1.2f, IsBoss = true, RetreatHpPct = .2f,
                Moves = new[] { EnemyMove.Attack("거력장", 1.2f), EnemyMove.Wind("금종조를 세운다"), EnemyMove.Attack("전신 압박", 2.5f) } },
            new EnemyData { Id = "donwang_hobeop", Name = "돈왕의 호법", Level = 78, HpMul = 0.9f, AtkMul = 1.1f, IsBoss = false, RetreatHpPct = .2f,
                Moves = new[] { EnemyMove.Attack("호법 검격", .7f, 2), EnemyMove.Wind("문 앞을 막는다"), EnemyMove.Attack("협공", 2.2f) } },
            new EnemyData { Id = "sipmyeon_arrows", Name = "십면매복 · 연노", Level = 80, HpMul = 45f, AtkMul = 1.2f, IsBoss = false, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("연노 화살비", .6f, 3).FromDistance(), EnemyMove.Wind("천리경의 시선이 옮겨 간다"), EnemyMove.Attack("교차 사격", 2.2f).FromDistance() } },
            new EnemyData { Id = "sipmyeon_water", Name = "십면매복 · 수중 합격", Level = 80, HpMul = 45f, AtkMul = 1.2f, IsBoss = false, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("다리 밑 창", .8f, 2).FromDistance(), EnemyMove.Wind("다리가 끊어지고 물결이 솟는다"), EnemyMove.Attack("고아격살", 2.3f).FromDistance() } },
            new EnemyData { Id = "heuksak_capture", Name = "흑삭포룡진", Level = 80, HpMul = 45f, AtkMul = 1.3f, IsBoss = false, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("흑룡삭 봉쇄", .7f, 2).FromDistance(), EnemyMove.Wind("포위망을 좁힌다"), EnemyMove.Attack("포룡의 올가미", 2.4f).FromDistance() } },
            new EnemyData { Id = "heuksak_break", Name = "흑삭구두룡살진", Level = 80, HpMul = 1.5f, AtkMul = 0.9f, IsBoss = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("아홉 갈래 삭영", .2f, 9).FromDistance(), EnemyMove.Wind("진법 한가운데로 줄이 모인다"), EnemyMove.Attack("구두룡살진", 2.6f).FromDistance() } },
            new EnemyData { Id = "baekmu_ju", Name = "백무후 · 달의 샘", Level = 82, HpMul = 1.8f, AtkMul = 1.1f, IsBoss = true, RetreatHpPct = .2f,
                Moves = new[] { EnemyMove.Attack("백호조", .7f, 2), EnemyMove.Wind("발톱에 검기가 맺힌다"), EnemyMove.Attack("호조 검기", 2.8f) } },
            new EnemyData { Id = "heukryong_anchor", Name = "흑룡왕 · 철혈묘", Level = 84, HpMul = 30f, AtkMul = 1.3f, IsBoss = false, ArtId = "heukryong_wang", StoryOnly = true, RetreatHpPct = 0.15f,
                Moves = new[] { EnemyMove.Attack("해폭난파", .5f, 3), EnemyMove.Wind("흙파도가 솟는다"), EnemyMove.Attack("광풍노도", 2.3f) } },
            new EnemyData { Id = "heukryong_shell_one", Name = "흑룡왕 · 첫 벽력포", Level = 84, HpMul = 45f, AtkMul = 1.2f, IsBoss = false, ArtId = "heukryong_wang", StoryOnly = true, RetreatHpPct = 0.15f,
                Moves = new[] { EnemyMove.Wind("철혈묘 포구에 불꽃이 맺힌다"), EnemyMove.Attack("첫 벽력포", 2.4f).FromDistance() } },
            new EnemyData { Id = "heukryong_shell_two", Name = "흑룡왕 · 특제 뇌탄", Level = 84, HpMul = 45f, AtkMul = 1.2f, IsBoss = false, ArtId = "heukryong_wang", StoryOnly = true, RetreatHpPct = 0.15f,
                Moves = new[] { EnemyMove.Wind("두 번째 뇌탄이 장전된다"), EnemyMove.Attack("특제 뇌탄", 2.6f).FromDistance() } },
            new EnemyData { Id = "heukryong_muzzle", Name = "흑룡왕 · 마지막 포구", Level = 84, HpMul = 45f, AtkMul = 1.2f, IsBoss = true, ArtId = "heukryong_wang", StoryOnly = true, RetreatHpPct = 0.15f,
                Moves = new[] { EnemyMove.Wind("셋째 벽력포를 준비한다"), EnemyMove.Attack("마지막 벽력포", 2.8f).FromDistance() } },
            new EnemyData { Id = "yecheong_left", Name = "빙월선자 예청 · 빙월난무", Level = 86, HpMul = 45f, AtkMul = 1.4f, IsBoss = false, ArtId = "ye_cheong", StoryOnly = true, RetreatHpPct = 0.15f,
                Moves = new[] { EnemyMove.Attack("빙월난무", .5f, 3), EnemyMove.Wind("빙월이 호를 그린다"), EnemyMove.Attack("언월의 공세", 2.4f) } },
            new EnemyData { Id = "yecheong_twins", Name = "빙월선자 예청 · 십이야", Level = 86, HpMul = 45f, AtkMul = 1.5f, IsBoss = true, ArtId = "ye_cheong", StoryOnly = true, RetreatHpPct = 0.15f,
                Moves = new[] { EnemyMove.Attack("쌍월야", .7f, 2), EnemyMove.Wind("두 언월의 길이 갈라진다"), EnemyMove.Attack("십이야", .25f, 12) } },
            new EnemyData { Id = "unstable_black_lightning", Name = "폭주하는 오른팔의 기류", Level = 86, HpMul = 45f, AtkMul = 0.8f, IsBoss = false, StoryOnly = true, RetreatHpPct = 0f,
                Moves = new[] { EnemyMove.Wind("검은 번개가 방향을 잃는다"), EnemyMove.Attack("흩어진 기류", .8f) } },
            new EnemyData { Id = "georyeok_wang", Name = "거력왕", Level = 88, HpMul = 45f, AtkMul = 1.2f, IsBoss = false, StoryOnly = true, RetreatHpPct = 0.15f,
                Moves = new[] { EnemyMove.Attack("풍압권", 1.1f), EnemyMove.Wind("주먹에 바람이 모인다"), EnemyMove.Attack("거력권", 2.4f) } },
            new EnemyData { Id = "geodo_tournament", Name = "거도", Level = 88, HpMul = 1.4f, AtkMul = 1.1f, IsBoss = false, StoryOnly = true, RetreatHpPct = 0f,
                Moves = new[] { EnemyMove.Attack("거도 찌르기", 1.2f), EnemyMove.Wind("칼의 축을 세운다"), EnemyMove.Attack("정면 돌진", 2.3f) } },
            new EnemyData { Id = "ha_usu", Name = "의호 하우수", Level = 88, HpMul = 30f, AtkMul = 1f, IsBoss = false, ArtId = "ha_usu", StoryOnly = true, RetreatHpPct = 0.2f,
                Moves = new[] { EnemyMove.Wind("개복 소도를 꺼낸다"), EnemyMove.Attack("마비산 침", .6f, 2).FromDistance() } },
            new EnemyData { Id = "chae_isu", Name = "문진 채이수", Level = 88, HpMul = 1.2f, AtkMul = 1f, IsBoss = false, ArtId = "chae_isu", StoryOnly = true, RetreatHpPct = 0.2f,
                Moves = new[] { EnemyMove.Attack("소도", .7f, 2), EnemyMove.Wind("마비 안개를 준비한다"), EnemyMove.Attack("마비 안개", 2f).FromDistance() } },
            new EnemyData { Id = "jin_ryeong_tournament", Name = "진령 · 비상련화", Level = 88, HpMul = 45f, AtkMul = 1.3f, IsBoss = true, ArtId = "jin_ryeong", StoryOnly = true, RetreatHpPct = 0.15f,
                Moves = new[] { EnemyMove.Attack("이분영 쌍화엽", .7f, 2).FromDistance(), EnemyMove.Wind("검영이 네 갈래로 피어난다"), EnemyMove.Attack("사련화영", .6f, 4).FromDistance() } },
            new EnemyData { Id = "mong_mu", Name = "몽무", Level = 90, HpMul = 1f, AtkMul = 1f, IsBoss = false, ArtId = "mong_mu", StoryOnly = true, RetreatHpPct = 0.15f,
                Moves = new[] { EnemyMove.Attack("몽환쌍영무", .7f, 2), EnemyMove.Wind("최면 가루를 흩뿌린다"), EnemyMove.Attack("몽영", 2f) } },
            new EnemyData { Id = "hwan_mu", Name = "환무", Level = 90, HpMul = 1f, AtkMul = 1f, IsBoss = false, ArtId = "hwan_mu", StoryOnly = true, RetreatHpPct = 0.15f,
                Moves = new[] { EnemyMove.Attack("쌍영무", .7f, 2), EnemyMove.Wind("소리와 검로가 어긋난다"), EnemyMove.Attack("환영", 2f) } },
            new EnemyData { Id = "yeongryeong_soan", Name = "영령 · 소안검의 기억", Level = 90, HpMul = 45f, AtkMul = 1.1f, IsBoss = false, ArtId = "dokgo_ryeong", StoryOnly = true, RetreatHpPct = 0.15f,
                Moves = new[] { EnemyMove.Attack("몽환사령검법", .7f, 2), EnemyMove.Wind("몸이 먼저 반응한다"), EnemyMove.Attack("어긋난 검음", 2.2f) } },
            new EnemyData { Id = "yeongryeong_ice", Name = "영령 · 검각의 한빙기", Level = 90, HpMul = 45f, AtkMul = 1.2f, IsBoss = false, ArtId = "dokgo_ryeong", StoryOnly = true, RetreatHpPct = 0.15f,
                Moves = new[] { EnemyMove.Attack("사령검", .7f, 2), EnemyMove.Wind("한빙기가 검 위에 맺힌다"), EnemyMove.Attack("기억의 검", 2.2f) } },
            new EnemyData { Id = "yeongryeong_flute", Name = "영령 · 혈흔적 이후", Level = 90, HpMul = 45f, AtkMul = 1.3f, IsBoss = true, ArtId = "dokgo_ryeong", StoryOnly = true, RetreatHpPct = 0.15f,
                Moves = new[] { EnemyMove.Attack("폭주의 검", .6f, 3), EnemyMove.Wind("붉은 눈이 흔들린다"), EnemyMove.Attack("심령금제의 잔향", 2.4f) } },
            new EnemyData { Id = "chilsang_broken", Name = "칠상흔 · 반 토막 도", Level = 92, HpMul = 1.6f, AtkMul = 1.2f, IsBoss = false, ArtId = "chil_sangheun", StoryOnly = true, RetreatHpPct = 0.2f,
                Moves = new[] { EnemyMove.Attack("혈강", .7f, 2), EnemyMove.Wind("반보 더 들어온다"), EnemyMove.Attack("붉은 도강", 2.5f) } },
            new EnemyData { Id = "chilsang_mist", Name = "칠상흔 · 혈무막막", Level = 92, HpMul = 45f, AtkMul = 1.3f, IsBoss = false, ArtId = "chil_sangheun", StoryOnly = true, RetreatHpPct = 0.15f,
                Moves = new[] { EnemyMove.Wind("혈무막막의 강기 안개가 모인다"), EnemyMove.Attack("혈류십이도", 2.5f) } },
            new EnemyData { Id = "chilsang_twins", Name = "칠상흔 · 쌍도와 혈십자", Level = 92, HpMul = 45f, AtkMul = 1.4f, IsBoss = false, ArtId = "chil_sangheun", StoryOnly = true, RetreatHpPct = 0.15f,
                Moves = new[] { EnemyMove.Attack("화선십자인", .7f, 2), EnemyMove.Wind("쌍도에 혈강이 교차한다"), EnemyMove.Attack("굉천혈류패왕십자인", 2.8f) } },
            new EnemyData { Id = "unsealed_current", Name = "손목의 묵룡환 · 흐름", Level = 92, HpMul = 45f, AtkMul = 0.8f, IsBoss = false, StoryOnly = true, RetreatHpPct = 0f,
                Moves = new[] { EnemyMove.Wind("억눌렀던 기가 길을 찾는다"), EnemyMove.Attack("흐름의 압력", 1f) } },
            new EnemyData { Id = "chilsang_heart", Name = "칠상흔 · 천외일도", Level = 92, HpMul = 45f, AtkMul = 1.4f, IsBoss = true, ArtId = "chil_sangheun", StoryOnly = true, RetreatHpPct = 0.15f,
                Moves = new[] { EnemyMove.Wind("거리를 넘어 심도가 다가온다"), EnemyMove.Attack("천외일도", 3f) } },
            new EnemyData { Id = "vent_traces", Name = "빈 통로의 흔적", Level = 94, HpMul = 45f, AtkMul = 0.6f, IsBoss = false, StoryOnly = true, RetreatHpPct = 0f,
                Moves = new[] { EnemyMove.Wind("환기 통로의 바람이 흐른다"), EnemyMove.Attack("잠긴 경계", .6f) } },
            new EnemyData { Id = "horn_trap", Name = "뿔 장식과 폭염진", Level = 94, HpMul = 45f, AtkMul = 0.8f, IsBoss = false, StoryOnly = true, RetreatHpPct = 0f,
                Moves = new[] { EnemyMove.Wind("밀실에 열기가 번진다"), EnemyMove.Attack("폭염진의 압력", 1f) } },
            new EnemyData { Id = "academy_alarm_guard", Name = "마천각 정문 경비", Level = 96, HpMul = 0.8f, AtkMul = 1.1f, IsBoss = false, StoryOnly = true, RetreatHpPct = 0.15f,
                Moves = new[] { EnemyMove.Attack("정문 제압", .7f, 2), EnemyMove.Attack("경계 도격", 1.3f) } },
            new EnemyData { Id = "cheolgap_front", Name = "철갑사패 · 전위", Level = 96, HpMul = 0.9f, AtkMul = 1.2f, IsBoss = false, ArtId = "cheolgap_sapae", StoryOnly = true, RetreatHpPct = 0.15f,
                Moves = new[] { EnemyMove.Attack("일격필살", 1.2f), EnemyMove.Wind("철갑의 틈을 가린다"), EnemyMove.Attack("수문장의 주먹", 2.3f) } },
            new EnemyData { Id = "cheolgap_rear", Name = "철갑사패 · 후위", Level = 96, HpMul = 0.9f, AtkMul = 1.2f, IsBoss = true, ArtId = "cheolgap_sapae", StoryOnly = true, RetreatHpPct = 0.15f,
                Moves = new[] { EnemyMove.Attack("철신관 봉쇄", .7f, 2), EnemyMove.Wind("뒤의 두 권이 모인다"), EnemyMove.Attack("철갑사패 합격", 2.3f) } },
            new EnemyData { Id = "rebound_probe", Name = "강순천갑 · 작은 충격", Level = 96, HpMul = 45f, AtkMul = 0.6f, IsBoss = false, StoryOnly = true, RetreatHpPct = 0f,
                Moves = new[] { EnemyMove.Wind("판이 다시 붙기를 기다린다"), EnemyMove.Attack("강기의 압력", .7f) } },
            new EnemyData { Id = "rebound_second", Name = "강순천갑 · 반응 확인", Level = 96, HpMul = 45f, AtkMul = 0.6f, IsBoss = false, StoryOnly = true, RetreatHpPct = 0f,
                Moves = new[] { EnemyMove.Wind("금 안에서 타격을 준비한다"), EnemyMove.Attack("강기의 압박", .7f) } },
            new EnemyData { Id = "rakbio_five", Name = "락비오 · 다섯 주먹", ArtId = "rakbio", Level = 98, HpMul = 45f, AtkMul = 0.8f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("철갑파쇄권", .8f) } },
            new EnemyData { Id = "rakbio_air", Name = "락비오 · 돌림판 일", ArtId = "rakbio", Level = 98, HpMul = 35f, AtkMul = 1f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("기를 모은다"), EnemyMove.Attack("공세", 1.5f) } },
            new EnemyData { Id = "stone_grid", Name = "석괴압살관 · 번호 낙하", Level = 98, HpMul = 50f, AtkMul = 0.6f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("37·47번은 가운데·오른쪽 칸이다"), EnemyMove.Attack("37·47번 석추 낙하", 1.5f) } },
            new EnemyData { Id = "rakbio_armor", Name = "락비오 · 강순천갑", ArtId = "rakbio", Level = 98, HpMul = 45f, AtkMul = 1f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("갑옷을 두르고 돌진한다"), EnemyMove.Attack("폭렬질주", 1.5f) } },
            new EnemyData { Id = "rakbio_shards", Name = "락비오 · 폭렬잔", ArtId = "rakbio", Level = 98, HpMul = 45f, AtkMul = 0.9f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("갑옷의 파편이 터져 나온다"), EnemyMove.Attack("폭렬잔", 1.5f) } },
            new EnemyData { Id = "rakbio_mind", Name = "락비오 · 아버지의 그림자", ArtId = "rakbio", Level = 98, HpMul = 35f, AtkMul = 1f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("기를 모은다"), EnemyMove.Attack("공세", 1.5f) } },
            new EnemyData { Id = "rakbio_bronze", Name = "락비오 · 청동의 반탄강", ArtId = "rakbio", Level = 98, HpMul = 45f, AtkMul = 1.1f, IsBoss = true, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("충격을 오른 주먹에 모은다"), EnemyMove.Attack("반탄강", 1.5f) } },
            new EnemyData { Id = "jagun_fan", Name = "자군 · 홍선", ArtId = "jagun", Level = 100, HpMul = 3.3f, AtkMul = 1.2f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("홍선의 검로", .8f), EnemyMove.Wind("기를 모은다"), EnemyMove.Attack("공세", 1.5f) } },
            new EnemyData { Id = "jagun_rose", Name = "자군 · 홍장미편", ArtId = "jagun", Level = 100, HpMul = 35f, AtkMul = 0.8f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("숨은 가시를 펼친다"), EnemyMove.Attack("홍장미란풍", 1.5f) } },
            new EnemyData { Id = "jagun_heat", Name = "자군 · 산화무영", ArtId = "jagun", Level = 100, HpMul = 40f, AtkMul = 0.7f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("발화한 꽃잎이 아지랑이를 만든다"), EnemyMove.Attack("격화", 1.5f) } },
            new EnemyData { Id = "jagun_union", Name = "자군 · 격화의 장벽", ArtId = "jagun", Level = 100, HpMul = 40f, AtkMul = 1f, IsBoss = true, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("열기와 환영이 겹친다"), EnemyMove.Attack("야마란풍", 1.5f) } },
            new EnemyData { Id = "merchant_oh", Name = "오대붕", Level = 102, HpMul = 1.5f, AtkMul = 1.1f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("강매의 위협", .8f), EnemyMove.Wind("기를 모은다"), EnemyMove.Attack("공세", 1.5f) } },
            new EnemyData { Id = "merchant_jeong", Name = "정홍돈", Level = 102, HpMul = 1.2f, AtkMul = 1f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("검로 봉쇄", .8f), EnemyMove.Wind("기를 모은다"), EnemyMove.Attack("공세", 1.5f) } },
            new EnemyData { Id = "merchant_yeo", Name = "여홍아", Level = 102, HpMul = 1.3f, AtkMul = 1f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("옷자락의 암습", .8f), EnemyMove.Wind("기를 모은다"), EnemyMove.Attack("공세", 1.5f) } },
            new EnemyData { Id = "false_south_king", Name = "가짜 남해왕", Level = 102, HpMul = 2f, AtkMul = 1.1f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("금융대란 국가부도", .8f), EnemyMove.Wind("기를 모은다"), EnemyMove.Attack("공세", 1.5f) } },
            new EnemyData { Id = "jeonhon_coins", Name = "전혼 · 탈명매혼전", ArtId = "jeonhon", Level = 102, HpMul = 40f, AtkMul = 0.8f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("일곱 동전을 겨눈다"), EnemyMove.Attack("칠연비성격", 1.5f) } },
            new EnemyData { Id = "jeonhon_bag", Name = "전혼 · 파산선고", ArtId = "jeonhon", Level = 102, HpMul = 45f, AtkMul = 0.9f, IsBoss = true, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("은전 주머니를 모두 펼친다"), EnemyMove.Attack("탈혼지천락 파산선고", 1.5f) } },
            new EnemyData { Id = "north_route_guard", Name = "북해도 길목의 대원", Level = 104, HpMul = 1.5f, AtkMul = 1f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("길목 봉쇄", .8f), EnemyMove.Wind("기를 모은다"), EnemyMove.Attack("공세", 1.5f) } },
            new EnemyData { Id = "sealed_meridians", Name = "육육쇄봉익금칩폐맥대법", Level = 104, HpMul = 50f, AtkMul = 0.4f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("물방울 반향이 벽 너머로 번진다"), EnemyMove.Attack("막힌 기맥의 압박", 1.5f) } },
            new EnemyData { Id = "coffin_assassin", Name = "목관 속 암습자", Level = 106, HpMul = 40f, AtkMul = 0.8f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("관의 깊이가 맞지 않는다"), EnemyMove.Attack("이중 바닥의 암검", 1.5f) } },
            new EnemyData { Id = "three_currents", Name = "원기·좌수룡·약력의 충돌", Level = 106, HpMul = 50f, AtkMul = 0.5f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("소생단 약력이 밀려온다"), EnemyMove.Attack("엇갈리는 세 기운", 1.5f) } },
            new EnemyData { Id = "west_deputy", Name = "철신대 부대장", Level = 106, HpMul = 40f, AtkMul = 0.8f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("부대장이 검을 들고 달려든다"), EnemyMove.Attack("호법선 침입", 1.5f) } },
            new EnemyData { Id = "armored_wolf", Name = "창랑대 철갑늑대", Level = 108, HpMul = 2.2f, AtkMul = 0.9f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("철갑늑대의 돌진", .8f), EnemyMove.Wind("기를 모은다"), EnemyMove.Attack("공세", 1.5f) } },
            new EnemyData { Id = "wolf_array", Name = "창랑멸아진", Level = 108, HpMul = 45f, AtkMul = 0.8f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("육섬이 사방에서 교차한다"), EnemyMove.Attack("아랑파멸진", 1.5f) } },
            new EnemyData { Id = "changrang_shadows", Name = "창랑 · 팔영잔뢰", ArtId = "changrang", Level = 108, HpMul = 45f, AtkMul = 0.9f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("여덟 분신이 길을 나눈다"), EnemyMove.Attack("팔영잔뢰 혈풍식", 1.5f) } },
            new EnemyData { Id = "changrang_final", Name = "마랑객 창랑", ArtId = "changrang", Level = 108, HpMul = 45f, AtkMul = 1f, IsBoss = true, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("발톱과 삼섬각이 이어진다"), EnemyMove.Attack("마랑혈풍조", 1.5f) } },
            new EnemyData { Id = "changranga", Name = "창랑아", Level = 108, HpMul = 40f, AtkMul = 0.8f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("창랑아가 등을 노린다"), EnemyMove.Attack("칼을 문 푸른 늑대", 1.5f) } },
            new EnemyData { Id = "last_needles", Name = "남은 금침과 닫힌 기맥", Level = 110, HpMul = 50f, AtkMul = 0.3f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("기를 모은다"), EnemyMove.Attack("공세", 1.5f) } },
            new EnemyData { Id = "cell_watch", Name = "장오영·이칠영의 감시", Level = 110, HpMul = 40f, AtkMul = 0.4f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("식사 당번이 교대한다"), EnemyMove.Attack("살피는 시선", 1.5f) } },
            new EnemyData { Id = "mumyeong_escape", Name = "무명 · 창고 앞의 조우", ArtId = "mumyeong", Level = 110, HpMul = 60f, AtkMul = 0.6f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("읽을 수 없는 마음 앞에 검이 나온다"), EnemyMove.Attack("백년녹의 검로", 1.5f) } },
            new EnemyData { Id = "mumyeong_steps", Name = "무명 · 허공순보", ArtId = "mumyeong", Level = 110, HpMul = 60f, AtkMul = 0.7f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("나예린을 안은 그림자가 멀어진다"), EnemyMove.Attack("허공순보", 1.5f) } },
            new EnemyData { Id = "mumyeong_three", Name = "무명 · 삼초지적", ArtId = "mumyeong", Level = 110, HpMul = 70f, AtkMul = 0.7f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("다음 수 · 무흔권"), EnemyMove.Attack("무흔권", 1.0f), EnemyMove.Wind("다음 수 · 무상장"), EnemyMove.Attack("무상장", 1.0f), EnemyMove.Wind("다음 수 · 무형검"), EnemyMove.Attack("무형검", 1.0f) } },
            new EnemyData { Id = "dorm_siege", Name = "허당과 숙소 포위망", Level = 112, HpMul = 45f, AtkMul = 0.7f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("기를 모은다"), EnemyMove.Attack("공세", 1.5f) } },
            new EnemyData { Id = "seocheon_delay", Name = "서천 · 항구의 위협", ArtId = "na_ilcheon", Level = 112, HpMul = 60f, AtkMul = 0.9f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("서풍광란이 퇴로를 좁힌다"), EnemyMove.Attack("유린조아", 1.5f) } },
            new EnemyData { Id = "first_unit", Name = "일번대 봉쇄대", Level = 112, HpMul = 2.8f, AtkMul = 0.8f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Attack("출항 봉쇄", .8f), EnemyMove.Wind("기를 모은다"), EnemyMove.Attack("공세", 1.5f) } },
            new EnemyData { Id = "seocheon_whirl", Name = "서천 · 검은 용권", ArtId = "na_ilcheon", Level = 112, HpMul = 60f, AtkMul = 0.9f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("서풍의 광시곡이 소용돌이친다"), EnemyMove.Attack("광풍요란", 1.5f) } },
            new EnemyData { Id = "seocheon_scar", Name = "서천 · 천형인의 상흔", ArtId = "na_ilcheon", Level = 112, HpMul = 60f, AtkMul = 0.9f, IsBoss = true, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("열등이라는 말에 분노가 향한다"), EnemyMove.Attack("파심장", 1.5f) } },
            new EnemyData { Id = "lake_crossing", Name = "출항한 배까지의 물길", Level = 114, HpMul = 45f, AtkMul = 0.5f, IsBoss = false, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("기를 모은다"), EnemyMove.Attack("공세", 1.5f) } },
            new EnemyData { Id = "lake_pursuit", Name = "귀문십장과 십이호법의 추격선", Level = 114, HpMul = 60f, AtkMul = 0.8f, IsBoss = true, StoryOnly = true, RetreatHpPct = .15f,
                Moves = new[] { EnemyMove.Wind("호수 위 추격선이 좁혀 온다"), EnemyMove.Attack("고속선 포위", 1.5f) } },
            new EnemyData { Id = "departing_ferry", Name = "출항선의 물결", Level = 116, HpMul = 40f, AtkMul = 0.3f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false,
                Moves = new[] { EnemyMove.Wind("배가 선착장에서 멀어진다"), EnemyMove.Attack("선체의 흔들림", 1.5f) } },
            new EnemyData { Id = "hubei_escort", Name = "호북 지국의 표행 준비", Level = 116, HpMul = 40f, AtkMul = 0.3f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false,
                Moves = new[] { EnemyMove.Wind("무창으로 갈 표사들이 모인다"), EnemyMove.Attack("흑천령의 검문 시각", 1.5f) } },
            new EnemyData { Id = "south_checkpoint", Name = "남문의 검문", Level = 116, HpMul = 40f, AtkMul = 0.4f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false,
                Moves = new[] { EnemyMove.Wind("흑견이 표행 앞을 막는다"), EnemyMove.Attack("삿갓을 벗으라는 요구", 1.5f) } },
            new EnemyData { Id = "ruined_contacts", Name = "무너진 네 거점의 흔적", Level = 118, HpMul = 40f, AtkMul = 0.3f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false,
                Moves = new[] { EnemyMove.Wind("다음 공세를 준비한다"), EnemyMove.Attack("끊긴 연락", 1.5f) } },
            new EnemyData { Id = "last_report", Name = "상현이 남긴 최후 보고", Level = 118, HpMul = 40f, AtkMul = 0.3f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false,
                Moves = new[] { EnemyMove.Wind("다음 공세를 준비한다"), EnemyMove.Attack("지워져 가는 흔적", 1.5f) } },
            new EnemyData { Id = "inn_encirclement", Name = "신용산객잔의 포위", Level = 118, HpMul = 40f, AtkMul = 0.5f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false,
                Moves = new[] { EnemyMove.Wind("일행이 잠든 객잔을 에워싼다"), EnemyMove.Attack("객잔을 조이는 발소리", 1.5f) } },
            new EnemyData { Id = "son_gwang_patrol", Name = "쾌영도 손광", Level = 120, HpMul = 3f, AtkMul = 1f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = true, ArtId = "son_gwang",
                Moves = new[] { EnemyMove.Wind("순찰단이 객잔으로 돌입한다"), EnemyMove.Attack("쾌영만천", 1.5f) } },
            new EnemyData { Id = "twins_fire_l", Name = "갈효린 · 이화합격진", Level = 120, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false, ArtId = "gal_hyo_rin",
                Moves = new[] { EnemyMove.Wind("왼쪽 화염이 밀려온다"), EnemyMove.Attack("홍염의 방패", 1.5f) } },
            new EnemyData { Id = "twins_fire_r", Name = "갈효진 · 건패염진", Level = 120, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false, ArtId = "gal_hyo_jin",
                Moves = new[] { EnemyMove.Wind("오른쪽 불꽃 창을 세운다"), EnemyMove.Attack("염창아돌", 1.5f) } },
            new EnemyData { Id = "muhwa_drain_fire", Name = "무화 · 구련흡성결", Level = 120, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false, ArtId = "mu_hwa",
                Moves = new[] { EnemyMove.Wind("검은 손이 염도의 천령개로 향한다"), EnemyMove.Attack("염도의 기를 붙든다", 1.5f) } },
            new EnemyData { Id = "muhwa_drain_ice", Name = "무화 · 붙잡힌 검", Level = 120, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false, ArtId = "mu_hwa",
                Moves = new[] { EnemyMove.Wind("빙령지순이 갈라지고 검이 붙들린다"), EnemyMove.Attack("빙검의 진기를 거둔다", 1.5f) } },
            new EnemyData { Id = "heukgyeon_howl", Name = "흑견 · 흑견후", Level = 122, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false, ArtId = "heukgyeon",
                Moves = new[] { EnemyMove.Wind("음파가 성문 앞에서 부풀어 오른다"), EnemyMove.Attack("흑견후", 1.5f) } },
            new EnemyData { Id = "heukgyeon_net", Name = "흑견 · 박룡삼십육식", Level = 122, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false, ArtId = "heukgyeon",
                Moves = new[] { EnemyMove.Wind("검은 포승줄을 펼친다"), EnemyMove.Attack("흑오박룡삭", 1.5f) } },
            new EnemyData { Id = "heukgyeon_gate", Name = "흑견 · 흑살호신강기", Level = 122, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = true, ArtId = "heukgyeon",
                Moves = new[] { EnemyMove.Wind("호신강기를 두르고 남문을 막는다"), EnemyMove.Attack("박룡삼십육식", 1.5f) } },
            new EnemyData { Id = "south_gate_bars", Name = "남문의 봉쇄", Level = 122, HpMul = 40f, AtkMul = 0.5f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false,
                Moves = new[] { EnemyMove.Wind("빗장이 성문을 고정한다"), EnemyMove.Attack("성문 봉쇄", 1.5f) } },
            new EnemyData { Id = "tracking_scent", Name = "흑견대의 추적", Level = 124, HpMul = 40f, AtkMul = 0.4f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false,
                Moves = new[] { EnemyMove.Wind("알림 방울이 울린다"), EnemyMove.Attack("긴장의 냄새를 좇는다", 1.5f) } },
            new EnemyData { Id = "third_dog_squad", Name = "흑견대 제삼대", Level = 124, HpMul = 0.7f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false,
                Moves = new[] { EnemyMove.Wind("소리가 나는 방향으로 달린다"), EnemyMove.Attack("추격 포위", 1.5f) } },
            new EnemyData { Id = "baekgu_signal", Name = "백구의 휘파람", Level = 124, HpMul = 40f, AtkMul = 0.5f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false,
                Moves = new[] { EnemyMove.Wind("백구가 점혈을 풀려 한다"), EnemyMove.Attack("휘파람 신호", 1.5f) } },
            new EnemyData { Id = "heukgyeon_captive", Name = "흑견 · 위장한 부상병", Level = 124, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = true, ArtId = "heukgyeon",
                Moves = new[] { EnemyMove.Wind("장홍을 포승줄로 붙든다"), EnemyMove.Attack("쌍룡양박", 1.5f) } },
            new EnemyData { Id = "silent_duet", Name = "효민·효효 · 무음탄주", Level = 126, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false, ArtId = "gal_hyo_duet",
                Moves = new[] { EnemyMove.Wind("옥소와 은금이 공진을 모은다"), EnemyMove.Attack("무음탄주", 1.5f) } },
            new EnemyData { Id = "rising_duet", Name = "효민·효효 · 다시 오른 합주", Level = 126, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false, ArtId = "gal_hyo_duet",
                Moves = new[] { EnemyMove.Wind("끊긴 음이 더 높이 이어진다"), EnemyMove.Attack("회복된 합주", 1.5f) } },
            new EnemyData { Id = "duet_peak", Name = "효민·효효 · 합주의 절정", Level = 126, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false, ArtId = "gal_hyo_duet",
                Moves = new[] { EnemyMove.Wind("소리가 최고조에 닿으려 한다"), EnemyMove.Attack("합주 공진", 1.5f) } },
            new EnemyData { Id = "exit_dog_squad", Name = "맹견과 출구의 포위망", Level = 126, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false,
                Moves = new[] { EnemyMove.Wind("흑견대 제이대가 나무 아래를 좁힌다"), EnemyMove.Attack("출구 봉쇄", 1.5f) } },
            new EnemyData { Id = "hyohyo_spear", Name = "갈효효 · 구련창", Level = 128, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false, ArtId = "gal_hyo_hyo",
                Moves = new[] { EnemyMove.Wind("여든한 찌르기가 창끝에 모인다"), EnemyMove.Attack("구련창", 1.5f) } },
            new EnemyData { Id = "hyohyo_storm", Name = "갈효효 · 은아 질풍아", Level = 128, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = true, ArtId = "gal_hyo_hyo",
                Moves = new[] { EnemyMove.Wind("은풍광영난무가 눈보라를 흩는다"), EnemyMove.Attack("은아 질풍아", 1.5f) } },
            new EnemyData { Id = "hyomin_binding", Name = "갈효민 · 무혼초령음", Level = 128, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false, ArtId = "gal_hyo_min",
                Moves = new[] { EnemyMove.Wind("천음신마선율이 의지를 누른다"), EnemyMove.Attack("검을 놓으라는 명령", 1.5f) } },
            new EnemyData { Id = "hyomyo_smoke", Name = "갈효묘 · 인질극의 틈", Level = 128, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false, ArtId = "gal_hyo_myo",
                Moves = new[] { EnemyMove.Wind("풀숲에서 효묘가 뛰어나온다"), EnemyMove.Attack("연막 속 추격", 1.5f) } },
            new EnemyData { Id = "three_sects", Name = "흑선문·적선문·백선문의 대치", Level = 130, HpMul = 40f, AtkMul = 0.4f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false,
                Moves = new[] { EnemyMove.Wind("다음 공세를 준비한다"), EnemyMove.Attack("문파 사이의 의심", 1.5f) } },
            new EnemyData { Id = "distrust_corridor", Name = "천라지망 안의 불신", Level = 130, HpMul = 40f, AtkMul = 0.5f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false,
                Moves = new[] { EnemyMove.Wind("흑저·나전·정오삼의 검이 교차한다"), EnemyMove.Attack("번지는 문파의 충돌", 1.5f) } },
            new EnemyData { Id = "sealed_hint", Name = "암시 봉인과 붉은 남천", Level = 130, HpMul = 40f, AtkMul = 0.3f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false,
                Moves = new[] { EnemyMove.Wind("다음 공세를 준비한다"), EnemyMove.Attack("흐릿해진 은신처의 기억", 1.5f) } },
            new EnemyData { Id = "dae_nan_current", Name = "대난원 앞의 급류", Level = 130, HpMul = 40f, AtkMul = 0.6f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false,
                Moves = new[] { EnemyMove.Wind("물 아래의 길이 잠시 드러난다"), EnemyMove.Attack("급류의 소용돌이", 1.5f) } },
            new EnemyData { Id = "mothers_fire_ice", Name = "단혜·사란 · 불꽃과 얼음", Level = 132, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false,
                Moves = new[] { EnemyMove.Wind("불꽃과 얼음이 포위를 메운다"), EnemyMove.Attack("절대빙옥과 운석낙하", 1.5f) } },
            new EnemyData { Id = "muhwa_hostage", Name = "무화 · 되찾아야 할 나백천", Level = 132, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false, ArtId = "mu_hwa",
                Moves = new[] { EnemyMove.Wind("나백천의 목을 붙든 손에 기가 모인다"), EnemyMove.Attack("격물흡성", 1.5f) } },
            new EnemyData { Id = "flame_prison", Name = "단혜 · 염벽 옥쇄", Level = 132, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false, ArtId = "dan_hye",
                Moves = new[] { EnemyMove.Wind("기름 고랑의 삼매진화가 조여 온다"), EnemyMove.Attack("염벽 옥쇄", 1.5f) } },
            new EnemyData { Id = "ice_dragon_wall", Name = "사란 · 절대빙벽", Level = 132, HpMul = 40f, AtkMul = 0.7f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = true, ArtId = "sa_ran",
                Moves = new[] { EnemyMove.Wind("화룡 앞에 얼음의 벽이 선다"), EnemyMove.Attack("절대빙벽", 1.5f) } },
            new EnemyData { Id = "rescue_exit", Name = "나백천을 옮길 탈출로", Level = 132, HpMul = 40f, AtkMul = 0.6f, StoryOnly = true, RetreatHpPct = .15f, IsBoss = false,
                Moves = new[] { EnemyMove.Wind("불꽃 사이의 길이 열린다"), EnemyMove.Attack("포위망의 혼란", 1.5f) } },
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
            new ItemData { Id = "claw_necklace", Grade = ItemGrade.Fine, AtkPct = 0.02f, CritAdd = 0.06f, SpeedAdd = 2, Name = "발톱 목걸이", Kind = ItemKind.Accessory, Description = "사냥한 짐승의 발톱을 꿴 목걸이. 공격 +2%, 치명타 +6%, 속도 +2." },
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
            new ItemData { Id = "cheonyul_pae", Grade = ItemGrade.Epic, Name = "천율패 기록", Kind = ItemKind.Key, Description = "조와 개인 이름이 새겨진 패의 기록. 잃어버린 한 장이 누명의 단서가 되었던 일을 적었다." },
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
            { 22, new[] { new ItemStack("ilgeup_yeongdan", 1), new ItemStack("ginseng", 3) } },
            { 23, new[] { new ItemStack("ginseng", 3), new ItemStack("okruju", 3) } },
            { 24, new[] { new ItemStack("ilgeup_yeongdan", 2), new ItemStack("ginseng", 4) } },
            { 25, new[] { new ItemStack("ginseng", 3), new ItemStack("okruju", 3) } },
            { 26, new[] { new ItemStack("ilgeup_yeongdan", 1), new ItemStack("bear_gall", 4), new ItemStack("ginseng", 2) } },
            { 27, new[] { new ItemStack("ginseng", 4), new ItemStack("okruju", 3) } },
            { 28, new[] { new ItemStack("ilgeup_yeongdan", 2), new ItemStack("ginseng", 3) } },
            { 29, new[] { new ItemStack("cheonyul_pae", 1), new ItemStack("ginseng", 4) } },   // 중토관 천율패
            { 30, new[] { new ItemStack("ilgeup_yeongdan", 3), new ItemStack("ginseng", 4) } },   // 화산규약지회 우승
            { 31, new[] { new ItemStack("ginseng", 4), new ItemStack("bear_gall", 3) } },   // 청룡은장의 밤
            { 32, new[] { new ItemStack("ilgeup_yeongdan", 1), new ItemStack("okruju", 3), new ItemStack("ginseng", 2) } },   // 승천무제 시험관
            { 33, new[] { new ItemStack("ilgeup_yeongdan", 2), new ItemStack("ginseng", 3) } },   // 무극검과 누명
            { 34, new[] { new ItemStack("ginseng", 3), new ItemStack("okruju", 3) } },   // 흰 봉황
            { 35, new[] { new ItemStack("ilgeup_yeongdan", 2), new ItemStack("ginseng", 3) } },   // 전설의 포효
            { 36, new[] { new ItemStack("ilgeup_yeongdan", 2), new ItemStack("ginseng", 4) } },   // 운명의 비무
            { 37, new[] { new ItemStack("ginseng", 2), new ItemStack("ilgeup_yeongdan", 1) } },
            { 38, new[] { new ItemStack("ginseng", 3), new ItemStack("ilgeup_yeongdan", 2) } },
            { 39, new[] { new ItemStack("ginseng", 3), new ItemStack("ilgeup_yeongdan", 2) } },
            { 40, new[] { new ItemStack("ginseng", 4), new ItemStack("ilgeup_yeongdan", 2) } },
            { 41, new[] { new ItemStack("ginseng", 3), new ItemStack("ilgeup_yeongdan", 3) } },
            { 42, new[] { new ItemStack("ginseng", 3), new ItemStack("ilgeup_yeongdan", 2) } },
            { 43, new[] { new ItemStack("ginseng", 3), new ItemStack("ilgeup_yeongdan", 2) } },
            { 44, new[] { new ItemStack("ginseng", 4), new ItemStack("ilgeup_yeongdan", 2) } },
            { 45, new[] { new ItemStack("ginseng", 4), new ItemStack("ilgeup_yeongdan", 3) } },
            { 46, new[] { new ItemStack("ginseng", 5), new ItemStack("ilgeup_yeongdan", 3) } },
            { 47, new[] { new ItemStack("ginseng", 3), new ItemStack("ilgeup_yeongdan", 2) } },
            { 48, new[] { new ItemStack("ginseng", 5), new ItemStack("ilgeup_yeongdan", 3) } },
            { 49, new[] { new ItemStack("ginseng", 5), new ItemStack("ilgeup_yeongdan", 3) } },
            { 50, new[] { new ItemStack("ginseng", 4), new ItemStack("ilgeup_yeongdan", 3) } },
            { 51, new[] { new ItemStack("ginseng", 4), new ItemStack("ilgeup_yeongdan", 3) } },
            { 52, new[] { new ItemStack("ginseng", 3), new ItemStack("ilgeup_yeongdan", 2) } },
            { 53, new[] { new ItemStack("ginseng", 4), new ItemStack("ilgeup_yeongdan", 3) } },
            { 54, new[] { new ItemStack("ginseng", 5), new ItemStack("ilgeup_yeongdan", 4) } },
            { 55, new[] { new ItemStack("ginseng", 5), new ItemStack("ilgeup_yeongdan", 4) } },
            { 56, new[] { new ItemStack("ginseng", 5), new ItemStack("ilgeup_yeongdan", 4) } },
            { 57, new[] { new ItemStack("ginseng", 4), new ItemStack("ilgeup_yeongdan", 4) } },
            { 58, new[] { new ItemStack("ginseng", 4), new ItemStack("ilgeup_yeongdan", 3) } },
            { 59, new[] { new ItemStack("ginseng", 4), new ItemStack("ilgeup_yeongdan", 3) } },
            { 60, new[] { new ItemStack("ginseng", 5), new ItemStack("ilgeup_yeongdan", 4) } },
            { 61, new[] { new ItemStack("ginseng", 5), new ItemStack("ilgeup_yeongdan", 4) } },
            { 62, new[] { new ItemStack("ginseng", 5), new ItemStack("ilgeup_yeongdan", 4) } },
            { 63, new[] { new ItemStack("ginseng", 5), new ItemStack("ilgeup_yeongdan", 4) } },
            { 64, new[] { new ItemStack("ginseng", 5), new ItemStack("ilgeup_yeongdan", 4) } },
            { 65, new[] { new ItemStack("ginseng", 5), new ItemStack("ilgeup_yeongdan", 4) } },
            { 66, new[] { new ItemStack("ginseng", 6), new ItemStack("ilgeup_yeongdan", 5) } },
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
                RewardEncounters = new[] { new[] { "biaosa" }, new[] { "biaosa", "biaosa" }, new[] { "gang_janghan" }, new[] { "deung_yeoun", "deung_yeoho" }, new[] { "jang_uyang" } },
                Encounters = new[] { new[] { "biaosa", "biaosa", "biaosa" }, new[] { "biaosa", "biaosa", "biaosa", "biaosa" }, new[] { "gang_janghan", "biaosa", "biaosa" }, new[] { "deung_yeoun", "deung_yeoho", "biaosa" }, new[] { "jang_uyang", "biaosa", "biaosa" } } },
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
                RewardEncounters = new[] { new[] { "hogol_bandit", "hogol_bandit" }, new[] { "hogol_bandit", "hogol_bandit", "hogol_bandit" }, new[] { "paeho_jung_gwang", "hogol_bandit" } },
                Encounters = new[] { new[] { "hogol_bandit", "hogol_bandit", "hogol_archer" }, new[] { "hogol_bandit", "hogol_bandit", "hogol_axe", "hogol_archer" }, new[] { "paeho_jung_gwang", "hogol_bandit", "hogol_bandit" } } },
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
                RewardEncounters = new[] { new[] { "yonggol_bandit", "yonggol_bandit" }, new[] { "yonggol_bandit", "yonggol_bandit", "yonggol_bandit" }, new[] { "paeryong_wooak", "yonggol_bandit" } },
                Encounters = new[] { new[] { "yonggol_bandit", "yonggol_bandit", "yonggol_archer" }, new[] { "yonggol_bandit", "yonggol_bandit", "yonggol_axe", "yonggol_archer" }, new[] { "paeryong_wooak", "yonggol_bandit", "yonggol_bandit" } } },
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
                RewardEncounters = new[] { new[] { "jang_ugang" }, new[] { "yeomdo" } },
                Encounters = new[] { new[] { "jang_ugang", "jungyang_pyosa", "jungyang_pyosa" }, new[] { "yeomdo" } } },
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
                RewardEncounters = new[] { new[] { "hoajang_disciple", "hoajang_disciple" }, new[] { "hoajang_disciple", "hoajang_disciple", "hoajang_disciple" }, new[] { "ho_cheonsang", "hoajang_disciple" } },
                Encounters = new[] { new[] { "hoajang_disciple", "hoajang_disciple", "hoajang_disciple" }, new[] { "hoajang_disciple", "hoajang_disciple", "hoajang_disciple", "hoajang_senior" }, new[] { "ho_cheonsang", "hoajang_disciple", "hoajang_disciple" } } },
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
                RewardEncounters = new[] { new[] { "jongri_hak" }, new[] { "jongri_hak", "jongri_hak" }, new[] { "chu_gayeon", "jongri_hak" } },
                Encounters = new[] { new[] { "jongri_hak", "hwasan_jeja", "hwasan_jeja" }, new[] { "jongri_hak", "jongri_hak", "hwasan_jeja" }, new[] { "chu_gayeon", "jongri_hak", "hwasan_jeja" } } },
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
                RewardEncounters = new[] { new[] { "gu_chang" }, new[] { "so_cheongun" }, new[] { "gu_chang", "so_cheongun" }, new[] { "wiji_cheon" } },
                Encounters = new[] { new[] { "gu_chang", "dobaek_member", "dobaek_member" }, new[] { "so_cheongun", "geomhon_member", "geomhon_member" }, new[] { "gu_chang", "so_cheongun", "dobaek_member", "geomhon_member" }, new[] { "wiji_cheon" } } },
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
                            "홍란|내가 그 금을 조금 연주해 보아도 되겠느냐.",
                            "비류연|현이 날카롭습니다. 함부로 건드리면 다쳐요.",
                            "홍란이 살짝 건드린 묵금의 뇌령사 현에 손끝이 베여 피가 맺혔다. 홍란은 이날부터 이 1학년을 눈여겨보게 된다.",
                        } },
                    new StoryScene { Title = "새벽의 나예린", Lines = new[] {
                            "새벽, 나예린은 풀잎의 이슬 한 방울을 검기로 세 갈래로 가른다. 사저 독고령이 찾아와 위지천을 방패막이로 둔 일을 두고 말을 건넨다.",
                            "나예린|다음에는 같은 실수를 하지 않겠어요.",
                            "용안으로도 읽을 수 없었던 낯선 남자의 행동이 마음에 남아 있었다.",
                            "같은 날 이진설이 새 기술 쌍검이연십이참격을 독고령에게 시험하려 했지만 나예린이 대신 나섰다. 스물네 줄기 검기가 모두 빗나가고 나예린의 검끝이 이진설의 목뒤에 닿았다.",
                            "나예린|큰 기술은 허점도 커요.",
                        } },
                    new StoryScene { Title = "후배의 뒤를 지키는 철곤", Lines = new[] {
                            "빙봉수호대의 두 미행자 강만현과 전문연이 비류연을 뒤쫓았다. 임성진은 철곤으로 하나의 검을 부수고 다른 검을 휘게 하며 후배를 보호했다.",
                            "뒤이어 따로 나타난 세 번째 미행자는 추일태였다. 빙봉수호대의 추적과 구정회가 맡긴 시험은 같은 움직임이 아니었다.",
                        } },
                    new StoryScene { Title = "추일태의 내기", Lines = new[] {
                            "구정회의 시험을 맡은 일은무영 추일태가 장소와 방법을 정해 비류연에게 은신 내기를 걸었다. 금남의 성지, 여성 전용 기숙사 백향관에 잠입해 수놓은 옷을 가져오는 것이다. 비연태가 증인을 맡았다.",
                            "추일태|내기 돈은 은자 이백 냥이다. 해볼 텐가?",
                            "비류연|돈이 적게 걸린 내기는 안 한다고 했잖아요. 이백 냥이면 하죠. 목표는요?",
                            "추일태|네 목표는 301호 나예린의 옷이다. 나는 독고령의 것을 가져오마.",
                        } },
                },
                Intro = new[] {
                    "한 달 동안 도면과 기관을 공부한 뒤, 비류연은 진법의 생문이 열리고 야간 기관이 완전히 가동되기 전을 골랐다. 달이 숨은 밤, 복면을 쓰고 백향관으로 향했다. 난화백무진과 절독난향십이선녀의 경계를 넘어야 했다.",
                    "비류연|(추일태는 독고령, 나는 나예린의 옷. 은자 이백 냥짜리 내기라지만 만만하지 않겠네.)",
                    "그는 봉황무로 그림자를 탔다. 동료들을 망꾼으로 세우지 않은 단독 잠입이었다.",
                },
                Outro = new[] {
                    "비류연은 쓰러진 진령에게 감아 둔 뇌령사를 움직였다. 괴뢰처럼 일어난 진령을 나예린이 부축하는 틈에, 비류연은 분뢰수와 검기의 충돌을 이용해 빠져나갔다. 나예린을 쓰러뜨려 얻은 승리는 아니었다.",
                    "백 장 밖에서 뇌령사를 당기자 목표물이 손에 들어왔다. 그러나 검후처에 침입한 또 다른 그림자의 목적은 내기와 달랐다.",
                    "같은 밤 검혼관 옆 숲. 무흔일호가 효룡 앞에 무릎을 꿇었다.",
                    "무흔일호|실패했습니다, 삼공자님.",
                    "효룡은 마천각 삼공자였다. 대사형인 일공자의 계획과 달리, 훔친 무공으로 승리를 얻는 일에는 반대했다.",
                    "효룡|감시가 있을 겁니다. 당분간 연락을 끊으세요.",
                    "효룡은 정보를 제공했지만 부하의 실패를 처벌하기보다 부상과 피의 흔적을 처리했다. 두 침입 사건은 겹쳤으나 비류연은 내통자의 정체를 몰랐다.",
                },
                Encounters = new[] { new[] { "sunyeo" }, new[] { "sunyeo", "sunyeo" }, new[] { "na_yerin" } },
                Trials = new[] {
                    TrialRule.Survive("교대 경계", "경계 4행동을 견디며 틈을 기다리세요.", 4),
                    TrialRule.Survive("진법의 생문", "경계 6행동을 견디며 퇴로를 찾으세요.", 6),
                    TrialRule.Survive("301호 탈출", "나예린의 공격 8행동을 견디고 빠져나오세요. 나예린을 쓰러뜨리는 목표가 아닙니다.", 8) } },
            new StageData { Number = 11, RewardWeightBudget = 3, Name = "승급시험", RecommendedLevel = 27, DooTotal = 4600,
                Prologue = new[]
                {
                    new StoryScene { Title = "삼성제 약속", Lines = new[] {
                            "다음 날 나예린이 애소저회를 직접 찾아와 비류연의 목에 검을 겨눴다.",
                            "나예린|어제 백향관에 침입한 사람이 소협인가요?",
                            "비류연|글쎄요.",
                            "나예린|검후의 무공을 훔칠 필요가 없다는 말인가요? 올해 삼성무제에서 우승한다면 그 말을 믿겠어요.",
                            "비류연|(삼성제가 뭔지도 모른 채) 우승하면 저를 다른 눈으로 봐 주세요.",
                            "나예린에게는 의심을 거둘 검증 조건, 비류연에게는 관계가 달라질 기대였다. 연인이 되겠다는 약속은 아니었다. 비류연은 그제야 대회를 알아보기 시작했다.",
                        } },
                    new StoryScene { Title = "대사형의 귀환", Lines = new[] {
                            "비류연은 염도를 찾아가 '주작단은 내 제자니 네 사제'라고 폭탄선언을 했다. 다섯 달 동안 인피면구를 쓴 가짜 노사부였다는 사실은 주작단에게 끝까지 비밀이다. 그래서 '만난 적 없는 대사형'으로 나서기로 했다.",
                            "다음 날 연무장. 인피면구 없이 나타난 비류연이 외쳤다.",
                            "비류연|내가 너희들의 대사형이다!",
                            "노학|뭐? 이런 애송이가 무슨… 으아악!",
                            "달려든 노학은 삼복구타권법에 날아갔고, 묵룡환을 본 주작단은 일제히 포권했다.",
                            "염도가 능청스럽게 보증을 섰다. 주작단은 눈앞의 대사형이 합숙 때의 노사부인 줄 끝내 몰랐다.",
                        } },
                    new StoryScene { Title = "천서각의 문턱", Lines = new[] {
                        "모용휘는 오검룡 이상에게만 허용된 비급 열람을 거절당했다. 학칙을 어기기보다 정식 승급시험을 치르기로 한다.",
                        "배움의 자격을 얻으려던 결심은 뒤에 조부를 모욕하는 검존과 만나 가문의 명예를 지키려는 약속으로 무거워진다.",
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
                            "회풍무류검으로 버텨 온 수련이 순간의 칠매검으로 이어졌다. 곧 두드러기가 번져 검을 떨어뜨렸지만, 염도는 처음으로 윤준호를 다르게 보았다.",
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
                    "결승으로 가는 길에는 사천당문의 독랄수 당문천이 남아 있었다. 당철영은 사촌 형에게 비류연을 조심하라고 경고했다.",
                    "당문천이 시험하듯 던진 삼환비선의 동전 세 개를 당철영은 손가락으로 튕겨 냈다. 매일 이어 온 구슬꿰기 수련이 감각을 키웠음을 스스로 확인했다.",
                    "당문천전 뒤 검후전과 검성전의 공방을 거쳐, 비류연과 위지천의 마지막 승부로 이어진다.",
                },
                Outro = new[] {
                    "운향정에서 나예린이 연주했던 곡이 묵금에서 흘렀다. 위지천은 질투에 흔들렸고, 끊긴 곡의 충격에 피를 토했다.",
                    "살초는 탄금행만으로 피하기 어려워 봉황무로 흘렸다. 선풍우뢰는 보이지 않는 막에, 천풍마뢰참은 뇌령신공을 운기한 묵금의 뇌령사 현에 막혔다.",
                    "비류연|비뢰도 오의, 뢰광류하곡.",
                    "왼손에서 은빛 뇌령사 세 줄기가 뻗었다. 위지천이 무너지고, 공손일취는 그 수법에 경악했다. 그가 떠올린 과거의 진상은 아직 드러나지 않았다.",
                    "검성전 다음 날 위지천과의 결승이 끝났다. 세 부문 무승부자는 종합 결승에 나설 수 없어 비류연이 종합 우승자가 되었다. 관중의 어부지리라는 평과 친구들의 축하는 함께 남았다.",
                    "나예린|검후처에 침입한 사람이 소협이라는 의심은 거두겠습니다.",
                    "사과는 신체 접촉에 대한 허락이 아니었다. 비류연이 허락 없이 다시 입맞추자 나예린은 검을 뽑았다. 나예린은 여전히 그의 마음을 읽지 못했고, 뽑은 검도 그에게 닿지 않았다.",
                    "금영호는 도광서에게 삼고구배를 받고 배당금을 챙겼지만 절반 이상을 비류연에게 내주었다.",
                    "한 달 뒤 염도는 연말 청룡단전을 앞두고 주작단을 다시 몰아붙였다. 현운의 대응이 좋아졌고 윤준호는 한 시진 가까이 버텼다. 검향 과민 반응이 완치된 것은 아니었다. 한편 무흔각의 비류연 사문 조사 결과는 불명이었다.",
                    "효룡은 청흔·모용휘·비류연, 그리고 장홍을 주시했다. 다가오는 때의 정체는 아직 일행에게 알려지지 않았다.",
                },
                Encounters = new[] { new[] { "dang_munceon" }, new[] { "gwan_seolji_story" }, new[] { "cheong_heun_story" }, new[] { "wiji_cheon_baek" } },
                Trials = new[] { null, TrialRule.Survive("검후전 결승", "관설지의 공격을 12행동 동안 견뎌 주세요.", 12), TrialRule.Survive("검성전 결승", "청흔의 검을 12행동 동안 견뎌 주세요.", 12), null } },
            new StageData { Number = 14, RewardWeightBudget = 10, Name = "무당산 고립", RecommendedLevel = 33, DooTotal = 6500,
                Prologue = StoryMidgame.Prologues[14],
                Intro = StoryMidgame.Intros[14],
                Outro = StoryMidgame.Outros[14],
                Encounters = new[] { new[] { "maeng_yeonho" }, new[] { "amryong_salsu", "amryong_salsu", "amryong_salsu" }, new[] { "amryong_salsu", "amryong_salsu" }, new[] { "mak_jeok" }, new[] { "heuk_sang" }, new[] { "amheon_guard", "amheon_guard", "amheon_guard" }, new[] { "gal_hyobong" }, new[] { "chohon_gwigeom", "chohon_gwigeom" }, new[] { "myeongbu_jang", "myeongbu_jang" }, new[] { "gal_hyobong" }, new[] { "cheonsal", "jisal" } },
                Trials = new[] { null, null, null, null, null, null, TrialRule.Survive("갈효봉의 첫 공방", "갈효봉의 공격 8행동을 버텨 주세요. 방어와 운기를 활용할 수 있습니다.", 8), null, null, TrialRule.Survive("갈효봉의 두 번째 공방", "공격 6행동을 견디며 동료를 엄호하세요. 형을 처치하는 목표가 아닙니다.", 6), null } },
            new StageData { Number = 15, RewardWeightBudget = 9, Name = "철각비마대", RecommendedLevel = 35, DooTotal = 7000,
                Prologue = StoryMidgame.Prologues[15],
                Intro = StoryMidgame.Intros[15],
                Outro = StoryMidgame.Outros[15],
                Encounters = new[] { new[] { "bikwae_ssangchang", "bikwae_ssangchang" }, new[] { "sadae_jilpung", "sadae_jilpung", "sadae_jilpung" }, new[] { "cheolbi_gyeok", "cheolbi_gyeok" }, new[] { "wi_musang" }, new[] { "gu_cheonhak" } } },
            new StageData { Number = 16, Name = "저잣거리 습격", RecommendedLevel = 37, DooTotal = 7500,
                Prologue = StoryMidgame.Prologues[16],
                Intro = StoryMidgame.Intros[16],
                Outro = StoryMidgame.Outros[16],
                RewardEncounters = new[] { new[] { "baegin_jagaek", "baegin_jagaek", "baegin_jagaek" }, new[] { "ssanggwi", "baegin_jagaek" }, new[] { "baek_tanjeo" } },
                Encounters = new[] { new[] { "baegin_jagaek", "baegin_jagaek", "baegin_jagaek" }, new[] { "ssanggwi", "baegin_jagaek", "baegin_jagaek" }, new[] { "baek_tanjeo", "baegin_jagaek", "baegin_jagaek" } } },
            new StageData { Number = 17, RewardWeightBudget = 7, Name = "마하령의 분노", RecommendedLevel = 39, DooTotal = 8000,
                Prologue = StoryMidgame.Prologues[17],
                Intro = StoryMidgame.Intros[17],
                Outro = StoryMidgame.Outros[17],
                Encounters = new[] { new[] { "pae_yugyeong" }, new[] { "sinung_daewon", "sinung_daewon", "sinung_daewon" }, new[] { "pae_hyeokseong" } },
                Trials = new[] { null, TrialRule.Survive("현장 통제의 압력", "적 4행동을 견디세요. 통제 인력을 소탕하는 목표가 아닙니다.", 4), null } },
            new StageData { Number = 18, RewardWeightBudget = 4, Name = "태극의 인재", RecommendedLevel = 41, DooTotal = 8500,
                Prologue = StoryMidgame.Prologues[18],
                Intro = StoryMidgame.Intros[18],
                Outro = StoryMidgame.Outros[18],
                Encounters = new[] { new[] { "yong_cheonmyeong" }, new[] { "hyo_ryong_rope" }, new[] { "gwan_cheolsu_trial" }, new[] { "gwan_cheolsu" } },
                Trials = new[] { TrialRule.Survive("백룡검의 한 합", "검격 2행동을 견디며 벗어나세요. 결판을 내는 비무가 아닙니다.", 2), null,
                    TrialRule.Survive("북극칠성인의 압력", "공격 6행동을 견디며 검세를 읽으세요. 기존 방어·회피로 대응할 수 있습니다.", 6), null } },
            new StageData { Number = 19, RewardWeightBudget = 8, RewardScale = 1.5, Name = "환마동 시험", RecommendedLevel = 43, DooTotal = 9000,
                Prologue = StoryMidgame.Prologues[19],
                Intro = StoryMidgame.Intros[19],
                Outro = StoryMidgame.Outros[19],
                Encounters = new[] { new[] { "hwan_geomseong" }, new[] { "hwan_gal_hyobong" }, new[] { "hwan_family_duty" }, new[] { "hwan_nameless_blade" }, new[] { "hwan_sukbu" }, new[] { "hwan_sabu" } },
                Trials = new[] { TrialRule.Survive("나뭇가지 앞에서", "공격 6행동을 견디며 자신을 돌아보세요. 실제 검성을 이기는 목표가 아닙니다.", 6), null,
                    TrialRule.Survive("아버지의 기대", "공격 6행동을 견디며 두려움을 마주하세요.", 6),
                    TrialRule.Survive("끝나지 않는 기억", "공격 6행동을 견디며 환상의 틈을 찾으세요.", 6),
                    TrialRule.Survive("도움을 청할 시간", "공격 6행동을 견디며 동료의 부름을 기다리세요. 가해자를 처치해야 통과하는 시험이 아닙니다.", 6), null } },
            new StageData { Number = 20, Name = "매몰 44일", RecommendedLevel = 45, DooTotal = 9500,
                Prologue = StoryMidgame.Prologues[20],
                Intro = StoryMidgame.Intros[20],
                Outro = StoryMidgame.Outros[20],
                Encounters = new[] { new[] { "cave_viper", "cave_viper", "cave_viper" }, new[] { "cave_viper", "cave_viper" }, new[] { "mukrin_hyeolmang" } },
                Trials = new[] { null, null, StoryShowdowns.HyeolmangTrial } },   // the 섬뢰창 into the opened mouth
            new StageData { Number = 21, RewardWeightBudget = 8, Name = "흑랑채의 청혼", RecommendedLevel = 47, DooTotal = 10000,
                Prologue = StoryMidgame.Prologues[21], Intro = StoryMidgame.Intros[21], Outro = StoryMidgame.Outros[21],
                Encounters = new[] { new[] { "heukrang_bandit", "heukrang_bandit", "heukrang_bandit" }, new[] { "im_gae" }, new[] { "lee_songhak" } },
                Trials = new[] { null, null, TrialRule.Survive("군자소요검의 수비", "적 6행동을 견디세요. 남궁상의 공방 뒤 노사들의 중재로 길이 열립니다.", 6) } },
            new StageData { Number = 22, RewardWeightBudget = 8, Name = "대홍산에서 낙뢰곡으로", RecommendedLevel = 49, DooTotal = 10500,
                Prologue = StoryMidgame.Prologues[22], Intro = StoryMidgame.Intros[22], Outro = StoryMidgame.Outros[22],
                Encounters = new[] { new[] { "im_deokseong_story" }, new[] { "hyeolma_dae", "hyeolma_dae", "hyeolma_dae" }, new[] { "hyeolma_dae", "hyeolma_dae" }, new[] { "hyeolma_dae", "hyeolma_dae", "hyeolma_dae" }, new[] { "hyeol_gung", "hyeolma_dae" }, new[] { "hyeol_swae" } },
                Trials = new[] { TrialRule.Survive("부자의 무승부", "적 8행동을 견디세요. 임덕성을 처치하는 승부가 아닙니다.", 8), null, null, null, null, null } },
            new StageData { Number = 23, RewardWeightBudget = 8, Name = "비사진과 혈창", RecommendedLevel = 51, DooTotal = 11000,
                Prologue = StoryMidgame.Prologues[23], Intro = StoryMidgame.Intros[23], Outro = StoryMidgame.Outros[23],
                Encounters = new[] { new[] { "bisa_snake", "bisa_snake", "bisa_snake" }, new[] { "cheonghong_ssanggaksa", "bisa_snake" }, new[] { "hyeol_chang" }, new[] { "mo_saryeong" } } },
            new StageData { Number = 24, RewardWeightBudget = 8, Name = "비뢰쌍마와 매화가면", RecommendedLevel = 53, DooTotal = 12000,
                Prologue = StoryMidgame.Prologues[24], Intro = StoryMidgame.Intros[24], Outro = StoryMidgame.Outros[24],
                Encounters = new[] { new[] { "hyeolma_dae_b", "hyeolma_dae_b", "hyeolma_dae_b" }, new[] { "dongbang_hak", "hyeolma_dae_b" }, new[] { "gong_pae" }, new[] { "maehwa_mask_trial" } },
                Trials = new[] { null, null, null, TrialRule.Survive("옷깃을 향한 도전", "적 6행동을 견디며 검로를 읽으세요. 윤준호의 도전은 이후 이야기로 이어집니다.", 6) } },
            new StageData { Number = 25, RewardWeightBudget = 8, Name = "멸겁삼관과 화평장", RecommendedLevel = 55, DooTotal = 13000,
                Prologue = StoryMidgame.Prologues[25], Intro = StoryMidgame.Intros[25], Outro = StoryMidgame.Outros[25],
                Encounters = new[] { new[] { "hwapyeong_soldier", "hwapyeong_soldier", "hwapyeong_soldier" }, new[] { "hwapyeong_soldier", "cheonsal_hwapyeong", "jisal_hwapyeong" }, new[] { "hwapyeong_soldier", "hwapyeong_soldier" }, new[] { "baek_son_gosu" } },
                Trials = new[] { TrialRule.Survive("잠입 뒤의 경보", "적 4행동을 견디며 동료를 엄호하세요.", 4), null, null, TrialRule.Survive("극안의 중심", "적 8행동을 견디며 구출한 동료를 보호하세요. 고수를 처치하는 목표가 아닙니다.", 8) } },
            new StageData { Number = 26, RewardWeightBudget = 8, Name = "화평장 탈출과 살귀대", RecommendedLevel = 57, DooTotal = 13500,
                Prologue = StoryMidgame.Prologues[26], Intro = StoryMidgame.Intros[26], Outro = StoryMidgame.Outros[26],
                Encounters = new[] { new[] { "sura_screen", "sura_screen", "sura_screen" }, new[] { "bi_first_clash" }, new[] { "hwapyeong_breakout", "hwapyeong_breakout", "hwapyeong_breakout" }, new[] { "salgwi_daewon", "salgwi_daewon", "salgwi_daewon" }, new[] { "salgwi_daewon", "salgwi_daewon" }, new[] { "salhon_sipgwi" } },
                Trials = new[] { null, TrialRule.Survive("인지의 간격", "적 6행동을 견디며 공격의 변화를 읽으세요. 생환 뒤에도 부상은 남습니다.", 6), null, null, TrialRule.Survive("노인들이 나설 때까지", "적 4행동을 견디며 동료를 엄호하세요.", 4), null } },
            new StageData { Number = 27, RewardWeightBudget = 7, Name = "마천칠걸과 개회", RecommendedLevel = 59, DooTotal = 14000,
                Prologue = StoryMidgame.Prologues[27], Intro = StoryMidgame.Intros[27], Outro = StoryMidgame.Outros[27],
                Encounters = new[] { new[] { "chu_myeong" }, new[] { "jongri_chu" }, new[] { "do_chuun" }, new[] { "baek_sayeong" }, new[] { "oh_munchu" }, new[] { "mu_jeong_public" }, new[] { "gyook_public" } },
                Trials = new[] { TrialRule.Survive("칠변연환 · 1", "공세를 견디며 빈틈을 읽으세요. 칠걸을 처형하는 목표가 아닙니다.", 6), TrialRule.Survive("칠변연환 · 2", "공세를 견디며 빈틈을 읽으세요. 칠걸을 처형하는 목표가 아닙니다.", 4), TrialRule.Survive("칠변연환 · 3", "공세를 견디며 빈틈을 읽으세요. 칠걸을 처형하는 목표가 아닙니다.", 4), TrialRule.Survive("칠변연환 · 4", "공세를 견디며 빈틈을 읽으세요. 칠걸을 처형하는 목표가 아닙니다.", 4), TrialRule.Survive("칠변연환 · 5", "공세를 견디며 빈틈을 읽으세요. 칠걸을 처형하는 목표가 아닙니다.", 4), TrialRule.Survive("칠변연환 · 6", "공세를 견디며 빈틈을 읽으세요. 칠걸을 처형하는 목표가 아닙니다.", 4), TrialRule.Survive("칠변연환 · 7", "공세를 견디며 빈틈을 읽으세요. 칠걸을 처형하는 목표가 아닙니다.", 4) } },
            new StageData { Number = 28, RewardWeightBudget = 10, Name = "목요관과 멸성대", RecommendedLevel = 61, DooTotal = 14500,
                Prologue = StoryMidgame.Prologues[28], Intro = StoryMidgame.Intros[28], Outro = StoryMidgame.Outros[28],
                Encounters = new[] { new[] { "jongri_chu_night", "do_chuun_night" }, new[] { "mulsung_daewon", "mulsung_daewon", "mulsung_daewon" }, new[] { "mulsung_daewon", "mulsung_daewon", "mulsung_daewon" }, new[] { "mulsung_daewon", "i_gae_bujo" }, new[] { "sama_heun" } },
                Trials = new[] { TrialRule.Deadline("목요관 야간 질주", "30행동 안에 길을 여세요. 계주의 공동 일위 판정은 이야기로 이어집니다.", 30), TrialRule.Survive("정보를 살려 보낼 시간", "적 6행동을 견디며 동료를 엄호하세요.", 6), null, null, null } },
            new StageData { Number = 29, RewardWeightBudget = 12, Name = "구조와 중토관의 누명", RecommendedLevel = 63, DooTotal = 15000,
                Prologue = StoryMidgame.Prologues[29], Intro = StoryMidgame.Intros[29], Outro = StoryMidgame.Outros[29],
                Encounters = new[] { new[] { "cliff_rescue" }, new[] { "geomhu_ioksang" }, new[] { "heuksaek_amhyeok", "wiji_hyeolpok" }, new[] { "macheon_samhyung", "macheon_samhyung", "macheon_samhyung" }, new[] { "mask_bitan" }, new[] { "mu_jeong" }, new[] { "witness_pursuer" } },
                Trials = new[] { TrialRule.Survive("사람을 낚는 뇌령사", "적 6행동을 견디며 낙하의 힘을 흘리세요. 구조는 이어지는 장면에서 완성됩니다.", 6), TrialRule.Survive("검후의 십초지적", "적 12행동을 견디세요. 원작의 열 초식 전체를 같은 단위로 재는 시험은 아닙니다.", 12), TrialRule.Harmony("수화관의 내공 조화", "모두가 연속으로 공격하면 다음 일격이 강화됩니다. 32행동 안에 압력을 해소하세요.", 32), null, TrialRule.Survive("비탄의 견제", "적 6행동을 견디며 동료를 보호하세요. 세 가면을 동시에 쓰러뜨리는 목표가 아닙니다.", 6), TrialRule.Survive("삼성각의 증인 보호", "적 6행동을 견디며 동료를 엄호하세요.", 6), null } },
            new StageData { Number = 30, RewardWeightBudget = 8, Name = "화룡멸겁대진", RecommendedLevel = 65, DooTotal = 16000,
                // 일반: 비 holds at 1 HP, then the scripted 화룡멸겁대진 (FireDragonFinale); 어려움·극악 keep the earlier four fights (WithDifficulty)
                Prologue = StoryMidgame.Prologues[30], Intro = StoryMidgame.Intros[30], Outro = FireDragonFinale.NormalOutro(),
                Encounters = FireDragonFinale.Encounters, RewardEncounters = FireDragonFinale.RewardEncounters, Trials = FireDragonFinale.Trials },
            // Volumes 17-20: fixed perspectives preserve who is actually present in each scene.
            new StageData { Number = 31, RewardWeightBudget = 7, RewardScale = 8.0 / 7.0, Name = "청룡은장의 마지막 장사", RecommendedLevel = 67, DooTotal = 16200,
                Prologue = StoryMidgame.Prologues[31], Intro = StoryMidgame.Intros[31], Outro = StoryMidgame.Outros[31],
                Encounters = new[] { new[] { "salryong_daewon", "salryong_daewon", "salryong_daewon" }, new[] { "cheongryong_escape" }, new[] { "salryong_yun" } },
                StoryActors = new[] { new[] { "story_yu_jaeryong" }, new[] { "story_yu_jaeryong" }, new[] { "story_yu_jaeryong" } },
                Trials = new[] { TrialRule.Survive("아이들이 빠져나갈 시간", "적 4행동을 견디며 공세를 읽으세요.", 4), TrialRule.Survive("진짜 통로를 숨긴다", "적 4행동을 견디며 공세를 읽으세요.", 4), TrialRule.Survive("아버지의 마지막 장사", "적 6행동을 견디며 남매의 퇴로를 지키세요. 이후 유재룡의 마지막 선택으로 이어집니다.", 6) } },
            new StageData { Number = 32, RewardWeightBudget = 5, RewardScale = 7.0 / 6.0, Name = "황금 완장과 보름의 수련", RecommendedLevel = 68, DooTotal = 16600,
                Prologue = StoryMidgame.Prologues[32], Intro = StoryMidgame.Intros[32], Outro = StoryMidgame.Outros[32],
                Encounters = new[] { new[] { "yu_ran", "yu_unbi" }, new[] { "gwan_eogeom_first" }, new[] { "gwan_cheolsu_eogeom" } },
                StoryActors = new[] { new[] { "story_namgung_sang" }, new[] { "story_namgung_sang" }, new[] { "story_namgung_sang" } },
                Trials = new[] { TrialRule.Deadline("황금 완장의 도전", "30행동 안에 두 도전자를 제압하세요. 원작의 세 초식과 게임 행동 수는 다른 단위입니다.", 30), TrialRule.Survive("눈앞의 허명", "적 4행동을 견디며 공세를 읽으세요.", 4), TrialRule.Survive("보름 동안의 적응", "적 8행동을 견디며 공세를 읽으세요.", 8) } },
            new StageData { Number = 33, RewardWeightBudget = 5, Name = "두 사건과 살인 누명", RecommendedLevel = 69, DooTotal = 17000,
                Prologue = StoryMidgame.Prologues[33], Intro = StoryMidgame.Intros[33], Outro = StoryMidgame.Outros[33],
                Encounters = new[] { new[] { "i_sigeon" }, new[] { "chimbaetneun_ja" }, new[] { "cheong_heun_mugeuk" }, new[] { "gongson_ambush" } },
                StoryActors = new[] { new[] { "player" }, new[] { "player" }, new[] { "player" }, new[] { "story_namgung_sang" } },
                Trials = new[] { TrialRule.Survive("금란주루의 대치", "적 6행동을 견디며 공세를 읽으세요.", 6), null, null, TrialRule.Survive("화산 옷의 유인책", "적 4행동을 견디며 야습에 대응하세요. 결판은 다음 이야기로 이어집니다.", 4) } },
            new StageData { Number = 34, RewardWeightBudget = 7, Name = "흰 봉황의 선택", RecommendedLevel = 70, DooTotal = 17400,
                Prologue = StoryMidgame.Prologues[34], Intro = StoryMidgame.Intros[34], Outro = StoryMidgame.Outros[34],
                Encounters = new[] { new[] { "jungwon_pyosa", "jungwon_pyosa" }, new[] { "o_yeong", "o_gi" }, new[] { "yun_ijeong", "i_sigeon_jungwon" } },
                StoryActors = new[] { new[] { "na_yerin", "hyo_ryong", "story_i_jinseol" }, new[] { "na_yerin", "hyo_ryong", "story_i_jinseol" }, new[] { "na_yerin", "hyo_ryong", "story_i_jinseol" } },
                Trials = new[] { TrialRule.Survive("표사들을 붙잡는 동료", "적 4행동을 견디며 공세를 읽으세요.", 4), null, TrialRule.Survive("가짜의 말을 듣기까지", "적 8행동을 견디며 사검의 흐름을 읽으세요. 상대의 고정된 도주가 전투 실패는 아닙니다.", 8) } },
            new StageData { Number = 35, RewardWeightBudget = 7, RewardScale = 8.0 / 7.0, Name = "미끼와 전설의 포효", RecommendedLevel = 71, DooTotal = 17800,
                Prologue = StoryMidgame.Prologues[35], Intro = StoryMidgame.Intros[35], Outro = StoryMidgame.Outros[35],
                Encounters = new[] { new[] { "i_sigeon_bait" }, new[] { "i_sigeon_arrow" }, new[] { "sipsamhyeol", "sipsamhyeol" }, new[] { "il_hyeol" } },
                StoryActors = new[] { new[] { "story_namgung_sang", "story_mo_yonghwi", "story_gongson_jeolhwi" }, new[] { "story_yu_eunseong" }, new[] { "story_jang_uyang", "story_jang_ugyeong" }, new[] { "story_jang_uyang", "story_jang_ugyeong" } },
                Trials = new[] { TrialRule.Survive("미끼 작전", "적 10행동을 견디며 이시건을 붙잡아 두세요. 인질 발생 뒤 생포 포기는 이야기의 결과입니다.", 10), TrialRule.Survive("후예사일이 닿는 순간", "적 4행동을 견디며 공세를 읽으세요.", 4), TrialRule.Survive("국주들을 노린 침입", "적 4행동을 견디며 공세를 읽으세요.", 4), TrialRule.Survive("노인이 일어나기 전", "적 4행동을 견디며 공세를 읽으세요.", 4) } },
            new StageData { Number = 36, RewardWeightBudget = 4, Name = "비상련화와 출발", RecommendedLevel = 72, DooTotal = 18200,
                Prologue = StoryMidgame.Prologues[36], Intro = StoryMidgame.Intros[36], Outro = StoryMidgame.Outros[36],
                Encounters = new[] { new[] { "geomjon_gongson" }, new[] { "jin_soryeong_two" }, new[] { "jin_soryeong_four" }, new[] { "jin_soryeong" } },
                StoryActors = new[] { new[] { "player" }, new[] { "story_namgung_sang" }, new[] { "story_namgung_sang" }, new[] { "story_namgung_sang" } },
                Trials = new[] { TrialRule.Survive("검존의 오해", "적 6행동을 견디며 공세를 읽으세요.", 6), TrialRule.Survive("비상련화 · 이분영", "적 4행동을 견디며 공세를 읽으세요.", 4), TrialRule.Survive("비상련화 · 사분영", "적 4행동을 견디며 공세를 읽으세요.", 4), TrialRule.Survive("비상련화 · 팔분영", "적 6행동을 견디세요. 이후 처음 펼치는 이기어검과 제한 비무의 인정으로 이어집니다.", 6) } },
            new StageData { Number = 37, Name = "붉은 수건과 흰 매", RecommendedLevel = 74, DooTotal = 19500, RewardWeightBudget = 4,
                Prologue = StoryMidgame.Prologues[37], Intro = StoryMidgame.Intros[37], Outro = StoryMidgame.Outros[37],
                Encounters = new[] { new[] { "baekgyo_scales" }, new[] { "baekgyo_opening" } },
                StoryActors = new[] { new[] { "story_young_yeonbi" }, new[] { "story_young_yeonbi" } },
                Trials = new TrialRule[] { TrialRule.Survive("둥지를 지킬 시간", "적 4행동을 견디며 비늘의 빈틈을 찾으세요.", 4), TrialRule.Deadline("힘을 한 점에", "14행동 안에 백교의 방어를 뚫으세요. 이후의 중독과 구조는 이야기의 결과입니다.", 14) } },
            new StageData { Number = 38, Name = "귀문 너머 임시 십삼대", RecommendedLevel = 76, DooTotal = 21000, RewardWeightBudget = 5,
                Prologue = StoryMidgame.Prologues[38], Intro = StoryMidgame.Intros[38], Outro = StoryMidgame.Outros[38],
                Encounters = new[] { new[] { "ok_yugyeong_reunion" }, new[] { "gongson_thirteenth" }, new[] { "yong_cheongmyeong_vote" } },
                StoryActors = new[] { new[] { "jang_hong" }, new[] { "yun_junho" }, new[] { "story_ma_haryeong" } },
                Trials = new TrialRule[] { TrialRule.Survive("다시 만난 혈풍란", "적 6행동을 견디세요. 장홍과 옥유경의 과거는 아직 밝혀지지 않습니다.", 6), null, null } },
            new StageData { Number = 39, Name = "환대와 오십만 냥의 약속", RecommendedLevel = 78, DooTotal = 22500, RewardWeightBudget = 5,
                Prologue = StoryMidgame.Prologues[39], Intro = StoryMidgame.Intros[39], Outro = StoryMidgame.Outros[39],
                Encounters = new[] { new[] { "donwang_guard", "donwang_guard" }, new[] { "georyeoksin" }, new[] { "donwang_hobeop", "donwang_hobeop" } },
                StoryActors = new[] { new[] { "story_yeonbi", "na_yerin" }, new[] { "story_yeonbi", "na_yerin" }, new[] { "story_yeonbi", "na_yerin" } } },
            new StageData { Number = 40, Name = "십면매복과 은빛 눈보라", RecommendedLevel = 80, DooTotal = 24000, RewardWeightBudget = 6,
                Prologue = StoryMidgame.Prologues[40], Intro = StoryMidgame.Intros[40], Outro = StoryMidgame.Outros[40],
                Encounters = new[] { new[] { "sipmyeon_arrows" }, new[] { "sipmyeon_water" }, new[] { "heuksak_capture" }, new[] { "heuksak_break" } },
                StoryActors = new[] { new[] { "story_yeonbi", "na_yerin" }, new[] { "story_yeonbi", "na_yerin" }, new[] { "story_yeonbi", "na_yerin" }, new[] { "na_yerin" } },
                Trials = new TrialRule[] { TrialRule.Survive("현천은린과 검막", "적 4행동을 견디며 화살의 방향을 읽으세요.", 4), TrialRule.Survive("물 위의 우산", "적 4행동을 견디며 수중의 공세에 대응하세요.", 4), TrialRule.Survive("배후가 입을 열 때까지", "적 6행동을 견디세요. 일부러 밀리는 연출은 전투 실패가 아닙니다.", 6), null } },
            new StageData { Number = 41, Name = "달의 이슬과 닫힌 방문", RecommendedLevel = 82, DooTotal = 25500, RewardWeightBudget = 3,
                Prologue = StoryMidgame.Prologues[41], Intro = StoryMidgame.Intros[41], Outro = StoryMidgame.Outros[41],
                Encounters = new[] { new[] { "baekmu_ju" } },
                StoryActors = new[] { new[] { "story_yeonbi" } } },
            new StageData { Number = 42, Name = "정신봉과 배 세 척", RecommendedLevel = 84, DooTotal = 27000, RewardWeightBudget = 6,
                Prologue = StoryTournament.Prologues[42], Intro = StoryTournament.Intros[42], Outro = StoryTournament.Outros[42],
                Encounters = new[] { new[] { "heukryong_anchor" }, new[] { "heukryong_shell_one" }, new[] { "heukryong_shell_two" }, new[] { "heukryong_muzzle" } },
                StoryActors = new[] { new[] { "story_biry_staff" }, new[] { "story_biry_staff" }, new[] { "story_biry_staff" }, new[] { "story_biry_staff" } },
                Trials = new TrialRule[] { TrialRule.Survive("철혈묘와 흙파도", "적 4행동을 견디며 공세를 읽으세요.", 4), TrialRule.Technique("첫 벽력포 · 흘리기", "장전 예고 중 벽력포 흘리기를 사용하세요.", "scene_shell_deflect", 20, true), TrialRule.Technique("둘째 벽력포 · 가르기", "장전 예고 중 벽력탄 가르기를 사용하세요.", "scene_shell_cut", 20, true), TrialRule.Technique("셋째 벽력포 · 포구 저지", "장전 예고 중 포구 막기를 사용하세요.", "scene_muzzle_stop", 20, true) } },
            new StageData { Number = 43, Name = "풀린 묵룡환과 날아갈 뜻", RecommendedLevel = 86, DooTotal = 28500, RewardWeightBudget = 5,
                Prologue = StoryTournament.Prologues[43], Intro = StoryTournament.Intros[43], Outro = StoryTournament.Outros[43],
                // the 십이야 and the 현천포월 fights became one scripted showdown (StoryShowdowns.Moon); it pays what the two paid
                Encounters = new[] { new[] { "yecheong_left" }, new[] { "yecheong_twins" } },
                RewardEncounters = new[] { new[] { "yecheong_left" }, new[] { "yecheong_twins", "unstable_black_lightning" } },
                StoryActors = new[] { new[] { "story_yeonbi_left" }, new[] { "story_yeonbi_left" } },
                Trials = new TrialRule[] { TrialRule.Survive("왼손의 현천은린", "적 4행동을 견디며 공세를 읽으세요.", 4), StoryShowdowns.MoonTrial } },
            new StageData { Number = 44, Name = "본선의 다른 검들", RecommendedLevel = 88, DooTotal = 30000, RewardWeightBudget = 7,
                Prologue = StoryTournament.Prologues[44], Intro = StoryTournament.Intros[44], Outro = StoryTournament.Outros[44],
                Encounters = new[] { new[] { "georyeok_wang" }, new[] { "geodo_tournament" }, new[] { "ha_usu" }, new[] { "chae_isu" }, new[] { "jin_ryeong_tournament" } },
                StoryActors = new[] { new[] { "story_ryu_eungyeong" }, new[] { "story_yong_cheongmyeong" }, new[] { "yun_junho" }, new[] { "na_yerin" }, new[] { "story_namgung_sang" } },
                Trials = new TrialRule[] { TrialRule.Technique("바람을 탄 검계", "선풍검계로 공방을 마무리하세요.", "scene_seonpung", 20, false), null, TrialRule.Technique("윤미의 칠매검", "칠매검을 펼치세요. 하우수의 체질과 향기가 맞부딪칩니다.", "scene_chilmae", 20, false), null, TrialRule.Survive("비상련화 대 뇌광단천", "적 6행동을 견디며 공세를 읽으세요.", 6) } },
            new StageData { Number = 45, Name = "검각의 기억과 붉은 피리", RecommendedLevel = 90, DooTotal = 31500, RewardWeightBudget = 6,
                Prologue = StoryTournament.Prologues[45], Intro = StoryTournament.Intros[45], Outro = StoryTournament.Outros[45],
                Encounters = new[] { new[] { "mong_mu", "hwan_mu" }, new[] { "yeongryeong_soan" }, new[] { "yeongryeong_ice" }, new[] { "yeongryeong_flute" } },
                StoryActors = new[] { new[] { "story_yeonbi_left" }, new[] { "na_yerin" }, new[] { "na_yerin" }, new[] { "na_yerin" } },
                Trials = new TrialRule[] { null, TrialRule.Technique("몸이 먼저 기억한 검", "소안검을 사용해 익숙한 반응을 끌어내세요.", "scene_soan", 20, false), TrialRule.Technique("함께 익힌 한빙기", "한빙섬옥으로 공동 수련의 기억을 잇습니다.", "scene_hanbing", 20, false), TrialRule.Technique("회접 뒤, 마지막 검", "해상비조천참절을 사용하세요. 외부 조종을 끊는 지원은 이야기로 이어집니다.", "scene_haesang", 20, false) } },
            new StageData { Number = 46, Name = "관 속의 과거와 천외일도", RecommendedLevel = 92, DooTotal = 33000, RewardWeightBudget = 7,
                Prologue = StoryTournament.Prologues[46], Intro = StoryTournament.Intros[46], Outro = StoryTournament.Outros[46],
                // the 봉황환 and the 묵뢰살 fights became one scripted showdown (StoryShowdowns.Cheonoe); it pays what the two paid
                Encounters = new[] { new[] { "chilsang_broken" }, new[] { "chilsang_mist" }, new[] { "chilsang_twins" }, new[] { "chilsang_heart" } },
                RewardEncounters = new[] { new[] { "chilsang_broken" }, new[] { "chilsang_mist" }, new[] { "chilsang_twins" }, new[] { "unsealed_current", "chilsang_heart" } },
                StoryActors = new[] { new[] { "story_yeonbi_left" }, new[] { "story_yeonbi_left" }, new[] { "story_yeonbi_left" }, new[] { "story_yeonbi_left" } },
                Trials = new TrialRule[] { null, TrialRule.Technique("혈무막막의 틈", "안개가 모이는 예고 중 천지무견 자다봉창으로 대응하세요.", "scene_umbrella_spear", 20, true), TrialRule.Survive("족쇄를 벗은 쌍도", "적 6행동을 견디며 공세를 읽으세요.", 6), StoryShowdowns.CheonoeTrial } },
            new StageData { Number = 47, Name = "남겨진 흔적과 도움의 청", RecommendedLevel = 94, DooTotal = 34500, RewardWeightBudget = 2, RewardScale = 2.0,
                Prologue = StoryTournament.Prologues[47], Intro = StoryTournament.Intros[47], Outro = StoryTournament.Outros[47],
                Encounters = new[] { new[] { "horn_trap" } },
                StoryActors = new[] { new[] { "story_biry_search", "jang_hong", "hyo_ryong" } },
                Trials = new TrialRule[] { TrialRule.Survive("폭염진의 퇴로", "적 4행동을 견디며 공세를 읽으세요.", 4) } },
            new StageData { Number = 48, Name = "마천총령과 네 전선", RecommendedLevel = 96, DooTotal = 36000, RewardWeightBudget = 7, RewardScale = 13.0 / 11.0,
                Prologue = StoryTournament.Prologues[48], Intro = StoryTournament.Intros[48], Outro = StoryTournament.Outros[48],
                Encounters = new[] { new[] { "academy_alarm_guard", "academy_alarm_guard", "academy_alarm_guard" }, new[] { "cheolgap_front", "cheolgap_front" }, new[] { "cheolgap_rear", "cheolgap_rear" } },
                StoryActors = new[] { new[] { "story_biry_half", "hyo_ryong", "jang_hong" }, new[] { "story_biry_half", "hyo_ryong" }, new[] { "story_biry_half", "hyo_ryong" } },
                Trials = new TrialRule[] { null, null, null } },
            new StageData { Number = 49, Name = "반탄강과 여든한 칸", RecommendedLevel = 98, DooTotal = 37500, RewardWeightBudget = 9,
                Prologue = StoryRescue.Prologues[49], Intro = StoryRescue.Intros[49], Outro = StoryRescue.Outros[49],
                Encounters = new[] { new[] { "rakbio_five" }, new[] { "rakbio_air" }, new[] { "stone_grid" }, new[] { "rakbio_armor" }, new[] { "rakbio_shards" }, new[] { "rakbio_mind" }, new[] { "rakbio_bronze" } },
                StoryActors = new[] { new[] { "story_biry_rescue" }, new[] { "story_biry_rescue" }, new[] { "story_biry_rescue" }, new[] { "story_biry_rescue" }, new[] { "story_biry_rescue" }, new[] { "story_biry_rescue" }, new[] { "story_biry_rescue" } },
                Trials = new TrialRule[] { TrialRule.Survive("돌림판 오 · 다섯 타격", "적 5행동을 견디며 목적을 지키세요.", 5), TrialRule.Technique("격공장", "격공장 기술로 대응하세요.", "scene_air_push", 25, false), TrialRule.Technique("번호와 칸의 대응", "이번 석추 배치: 왼쪽27·가운데37·오른쪽47번입니다. 37·47번 낙하 예고에 안전한 칸을 선택하세요.", "scene_safe_27", 25, true), TrialRule.Technique("일점집중 · 뇌섬일시", "일점집중 · 뇌섬일시 기술로 대응하세요.", "scene_needle_flash", 25, true), TrialRule.Technique("우중거 불점의", "우중거 불점의 기술로 대응하세요.", "scene_rain_step", 25, true), TrialRule.Technique("마음의 균열 · 심뢰열파", "마음의 균열 · 심뢰열파 기술로 대응하세요.", "scene_mind_wave", 25, false), TrialRule.Technique("팔섬 뇌광굉타", "팔섬 뇌광굉타 기술로 대응하세요.", "scene_eight_shadows", 25, true) } },
            new StageData { Number = 50, Name = "아지랑이를 걷어 낸 검", RecommendedLevel = 100, DooTotal = 39000, RewardWeightBudget = 6,
                Prologue = StoryRescue.Prologues[50], Intro = StoryRescue.Intros[50], Outro = StoryRescue.Outros[50],
                Encounters = new[] { new[] { "jagun_fan" }, new[] { "jagun_rose" }, new[] { "jagun_heat" }, new[] { "jagun_union" } },
                StoryActors = new[] { new[] { "story_mo_yonghwi" }, new[] { "story_mo_yonghwi" }, new[] { "story_mo_yonghwi" }, new[] { "story_mo_yonghwi" } },
                Trials = new TrialRule[] { null, TrialRule.Survive("채찍과 꽃잎 사이", "적 4행동을 견디며 목적을 지키세요.", 4), TrialRule.Technique("눈을 감은 관찰", "산화무영의 열기를 읽으세요. 다른 냉기 기술로 대체할 수 없습니다.", "scene_sense_heat", 25, false), TrialRule.Technique("염천빙지 일원합", "염천빙지 일원합 기술로 대응하세요.", "scene_ice_union", 25, true) } },
            new StageData { Number = 51, Name = "동전을 휘게 하는 바람", RecommendedLevel = 102, DooTotal = 40500, RewardWeightBudget = 6,
                Prologue = StoryRescue.Prologues[51], Intro = StoryRescue.Intros[51], Outro = StoryRescue.Outros[51],
                Encounters = new[] { new[] { "merchant_oh", "merchant_jeong", "merchant_yeo" }, new[] { "false_south_king" }, new[] { "jeonhon_coins" }, new[] { "jeonhon_bag" } },
                StoryActors = new[] { new[] { "story_namgung_sansan", "story_hyeon_un" }, new[] { "story_namgung_sansan" }, new[] { "story_hyeon_un" }, new[] { "story_hyeon_un" } },
                Trials = new TrialRule[] { null, null, TrialRule.Technique("알까기에서 익힌 대응", "알까기에서 익힌 대응 기술로 대응하세요.", "scene_coin_parry", 25, true), TrialRule.Technique("풍우만곡 · 풍인참", "파산선고 예고에 풍우만곡으로 궤적을 바꾸세요. 결판은 풍인참 장면으로 이어집니다.", "scene_bend_wind", 25, true) } },
            new StageData { Number = 52, Name = "빈 관과 안쪽에서 열린 눈", RecommendedLevel = 104, DooTotal = 42000, RewardWeightBudget = 2,
                Prologue = StoryRescue.Prologues[52], Intro = StoryRescue.Intros[52], Outro = StoryRescue.Outros[52],
                Encounters = new[] { new[] { "north_route_guard", "north_route_guard" }, new[] { "sealed_meridians" } },
                StoryActors = new[] { new[] { "jang_hong", "story_ok_yugyeong" }, new[] { "na_yerin" } },
                Trials = new TrialRule[] { null, TrialRule.Technique("개안 · 서른여섯 금침", "흐름을 관조해 금제와 해제 방법을 찾으세요. 이번 장면은 해제 작업의 시작입니다.", "scene_read_needles", 25, false) } },
            new StageData { Number = 53, Name = "비우고 품은 한 마리 용", RecommendedLevel = 106, DooTotal = 43500, RewardWeightBudget = 3,
                Prologue = StoryRescue.Prologues[53], Intro = StoryRescue.Intros[53], Outro = StoryRescue.Outros[53],
                Encounters = new[] { new[] { "coffin_assassin" }, new[] { "three_currents" }, new[] { "west_deputy" } },
                StoryActors = new[] { new[] { "hyo_ryong" }, new[] { "story_biry_rescue" }, new[] { "story_biry_rescue" } },
                Trials = new TrialRule[] { TrialRule.Technique("효룡의 관찰과 암습 저지", "효룡의 관찰과 암습 저지 기술로 대응하세요.", "scene_flying_twin", 25, true), TrialRule.Technique("공저물사 · 포룡귀원", "비우는 호흡으로 세 기운을 한자리에 품으세요.", "scene_empty_nest", 25, false), TrialRule.Technique("두 손가락과 황금 실", "두 손가락과 황금 실 기술로 대응하세요.", "scene_golden_thread", 25, true) } },
            new StageData { Number = 54, Name = "늑대의 진과 세 사람의 믿음", RecommendedLevel = 108, DooTotal = 45000, RewardWeightBudget = 7,
                Prologue = StoryRescue.Prologues[54], Intro = StoryRescue.Intros[54], Outro = StoryRescue.Outros[54],
                Encounters = new[] { new[] { "armored_wolf", "armored_wolf", "armored_wolf" }, new[] { "wolf_array" }, new[] { "changrang_shadows" }, new[] { "changrang_final" }, new[] { "changranga" } },
                StoryActors = new[] { new[] { "story_hyeon_un", "story_mo_yonghwi", "story_gongson_jeolhwi" }, new[] { "story_hyeon_un", "story_mo_yonghwi", "story_gongson_jeolhwi" }, new[] { "story_hyeon_un", "story_mo_yonghwi", "story_gongson_jeolhwi" }, new[] { "story_hyeon_un", "story_mo_yonghwi", "story_gongson_jeolhwi" }, new[] { "story_biry_rescue" } },
                Trials = new TrialRule[] { null, TrialRule.Technique("풍인벽으로 여는 길", "풍인벽으로 여는 길 기술로 대응하세요.", "scene_wind_wall", 25, true), TrialRule.Technique("지존군림보를 믿는다", "지존군림보를 믿는다 기술로 대응하세요.", "scene_gongson_support", 25, true), TrialRule.Technique("은하멸멸 · 섬", "은하멸멸 · 섬 기술로 대응하세요.", "scene_stars_end", 25, true), TrialRule.Technique("마지막 지원", "마지막 지원 기술로 대응하세요.", "scene_wolf_stop", 25, true) } },
            new StageData { Number = 55, Name = "돌아온 날개와 세 수의 거리", RecommendedLevel = 110, DooTotal = 46500, RewardWeightBudget = 5,
                Prologue = StoryRescue.Prologues[55], Intro = StoryRescue.Intros[55], Outro = StoryRescue.Outros[55],
                Encounters = new[] { new[] { "last_needles" }, new[] { "cell_watch" }, new[] { "mumyeong_escape" }, new[] { "mumyeong_steps" }, new[] { "mumyeong_three" } },
                StoryActors = new[] { new[] { "na_yerin" }, new[] { "na_yerin" }, new[] { "na_yerin" }, new[] { "story_biry_rescue" }, new[] { "story_biry_rescue" } },
                Trials = new TrialRule[] { TrialRule.Technique("마지막 두 금침", "마지막 두 금침 기술로 대응하세요.", "scene_release_needles", 25, false), TrialRule.Technique("관찰과 유인", "간수의 동선을 읽고 문을 열게 한 뒤 검을 회수하세요.", "scene_open_door", 25, false), TrialRule.Technique("기력이 남은 마지막 검", "해상비조천참절로 맞서세요. 이어지는 기절은 고정 이야기 결과입니다.", "scene_escape_sword", 25, true), TrialRule.Technique("순신과 뇌령사", "순신과 뇌령사 기술로 대응하세요.", "scene_return_yerin", 25, true), TrialRule.Responses("삼초지적 · 승부 중단", "무흔권에는 맞선 주먹, 무상장에는 극쾌식, 무형검에는 뇌광류하곡으로 차례로 대응하세요. 무명을 쓰러뜨리는 목표가 아닙니다.", new[] { "scene_fist_clash", "scene_rain_step", "scene_roegwang_response" }, new[] { "무흔권", "무상장", "무형검" }, 35) } },
            new StageData { Number = 56, Name = "남겨진 자리와 출항 명령", RecommendedLevel = 112, DooTotal = 48000, RewardWeightBudget = 7,
                Prologue = StoryRescue.Prologues[56], Intro = StoryRescue.Intros[56], Outro = StoryRescue.Outros[56],
                Encounters = new[] { new[] { "dorm_siege" }, new[] { "seocheon_delay" }, new[] { "first_unit", "first_unit", "first_unit" }, new[] { "seocheon_whirl" }, new[] { "seocheon_scar" } },
                StoryActors = new[] { new[] { "story_namgung_sang", "story_dangsam", "story_baek_muyeong" }, new[] { "story_namgung_sang" }, new[] { "story_biry_rescue" }, new[] { "story_biry_rescue" }, new[] { "story_biry_rescue" } },
                Trials = new TrialRule[] { TrialRule.Technique("안락휴와 부채질", "당문 남매의 안락휴를 백무영의 부채질로 퍼뜨리세요.", "scene_sleep_fan", 25, false), TrialRule.Survive("남궁상의 지연", "적 4행동을 견디며 목적을 지키세요.", 4), null, TrialRule.Technique("회선용권 · 상살", "회선용권 · 상살 기술로 대응하세요.", "scene_silver_whirl", 25, true), TrialRule.Technique("심뢰 극 천형인", "출항을 확보한 뒤 천형인으로 상흔을 남기고 이탈하세요. 서천 처치가 목표가 아닙니다.", "scene_soul_scar", 25, true) } },
            new StageData { Number = 57, Name = "장송곡과 흰 검의 누명", RecommendedLevel = 114, DooTotal = 49500, RewardWeightBudget = 4, RewardScale = 4.0 / 3.0,
                Prologue = StoryRescue.Prologues[57], Intro = StoryRescue.Intros[57], Outro = StoryRescue.Outros[57],
                Encounters = new[] { new[] { "lake_pursuit" } },
                StoryActors = new[] { new[] { "story_biry_rescue" } },
                Trials = new TrialRule[] { TrialRule.Technique("장송곡 · 편수 풍신", "다섯 비뢰도로 물기둥을 일으켜 추격을 차단하세요.", "scene_water_funeral", 25, true) } },
            new StageData { Number = 58, Name = "상실 뒤에 오른 연화기", RecommendedLevel = 116, DooTotal = 51000, RewardWeightBudget = 3,
                Prologue = StoryEscape.Prologues[58], Intro = StoryEscape.Intros[58], Outro = StoryEscape.Outros[58],
                Encounters = new[] { new string[0], new string[0], new string[0] },
                RewardEncounters = new[] { new[] { "departing_ferry" }, new[] { "hubei_escort" }, new[] { "south_checkpoint" } },
                Events = new[] { StoryChoiceEvents.DepartingFerry, StoryChoiceEvents.HubeiEscort, StoryChoiceEvents.SouthCheckpoint },
                StoryActors = new[] { new[] { "na_yerin" }, new[] { "story_biry_escape", "jang_hong" }, new[] { "story_biry_escape", "story_bing_geom" } },
                Trials = new TrialRule[] { null, null, null } },
            new StageData { Number = 59, Name = "남겨 둔 다섯째 거점", RecommendedLevel = 118, DooTotal = 52500, RewardWeightBudget = 3,
                Prologue = StoryEscape.Prologues[59], Intro = StoryEscape.Intros[59], Outro = StoryEscape.Outros[59],
                Encounters = new[] { new string[0], new string[0], new string[0] },
                RewardEncounters = new[] { new[] { "ruined_contacts" }, new[] { "last_report" }, new[] { "inn_encirclement" } },
                Events = new[] { StoryChoiceEvents.RuinedContacts, StoryChoiceEvents.LastReport, StoryChoiceEvents.InnEncirclement },
                StoryActors = new[] { new[] { "story_biry_escape", "jang_hong", "story_mo_yonghwi", "story_namgung_sang" }, new[] { "story_biry_escape", "jang_hong", "story_mo_yonghwi", "story_namgung_sang" }, new[] { "na_yerin" } },
                Trials = new TrialRule[] { null, null, null } },
            new StageData { Number = 60, Name = "두 노사가 남긴 시간", RecommendedLevel = 120, DooTotal = 54000, RewardWeightBudget = 6,
                Prologue = StoryEscape.Prologues[60], Intro = StoryEscape.Intros[60], Outro = StoryEscape.Outros[60],
                Encounters = new[] { new[] { "son_gwang_patrol" }, new[] { "twins_fire_l", "twins_fire_r" }, new[] { "muhwa_drain_fire" }, new[] { "muhwa_drain_ice" } },
                StoryActors = new[] { new[] { "story_yeom_do", "story_bing_geom" }, new[] { "story_yeom_do", "story_bing_geom" }, new[] { "story_yeom_do", "story_bing_geom" }, new[] { "story_yeom_do", "story_bing_geom" } },
                Trials = new TrialRule[] { null, TrialRule.Responses("각자 맡은 상대", "목표로 표시된 인물의 기술을 전조에 맞춰 차례로 사용하세요.", new[] { "scene_escape_fire_wall", "scene_escape_ice_guard" }, new[] { "홍염의 방패", "염창아돌" }, 40), TrialRule.Technique("빙검의 구원", "빙검의 구원 기술로 대응하세요.", "scene_escape_save_fire", 30, true), TrialRule.Technique("염도의 구원", "염도의 구원 기술로 대응하세요.", "scene_escape_save_ice", 30, true) } },
            new StageData { Number = 61, Name = "흑견후와 부서진 남문", RecommendedLevel = 122, DooTotal = 55500, RewardWeightBudget = 6,
                Prologue = StoryEscape.Prologues[61], Intro = StoryEscape.Intros[61], Outro = StoryEscape.Outros[61],
                Encounters = new[] { new[] { "heukgyeon_howl" }, new[] { "heukgyeon_net" }, new[] { "heukgyeon_gate" }, new[] { "south_gate_bars" } },
                StoryActors = new[] { new[] { "story_biry_escape" }, new[] { "story_biry_escape" }, new[] { "story_biry_escape" }, new[] { "story_namgung_sang", "jang_hong", "story_mo_yonghwi" } },
                Trials = new TrialRule[] { TrialRule.Technique("뇌절단음", "뇌절단음 기술로 대응하세요.", "scene_escape_cut_sound", 30, true), TrialRule.Technique("천라은망지세 뇌망옥", "천라은망지세 뇌망옥 기술로 대응하세요.", "scene_escape_silver_net", 30, true), TrialRule.Technique("돌아온 말복 삼년구타", "돌아온 말복 삼년구타 기술로 대응하세요.", "scene_escape_returned_fist", 30, true), TrialRule.Responses("동료의 삼인연환", "목표로 표시된 인물의 기술을 전조에 맞춰 차례로 사용하세요.", new[] { "scene_escape_gate_thunder", "scene_escape_gate_gray", "scene_escape_gate_galaxy" }, new[] { "성문 봉쇄", "성문 봉쇄", "성문 봉쇄" }, 40) } },
            new StageData { Number = 62, Name = "웃음소리가 남긴 그림자", RecommendedLevel = 124, DooTotal = 57000, RewardWeightBudget = 6, RewardScale = 8.0 / 7.0,
                Prologue = StoryEscape.Prologues[62], Intro = StoryEscape.Intros[62], Outro = StoryEscape.Outros[62],
                Encounters = new[] { new[] { "third_dog_squad", "third_dog_squad", "third_dog_squad" }, new[] { "baekgu_signal" }, new[] { "heukgyeon_captive" } },
                StoryActors = new[] { new[] { "story_biry_escape", "jang_hong", "story_mo_yonghwi", "story_namgung_sang" }, new[] { "story_biry_escape", "jang_hong", "story_mo_yonghwi", "story_namgung_sang" }, new[] { "story_biry_escape", "jang_hong", "story_mo_yonghwi", "story_namgung_sang" } },
                Trials = new TrialRule[] { null, TrialRule.Technique("차음막과 그림자 묶기", "차음막과 그림자 묶기 기술로 대응하세요.", "scene_escape_shadow_bind", 30, true), TrialRule.Technique("변장 뒤의 몽둥이", "변장 뒤의 몽둥이 기술로 대응하세요.", "scene_escape_dog_capture", 30, true) } },
            new StageData { Number = 63, Name = "합주를 끊는 네 사람", RecommendedLevel = 126, DooTotal = 58500, RewardWeightBudget = 4,
                Prologue = StoryEscape.Prologues[63], Intro = StoryEscape.Intros[63], Outro = StoryEscape.Outros[63],
                Encounters = new[] { new[] { "silent_duet" }, new[] { "rising_duet" }, new[] { "duet_peak" }, new[] { "exit_dog_squad" } },
                StoryActors = new[] { new[] { "story_baek_muyeong", "story_cheong_heun", "story_yong_cheongmyeong", "story_ma_haryeong" }, new[] { "story_baek_muyeong", "story_cheong_heun", "story_yong_cheongmyeong", "story_ma_haryeong" }, new[] { "story_baek_muyeong", "story_cheong_heun", "story_yong_cheongmyeong", "story_ma_haryeong" }, new[] { "story_baek_muyeong", "story_cheong_heun", "story_yong_cheongmyeong", "story_ma_haryeong" } },
                Trials = new TrialRule[] { TrialRule.Technique("백무영의 시선 교란", "백무영의 시선 교란 기술로 대응하세요.", "scene_escape_flute_feint", 30, true), TrialRule.Technique("청흔의 삼검귀일", "청흔의 삼검귀일 기술로 대응하세요.", "scene_escape_three_swords", 30, true), TrialRule.Technique("용천명의 삼중 사자후", "용천명의 삼중 사자후 기술로 대응하세요.", "scene_escape_triple_roar", 30, true), TrialRule.Technique("마하령의 지진각", "마하령의 지진각 기술로 대응하세요.", "scene_escape_earth_wave", 30, true) } },
            new StageData { Number = 64, Name = "폭풍 속에서 돌아온 사자", RecommendedLevel = 128, DooTotal = 60000, RewardWeightBudget = 6,
                Prologue = StoryEscape.Prologues[64], Intro = StoryEscape.Intros[64], Outro = StoryEscape.Outros[64],
                Encounters = new[] { new[] { "hyohyo_spear" }, new[] { "hyohyo_storm" }, new[] { "hyomin_binding" }, new[] { "hyomyo_smoke" } },
                StoryActors = new[] { new[] { "na_yerin" }, new[] { "na_yerin" }, new[] { "na_yerin" }, new[] { "story_yeongryeong" } },
                Trials = new TrialRule[] { TrialRule.Technique("비설보와 용안", "비설보와 용안 기술로 대응하세요.", "scene_escape_spear_read", 30, true), TrialRule.Technique("폭풍비상 비홍관운", "폭풍비상 비홍관운 기술로 대응하세요.", "scene_escape_storm_wings", 30, true), TrialRule.Technique("검을 놓지 않는 의지", "검을 놓지 않는 의지 기술로 대응하세요.", "scene_escape_hold_sword", 30, true), TrialRule.Technique("영령의 귀환과 연막", "영령의 귀환과 연막 기술로 대응하세요.", "scene_escape_sister_smoke", 30, true) } },
            new StageData { Number = 65, Name = "삼천의 틈과 붉은 꽃", RecommendedLevel = 130, DooTotal = 61500, RewardWeightBudget = 4, RewardScale = 4.0 / 3.0,
                Prologue = StoryEscape.Prologues[65], Intro = StoryEscape.Intros[65], Outro = StoryEscape.Outros[65],
                Encounters = new[] { new[] { "three_sects" }, new[] { "sealed_hint" }, new[] { "dae_nan_current" } },
                StoryActors = new[] { new[] { "story_biry_escape", "jang_hong", "story_mo_yonghwi", "story_namgung_sang" }, new[] { "story_biry_escape", "jang_hong", "story_mo_yonghwi", "story_namgung_sang" }, new[] { "story_biry_escape", "jang_hong", "story_mo_yonghwi", "story_namgung_sang" } },
                Trials = new TrialRule[] { TrialRule.Technique("뇌령사를 건 세 문파", "뇌령사를 건 세 문파 기술로 대응하세요.", "scene_escape_sect_threads", 30, false), TrialRule.Technique("장홍의 암시 봉인 해제", "장홍의 암시 봉인 해제 기술로 대응하세요.", "scene_escape_red_flower", 30, false), TrialRule.Technique("선행 진입과 생존 확인", "선행 진입과 생존 확인 기술로 대응하세요.", "scene_escape_current", 30, true) } },
            new StageData { Number = 66, Name = "불꽃을 넘어 남쪽으로", RecommendedLevel = 132, DooTotal = 63000, RewardWeightBudget = 7,
                Prologue = StoryEscape.Prologues[66], Intro = StoryEscape.Intros[66], Outro = StoryEscape.Outros[66],
                Encounters = new[] { new[] { "mothers_fire_ice" }, new[] { "muhwa_hostage" }, new[] { "flame_prison" }, new[] { "ice_dragon_wall" }, new[] { "rescue_exit" } },
                StoryActors = new[] { new[] { "story_biry_escape", "jang_hong", "story_mo_yonghwi", "story_namgung_sang" }, new[] { "story_biry_escape" }, new[] { "story_biry_escape" }, new[] { "story_biry_escape" }, new[] { "jang_hong", "story_mo_yonghwi", "story_namgung_sang" } },
                Trials = new TrialRule[] { TrialRule.Technique("풍신 질풍", "풍신 질풍 기술로 대응하세요.", "scene_escape_clear_elements", 30, true), TrialRule.Technique("뇌령사로 되찾는 사람", "뇌령사로 되찾는 사람 기술로 대응하세요.", "scene_escape_free_father", 30, true), TrialRule.Technique("화룡난류", "화룡난류 기술로 대응하세요.", "scene_escape_fire_dragon", 30, true), TrialRule.Technique("쌍룡귀천", "쌍룡귀천 기술로 대응하세요.", "scene_escape_twin_dragons", 30, true), TrialRule.Survive("부상자를 업은 이탈", "적 4행동을 견디며 이탈할 시간을 확보하세요.", 4) } },
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
            for (int k = 0; k < st.Encounters.Length; k++) foreach (var id in RewardIds(st, k)) w += RewardWeight(id);
            return w;
        }

        /// <summary>The enemies a fight pays exp, 두 and drops for: its line-up before 전투 인원 확장 (<see cref="StageData.RewardEncounters"/>), else its real one.</summary>
        public static string[] RewardIds(StageData st, int fight)
        {
            return st.RewardEncounters != null && fight < st.RewardEncounters.Length ? st.RewardEncounters[fight] : st.Encounters[fight];
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
            return (long)Math.Ceiling((double)StageExpSpan(st) * RewardWeight(enemyId) * st.RewardScale / RewardDenominator(st));
        }

        /// <summary>Exp between this stage's level and the next one's: the pool <see cref="ExpReward"/> shares out by weight.</summary>
        private static long StageExpSpan(StageData st)
        {
            var next = FindStage(st.Number + 1);
            // the last stage has no next one: it spans the usual two curve steps, so it pays like the others and still ends at its recommended level + 1
            double to = (next != null ? ExactPlayerLevel(next) : ExactPlayerLevel(st) + 2.0 / Progression.CurvePerPlayerLevel) + LevelHeadroom;
            double from = st.Number > 1 ? ExactPlayerLevel(st) + LevelHeadroom : st.RecommendedPlayerLevel;
            return Math.Max(0L, CumulativeExpAt(to) - CumulativeExpAt(from));
        }

        /// <summary>Each follower added by 전투 인원 확장 pays this share of an ordinary enemy's exp and 두, on top of the original line-up.</summary>
        public const double FollowerRewardShare = 0.5;

        /// <summary>How many more enemies a fight holds than its line-up before 전투 인원 확장 (0 for quests, tower floors and untouched fights).</summary>
        public static int FollowerCount(StageData st, int fight)
        {
            if (st.RewardEncounters == null || fight < 0 || fight >= st.RewardEncounters.Length || fight >= st.Encounters.Length) return 0;
            return Math.Max(0, st.Encounters[fight].Length - st.RewardEncounters[fight].Length);
        }

        /// <summary>Extra exp a fight pays for its added followers (<see cref="FollowerRewardShare"/> of an ordinary enemy each).</summary>
        public static long FollowerExp(StageData st, int fight)
        {
            int n = FollowerCount(st, fight);
            return n == 0 ? 0 : (long)Math.Ceiling(StageExpSpan(st) * FollowerRewardShare * n / RewardDenominator(st));
        }

        /// <summary>Extra 두 a fight pays for its added followers.</summary>
        public static long FollowerDoo(StageData st, int fight)
        {
            int n = FollowerCount(st, fight);
            return n == 0 ? 0 : (long)Math.Round(st.DooTotal * StageDooFactor * FollowerRewardShare * n / RewardDenominator(st));
        }

        /// <summary>Stage 두 payouts are raised by this factor on top of <see cref="StageData.DooTotal"/> (clears, replays and quests alike).</summary>
        public const double StageDooFactor = 1.8;   // x1.5 more 두 from fights than before (was 1.2)

        public static long DooReward(StageData st, string enemyId)
        {
            return (long)Math.Round((double)st.DooTotal * StageDooFactor * RewardWeight(enemyId) * st.RewardScale / RewardDenominator(st));
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
            if (id == "ny_biseolbo") id = "ny_biseol";   // legacy lesson and the level-learned art are the same footwork
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
