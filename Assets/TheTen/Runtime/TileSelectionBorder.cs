using UnityEngine;
using UnityEngine.UI;

namespace TheTen
{
    // An untextured ring keeps the highlight equally bright on every tile pattern.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TileSelectionBorder : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * .20f;
            var ink = JewelTheme.Ink;
            var gold = new Color32(255, 221, 94, 255);
            AddRing(mesh, rect, radius, 1f, 2.5f, ink);
            AddRing(mesh, rect, radius, 2.5f, 7.5f, gold);
            AddRing(mesh, rect, radius, 7.5f, 9f, ink);
        }

        static void AddRing(VertexHelper mesh, Rect rect, float radius, float outerInset, float innerInset, Color32 tint)
        {
            const int steps = 8;
            int first = mesh.currentVertCount;
            for (int corner = 0; corner < 4; corner++)
            {
                var center = new Vector2(corner < 2 ? rect.xMax - radius : rect.xMin + radius,
                    corner == 0 || corner == 3 ? rect.yMin + radius : rect.yMax - radius);
                for (int i = 0; i <= steps; i++)
                {
                    float angle = (corner * 90 - 90 + 90f * i / steps) * Mathf.Deg2Rad;
                    var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    mesh.AddVert(center + direction * (radius - outerInset), tint, Vector2.zero);
                    mesh.AddVert(center + direction * (radius - innerInset), tint, Vector2.zero);
                }
            }
            int end = mesh.currentVertCount;
            for (int outer = first; outer < end; outer += 2)
            {
                int next = outer + 2 < end ? outer + 2 : first;
                mesh.AddTriangle(outer, outer + 1, next);
                mesh.AddTriangle(next, outer + 1, next + 1);
            }
        }
    }
}
