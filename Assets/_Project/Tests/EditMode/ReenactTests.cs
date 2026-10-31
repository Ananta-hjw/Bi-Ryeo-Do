using System;
using System.Collections.Generic;
using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class ReenactTests
    {

        [Test]
        public void NamgungSangAndMoYonghwiJoinWithTheirOwnArts()
        {
            var sang = EarlyGameContent.FindCompanion("namgung_sang");
            var hwi = EarlyGameContent.FindCompanion("mo_yonghwi");
            Assert.AreEqual(32, sang.JoinStage);
            Assert.AreEqual(35, hwi.JoinStage);
            foreach (var c in new[] { sang, hwi })
            {
                var young = c.CreateCombatant(26);
                var grown = c.CreateCombatant(60);
                Assert.Greater(young.Skills.Count(s => s.Id != "basic"), 2, c.Id);
                Assert.LessOrEqual(grown.Skills.Count(s => s.Id != "basic" && s.SpiritCost <= 0), Progression.MaxEquippedArts + 1, c.Id + " fits the command bar");
                Assert.AreEqual(1, grown.Skills.Count(s => s.SpiritCost == 100), c.Id + " learns one 비기");
                Assert.IsFalse(young.Skills.Any(s => s.SpiritCost > 0));
            }
            CollectionAssert.Contains(EarlyGameContent.CompanionsFor(32), sang);
            CollectionAssert.DoesNotContain(EarlyGameContent.CompanionsFor(34), hwi);
            Assert.AreEqual("namgung_sang", Roster.CompanionIdOf("story_namgung_sang"));
            Assert.AreEqual("mo_yonghwi", Roster.CompanionIdOf("story_mo_yonghwi"));
        }

        [Test]
        public void EveryStageFrom31UsesThePlayersPartyAndNoStoryOnlyBody()
        {
            foreach (var stage in EarlyGameContent.Stages)
                Assert.AreEqual(stage.Number >= 31, Roster.Reenacted(stage), "stage " + stage.Number);
            Assert.IsNotNull(Roster.StoryActorsFor(EarlyGameContent.FindStage(24), 1), "24's coin duel stays 비류연 alone");
            var save = BalanceSimulation.Starter(66, 50);
            foreach (var stage in EarlyGameContent.Stages.Where(x => x.Number >= 31))
            {
                int n = stage.Number;
                Assert.AreEqual("", Roster.StoryPartySummary(stage));
                StringAssert.Contains("원작", Roster.ReenactSummary(stage));
                for (int i = 0; i < stage.Encounters.Length; i++)
                {
                    Assert.IsNull(Roster.StoryActorsFor(stage, i));
                    var party = BalanceSimulation.FightParty(save, stage, i);
                    BalanceSimulation.AssertActors(stage, i, party);
                    Assert.LessOrEqual(party.Count, EarlyGameContent.MaxCompanions + 1);
                    var trial = stage.TrialFor(i);
                    if (trial != null && trial.Kind == TrialKind.Technique)
                        foreach (var id in trial.TechniqueSequence ?? new[] { trial.TechniqueId })
                            Assert.IsTrue(party.Any(m => m.Skills.Any(s => s.Id == id)), n + "/" + i + " needs " + id + " from a required companion");
                }
            }
            CollectionAssert.AreEqual(new[] { "na_yerin", "hyo_ryong" }, Roster.RequiredFor(EarlyGameContent.FindStage(34), 0).Select(c => c.Id));
            CollectionAssert.AreEqual(new[] { "namgung_sang", "mo_yonghwi" }, Roster.RequiredFor(EarlyGameContent.FindStage(35), 0).Select(c => c.Id));
            Assert.IsEmpty(Roster.RequiredFor(EarlyGameContent.FindStage(63), 0), "백무영·청흔·용천명·마하령 are not companions");
            Assert.IsNull(EarlyGameContent.FindStage(63).TrialFor(0), "their scene technique became an ordinary fight");
            Assert.AreEqual(TrialKind.Technique, EarlyGameContent.FindStage(50).TrialFor(3).Kind, "모용휘 is a companion: his 염천빙지 일원합 stays");
        }

        [Test]
        public void RequiredCompanionsFightEvenWhenBenchedAndTakeTheLastPicksPlace()
        {
            var save = BalanceSimulation.Starter(35, 40);
            save.ActiveCompanions = new List<string> { "na_yerin", "im_seongjin", "jang_hong" };
            save.BenchedCompanions = new List<string> { "namgung_sang", "mo_yonghwi", "hyo_ryong", "yun_junho" };
            var stage = EarlyGameContent.FindStage(35);
            CollectionAssert.AreEqual(new[] { "namgung_sang", "mo_yonghwi", "na_yerin" }, Roster.CompanionsFor(stage, 0, save).Select(c => c.Id));
            CollectionAssert.AreEqual(new[] { "na_yerin", "im_seongjin", "jang_hong" }, Roster.CompanionsFor(stage, 1, save).Select(c => c.Id), "no one is required: the player's own picks");
            Assert.IsTrue(Roster.RequiredInStage(stage, "mo_yonghwi"));
            Assert.IsFalse(Roster.RequiredInStage(stage, "na_yerin"));
            Assert.IsTrue(Roster.RequiredInStage(EarlyGameContent.FindStage(36), "namgung_sang"));
            Assert.IsFalse(Roster.RequiredInStage(EarlyGameContent.FindStage(24), "hyo_ryong"), "a fixed story stage before 31 locks no one in the party screen");
            Assert.AreEqual(Inventory.Hero, Roster.CompanionIdOf("story_biry_escape"), "비류연's scene bodies are 비류연");
            CollectionAssert.AreEqual(new[] { "na_yerin", "im_seongjin", "jang_hong" }, save.ActiveCompanions, "the saved picks are never rewritten");
            var quest = Quests.ToStage(Quests.Find("q_hyeol"));
            Assert.IsFalse(Roster.Reenacted(quest));
        }

        [Test]
        public void SceneTechniqueJoinsTheCompanionsOwnArtsAndWoundsCarryWhenThePartyChanges()
        {
            var save = BalanceSimulation.Starter(50, 60);
            var stage = EarlyGameContent.FindStage(50);
            var hwi = CompanionGrowth.Create(save, EarlyGameContent.FindCompanion("mo_yonghwi"));
            int own = hwi.Skills.Count(s => s.Id.StartsWith("mh_"));
            var party = new List<Combatant> { hwi };
            Roster.PrepareStoryEncounter(stage, 3, party);
            Assert.IsTrue(hwi.Skills.Any(s => s.Id == "scene_ice_union"));
            Assert.LessOrEqual(hwi.Skills.Count(s => s.Id != "basic" && s.SpiritCost <= 0), Progression.MaxEquippedArts + 1);
            Assert.GreaterOrEqual(hwi.Skills.Count(s => s.Id.StartsWith("mh_")), own - 1, "at most one own art makes room");
            Roster.PrepareStoryEncounter(stage, 0, party);
            Assert.IsFalse(hwi.Skills.Any(s => s.Id.StartsWith("scene_")), "the technique belongs to that fight only");
            Assert.IsFalse(Roster.SamePerspective(stage, 2, 3), "technique fights rebuild the party so the borrowed slot returns");

            var hall = EarlyGameContent.FindStage(44);
            Assert.IsFalse(Roster.SamePerspective(hall, 3, 4), "나예린 then 남궁상 are required");
            var before = BalanceSimulation.FightParty(save, hall, 3);
            before[0].Hp = 77; before[0].Spirit = 40;
            var after = BalanceSimulation.FightParty(save, hall, 4);
            Roster.CarryWounds(before, after);
            Assert.AreEqual(Inventory.Hero, after[0].Id);
            Assert.AreEqual(40, after[0].Spirit);
            Assert.Less(after[0].Hp, after[0].Stats.MaxHp);
            Assert.IsTrue(after.Any(m => m.Id == "namgung_sang"));
        }

        [Test]
        public void EnemiesFrom31GrowForThePartyOfFourOn일반Only()
        {
            Assert.AreEqual(1f, StageStars.PartyHarden(30));
            Assert.AreEqual(6f, StageStars.PartyHarden(31), 1e-4f, "유재룡 fought alone: 4 / 1 x 1.5");
            Assert.AreEqual(1.5f, StageStars.PartyHarden(63), 1e-4f, "the four of the 합주 scene: 4 / 4 x 1.5");
            var stage = EarlyGameContent.FindStage(32);
            string id = stage.Encounters[0][0];
            double scale = StageStars.PartyHardenScale;
            try
            {
                var harder = stage.EnemyFor(id, stage.TrialFor(0)).BuildStatsBeforeRealm();
                var hard = stage.WithDifficulty(1).EnemyFor(id, stage.TrialFor(0)).BuildStatsBeforeRealm();
                StageStars.PartyHardenScale = 0;
                var plain = stage.EnemyFor(id, stage.TrialFor(0)).BuildStatsBeforeRealm();
                var hardPlain = stage.WithDifficulty(1).EnemyFor(id, stage.TrialFor(0)).BuildStatsBeforeRealm();
                Assert.Greater(harder.EffAttack, plain.EffAttack * 2);
                Assert.AreEqual(hardPlain.EffAttack, hard.EffAttack, 1e-6, "어려움·극악 keep their own tables");
            }
            finally { StageStars.PartyHardenScale = scale; }
        }

        [Test]
        public void TrialWallsFrom31HitLikeTheStagesOtherEnemies()
        {
            var stage = EarlyGameContent.FindStage(55);
            var trial = stage.TrialFor(0);
            string id = stage.Encounters[0][0];
            Assert.Greater(EarlyGameContent.FindEnemy(id).HpMul, 10f, "a wall: endured or answered, not beaten");
            var now = stage.EnemyFor(id, trial).BuildStatsBeforeRealm();
            double grow = Math.Pow(StageStars.PartyHarden(55), StageStars.HardenHpExp);
            int from = Roster.ReenactFrom;
            try
            {
                Roster.ReenactFrom = 67;
                var before = stage.EnemyFor(id, trial).BuildStatsBeforeRealm();
                Assert.Greater(now.EffAttack, before.EffAttack * 2);
                Assert.AreEqual(before.MaxHp * grow, now.MaxHp, before.MaxHp * 0.01, "its HP only follows the party factor");
            }
            finally { Roster.ReenactFrom = from; }
        }
    }
}
