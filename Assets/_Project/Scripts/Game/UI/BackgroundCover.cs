using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Crops UVs around the centre to cover any viewport without stretching or drawing outside it.</summary>
    [RequireComponent(typeof(Image))]
    public sealed class BackgroundCover : BaseMeshEffect
    {
        public static void Apply(Image image)
        {
            image.preserveAspect = false;
            image.raycastTarget = false;
            if (image.GetComponent<BackgroundCover>() == null) image.gameObject.AddComponent<BackgroundCover>();
        }

        public static Vector2 UvScale(float sourceAspect, float viewportAspect)
        {
            if (sourceAspect <= 0f || viewportAspect <= 0f) return Vector2.one;
            return new Vector2(Mathf.Min(1f, viewportAspect / sourceAspect), Mathf.Min(1f, sourceAspect / viewportAspect));
        }

        public override void ModifyMesh(VertexHelper mesh)
        {
            var image = graphic as Image;
            if (!IsActive() || image == null || image.sprite == null) return;
            var rect = image.rectTransform.rect;
            if (rect.height <= 0f) return;
            var source = image.sprite.rect;
            var scale = UvScale(source.width / source.height, rect.width / rect.height);
            var center = new Vector2(source.center.x / image.sprite.texture.width, source.center.y / image.sprite.texture.height);
            var vertex = new UIVertex();
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                vertex.uv0 = center + Vector2.Scale((Vector2)vertex.uv0 - center, scale);
                mesh.SetUIVertex(vertex, i);
            }
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (graphic != null) graphic.SetVerticesDirty();
        }
    }
}
