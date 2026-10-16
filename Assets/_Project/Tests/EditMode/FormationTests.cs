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

    private static InteractiveBattle Fight(Combatant[] party, Combatant[] foes, int seed = 1)
    {
        var battle = new InteractiveBattle(party, foes, new SeededRandom(seed));
        battle.Begin();
        return battle;
    }

    [Test]
    public void PositionsNeverChangeDuringAFight()
    {
        var hero = Hero(); var buddy = new Combatant("yun_junho", "윤준호", Team.Player, Stats(), null);
        var foe = Foe("e");
        foe.Moves = new List<EnemyMove> { EnemyMove.Attack("베기", 0.1f) };
        var battle = Fight(new[] { hero, buddy }, new[] { foe });
        var before = new[] { hero.Col, hero.Row, buddy.Col, buddy.Row, foe.Col, foe.Row };
        for (int i = 0; i < 8 && !battle.IsOver; i++) battle.PlayerAct(PlayerCommand.Guard());
        CollectionAssert.AreEqual(before, new[] { hero.Col, hero.Row, buddy.Col, buddy.Row, foe.Col, foe.Row });
    }

    [Test]
    public void ReachCountsLinesFromTheEnemyFront()
    {
        var one = new SkillSpec { Id = "one", Name = "근접", Power = 1f, Range = 1 };
        var two = new SkillSpec { Id = "two", Name = "중거리", Power = 1f, Range = 2 };
        var far = new SkillSpec { Id = "far", Name = "원거리", Power = 1f, Range = 4 };
        var hero = Hero(one, two, far);
        var front = Foe("front", 3, 1); var mid = Foe("mid", 4, 1); var back = Foe("back", 5, 1);
        var battle = Fight(new[] { hero }, new[] { back, mid, front });
        Assert.AreEqual(0, battle.Depth(front)); Assert.AreEqual(1, battle.Depth(mid)); Assert.AreEqual(2, battle.Depth(back));
        CollectionAssert.AreEqual(new[] { front }, battle.EnemiesInRange(1));
        CollectionAssert.AreEqual(new[] { front, mid }, battle.EnemiesInRange(2));
        Assert.AreEqual(3, battle.EnemiesInRange(4).Count);
        Assert.AreSame(front, battle.ResolveTarget(back, one), "a pick outside the reach falls back to the front line");
        Assert.AreSame(mid, battle.ResolveTarget(mid, two), "a pick inside the reach is honoured");
        Assert.AreSame(back, battle.ResolveTarget(back, far));
        Assert.IsFalse(battle.PlayerAct(PlayerCommand.UseSkill(one, back)).Any(e => e.TargetId == "back" && e.Damage > 0));
    }

    [Test]
    public void WhenTheFrontLineFallsTheNextLineBecomesFront()
    {
        var hero = Hero(); var one = new SkillSpec { Id = "one", Name = "근접", Power = 50f, Range = 1 };
        hero.Skills.Add(one);
        var front = new Combatant("front", "front", Team.Enemy, Stats(1, 1), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("x", 0.1f) }, Col = 3, Row = 1 };
        var back = Foe("back", 5, 1);
        var battle = Fight(new[] { hero }, new[] { front, back });
        battle.PlayerAct(PlayerCommand.UseSkill(one, front));
        Assert.IsFalse(front.IsAlive);
        Assert.AreEqual(0, battle.Depth(back), "the survivor is now the front line");
        Assert.IsTrue(battle.CanReach(one));
    }

    [Test]
    public void AreaArtsHitEveryEnemyInReachOnly()
    {
        var aoe = new SkillSpec { Id = "aoe", Name = "범위", Power = 1.0f, AllEnemies = true, Range = 2 };
        var hero = Hero(aoe);
        var a = Foe("a", 3, 1); var b = Foe("b", 4, 0); var c = Foe("c", 5, 2);
        var battle = Fight(new[] { hero }, new[] { a, b, c });
        var events = battle.PlayerAct(PlayerCommand.UseSkill(aoe, a));
        Assert.IsTrue(events.Any(e => e.TargetId == "a" && e.Damage > 0));
        Assert.IsTrue(events.Any(e => e.TargetId == "b" && e.Damage > 0));
        Assert.IsFalse(events.Any(e => e.TargetId == "c" && e.Damage > 0), "the back line is out of reach");
    }

    [Test]
    public void UnpickedSingleTargetHitsTheFrontAndSnipeHitsTheBack()
    {
        var plain = new SkillSpec { Id = "plain", Name = "일반", Power = 1f, Range = 4 };
        var snipe = new SkillSpec { Id = "snipe", Name = "저격", Power = 1f, Range = 4, Snipe = true };
        var hero = Hero(plain, snipe);
        var front = Foe("front", 3, 1); var back = Foe("back", 5, 1);
        var battle = Fight(new[] { hero }, new[] { back, front });
        Assert.AreSame(front, battle.ResolveTarget(null, plain), "no pick: the front line");
        Assert.AreSame(back, battle.ResolveTarget(null, snipe), "a 저격 art with no pick: the back-most within reach");
        Assert.AreSame(front, battle.ResolveTarget(front, snipe), "an explicit pick always wins");
    }

    [Test]
    public void EnemiesHitTheFrontLineAndSnipersTheBack()
    {
        Combatant Member(string id, int col) { return new Combatant(id, id, Team.Player, Stats(), null) { Col = col, Row = 1 }; }
        var front = Member("player", 2); var mid = Member("m", 1); var back = Member("b", 0);
        var brute = new Combatant("e", "산적", Team.Enemy, Stats(1000000, 100, 140), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("베기", 1.0f) }, Col = 3, Row = 1 };
        var battle = Fight(new[] { front, mid, back }, new[] { brute });
        for (int i = 0; i < 8 && !battle.IsOver; i++) battle.PlayerAct(PlayerCommand.Guard());
        Assert.Less(front.Hp, front.Stats.MaxHp);
        Assert.AreEqual(mid.Stats.MaxHp, mid.Hp); Assert.AreEqual(back.Stats.MaxHp, back.Hp);

        front = Member("player", 2); mid = Member("m", 1); back = Member("b", 0);
        var archer = new Combatant("e", "궁수", Team.Enemy, Stats(1000000, 100, 140), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("화살", 1.0f).Sniping() }, Col = 5, Row = 1 };
        battle = Fight(new[] { front, mid, back }, new[] { archer });
        for (int i = 0; i < 8 && !battle.IsOver; i++) battle.PlayerAct(PlayerCommand.Guard());
        Assert.Less(back.Hp, back.Stats.MaxHp, "the archer shoots the back-most member");
        Assert.AreEqual(front.Stats.MaxHp, front.Hp);
    }

    [Test]
    public void WhenTheFrontFallsTheNextLineIsHit()
    {
        var front = new Combatant("player", "비류연", Team.Player, Stats(1), null) { Col = 2, Row = 1 };
        var back = new Combatant("b", "윤준호", Team.Player, Stats(), null) { Col = 0, Row = 1 };
        var brute = new Combatant("e", "산적", Team.Enemy, Stats(1000000, 500, 140), null) { Moves = new List<EnemyMove> { EnemyMove.Attack("베기", 1.0f) }, Col = 3, Row = 1 };
        var battle = Fight(new[] { front, back }, new[] { brute });
        for (int i = 0; i < 8 && !battle.IsOver; i++) battle.PlayerAct(PlayerCommand.Guard());
        Assert.IsFalse(front.IsAlive);
        Assert.Less(back.Hp, back.Stats.MaxHp, "with the front line down, the back line is next");
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
