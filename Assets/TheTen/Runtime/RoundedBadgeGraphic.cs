using UnityEngine;
using UnityEngine.UI;

namespace TheTen
{
    // A pill mesh keeps its rounded ends at any canvas scale without an extra texture.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class RoundedBadgeGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = GetPixelAdjustedRect();
            AddPill(vh, rect, Color.Lerp(color, new Color32(18, 48, 39, 255), .65f));
            rect = Inset(rect, 2);
            AddPill(vh, rect, Color.Lerp(color, Color.white, .4f));
            rect = Inset(rect, 2); AddPill(vh, rect, color);
        }
        static Rect Inset(Rect rect, float amount) => new Rect(rect.x + amount, rect.y + amount,
            Mathf.Max(0, rect.width - 2 * amount), Mathf.Max(0, rect.height - 2 * amount));
        static void AddPill(VertexHelper vh, Rect rect, Color color)
        {
            const int steps = 8;
            float radius = Mathf.Min(rect.width, rect.height) / 2;
            int start = vh.currentVertCount;
            vh.AddVert(rect.center, color, Vector2.zero);
            for (int corner = 0; corner < 4; corner++)
            {
                var center = new Vector2(corner < 2 ? rect.xMax - radius : rect.xMin + radius,
                    corner == 0 || corner == 3 ? rect.yMin + radius : rect.yMax - radius);
                for (int i = 0; i <= steps; i++)
                {
                    float angle = (-90 + corner * 90 + i * 90f / steps) * Mathf.Deg2Rad;
                    vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
                }
            }
            int count = 4 * (steps + 1);
            for (int i = 0; i < count; i++) vh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % count);
        }
    }
}
