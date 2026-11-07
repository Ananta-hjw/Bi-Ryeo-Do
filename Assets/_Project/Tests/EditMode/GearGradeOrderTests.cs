using System;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    /// <summary>Docs/GEAR_EFFECT_REBALANCE.md: in every slot the weakest piece of a grade beats the strongest piece of the grade below.</summary>
    public class GearGradeOrderTests
    {
        [Test]
        public void HigherGradesAreAlwaysBetterInTheSameSlot()
        {
            var bare = Progression.HeroStatsFor(30);
            double p0 = Realm.Power(bare);
            Func<ItemData, double> value = i => Realm.Power(Inventory.ApplyGear(bare, new[] { i.Id })) / p0;
            var gear = EarlyGameContent.Items.Where(i => i.IsGear && i.Grade != ItemGrade.Transcendent).ToList();
            var grades = Enum.GetValues(typeof(ItemGrade)).Cast<ItemGrade>().Where(g => g != ItemGrade.Transcendent).ToArray();
            foreach (var kind in new[] { ItemKind.Weapon, ItemKind.Armor, ItemKind.Pants, ItemKind.Boots, ItemKind.Accessory })
                for (int g = 1; g < grades.Length; g++)
                {
                    var lower = gear.Where(i => i.Kind == kind && i.Grade == grades[g - 1]).ToList();
                    var higher = gear.Where(i => i.Kind == kind && i.Grade == grades[g]).ToList();
                    var strongest = lower.OrderByDescending(value).First();
                    var weakest = higher.OrderBy(value).First();
                    Assert.Greater(value(weakest), value(strongest), kind + ": " + weakest.Name + " (" + grades[g] + ") vs " + strongest.Name + " (" + grades[g - 1] + ")");
                }
        }
    }
}
