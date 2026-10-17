using System.Collections.Generic;
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
            foreach (var c in EarlyGameContent.Companions) ids.Add(c.Id);
            foreach (var e in EarlyGameContent.Enemies) ids.Add(e.ArtKey);
            foreach (var c in EarlyGameContent.Companions) names.Add(c.Name);
            foreach (var e in StoryCast.All)
            {
                Assert.IsTrue(names.Add(e.Name), "duplicate name " + e.Name);
                Assert.IsTrue(ids.Add(e.Id), "duplicate art id " + e.Id);
                Assert.LessOrEqual(e.Name.Length, 8);
            }
        }
    }
}
