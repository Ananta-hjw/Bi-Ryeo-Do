using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

public class RitualTests
{
    private static void Clear(SaveData save, int stage, int diff)
    {
        for (int f = 0; f < StageStars.FightCount(stage); f++) StageStars.Record(save, stage, f, diff, 1);
    }

    private static SaveData Ready()
    {
        var save = new SaveData { SwordRank = 10, TowerBest = 50, ClearedStage = 30 };
        save.LearnedArts.Add("pungsin");
        save.LearnedInner.Add("supreme");
        save.InnerLevels.Add(new GearLevel { Id = "supreme", Level = InnerArts.MaxLevel });
        Clear(save, 30, 1);
        return save;
    }

    [Test]
    public void TheRewardIsATranscendentWeaponAboveEveryGachaGradeAndNeverInThePool()
    {
        var sword = EarlyGameContent.FindItem(Ritual.RewardId);
        Assert.AreEqual(ItemGrade.Transcendent, sword.Grade);
        Assert.AreEqual("초월", Grades.NameOf(sword.Grade));
        Assert.IsFalse(sword.InPool);
        Assert.IsFalse(Gacha.Pool(ItemGrade.Transcendent).Any());
        foreach (var other in EarlyGameContent.Items.Where(i => i.IsGear && i.Kind == ItemKind.Weapon && i.Id != sword.Id))
            Assert.Greater(sword.AtkPct, other.AtkPct, other.Id);
        Assert.AreEqual(Grades.Rates.Length + 1, Grades.Names.Length);
        Assert.Greater(Gear.CostFor(sword, 1), Gear.CostFor(EarlyGameContent.FindItem("heaven_cutter"), 1));
    }

    [Test]
    public void EveryConditionMustBeMetAndTheRewardComesOnlyOnce()
    {
        var save = new SaveData();
        Assert.AreEqual(5, Ritual.Conditions(save).Count);
        Assert.IsFalse(Ritual.CanComplete(save));
        Assert.IsNull(Ritual.Complete(save));
        Assert.AreEqual(0, Inventory.Count(save, Ritual.RewardId));

        save = Ready();
        Assert.IsTrue(Ritual.CanComplete(save));
        save.TowerBest = 49;
        Assert.IsFalse(Ritual.CanComplete(save), "tower floor 50 is required");
        save.TowerBest = 50;
        save.InnerLevels[0].Level = 4;
        Assert.IsFalse(Ritual.CanComplete(save), "무극대성 must be fully enhanced");
        save.InnerLevels[0].Level = InnerArts.MaxLevel;

        Assert.IsNotNull(Ritual.Complete(save));
        Assert.IsTrue(save.RitualDone);
        Assert.AreEqual(1, Inventory.Count(save, Ritual.RewardId));
        Assert.IsNull(Ritual.Complete(save), "no second reward");
        Assert.IsFalse(Ritual.CanComplete(save));
    }
}
