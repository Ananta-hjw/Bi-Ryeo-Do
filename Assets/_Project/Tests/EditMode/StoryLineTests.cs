using System.Linq;
using BiRyeoDo.Core;
using NUnit.Framework;

namespace BiRyeoDo.Tests
{
    public class StoryLineTests
    {
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
