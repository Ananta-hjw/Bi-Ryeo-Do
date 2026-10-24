using BiRyeoDo.Game.Systems;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

namespace BiRyeoDo.Game.UI
{
    /// <summary>Painted semantic menu icons with vector fallbacks for frames, floor markers and ornaments.</summary>
    public sealed class WuxiaGlyph : MaskableGraphic
    {
        [SerializeField, FormerlySerializedAs("Key")]
        private string _key = "stage";
        public string Key
        {
            get { return _key; }
            set
            {
                value = value ?? "";
                if (_key == value) return;
                _key = value;
                _iconKey = null; _icon = null;
                SetAllDirty();
            }
        }
        public float Stroke = 2f;
        private Rect _bounds;
        private float _size;

        private string _iconKey;
        private Sprite _icon;

        public override Material defaultMaterial
        {
            get { ResolveIcon(); return _icon != null ? IconCutout.Material ?? base.defaultMaterial : base.defaultMaterial; }
        }

        public override Texture mainTexture
        {
            get { ResolveIcon(); return _icon != null ? _icon.texture : base.mainTexture; }
        }

        private void ResolveIcon()
        {
            if (_iconKey == Key) return;
            _iconKey = Key;
            _icon = ArtLibrary.Icon(Key);
        }

        private void DrawIcon(VertexHelper mesh)
        {
            var source = _icon.rect;
            float scale = Mathf.Min(_bounds.width / source.width, _bounds.height / source.height);
            var half = source.size * scale * .5f;
            var center = _bounds.center;
            float left = source.xMin / _icon.texture.width, right = source.xMax / _icon.texture.width;
            float bottom = source.yMin / _icon.texture.height, top = source.yMax / _icon.texture.height;
            // Keep the original painted colours; the existing alpha still controls visibility.
            var tint = new Color(1f, 1f, 1f, color.a);
            mesh.AddVert(center - half, tint, new Vector2(left, bottom));
            mesh.AddVert(center + new Vector2(-half.x, half.y), tint, new Vector2(left, top));
            mesh.AddVert(center + half, tint, new Vector2(right, top));
            mesh.AddVert(center + new Vector2(half.x, -half.y), tint, new Vector2(right, bottom));
            mesh.AddTriangle(0, 1, 2); mesh.AddTriangle(2, 3, 0);
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            _bounds = rectTransform.rect;
            _size = Mathf.Min(_bounds.width, _bounds.height);
            if (_size <= 0f) return;
            ResolveIcon();
            if (_icon != null) { DrawIcon(mesh); return; }
            if (Key == "frame") { Frame(mesh); return; }
            if (Key == "stance")
            {
                for (int i = 0; i < 48; i++)
                {
                    float a = i * Mathf.PI / 24f, b = (i + 1) * Mathf.PI / 24f;
                    var radius = new Vector2(_bounds.width * .46f, _bounds.height * .4f);
                    Segment(mesh, _bounds.center + new Vector2(Mathf.Cos(a) * radius.x, Mathf.Sin(a) * radius.y),
                        _bounds.center + new Vector2(Mathf.Cos(b) * radius.x, Mathf.Sin(b) * radius.y), Stroke, color);
                }
                return;
            }
            string key = Key.Replace("tab_", "");
            switch (key)
            {
                case "icon_rim":
                    Ring(mesh, .5f, .5f, .48f, 64);
                    break;
                case "back":   // 돌아가기: a thin U-turn arrow pointing left
                    Line(mesh, .14f, .66f, .62f, .66f);
                    Line(mesh, .14f, .66f, .32f, .84f); Line(mesh, .14f, .66f, .32f, .48f);
                    for (int i = 0; i < 16; i++)
                    {
                        float a = Mathf.PI * .5f - i * Mathf.PI / 16f, b = Mathf.PI * .5f - (i + 1) * Mathf.PI / 16f;
                        Line(mesh, .62f + Mathf.Cos(a) * .2f, .46f + Mathf.Sin(a) * .2f, .62f + Mathf.Cos(b) * .2f, .46f + Mathf.Sin(b) * .2f);
                    }
                    Line(mesh, .62f, .26f, .4f, .26f);
                    break;
                case "close":
                    Line(mesh, .28f, .28f, .72f, .72f); Line(mesh, .28f, .72f, .72f, .28f);
                    break;
                case "guard":
                    Path(mesh,.5f,.92f,.83f,.78f,.77f,.35f,.5f,.09f,.23f,.35f,.17f,.78f,.5f,.92f);
                    Line(mesh,.5f,.76f,.5f,.27f); Line(mesh,.34f,.57f,.66f,.57f);
                    break;
                case "lightning":
                    Path(mesh,.59f,.93f,.19f,.43f,.47f,.43f,.35f,.07f,.83f,.61f,.55f,.61f,.59f,.93f);
                    break;
                case "halo":
                    Ring(mesh, .5f, .5f, .46f, 64); Ring(mesh, .5f, .5f, .41f, 64);
                    for (int i = 0; i < 12; i++)
                    {
                        float a = i * Mathf.PI / 6f;
                        Line(mesh, .5f + Mathf.Cos(a) * .43f, .5f + Mathf.Sin(a) * .43f, .5f + Mathf.Cos(a) * .46f, .5f + Mathf.Sin(a) * .46f);
                    }
                    break;
                case "world": case "stage":
                    Path(mesh, .1f,.25f, .37f,.73f, .56f,.4f, .7f,.61f, .91f,.25f);
                    Path(mesh, .28f,.25f, .45f,.51f, .58f,.25f); Line(mesh,.12f,.16f,.88f,.16f);
                    break;
                case "arts": case "promotion":
                    Path(mesh, .34f,.3f, .55f,.82f, .69f,.93f, .7f,.74f, .46f,.26f);
                    Line(mesh,.23f,.36f,.59f,.2f); Line(mesh,.4f,.26f,.31f,.08f); Line(mesh,.23f,.13f,.37f,.06f);
                    break;
                case "tower":
                    for (int i = 0; i < 3; i++)
                    {
                        float y = .25f + i * .24f, inset = i * .09f;
                        Path(mesh,.12f+inset,y,.5f,y+.18f,.88f-inset,y);
                        Line(mesh,.26f+inset,y,.26f+inset,y-.13f); Line(mesh,.74f-inset,y,.74f-inset,y-.13f);
                    }
                    Line(mesh,.15f,.08f,.85f,.08f);
                    break;
                case "party":
                    Ring(mesh,.5f,.67f,.13f); Ring(mesh,.19f,.6f,.09f); Ring(mesh,.81f,.6f,.09f);
                    Path(mesh,.27f,.15f,.3f,.4f,.5f,.48f,.7f,.4f,.73f,.15f);
                    Path(mesh,.06f,.2f,.08f,.37f,.2f,.42f); Path(mesh,.8f,.42f,.92f,.37f,.94f,.2f);
                    break;
                case "equipment":
                    Path(mesh,.2f,.85f,.38f,.94f,.43f,.80f,.57f,.80f,.62f,.94f,.8f,.85f,
                        .93f,.64f,.77f,.52f,.7f,.66f,.7f,.12f,.3f,.12f,.3f,.66f,.23f,.52f,.07f,.64f,.2f,.85f);
                    Line(mesh,.32f,.38f,.68f,.38f); Line(mesh,.5f,.77f,.5f,.15f);
                    break;
                case "bag":
                    Path(mesh,.3f,.8f,.7f,.8f,.63f,.66f,.82f,.33f,.75f,.14f,.25f,.14f,.18f,.33f,.37f,.66f,.3f,.8f);
                    Line(mesh,.3f,.64f,.7f,.64f); Ring(mesh,.5f,.37f,.1f);
                    break;
                case "gacha": case "fortune":
                    Path(mesh,.5f,.12f,.24f,.36f,.16f,.65f,.4f,.52f,.5f,.88f,.6f,.52f,.84f,.65f,.76f,.36f,.5f,.12f);
                    Line(mesh,.5f,.2f,.5f,.65f); Line(mesh,.28f,.38f,.5f,.2f); Line(mesh,.72f,.38f,.5f,.2f);
                    break;
                case "star": case "coin":
                    Ring(mesh,.5f,.5f,.36f); Ring(mesh,.5f,.5f,.28f);
                    Path(mesh,.4f,.4f,.4f,.6f,.6f,.6f,.6f,.4f,.4f,.4f);
                    break;
                case "settings":   // a cog: eight square teeth around the rim and a hub
                    {
                        const int teeth = 8;
                        float inner = .30f, outer = .42f, half = Mathf.PI / teeth * .45f;
                        var pts = new System.Collections.Generic.List<float>();
                        for (int i = 0; i < teeth; i++)
                        {
                            float c = i * Mathf.PI * 2f / teeth;
                            foreach (var (r, a) in new[] { (inner, c - Mathf.PI / teeth + half), (inner, c - half), (outer, c - half * .8f), (outer, c + half * .8f), (inner, c + half) })
                            { pts.Add(.5f + Mathf.Cos(a) * r); pts.Add(.5f + Mathf.Sin(a) * r); }
                        }
                        pts.Add(pts[0]); pts.Add(pts[1]);
                        Path(mesh, pts.ToArray());
                        Ring(mesh, .5f, .5f, .13f, 32);
                    }
                    break;
                default: // scroll: records, daily missions and commissions
                    Path(mesh,.23f,.15f,.23f,.86f,.8f,.86f,.8f,.2f,.23f,.2f);
                    Line(mesh,.17f,.12f,.3f,.12f); Line(mesh,.14f,.88f,.31f,.88f);
                    for (int i = 0; i < 3; i++) Line(mesh,.36f,.66f-i*.16f,.67f,.66f-i*.16f);
                    break;
            }
        }

        private Vector2 Point(float x, float y) { return _bounds.center + new Vector2(x - .5f, y - .5f) * _size; }
        private void Path(VertexHelper mesh, params float[] points)
        {
            for (int i = 0; i + 3 < points.Length; i += 2) Line(mesh, points[i], points[i+1], points[i+2], points[i+3]);
        }
        private void Ring(VertexHelper mesh, float x, float y, float radius, int segments = 24)
        {
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments, b = (i+1) * Mathf.PI * 2f / segments;
                Line(mesh,x+Mathf.Cos(a)*radius,y+Mathf.Sin(a)*radius,x+Mathf.Cos(b)*radius,y+Mathf.Sin(b)*radius);
            }
        }
        private void Line(VertexHelper mesh, float x0, float y0, float x1, float y1)
        {
            Segment(mesh, Point(x0,y0), Point(x1,y1), Mathf.Max(1.5f, _size * .025f), color);
        }
        private void Frame(VertexHelper mesh)
        {
            float left = _bounds.xMin + Stroke, right = _bounds.xMax - Stroke;
            float bottom = _bounds.yMin + Stroke, top = _bounds.yMax - Stroke;
            float cut = Mathf.Min(20f, _size * .16f);
            var dim = color; dim.a *= .35f;
            Segment(mesh,new Vector2(left,top),new Vector2(right,top),Stroke*.5f,dim);
            Segment(mesh,new Vector2(left,bottom),new Vector2(right,bottom),Stroke*.5f,dim);
            foreach (float x in new[] { left, right }) foreach (float y in new[] { bottom, top })
            {
                Segment(mesh,new Vector2(x,y),new Vector2(x+(x==left?cut:-cut),y),Stroke,color);
                Segment(mesh,new Vector2(x,y),new Vector2(x,y+(y==bottom?cut:-cut)),Stroke,color);
            }
        }
        private static void Segment(VertexHelper mesh, Vector2 start, Vector2 end, float width, Color tint)
        {
            var direction = end - start;
            if (direction.sqrMagnitude < .001f) return;
            var normal = new Vector2(-direction.y, direction.x).normalized * width * .5f;
            int n = mesh.currentVertCount;
            mesh.AddVert(start-normal, tint, Vector2.zero); mesh.AddVert(start+normal, tint, Vector2.zero);
            mesh.AddVert(end+normal, tint, Vector2.zero); mesh.AddVert(end-normal, tint, Vector2.zero);
            mesh.AddTriangle(n,n+1,n+2); mesh.AddTriangle(n,n+2,n+3);
        }
    }
}
