using UnityEngine;
using UnityEngine.UI;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Resolution-independent curved ink roads; unreached destinations use a broken trail.</summary>
    public sealed class StageMapRoute : MaskableGraphic
    {
        private Vector2[] _points = new Vector2[0];
        private Color[] _colors = new Color[0];
        private bool[] _open = new bool[0];

        public void SetRoute(Vector2[] points, Color[] colors, bool[] open)
        {
            _points = points;
            _colors = colors;
            _open = open;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var bounds = rectTransform.rect;
            for (int i = 1; i < _points.Length; i++)
            {
                Vector2 a = Point(bounds, _points[i - 1]), b = Point(bounds, _points[i]);
                Vector2 delta = b - a;
                Vector2 tangent = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                    ? new Vector2(delta.x * .6f, 0) : new Vector2(0, delta.y * .6f);
                Vector2 previous = a;
                const int steps = 64;
                for (int step = 1; step <= steps; step++)
                {
                    float t = step / (float)steps, u = 1 - t;
                    Vector2 next = u * u * u * a + 3 * u * u * t * (a + tangent)
                        + 3 * u * t * t * (b - tangent) + t * t * t * b;
                    if (_open[i - 1] || step % 6 < 3)
                    {
                        Segment(mesh, previous, next, 12f, new Color(.13f, .16f, .12f, .32f));
                        Segment(mesh, previous, next, 5f, _colors[i - 1]);
                    }
                    previous = next;
                }
            }
        }

        private static Vector2 Point(Rect bounds, Vector2 value)
        {
            return bounds.min + Vector2.Scale(bounds.size, value);
        }

        private static void Segment(VertexHelper mesh, Vector2 a, Vector2 b, float width, Color color)
        {
            var normal = new Vector2(-(b - a).y, (b - a).x).normalized * width * .5f;
            int first = mesh.currentVertCount;
            mesh.AddVert(a - normal, color, Vector2.zero);
            mesh.AddVert(a + normal, color, Vector2.zero);
            mesh.AddVert(b + normal, color, Vector2.zero);
            mesh.AddVert(b - normal, color, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first + 2, first + 3, first);
        }
    }
}
