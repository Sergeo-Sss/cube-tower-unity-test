using CubeTower.Runtime.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace CubeTower.Runtime.UI
{
    /// <summary>
    /// Default drag threshold (10 px) is tiny on high-DPI phones and makes taps turn into drags.
    /// Sets it in physical units instead.
    /// </summary>
    [RequireComponent(typeof(EventSystem))]
    public sealed class DragThresholdByDpi : MonoBehaviour
    {
        private const float FallbackDpi = 160f;
        private const int MinThresholdPixels = 5;

        private GameplaySettings _settings;

        [Inject]
        public void Construct(GameplaySettings settings)
        {
            _settings = settings;
        }

        private void Start()
        {
            var dpi = Screen.dpi > 0f ? Screen.dpi : FallbackDpi;
            var threshold = Mathf.RoundToInt(_settings.Drag.DragThresholdInches * dpi);
            GetComponent<EventSystem>().pixelDragThreshold = Mathf.Max(MinThresholdPixels, threshold);
        }
    }
}
