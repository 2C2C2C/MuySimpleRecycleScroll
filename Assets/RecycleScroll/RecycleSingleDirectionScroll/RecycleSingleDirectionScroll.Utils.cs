using UnityEngine;
using UnityEngine.UI.Extend;
using ScrollDirection = RecycleScrollView.SingleDirectionScrollParam.ScrollDirection;

namespace RecycleScrollView
{
    public partial class RecycleSingleDirectionScroll
    {
        /// <param name="elementIndex"> Index from scroll progress </param>
        /// <param name="element"></param>
        /// <returns></returns>
        private bool TryGetShowingElement(int elementIndex, out RecycleSingleDirectionScrollElement element)
        {
            for (int i = 0, length = m_currentUsingElements.Count; i < length; i++)
            {
                if (m_currentUsingElements[i].ElementIndex == elementIndex)
                {
                    element = m_currentUsingElements[i];
                    return true;
                }
            }
            element = null;
            return false;
        }

        private float CalculateCurrentContentTotalPreferredSize(int indexOfUsingElements = -1)
        {
            float totalSize = 0f;
            int length = m_currentUsingElements.Count;
            for (int i = 0; i < length; i++)
            {
                if (-1 != indexOfUsingElements && i == indexOfUsingElements)
                {
                    break;
                }

                RecycleSingleDirectionScrollElement element = m_currentUsingElements[i];
                if (IsVertical)
                {
                    totalSize += element.ElementPreferredSize.y;
                }
                else if (IsHorizontal)
                {
                    totalSize += element.ElementPreferredSize.x;
                }

                if (i > 0 && i < length - 1)
                {
                    totalSize += _scrollParam.spacing;
                }
            }
            return totalSize;
        }

        private bool TryCalculateGapBetweenElement(int lowElementIndex, int highElementIndex, out float gapSize)
        {
            if (null == m_dataSource || lowElementIndex >= highElementIndex)
            {
                gapSize = 0f;
                return false;
            }

            int dataCount = m_dataSource.DataElementCount;
            if (lowElementIndex < 0 || highElementIndex < 0 || dataCount - 1 < lowElementIndex || dataCount - 1 < highElementIndex)
            {
                gapSize = 0f;
                return false;
            }

            if (!TryGetShowingElement(lowElementIndex, out RecycleSingleDirectionScrollElement lowElement))
            {
                if (null != m_preCacheHeadElement && m_preCacheHeadElement.ElementIndex == lowElementIndex)
                {
                    lowElement = m_preCacheHeadElement;
                }
            }
            if (!TryGetShowingElement(highElementIndex, out RecycleSingleDirectionScrollElement highElement))
            {
                if (null != m_preCacheTailElement && m_preCacheTailElement.ElementIndex == highElementIndex)
                {
                    highElement = m_preCacheTailElement;
                }
            }

            if (null != lowElement && null != highElement)
            {
                float lowBoundPosition = CalculateExpectedPositionForData(lowElementIndex);
                Vector2 lowElementSize = lowElement.ElementPreferredSize;
                float hightBoundPosition = CalculateExpectedPositionForData(highElementIndex);
                Vector2 highElementSize = highElement.ElementPreferredSize;

                // From low element to high element
                if (IsVertical)
                {
                    gapSize = (lowElementSize.y * (1f - lowBoundPosition)) + (highElementSize.y * hightBoundPosition);
                }
                else if (IsHorizontal)
                {
                    gapSize = (lowElementSize.x * (1f - lowBoundPosition)) + (highElementSize.x * hightBoundPosition);
                }
                else
                {
                    gapSize = 0f;
                    return false;
                }
                gapSize += _scrollParam.spacing;
                return true;
            }
            gapSize = 0f;
            return false;
        }

        // To solve reverse arrangement issues
        private int ElementIndexDataIndex2WayConvert(int index)
        {
            if (null == m_dataSource)
            {
                return -1;
            }
            int dataCount = m_dataSource.DataElementCount;
            int result = _scrollParam.reverseArrangement ?
                dataCount - index - 1 :
                index;
            return result;
        }

        /// <summary> The element index of the element for adding head </summary>
        /// <returns> -1 Means it can not find valid index </returns>
        private int CalculateAvailabeNextHeadElementIndex()
        {
            if (null == m_dataSource || 0 == m_dataSource.DataElementCount)
            {
                return -1;
            }
            if (0 == m_currentUsingElements.Count)
            {
                return 0;
            }

            int index = m_currentUsingElements[0].ElementIndex;
            if (0 >= index)
            {
                return -1;
            }
            return index - 1;
        }

        /// <summary> The element index of the element for adding tail </summary>
        /// <returns> -1 Means it can not find valid index </returns>
        private int CalculateAvailabeNextTailElementIndex()
        {
            if (null == m_dataSource || 0 == m_dataSource.DataElementCount)
            {
                return -1;
            }
            if (0 == m_currentUsingElements.Count)
            {
                return 0;
            }

            int dataCount = m_dataSource.DataElementCount;
            int index = m_currentUsingElements[m_currentUsingElements.Count - 1].ElementIndex;
            if (dataCount - 1 <= index)
            {
                return -1;
            }
            return index + 1;
        }

        public int GetCurrentShowingElementIndexLowerBound()
        {
            int elementCount = m_currentUsingElements.Count;
            return (0 < elementCount) ? m_currentUsingElements[elementCount - 1].ElementIndex : -1;
        }

        public int GetCurrentShowingElementIndexUpperBound()
        {
            int elementCount = m_currentUsingElements.Count;
            return (0 < elementCount) ? m_currentUsingElements[0].ElementIndex : -1;
        }

        private Vector2 GetScrollDirectionVector(ScrollDirection scrollDirection)
        {
            Vector2 result = scrollDirection switch
            {
                ScrollDirection.Vertical_UpToDown => Vector2.down,
                ScrollDirection.Vertical_DownToUp => Vector2.up,
                ScrollDirection.Horizontal_LeftToRight => Vector2.right,
                ScrollDirection.Horizontal_RightToLeft => Vector2.left,
                _ => Vector2.zero,
            };
            return result;
        }

        // Only use this for edge elements
        private Vector3 ClampLocalPosInForHead(Vector3 position)
        {
            Vector3 result = position;
            if (-1 == CalculateAvailabeNextHeadElementIndex())
            {
                RectTransform viewport = _scrollRect.viewport;
                Vector2 edgeHead = CalculateNormalizedRectPosition(0f);
                Vector3 edgeHeadLocalPos = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, edgeHead);

                switch (_scrollParam.scrollDirection)
                {
                    case ScrollDirection.Vertical_UpToDown:
                        result.y = Mathf.Clamp(result.y, edgeHeadLocalPos.y, float.MaxValue);
                        break;
                    case ScrollDirection.Vertical_DownToUp:
                        result.y = Mathf.Clamp(result.y, float.MinValue, edgeHeadLocalPos.y);
                        break;
                    case ScrollDirection.Horizontal_LeftToRight:
                        result.x = Mathf.Clamp(result.x, float.MinValue, edgeHeadLocalPos.x);
                        break;
                    case ScrollDirection.Horizontal_RightToLeft:
                        result.x = Mathf.Clamp(result.x, edgeHeadLocalPos.x, float.MaxValue);
                        break;
                    default:
                        break;
                }
            }
            return result;
        }

        private Vector3 ClampLocalPosInForTail(Vector3 position)
        {
            Vector3 result = position;
            if (-1 == CalculateAvailabeNextTailElementIndex())
            {
                RectTransform viewport = _scrollRect.viewport;
                RectTransform content = _scrollRect.content;
                Vector2 edgeHead = CalculateNormalizedRectPosition(0f);
                Vector2 contentHeadRectPositionInViewport = RectTransformEx.TransformLocalPositionToRectPosition(viewport, viewport.InverseTransformPoint(RectTransformEx.TransformNormalizedRectPositionToWorldPosition(content, edgeHead)));
                Vector2 edgeTail = CalculateNormalizedRectPosition(1f);
                Vector2 viewportTailRectPosition = RectTransformEx.CalulateRectPosition(viewport, edgeTail);

                Vector2 scrollDirection = GetScrollDirectionVector(_scrollParam.scrollDirection);
                float contentSize = CalculateCurrentContentTotalPreferredSize();
                Vector2 contentTailRectPosition = contentHeadRectPositionInViewport + contentSize * scrollDirection;

                // HACK I force the content use fixed pivot
                float delta;
                switch (_scrollParam.scrollDirection)
                {
                    case ScrollDirection.Vertical_UpToDown:
                        delta = contentTailRectPosition.y - viewportTailRectPosition.y;
                        if (0f < delta)
                        {
                            result.y -= delta;
                        }
                        break;
                    case ScrollDirection.Vertical_DownToUp:
                        delta = contentTailRectPosition.y - viewportTailRectPosition.y;
                        if (0f < delta)
                        {
                            result.y -= delta;
                        }
                        break;
                    case ScrollDirection.Horizontal_LeftToRight:
                        delta = contentTailRectPosition.x - viewportTailRectPosition.x;
                        // UnityEngine.Debug.LogError($"{contentTailRectPosition.x} - {viewportTailRectPosition.x} = {delta}");
                        // if (0f > delta)
                        // {
                        //     result.x -= delta;
                        // }
                        break;
                    case ScrollDirection.Horizontal_RightToLeft:
                        delta = contentTailRectPosition.x - viewportTailRectPosition.x;
                        // UnityEngine.Debug.LogError($"{contentTailRectPosition.x} - {viewportTailRectPosition.x} = {delta}");
                        // if (0f > delta)
                        // {
                        //     result.x -= delta;
                        // }
                        break;
                    default:
                        break;
                }
            }
            return result;
        }

        // private void PrintEdge()
        // {
        //     RectTransform viewport = _scrollRect.viewport;
        //     Vector2 edgeHead = CalculateNormalizedRectPosition(0f);
        //     Vector2 edgeTail = CalculateNormalizedRectPosition(1f);
        //     Vector3 edgeHeadLocalPos = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, edgeHead);
        //     Vector3 edgeTailLocalPos = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, edgeTail);
        //     Debug.LogError($"Check edge local pos; Head {edgeHeadLocalPos}; Tail {edgeTailLocalPos}");
        // }

    }
}