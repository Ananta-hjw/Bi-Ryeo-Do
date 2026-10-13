using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>Plain-data martial art entry. The Editor setup turns these into MartialArtDef assets.</summary>
    public sealed class ArtData
    {
        public string Id, Name, Description;
        public float Power;
        public int QiCost, Cooldown, UnlockStage;
        public long LearnCost;
    }

    /// <summary>Plain-data enemy. Stats derive from the player curve at <see cref="Level"/> so difficulty tracks Progression.</summary>
    public sealed class EnemyData
    {
        public string Id, Name;
        public bool IsBoss;
        public int Level;
        public float HpMul = 1f, AtkMul = 1f, DefMul = 1f;
        public int SpeedAdd;

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
                CritRate = IsBoss ? 0.05f : 0.01f,
                CritDamage = 1.5f
            };
        }

        /// <summary>About eight kills per level at equal level; bosses pay six times as much.</summary>
        public long ExpReward { get { return (long)Math.Round(Progression.ExpToNext(Level) / 8.0 * (IsBoss ? 6 : 1)); } }
        public long DooReward { get { return (long)Math.Round(Progression.ExpToNext(Level) / 14.0 * (IsBoss ? 6 : 1)); } }
    }

    public sealed class StageData
    {
        public int Number, KillsToBoss;
        public string Name, Story, BossId;
        public string[] EnemyIds;
    }

    /// <summary>
    /// Early-game content (stages 1-7) taken from the analyzed volumes 1-3 (see Story/GAME_MAPPING.md).
    /// Names and events are canon; every number here is a placeholder balance value.
    /// </summary>
    public static class EarlyGameContent
    {
        public const string StarterArtId = "bido_throw";

        public static readonly ArtData[] Arts =
        {
            new ArtData { Id = StarterArtId, Name = "비도 던지기", Description = "비뢰도의 기초. 비도를 던져 적을 맞힌다.", Power = 1.2f },
            new ArtData { Id = "samboku_chobok", Name = "삼복구타권법 초복", Description = "사부가 창안한 권법. 초복은 근육을 노린다.", Power = 2.0f, QiCost = 15, Cooldown = 2, LearnCost = 60, UnlockStage = 1 },
            new ArtData { Id = "bunroesu", Name = "분뢰수", Description = "손을 강철처럼 만드는 기공.", Power = 2.8f, QiCost = 20, Cooldown = 2, LearnCost = 300, UnlockStage = 2 },
            new ArtData { Id = "bi_roe_do", Name = "비뢰도", Description = "비도와 뇌령사를 쓰는 비뢰문의 절기.", Power = 4.0f, QiCost = 30, Cooldown = 3, LearnCost = 1500, UnlockStage = 3 },
            new ArtData { Id = "yeobanjang", Name = "여반장", Description = "손바닥을 뒤집는 회전력으로 일격에 제압하는 일장법.", Power = 5.0f, QiCost = 35, Cooldown = 3, LearnCost = 4500, UnlockStage = 5 },
            new ArtData { Id = "roegwang_ryuhagok", Name = "뢰광류하곡", Description = "비뢰도 여섯 자루를 동시에 운용하는 초식.", Power = 6.5f, QiCost = 45, Cooldown = 4, LearnCost = 12000, UnlockStage = 6 },
            new ArtData { Id = "seonpung_roejeol", Name = "선풍뇌절", Description = "비뢰도 검기회선 용권의 장. 은빛 섬광이 적의 손을 휘감는다.", Power = 8.5f, QiCost = 55, Cooldown = 5, LearnCost = 30000, UnlockStage = 7 },
        };

        public static readonly EnemyData[] Enemies =
        {
            // Stage 1: 아미산 (1권)
            new EnemyData { Id = "tiger", Name = "호랑이", Level = 1, HpMul = 0.7f, AtkMul = 0.9f, SpeedAdd = 8 },
            new EnemyData { Id = "bear", Name = "곰", Level = 1, HpMul = 0.9f, AtkMul = 0.9f, SpeedAdd = -5 },
            new EnemyData { Id = "white_tiger", Name = "백호", Level = 3, IsBoss = true, HpMul = 1.3f, AtkMul = 1.0f, SpeedAdd = 6 },
            // Stage 2: 중양표국 (1권)
            new EnemyData { Id = "biaosa", Name = "표사", Level = 5, HpMul = 0.9f, AtkMul = 0.9f },
            new EnemyData { Id = "gang_janghan", Name = "비풍도 강장한", Level = 8, IsBoss = true, HpMul = 1.5f, AtkMul = 1.0f },
            // Stage 3: 호골채 (1권)
            new EnemyData { Id = "hogol_bandit", Name = "호골채 산적", Level = 9, HpMul = 0.9f, AtkMul = 0.9f },
            new EnemyData { Id = "paeho_jung_gwang", Name = "패호 중광", Level = 12, IsBoss = true, HpMul = 1.5f, AtkMul = 1.0f },
            // Stage 4: 용골채 (1권)
            new EnemyData { Id = "yonggol_bandit", Name = "용골채 산적", Level = 13, HpMul = 0.9f, AtkMul = 0.9f },
            new EnemyData { Id = "paeryong_wooak", Name = "패룡 우악", Level = 17, IsBoss = true, HpMul = 1.5f, AtkMul = 1.0f },
            // Stage 5: 윤하루 (2권)
            new EnemyData { Id = "jang_ugang", Name = "장우강", Level = 19, HpMul = 0.9f, AtkMul = 0.9f },
            new EnemyData { Id = "yeomdo", Name = "염도 곽영희", Level = 23, IsBoss = true, HpMul = 1.8f, AtkMul = 1.1f },
            // Stage 6: 호아장 (2권)
            new EnemyData { Id = "hoajang_disciple", Name = "호아장 제자", Level = 24, HpMul = 0.9f, AtkMul = 0.9f },
            new EnemyData { Id = "ho_cheonsang", Name = "호아장주 호천상", Level = 28, IsBoss = true, HpMul = 1.7f, AtkMul = 1.05f },
            // Stage 7: 운향정 (3권)
            new EnemyData { Id = "jongri_hak", Name = "종리학", Level = 28, HpMul = 0.9f, AtkMul = 0.9f },
            new EnemyData { Id = "gu_chang", Name = "일도파쇄 구창", Level = 29, HpMul = 0.95f, AtkMul = 0.95f },
            new EnemyData { Id = "wiji_cheon", Name = "선풍검룡 위지천", Level = 33, IsBoss = true, HpMul = 1.8f, AtkMul = 1.1f },
        };

        public static readonly StageData[] Stages =
        {
            new StageData { Number = 1, Name = "아미산 숲", KillsToBoss = 8,
                Story = "사부의 모옥 아래 아미산 숲. 장작 패기와 빨래로 위장한 수련 끝에 곰과 호랑이가 사냥감이 되었다. 숲 깊은 곳에는 백호가 있다.",
                EnemyIds = new[] { "tiger", "bear" }, BossId = "white_tiger" },
            new StageData { Number = 2, Name = "중양표국", KillsToBoss = 10,
                Story = "당철영이 집단구타를 당했다. 주작단 열여섯과 함께 중양표국을 습격해 보상을 받아내야 한다.",
                EnemyIds = new[] { "biaosa" }, BossId = "gang_janghan" },
            new StageData { Number = 3, Name = "호골채 매복", KillsToBoss = 10,
                Story = "남창으로 가는 사천 경계 산길. 녹림 72채 중 호골채 오백 명이 극비 표물을 노리고 매복해 있다.",
                EnemyIds = new[] { "hogol_bandit" }, BossId = "paeho_jung_gwang" },
            new StageData { Number = 4, Name = "용골채 매복", KillsToBoss = 10,
                Story = "호골채에 이어 용골채 천 명이 길을 막았다. 표사 사상자는 한 명도 내서는 안 된다.",
                EnemyIds = new[] { "yonggol_bandit" }, BossId = "paeryong_wooak" },
            new StageData { Number = 5, Name = "주주 윤하루", KillsToBoss = 12,
                Story = "새해를 앞두고 사부의 비보를 들고 집을 나왔다. 주주의 객잔 윤하루에 사천제일 도객 염도가 나타났다. 그의 이름을 부르는 것은 금기다.",
                EnemyIds = new[] { "jang_ugang" }, BossId = "yeomdo" },
            new StageData { Number = 6, Name = "남창 호아장", KillsToBoss = 12,
                Story = "천무학관 입관을 위해 승룡패가 필요하다. 남창 호아장에 난입해 장주 호천상과 제자들을 쓰러뜨린다.",
                EnemyIds = new[] { "hoajang_disciple" }, BossId = "ho_cheonsang" },
            new StageData { Number = 7, Name = "천무학관 운향정", KillsToBoss = 12,
                Story = "수석으로 입관한 천무학관. 오검룡 이상만 드는 정자 운향정을 거닐다 칠검룡 위지천이 칼을 뽑았다.",
                EnemyIds = new[] { "jongri_hak", "gu_chang" }, BossId = "wiji_cheon" },
        };

        public static EnemyData FindEnemy(string id)
        {
            foreach (var e in Enemies) if (e.Id == id) return e;
            return null;
        }
    }
}
