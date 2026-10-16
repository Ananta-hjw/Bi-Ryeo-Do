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
}
