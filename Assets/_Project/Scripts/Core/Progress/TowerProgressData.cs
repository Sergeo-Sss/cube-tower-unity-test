using System;
using System.Collections.Generic;

namespace CubeTower.Core.Progress
{
    /// <summary>
    /// Serializable snapshot of the tower. Coordinates are in cube units, so the save
    /// is valid for any screen resolution / aspect ratio.
    /// </summary>
    [Serializable]
    public sealed class TowerProgressData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public float BaseX;
        public float BaseY;
        public List<TowerCubeData> Cubes = new List<TowerCubeData>();
    }

    [Serializable]
    public sealed class TowerCubeData
    {
        public string ColorId;
        public float Offset;
    }
}
