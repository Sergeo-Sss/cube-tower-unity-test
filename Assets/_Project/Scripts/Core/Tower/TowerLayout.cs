using System.Collections.Generic;
using UnityEngine;

namespace CubeTower.Core.Tower
{
    /// <summary>
    /// Converts resolution independent <see cref="TowerModel"/> state into area coordinates and back.
    /// </summary>
    public static class TowerLayout
    {
        // Tolerance for float rounding of layout values.
        private const float HeightEpsilon = 0.01f;

        /// <summary>
        /// Calculates centres of all cubes (area coordinates, origin at bottom-left).
        /// If the tower doesn't fit horizontally (e.g. the saved tower is restored on a narrower screen)
        /// the whole tower is shifted uniformly to stay inside the area.
        /// </summary>
        public static void CalculateCenters(TowerModel tower, TowerArea area, List<Vector2> centers)
        {
            centers.Clear();
            if (tower.IsEmpty || !area.IsValid)
                return;

            var size = area.CubeSize;
            var half = size * 0.5f;
            var x = area.Size.x * 0.5f + tower.BasePosition.x * size;
            var y = tower.BasePosition.y * size + half;
            var minX = float.MaxValue;
            var maxX = float.MinValue;

            var cubes = tower.Cubes;
            for (var i = 0; i < cubes.Count; i++)
            {
                if (i > 0)
                {
                    x += cubes[i].Offset * size;
                    y += size;
                }

                centers.Add(new Vector2(x, y));
                minX = Mathf.Min(minX, x - half);
                maxX = Mathf.Max(maxX, x + half);
            }

            var shift = CalculateFitShift(minX, maxX, area.Size.x);
            if (Mathf.Approximately(shift, 0f))
                return;

            for (var i = 0; i < centers.Count; i++)
                centers[i] += new Vector2(shift, 0f);
        }

        /// <summary>
        /// Converts a desired centre of the base cube into model coordinates, clamping it into the area.
        /// </summary>
        public static Vector2 ToBasePosition(Vector2 cubeCenter, TowerArea area)
        {
            var size = area.CubeSize;
            var half = size * 0.5f;
            var centerX = Mathf.Clamp(cubeCenter.x, half, Mathf.Max(half, area.Size.x - half));
            var bottom = Mathf.Clamp(cubeCenter.y - half, 0f, Mathf.Max(0f, area.Size.y - size));
            return new Vector2((centerX - area.Size.x * 0.5f) / size, bottom / size);
        }

        /// <summary>
        /// Top edge of the tower (area coordinates) as if it had <paramref name="cubesCount"/> cubes.
        /// </summary>
        public static float TopEdge(float baseY, int cubesCount, float cubeSize) => (baseY + cubesCount) * cubeSize;

        /// <summary>
        /// Whether a tower of <paramref name="cubesCount"/> cubes standing at <paramref name="baseY"/> fits under the ceiling.
        /// </summary>
        public static bool Fits(float baseY, int cubesCount, TowerArea area) =>
            TopEdge(baseY, cubesCount, area.CubeSize) <= area.Size.y + HeightEpsilon;

        public static Rect CubeRect(Vector2 center, float size) =>
            new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);

        private static float CalculateFitShift(float minX, float maxX, float width)
        {
            if (maxX - minX > width)
                return width * 0.5f - (minX + maxX) * 0.5f;
            if (minX < 0f)
                return -minX;
            if (maxX > width)
                return width - maxX;
            return 0f;
        }
    }
}
