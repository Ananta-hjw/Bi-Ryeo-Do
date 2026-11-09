#if UNITY_EDITOR
using BiRyeoDo.Game.Systems;
using NUnit.Framework;
using UnityEngine;

namespace BiRyeoDo.Tests.UI
{
    public class ExplorationSpritesTests
    {
        [Test]
        public void PlayerAtlasImportsAtSourceResolutionAndProvidesAllPoses()
        {
            var frames = ExplorationSprites.Player();
            Assert.IsNotNull(frames, "Atlas texture and metadata must import together without resizing.");
            Assert.AreEqual(16, frames.Length);
            foreach (var sprite in frames)
            {
                Assert.IsNotNull(sprite);
                Assert.Greater(sprite.rect.width, 0f);
                Assert.Greater(sprite.rect.height, 0f);
                Assert.AreEqual(0f, sprite.pivot.y);
                Assert.LessOrEqual(sprite.rect.xMax, sprite.texture.width);
                Assert.LessOrEqual(sprite.rect.yMax, sprite.texture.height);
            }
            Assert.AreSame(frames, ExplorationSprites.Player(), "Reopening the campus must reuse cached sprites.");
        }
    }
}
#endif
