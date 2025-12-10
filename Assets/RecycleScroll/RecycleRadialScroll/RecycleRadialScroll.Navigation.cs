using UnityEngine;

namespace RecycleScrollView
{
    public partial class RecycleRadialScroll
    {
        [Header("Navigation params"), Tooltip("Rotate anticlockwise")]
        [SerializeField, Range(0f, 360f)]
        private float _jumpToAngle;

        public void JumpToByDataIndex(int dataIndex)
        {
            int elementIndex = ElementIndexDataIndex2WayConvert(dataIndex);
            JumpToByElementIndex(elementIndex);
        }

        public void JumpToByElementIndex(int elementIndex)
        {
            int dataCount = m_dataSource.DataElementCount;
            if (0 > elementIndex || dataCount - 1 < elementIndex)
            {
                return; // Invalid case
            }

            // Element index is the same as gap count
            float move = elementIndex * _internvalAngle;
            float totalMove = (dataCount - 1) * _internvalAngle;

            float tempDelta;
            // start angle and jump to angle are all rotate anticlockwise
            if (_antiClockwise)
            {
                tempDelta = -(_jumpToAngle - _startAngle); // Result positive means further in anticlockwise round 
            }
            else
            {
                tempDelta = _jumpToAngle - _startAngle;
            }
            move += tempDelta;

            float scrollProgress = Mathf.Clamp01(move / totalMove);
            SetScrollProgress(scrollProgress);
        }

        public void SetScrollProgress(float normalized)
        {
            normalized = Mathf.Clamp01(normalized);
            m_normalizedProgress = normalized;

            Vector2 scrollRectValue = _scrollRect.normalizedPosition;
            switch (_dragContentScrollDirection)
            {
                case ScrollDirection.Horizontal_LeftToRight:
                    scrollRectValue.x = m_normalizedProgress;
                    break;
                case ScrollDirection.Horizontal_RightToLeft:
                    scrollRectValue.x = 1f - m_normalizedProgress;
                    break;
                case ScrollDirection.Vertical_UpToDown:
                    scrollRectValue.y = 1f - m_normalizedProgress;
                    break;
                case ScrollDirection.Vertical_DownToUp:
                    scrollRectValue.y = m_normalizedProgress;
                    break;
                default:
                    break;
            }
            _scrollRect.normalizedPosition = scrollRectValue;
        }
    }
}