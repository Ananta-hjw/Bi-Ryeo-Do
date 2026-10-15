using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StatusSheetTests
    {
        [Test]
        public void HeroLinesMatchWhatBattleUses()
        {
            var save = new SaveData { Level = 10 };
            var lines = StatusSheet.Hero(save);
            var b = Promotion.ApplyRankBonus(Progression.HeroStatsFor(10), 1);   // 일검룡 already gives +3%
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

        [Test]
        public void ArtLinesShowMasteryAndVariant()
        {
            var save = new SaveData();
            save.EquippedArts.Add("bunroesu");
            for (int i = 0; i < 30; i++) Mastery.RecordUse(save, "bunroesu");
            Mastery.Find(save, "bunroesu").Variant = (int)ArtVariant.Power;
            var line = StatusSheet.ArtLines(save).Single();
            StringAssert.Contains("★★★☆", line);
            StringAssert.Contains("대성", line);
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
