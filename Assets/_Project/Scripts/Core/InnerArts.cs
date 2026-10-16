using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>
    /// 심법: passive inner-energy methods that shape the protagonist's build. Each trades something for something else
    /// (stats) and some change how a fight flows (内공 regen, 기세 gain, life steal, opening 기세). Names are game design, not canon.
    /// </summary>
    public sealed class InnerArt
    {
        public string Id, Name, Description;
        public int UnlockStage, UnlockDiff;   // UnlockDiff 0: stage reached, otherwise cleared on that difficulty
        public long LearnCost;
        public float HpPct, QiPct, AtkPct, DefPct, CritAdd, CritDmgAdd, Lifesteal;
        public int SpeedAdd, QiRegen, SpiritDealt, SpiritTaken, StartSpirit;
    }

    public static class InnerArts
    {

        // Learning ladder: 일반 stages first, then the 어려움 stages (UnlockDiff 1), ending with 무극대성 at 어려움 30.
        public static readonly InnerArt[] All =
        {
            new InnerArt { Id = "breath", Name = "기초 호흡법", UnlockStage = 1, LearnCost = 0, Description = "기본 호흡. 오래 싸워도 숨이 차지 않는다.", QiPct = 0.15f, QiRegen = 3 },
            new InnerArt { Id = "vigor", Name = "강건심법", UnlockStage = 3, LearnCost = 2000, Description = "몸을 단단히 다지는 수련.", HpPct = 0.15f, DefPct = 0.06f },
            new InnerArt { Id = "tiger", Name = "맹호심법", UnlockStage = 5, LearnCost = 4000, Description = "공격에 모든 기운을 싣는다. 방어는 허술해진다.", AtkPct = 0.15f, DefPct = -0.05f },
            new InnerArt { Id = "swift", Name = "경공심법", UnlockStage = 7, LearnCost = 6000, Description = "가볍게 움직여 먼저 치고 급소를 노린다.", SpeedAdd = 6, CritAdd = 0.05f },
            new InnerArt { Id = "life", Name = "생기심법", UnlockStage = 9, LearnCost = 7000, Description = "때릴 때마다 상대의 기운을 조금 빼앗아 회복한다.", Lifesteal = 0.12f, HpPct = 0.06f },
            new InnerArt { Id = "spirit", Name = "투지심법", UnlockStage = 11, LearnCost = 3500, Description = "기세가 빨리 오른다. 싸움을 기세 30으로 시작한다.", SpiritDealt = 6, SpiritTaken = 4, StartSpirit = 30 },
            new InnerArt { Id = "calm", Name = "명경지수", UnlockStage = 13, LearnCost = 4000, Description = "마음이 고요해 내공이 넘친다. 공격은 다소 무뎌진다.", QiPct = 0.35f, QiRegen = 6, AtkPct = -0.06f },
            new InnerArt { Id = "fury", Name = "광폭심법", UnlockStage = 15, LearnCost = 8000, Description = "급소를 노린 일격에 모든 것을 건다. 몸은 상한다.", CritDmgAdd = 0.6f, CritAdd = 0.06f, HpPct = -0.10f },
            new InnerArt { Id = "wall", Name = "철벽심법", UnlockStage = 17, LearnCost = 5000, Description = "어떤 공격도 버텨 내는 수비의 심법. 몸놀림은 느려진다.", DefPct = 0.18f, SpeedAdd = -3 },
            new InnerArt { Id = "blood", Name = "혈투심법", UnlockStage = 19, LearnCost = 12000, Description = "피를 흘리며 싸울수록 강해지는 거친 심법. 방어를 버린다.", AtkPct = 0.18f, Lifesteal = 0.08f, DefPct = -0.10f },
            new InnerArt { Id = "ironbone", Name = "철골심법", UnlockStage = 21, LearnCost = 16000, Description = "뼈와 근육을 쇠처럼 단련한다. 몸놀림은 조금 무뎌진다.", HpPct = 0.20f, DefPct = 0.10f, SpeedAdd = -3 },
            new InnerArt { Id = "stream", Name = "청류심법", UnlockStage = 23, LearnCost = 18000, Description = "맑은 물처럼 막힘없이 흐르는 기운. 내공이 마르지 않는다.", QiPct = 0.30f, QiRegen = 8, HpPct = 0.06f, AtkPct = 0.05f },
            new InnerArt { Id = "thunder", Name = "뇌정심법", UnlockStage = 25, LearnCost = 14000, Description = "번개 같은 선공. 몸은 약해지지만 기세를 품고 시작한다.", SpeedAdd = 9, CritAdd = 0.09f, StartSpirit = 45, HpPct = -0.09f },
            new InnerArt { Id = "unity", Name = "천지합일", UnlockStage = 28, LearnCost = 26000, Description = "몸과 기운이 하나가 되어 모든 면이 고르게 오른다.", HpPct = 0.09f, QiPct = 0.09f, AtkPct = 0.09f, DefPct = 0.09f, QiRegen = 3 },
            new InnerArt { Id = "breaker", Name = "파천심법", UnlockStage = 5, UnlockDiff = 1, LearnCost = 32000, Description = "하늘도 가를 듯한 한 방의 기세. 방어는 내려놓는다.", AtkPct = 0.22f, CritAdd = 0.08f, CritDmgAdd = 0.3f, DefPct = -0.08f },
            new InnerArt { Id = "immortal", Name = "불괴심법", UnlockStage = 10, UnlockDiff = 1, LearnCost = 36000, Description = "무너지지 않는 몸. 맞는 만큼 기운이 돌아온다.", HpPct = 0.28f, DefPct = 0.16f, Lifesteal = 0.04f, SpeedAdd = -4 },
            new InnerArt { Id = "flash", Name = "섬전심법", UnlockStage = 15, UnlockDiff = 1, LearnCost = 40000, Description = "눈에 보이기 전에 끝낸다. 대신 몸이 가볍다.", SpeedAdd = 12, CritAdd = 0.10f, StartSpirit = 40, SpiritDealt = 4, HpPct = -0.08f },
            new InnerArt { Id = "harmony", Name = "합벽심법", UnlockStage = 20, UnlockDiff = 1, LearnCost = 50000, Description = "몸과 마음, 기와 힘이 맞물려 어느 하나 모자람이 없다.", HpPct = 0.12f, QiPct = 0.12f, AtkPct = 0.12f, DefPct = 0.12f, QiRegen = 4, SpiritDealt = 3 },
            new InnerArt { Id = "bloodking", Name = "혈왕심법", UnlockStage = 25, UnlockDiff = 1, LearnCost = 60000, Description = "피를 빨아 살아남는 패왕의 심법. 방어와 체력을 내놓는다.", AtkPct = 0.28f, Lifesteal = 0.15f, CritDmgAdd = 0.4f, DefPct = -0.14f, HpPct = -0.06f },
            new InnerArt { Id = "supreme", Name = "무극대성", UnlockStage = 30, UnlockDiff = 1, LearnCost = 90000, Description = "모든 심법의 끝. 어느 한 곳도 잃지 않고 모든 면이 오른다.", HpPct = 0.15f, QiPct = 0.15f, AtkPct = 0.15f, DefPct = 0.15f, CritAdd = 0.06f, SpeedAdd = 4, QiRegen = 4, Lifesteal = 0.05f, StartSpirit = 20 },
        };

        public static InnerArt Find(string id)
        {
            foreach (var a in All) if (a.Id == id) return a;
            return null;
        }

        /// <summary>Slots: one from the start, a second at stage 10, a third at stage 20, a fourth once 어려움 stage 15 is cleared.</summary>
        public static int SlotCount(SaveData save) { return 1 + (save.ClearedStage >= 10 ? 1 : 0) + (save.ClearedStage >= 20 ? 1 : 0) + (StageStars.StageCleared(save, FourthSlotStage, 1) ? 1 : 0); }
        public const int FourthSlotStage = 15;

        public static bool IsUnlocked(SaveData save, InnerArt a) { return StageStars.Reached(save, a.UnlockStage, a.UnlockDiff); }
        public static string UnlockText(InnerArt a) { return StageStars.ReachText(a.UnlockStage, a.UnlockDiff); }
        public static bool IsLearned(SaveData save, string id) { return save.LearnedInner.Contains(id); }
        public static bool IsEquipped(SaveData save, string id) { return save.EquippedInner.Contains(id); }

        public static bool Learn(SaveData save, string id, out string error)
        {
            error = null;
            var a = Find(id);
            if (a == null) { error = "알 수 없는 심법입니다."; return false; }
            if (IsLearned(save, id)) { error = "이미 익혔습니다."; return false; }
            if (!IsUnlocked(save, a)) { error = UnlockText(a) + "해야 합니다."; return false; }
            if (save.Doo < a.LearnCost) { error = "두가 모자랍니다. (" + a.LearnCost.ToString("N0") + "두 필요)"; return false; }
            save.Doo -= a.LearnCost;
            save.LearnedInner.Add(id);
            return true;
        }

        public static bool Equip(SaveData save, string id, out string error)
        {
            error = null;
            if (!IsLearned(save, id)) { error = "먼저 익혀야 합니다."; return false; }
            if (IsEquipped(save, id)) return true;
            if (save.EquippedInner.Count >= SlotCount(save)) { error = "심법 칸이 가득 찼습니다. (" + SlotCount(save) + "칸) 하나를 먼저 해제해 주세요."; return false; }
            save.EquippedInner.Add(id);
            return true;
        }

        public static void Unequip(SaveData save, string id) { save.EquippedInner.Remove(id); }

        /// <summary>Applies the stat part of the equipped methods (after gear).</summary>
        public static CombatStats Apply(CombatStats stats, IEnumerable<string> equipped)
        {
            float hp = 0f, qi = 0f, atk = 0f, def = 0f, crit = 0f, critDmg = 0f;
            int speed = 0;
            foreach (var id in equipped)
            {
                var a = Find(id);
                if (a == null) continue;
                hp += a.HpPct; qi += a.QiPct; atk += a.AtkPct; def += a.DefPct; crit += a.CritAdd; critDmg += a.CritDmgAdd; speed += a.SpeedAdd;
            }
            stats.MaxHp = Math.Max(1, (int)Math.Round(stats.MaxHp * (1f + hp)));
            stats.MaxQi = Math.Max(0, (int)Math.Round(stats.MaxQi * (1f + qi)));
            stats.Attack = Math.Max(1, (int)Math.Round(stats.Attack * (1f + atk)));
            stats.Defense = Math.Max(0, (int)Math.Round(stats.Defense * (1f + def)));
            stats.Speed = Math.Max(1, stats.Speed + speed);
            stats.CritRate = Math.Max(0f, Math.Min(1f, stats.CritRate + crit));
            stats.CritDamage += critDmg;
            return stats;
        }

        /// <summary>Applies the in-fight part (내공 regen, 기세, life steal) to the protagonist's combatant.</summary>
        public static void ApplyPerks(Combatant hero, IEnumerable<string> equipped)
        {
            foreach (var id in equipped)
            {
                var a = Find(id);
                if (a == null) continue;
                hero.QiRegenBonus += a.QiRegen; hero.SpiritDealtBonus += a.SpiritDealt; hero.SpiritTakenBonus += a.SpiritTaken;
                hero.StartSpirit += a.StartSpirit; hero.Lifesteal += a.Lifesteal;
            }
        }

        /// <summary>The protagonist's stats with gear and 심법 applied (what battles and the 상태창 use).</summary>
        public static CombatStats HeroStats(SaveData save)
        {
            return Apply(Promotion.ApplyRankBonus(Inventory.ApplyGear(Progression.HeroStatsFor(save.Level), save.EquippedGear, save.GearLevels), Promotion.EffectiveRank(save)), save.EquippedInner);
        }

        /// <summary>"체력 +10%  방어 +4%  내공 회복 +2"</summary>
        public static string EffectText(InnerArt a)
        {
            var p = new List<string>();
            if (a.HpPct != 0f) p.Add("체력 " + Pct(a.HpPct));
            if (a.QiPct != 0f) p.Add("내공 " + Pct(a.QiPct));
            if (a.AtkPct != 0f) p.Add("공격 " + Pct(a.AtkPct));
            if (a.DefPct != 0f) p.Add("방어 " + Pct(a.DefPct));
            if (a.SpeedAdd != 0) p.Add("속도 " + (a.SpeedAdd > 0 ? "+" : "") + a.SpeedAdd);
            if (a.CritAdd != 0f) p.Add("치명타 " + Pct(a.CritAdd));
            if (a.CritDmgAdd != 0f) p.Add("치명타 피해 +" + Math.Round(a.CritDmgAdd * 100f) + "%p");
            if (a.QiRegen != 0) p.Add("내공 회복 +" + a.QiRegen);
            if (a.SpiritDealt != 0) p.Add("가한 기세 +" + a.SpiritDealt);
            if (a.SpiritTaken != 0) p.Add("받은 기세 +" + a.SpiritTaken);
            if (a.StartSpirit != 0) p.Add("시작 기세 " + a.StartSpirit);
            if (a.Lifesteal != 0f) p.Add("흡수 " + Math.Round(a.Lifesteal * 100f) + "%");
            return string.Join("  ", p);
        }

        private static string Pct(float v) { return (v > 0 ? "+" : "") + Math.Round(v * 100f) + "%"; }
    }
}
