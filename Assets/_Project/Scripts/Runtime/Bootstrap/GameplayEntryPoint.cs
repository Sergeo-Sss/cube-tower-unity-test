using CubeTower.Core.Config;
using CubeTower.Runtime.Gameplay.Palette;
using CubeTower.Runtime.Gameplay.Tower;
using CubeTower.Runtime.Progress;
using Zenject;

namespace CubeTower.Runtime.Bootstrap
{
    /// <summary>
    /// Explicit start-up order of the gameplay scene.
    /// </summary>
    public sealed class GameplayEntryPoint : IInitializable
    {
        private readonly GameConfig _config;
        private readonly TowerProgressService _progress;
        private readonly TowerView _tower;
        private readonly PaletteView _palette;

        public GameplayEntryPoint(GameConfig config, TowerProgressService progress, TowerView tower, PaletteView palette)
        {
            _config = config;
            _progress = progress;
            _tower = tower;
            _palette = palette;
        }

        public void Initialize()
        {
            _progress.Restore();
            _tower.Initialize();
            _palette.Build(_config.PaletteCubes);
            _progress.StartAutoSave();
        }
    }
}
