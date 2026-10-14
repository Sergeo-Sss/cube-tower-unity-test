using UnityEngine;

namespace CubeTower.Core.Tower
{
    /// <summary>
    /// Snapshot of the tower area geometry in presentation units.
    /// Coordinates inside the area have the origin at its bottom-left corner.
    /// </summary>
    public readonly struct TowerArea
    {
        public TowerArea(Vector2 size, float cubeSize)
        {
            Size = size;
            CubeSize = cubeSize;
        }

        public Vector2 Size { get; }

        public float CubeSize { get; }

        public bool IsValid => Size.x > 0f && Size.y > 0f && CubeSize > 0f;

        public Rect Bounds => new Rect(Vector2.zero, Size);
    }
}
