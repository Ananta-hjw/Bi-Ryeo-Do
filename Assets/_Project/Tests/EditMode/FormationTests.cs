using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

public class FormationTests
{
    private static CombatStats Stats(int hp = 5000, int atk = 100)
    {
        return new CombatStats { MaxHp = hp, MaxQi = 100, Attack = atk, Defense = 50, Speed = 100, CritRate = 0f, CritDamage = 1.5f };
    }

    [Test]
    public void AutoFormationPutsTheHeroAndGuardInFrontAndTheHealerBehind()
    {
        var back = Formation.AutoBack(new[] { "player", "yun_junho", "hyo_ryong", "jang_hong" });
        Assert.AreEqual(2, back.Count);
        Assert.IsTrue(back.Contains("yun_junho"));   // healer
        Assert.IsTrue(back.Contains("jang_hong"));
        Assert.IsFalse(back.Contains("player"));
        Assert.IsFalse(back.Contains("hyo_ryong"));   // guard stays in front
        Assert.AreEqual(0, Formation.AutoBack(new[] { "player" }).Count);
        Assert.AreEqual(0, Formation.AutoBack(new[] { "player", "hyo_ryong" }).Count);
        Assert.AreEqual(1, Formation.AutoBack(new[] { "player", "hyo_ryong", "yun_junho" }).Count);
    }

    [Test]
    public void MovingSwapsWhenTheRowIsFullAndKeepsOneInFront()
    {
        var save = new SaveData();
        var ids = new[] { "player", "hyo_ryong", "jang_hong", "yun_junho" };
        string err;
        Assert.IsFalse(Formation.Move(save, new[] { "player" }, "player", out err), "the only member must stay in front");
        Assert.IsFalse(save.FormationCustom);
        Assert.IsTrue(Formation.Move(save, ids, "yun_junho", out err), err);   // healer forward: the guard (most back-minded front member) steps back
        Assert.IsTrue(save.FormationCustom);
        var back = Formation.BackSet(save, ids);
        Assert.AreEqual(2, back.Count);
        Assert.IsFalse(back.Contains("yun_junho"));
        Assert.IsTrue(back.Contains("hyo_ryong"));
        Assert.IsTrue(back.Contains("jang_hong"));
        Assert.IsTrue(Formation.Move(save, ids, "hyo_ryong", out err));   // and forward again
        Assert.IsFalse(Formation.BackSet(save, ids).Contains("hyo_ryong"));
        Formation.Reset(save);
        Assert.IsFalse(save.FormationCustom);
    }

    [Test]
    public void SavedFormationFallsBackToAutoWhenItNoLongerFitsTheParty()
    {
        var save = new SaveData { FormationCustom = true, BackRowIds = new List<string> { "hyo_ryong", "jang_hong", "yun_junho" } };
        var ids = new[] { "player", "hyo_ryong", "jang_hong", "yun_junho" };
        CollectionAssert.AreEquivalent(Formation.AutoBack(ids), Formation.BackSet(save, ids));   // 3 behind is invalid
        save.BackRowIds = new List<string> { "player", "hyo_ryong" };
        var back = Formation.BackSet(save, ids);
        Assert.IsTrue(back.Contains("player"));
        Assert.AreEqual(2, back.Count);
    }

    [Test]
    public void EnemiesHitTheFrontRowFirstAndTheBackRowTakesLess()
    {
        var front = new Combatant("player", "비류연", Team.Player, Stats(), null);
        var back = new Combatant("yun_junho", "윤준호", Team.Player, Stats(), null) { BackRow = true };
        var foe = new Combatant("e", "산적", Team.Enemy, Stats(1000000, 100), null)
        {
            Moves = new List<EnemyMove> { EnemyMove.Attack("베기", 1.0f) }
        };
        var battle = new InteractiveBattle(new[] { front, back }, new[] { foe }, new SeededRandom(7));
        battle.Begin();
        for (int i = 0; i < 12 && !battle.IsOver; i++) battle.PlayerAct(PlayerCommand.Guard());
        Assert.Less(front.Hp, front.Stats.MaxHp);
        Assert.AreEqual(back.Stats.MaxHp, back.Hp, "back row untouched while the front stands");

        // with the front fallen, the back row is the target and takes the reduced damage
        var a = new Combatant("a", "A", Team.Player, Stats(1), null);
        var b = new Combatant("b", "B", Team.Player, Stats(), null) { BackRow = true };
        var plain = new Combatant("c", "C", Team.Player, Stats(), null);
        var foe2 = new Combatant("e", "산적", Team.Enemy, Stats(1000000, 100), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("베기", 1.0f) } };
        var foe3 = new Combatant("e", "산적", Team.Enemy, Stats(1000000, 100), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("베기", 1.0f) } };
        int dmgBack = FirstHitOn(b, foe2);
        int dmgFront = FirstHitOn(plain, foe3);
        Assert.Less(dmgBack, dmgFront);
    }

    private static int FirstHitOn(Combatant victim, Combatant foe)
    {
        var battle = new InteractiveBattle(new[] { victim }, new[] { foe }, new SeededRandom(3));
        var events = battle.Begin();
        for (int i = 0; i < 30 && !events.Any(e => e.Kind == EventKind.Attack && e.Damage > 0) && !battle.IsOver; i++) events = battle.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), foe));
        return events.First(e => e.Kind == EventKind.Attack && e.TargetId == victim.Id && e.Damage > 0).Damage;
    }

    [Test]
    public void FrontRowHitsHarderThanBackRow()
    {
        var f = new Combatant("player", "비류연", Team.Player, Stats(atk: 300), null);
        var b = new Combatant("x", "X", Team.Player, Stats(atk: 300), null) { BackRow = true };
        var dummyF = new Combatant("d1", "허수아비", Team.Enemy, Stats(1000000, 1), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("x", 0.1f) } };
        var dummyB = new Combatant("d2", "허수아비", Team.Enemy, Stats(1000000, 1), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("x", 0.1f) } };
        int dealtFront = HitDamage(f, dummyF);
        int dealtBack = HitDamage(b, dummyB);
        Assert.Greater(dealtFront, dealtBack);
    }

    private static int HitDamage(Combatant attacker, Combatant dummy)
    {
        var battle = new InteractiveBattle(new[] { attacker }, new[] { dummy }, new SeededRandom(1));
        var events = battle.Begin();
        events = battle.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), dummy));
        return events.First(e => e.ActorId == attacker.Id && e.Kind == EventKind.Attack).Damage;
    }
}
