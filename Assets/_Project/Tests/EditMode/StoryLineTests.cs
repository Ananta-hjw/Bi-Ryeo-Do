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
