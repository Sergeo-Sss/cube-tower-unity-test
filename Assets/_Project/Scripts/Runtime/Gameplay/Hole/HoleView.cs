using System.Threading;
using CubeTower.Core.Geometry;
using CubeTower.Runtime.Config;
using CubeTower.Runtime.Gameplay.Cubes;
using CubeTower.Runtime.Gameplay.Drag;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace CubeTower.Runtime.Gameplay.Hole
{
    /// <summary>
    /// The hole. Hit test respects its oval shape; discarded cubes sink under the ellipse mask.
    /// </summary>
    public sealed class HoleView : MonoBehaviour
    {
        [Tooltip("Rect of the hole opening. The ellipse inscribed into it is used for hit testing.")]
        [SerializeField] private RectTransform _opening;

        [Tooltip("Masked (ellipse) container the cubes sink into.")]
        [SerializeField] private RectTransform _sinkRoot;

        private GameplaySettings _settings;
        private DragLayer _layer;

        [Inject]
        public void Construct(GameplaySettings settings, DragLayer layer)
        {
            _settings = settings;
            _layer = layer;
        }

        public Vector3 CenterWorld => _opening.TransformPoint(_opening.rect.center);

        public bool Contains(Vector2 screenPoint)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_opening, screenPoint, _layer.EventCamera, out var local))
                return false;

            return Ellipse.FromRect(_opening.rect).Contains(local);
        }

        public async UniTask PlayDiscardAsync(CubeView view, CancellationToken cancellationToken)
        {
            var animation = _settings.Animation;
            var rectTransform = view.RectTransform;
            view.KillAnimations();

            // 1. Fly over the hole centre, turning to a diamond as in the reference art.
            view.AttachTo(_opening);
            await DOTween.Sequence()
                .Join(rectTransform.DOAnchorPos(Vector2.zero, animation.FlyToHoleDuration).SetEase(Ease.OutQuad))
                .Join(rectTransform.DOLocalRotate(new Vector3(0f, 0f, 45f), animation.FlyToHoleDuration).SetEase(Ease.OutQuad))
                .Join(rectTransform.DOScale(animation.SinkScale, animation.FlyToHoleDuration).SetEase(Ease.OutQuad))
                .SetLink(view.gameObject)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, cancellationToken);

            // 2. Sink below the lower edge of the opening; the ellipse mask hides the cube gradually.
            view.AttachTo(_sinkRoot);
            var diagonal = _settings.CubeSize * animation.SinkScale * Mathf.Sqrt(2f);
            var depth = _opening.rect.height * 0.5f + diagonal;
            await rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y - depth, animation.SinkDuration)
                .SetEase(Ease.InQuad)
                .SetLink(view.gameObject)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, cancellationToken);
        }
    }
}
