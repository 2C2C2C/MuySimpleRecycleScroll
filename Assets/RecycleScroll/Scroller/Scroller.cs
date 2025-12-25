using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using MovementType = UnityEngine.UI.ScrollRect.MovementType;

namespace RecycleScrollView
{
    /// <summary>
    /// Simulate the scroll view movement.
    /// The scroll position is described by normalized position and the offset beyound viewport edge.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed partial class Scroller : UIBehaviour
    {
        /// <summary> Arg 1 noralized position, Arg 2 beyoud edge offset </summary>
        public class ScrollerValueChanged : UnityEvent<Vector2, Vector2> { }

        [SerializeField]
        private RectTransform _viewport;
        [SerializeField]
        private bool _horizontal = true;
        [SerializeField]
        private bool _vertical = true;

        [Header("Scroll params")]
        [SerializeField]
        private MovementType _movementType = MovementType.Elastic;
        [SerializeField]
        private float _elasticity = 0.1f;
        [SerializeField]
        private float _scrollingSmooth = 3f;
        [SerializeField]
        private bool _inertia = true;
        [SerializeField]
        private float _decelerationRate = 0.135f; // Only used when inertia is enabled
        [SerializeField]
        private float _scrollSensitivity = 1.0f;

        [Header("Velocity params")]
        [SerializeField]
        private float _velocityStopSqrMagThreshold = 7f;
        [SerializeField]
        private float _velocityMaxSqrMag = 1000f;

        [SerializeField]
        private ScrollerValueChanged _onScrollerValueChanged = new ScrollerValueChanged();
        [SerializeField]
        private UnityEvent _afterLateUpdate = new UnityEvent();

        private Bounds m_viewportBounds;
        private Bounds m_contentBounds;

        // Drag related flags
        private bool m_isDragging;
        private int m_dragPointerId = int.MinValue;
        private bool m_isScrolling;

        /// <summary> Based on unity unit </summary>
        private Vector2 m_velocity;

        private Vector2 m_dragStartNormalizedPosition;
        private Vector2 m_dragStartOffset;

        /// <summary> Local position in viewport </summary>
        private Vector2 m_pointerStartLocalCursor = Vector2.zero;
        /// <summary> Local delta in viewport </summary>
        private Vector2 m_currentPointerDelta = Vector2.zero;

        /// <summary> 
        /// 0~1; Means from Left/Bottom to Right/Top
        /// TODO Should be non-serialized but show in inspector 
        /// </summary>
        private Vector2 m_noramlizedPosition = Vector2.one;

        /// <summary> 
        /// Actual units that extend beyoud the viewport edge.
        /// Minus value(Left/Bottom); Positive value(Right/Top);
        /// TODO Should be non-serialized but show in inspector 
        /// </summary>
        private Vector2 m_beyoudEdgeOffset = Vector2.zero;

        private IScrollMoveTarget m_target;

        public bool Horizontal
        {
            get => _horizontal;
            set => _horizontal = value;
        }
        public bool Vertical
        {
            get => _vertical;
            set => _vertical = value;
        }
        /// <summary> 0~1; Means from Left/Bottom to Right/Top </summary>
        public Vector2 NormalizedPosition => m_noramlizedPosition;
        /// <summary> 
        /// Actual units that extend beyoud the viewport edge.
        /// Minus value(Left/Bottom); Positive value(Right/Top);
        /// </summary>
        public Vector2 BeyoudEdgeOffset => m_beyoudEdgeOffset;
        public bool HasScrollTarget => null != m_target;

        public ScrollerValueChanged OnScrollerValueChanged => _onScrollerValueChanged;

        public void Setup(IScrollMoveTarget receiver)
        {
            m_target = receiver;
            InitDefaultBounds();
            UpdateBounds();
        }

        /// <summary> Only stop the inertia movement </summary>
        public void StopMovement()
        {
            m_velocity = Vector2.zero;
        }

        /// <param name="normalizedPosition"> Vector2.zero ~ Vector2.one </param>
        /// <param name="extraOffset"> Unity units </param>
        /// <param name="force"> Force set and trigger event</param>
        public void SetNormalizedPositionWithNotifyIfNeed(Vector2 normalizedPosition, Vector2 extraOffset, bool force = false)
        {
            normalizedPosition.x = Mathf.Clamp01(normalizedPosition.x);
            normalizedPosition.y = Mathf.Clamp01(normalizedPosition.y);
            if (m_noramlizedPosition == normalizedPosition && !force)
            {
                return;
            }
            InternalSetNormalizedPosition(normalizedPosition, extraOffset);
        }

        /// <param name="normalizedPosition"> Vector2.zero ~ Vector2.one </param>
        /// <param name="extraOffset"> Unity units </param>
        public void SetNormalizedPositionWithoutNotify(Vector2 normalizedPosition, Vector2 extraOffset)
        {
            m_noramlizedPosition = normalizedPosition;
            m_beyoudEdgeOffset = extraOffset;
            SyncValueToScrollBar();
            UpdateBounds();
        }

        private void InternalSetNormalizedPosition(Vector2 normalizedPosition, Vector2 extraOffset)
        {
            m_noramlizedPosition = normalizedPosition;
            m_beyoudEdgeOffset = extraOffset;
            SyncValueToScrollBar();
            UpdateBounds();
            _onScrollerValueChanged?.Invoke(m_noramlizedPosition, BeyoudEdgeOffset);
        }

        protected override void Start()
        {
            InitDefaultBounds();
            UpdateBounds();
        }

        protected override void OnEnable()
        {
            BindScrollBars();
        }

        protected override void OnDisable()
        {
            UnbindScrollBars();
            // Reset some flags
            m_dragPointerId = int.MaxValue;
            m_isDragging = false;
            m_isScrolling = false;
        }

        private void LateUpdate()
        {
            float deltaTime = Time.unscaledDeltaTime;
            if (HasScrollTarget && 0f < deltaTime)
            {
                UpdateBounds();
                if (m_isDragging)
                {
                    if (_inertia)
                    {
                        // Velocity is based on drag move delta
                        Vector2 currentPointerDelta = -m_currentPointerDelta;
                        Vector3 newVelocity = currentPointerDelta / deltaTime;
                        m_velocity = Vector3.Lerp(m_velocity, newVelocity, deltaTime * 10);
                    }
                }
                else // Is not dragging
                {
                    UpdateBounds();
                    if (m_beyoudEdgeOffset != Vector2.zero || m_velocity != Vector2.zero)
                    {
                        Vector2 move = m_beyoudEdgeOffset;
                        for (int axis = 0; axis < 2; axis++) // Horizontal and Vertical
                        {
                            // Apply spring physics if movement is elastic and content has an offset from the view.
                            float speed = m_velocity[axis];
                            if (MovementType.Elastic == _movementType && !Mathf.Approximately(move[axis], 0f))
                            {
                                float smoothTime = _elasticity;
                                if (m_isScrolling)
                                {
                                    smoothTime *= _scrollingSmooth;
                                }
                                float temp = move[axis];
                                move[axis] = Mathf.SmoothDamp(temp, 0f, ref speed, smoothTime, Mathf.Infinity, deltaTime);
                                if (1 > Mathf.Abs(speed))
                                {
                                    speed = 0;
                                }
                            }
                            else if (_inertia)
                            {
                                // Inertia move according to velocity with deceleration applied.
                                speed *= Mathf.Pow(_decelerationRate, deltaTime);
                                if (1 > Mathf.Abs(speed))
                                {
                                    speed = 0;
                                }
                                move[axis] += speed * deltaTime;
                                // RecycleScrollLogger.LogError($"inertia speed {speed}; scroll move {scrollMoveV2[axis]}; normal move {move[axis]}");
                            }
                            else
                            {
                                // If we have neither elaticity or friction, there shouldn't be any velocity.
                                speed = 0;
                            }
                            m_velocity[axis] = speed;
                            // Clamp unnecessary movement
                            float tempAbs = Mathf.Abs(move[axis]);
                            if (tempAbs < float.Epsilon && !Mathf.Approximately(0f, tempAbs) ||
                                MovementType.Clamped == _movementType)
                            {
                                move[axis] = 0f;
                            }
                        }

                        ClampNormalizedPositionAndOffset(NormalizedPosition, move, out Vector2 newNormalizedPostion, out Vector2 newOffset);
                        InternalSetNormalizedPosition(newNormalizedPostion, newOffset);
                        UpdateBounds();
                        _onScrollerValueChanged?.Invoke(NormalizedPosition, BeyoudEdgeOffset);
                    }
                }

                m_velocity = ClampVelocity(m_velocity);
                // Reset some flags
                m_isScrolling = false;
                m_currentPointerDelta = Vector2.zero;
                _afterLateUpdate.Invoke();
            }
        }

    }
}