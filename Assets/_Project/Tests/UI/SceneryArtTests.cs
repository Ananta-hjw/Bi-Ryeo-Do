#if UNITY_EDITOR
using System.Linq;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using BiRyeoDo.Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BiRyeoDo.Tests.UI
{
    public class SceneryArtTests
    {
        [Test]
        public void EveryStoryStageHasOneOfSevenAvailableEnvironmentsAndMenusResolve()
        {
            var keys = EarlyGameContent.Stages.Select(stage => SceneryArt.StageKey(stage.Number)).Distinct().ToArray();
            Assert.AreEqual(7, keys.Length);
            foreach (var key in keys) Assert.IsNotNull(ArtLibrary.Environment(key), key);
            foreach (var stage in EarlyGameContent.Stages) Assert.AreSame(ArtLibrary.Environment(SceneryArt.StageKey(stage.Number)), SceneryArt.Battle(stage));
            foreach (var key in new[] { "world", "companions", "equipment", "arts", "gacha", "bag", "record" }) Assert.IsNotNull(SceneryArt.Menu(key), key);
            Assert.IsNull(Resources.Load<Texture2D>("Art/Backgrounds/stage_01"));
        }

        [Test]
        public void EveryJourneyPageLoadsADistinctMapPainting()
        {
            var paintings = Enumerable.Range(0, 6).Select(SceneryArt.Journey).ToArray();
            foreach (var painting in paintings) Assert.IsNotNull(painting);
            Assert.AreEqual(6, paintings.Distinct().Count());
            Assert.AreEqual(SceneryArt.JourneyKey(0), SceneryArt.JourneyKey(-1));
            Assert.AreEqual(SceneryArt.JourneyKey(5), SceneryArt.JourneyKey(6));
        }

        [TestCase(1, "battle_forest")]
        [TestCase(2, "battle_manor")]
        [TestCase(7, "battle_cave")]
        [TestCase(9, "battle_pavilion")]
        [TestCase(10, "battle_pavilion")]
        [TestCase(12, "battle_academy")]
        [TestCase(19, "battle_cave")]
        [TestCase(20, "battle_cave")]
        [TestCase(22, "battle_valley")]
        [TestCase(30, "battle_valley")]
        public void StorySettingsChooseAppropriateThemes(int stage, string expected)
        {
            Assert.AreEqual(expected, SceneryArt.StageKey(stage));
        }

        [TestCase(16f / 9f)]
        [TestCase(20f / 9f)]
        [TestCase(4f / 3f)]
        public void CoverCroppingKeepsTheSceneUndistortedInsideItsOriginalQuad(float viewportAspect)
        {
            var go = new GameObject("CoverTest", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var texture = new Texture2D(160, 90);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 160, 90), Vector2.one * .5f);
            try
            {
                var image = go.GetComponent<Image>(); image.sprite = sprite;
                image.rectTransform.sizeDelta = new Vector2(900f * viewportAspect, 900f);
                BackgroundCover.Apply(image);
                var scale = BackgroundCover.UvScale(160f / 90f, viewportAspect);
                Assert.AreEqual(viewportAspect, (160f * scale.x) / (90f * scale.y), .001f);
                Assert.AreEqual(1f, Mathf.Max(scale.x, scale.y), .001f);
                Assert.Greater(scale.x, 0f); Assert.Greater(scale.y, 0f);
                Assert.IsFalse(image.raycastTarget);
                using (var mesh = new VertexHelper())
                {
                    mesh.AddVert(Vector3.zero, Color.white, Vector2.zero);
                    mesh.AddVert(Vector3.up, Color.white, Vector2.up);
                    mesh.AddVert(Vector3.one, Color.white, Vector2.one);
                    mesh.AddVert(Vector3.right, Color.white, Vector2.right);
                    go.GetComponent<BackgroundCover>().ModifyMesh(mesh);
                    var vertex = new UIVertex();
                    mesh.PopulateUIVertex(ref vertex, 0);
                    Assert.AreEqual(Vector3.zero, vertex.position);
                    Assert.AreEqual((1f - scale.x) * .5f, vertex.uv0.x, .001f);
                    Assert.AreEqual((1f - scale.y) * .5f, vertex.uv0.y, .001f);
                }
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(sprite); Object.DestroyImmediate(texture); }
        }
    }
}
#endif
