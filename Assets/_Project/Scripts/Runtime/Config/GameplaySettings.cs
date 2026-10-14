using System;
using DG.Tweening;
using UnityEngine;

namespace CubeTower.Runtime.Config
{
    /// <summary>
    /// Presentation tuning: sizes, input and animations. Not a part of the gameplay config.
    /// </summary>
    [CreateAssetMenu(menuName = "Cube Tower/Gameplay Settings", fileName = "GameplaySettings")]
    public sealed class GameplaySettings : ScriptableObject
    {
        [Tooltip("Edge length of a cube in canvas units.")]
        [SerializeField, Min(1f)] private float _cubeSize = 178f;

        [SerializeField] private DragSettings _drag = new DragSettings();
        [SerializeField] private AnimationSettings _animation = new AnimationSettings();
        [SerializeField] private MessageSettings _messages = new MessageSettings();
        [SerializeField] private TutorialSettings _tutorial = new TutorialSettings();

        [Tooltip("Delay before the progress is written to disk after a change (seconds).")]
        [SerializeField, Min(0f)] private float _saveDelay = 0.5f;

        public float CubeSize => _cubeSize;
        public DragSettings Drag => _drag;
        public AnimationSettings Animation => _animation;
        public MessageSettings Messages => _messages;
        public TutorialSettings Tutorial => _tutorial;
        public float SaveDelay => _saveDelay;
    }

    [Serializable]
    public sealed class DragSettings
    {
        [Tooltip("Min angle of the gesture from the horizontal to pick a cube from the scroll. Flatter gestures scroll.")]
        [SerializeField, Range(0f, 90f)] private float _pickAngle = 40f;

        [Tooltip("Physical distance (inches) a finger must move before a drag starts.")]
        [SerializeField, Min(0f)] private float _dragThresholdInches = 0.05f;

        [SerializeField, Min(1f)] private float _pickupScale = 1.1f;
        [SerializeField, Min(0f)] private float _pickupDuration = 0.12f;

        public float PickAngle => _pickAngle;
        public float DragThresholdInches => _dragThresholdInches;
        public float PickupScale => _pickupScale;
        public float PickupDuration => _pickupDuration;
    }

    [Serializable]
    public sealed class AnimationSettings
    {
        [Header("Stack")]
        [SerializeField, Min(0f)] private float _jumpPower = 140f;
        [SerializeField, Min(0.01f)] private float _jumpDuration = 0.45f;

        [Header("Tower")]
        [SerializeField, Min(0.01f)] private float _collapseDuration = 0.35f;
        [SerializeField] private Ease _collapseEase = Ease.InOutSine;
        [SerializeField, Min(0.01f)] private float _returnDuration = 0.25f;

        [Header("Hole")]
        [SerializeField, Min(0.01f)] private float _flyToHoleDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float _sinkDuration = 0.45f;
        [SerializeField, Range(0.1f, 1f)] private float _sinkScale = 0.7f;

        [Header("Miss")]
        [SerializeField, Min(0.01f)] private float _missDuration = 0.35f;
        [SerializeField, Range(0, 32)] private int _missFragments = 10;
        [SerializeField, Min(0f)] private float _missSpread = 260f;

        public float JumpPower => _jumpPower;
        public float JumpDuration => _jumpDuration;
        public float CollapseDuration => _collapseDuration;
        public Ease CollapseEase => _collapseEase;
        public float ReturnDuration => _returnDuration;
        public float FlyToHoleDuration => _flyToHoleDuration;
        public float SinkDuration => _sinkDuration;
        public float SinkScale => _sinkScale;
        public float MissDuration => _missDuration;
        public int MissFragments => _missFragments;
        public float MissSpread => _missSpread;
    }

    [Serializable]
    public sealed class MessageSettings
    {
        [SerializeField, Min(0.1f)] private float _visibleDuration = 1.8f;
        [SerializeField, Min(0f)] private float _fadeDuration = 0.2f;

        public float VisibleDuration => _visibleDuration;
        public float FadeDuration => _fadeDuration;
    }

    [Serializable]
    public sealed class TutorialSettings
    {
        [SerializeField] private bool _enabled = true;

        [Tooltip("Seconds without input before the hand shows what to do.")]
        [SerializeField, Min(0.5f)] private float _idleDelay = 5f;

        [Tooltip("Delay before the palette scroll hint at start.")]
        [SerializeField, Min(0f)] private float _startDelay = 0.6f;

        [Header("Hand")]
        [SerializeField, Min(0.01f)] private float _fadeDuration = 0.25f;
        [SerializeField, Min(0.01f)] private float _pressDuration = 0.2f;
        [SerializeField, Range(0.5f, 1f)] private float _pressScale = 0.85f;
        [SerializeField, Min(0f)] private float _holdDuration = 0.2f;
        [SerializeField, Min(0.01f)] private float _dragDuration = 0.9f;
        [SerializeField, Min(0f)] private float _repeatPause = 1f;
        [SerializeField, Range(1f, 1.5f)] private float _highlightScale = 1.1f;

        [Tooltip("Where the finger touches a cube: offset from its centre in cube edges (0.3 = upper part).")]
        [SerializeField, Range(-0.5f, 0.5f)] private float _tapOffset = 0.3f;

        [Header("Palette scroll hint")]
        [Tooltip("Scroll distance in palette steps (cube + spacing).")]
        [SerializeField, Min(0f)] private float _scrollHintSteps = 1.6f;
        [SerializeField, Min(0.01f)] private float _scrollHintDuration = 0.7f;

        public bool Enabled => _enabled;
        public float IdleDelay => _idleDelay;
        public float StartDelay => _startDelay;
        public float FadeDuration => _fadeDuration;
        public float PressDuration => _pressDuration;
        public float PressScale => _pressScale;
        public float HoldDuration => _holdDuration;
        public float DragDuration => _dragDuration;
        public float RepeatPause => _repeatPause;
        public float HighlightScale => _highlightScale;
        public float TapOffset => _tapOffset;
        public float ScrollHintSteps => _scrollHintSteps;
        public float ScrollHintDuration => _scrollHintDuration;
    }
}
