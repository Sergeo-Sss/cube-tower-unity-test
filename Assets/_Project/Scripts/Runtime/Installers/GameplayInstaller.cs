using CubeTower.Core.Config;
using CubeTower.Core.Localization;
using CubeTower.Core.Messaging;
using CubeTower.Core.Progress;
using CubeTower.Core.Randomness;
using CubeTower.Core.Tower;
using CubeTower.Core.Tower.Placement;
using CubeTower.Runtime.Bootstrap;
using CubeTower.Runtime.Config;
using CubeTower.Runtime.Gameplay.Cubes;
using CubeTower.Runtime.Gameplay.Drag;
using CubeTower.Runtime.Gameplay.Effects;
using CubeTower.Runtime.Gameplay.Hole;
using CubeTower.Runtime.Gameplay.Palette;
using CubeTower.Runtime.Gameplay.Tower;
using CubeTower.Runtime.Localization;
using CubeTower.Runtime.Progress;
using CubeTower.Runtime.Tutorial;
using CubeTower.Runtime.UI;
using UnityEngine;
using Zenject;

namespace CubeTower.Runtime.Installers
{
    /// <summary>
    /// Composition root of the gameplay scene.
    /// </summary>
    public sealed class GameplayInstaller : MonoInstaller
    {
        private const int InitialCubesPoolSize = 12;

        [Header("Settings")]
        [SerializeField] private GameplaySettings _settings;
        [SerializeField] private CubeSkinCatalog _skins;

        [Tooltip("Used when the scene is started directly, without the bootstrap scene.")]
        [SerializeField] private GameConfigAsset _fallbackConfig;

        [Header("Scene")]
        [SerializeField] private TowerView _tower;
        [SerializeField] private HoleView _hole;
        [SerializeField] private PaletteView _palette;
        [SerializeField] private MessageView _message;
        [SerializeField] private DragLayer _dragLayer;
        [SerializeField] private CubeBurstEffect _burst;
        [SerializeField] private TutorialHandView _tutorialHand;
        [SerializeField] private RectTransform _poolRoot;

        [Header("Prefabs")]
        [SerializeField] private CubeView _cubePrefab;
        [SerializeField] private PaletteItem _paletteItemPrefab;

        public override void InstallBindings()
        {
            BindConfig();
            BindDomain();
            BindInfrastructure();
            BindPresentation();

            Container.BindInterfacesTo<GameplayEntryPoint>().AsSingle();
        }

        private void BindConfig()
        {
            if (!Container.HasBinding<GameConfig>())
            {
                Debug.LogWarning($"[{nameof(GameplayInstaller)}] Game config is not provided by the bootstrap scene, " +
                                 $"'{_fallbackConfig.name}' is used.");
                Container.BindInstance(_fallbackConfig.CreateConfig());
            }

            Container.Bind<TowerRulesConfig>().FromResolveGetter<GameConfig>(config => config.TowerRules).AsSingle();
            Container.BindInstance(_settings);
            Container.BindInstance(_skins);
        }

        private void BindDomain()
        {
            Container.BindInterfacesAndSelfTo<TowerModel>().AsSingle();
            Container.Bind<IRandomSource>().FromInstance(new SystemRandomSource()).AsSingle();

            // Placement rules are applied in the binding order.
            Container.Bind<IPlacementRule>().To<HeightLimitRule>().AsSingle();
            Container.Bind<IPlacementRule>().To<SameColorRule>().AsSingle();

            Container.Bind<TowerPlacementService>().AsSingle();
            Container.Bind<TowerGameplay>().AsSingle();
            Container.BindInterfacesTo<GameMessenger>().AsSingle();
        }

        private void BindInfrastructure()
        {
            Container.Bind<IProgressStorage>().FromInstance(new JsonFileStorage()).AsSingle();
            Container.BindInterfacesAndSelfTo<TowerProgressService>().AsSingle();

            Container.Bind<ILocalizationService>().To<UnityLocalizationService>().AsSingle();
        }

        private void BindPresentation()
        {
            Container.BindInstance(_tower);
            Container.BindInstance(_hole);
            Container.BindInstance(_palette);
            Container.BindInstance(_message);
            Container.BindInstance(_dragLayer);
            Container.BindInstance(_burst);
            Container.BindInstance(_tutorialHand);

            Container.BindMemoryPool<CubeView, CubeViewPool>()
                .WithInitialSize(InitialCubesPoolSize)
                .FromComponentInNewPrefab(_cubePrefab)
                .UnderTransform(_poolRoot);

            Container.BindFactory<PaletteItem, PaletteItem.Factory>().FromComponentInNewPrefab(_paletteItemPrefab);

            Container.BindInterfacesAndSelfTo<DragController>().AsSingle();
            Container.BindInterfacesTo<GameMessagePresenter>().AsSingle();
            Container.BindInterfacesTo<TutorialPresenter>().AsSingle();
        }
    }
}
