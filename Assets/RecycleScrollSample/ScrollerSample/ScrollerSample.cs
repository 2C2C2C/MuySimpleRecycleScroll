using UnityEngine;
using UnityEngine.UI;

namespace RecycleScrollView.Sample
{
    public class ScrollerSample : MonoBehaviour, IScrollMoveTarget
    {
        [SerializeField]
        private ScrollRect _unityScrollrect;
        [SerializeField]
        private Scroller _scroller;
        [SerializeField]
        private bool _horizontal = true;
        [SerializeField]
        private bool _vertical = true;
        [SerializeField]
        private Vector2 _scrollsize = Vector2.zero;

        [SerializeField]
        private Vector2 _startProgress;
        [SerializeField]
        private bool _inversedXScroll;
        [SerializeField]
        private bool _inversedYScroll;

        private Vector2 m_noramlizedProgress = Vector2.one;

        public Vector2 ConvertToNormalizedMoveFromCurrentPosition(Vector2 move, out Vector2 offset)
        {
            Vector2 result = Vector2.one;
            result.x = (_scroller.Horizontal && _scrollsize.x > 0f) ? move.x / _scrollsize.x : 0f;
            result.y = (_scroller.Vertical && _scrollsize.y > 0f) ? move.y / _scrollsize.y : 0f;
            // Debug.LogError($"Convert {move} to {result} by size {_startSize}");

            offset = Vector2.zero;
            for (int i = 0; i < 2; i++)
            {
                int axis = i;
                float tempValue = m_noramlizedProgress[axis] + result[axis];
                if (0f > tempValue)
                {
                    offset[axis] = tempValue * _scrollsize[axis];
                }
                else if (1f < tempValue)
                {
                    offset[axis] = (tempValue - 1f) * _scrollsize[axis];
                }
            }
            return result;
        }

        [ContextMenu(nameof(PrintUnityScrollRectScrollPosition))]
        private void PrintUnityScrollRectScrollPosition()
        {
            Debug.LogError($"Unity ScrollRect scroll position {_unityScrollrect.normalizedPosition}");
        }

        private void OnScrollerValueChanged(Vector2 scrollerNormalizedPosition, Vector2 scrollOffset)
        {
            Vector2 convertedNormalizedPosition = scrollerNormalizedPosition;
            // Vector2 scroll
            Vector2 normalizedOffset = new Vector2(scrollOffset.x / _scrollsize.x, scrollOffset.y / _scrollsize.y);
            convertedNormalizedPosition += normalizedOffset;
            m_noramlizedProgress = scrollerNormalizedPosition;

            if (_inversedXScroll)
            {
                convertedNormalizedPosition.x = 1f - convertedNormalizedPosition.x;
            }
            if (_inversedYScroll)
            {
                convertedNormalizedPosition.y = 1f - convertedNormalizedPosition.y;
            }
            _unityScrollrect.normalizedPosition = convertedNormalizedPosition;
        }

        private void Start()
        {
            _scroller.Vertical = _unityScrollrect.vertical = _vertical;
            _scroller.Horizontal = _unityScrollrect.horizontal = _horizontal;

            m_noramlizedProgress = Vector2.zero;
            Vector2 viewportSize = _unityScrollrect.viewport.rect.size;
            RectTransform content = _unityScrollrect.content;
            _unityScrollrect.movementType = ScrollRect.MovementType.Unrestricted;
            _unityScrollrect.inertia = false;
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, _horizontal ? viewportSize.x + _scrollsize.x : viewportSize.x);
            content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, _vertical ? viewportSize.y + _scrollsize.y : viewportSize.y);

            _scroller.Setup(this);
            Vector2 startProgress = _startProgress;
            if (_inversedXScroll)
            {
                startProgress.x = Mathf.Clamp01(1f - startProgress.x);
            }
            if (_inversedYScroll)
            {
                startProgress.y = Mathf.Clamp01(1f - startProgress.y);
            }
            _scroller.SetNormalizedPositionWithNotifyIfNeed(startProgress, Vector2.zero, true);
        }

        private void OnEnable()
        {
            _scroller.OnScrollerValueChanged.AddListener(OnScrollerValueChanged);
        }

        private void OnDisable()
        {
            _scroller.OnScrollerValueChanged.RemoveListener(OnScrollerValueChanged);
        }

    }
}