using CubeTower.Core.Config;
using CubeTower.Runtime.Bootstrap;
using CubeTower.Runtime.Config;
using UnityEngine;
using Zenject;

namespace CubeTower.Runtime.Installers
{
    /// <summary>
    /// Composition root of the bootstrap scene: chooses the config source.
    /// </summary>
    public sealed class BootstrapInstaller : MonoInstaller
    {
        [SerializeField] private GameConfigAsset _gameConfig;
        [SerializeField] private string _gameplayScene = "Game";

        public override void InstallBindings()
        {
            // Swap this binding to load the config from JSON / remote config / etc.
            Container.Bind<IGameConfigProvider>().To<ScriptableObjectGameConfigProvider>().AsSingle().WithArguments(_gameConfig);
            Container.BindInterfacesTo<BootstrapFlow>().AsSingle().WithArguments(_gameplayScene);
        }
    }
}
