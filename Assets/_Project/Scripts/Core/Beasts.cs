using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    /// <summary>Who a 영수 skill lands on.</summary>
    public enum BeastTarget { Enemy, FrontEnemy, AllEnemies, Ally, AllAllies }

    /// <summary>What a 영수 skill does (see <see cref="InteractiveBattle.CallBeast"/>).</summary>
    public enum BeastEffect { Strike, Scout, Poison, Paralyze, Roar, Bleed, Spirit, Heal, CleanseHeal, Shell, PartyShell, Dodge, PartyQi }

    public sealed class BeastSkill
    {
        public string Id, Name, Description;
        public BeastTarget Target;
        public BeastEffect Effect;
        public float Power;      // damage: x the protagonist's attack, like an art's power (scaled by 친밀도)
        public int Hits = 1;
        public float Value;      // heal share / 기세 / damage-taken factor / dodge chance / 내공 share, by effect
        public bool SureCrit;

        public bool NeedsEnemy { get { return Target == BeastTarget.Enemy || Target == BeastTarget.FrontEnemy; } }
        public bool NeedsAlly { get { return Target == BeastTarget.Ally; } }
    }

    /// <summary>A 영수 (spirit beast). It has no body on the board: 비류연 calls it on his turn as a free action (see <see cref="InteractiveBattle.CallBeast"/>).</summary>
    public sealed class BeastData
    {
        public string Id, Name, Kind, Description, Source;
        public int JoinStage;        // the 영수 comes once this 일반 stage is cleared
        public int Cooldown;         // 비류연's own turns before it can be called again
        public bool Original;        // a game original (not in the novel)
        public string ArtKey;        // battle figure / portrait key (Resources/Art/Beasts/<id>, else Battle/<ArtKey>)
        public BeastSkill[] Skills;  // [0] from the start, [1] from 친밀도 SecondSkillBond
    }

    /// <summary>What a fight gets from the chosen 영수: its data, 친밀도 and the skills that are open.</summary>
    public sealed class BeastLoadout
    {
        public BeastData Data;
        public int Bond;
        public int Cooldown;
        public float PowerFactor;
        public readonly List<BeastSkill> Skills = new List<BeastSkill>();
    }

    /// <summary>
    /// 영수 (Game design; 우뢰·청홍쌍각사·백무후 are named after the novel, their battle effects are game design; 묵묵이·약초 원숭이·청석 거북·백학 are game originals).
    /// One 영수 goes along to each fight (<see cref="SaveData.ActiveBeast"/>). 친밀도 grows by feeding 영수 먹이 (battle drops, 객잔).
    /// </summary>
    public static class Beasts
    {
        public const string FoodId = "beast_food";
        public const string FeastId = "beast_feast";
        public const int FoodPoints = 1, FeastPoints = 5;
        /// <summary>Fed points for 친밀도 1..5.</summary>
        public static readonly int[] BondThresholds = { 3, 8, 15, 25, 40 };
        public const int MaxBond = 5;
        public const int SecondSkillBond = 3;
        public const float PowerPerBond = 0.10f;
        /// <summary>Food from a won fight: chance of one (plus per difficulty step); a boss fight always adds one and may give a 보양식.</summary>
        public const float FoodChance = 0.35f, FoodChancePerDifficulty = 0.10f, FeastChanceBoss = 0.10f;

        private static BeastSkill Skill(string id, string name, BeastTarget target, BeastEffect effect, float power, int hits, float value, string desc, bool sureCrit = false)
        {
            return new BeastSkill { Id = id, Name = name, Target = target, Effect = effect, Power = power, Hits = hits, Value = value, Description = desc, SureCrit = sureCrit };
        }

        public static readonly BeastData[] All =
        {
            new BeastData
            {
                Id = "mukmuk", Name = "묵묵이", Kind = "검은 고양이", JoinStage = 4, Cooldown = 3, Original = true, ArtKey = "beast_mukmuk",
                Description = "객잔 지붕에서 내려와 비류연을 따라다니게 된 검은 고양이. 부르면 귀찮다는 듯 다가와 기운을 북돋운다.",
                Source = "게임 오리지널(원작에 없는 영수).",
                Skills = new[]
                {
                    Skill("mukmuk_charm", "애교", BeastTarget.Ally, BeastEffect.Spirit, 0f, 1, 20f, "고른 아군의 기세 +20."),
                    Skill("mukmuk_claw", "할퀴기", BeastTarget.FrontEnemy, BeastEffect.Bleed, 0.8f, 1, 0f, "맨 앞 줄의 적 하나를 할퀴어 피해를 주고 출혈을 겁니다."),
                }
            },
            new BeastData
            {
                Id = "urwe", Name = "우뢰", Kind = "해동청(전서응)", JoinStage = 9, Cooldown = 3, ArtKey = "beast_urwe",
                Description = "푸른 깃털의 고려산 해동청. 서른 명 넘게 실패한 매를 비류연이 길들여 흥정 끝에 사고 '우뢰'라 이름 붙였다.",
                Source = "3권: 천응방 분타에서 비류연이 산 전서응 우뢰. 전투 기술은 게임 각색.",
                Skills = new[]
                {
                    Skill("urwe_scout", "정찰", BeastTarget.Enemy, BeastEffect.Scout, 0.5f, 1, 0f, "적 하나를 가볍게 쪼고 그 적의 다음 행동과 걸린 상태 이상을 알려 줍니다. 뒷줄에도 닿습니다."),
                    Skill("urwe_dive", "급강하", BeastTarget.Enemy, BeastEffect.Strike, 1.0f, 2, 0f, "하늘에서 내리꽂아 적 하나를 두 번 쪼습니다. 반드시 치명타. 뒷줄에도 닿습니다.", true),
                }
            },
            new BeastData
            {
                Id = "monkey", Name = "약초 원숭이", Kind = "원숭이", JoinStage = 14, Cooldown = 4, Original = true, ArtKey = "beast_monkey",
                Description = "산길에서 약초 바구니를 뒤지다 붙잡힌 원숭이. 먹이를 주면 산에서 약초를 캐 온다.",
                Source = "게임 오리지널(원작에 없는 영수).",
                Skills = new[]
                {
                    Skill("monkey_herb", "채집", BeastTarget.Ally, BeastEffect.Heal, 0f, 1, 0.12f, "고른 아군의 체력을 12% 회복합니다."),
                    Skill("monkey_antidote", "해독초", BeastTarget.Ally, BeastEffect.CleanseHeal, 0f, 1, 0.08f, "고른 아군의 체력 8% 회복과 상태 이상을 모두 풉니다."),
                }
            },
            new BeastData
            {
                Id = "turtle", Name = "청석 거북", Kind = "거북", JoinStage = 18, Cooldown = 4, Original = true, ArtKey = "beast_turtle",
                Description = "푸른 돌 같은 등딱지를 진 늙은 거북. 느리지만 등 뒤에 숨은 자를 끝까지 지킨다.",
                Source = "게임 오리지널(원작에 없는 영수).",
                Skills = new[]
                {
                    Skill("turtle_shell", "등껍질", BeastTarget.Ally, BeastEffect.Shell, 0f, 1, 0.30f, "고른 아군이 다음에 받는 공격 한 번의 피해를 70% 줄입니다."),
                    Skill("turtle_rock", "바위 진", BeastTarget.AllAllies, BeastEffect.PartyShell, 0f, 1, 0.60f, "아군 모두가 다음에 받는 공격 한 번의 피해를 40% 줄입니다."),
                }
            },
            new BeastData
            {
                Id = "ssanggaksa", Name = "청홍쌍각사", Kind = "뱀 한 쌍", JoinStage = 23, Cooldown = 4, ArtKey = "cheonghong_ssanggaksa",
                Description = "푸른 비늘 수컷과 붉은 비늘 암컷, 작은 뿔이 난 뱀 한 쌍. 비류연에게 붙잡혀 협박과 내기 끝에 그를 따르기로 했다.",
                Source = "12권: 모사령의 영물 청홍쌍각사. 비류연이 맨손으로 잡아 제압하고, 내기 끝에 뱀들이 그를 택했다. 전투 기술은 게임 각색.",
                Skills = new[]
                {
                    Skill("ssang_fang", "독아", BeastTarget.Enemy, BeastEffect.Poison, 0.4f, 1, 0f, "적 하나를 물어 중독을 반드시 겁니다(이미 다른 상태 이상이 있으면 걸리지 않음). 뒷줄에도 닿습니다."),
                    Skill("ssang_glare", "요광", BeastTarget.Enemy, BeastEffect.Paralyze, 0.3f, 1, 0f, "눈빛으로 적 하나를 굳힙니다. 보스가 아니면 다음 차례를 잃고, 보스는 준비하던 강타만 끊깁니다. 뒷줄에도 닿습니다."),
                }
            },
            new BeastData
            {
                Id = "crane", Name = "백학", Kind = "두루미", JoinStage = 27, Cooldown = 5, Original = true, ArtKey = "beast_crane",
                Description = "안개 낀 호숫가에서 날아든 흰 학. 날갯짓 한 번에 일행의 몸이 가벼워진다.",
                Source = "게임 오리지널(원작에 없는 영수).",
                Skills = new[]
                {
                    Skill("crane_dance", "학의 춤", BeastTarget.AllAllies, BeastEffect.Dodge, 0f, 1, 0.25f, "아군 모두가 자기 다음 차례까지 25% 확률로 공격을 피합니다."),
                    Skill("crane_cry", "학의 울음", BeastTarget.AllAllies, BeastEffect.PartyQi, 0f, 1, 0.15f, "아군 모두 내공을 최대치의 15% 얻고 상태 이상 하나씩을 풉니다."),
                }
            },
            new BeastData
            {
                Id = "baekmuhu", Name = "백무후", Kind = "암백호", JoinStage = 31, Cooldown = 6, ArtKey = "white_tiger",
                Description = "노사부를 따르는 흰 암호랑이. 비류연의 짐승이 아니라, 노사부의 허락으로 잠시 힘을 빌려준다.",
                Source = "18·20·22권: 노사부와 함께 다니는 백무후. 비류연이 기르는 영수가 아니므로 '힘을 빌린다'로 연출. 전투 기술은 게임 각색.",
                Skills = new[]
                {
                    Skill("baek_roar", "포효", BeastTarget.AllEnemies, BeastEffect.Roar, 0.6f, 1, 40f, "적 모두에게 피해를 주고 적의 기세를 40 깎으며, 아군 모두의 기세를 15 올립니다."),
                    Skill("baek_claw", "호조 검기", BeastTarget.AllEnemies, BeastEffect.Strike, 1.6f, 1, 0f, "발톱에서 뻗은 검기로 모든 줄의 적을 벱니다."),
                }
            },
        };

        public const int RoarPartySpirit = 15;

        public static BeastData Find(string id)
        {
            foreach (var b in All) if (b.Id == id) return b;
            return null;
        }

        public static BeastSkill FindSkill(string skillId)
        {
            foreach (var b in All) foreach (var sk in b.Skills) if (sk.Id == skillId) return sk;
            return null;
        }

        /// <summary>The 영수 of a battle-event actor id ("beast_&lt;id&gt;"), or null.</summary>
        public static BeastData OfUnit(string unitId)
        {
            return unitId != null && unitId.StartsWith(UnitPrefix, StringComparison.Ordinal) ? Find(unitId.Substring(UnitPrefix.Length)) : null;
        }

        public const string UnitPrefix = "beast_";

        public static bool Owns(SaveData save, string id) { return save != null && save.Beasts.Contains(id); }

        /// <summary>Gives every 영수 whose stage is cleared; returns the ones that came now (for the 「새 영수!」 notice). The first one is taken along at once.</summary>
        public static List<BeastData> Sync(SaveData save)
        {
            var added = new List<BeastData>();
            foreach (var b in All)
            {
                if (save.ClearedStage < b.JoinStage || save.Beasts.Contains(b.Id)) continue;
                save.Beasts.Add(b.Id);
                added.Add(b);
            }
            if (string.IsNullOrEmpty(save.ActiveBeast) && save.Beasts.Count > 0) save.ActiveBeast = save.Beasts[0];
            return added;
        }

        public static int Fed(SaveData save, string id)
        {
            string prefix = id + ":";
            foreach (var entry in save.BeastFed)
                if (entry.StartsWith(prefix, StringComparison.Ordinal))
                {
                    int n;
                    if (int.TryParse(entry.Substring(prefix.Length), out n)) return n;
                }
            return 0;
        }

        private static void SetFed(SaveData save, string id, int value)
        {
            string prefix = id + ":";
            save.BeastFed.RemoveAll(e => e.StartsWith(prefix, StringComparison.Ordinal));
            save.BeastFed.Add(prefix + value);
        }

        public static int BondOf(int fed)
        {
            int level = 0;
            while (level < MaxBond && fed >= BondThresholds[level]) level++;
            return level;
        }

        public static int Bond(SaveData save, string id) { return BondOf(Fed(save, id)); }

        /// <summary>Points still needed for the next 친밀도 (0 at the top).</summary>
        public static int ToNext(SaveData save, string id)
        {
            int fed = Fed(save, id), bond = BondOf(fed);
            return bond >= MaxBond ? 0 : BondThresholds[bond] - fed;
        }

        public static float PowerFactor(int bond) { return 1f + PowerPerBond * bond; }
        public static int CooldownFor(BeastData data, int bond) { return Math.Max(1, data.Cooldown - (bond >= MaxBond ? 1 : 0)); }
        public static int PointsOf(string itemId) { return itemId == FeastId ? FeastPoints : itemId == FoodId ? FoodPoints : 0; }

        /// <summary>Feeds <paramref name="count"/> of a food to an owned 영수 (never past the top 친밀도). Returns how many were used.</summary>
        public static int Feed(SaveData save, string id, string foodId, int count)
        {
            int points = PointsOf(foodId);
            if (points <= 0 || !Owns(save, id)) return 0;
            int used = 0;
            while (used < count && Inventory.Count(save, foodId) > 0 && Bond(save, id) < MaxBond)
            {
                Inventory.Remove(save, foodId, 1);
                SetFed(save, id, Math.Min(BondThresholds[MaxBond - 1], Fed(save, id) + points));
                used++;
            }
            return used;
        }

        /// <summary>The 영수 a fight takes along (null when none is owned or chosen).</summary>
        public static BeastLoadout LoadoutFor(SaveData save)
        {
            if (save == null || string.IsNullOrEmpty(save.ActiveBeast) || !Owns(save, save.ActiveBeast)) return null;
            return Loadout(Find(save.ActiveBeast), Bond(save, save.ActiveBeast));
        }

        public static BeastLoadout Loadout(BeastData data, int bond)
        {
            if (data == null) return null;
            var l = new BeastLoadout { Data = data, Bond = bond, Cooldown = CooldownFor(data, bond), PowerFactor = PowerFactor(bond) };
            l.Skills.Add(data.Skills[0]);
            if (bond >= SecondSkillBond && data.Skills.Length > 1) l.Skills.Add(data.Skills[1]);
            return l;
        }

        /// <summary>영수 먹이 from a won fight (added to the bag and returned).</summary>
        public static List<ItemStack> RollFood(SaveData save, IRandom rng, bool bossFight, int difficulty)
        {
            var got = new List<ItemStack>();
            int food = rng.NextDouble() < FoodChance + FoodChancePerDifficulty * difficulty ? 1 : 0;
            if (bossFight) food++;
            if (food > 0) { Inventory.Add(save, FoodId, food); got.Add(new ItemStack(FoodId, food)); }
            if (bossFight && rng.NextDouble() < FeastChanceBoss) { Inventory.Add(save, FeastId, 1); got.Add(new ItemStack(FeastId, 1)); }
            return got;
        }

        public static string TargetText(BeastSkill s)
        {
            switch (s.Target)
            {
                case BeastTarget.Enemy: return "적 하나 · 모든 줄";
                case BeastTarget.FrontEnemy: return "적 하나 · 맨 앞 줄";
                case BeastTarget.AllEnemies: return "적 모두";
                case BeastTarget.Ally: return "아군 하나";
                default: return "아군 모두";
            }
        }

        /// <summary>One-line effect of a skill at a 친밀도 (damage and heals scale with it).</summary>
        public static string EffectText(BeastSkill s, int bond)
        {
            float f = PowerFactor(bond);
            string power = s.Power > 0f ? "위력 " + (s.Power * f).ToString("0.##") + (s.Hits > 1 ? " x" + s.Hits : "") : "";
            switch (s.Effect)
            {
                case BeastEffect.Heal: return "체력 " + Math.Round(s.Value * f * 100f) + "% 회복";
                case BeastEffect.CleanseHeal: return "체력 " + Math.Round(s.Value * f * 100f) + "% 회복 · 상태 이상 해제";
                case BeastEffect.Spirit: return "기세 +" + Math.Round(s.Value * f);
                case BeastEffect.Shell: case BeastEffect.PartyShell: return "다음 피해 -" + Math.Round((1f - s.Value) * 100f) + "%";
                case BeastEffect.Dodge: return "회피 " + Math.Round(s.Value * 100f) + "%";
                case BeastEffect.PartyQi: return "내공 +" + Math.Round(s.Value * f * 100f) + "% · 상태 이상 하나 해제";
                case BeastEffect.Scout: return power + " · 다음 행동 정찰";
                case BeastEffect.Poison: return power + " · 중독 확정";
                case BeastEffect.Paralyze: return power + " · 마비(보스는 강타 끊기)";
                case BeastEffect.Bleed: return power + " · 출혈 확정";
                case BeastEffect.Roar: return power + " · 적 기세 -" + s.Value + " · 아군 기세 +" + RoarPartySpirit;
                default: return power + (s.SureCrit ? " · 치명타 확정" : "");
            }
        }
    }
}
