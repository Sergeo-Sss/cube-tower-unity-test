using CubeTower.Core.Cubes;
using CubeTower.Runtime.Config;
using Zenject;

namespace CubeTower.Runtime.Gameplay.Cubes
{
    /// <summary>
    /// Pool of cube visuals: cubes are created and destroyed constantly (infinite palette), so they are reused.
    /// </summary>
    public sealed class CubeViewPool : MonoMemoryPool<CubeColorId, CubeView>
    {
        private CubeSkinCatalog _skins;
        private GameplaySettings _settings;

        [Inject]
        public void Construct(CubeSkinCatalog skins, GameplaySettings settings)
        {
            _skins = skins;
            _settings = settings;
        }

        protected override void Reinitialize(CubeColorId color, CubeView view)
        {
            view.Setup(color, _skins.GetSprite(color));
            view.SetSize(_settings.CubeSize);
        }

        protected override void OnDespawned(CubeView view)
        {
            view.ResetVisualState();
            view.SetDragReceiver(null);
            base.OnDespawned(view);
        }
    }
}
