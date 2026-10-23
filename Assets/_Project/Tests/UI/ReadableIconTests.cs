#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using BiRyeoDo.Core;
using BiRyeoDo.Game.Systems;
using BiRyeoDo.Game.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BiRyeoDo.Tests.UI
{
    public class ReadableIconTests
    {
        private static void CheckBounds(Sprite sprite, string id)
        {
            Assert.IsNotNull(sprite, id);
            Assert.Greater(sprite.rect.width, 100f, id);
            Assert.Greater(sprite.rect.height, 100f, id);
            Assert.GreaterOrEqual(sprite.rect.xMin, 0f, id);
            Assert.GreaterOrEqual(sprite.rect.yMin, 0f, id);
            Assert.LessOrEqual(sprite.rect.xMax, sprite.texture.width, id);
            Assert.LessOrEqual(sprite.rect.yMax, sprite.texture.height, id);
        }

        [Test]
        public void EveryItemHasItsOwnMappedPictureAndEveryArtAndInnerMethodIsCovered()
        {
            var locations = new HashSet<string>();
            foreach (var item in EarlyGameContent.Items)
            {
                var sprite = ReadableIconLibrary.Find("Item", item.Id);
                CheckBounds(sprite, item.Id);
                Assert.AreSame(sprite, ArtLibrary.Item(item));
                Assert.IsTrue(locations.Add(sprite.texture.GetInstanceID() + ":" + sprite.rect), "Reused item picture: " + item.Id);
            }
            foreach (var art in EarlyGameContent.Arts)
            {
                var sprite = ReadableIconLibrary.Find("Art", art.Id);
                CheckBounds(sprite, art.Id);
                Assert.AreSame(sprite, ArtLibrary.Art(art.Id));
            }
            foreach (var method in InnerArts.All)
            {
                var sprite = ReadableIconLibrary.Find("Inner", method.Id);
                CheckBounds(sprite, method.Id);
                Assert.AreSame(sprite, ArtLibrary.InnerArt(method.Id));
            }
        }

        [Test]
        public void MenuAndCommandIconsResolveAndUnknownItemUsesItsKindFallback()
        {
            foreach (var key in new[] { "cmd_attack", "cmd_guard", "cmd_meditate", "cmd_item", "cmd_joint", "ultimate_slot",
                "tab_world", "party", "equipment", "tab_arts", "tab_bag", "tab_record", "gacha", "quest", "daily",
                "fortune", "tower", "promotion", "star", "record", "settings", "coin" })
                CheckBounds(ArtLibrary.Icon(key), key);
            Assert.IsNull(ReadableIconLibrary.Find("Item", "unknown_icon_id"));
            var item = new ItemData { Id = "unknown_icon_id", Name = "Test weapon", Kind = ItemKind.Weapon };
            Assert.AreSame(ReadableIconLibrary.Find("DefaultItem", "Weapon"), ArtLibrary.Item(item));
            Assert.IsNull(ArtLibrary.Item(null));
        }

        [Test]
        public void DynamicGlyphChangesTextureAndKeepsVectorDecorationsAndAlpha()
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(WuxiaGlyph));
            try
            {
                go.GetComponent<RectTransform>().sizeDelta = new Vector2(112f, 112f);
                var glyph = go.GetComponent<WuxiaGlyph>();
                var populate = typeof(WuxiaGlyph).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic);
                glyph.color = new Color(.9f, .7f, .3f, .6f);
                foreach (var key in new[] { "settings", "party" })
                {
                    glyph.Key = key;
                    using (var vertices = new VertexHelper())
                    {
                        populate.Invoke(glyph, new object[] { vertices });
                        Assert.AreEqual(4, vertices.currentVertCount);
                        Assert.AreSame(ArtLibrary.Icon(key).texture, glyph.mainTexture);
                        var vertex = new UIVertex(); vertices.PopulateUIVertex(ref vertex, 0);
                        Assert.AreEqual(255, vertex.color.r);
                        Assert.That(vertex.color.a, Is.EqualTo(153).Within(1));
                    }
                }
                glyph.Key = "frame";
                using (var vertices = new VertexHelper())
                {
                    populate.Invoke(glyph, new object[] { vertices });
                    Assert.Greater(vertices.currentVertCount, 4);
                }
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
#endif
