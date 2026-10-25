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

        [TestCase("world")]
        [TestCase("companions")]
        [TestCase("equipment")]
        [TestCase("arts")]
        [TestCase("gacha")]
        [TestCase("bag")]
        [TestCase("record")]
        public void MenuPaintingsAndGoldFramesLeaveInputAndContentUnobstructed(string key)
        {
            var go = new GameObject("MenuTest", typeof(RectTransform));
            try
            {
                var root = go.GetComponent<RectTransform>();
                UiFactory.MenuBackdrop(root, key);
                UiFactory.Frame(root, UiFactory.Gold);
                var art = root.Find("MenuScenery").GetComponent<Image>();
                Assert.AreSame(SceneryArt.Menu(key), art.sprite);
                Assert.IsNotNull(art.GetComponent<BackgroundCover>());
                foreach (var graphic in root.GetComponentsInChildren<Graphic>()) Assert.IsFalse(graphic.raycastTarget);
                Assert.IsEmpty(root.GetComponentsInChildren<Button>());
                Assert.Greater(UiFactory.Ink.a, .9f, "Text panels must remain readable above paintings");
                Assert.Less(root.Find("MenuWash").GetComponent<Image>().color.a, .3f);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [TestCase("ShopOverlay", "companions")]
        [TestCase("CodexOverlay", "record")]
        [TestCase("DailyOverlay", "arts")]
        [TestCase("PartyOverlay", "world")]
        public void SecondaryMenusResolveTheSamePaintingFamily(string name, string expected)
        {
            Assert.AreEqual(expected, SceneryArt.PanelKey(name));
            Assert.IsNotNull(SceneryArt.Menu(SceneryArt.PanelKey(name)));
        }

        [Test]
        public void PresentationPortraitsPreferStandingAndFallBackToCharacters()
        {
            Assert.AreSame(ArtLibrary.StandingPortrait(StoryLine.Protagonist), ArtLibrary.CharacterPortrait(StoryLine.Protagonist));
            foreach (var companion in EarlyGameContent.Companions)
            {
                Assert.AreSame(ArtLibrary.StandingPortrait(companion.Name), ArtLibrary.CharacterPortrait(companion.Name));
                Assert.AreSame(ArtLibrary.CharacterPortrait(companion.Name), ArtLibrary.CharacterPortrait(companion.Id));
            }
            // A synthetic legacy-only asset keeps the fallback test valid as the real catalog fills up.
            const string key = "legacy_only_portrait_test";
            var cache = (System.Collections.Generic.Dictionary<string, Sprite>)typeof(ArtLibrary)
                .GetField("Cache", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
            var texture = new Texture2D(2, 2);
            var legacy = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * .5f);
            cache["Characters/" + key] = legacy;
            cache["Standing/" + key] = null;
            try { Assert.AreSame(legacy, ArtLibrary.CharacterPortrait(key)); }
            finally
            {
                cache.Remove("Characters/" + key); cache.Remove("Standing/" + key);
                Object.DestroyImmediate(legacy); Object.DestroyImmediate(texture);
            }
            Assert.IsNull(ArtLibrary.CharacterPortrait(null));
            Assert.IsNull(ArtLibrary.CharacterPortrait("missing_portrait_for_test"));
            Assert.IsNotNull(ArtLibrary.SdFigure("missing_sd_for_test"));
            Assert.AreNotSame(ArtLibrary.CharacterPortrait(StoryLine.Protagonist), ArtLibrary.SdFigure(Inventory.Hero));
        }

        [TestCase("염도", "yeomdo")]
        [TestCase("빙검", "gwan_cheolsu")]
        [TestCase("구창", "gu_chang")]
        [TestCase("검후", "geomhu_ioksang")]
        public void ShortDialogueNamesResolveTheNewStandingOriginal(string speaker, string id)
        {
            Assert.IsNotNull(ArtLibrary.StandingPortrait(id));
            Assert.AreSame(ArtLibrary.StandingPortrait(id), ArtLibrary.DialoguePortrait(speaker));
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
