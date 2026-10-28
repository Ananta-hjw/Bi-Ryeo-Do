using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using BiRyeoDo.Core;

namespace BiRyeoDo.Tests
{
    public class StoryCastTests
    {
        [Test]
        public void NamesAndIdsAreUniqueAndDoNotClashWithOtherCharacters()
        {
            var names = new HashSet<string>();
            var ids = new HashSet<string>();
            var storyIds = new HashSet<string>();
            foreach (var c in EarlyGameContent.Companions) ids.Add(c.Id);
            foreach (var e in EarlyGameContent.Enemies) ids.Add(e.ArtKey);
            foreach (var c in EarlyGameContent.Companions) names.Add(c.Name);
            foreach (var e in StoryCast.All)
            {
                Assert.IsTrue(names.Add(e.Name), "duplicate name " + e.Name);
                Assert.IsTrue(storyIds.Add(e.Id), "duplicate story art id " + e.Id);
                if (!ids.Add(e.Id))
                    Assert.IsTrue(EarlyGameContent.Enemies.Any(enemy => enemy.ArtKey == e.Id && enemy.Name.Contains(e.Name)),
                        "A shared art key must depict the same person: " + e.Id);
                Assert.LessOrEqual(e.Name.Length, 8);
            }
        }
    }
}
