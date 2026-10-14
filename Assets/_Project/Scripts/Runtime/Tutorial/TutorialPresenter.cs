using System;
using System.Threading;
using CubeTower.Core.Tower;
using CubeTower.Core.Tutorial;
using CubeTower.Runtime.Config;
using CubeTower.Runtime.Gameplay.Cubes;
using CubeTower.Runtime.Gameplay.Drag;
using CubeTower.Runtime.Gameplay.Hole;
using CubeTower.Runtime.Gameplay.Palette;
using CubeTower.Runtime.Gameplay.Tower;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace CubeTower.Runtime.Tutorial
{
    /// <summary>
    /// Shows the player what to do:
    /// - at start the palette scrolls back and forth under a swiping hand (it is scrollable);
    /// - after <see cref="TutorialSettings.IdleDelay"/> seconds without input the hand demonstrates
    ///   the next useful action (place / stack / throw into the hole) and repeats while the player is idle.
    /// Any touch interrupts the hint immediately.
    /// </summary>
    public sealed class TutorialPresenter : IInitializable, ITickable, IDisposable
    {
        private readonly TutorialHandView _hand;
        private readonly PaletteView _palette;
        private readonly TowerView _towerView;
        private readonly HoleView _hole;
        private readonly TowerModel _tower;
        private readonly DragController _drag;
        private readonly TutorialSettings _settings;
        private readonly float _cubeSize;
        private readonly TutorialHintPlanner _planner = new TutorialHintPlanner();
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();

        private CancellationTokenSource _hint;
        private CubeView _highlighted;
        private float _idleTime;
        private bool _playerWasActive;
        private bool _disposed;

        public TutorialPresenter(TutorialHandView hand, PaletteView palette, TowerView towerView, HoleView hole,
            TowerModel tower, DragController drag, GameplaySettings settings)
        {
            _hand = hand;
            _palette = palette;
            _towerView = towerView;
            _hole = hole;
            _tower = tower;
            _drag = drag;
            _settings = settings.Tutorial;
            _cubeSize = settings.CubeSize;
        }

        public void Initialize()
        {
            if (_settings.Enabled)
                RunAsync(_lifetime.Token).Forget();
        }

        public void Tick()
        {
            if (IsPlayerActive())
            {
                _idleTime = 0f;
                _playerWasActive = true;
                InterruptHint();
                return;
            }

            _idleTime += Time.unscaledDeltaTime;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            InterruptHint();
            _lifetime.Cancel();
            _lifetime.Dispose();
        }

        private bool IsPlayerActive() => Input.touchCount > 0 || Input.GetMouseButton(0) || _drag.IsDragging;

        private async UniTaskVoid RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_settings.StartDelay), ignoreTimeScale: true, cancellationToken: cancellationToken);
                // The player who already touched the screen doesn't need the scroll hint.
                if (!_playerWasActive)
                    await PlayHintAsync(PlayScrollHintAsync);

                while (!cancellationToken.IsCancellationRequested)
                {
                    await UniTask.WaitUntil(() => _idleTime >= _settings.IdleDelay, cancellationToken: cancellationToken);
                    await PlayHintAsync(PlayGameplayHintAsync);
                    await UniTask.Delay(TimeSpan.FromSeconds(_settings.RepeatPause), ignoreTimeScale: true, cancellationToken: cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // The scene is being unloaded.
            }
        }

        private async UniTask PlayHintAsync(Func<CancellationToken, UniTask> hint)
        {
            var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            _hint = cancellation;
            try
            {
                await hint(cancellation.Token);
            }
            catch (OperationCanceledException) when (!_lifetime.IsCancellationRequested)
            {
                // Interrupted by the player - expected.
            }
            finally
            {
                // Tweens are awaited with CancelAwait: stopping them is done here, in one place,
                // so a cancelled tween is never killed twice.
                ResetHighlight();
                _palette.StopScrollAnimation();
                _hand.Hide();
                if (_hint == cancellation)
                {
                    _hint = null;
                    cancellation.Dispose();
                }
            }
        }

        private void InterruptHint()
        {
            var cancellation = _hint;
            if (cancellation == null)
                return;

            _hint = null;
            cancellation.Cancel();
            cancellation.Dispose();
        }

        private async UniTask PlayScrollHintAsync(CancellationToken cancellationToken)
        {
            var travel = Mathf.Min(_palette.MaxScroll, _palette.ItemStep * _settings.ScrollHintSteps);
            if (travel <= 1f)
                return;

            var start = _palette.ScrollHintStart;
            var finish = start + _palette.ContentToWorldVector(new Vector2(-travel, 0f));
            var duration = _settings.ScrollHintDuration;

            await _hand.PressAtAsync(start, finish, null, Vector2.zero, cancellationToken);
            await UniTask.WhenAll(
                _hand.MoveToAsync(finish, duration, cancellationToken),
                _palette.ScrollByAsync(-travel, duration, cancellationToken));
            await _hand.ReleaseAsync(cancellationToken);
            await _palette.ScrollByAsync(travel, duration, cancellationToken);
        }

        private async UniTask PlayGameplayHintAsync(CancellationToken cancellationToken)
        {
            if (!TryPlanGesture(out var source, out var target))
                return;

            // Touch the upper part of the cube (as a finger would) and carry the preview with the same offset,
            // so the preview covers the real cube at start and lands exactly on the target.
            var tap = new Vector2(0f, _cubeSize * _settings.TapOffset);
            var tapWorld = source.RectTransform.TransformVector(tap);
            var start = source.RectTransform.position + tapWorld;
            var finish = target + tapWorld;

            Highlight(source);
            await _hand.PressAtAsync(start, finish, source.Sprite, -tap, cancellationToken);
            await _hand.MoveToAsync(finish, _settings.DragDuration, cancellationToken);
            ResetHighlight();
            await _hand.ReleaseAsync(cancellationToken);
        }

        private bool TryPlanGesture(out CubeView source, out Vector3 target)
        {
            var area = _towerView.Area;
            switch (_planner.Next(_tower, area))
            {
                case TutorialHint.PlaceFirstCube:
                    target = _towerView.AreaToWorld(new Vector2(area.Size.x * 0.5f, _cubeSize * 1.5f));
                    return _palette.TryGetHintCube(out source);

                case TutorialHint.StackCube when _towerView.TryGetTopCube(out var top):
                    target = top.RectTransform.TransformPoint(new Vector3(0f, _cubeSize, 0f));
                    return _palette.TryGetHintCube(out source);

                case TutorialHint.DiscardCube when _towerView.TryGetTopCube(out var top):
                    source = top;
                    target = _hole.CenterWorld;
                    return true;

                default:
                    source = null;
                    target = default;
                    return false;
            }
        }

        private void Highlight(CubeView cube)
        {
            _highlighted = cube;
            cube.ScaleTo(_settings.HighlightScale, _settings.PressDuration, DG.Tweening.Ease.OutBack);
        }

        private void ResetHighlight()
        {
            if (_highlighted == null)
                return;

            // The cube may have been picked up meanwhile - the drag owns its scale then.
            if (!_drag.IsDragging && _highlighted.isActiveAndEnabled)
                _highlighted.ScaleTo(1f, _settings.PressDuration);

            _highlighted = null;
        }
    }
}
