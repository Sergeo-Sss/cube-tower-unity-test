using UnityEngine;

namespace CubeTower.Core.Geometry
{
    /// <summary>
    /// Axis-aligned ellipse used for precise hit testing of oval shapes (the hole).
    /// </summary>
    public readonly struct Ellipse
    {
        public Ellipse(Vector2 center, Vector2 radii)
        {
            Center = center;
            Radii = radii;
        }

        public Vector2 Center { get; }

        public Vector2 Radii { get; }

        public bool IsValid => Radii.x > 0f && Radii.y > 0f;

        public static Ellipse FromRect(Rect rect) => new Ellipse(rect.center, rect.size * 0.5f);

        public bool Contains(Vector2 point)
        {
            if (!IsValid)
                return false;

            var nx = (point.x - Center.x) / Radii.x;
            var ny = (point.y - Center.y) / Radii.y;
            return nx * nx + ny * ny <= 1f;
        }
    }
}
