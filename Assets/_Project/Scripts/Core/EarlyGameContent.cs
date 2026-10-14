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
                Intro = new[] { "천무학관에 들어가려면 승룡패가 필요하다.", "염도는 무사부로, 비류연은 특별전형으로 입관한다. 호아장이 쥔 승룡패를 빼앗으러 간다." },
                Outro = new[] { "후견인 빙검 관철수가 나타났다. 염도와 일검을 나눈 끝에 승룡패가 넘어온다." },
                Encounters = new[] { new[] { "hoajang_disciple", "hoajang_disciple" }, new[] { "hoajang_disciple", "hoajang_disciple", "hoajang_disciple" }, new[] { "ho_cheonsang", "hoajang_disciple" } } },
            new StageData { Number = 7, Name = "천무학관 검혼관", RecommendedLevel = 19, DooTotal = 3500,
                Intro = new[] { "입관식 날, 지붕에서 낮잠을 자다 효룡과 친구가 되었다. 기숙사 검혼관으로 가는 길에 신입 장홍이 따라붙는다.", "효룡과 장홍이 함께한다. 기숙사 앞 기관 노방에서 호아장의 호천강이 복수의 칼을 뽑았다." },
                Outro = new[] { "호천강의 패왕노호검이 반 토막 났다. 기관이 3단계까지 가동되어 은자 1500냥의 피해가 났지만 정당방위로 무죄다." },
                Encounters = new[] { new[] { "trap_arrow" }, new[] { "trap_needle", "trap_arrow" }, new[] { "trap_log", "trap_needle" }, new[] { "ho_cheongang" } } },
            new StageData { Number = 8, Name = "천무식당", RecommendedLevel = 21, DooTotal = 3500,
                Intro = new[] { "첫 저녁, 천무식당. 화산파의 윤준호가 선배들에게 조롱을 당하고 있다. 매화검법도 칠매검도 쓰지 못한다는 이유다.", "비류연은 식판을 쥐었다. 윤준호가 일행에 합류한다. (싸움은 게임 연출)" },
                Outro = new[] { "모용휘가 나타나 싸움을 말렸다. 윤준호가 검향의 경지에 올랐다는 사실은 아직 본인만 모른다." },
                Encounters = new[] { new[] { "jongri_hak" }, new[] { "jongri_hak", "jongri_hak" }, new[] { "chu_gayeon", "jongri_hak" } } },
            new StageData { Number = 9, Name = "천무학관 운향정", RecommendedLevel = 23, DooTotal = 4000,
                Prologue = new[]
                {
                    new StoryScene { Title = "운향정의 소녀", Lines = new[] {
                        "학관을 둘러보던 비류연은 오검룡 이상만 드는 정자 운향정에서 걸음을 멈췄다. 밤하늘을 담은 듯한 눈을 한 소녀가 홀로 금을 타고 있었다.",
                        "소녀의 곡이 끝나자 비류연은 자기도 모르게 다가갔다. 그리고 허락도 없이 그녀의 입술을 훔쳤다.",
                        "소녀의 검이 번쩍였다. 비류연은 한 걸음 물러났고, 검기는 그의 앞섶만 갈랐다. 소녀는 평소와 달리 눈앞의 남자가 무슨 생각을 하는지 읽을 수 없었다.",
                        "(훗날 비류연은 그녀가 천하제일미로 불리는 빙백봉 나예린이라는 것을 알게 된다. 지금은 이름도 묻지 못했다.)",
                    } },
                },
                Intro = new[] { "수석으로 입관한 천무학관. 오검룡 이상만 드는 정자 운향정. 방금 일어난 일을 곱씹을 틈도 없이 인기척이 몰려온다.", "소녀의 호위대장 선풍검룡 위지천이 칼을 뽑았다. 길을 막는 선배들이 먼저 달려든다. (게임 연출)" },
                Outro = new[] { "선풍뇌절 한 줄기가 위지천의 검을 쳐냈다. 손목은 남겨 두었다.", "소녀는 어느새 사라지고 없었다. 이름조차 묻지 못한 것이 못내 아쉽다. 사람들이 몰려오기 전에 자리를 뜬다." },
                Encounters = new[] { new[] { "gu_chang" }, new[] { "so_cheongun" }, new[] { "gu_chang", "so_cheongun" }, new[] { "wiji_cheon" } } },
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
