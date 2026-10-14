using System.Threading;
using CubeTower.Runtime.Config;
using CubeTower.Runtime.Gameplay.Cubes;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;
using Zenject;

namespace CubeTower.Runtime.Gameplay.Effects
{
    /// <summary>
    /// "Explosion" of a lost cube: the cube pops and shrinks while small fragments scatter around.
    /// Fragments are pooled UI images, no physics involved.
    /// </summary>
    public sealed class CubeBurstEffect : MonoBehaviour
    {
        private const int MaxPooledFragments = 64;

        [SerializeField] private RectTransform _root;
        [SerializeField, Range(0.05f, 1f)] private float _minFragmentScale = 0.16f;
        [SerializeField, Range(0.05f, 1f)] private float _maxFragmentScale = 0.3f;

        private GameplaySettings _settings;
        private ObjectPool<RectTransform> _fragments;

        [Inject]
        public void Construct(GameplaySettings settings)
        {
            _settings = settings;
        }

        private void Awake()
        {
            _fragments = new ObjectPool<RectTransform>(CreateFragment,
                fragment => fragment.gameObject.SetActive(true),
                fragment =>
                {
                    fragment.DOKill();
                    fragment.gameObject.SetActive(false);
                },
                fragment =>
                {
                    if (fragment != null)
                        Destroy(fragment.gameObject);
                },
                collectionCheck: false,
                maxSize: MaxPooledFragments);
        }

        private void Start()
        {
            // Create fragments for one burst up front instead of during the first miss.
            var count = _settings.Animation.MissFragments;
            var warm = new RectTransform[count];
            for (var i = 0; i < count; i++)
                warm[i] = _fragments.Get();
            foreach (var fragment in warm)
                _fragments.Release(fragment);
        }

        public UniTask PlayAsync(CubeView view, CancellationToken cancellationToken)
        {
            var animation = _settings.Animation;
            var rectTransform = view.RectTransform;
            view.KillAnimations();

            SpawnFragments(view.Sprite, rectTransform.position, animation);

            var scale = rectTransform.localScale.x;
            var popDuration = animation.MissDuration * 0.3f;
            var vanishDuration = animation.MissDuration - popDuration;
            return DOTween.Sequence()
                .Append(rectTransform.DOScale(scale * 1.2f, popDuration).SetEase(Ease.OutQuad))
                .Append(rectTransform.DOScale(0f, vanishDuration).SetEase(Ease.InBack))
                .Join(view.CanvasGroup.DOFade(0f, vanishDuration))
                .SetLink(view.gameObject)
                .ToUniTask(TweenCancelBehaviour.KillAndCancelAwait, cancellationToken);
        }

        private void SpawnFragments(Sprite sprite, Vector3 worldPosition, AnimationSettings animation)
        {
            var count = animation.MissFragments;
            var duration = animation.MissDuration * 1.4f;
            for (var i = 0; i < count; i++)
            {
                var fragment = _fragments.Get();
                fragment.GetComponent<Image>().sprite = sprite;
                fragment.SetAsLastSibling();
                fragment.position = worldPosition;
                fragment.localRotation = Quaternion.identity;
                fragment.localScale = Vector3.one;
                fragment.sizeDelta = Vector2.one * (_settings.CubeSize * Random.Range(_minFragmentScale, _maxFragmentScale));

                var angle = (360f / count * i + Random.Range(-15f, 15f)) * Mathf.Deg2Rad;
                var distance = animation.MissSpread * Random.Range(0.6f, 1f);
                var target = fragment.anchoredPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

                DOTween.Sequence()
                    .Join(fragment.DOAnchorPos(target, duration).SetEase(Ease.OutCubic))
                    .Join(fragment.DOLocalRotate(new Vector3(0f, 0f, Random.Range(-360f, 360f)), duration, RotateMode.FastBeyond360))
                    .Join(fragment.DOScale(0f, duration).SetEase(Ease.InQuad))
                    .SetTarget(fragment)
                    .SetLink(fragment.gameObject)
                    .OnComplete(() => _fragments.Release(fragment));
            }
        }

        private RectTransform CreateFragment()
        {
            var go = new GameObject("Fragment", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rectTransform = (RectTransform)go.transform;
            rectTransform.SetParent(_root, false);
            rectTransform.anchorMin = rectTransform.anchorMax = rectTransform.pivot = new Vector2(0.5f, 0.5f);
            go.GetComponent<Image>().raycastTarget = false;
            return rectTransform;
        }

        private void OnDestroy() => _fragments?.Dispose();

        private void Reset() => _root = (RectTransform)transform;
    }
}
