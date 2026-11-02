using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StoryLineTests
    {
        [TestCase("사부")]
        [TestCase("사부님")]
        [TestCase("노사부")]
        [TestCase("노사부님")]
        public void BorrowedMasterTitleUsesTheSceneIdentity(string speaker)
        {
            var line = StoryLine.Parse(speaker + "|제자야.");
            Assert.IsFalse(StoryAppearance.IsHeroSpeaking(line, null));
            Assert.IsTrue(StoryAppearance.IsHeroSpeaking(line, StoryAppearance.OldMasterDisguise));
            Assert.AreEqual(speaker, line.Speaker); // the displayed name and dialogue remain intact
            Assert.IsFalse(StoryAppearance.IsHeroSpeaking(StoryLine.Parse("주작단원|예, 사부님!"), StoryAppearance.OldMasterDisguise));
            Assert.IsFalse(StoryAppearance.IsHeroSpeaking(StoryLine.Parse("노사부가 돌아갔다."), StoryAppearance.OldMasterDisguise));
        }

        [Test]
        public void DisguiseScenesDoNotChangeTheRealMastersFlashback()
        {
            var first = EarlyGameContent.Stages.Single(s => s.Number == 1);
            var second = EarlyGameContent.Stages.Single(s => s.Number == 2);
            var third = EarlyGameContent.Stages.Single(s => s.Number == 3);
            Assert.IsNull(first.IntroHeroArtKey); // the hero talks to his actual master before the waterfall
            Assert.AreEqual(StoryAppearance.OldMasterDisguise, first.OutroHeroArtKey);
            Assert.AreEqual(StoryAppearance.OldMasterDisguise, second.IntroHeroArtKey);
            Assert.AreEqual(StoryAppearance.OldMasterDisguise, second.Prologue.Single(s => s.Title == "회상 — 합숙 초기와 연장된 수련").HeroArtKey);
            Assert.AreEqual(StoryAppearance.OldMasterDisguise, third.IntroHeroArtKey); // farewell, before the party travels alone
            Assert.IsNull(third.OutroHeroArtKey);
            Assert.IsNull(StoryBriefings.Interlude(1).HeroArtKey); // actual master in the same early volume
            Assert.AreEqual(StoryAppearance.OldMasterDisguise, StoryBriefings.Interlude(2).HeroArtKey);
            var arrival = StoryEpisodes.BeforeFight(first, 3, new SaveData()).Single(s => s.Title == "노사부와 열여섯 제자");
            Assert.AreEqual(StoryAppearance.OldMasterDisguise, arrival.HeroArtKey);
        }

        [Test]
        public void DisguiseChangesAtTheWhiteTigerAndIsClearedAfterTheAgency()
        {
            var first = EarlyGameContent.Stages.Single(s => s.Number == 1);
            var second = EarlyGameContent.Stages.Single(s => s.Number == 2);
            var third = EarlyGameContent.Stages.Single(s => s.Number == 3);
            var hero = Formation.BuildHero(new SaveData());
            int hp = hero.Hp, qi = hero.Qi;
            var party = new[] { hero };
            Roster.PrepareStoryEncounter(first, 0, party);
            Assert.AreEqual("player", Roster.StoryArtKey(hero));
            Roster.PrepareStoryEncounter(first, 3, party);
            Assert.AreEqual(StoryAppearance.OldMasterDisguise, Roster.StoryArtKey(hero));
            Roster.PrepareStoryEncounter(second, 0, party);
            Assert.AreEqual(StoryAppearance.OldMasterDisguise, Roster.StoryArtKey(hero));
            Roster.PrepareStoryEncounter(third, 0, party);
            Assert.AreEqual("player", Roster.StoryArtKey(hero));
            Assert.AreEqual("비류연", hero.Name);
            Assert.AreEqual(hp, hero.Hp);
            Assert.AreEqual(qi, hero.Qi);
            var tower = new StageData { Number = 2, IsTower = true };
            Assert.AreEqual("player", StoryAppearance.HeroBattleKey(tower, 0));
            var quest = new StageData { Number = 2, IsQuest = true };
            Assert.AreEqual("player", StoryAppearance.HeroBattleKey(quest, 0));
        }

        [Test]
        public void ParsesDialogueAndNarration()
        {
            var d = StoryLine.Parse("비류연|내가 진조운이 되면 되겠네.");
            Assert.IsTrue(d.IsDialogue);
            Assert.IsTrue(d.IsProtagonist);
            Assert.AreEqual("내가 진조운이 되면 되겠네.", d.Text);
            var n = StoryLine.Parse("아미산 중턱 사부의 모옥.");
            Assert.IsFalse(n.IsDialogue);
            Assert.AreEqual("아미산 중턱 사부의 모옥.", n.Text);
        }

        [Test]
        public void InventedLinesAreMarkedAndShownWithoutTheMark()
        {
            var d = StoryLine.Parse("+장홍|입구가……! 완전히 갇혔어!");
            Assert.IsTrue(d.Invented);
            Assert.AreEqual("장홍", d.Speaker);
            Assert.AreEqual("입구가……! 완전히 갇혔어!", d.Text);
            var n = StoryLine.Parse("+연기 속에서 마천칠걸이 뛰쳐나왔다.");
            Assert.IsTrue(n.Invented);
            Assert.IsFalse(n.IsDialogue);
            Assert.AreEqual("연기 속에서 마천칠걸이 뛰쳐나왔다.", n.Text);
            Assert.IsFalse(StoryLine.Parse("비류연|그렇네요.").Invented);
            Assert.AreEqual("비류연|그렇네요.", StoryLine.Clean("+비류연|그렇네요."));
        }

        /// <summary>Lists every line written for the game (not from the novel) so the user can review or remove them.</summary>
        [Test]
        public void ListInventedLines()
        {
            int count = 0;
            foreach (var st in EarlyGameContent.Stages)
                foreach (var raw in StoryEpisodes.Replay(st) ?? new string[0])
                    if (StoryLine.IsInvented(raw)) { count++; TestContext.Out.WriteLine(st.Number + ": " + raw.Substring(1)); }
            TestContext.Out.WriteLine(count + " invented lines");
            Assert.Pass();
        }

        [Test]
        public void StageStoriesNeverHaveEmptyLinesOrBadSpeakers()
        {
            foreach (var st in EarlyGameContent.Stages)
            {
                var all = (st.Intro ?? new string[0]).Concat(st.Outro ?? new string[0])
                    .Concat((st.Prologue ?? new StoryScene[0]).SelectMany(sc => sc.Lines));
                foreach (var raw in all)
                {
                    var line = StoryLine.Parse(raw);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(line.Text), st.Name + ": " + raw);
                    if (line.IsDialogue) Assert.LessOrEqual(line.Speaker.Length, 8, st.Name + ": " + raw);
                }
            }
        }
    }
}
