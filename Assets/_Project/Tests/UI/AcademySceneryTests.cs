#if UNITY_EDITOR
using BiRyeoDo.Game.Systems;
using NUnit.Framework;
using UnityEngine;

namespace BiRyeoDo.Tests.UI
{
    public class AcademySceneryTests
    {
        [Test]
        public void CampusArtImportsWithoutResizingAtlasCoordinates()
        {
            var ground = AcademyScenery.Ground;
            Assert.IsNotNull(ground);
            Assert.AreEqual(1.5f, ground.rect.width / ground.rect.height, .001f);
            for (int i = 0; i < 3; i++)
            {
                var sprite = AcademyScenery.Facility(i);
                Assert.IsNotNull(sprite, "Facility atlas must retain source resolution.");
                Assert.AreEqual(0f, sprite.pivot.y);
                Assert.LessOrEqual(sprite.rect.xMax, sprite.texture.width);
                Assert.LessOrEqual(sprite.rect.yMax, sprite.texture.height);
                Assert.AreSame(sprite, AcademyScenery.Facility(i));
            }
            Assert.IsNull(AcademyScenery.Facility(-1));
            Assert.IsNull(AcademyScenery.Facility(3));
        }
    }
}
#endif
