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
        public int UnlockStage;
        public long LearnCost;
        public float HpPct, QiPct, AtkPct, DefPct, CritAdd, CritDmgAdd, Lifesteal;
        public int SpeedAdd, QiRegen, SpiritDealt, SpiritTaken, StartSpirit;
    }

    public static class InnerArts
    {

        public static readonly InnerArt[] All =
        {
            new InnerArt { Id = "breath", Name = "기초 호흡법", UnlockStage = 1, LearnCost = 0, Description = "기본 호흡. 오래 싸워도 숨이 차지 않는다.", QiPct = 0.10f, QiRegen = 2 },
            new InnerArt { Id = "vigor", Name = "강건심법", UnlockStage = 3, LearnCost = 2000, Description = "몸을 단단히 다지는 수련.", HpPct = 0.10f, DefPct = 0.04f },
            new InnerArt { Id = "tiger", Name = "맹호심법", UnlockStage = 5, LearnCost = 4000, Description = "공격에 모든 기운을 싣는다. 방어는 허술해진다.", AtkPct = 0.10f, DefPct = -0.04f },
            new InnerArt { Id = "swift", Name = "경공심법", UnlockStage = 7, LearnCost = 6000, Description = "가볍게 움직여 먼저 치고 급소를 노린다.", SpeedAdd = 4, CritAdd = 0.03f },
            new InnerArt { Id = "life", Name = "생기심법", UnlockStage = 9, LearnCost = 7000, Description = "때릴 때마다 상대의 기운을 조금 빼앗아 회복한다.", Lifesteal = 0.08f, HpPct = 0.04f },
            new InnerArt { Id = "spirit", Name = "투지심법", UnlockStage = 11, LearnCost = 3500, Description = "기세가 빨리 오른다. 싸움을 기세 20으로 시작한다.", SpiritDealt = 4, SpiritTaken = 3, StartSpirit = 20 },
            new InnerArt { Id = "calm", Name = "명경지수", UnlockStage = 13, LearnCost = 4000, Description = "마음이 고요해 내공이 넘친다. 공격은 다소 무뎌진다.", QiPct = 0.25f, QiRegen = 4, AtkPct = -0.05f },
            new InnerArt { Id = "fury", Name = "광폭심법", UnlockStage = 15, LearnCost = 8000, Description = "급소를 노린 일격에 모든 것을 건다. 몸은 상한다.", CritDmgAdd = 0.4f, CritAdd = 0.04f, HpPct = -0.08f },
            new InnerArt { Id = "wall", Name = "철벽심법", UnlockStage = 17, LearnCost = 5000, Description = "어떤 공격도 버텨 내는 수비의 심법. 몸놀림은 느려진다.", DefPct = 0.12f, SpeedAdd = -2 },
            new InnerArt { Id = "blood", Name = "혈투심법", UnlockStage = 19, LearnCost = 12000, Description = "피를 흘리며 싸울수록 강해지는 거친 심법. 방어를 버린다.", AtkPct = 0.12f, Lifesteal = 0.05f, DefPct = -0.08f },
            new InnerArt { Id = "unity", Name = "천지합일", UnlockStage = 22, LearnCost = 20000, Description = "몸과 기운이 하나가 되어 모든 면이 고르게 오른다.", HpPct = 0.06f, QiPct = 0.06f, AtkPct = 0.06f, DefPct = 0.06f, QiRegen = 2 },
            new InnerArt { Id = "thunder", Name = "뇌정심법", UnlockStage = 25, LearnCost = 14000, Description = "번개 같은 선공. 몸은 약해지지만 기세를 품고 시작한다.", SpeedAdd = 6, CritAdd = 0.06f, StartSpirit = 30, HpPct = -0.06f },
        };

        public static InnerArt Find(string id)
        {
            foreach (var a in All) if (a.Id == id) return a;
            return null;
        }

        /// <summary>Slots: one from the start, a second at stage 10, a third at stage 20.</summary>
        public static int SlotCount(SaveData save) { return 1 + (save.ClearedStage >= 10 ? 1 : 0) + (save.ClearedStage >= 20 ? 1 : 0); }

        public static bool IsUnlocked(SaveData save, InnerArt a) { return save.HighestUnlockedStage >= a.UnlockStage; }
        public static bool IsLearned(SaveData save, string id) { return save.LearnedInner.Contains(id); }
        public static bool IsEquipped(SaveData save, string id) { return save.EquippedInner.Contains(id); }

        public static bool Learn(SaveData save, string id, out string error)
        {
            error = null;
            var a = Find(id);
            if (a == null) { error = "알 수 없는 심법이다."; return false; }
            if (IsLearned(save, id)) { error = "이미 익혔다."; return false; }
            if (!IsUnlocked(save, a)) { error = "스테이지 " + a.UnlockStage + "에 도달해야 한다."; return false; }
            if (save.Doo < a.LearnCost) { error = "두가 모자란다. (" + a.LearnCost.ToString("N0") + "두 필요)"; return false; }
            save.Doo -= a.LearnCost;
            save.LearnedInner.Add(id);
            return true;
        }

        public static bool Equip(SaveData save, string id, out string error)
        {
            error = null;
            if (!IsLearned(save, id)) { error = "먼저 익혀야 한다."; return false; }
            if (IsEquipped(save, id)) return true;
            if (save.EquippedInner.Count >= SlotCount(save)) { error = "심법 칸이 가득 찼다. (" + SlotCount(save) + "칸) 하나를 먼저 해제하자."; return false; }
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
            return Apply(Inventory.ApplyGear(Progression.HeroStatsFor(save.Level), save.EquippedGear, save.GearLevels), save.EquippedInner);
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
