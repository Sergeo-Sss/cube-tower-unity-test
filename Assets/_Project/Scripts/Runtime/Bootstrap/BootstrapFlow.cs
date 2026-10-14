using System;
using System.Threading;
using CubeTower.Core.Config;
using CubeTower.Runtime.Localization;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

namespace CubeTower.Runtime.Bootstrap
{
    /// <summary>
    /// Loads the config from the bound source and the localization, then opens the gameplay scene with the config injected.
    /// </summary>
    public sealed class BootstrapFlow : IInitializable, IDisposable
    {
        private const int TargetFrameRate = 60;

        private readonly IGameConfigProvider _configProvider;
        private readonly ZenjectSceneLoader _sceneLoader;
        private readonly string _gameplayScene;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();

        public BootstrapFlow(IGameConfigProvider configProvider, ZenjectSceneLoader sceneLoader, string gameplayScene)
        {
            _configProvider = configProvider;
            _sceneLoader = sceneLoader;
            _gameplayScene = gameplayScene;
        }

        public void Initialize()
        {
            // Mobile platforms default to 30 FPS which makes animations and dragging feel laggy.
            // Desktop / editor are left to VSync: a fixed 60 on a 144 Hz display produces uneven frame pacing.
            if (Application.isMobilePlatform)
                Application.targetFrameRate = TargetFrameRate;
            DOTween.Init(recycleAllByDefault: false, useSafeMode: true, LogBehaviour.ErrorsOnly).SetCapacity(200, 50);

            RunAsync(_lifetime.Token).Forget();
        }

        public void Dispose()
        {
            _lifetime.Cancel();
            _lifetime.Dispose();
        }

        private async UniTaskVoid RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Config and localization are independent - load them in parallel.
                var (config, _) = await UniTask.WhenAll(
                    _configProvider.LoadAsync(cancellationToken),
                    UnityLocalizationService.PreloadAsync(cancellationToken).ContinueWith(() => true));
                await _sceneLoader
                    .LoadSceneAsync(_gameplayScene, LoadSceneMode.Single, container => container.BindInstance(config))
                    .ToUniTask(cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // The bootstrap scene is unloaded by the gameplay scene - expected.
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
