using System;
using CubeTower.Core.Config;
using CubeTower.Core.Progress;
using CubeTower.Core.Tower;
using CubeTower.Runtime.Config;
using UniRx;
using UnityEngine;

namespace CubeTower.Runtime.Progress
{
    /// <summary>
    /// Restores the tower on start and saves it after changes (debounced),
    /// on application pause (mobile) and on quit.
    /// </summary>
    public sealed class TowerProgressService : IDisposable
    {
        public const string StorageKey = "tower";

        private readonly TowerModel _tower;
        private readonly IProgressStorage _storage;
        private readonly GameConfig _config;
        private readonly GameplaySettings _settings;
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
        private bool _isDirty;

        public TowerProgressService(TowerModel tower, IProgressStorage storage, GameConfig config, GameplaySettings settings)
        {
            _tower = tower;
            _storage = storage;
            _config = config;
            _settings = settings;
        }

        public void Restore()
        {
            if (!_storage.TryLoad<TowerProgressData>(StorageKey, out var data))
                return;

            if (!TowerProgressMapper.TryRestore(_tower, data, _config))
                Debug.LogWarning($"[{nameof(TowerProgressService)}] Saved tower is incompatible and was ignored.");
        }

        public void StartAutoSave()
        {
            _subscriptions.Clear();

            var changes = _tower.Changes.Do(_ => _isDirty = true);
            var delay = TimeSpan.FromSeconds(_settings.SaveDelay);
            (delay > TimeSpan.Zero ? changes.Throttle(delay) : changes)
                .Subscribe(_ => SaveIfDirty())
                .AddTo(_subscriptions);

            Observable.EveryApplicationPause()
                .Where(paused => paused)
                .Subscribe(_ => SaveIfDirty())
                .AddTo(_subscriptions);

            Observable.OnceApplicationQuit()
                .Subscribe(_ => SaveIfDirty())
                .AddTo(_subscriptions);
        }

        public void SaveIfDirty()
        {
            if (!_isDirty)
                return;

            _isDirty = false;
            _storage.Save(StorageKey, TowerProgressMapper.Capture(_tower));
        }

        public void Dispose()
        {
            SaveIfDirty();
            _subscriptions.Dispose();
        }
    }
}
