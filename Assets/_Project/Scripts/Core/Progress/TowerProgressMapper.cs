using System;
using System.Collections.Generic;
using CubeTower.Core.Config;
using CubeTower.Core.Cubes;
using CubeTower.Core.Tower;
using UnityEngine;

namespace CubeTower.Core.Progress
{
    /// <summary>
    /// Maps <see cref="TowerModel"/> to <see cref="TowerProgressData"/> and back.
    /// Restoring is defensive: the save may be produced by another version of the game or a different config.
    /// </summary>
    public static class TowerProgressMapper
    {
        public static TowerProgressData Capture(TowerModel tower)
        {
            var data = new TowerProgressData
            {
                BaseX = tower.BasePosition.x,
                BaseY = tower.BasePosition.y
            };

            foreach (var cube in tower.Cubes)
                data.Cubes.Add(new TowerCubeData { ColorId = cube.Color.Value, Offset = cube.Offset });

            return data;
        }

        /// <summary>
        /// Restores the tower from the save. Cubes with colours unknown to the current config are skipped,
        /// their offsets are carried to the next cube so the tower keeps its shape.
        /// </summary>
        /// <returns><c>false</c> if the save can't be used at all.</returns>
        public static bool TryRestore(TowerModel tower, TowerProgressData data, GameConfig config)
        {
            if (tower == null)
                throw new ArgumentNullException(nameof(tower));
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (data == null || data.Version <= 0 || data.Version > TowerProgressData.CurrentVersion)
                return false;
            if (!IsFinite(data.BaseX) || !IsFinite(data.BaseY))
                return false;

            var basePosition = new Vector2(data.BaseX, Mathf.Max(0f, data.BaseY));
            var cubes = new List<(CubeColorId, float)>();
            var carriedOffset = 0f;

            if (data.Cubes != null)
            {
                foreach (var entry in data.Cubes)
                {
                    var offset = entry != null && IsFinite(entry.Offset) ? entry.Offset : 0f;
                    if (entry == null || !CubeColorId.TryCreate(entry.ColorId, out var color) || !config.HasColor(color))
                    {
                        if (cubes.Count == 0)
                            basePosition.x += offset;
                        else
                            carriedOffset += offset;
                        continue;
                    }

                    if (cubes.Count == 0)
                    {
                        basePosition.x += offset;
                        cubes.Add((color, 0f));
                    }
                    else
                    {
                        cubes.Add((color, offset + carriedOffset));
                    }

                    carriedOffset = 0f;
                }
            }

            tower.Restore(basePosition, cubes);
            return true;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
