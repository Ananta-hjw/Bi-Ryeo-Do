using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

public class FormationTests
{
    private static CombatStats Stats(int hp = 5000, int atk = 100, int speed = 100)
    {
        return new CombatStats { MaxHp = hp, MaxQi = 100, Attack = atk, Defense = 50, Speed = speed, CritRate = 0f, CritDamage = 1.5f };
    }

    private static Combatant Hero(params SkillSpec[] skills) { return new Combatant("player", "비류연", Team.Player, Stats(atk: 300), skills); }

    private static Combatant Foe(string id, int col = -1, int row = -1)
    {
        return new Combatant(id, id, Team.Enemy, Stats(1000000, 1), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("x", 0.1f) }, Col = col, Row = row };
    }

    [Test]
    public void AutoPlacementPutsTheHeroAndGuardInFrontAndTheHealerBehind()
    {
        var map = Formation.Auto(new[] { "player", "yun_junho", "hyo_ryong", "jang_hong" });
        CollectionAssert.AreEqual(new[] { 2, 1 }, map["player"]);
        CollectionAssert.AreEqual(new[] { 2, 0 }, map["hyo_ryong"]);   // guard
        CollectionAssert.AreEqual(new[] { 2, 2 }, map["jang_hong"]);
        CollectionAssert.AreEqual(new[] { 1, 1 }, map["yun_junho"]);   // healer stays back
        Assert.AreEqual(4, map.Values.Select(t => t[0] * 10 + t[1]).Distinct().Count());
    }

    [Test]
    public void ACustomPlacementSwapsAndFallsBackWhenItNoLongerFitsTheParty()
    {
        var save = new SaveData();
        var ids = new[] { "player", "hyo_ryong", "yun_junho" };
        string err;
        Assert.IsFalse(Formation.SetTile(save, ids, "player", 3, 1, out err), "the enemy half is off limits");
        Assert.IsFalse(save.PlacementCustom);
        Assert.IsTrue(Formation.SetTile(save, ids, "yun_junho", 2, 1, out err), err);   // onto the hero's tile: they swap
        var map = Formation.Resolve(save, ids);
        CollectionAssert.AreEqual(new[] { 2, 1 }, map["yun_junho"]);
        Assert.AreNotEqual(new[] { 2, 1 }, map["player"]);
        Assert.IsTrue(save.PlacementCustom);
        // a companion who is not in the party any more: the saved tiles still fit the three who are
        var two = Formation.Resolve(save, new[] { "player", "yun_junho" });
        CollectionAssert.AreEqual(new[] { 2, 1 }, two["yun_junho"]);
        // a newcomer without a saved tile: back to the automatic arrangement
        var four = Formation.Resolve(save, new[] { "player", "hyo_ryong", "yun_junho", "jang_hong" });
        CollectionAssert.AreEqual(Formation.Auto(new[] { "player", "hyo_ryong", "yun_junho", "jang_hong" })["player"], four["player"]);
        Formation.Reset(save);
        Assert.IsFalse(save.PlacementCustom);
    }

    [Test]
    public void EnemiesFillTheFrontColumnAndBossesStandOneColumnBehind()
    {
        var boss = Foe("boss"); boss.IsBoss = true;
        var a = Foe("a"); var b = Foe("b"); var c = Foe("c");
        Formation.PlaceEnemies(new[] { boss, a, b, c });
        Assert.AreEqual(4, boss.Col);
        Assert.AreEqual(3, a.Col); Assert.AreEqual(3, b.Col); Assert.AreEqual(3, c.Col);
        Assert.AreEqual(3, new[] { a, b, c }.Select(e => e.Row).Distinct().Count());
        var lone = Foe("lone"); lone.IsBoss = true;
        Formation.PlaceEnemies(new[] { lone });
        Assert.AreEqual(3, lone.Col, "a lone enemy stands right in front of us");
    }

    [Test]
    public void WalkingIsLimitedToFreeTilesWithinTwoAndCostsNoTurn()
    {
        var hero = Hero();
        var foe = Foe("a");
        var battle = new InteractiveBattle(new[] { hero }, new[] { foe }, new SeededRandom(1));
        battle.Begin();
        Assert.AreEqual(2, hero.Col); Assert.AreEqual(1, hero.Row);
        Assert.IsTrue(battle.CanMoveTo(hero, 0, 1));
        Assert.IsTrue(battle.CanMoveTo(hero, 1, 2));
        Assert.IsFalse(battle.CanMoveTo(hero, 3, 1), "occupied by the enemy");
        Assert.IsFalse(battle.CanMoveTo(hero, 5, 1), "too far");
        Assert.IsFalse(battle.CanMoveTo(hero, 2, 3), "off the board");
        Assert.Throws<System.InvalidOperationException>(() => battle.PlayerAct(PlayerCommand.Guard().WithMove(5, 0)));
    }

    [Test]
    public void AMoveEventPrecedesTheActionAndOutOfRangeIsRejected()
    {
        var hero = Hero();
        var foe = Foe("a", 4, 1);   // two tiles from the hero's start (2,1)
        var battle = new InteractiveBattle(new[] { hero }, new[] { foe }, new SeededRandom(1));
        battle.Begin();
        Assert.Throws<System.InvalidOperationException>(() => battle.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), foe)), "the basic attack reaches 1");
        Assert.IsNotNull(battle.Current, "a rejected command leaves the turn intact");
        var events = battle.PlayerAct(PlayerCommand.UseSkill(SkillSpec.BasicAttack(), foe).WithMove(3, 1));
        Assert.AreEqual(EventKind.Move, events[0].Kind);
        Assert.AreEqual(3, events[0].ToCol);
        Assert.AreEqual(3, hero.Col);
        Assert.IsTrue(events.Any(e => e.Kind == EventKind.Attack && e.TargetId == "a" && e.Damage > 0));
    }

    [Test]
    public void RangesDecideWhoCanBeHit()
    {
        var far = new SkillSpec { Id = "far", Name = "원거리", Power = 1f, Range = 4 };
        var hero = Hero(far);
        var near = Foe("near", 3, 1); var farFoe = Foe("far", 5, 1);
        var battle = new InteractiveBattle(new[] { hero }, new[] { near, farFoe }, new SeededRandom(1));
        battle.Begin();
        Assert.AreEqual(1, battle.EnemiesInRange(2, 1, 1).Count);
        Assert.AreEqual(2, battle.EnemiesInRange(2, 1, 4).Count);
        Assert.AreSame(farFoe, battle.ResolveTarget(farFoe, far, 2, 1), "range 4 reaches column 5");
        Assert.AreSame(near, battle.ResolveTarget(farFoe, SkillSpec.BasicAttack(), 2, 1), "out of the pick's range: the nearest in range");
        var events = battle.PlayerAct(PlayerCommand.UseSkill(far, farFoe));
        Assert.IsTrue(events.Any(e => e.TargetId == "far" && e.Damage > 0));
    }

    [Test]
    public void AreaArtsHitEveryEnemyInRangeOnly()
    {
        var aoe = new SkillSpec { Id = "aoe", Name = "범위", Power = 1.0f, AllEnemies = true, Range = 2 };
        var hero = Hero(aoe);
        var a = Foe("a", 3, 1); var b = Foe("b", 4, 0); var c = Foe("c", 5, 2);
        var battle = new InteractiveBattle(new[] { hero }, new[] { a, b, c }, new SeededRandom(1));
        battle.Begin();
        var events = battle.PlayerAct(PlayerCommand.UseSkill(aoe, a));
        Assert.IsTrue(events.Any(e => e.TargetId == "a" && e.Damage > 0));
        Assert.IsTrue(events.Any(e => e.TargetId == "b" && e.Damage > 0), "(4,0) is two tiles from (2,1)");
        Assert.IsFalse(events.Any(e => e.TargetId == "c" && e.Damage > 0), "(5,2) is out of range");
    }

    [Test]
    public void EnemiesWalkTowardsUsAndOnlyStrikeInRange()
    {
        var hero = new Combatant("player", "비류연", Team.Player, Stats(speed: 130), null) { Col = 0, Row = 1 };   // far away, acts first
        var foe = new Combatant("e", "산적", Team.Enemy, Stats(1000000, 100, 100), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("베기", 1.0f) }, Col = 5, Row = 1 };
        var battle = new InteractiveBattle(new[] { hero }, new[] { foe }, new SeededRandom(1));
        battle.Begin();
        var events = battle.PlayerAct(PlayerCommand.Guard());
        var walk = events.First(e => e.Kind == EventKind.Move);
        Assert.AreEqual(5, walk.FromCol);
        Assert.AreEqual(3, foe.Col, "walked two tiles");
        Assert.IsFalse(events.Any(e => e.Kind == EventKind.Attack), "three tiles still between them: no attack yet");
        Assert.AreEqual(hero.Stats.MaxHp, hero.Hp);
        // it keeps coming: next turn it is adjacent and swings in the same turn
        var more = battle.PlayerAct(PlayerCommand.UseItem(new ItemData { Id = "x", Kind = ItemKind.Consumable, Name = "x" }));
        Assert.IsTrue(more.Any(e => e.Kind == EventKind.Move && e.ToCol == 1));
        Assert.IsTrue(more.Any(e => e.Kind == EventKind.Attack && e.TargetId == "player"));
    }

    [Test]
    public void EnemiesTargetTheNearestMember()
    {
        var front = new Combatant("player", "비류연", Team.Player, Stats(), null);
        var back = new Combatant("yun_junho", "윤준호", Team.Player, Stats(), null);
        var foe = new Combatant("e", "산적", Team.Enemy, Stats(1000000, 100), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("베기", 1.0f) } };
        var battle = new InteractiveBattle(new[] { front, back }, new[] { foe }, new SeededRandom(3));
        battle.Begin();
        for (int i = 0; i < 10 && !battle.IsOver; i++) battle.PlayerAct(PlayerCommand.Guard());
        Assert.Less(front.Hp, front.Stats.MaxHp);
        Assert.AreEqual(back.Stats.MaxHp, back.Hp, "the one behind is out of reach while the front member stands next to the enemy");
    }

    [Test]
    public void ArtRangesFollowTheirKind()
    {
        Assert.AreEqual(1, SkillSpec.BasicAttack().Range);
        foreach (var id in new[] { EarlyGameContent.StarterArtId, "bi_roe_do", "seomroe", "isj_iljeom", "ny_simahn" }) Assert.AreEqual(4, EarlyGameContent.FindArt(id).EffectiveRange, id);
        Assert.AreEqual(3, EarlyGameContent.FindArt("roegwang_ryuhagok").EffectiveRange, "area art");
        Assert.AreEqual(3, EarlyGameContent.FindArt("pungsin").EffectiveRange, "비기");
        Assert.AreEqual(1, EarlyGameContent.FindArt("jh_stun").EffectiveRange, "점혈");
        Assert.AreEqual(2, EarlyGameContent.FindArt("yeobanjang").EffectiveRange);
        Assert.AreEqual(4, EarlyGameContent.FindArt("bi_roe_do").ToSpec().Range);
    }
}
