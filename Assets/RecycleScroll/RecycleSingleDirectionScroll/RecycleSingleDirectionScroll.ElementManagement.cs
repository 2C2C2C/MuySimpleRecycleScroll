using UnityEngine;
using UnityEngine.UI.Extend;

namespace RecycleScrollView
{
    public partial class RecycleSingleDirectionScroll
    {
        private const int SIDE_STATUS_ENOUGH = 0;
        private const int SIDE_STATUS_NEEDADD = -1;
        private const int SIDE_STATUS_NEEDREMOVE = 1;

        private const float EDGE_HEAD = 0F;
        private const float EDGE_TAIL = 1F;

        private void AddElementToHead(int elementIndex)
        {
            RecycleSingleDirectionScrollElement newElement = InternalAddElement(elementIndex);
            m_currentUsingElements.Insert(0, newElement);
            InternalChangeElementIndex(newElement, elementIndex, true);
            newElement.ElementTransform.SetAsFirstSibling();

            // Set pre cache element
            int indexForPreCache = (0 < elementIndex) ? elementIndex - 1 : 0;
            SetPreCacheElement(indexForPreCache, ref m_preCacheHeadElement);
            // Log($"Add on top index {elementIndex} Time {Time.time}");
        }

        private void AddElementToTail(int elementIndex)
        {
            RecycleSingleDirectionScrollElement newElement = InternalAddElement(elementIndex);
            m_currentUsingElements.Add(newElement);
            InternalChangeElementIndex(newElement, elementIndex, true);
            newElement.ElementTransform.SetAsLastSibling();

            int dataCount = m_dataSource.DataElementCount;
            int indexForPreCache = (dataCount - 1 > elementIndex) ? elementIndex + 1 : elementIndex;
            SetPreCacheElement(indexForPreCache, ref m_preCacheTailElement);
            // Log($"Add on bottom index {elementIndex} Time {Time.time}");
        }

        private void RemoveElementFromHead()
        {
            RecycleSingleDirectionScrollElement element = m_currentUsingElements[0];
            if (null != m_preCacheHeadElement)
            {
                int dataIndex = element.DataIndex;
                InternalChangeElementIndex(m_preCacheHeadElement, dataIndex, true);
            }
            m_currentUsingElements.RemoveAt(0);
            InternalRemoveElement(element);
        }

        private void RemoveElementFromTail()
        {
            int elementIndex = m_currentUsingElements.Count - 1;
            RecycleSingleDirectionScrollElement element = m_currentUsingElements[elementIndex];
            if (null != m_preCacheTailElement)
            {
                int dataIndex = element.DataIndex;
                InternalChangeElementIndex(m_preCacheHeadElement, dataIndex, true);
            }
            m_currentUsingElements.RemoveAt(elementIndex);
            InternalRemoveElement(element);
        }

        /// <returns> -1 Need add, 0 Enough, 1 Need remove</returns>
        private int CheckHeadSideStatus()
        {
            if (null == m_dataSource)
            {
                return SIDE_STATUS_ENOUGH;
            }

            int elementCount = m_currentUsingElements.Count;
            if (0 == elementCount)
            {
                return SIDE_STATUS_ENOUGH; // HACK
            }

            ScrollDirection checkDirection = _scrollParam.scrollDirection;
            switch (checkDirection)
            {
                case ScrollDirection.Horizontal_LeftToRight:
                    checkDirection = ScrollDirection.Horizontal_RightToLeft;
                    break;
                case ScrollDirection.Horizontal_RightToLeft:
                    checkDirection = ScrollDirection.Horizontal_LeftToRight;
                    break;
                case ScrollDirection.Vertical_UpToDown:
                    checkDirection = ScrollDirection.Vertical_DownToUp;
                    break;
                case ScrollDirection.Vertical_DownToUp:
                    checkDirection = ScrollDirection.Vertical_UpToDown;
                    break;
                default:
                    break;
            }
            bool isBeyoudEdge = IsElementEdgeBeyoudViewportEdge(0, normalizedViewportEdgePosition: EDGE_HEAD, normalizedElementEdgePosition: EDGE_TAIL, checkDirection);
            if (isBeyoudEdge)
            {
                if (2 <= elementCount)
                {
                    isBeyoudEdge = IsElementEdgeBeyoudViewportEdge(1, normalizedViewportEdgePosition: EDGE_HEAD, normalizedElementEdgePosition: EDGE_TAIL, checkDirection);
                    if (isBeyoudEdge)
                    {
                        return SIDE_STATUS_NEEDREMOVE;
                    }
                }
            }
            else
            {
                return SIDE_STATUS_NEEDADD;
            }
            return SIDE_STATUS_ENOUGH;
        }

        /// <returns>-1 Need add, 0 Enough, 1 Need remove</returns>
        private int CheckTailSideStatus()
        {
            if (null == m_dataSource)
            {
                return SIDE_STATUS_ENOUGH;
            }

            int elementCount = m_currentUsingElements.Count;
            if (0 == elementCount)
            {
                if (m_dataSource.DataElementCount > 0)
                {
                    return SIDE_STATUS_NEEDADD; // HACK
                }
                else
                {
                    return SIDE_STATUS_ENOUGH; // HACK
                }
            }

            ScrollDirection checkDirection = _scrollParam.scrollDirection;
            bool isBeyoudEdge = IsElementEdgeBeyoudViewportEdge(elementCount - 1, normalizedViewportEdgePosition: EDGE_TAIL, normalizedElementEdgePosition: EDGE_HEAD, checkDirection);
            if (isBeyoudEdge)
            {
                if (2 <= elementCount)
                {
                    isBeyoudEdge = IsElementEdgeBeyoudViewportEdge(elementCount - 2, normalizedViewportEdgePosition: EDGE_TAIL, normalizedElementEdgePosition: EDGE_HEAD, checkDirection);
                    if (isBeyoudEdge)
                    {
                        return SIDE_STATUS_NEEDREMOVE;
                    }
                }
            }
            else
            {
                return SIDE_STATUS_NEEDADD;
            }
            return SIDE_STATUS_ENOUGH;
        }

        /// <param name="indexOfUsingElements"> Index in the list of current in using elements </param>
        /// <param name="normalizedElementEdgePosition"> Head(0) ~ Tail(1) </param>
        /// <param name="normalizedViewportEdgePosition"> Head(0) ~ Tail(1) </param>
        /// <returns></returns>
        private bool IsElementEdgeBeyoudViewportEdge(int indexOfUsingElements, float normalizedViewportEdgePosition, float normalizedElementEdgePosition, ScrollDirection checkDirection)
        {
            if (0 > indexOfUsingElements || indexOfUsingElements >= m_currentUsingElements.Count)
            {
                return false;
            }

            RectTransform viewport = _scrollRect.viewport;
            RectTransform content = _scrollRect.content;
            Vector2 viewportSize = viewport.rect.size;
            Vector2 viewportEdgeRectPosition = CalculateNormalizedRectPosition(normalizedViewportEdgePosition);
            viewportEdgeRectPosition = new Vector2(viewportSize.x * viewportEdgeRectPosition.x, viewportSize.y * viewportEdgeRectPosition.y);
            // ContentPivotRectPositionInViewport
            Vector2 baseRectPosition = RectTransformEx.TransformLocalPositionToRectPosition(viewport, content.localPosition);

            float tempSize = CalculateCurrentContentTotalPreferredSize(indexOfUsingElements);
            Vector2 elementEdgeRectPosition = CalculateNormalizedRectPosition(normalizedElementEdgePosition);
            RecycleSingleDirectionScrollElement element = m_currentUsingElements[indexOfUsingElements];
            float elementEdgePositionExtra = IsHorizontal ? element.ElementPreferredSize.x * elementEdgeRectPosition.x : element.ElementPreferredSize.y * elementEdgeRectPosition.y;
            tempSize += elementEdgePositionExtra;

            bool isBeyoudEdge = false;
            switch (_scrollParam.scrollDirection)
            {
                case ScrollDirection.Horizontal_LeftToRight:
                    baseRectPosition.x += tempSize;
                    break;
                case ScrollDirection.Horizontal_RightToLeft:
                    baseRectPosition.x -= tempSize;
                    break;

                case ScrollDirection.Vertical_UpToDown:
                    baseRectPosition.y -= tempSize;
                    break;
                case ScrollDirection.Vertical_DownToUp:
                    baseRectPosition.y += tempSize;
                    break;
                default:
                    break;
            }

            switch (checkDirection)
            {
                case ScrollDirection.Horizontal_LeftToRight:
                    isBeyoudEdge = baseRectPosition.x > viewportEdgeRectPosition.x;
                    break;
                case ScrollDirection.Horizontal_RightToLeft:
                    isBeyoudEdge = baseRectPosition.x < viewportEdgeRectPosition.x;
                    break;

                case ScrollDirection.Vertical_UpToDown:
                    isBeyoudEdge = baseRectPosition.y < viewportEdgeRectPosition.y;
                    break;
                case ScrollDirection.Vertical_DownToUp:
                    isBeyoudEdge = baseRectPosition.y > viewportEdgeRectPosition.y;
                    break;
                default:
                    break;
            }

            return isBeyoudEdge;
        }

        private bool RemoveElementsFromHeadIfNeed()
        {
            int prevElementCount = m_currentUsingElements.Count;
            if (0 < prevElementCount)
            {
                float removeSize = 0f;
                RectTransform content = _scrollRect.content;
                while (SIDE_STATUS_NEEDREMOVE == CheckHeadSideStatus() && -1 != CalculateAvailabeNextTailElementIndex())
                {
                    RecycleSingleDirectionScrollElement toRemove = m_currentUsingElements[0];
                    if (IsVertical)
                    {
                        removeSize = toRemove.ElementPreferredSize.y + _scrollParam.spacing;
                    }
                    else if (IsHorizontal)
                    {
                        removeSize = toRemove.ElementPreferredSize.x + _scrollParam.spacing;
                    }
                    RemoveElementFromHead();

                    switch (_scrollParam.scrollDirection)
                    {
                        case ScrollDirection.Horizontal_LeftToRight:
                            content.localPosition += Vector3.right * removeSize;
                            break;
                        case ScrollDirection.Horizontal_RightToLeft:
                            content.localPosition += Vector3.left * removeSize;
                            break;
                        default:
                            break;

                        case ScrollDirection.Vertical_UpToDown:
                            content.localPosition += Vector3.down * removeSize;
                            break;
                        case ScrollDirection.Vertical_DownToUp:
                            content.localPosition += Vector3.up * removeSize;
                            break;
                    }
                }
                return 0f < removeSize;
            }
            return false;
        }

        private bool RemoveElementsFromTailIfNeed()
        {
            bool hasRemoveElements = false;
            int prevElementCount = m_currentUsingElements.Count;
            if (0 < prevElementCount)
            {
                while (SIDE_STATUS_NEEDREMOVE == CheckTailSideStatus() && -1 != CalculateAvailabeNextHeadElementIndex())
                {
                    RemoveElementFromTail();
                    hasRemoveElements = true;
                }
                // HACK Since I force the pivot of content, no need to adjust position at this case
            }
            return hasRemoveElements;
        }

        private bool AddElementsToHeadIfNeed()
        {
            RectTransform content = _scrollRect.content;
            float addSize = 0f;
            int canAddIndex;
            while (SIDE_STATUS_NEEDADD == CheckHeadSideStatus() && -1 != (canAddIndex = CalculateAvailabeNextHeadElementIndex()))
            {
                AddElementToHead(canAddIndex);
                if (IsVertical)
                {
                    addSize += m_currentUsingElements[0].ElementPreferredSize.y + _scrollParam.spacing;
                }
                else if (IsHorizontal)
                {
                    addSize += m_currentUsingElements[0].ElementPreferredSize.x + _scrollParam.spacing;
                }

                // HACK Becuz I use a fixed pivot for content, so I can directly adjust local position
                switch (_scrollParam.scrollDirection)
                {
                    case ScrollDirection.Horizontal_LeftToRight:
                        content.localPosition += Vector3.left * addSize;
                        break;
                    case ScrollDirection.Horizontal_RightToLeft:
                        content.localPosition += Vector3.right * addSize;
                        break;

                    case ScrollDirection.Vertical_UpToDown:
                        content.localPosition += Vector3.up * addSize;
                        break;
                    case ScrollDirection.Vertical_DownToUp:
                        content.localPosition += Vector3.down * addSize;
                        break;
                    default:
                        break;
                }
            }

            return 0f < addSize;
        }

        private bool AddElementsToTailIfNeed()
        {
            int addCount = 0;
            while (SIDE_STATUS_NEEDADD == CheckTailSideStatus())
            {
                int canAddIndex = CalculateAvailabeNextTailElementIndex();
                if (-1 != canAddIndex)
                {
                    AddElementToTail(canAddIndex);
                    addCount++;
                }
                else
                {
                    break;
                }
                // HACK Since I force the pivot of content, no need to adjust position at this case
            }
            return 0 < addCount;
        }

        private void SetPreCacheElement(int elementIndex, ref RecycleSingleDirectionScrollElement element)
        {
            if (null != m_dataSource)
            {
                int dataCount = m_dataSource.DataElementCount;
                elementIndex = Mathf.Clamp(elementIndex, 0, dataCount - 1);

                if (null == element)
                {
                    element = InternalAddElement(elementIndex);
                    element.ElementTransform.SetParent(_preCacheContainer);
                    element.ClearPreferredSize();
                    element.CalculatePreferredSize();
                }
                else if (element.ElementIndex != elementIndex)
                {
                    InternalChangeElementIndex(element, elementIndex, true);
                }
                else
                {
                    return;
                }

#if UNITY_EDITOR
                ChangeObjectName_EditorOnly(element, elementIndex);
#endif
            }
        }

    }
}