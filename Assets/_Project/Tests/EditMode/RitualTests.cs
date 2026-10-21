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
        Inventory.Add(save, Ritual.BladeId, 1);
        Gear.EnsureProgression(save);
        save.GearBreakthroughs.Add(new GearLevel { Id = Ritual.BladeId, Level = Gear.MaxStars });
        return save;
    }

    [Test]
    public void FiveStarRewardEffectsMatchPreviewAndBattleWithoutDoubleBonus()
    {
        var save = new SaveData();
        Inventory.Add(save, Ritual.RewardId, 1);
        string error;
        Assert.IsTrue(Inventory.Equip(save, Ritual.RewardId, out error), error);
        var item = EarlyGameContent.FindItem(Ritual.RewardId);
        Assert.AreEqual(5, Gear.Stars(save, item.Id));
        var stats = new CombatStats { MaxHp = 1000, MaxQi = 100, Attack = 1000, Defense = 100, Speed = 100, CritRate = 0f, CritDamage = 1.5f };
        var actual = Inventory.ApplyGear(stats, save.EquippedGear, Gear.LevelsFor(save, Inventory.Hero));
        Assert.AreEqual(1462, actual.MaxHp);
        Assert.AreEqual(2846, actual.Attack);
        Assert.AreEqual(131, actual.Speed);
        Assert.AreEqual(.30f * 1.5f / 1.3f, actual.CritRate, .00001f);
        Assert.AreEqual(1.5f + .50f * 2f / 1.3f, actual.CritDamage, .00001f);
        Assert.AreEqual(100, actual.Defense);
        Assert.AreEqual(100, actual.MaxQi);
        Assert.AreEqual(0, actual.StartSpirit);
        var effects = StatusSheet.EffectText(item, 0, Gear.MaxStars);
        StringAssert.Contains("체력 +46.2%", effects);
        StringAssert.Contains("공격 +184.6%", effects);
        StringAssert.Contains("속도 +31", effects);
        StringAssert.Contains("치명타 +34.6%", effects);
        StringAssert.Contains("치명타 피해 +76.9%", effects);
        Assert.AreEqual(effects, StatusSheet.EffectText(item));
        Assert.AreEqual(2f / 1.3f, Gear.EffectMultiplier(item, 0, 5), .00001f);
        Assert.AreEqual((Gear.Multiplier(100) + 1f) / 1.3f, Gear.EffectMultiplier(item, 100, 5), .00001f);
        Assert.Greater(Gear.EffectMultiplier(item, 10, 5), 1f, "slot enhancement must still improve the reward");
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
    }

    [Test]
    public void EveryConditionMustBeMetAndTheRewardComesOnlyOnce()
    {
        var save = new SaveData();
        Assert.AreEqual(6, Ritual.Conditions(save).Count);
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
        save.GearBreakthroughs.First(l => l.Id == Ritual.BladeId).Level = Gear.MaxStars - 1;
        Assert.IsFalse(Ritual.CanComplete(save), "비뢰도 낙뢰 must have five breakthrough stars");
        save.GearBreakthroughs.First(l => l.Id == Ritual.BladeId).Level = Gear.MaxStars;
        Inventory.Remove(save, Ritual.BladeId, 1);
        Assert.IsFalse(Ritual.CanComplete(save), "비뢰도 낙뢰 must be owned");
        Inventory.Add(save, Ritual.BladeId, 1);
        Gear.EnsureProgression(save);
        save.GearBreakthroughs.Add(new GearLevel { Id = Ritual.BladeId, Level = Gear.MaxStars });

        Assert.IsNotNull(Ritual.Complete(save));
        Assert.IsTrue(save.RitualDone);
        Assert.AreEqual(1, Inventory.Count(save, Ritual.RewardId));
        Assert.IsNull(Ritual.Complete(save), "no second reward");
        Assert.IsFalse(Ritual.CanComplete(save));
    }
}
