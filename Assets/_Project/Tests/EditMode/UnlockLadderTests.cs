using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

public class UnlockLadderTests
{
    private static void Clear(SaveData save, int stage, int diff)
    {
        for (int f = 0; f < StageStars.FightCount(stage); f++) StageStars.Record(save, stage, f, diff, 1);
    }

    [Test]
    public void TheFinalArtAndTheFinalInnerArtSitAtHardStageThirty()
    {
        var pungsin = EarlyGameContent.FindArt("pungsin");
        Assert.AreEqual(30, pungsin.UnlockStage);
        Assert.AreEqual(1, pungsin.UnlockDiff);
        var save = new SaveData { ClearedStage = 30 };
        Assert.IsFalse(pungsin.IsUnlocked(save));
        for (int s = 1; s <= 30; s++) Clear(save, s, 0);
        Assert.IsFalse(pungsin.IsUnlocked(save));   // normal clear is not enough
        for (int s = 1; s <= 29; s++) Clear(save, s, 1);
        Assert.IsFalse(pungsin.IsUnlocked(save));
        Clear(save, 30, 1);
        Assert.IsTrue(pungsin.IsUnlocked(save));
        Assert.IsTrue(InnerArts.IsUnlocked(save, InnerArts.Find("supreme")));
    }

    [Test]
    public void LearningStepsAreSpreadOutAndOrderedByPower()
    {
        // the ladder: 일반 stage N = N, 어려움 stage N = 30 + N
        var steps = EarlyGameContent.Arts.Where(a => a.OwnerId == null && a.LearnCost > 0)
            .Select(a => a.UnlockDiff * 30 + a.UnlockStage).OrderBy(x => x).ToList();
        Assert.GreaterOrEqual(steps.Distinct().Count(), steps.Count - 1);   // nearly every art has its own step
        Assert.AreEqual(60, steps.Last());
        for (int i = 1; i < steps.Count; i++) Assert.LessOrEqual(steps[i] - steps[i - 1], 12, "gap before step " + steps[i]);
        var inner = InnerArts.All.Select(a => a.UnlockDiff * 30 + a.UnlockStage).OrderBy(x => x).ToList();
        Assert.AreEqual(60, inner.Last());
        for (int i = 1; i < inner.Count; i++) Assert.LessOrEqual(inner[i] - inner[i - 1], 12);
    }

    [Test]
    public void FourthInnerSlotNeedsHardStageFifteen()
    {
        var save = new SaveData { ClearedStage = 30 };
        Assert.AreEqual(3, InnerArts.SlotCount(save));
        Clear(save, InnerArts.FourthSlotStage, 1);
        Assert.AreEqual(4, InnerArts.SlotCount(save));
    }

    [Test]
    public void LearningAHardArtFailsBeforeItsStageIsCleared()
    {
        var save = new SaveData { ClearedStage = 30, Doo = 1000000 };
        string err;
        Assert.IsFalse(InnerArts.Learn(save, "breaker", out err));
        StringAssert.Contains("어려움", err);
        Clear(save, 5, 1);
        Assert.IsTrue(InnerArts.Learn(save, "breaker", out err));
    }

    [Test]
    public void InsightStonesEnhanceInnerArtsAndShrinkDrawbacks()
    {
        var save = new SaveData { ClearedStage = 30, LearnedInner = { "tiger" } };
        string err;
        Assert.IsFalse(InnerArts.Enhance(save, "tiger", out err));   // no stones
        Inventory.Add(save, InnerArts.StoneId, 19);
        for (int i = 0; i < InnerArts.MaxLevel; i++) Assert.IsTrue(InnerArts.Enhance(save, "tiger", out err), err);
        Assert.AreEqual(0, InnerArts.Stones(save));   // 1+2+3+5+8
        Assert.IsFalse(InnerArts.Enhance(save, "tiger", out err));   // at max
        Assert.AreEqual(InnerArts.MaxLevel, InnerArts.Level(save, "tiger"));
        var stats = new CombatStats { MaxHp = 1000, MaxQi = 100, Attack = 100, Defense = 100, Speed = 100, CritRate = 0.05f, CritDamage = 1.5f };
        var plain = InnerArts.Apply(stats, new[] { "tiger" });
        var maxed = InnerArts.Apply(stats, new[] { "tiger" }, save.InnerLevels);
        Assert.AreEqual(115, plain.Attack);
        Assert.AreEqual(122, maxed.Attack);   // +15% x1.5 = +22.5%
        Assert.AreEqual(95, plain.Defense);
        Assert.AreEqual(98, maxed.Defense);   // -5% halved = -2.5%
    }

    [Test]
    public void InsightStonesComeFromHardClearsChestsAndDailyRewards()
    {
        Assert.AreEqual(0, StageStars.ClearInsight(3, 0));
        Assert.AreEqual(1, StageStars.ClearInsight(5, 0));
        Assert.AreEqual(1, StageStars.ClearInsight(3, 1));
        Assert.AreEqual(2, StageStars.ClearInsight(10, 1));
        Assert.AreEqual(3, StageStars.ClearInsight(10, 2));
        var save = new SaveData();
        for (int f = 0; f < StageStars.FightCount(3); f++) StageStars.Record(save, 3, f, 1, 1);
        Assert.AreEqual(1, StageStars.GrantClearInsight(save, 3, 1, false));
        Assert.AreEqual(0, StageStars.GrantClearInsight(save, 3, 1, true));   // replays pay nothing
        Assert.AreEqual(1, InnerArts.Stones(save));
        Assert.IsTrue(StageStars.Chests.All(c => c.Insight > 0));
    }
}
