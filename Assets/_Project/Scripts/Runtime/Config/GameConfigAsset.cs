using System.Collections.Generic;
using System.Threading;
using CubeTower.Core.Config;
using CubeTower.Core.Cubes;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CubeTower.Runtime.Config
{
    /// <summary>
    /// Game config authored in the editor. One of possible config sources.
    /// </summary>
    [CreateAssetMenu(menuName = "Cube Tower/Game Config", fileName = "GameConfig")]
    public sealed class GameConfigAsset : ScriptableObject
    {
        [Tooltip("Number of cubes in the bottom scroll.")]
        [SerializeField, Min(1)] private int _paletteCubesCount = 20;

        [Tooltip("Colour ids of the bottom cubes in display order. Cycled if the count is bigger than the list. " +
                 "Ids must exist in the Cube Skin Catalog.")]
        [SerializeField] private List<string> _colors = new List<string>();

        [Header("Tower rules")]
        [Tooltip("Max random horizontal shift relative to the cube below, in cube edge lengths.")]
        [SerializeField, Range(0f, 1f)] private float _maxHorizontalOffset = TowerRulesConfig.DefaultMaxHorizontalOffset;

        [Tooltip("Allow stacking only onto a cube of the same colour.")]
        [SerializeField] private bool _requireSameColor;

        public IReadOnlyList<string> Colors => _colors;

        public GameConfig CreateConfig()
        {
            var colors = new List<CubeColorId>(_colors.Count);
            foreach (var color in _colors)
            {
                if (CubeColorId.TryCreate(color, out var id))
                    colors.Add(id);
            }

            return GameConfig.Create(_paletteCubesCount, colors, new TowerRulesConfig(_maxHorizontalOffset, _requireSameColor));
        }
    }

    /// <summary>
    /// <see cref="IGameConfigProvider"/> backed by <see cref="GameConfigAsset"/>.
    /// </summary>
    public sealed class ScriptableObjectGameConfigProvider : IGameConfigProvider
    {
        private readonly GameConfigAsset _asset;

        public ScriptableObjectGameConfigProvider(GameConfigAsset asset)
        {
            _asset = asset;
        }

        public UniTask<GameConfig> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_asset == null)
                throw new GameConfigException($"{nameof(GameConfigAsset)} is not assigned.");

            return UniTask.FromResult(_asset.CreateConfig());
        }
    }
}
