using System;
using System.Collections.Generic;

namespace BiRyeoDo.Core
{
    [Serializable]
    public sealed class ReputationEntry
    {
        public string Id;
        public int Points;
    }

    [Serializable]
    public sealed class ShopPurchase
    {
        public string Id;
        public int Count;
    }

    public enum PerkKind { ExpPct, DooPct, DropPct, RestHeal, JointPower }

    public sealed class FactionData
    {
        public string Id, Name, Description;
        public int[] Stages;          // stages whose fights earn reputation with this faction (the fight's setting, a game-layer mapping)
        public PerkKind Perk;
        public float[] PerkSteps;     // bonus at 우호 / 신뢰 / 존경
        public string PerkText;       // what the perk does, e.g. "경험치"
    }

    /// <summary>
    /// 세력 평판: the first win of every fight in a faction's stages (+1 on 일반, +2 어려움, +3 극악) and finishing its 의뢰 build reputation.
    /// Tiers (무명 / 안면 / 우호 / 신뢰 / 존경) open the faction's shelf in the 객잔 and give small permanent perks.
    /// Faction names come from the novel; which stages feed which faction is game design.
    /// </summary>
    public static class Factions
    {
        public static readonly int[] Thresholds = { 0, 12, 40, 90, 160 };
        public static readonly string[] TierNames = { "무명", "안면", "우호", "신뢰", "존경" };
        public const int MaxTier = 4;

        public static readonly FactionData[] All =
        {
            new FactionData { Id = "academy", Name = "천무학관", Description = "무림맹 산하의 학관. 수련과 시험이 이어진다.", PerkText = "경험치",
                Stages = new[] { 2, 6, 7, 8, 9, 10, 11, 12, 13, 16, 17, 19, 20 }, Perk = PerkKind.ExpPct, PerkSteps = new[] { 0.03f, 0.06f, 0.10f } },
            new FactionData { Id = "biaoguk", Name = "중양표국", Description = "표행을 맡는 표국. 길과 물자에 밝다.", PerkText = "얻는 두",
                Stages = new[] { 2, 3, 5, 22, 25 }, Perk = PerkKind.DooPct, PerkSteps = new[] { 0.03f, 0.06f, 0.10f } },
            new FactionData { Id = "wudang", Name = "무당파", Description = "태극의 문파. 호흡과 회복에 능하다.", PerkText = "전투 사이 정비 회복량",
                Stages = new[] { 14, 15, 18 }, Perk = PerkKind.RestHeal, PerkSteps = new[] { 0.05f, 0.10f, 0.15f } },
            new FactionData { Id = "greenwood", Name = "녹림", Description = "산채의 사람들. 산길과 노획품에 밝다.", PerkText = "아이템 드롭 확률",
                Stages = new[] { 21, 22, 28 }, Perk = PerkKind.DropPct, PerkSteps = new[] { 0.10f, 0.20f, 0.35f } },
            new FactionData { Id = "huashan", Name = "화산파", Description = "매화의 문파. 합을 맞추는 검에 능하다.", PerkText = "합공 위력",
                Stages = new[] { 8, 11, 23, 24, 25, 26, 27, 28, 29, 30 }, Perk = PerkKind.JointPower, PerkSteps = new[] { 0.05f, 0.10f, 0.15f } },
        };

        public static FactionData Find(string id)
        {
            foreach (var f in All) if (f.Id == id) return f;
            return null;
        }

        public static int Points(SaveData save, string id)
        {
            foreach (var e in save.Reputation) if (e.Id == id) return e.Points;
            return 0;
        }

        public static int TierOf(int points)
        {
            int tier = 0;
            for (int i = 0; i < Thresholds.Length; i++) if (points >= Thresholds[i]) tier = i;
            return tier;
        }

        public static int Tier(SaveData save, string id) { return TierOf(Points(save, id)); }

        /// <summary>Points still needed for the next tier, or -1 at 존경.</summary>
        public static int ToNext(SaveData save, string id)
        {
            int tier = Tier(save, id);
            return tier >= MaxTier ? -1 : Thresholds[tier + 1] - Points(save, id);
        }

        /// <summary>Adds reputation; returns the new tier when it just rose, otherwise -1.</summary>
        public static int Gain(SaveData save, string id, int points)
        {
            if (points <= 0 || Find(id) == null) return -1;
            ReputationEntry e = null;
            foreach (var r in save.Reputation) if (r.Id == id) e = r;
            if (e == null) { e = new ReputationEntry { Id = id }; save.Reputation.Add(e); }
            int before = TierOf(e.Points);
            e.Points += points;
            int after = TierOf(e.Points);
            return after > before ? after : -1;
        }

        /// <summary>
        /// Reputation is earned once per (stage, fight, difficulty), exactly when the fight is first won, so it can be rebuilt from the
        /// star records. Older saves (cleared stages before reputation existed) are lifted to that value; quest bonuses stay on top.
        /// </summary>
        public static void SyncFromStars(SaveData save)
        {
            var computed = new Dictionary<string, int>();
            foreach (var e in save.StageStars)
            {
                if ((e.Mask & 1) == 0) continue;
                foreach (var f in ForStage(e.Stage))
                {
                    int have;
                    computed.TryGetValue(f.Id, out have);
                    computed[f.Id] = have + 1 + Math.Max(0, e.Diff);
                }
            }
            foreach (var kv in computed)
            {
                int diff = kv.Value - Points(save, kv.Key);
                if (diff > 0) Gain(save, kv.Key, diff);
            }
        }

        public static List<FactionData> ForStage(int stage)
        {
            var list = new List<FactionData>();
            foreach (var f in All) if (Array.IndexOf(f.Stages, stage) >= 0) list.Add(f);
            return list;
        }

        /// <summary>A won fight of a stage: every faction tied to it gains (difficulty + 1). Returns "천무학관 우호 달성!" style notes for tier-ups.</summary>
        public static List<string> OnFightWon(SaveData save, int stage, int difficulty)
        {
            var notes = new List<string>();
            foreach (var f in ForStage(stage))
            {
                int rose = Gain(save, f.Id, 1 + Math.Max(0, difficulty));
                if (rose >= 0) notes.Add(f.Name + " 평판 " + TierNames[rose] + "!");
            }
            return notes;
        }

        /// <summary>"천무학관 +1, 중양표국 +1" for the stage (what the next win earns).</summary>
        public static string GainText(int stage, int difficulty)
        {
            var parts = new List<string>();
            foreach (var f in ForStage(stage)) parts.Add(f.Name + " +" + (1 + Math.Max(0, difficulty)));
            return string.Join(", ", parts);
        }

        /// <summary>The perk bonus of one faction at its current tier (0 below 우호).</summary>
        public static float PerkValue(SaveData save, FactionData f)
        {
            int tier = Tier(save, f.Id);
            return tier >= 2 ? f.PerkSteps[tier - 2] : 0f;
        }

        public static float Bonus(SaveData save, PerkKind kind)
        {
            float total = 0f;
            foreach (var f in All) if (f.Perk == kind) total += PerkValue(save, f);
            return total;
        }

        public static string PerkLine(FactionData f)
        {
            switch (f.Perk)
            {
                case PerkKind.RestHeal: return "우호 +5%p / 신뢰 +10%p / 존경 +15%p";
                default:
                    return "우호 +" + Math.Round(f.PerkSteps[0] * 100f) + "% / 신뢰 +" + Math.Round(f.PerkSteps[1] * 100f) + "% / 존경 +" + Math.Round(f.PerkSteps[2] * 100f) + "%";
            }
        }

        public static int RespectedCount(SaveData save)
        {
            int n = 0;
            foreach (var f in All) if (Tier(save, f.Id) >= MaxTier) n++;
            return n;
        }
    }

    public enum ShopKind { Item, Training, ExpScroll }

    public sealed class ShopGood
    {
        public string Id;            // unique key
        public string FactionId;     // null = the 객잔 itself (always open)
        public int MinTier;
        public ShopKind Kind;
        public string ItemId;        // Item
        public int Amount;           // Training: uses for every equipped art; ExpScroll: percent of the current level-up cost
        public string Name, Description;
        public long Price;
        public int Limit;            // 0 = unlimited
    }

    /// <summary>객잔: buy goods with 두. Each faction's shelf opens with its reputation tier.</summary>
    public static class Shop
    {
        private static ShopGood Item(string id, string faction, int tier, string itemId, long price, int limit = 0)
        {
            return new ShopGood { Id = id, FactionId = faction, MinTier = tier, Kind = ShopKind.Item, ItemId = itemId, Price = price, Limit = limit };
        }

        public static readonly ShopGood[] Goods =
        {
            // 객잔 itself
            Item("inn_gall", null, 0, "bear_gall", 600),
            Item("inn_okruju", null, 0, "okruju", 800),
            Item("inn_antidote", null, 0, "antidote", 700),
            // 천무학관
            Item("aca_meal", "academy", 1, "meal_academy", 500),
            Item("aca_snow", "academy", 1, "snow_ginseng", 1800),
            Item("aca_art5", "academy", 2, "art_scroll_5", 4500),
            Item("aca_art10", "academy", 2, "art_scroll_10", 8200),
            Item("aca_art20", "academy", 2, "art_scroll_20", 13000),
            new ShopGood { Id = "aca_scroll", FactionId = "academy", MinTier = 2, Kind = ShopKind.ExpScroll, Amount = 20, Name = "수련 일지", Description = "한 단계 오르는 데 필요한 경험치의 20%를 얻습니다.", Price = 7500, Limit = 5 },
            Item("aca_ginseng", "academy", 3, "ginseng", 5200),
            new ShopGood { Id = "aca_scroll2", FactionId = "academy", MinTier = 4, Kind = ShopKind.ExpScroll, Amount = 50, Name = "비전 수련 일지", Description = "한 단계 오르는 데 필요한 경험치의 50%를 얻습니다.", Price = 19500, Limit = 3 },
            // 중양표국
            Item("bia_ration", "biaoguk", 1, "biaoguk_ration", 400),
            Item("bia_ginseng", "biaoguk", 2, "ginseng", 4800),
            Item("bia_yeongdan", "biaoguk", 3, "ilgeup_yeongdan", 18200, 3),
            Item("bia_yeongdan2", "biaoguk", 4, "ilgeup_yeongdan", 14300, 5),
            // 무당파
            new ShopGood { Id = "wud_train1", FactionId = "wudang", MinTier = 2, Kind = ShopKind.Training, Amount = 10, Name = "태극 연공", Description = "장착한 무공 모두 숙련 +10회.", Price = 4500, Limit = 10 },
            Item("wud_pill", "wudang", 3, "taegeuk_pill", 2200),
            new ShopGood { Id = "wud_train2", FactionId = "wudang", MinTier = 3, Kind = ShopKind.Training, Amount = 25, Name = "태극 심연공", Description = "장착한 무공 모두 숙련 +25회.", Price = 10500, Limit = 10 },
            new ShopGood { Id = "wud_train3", FactionId = "wudang", MinTier = 4, Kind = ShopKind.Training, Amount = 60, Name = "태극 대연공", Description = "장착한 무공 모두 숙련 +60회.", Price = 19500, Limit = 5 },
            // 녹림
            Item("gre_liquor", "greenwood", 1, "greenwood_liquor", 1000),
            Item("gre_gall", "greenwood", 2, "bear_gall", 500),
            Item("gre_snow", "greenwood", 3, "snow_ginseng", 1400),
            // 화산파
            Item("hua_elixir", "huashan", 2, "plum_elixir", 1800),
            Item("hua_snow", "huashan", 3, "snow_ginseng", 1500),
            Item("hua_ginseng", "huashan", 4, "ginseng", 4500),
        };

        public static ShopGood Find(string id)
        {
            foreach (var g in Goods) if (g.Id == id) return g;
            return null;
        }

        public static string NameOf(ShopGood g)
        {
            if (g.Kind != ShopKind.Item) return g.Name;
            var item = EarlyGameContent.FindItem(g.ItemId);
            return item != null ? item.Name : g.ItemId;
        }

        public static string DescriptionOf(ShopGood g)
        {
            if (g.Kind != ShopKind.Item) return g.Description;
            var item = EarlyGameContent.FindItem(g.ItemId);
            return item != null ? item.Description : "";
        }

        public static int Bought(SaveData save, string id)
        {
            foreach (var p in save.ShopBought) if (p.Id == id) return p.Count;
            return 0;
        }

        public static bool IsOpen(SaveData save, ShopGood g) { return g.FactionId == null || Factions.Tier(save, g.FactionId) >= g.MinTier; }

        public static bool SoldOut(SaveData save, ShopGood g) { return g.Limit > 0 && Bought(save, g.Id) >= g.Limit; }

        /// <summary>Buys one; returns a one-line result, or null with the reason in <paramref name="error"/>.</summary>
        public static string Buy(SaveData save, string id, out string error)
        {
            error = null;
            var g = Find(id);
            if (g == null) { error = "없는 물건입니다."; return null; }
            if (!IsOpen(save, g)) { error = Factions.Find(g.FactionId).Name + " 평판 " + Factions.TierNames[g.MinTier] + " 이상이 필요합니다."; return null; }
            if (SoldOut(save, g)) { error = "더 이상 살 수 없습니다."; return null; }
            if (save.Doo < g.Price) { error = "두가 모자랍니다. (" + g.Price.ToString("N0") + "두)"; return null; }
            string note;
            switch (g.Kind)
            {
                case ShopKind.Item:
                    if (EarlyGameContent.FindItem(g.ItemId) == null) { error = "물건이 없습니다."; return null; }
                    Inventory.Add(save, g.ItemId, 1);
                    note = NameOf(g) + " 구매";
                    break;
                case ShopKind.Training:
                    if (Progression.EquippedCount(save, false) == 0 && Progression.EquippedCount(save, true) == 0) { error = "장착한 무공이 있어야 수련할 수 있습니다."; return null; }
                    Mastery.Train(save, g.Amount);
                    note = NameOf(g) + ": 장착 무공 숙련 +" + g.Amount + "회";
                    break;
                default:
                    {
                        long exp = (long)Math.Round(Progression.ExpToNext(save.Level) * g.Amount / 100.0);
                        int levels = Progression.AddExp(save, exp);
                        note = NameOf(g) + ": 경험 +" + exp + (levels > 0 ? " (Lv." + save.Level + " 상승!)" : "");
                        break;
                    }
            }
            save.Doo -= g.Price;
            if (g.Limit > 0)
            {
                ShopPurchase p = null;
                foreach (var x in save.ShopBought) if (x.Id == g.Id) p = x;
                if (p == null) { p = new ShopPurchase { Id = g.Id }; save.ShopBought.Add(p); }
                p.Count++;
            }
            return note;
        }
    }
}
