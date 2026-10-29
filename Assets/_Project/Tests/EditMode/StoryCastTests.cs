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
            foreach (var c in EarlyGameContent.Companions) names.Add(c.Name);
            // A story-only character may later appear as an enemy too (청흔 → 「삼절검 청흔」); sharing the picture is fine
            // only when that enemy is the same person, i.e. its name contains the cast name.
            var enemyNames = new Dictionary<string, List<string>>();
            foreach (var e in EarlyGameContent.Enemies)
            {
                if (!enemyNames.ContainsKey(e.ArtKey)) enemyNames[e.ArtKey] = new List<string>();
                enemyNames[e.ArtKey].Add(e.Name);
            }
            foreach (var e in StoryCast.All)
            {
                Assert.IsTrue(names.Add(e.Name), "duplicate name " + e.Name);
                Assert.IsTrue(ids.Add(e.Id), "duplicate art id " + e.Id);
                List<string> sharing;
                if (enemyNames.TryGetValue(e.Id, out sharing))
                    foreach (var enemyName in sharing)
                        Assert.IsTrue(enemyName.Contains(e.Name), "art id " + e.Id + " of " + e.Name + " is used by another enemy " + enemyName);
                Assert.LessOrEqual(e.Name.Length, 8);
            }
        }
    }
}
