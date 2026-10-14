using System.Threading;
using CubeTower.Runtime.Config;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace CubeTower.Runtime.Tutorial
{
    /// <summary>
    /// Tutorial cursor: appears at a point, presses, optionally drags a cube preview and releases.
    /// The root is placed at the fingertip; the hand sprite's pivot is the fingertip too, so pressing,
    /// tilting and mirroring never move the touch point. Before every gesture the hand is oriented
    /// (tilted around the fingertip and mirrored if needed) so it stays fully on screen along the whole path.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class TutorialHandView : MonoBehaviour
    {
        [SerializeField] private RectTransform _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _hand;
        [SerializeField] private Image _ghost;

        [Tooltip("Min distance between the hand and the screen edges, in canvas units.")]
        [SerializeField, Min(0f)] private float _edgeMargin = 12f;

        [Tooltip("Max rotation around the fingertip used to keep the hand on screen.")]
        [SerializeField, Range(0f, 90f)] private float _maxTilt = 75f;

        [Tooltip("Opacity of the cube preview carried by the hand.")]
        [SerializeField, Range(0f, 1f)] private float _ghostAlpha = 0.85f;

        private TutorialSettings _settings;
        private float _cubeSize;
        private float _mirror = 1f;
        private readonly Vector3[] _corners = new Vector3[4];

        [Inject]
        public void Construct(GameplaySettings settings)
        {
            _settings = settings.Tutorial;
            _cubeSize = settings.CubeSize;
        }

        private void Awake()
        {
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
            Hide();
        }

        /// <summary>
        /// Fades in at <paramref name="worldPosition"/> and presses. The hand is oriented to stay on screen
        /// all the way to <paramref name="pathEnd"/>. <paramref name="ghost"/> - cube carried by the hand (optional),
        /// its centre is placed at <paramref name="ghostOffset"/> (canvas units) from the fingertip.
        /// </summary>
        public UniTask PressAtAsync(Vector3 worldPosition, Vector3 pathEnd, Sprite ghost, Vector2 ghostOffset,
            CancellationToken cancellationToken)
        {
            Kill();
            _root.position = worldPosition;
            Orient(worldPosition, pathEnd);

            _ghost.gameObject.SetActive(ghost != null);
            _ghost.sprite = ghost;
            _ghost.rectTransform.sizeDelta = new Vector2(_cubeSize, _cubeSize);
            _ghost.rectTransform.anchoredPosition = ghostOffset;
            _ghost.color = new Color(1f, 1f, 1f, 0f);

            var pressed = new Vector3(_mirror * _settings.PressScale, _settings.PressScale, 1f);
            return DOTween.Sequence()
                .Append(_canvasGroup.DOFade(1f, _settings.FadeDuration))
                .Append(_hand.DOScale(pressed, _settings.PressDuration).SetEase(Ease.OutQuad))
                .Join(_ghost.DOFade(_ghostAlpha, _settings.PressDuration))
                .SetLink(gameObject)
                .SetTarget(this)
                .ToUniTask(TweenCancelBehaviour.CancelAwait, cancellationToken);
        }

        public UniTask MoveToAsync(Vector3 worldPosition, float duration, CancellationToken cancellationToken) =>
            _root.DOMove(worldPosition, duration)
                .SetEase(Ease.InOutSine)
                .SetLink(gameObject)
                .SetTarget(this)
                .ToUniTask(TweenCancelBehaviour.CancelAwait, cancellationToken);

        public UniTask ReleaseAsync(CancellationToken cancellationToken) =>
            DOTween.Sequence()
                .Append(_hand.DOScale(new Vector3(_mirror, 1f, 1f), _settings.PressDuration).SetEase(Ease.OutBack))
                .Join(_ghost.DOFade(0f, _settings.PressDuration))
                .AppendInterval(_settings.HoldDuration)
                .Append(_canvasGroup.DOFade(0f, _settings.FadeDuration))
                .SetLink(gameObject)
                .SetTarget(this)
                .ToUniTask(TweenCancelBehaviour.CancelAwait, cancellationToken);

        public void Hide()
        {
            Kill();
            _canvasGroup.alpha = 0f;
            _ghost.gameObject.SetActive(false);
        }

        private void Kill()
        {
            DOTween.Kill(this);
            _root.DOKill();
            _hand.DOKill();
            _ghost.DOKill();
            _canvasGroup.DOKill();
        }

        /// <summary>
        /// Picks the smallest tilt (preferring the natural, non-mirrored hand) that keeps the whole sprite
        /// inside the screen at both ends of the gesture. Falls back to the least overflowing orientation.
        /// </summary>
        private void Orient(Vector3 fromWorld, Vector3 toWorld)
        {
            var parent = _root.parent as RectTransform;
            _mirror = 1f;
            var bestTilt = 0f;

            if (parent != null)
            {
                var bounds = parent.rect;
                bounds.xMin += _edgeMargin;
                bounds.yMin += _edgeMargin;
                bounds.xMax -= _edgeMargin;
                bounds.yMax -= _edgeMargin;

                var from = (Vector2)parent.InverseTransformPoint(fromWorld);
                var to = (Vector2)parent.InverseTransformPoint(toWorld);
                var bestOverflow = float.MaxValue;
                const float tiltStep = 5f;

                for (var mirror = 1f; mirror >= -1f; mirror -= 2f)
                {
                    for (var tilt = 0f; tilt <= _maxTilt + 0.01f; tilt += tiltStep)
                    {
                        var overflow = Mathf.Max(Overflow(from, mirror, tilt, bounds), Overflow(to, mirror, tilt, bounds));
                        if (overflow < bestOverflow - 0.01f)
                        {
                            bestOverflow = overflow;
                            _mirror = mirror;
                            bestTilt = tilt;
                        }

                        if (overflow <= 0f)
                            break;
                    }

                    if (bestOverflow <= 0f)
                        break;
                }
            }

            _hand.localScale = new Vector3(_mirror, 1f, 1f);
            _hand.localRotation = Quaternion.Euler(0f, 0f, bestTilt * _mirror);
        }

        /// <summary>
        /// How far (canvas units) the hand would stick out of <paramref name="bounds"/> with the fingertip at <paramref name="tip"/>.
        /// Positive tilt lifts the hand body: counter-clockwise for the normal hand, clockwise for the mirrored one.
        /// </summary>
        private float Overflow(Vector2 tip, float mirror, float tilt, Rect bounds)
        {
            var rect = _hand.rect;
            var rotation = Quaternion.Euler(0f, 0f, tilt * mirror);
            _corners[0] = new Vector3(rect.xMin, rect.yMin);
            _corners[1] = new Vector3(rect.xMin, rect.yMax);
            _corners[2] = new Vector3(rect.xMax, rect.yMax);
            _corners[3] = new Vector3(rect.xMax, rect.yMin);

            var overflow = 0f;
            foreach (var corner in _corners)
            {
                var point = tip + (Vector2)(rotation * new Vector3(corner.x * mirror, corner.y));
                overflow = Mathf.Max(overflow, bounds.xMin - point.x, point.x - bounds.xMax, bounds.yMin - point.y, point.y - bounds.yMax);
            }

            return overflow;
        }

        private void OnDestroy() => Kill();

        private void Reset()
        {
            _root = (RectTransform)transform;
            _canvasGroup = GetComponent<CanvasGroup>();
        }
    }
}
