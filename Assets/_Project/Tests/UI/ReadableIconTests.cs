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

        [TestCase("cloth_robe", "equipment_repair", 1333, 3, 438, 437)]
        [TestCase("straw_sandals", "equipment_repair", 3, 446, 437, 438)]
        [TestCase("starfall_sword", "equipment_repair", 3, 3, 437, 437)]
        [TestCase("heaven_cutter", "equipment_repair", 446, 3, 438, 437)]
        [TestCase("gen_wea_myt01", "equipment_repair", 890, 3, 437, 437)]
        [TestCase("leather_vest", "items_0", 160, 771, 150, 142)]
        [TestCase("vajra_armor", "items_0", 1099, 919, 152, 147)]
        [TestCase("jade_ring", "items_0", 3, 1072, 113, 179)]
        [TestCase("cinnabar_amulet", "items_0", 1103, 1072, 148, 179)]
        [TestCase("gen_wea_nor01", "items_1", 3, 787, 151, 150)]
        [TestCase("gen_pan_nor03", "items_1", 943, 1085, 151, 166)]
        [TestCase("gen_acc_myt01", "elemental_equipment", 365, 727, 356, 356)]
        [TestCase("thunder_beads", "elemental_equipment", 3, 3, 356, 356)]
        [TestCase("blackiron_blade", "elemental_equipment", 3, 365, 356, 356)]
        [TestCase("heavenly_demon_core", "elemental_equipment", 727, 727, 356, 356)]
        [TestCase("biroe_blade", "items_0", 160, 154, 150, 151)]
        [TestCase("jin_biroedo", "jin_biroedo", 3, 3, 1248, 1248)]
        [TestCase("frost_sword", "items_0", 3, 614, 151, 151)]
        [TestCase("gen_acc_leg02", "items_3", 943, 943, 151, 151)]
        [TestCase("gen_acc_leg01", "items_3", 786, 943, 151, 151)]
        [TestCase("wind_boots", "items_0", 943, 1072, 154, 179)]
        [TestCase("wooden_charm", "items_0", 122, 1072, 119, 179)]
        [TestCase("mat_weapon_0", "items_3", 3, 1100, 202, 151)]
        [TestCase("gen_arm_nor07", "items_1", 3, 1085, 151, 166)]
        public void AuditedEquipmentRegionsStayWithinTheActualIllustrationCell(string id, string sheet, int x, int top, int width, int height)
        {
            var sprite = ReadableIconLibrary.Find("Item", id);
            CheckBounds(sprite, id);
            Assert.AreEqual(sheet, sprite.texture.name);
            Assert.AreEqual(x, sprite.rect.x, .1f);
            Assert.AreEqual(sprite.texture.height - top - height, sprite.rect.y, .1f);
            Assert.AreEqual(width, sprite.rect.width, .1f);
            Assert.AreEqual(height, sprite.rect.height, .1f);
        }

        [TestCase("burn", 3, 150)]
        [TestCase("chill", 159, 151)]
        [TestCase("stun", 316, 180)]
        [TestCase("poison", 502, 160)]
        [TestCase("innerwound", 668, 175)]
        [TestCase("speed", 849, 190)]
        [TestCase("attack", 1045, 206)]
        public void StatusIconsFollowTheirActualSevenColumnRow(string id, int left, int width)
        {
            var sprite = ArtLibrary.Icon(id);
            CheckBounds(sprite, id);
            Assert.AreEqual("items_4", sprite.texture.name);
            Assert.AreEqual(left, sprite.rect.x, .1f);
            Assert.AreEqual(1254 - 1099 - 152, sprite.rect.y, .1f);
            Assert.AreEqual(width, sprite.rect.width, .1f);
            Assert.AreEqual(152, sprite.rect.height, .1f);
        }

        [Test]
        public void ItemRegionsNeverIncludeAnotherItemsIllustration()
        {
            var sprites = new List<Sprite>();
            foreach (var item in EarlyGameContent.Items)
            {
                var sprite = ArtLibrary.Item(item);
                foreach (var previous in sprites)
                    if (sprite.texture == previous.texture)
                        Assert.IsFalse(sprite.rect.Overlaps(previous.rect), sprite.name + " overlaps " + previous.name);
                sprites.Add(sprite);
            }
        }

        [Test]
        public void ItemsAndMenuIconsUseOneTransparentMaterialWhileArtsAndPortraitsRemainUnchanged()
        {
            var go = new GameObject("CutoutIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            try
            {
                var image = go.GetComponent<Image>();
                foreach (var item in EarlyGameContent.Items)
                {
                    image.sprite = ArtLibrary.Item(item);
                    IconCutout.Apply(image);
                    Assert.AreSame(IconCutout.Material, image.material, item.Id);
                    Assert.IsFalse(image.material.HasProperty("_Background"));
                    Assert.IsTrue(image.material.HasProperty("_Stencil"));
                }
                image.sprite = ArtLibrary.Icon("companion"); IconCutout.Apply(image);
                Assert.AreSame(IconCutout.Material, image.material);
                foreach (var art in EarlyGameContent.Arts)
                {
                    image.sprite = ArtLibrary.Art(art.Id); IconCutout.Apply(image);
                    Assert.AreEqual(Graphic.defaultGraphicMaterial, image.material, art.Id);
                }
                image.sprite = ArtLibrary.InnerArt(InnerArts.All[0].Id); IconCutout.Apply(image);
                Assert.AreEqual(Graphic.defaultGraphicMaterial, image.material);
                image.sprite = null; IconCutout.Apply(image);
                Assert.AreEqual(Graphic.defaultGraphicMaterial, image.material);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void DifferentMenuFunctionsUseDifferentPaintedRegions()
        {
            var regions = new HashSet<string>();
            foreach (var key in new[] { "companion", "party", "faction", "tower", "codex", "record", "achievement", "star",
                "story", "quest", "memories", "fortune", "ritual", "arts", "missions", "gacha" })
            {
                var sprite = ArtLibrary.Icon(key);
                CheckBounds(sprite, key);
                Assert.IsTrue(regions.Add(sprite.texture.name + ":" + sprite.rect), key);
            }
        }

        [Test]
        public void MenuAndCommandIconsResolveAndUnknownItemUsesItsKindFallback()
        {
            foreach (var key in new[] { "cmd_attack", "cmd_guard", "cmd_meditate", "cmd_item", "cmd_joint", "ultimate_slot",
                "tab_world", "party", "equipment", "tab_arts", "tab_bag", "tab_record", "gacha", "quest", "daily",
                "fortune", "tower", "promotion", "star", "record", "settings", "coin",
                "companion", "faction", "codex", "achievement", "story", "memories", "ritual", "missions" })
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
