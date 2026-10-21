using System.Linq;
using System.Text.RegularExpressions;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StatusSheetTests
    {
        [Test]
        public void HeroLinesMatchWhatBattleUses()
        {
            var save = new SaveData { Level = 2 };   // low enough to stand on the first realm step (no realm bonus yet)
            var lines = StatusSheet.Hero(save);
            var b = Promotion.ApplyRankBonus(Progression.HeroStatsFor(2), 1);   // 일검룡 already gives +3%
            Assert.AreEqual(b.MaxHp.ToString("N0"), lines.First(l => l.Label == "체력").Value);
            Assert.AreEqual(b.Attack.ToString("N0"), lines.First(l => l.Label == "공격력").Value);
            Assert.AreEqual(InnerArts.HeroStats(save).Attack, b.Attack);
        }

        [Test]
        public void GearBonusShowsUpInTheLines()
        {
            var save = new SaveData { Level = 10 };
            string weapon = EarlyGameContent.Items.First(i => i.Kind == ItemKind.Weapon && i.AtkPct > 0f).Id;
            Inventory.Add(save, weapon, 1);
            string error;
            Assert.IsTrue(Inventory.Equip(save, weapon, out error));
            var atk = StatusSheet.Hero(save).First(l => l.Label == "공격력");
            Assert.IsTrue(atk.Bonus.StartsWith("+"), atk.Bonus);
            Assert.IsNotNull(StatusSheet.Weapon(save));
            Assert.AreEqual(0, StatusSheet.Accessories(save).Count);
        }

        [Test]
        public void EffectTextListsEveryNonZeroField()
        {
            var item = new ItemData { AtkPct = 0.1f, SpeedAdd = -6, CritAdd = 0.05f };
            var text = StatusSheet.EffectText(item);
            StringAssert.Contains("공격 +10%", text);
            StringAssert.Contains("속도 -6", text);
            StringAssert.Contains("치명타 +5%", text);
            Assert.AreEqual("특별한 효과 없음", StatusSheet.EffectText(new ItemData()));
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(3, 2)]
        [TestCase(5, 3)]
        public void GearColorsOnlyUnlockedBreakthroughOptionsAndPreservesTheShownValues(int stars, int coloredCount)
        {
            var item = EarlyGameContent.FindItem("biroe_blade");
            string plain = StatusSheet.EffectText(item, 100, stars);
            string colored = StatusSheet.EffectText(item, 100, stars, "#8ECFFF");
            Assert.AreEqual(coloredCount, Regex.Matches(colored, "<color=").Count);
            Assert.AreEqual(plain, Regex.Replace(colored, "</?color[^>]*>", ""));
            StringAssert.DoesNotContain("<color=#8ECFFF>공격", colored);
            StringAssert.DoesNotContain("<color=#8ECFFF>체력", colored);
            StringAssert.DoesNotContain("<color=#8ECFFF>치명타 +", colored);
            if (stars >= 1) StringAssert.Contains("<color=#8ECFFF>치명타 피해 +32%</color>", colored);
            if (stars >= 3) StringAssert.Contains("<color=#8ECFFF>내공 +36%</color>", colored);
            if (stars >= 5) StringAssert.Contains("<color=#8ECFFF>방어 +69%</color>", colored);
        }

        [TestCase("iron_sword")]
        [TestCase("jin_biroedo")]
        public void BaseEffectsWithoutBreakthroughTraitsAreNeverRecolored(string id)
        {
            var item = EarlyGameContent.FindItem(id);
            Assert.AreEqual(StatusSheet.EffectText(item, 100, 5), StatusSheet.EffectText(item, 100, 5, "#8ECFFF"));
        }

        [Test]
        public void BreakthroughDetailsDistinguishUnlockedAndLockedOptionsIncludingFlatSpirit()
        {
            var item = EarlyGameContent.FindItem("roegeum");
            string text = Gear.TraitsText(item, 3, "#8ECFFF", "#9CA6A0");
            StringAssert.Contains("<color=#8ECFFF>1성 [적용] 치명타 피해 +32%p</color>", text);
            StringAssert.Contains("<color=#8ECFFF>3성 [적용] 방어 +36%</color>", text);
            StringAssert.Contains("<color=#9CA6A0>5성 [잠김] 시작 기세 +55</color>", text);
            StringAssert.Contains("<color=#8ECFFF>시작 기세 +55</color>", StatusSheet.EffectText(item, 100, 5, "#8ECFFF"));
            Assert.AreEqual(Gear.TraitsText(item, 3), Regex.Replace(text, "</?color[^>]*>", ""));
        }

        [Test]
        public void ArtLinesShowMasteryAndVariant()
        {
            var save = new SaveData();
            save.EquippedArts.Add("bunroesu");
            for (int i = 0; i < 50; i++) Mastery.RecordUse(save, "bunroesu");
            Mastery.Find(save, "bunroesu").Variant = (int)ArtVariant.Power;
            var line = StatusSheet.ArtLines(save).Single();
            StringAssert.Contains("★★★★☆", line);
            StringAssert.Contains("극성", line);
            StringAssert.Contains("(강)", line);
        }

        [Test]
        public void AllyLinesFollowTheLevel()
        {
            var c = EarlyGameContent.FindCompanion("hyo_ryong");
            var low = StatusSheet.Ally(c, 10).First(l => l.Label == "체력").Value;
            var high = StatusSheet.Ally(c, 40).First(l => l.Label == "체력").Value;
            Assert.AreNotEqual(low, high);
        }

        [Test]
        public void GearFlavorDropsTheTrailingEffectSentence()
        {
            Assert.AreEqual("수련용 강철 방망이(50근). 무기라기엔 빨래용이다.", StatusSheet.Flavor(EarlyGameContent.FindItem("laundry_bat")));
            Assert.AreEqual("", StatusSheet.Flavor(new ItemData { Kind = ItemKind.Weapon, Description = "공격 +4%." }));
            Assert.AreEqual("튼튼한 갑옷.", StatusSheet.Flavor(new ItemData { Kind = ItemKind.Armor, Description = "튼튼한 갑옷. 방어 +5%." }));
            // Non-gear descriptions are untouched (they describe the effect in words).
            var potion = EarlyGameContent.FindItem("bear_gall");
            Assert.AreEqual(potion.Description, StatusSheet.Flavor(potion));
            foreach (var it in EarlyGameContent.Items.Where(x => x.IsGear))
                StringAssert.DoesNotContain("%", StatusSheet.Flavor(it), it.Id);
        }
    }
}
