using UnityEngine;
using UnityEngine.UI.Extend;
using ScrollDirection = RecycleScrollView.SingleDirectionScrollParam.ScrollDirection;

namespace RecycleScrollView
{
    public partial class RecycleSingleDirectionScroll
    {
        [System.Serializable]
        public struct SingleScrollElementNavigationParams
        {
            public float normalizedPositionInViewPort;
            public float normalizedElementPositionAdjustment;
        }

        [Header("Navigation params")]
        [SerializeField]
        private SingleScrollElementNavigationParams _defaultNavigationParams;

        public void JumpToElementInstant(int dataIndex)
        {
            JumpToElementInstant(ElementIndexDataIndex2WayConvert(dataIndex), _defaultNavigationParams);
        }

        public void JumpToElementInstant(int elementIndex, SingleScrollElementNavigationParams navigationParams)
        {
            if (null == m_dataSource || elementIndex < 0 || elementIndex >= m_dataSource.DataElementCount)
            {
                return;
            }

            RectTransform content = _scrollRect.content;
            RectTransform viewport = _scrollRect.viewport;
            if (TryGetShowingElement(elementIndex, out RecycleSingleDirectionScrollElement element))
            {
                Vector2 delta = Vector2.zero;
                if (IsHorizontal)
                {
                    Vector2 verticalPostion = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, new Vector2(0f, navigationParams.normalizedPositionInViewPort));
                    Vector2 elementPosition = RectTransformEx.TransformNormalizedRectPositionToWorldPosition(element.ElementTransform, new Vector2(0f, navigationParams.normalizedElementPositionAdjustment));
                    elementPosition = viewport.InverseTransformPoint(elementPosition);
                    delta = verticalPostion - elementPosition;
                    // LogError($"elementPosition_{elementPosition} -> verticalPostion_{verticalPostion}");
                }
                else if (IsVertical)
                {
                    Vector2 horizontalPosition = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, new Vector2(navigationParams.normalizedPositionInViewPort, 0f));
                    Vector2 elementPosition = RectTransformEx.TransformNormalizedRectPositionToWorldPosition(element.ElementTransform, new Vector2(navigationParams.normalizedElementPositionAdjustment, 0f));
                    elementPosition = viewport.InverseTransformPoint(elementPosition);
                    delta = horizontalPosition - elementPosition;
                    // LogError($"elementPosition_{elementPosition} -> horizontalPosition_{horizontalPosition}");
                }
                Vector2 localPosition = content.localPosition;
                localPosition += delta;
                content.localPosition = localPosition;
                ForceRebuildAndStopMove();
                return;
            }

            RemoveAllCurrentElements();
            _scrollRect.StopMovement();

            RecycleSingleDirectionScrollElement targetElement = InternalAddElement(elementIndex);
            InternalChangeElementIndex(element, elementIndex, true);
            m_currentUsingElements.Add(targetElement);
            Vector2 targetElementSize = targetElement.ElementPreferredSize;

            // HACK Since the pivot of content must be fixed, we need to adjust the position of content to make the target element at the correct position
            Vector2 headCheckRectPosition = Vector2.zero;
            if (IsVertical)
            {
                Vector2 verticalPostion = RectTransformEx.CalulateRectPosition(viewport, new Vector2(0.5f, navigationParams.normalizedPositionInViewPort));
                // Content pivot is (0.5, 0) (true _scrollParam.reverseArrangement) ; Content pivot is (0.5, 1) (false _scrollParam.reverseArrangement)
                if (ScrollDirection.Vertical_UpToDown == _scrollParam.scrollDirection)
                {
                    verticalPostion.y += navigationParams.normalizedElementPositionAdjustment * targetElementSize.y;
                }
                else // ScrollDirection.Vertical_DownToUp
                {
                    verticalPostion.y -= (1f - navigationParams.normalizedElementPositionAdjustment) * targetElementSize.y;
                }
                headCheckRectPosition = verticalPostion;
                Vector3 localPosition = RectTransformEx.TransformRectPositionToLocalPosition(viewport, verticalPostion);
                content.localPosition = localPosition;
            }
            else if (IsHorizontal)
            {
                Vector2 horizontalPostion = RectTransformEx.CalulateRectPosition(viewport, new Vector2(navigationParams.normalizedPositionInViewPort, 0.5f));
                // Content pivot is (0, 0.5) (false _scrollParam.reverseArrangement) ; Content pivot is (1, 0.5) (true _scrollParam.reverseArrangement)
                if (ScrollDirection.Horizontal_LeftToRight == _scrollParam.scrollDirection)
                {
                    horizontalPostion.x -= navigationParams.normalizedElementPositionAdjustment * targetElementSize.x;
                }
                else // ScrollDirection.Horizontal_RightToLeft
                {
                    horizontalPostion.x += (1f - navigationParams.normalizedElementPositionAdjustment) * targetElementSize.x;
                }
                headCheckRectPosition = horizontalPostion;
                Vector3 localPosition = RectTransformEx.TransformRectPositionToLocalPosition(viewport, horizontalPostion);
                content.localPosition = localPosition;
            }

            // Add elements to fill the view port
            Vector2 viewportSize = viewport.rect.size;
            Vector2 headRectPosition = CalculateNormalizedRectPosition(0f);
            headRectPosition = new Vector2(viewportSize.x * headRectPosition.x, viewportSize.y * headRectPosition.y);
            int canAddIndex;
            float spacing = _scrollParam.spacing;
            while (-1 != (canAddIndex = CalculateAvailabeNextHeadElementIndex()))
            {
                AddElementToHead(canAddIndex);
                Vector2 size = m_currentUsingElements[0].ElementPreferredSize;
                bool doBreak = false;
                switch (_scrollParam.scrollDirection)
                {
                    // Vertical
                    case ScrollDirection.Vertical_UpToDown:
                        headCheckRectPosition += Vector2.up * (size.y + spacing);
                        content.localPosition += Vector3.up * (size.y + spacing);
                        doBreak = headCheckRectPosition.y > headRectPosition.y;
                        break;
                    case ScrollDirection.Vertical_DownToUp:
                        headCheckRectPosition += Vector2.down * (size.y + spacing);
                        content.localPosition += Vector3.down * (size.y + spacing);
                        doBreak = headCheckRectPosition.y < headRectPosition.y;
                        break;

                    // Horizontal
                    case ScrollDirection.Horizontal_LeftToRight:
                        headCheckRectPosition += Vector2.left * (size.x + spacing);
                        content.localPosition += Vector3.left * (size.x + spacing);
                        doBreak = headCheckRectPosition.x < headRectPosition.x;
                        break;
                    case ScrollDirection.Horizontal_RightToLeft:
                        headCheckRectPosition += Vector2.right * (size.x + spacing);
                        content.localPosition += Vector3.right * (size.x + spacing);
                        doBreak = headCheckRectPosition.x > headRectPosition.x;
                        break;
                    default:
                        break;
                }

                if (doBreak)
                {
                    break;
                }
            }

            InternalAdjustment();
            ForceRebuildAndStopMove();
        }

        public void JumpToElementInstant(int elementIndex, float normalizedScrollProgressBase, float normalizedScrollProgressOffset)
        {
            if (null == m_dataSource || elementIndex < 0 || elementIndex >= m_dataSource.DataElementCount)
            {
                return;
            }

            if (!TryGetRefElementFormScrollProgress(m_scrollProgress, out int currentBaseIndex, out float _, out _))
            {
                return;
            }

            if (TryGetShowingElement(elementIndex, out _) && TryGetShowingElement(currentBaseIndex, out _))
            {
                // Target element is current showing and also in the viewport
                InternalJumpToExistElement(elementIndex, normalizedScrollProgressBase, normalizedScrollProgressOffset);
            }
            else
            {
                InternalJumpToNonExistElement(elementIndex, normalizedScrollProgressBase, normalizedScrollProgressOffset);
            }

            int indexForPreCache = CalculateAvailabeNextHeadElementIndex();
            SetPreCacheElement(indexForPreCache, ref m_preCacheHeadElement);
            indexForPreCache = CalculateAvailabeNextTailElementIndex();
            SetPreCacheElement(indexForPreCache, ref m_preCacheTailElement);

            InternalAdjustment();
            ForceRebuildAndStopMove();
        }

        private void InternalJumpToExistElement(int elementIndex, float normalizedScrollProgressBase, float normalizedScrollProgressOffset)
        {
            Log($"InternalJumpToExistElement elementIndex_{elementIndex}; normalizedScrollProgressBase_{normalizedScrollProgressBase}; normalizedScrollProgressOffset_{normalizedScrollProgressOffset} || Frame:{Time.frameCount}");
            if (null == m_dataSource || elementIndex < 0 || elementIndex >= m_dataSource.DataElementCount)
            {
                return;
            }

            RectTransform content = _scrollRect.content;
            int dataCount = m_dataSource.DataElementCount;
            if (!TryGetRefElementFormScrollProgress(m_scrollProgress, out int currentBaseIndex, out float currentNormalizedProgressBase, out float currentNormalizedProgressOffset))
            {
                return;
            }

            float tempMove = 0f;
            float stepSize = 1f / (m_dataSource.DataElementCount - 1);
            if (currentBaseIndex == elementIndex)
            {
                tempMove = normalizedScrollProgressOffset - currentNormalizedProgressOffset;
                if (0f < tempMove)
                {
                    if (TryCalculateGapBetween2Elements(elementIndex, elementIndex + 1, out float gapSize))
                    {
                        tempMove = tempMove / stepSize * gapSize;
                    }
                    else
                    {
                        LogError($"Error case");
                    }
                }
                else if (0f > tempMove)
                {
                    if (TryCalculateGapBetween2Elements(elementIndex - 1, elementIndex, out float gapSize) || TryCalculateGapBetween2Elements(elementIndex, elementIndex + 1, out gapSize))
                    {
                        tempMove = tempMove / stepSize * gapSize;
                    }
                    else
                    {
                        LogError($"Error case");
                    }
                }
                else // 0f == tempMove // no need move
                {
                    return;
                }
            }
            else
            {
                int tempIndex = currentBaseIndex;
                float gapSize = 0f;
                // Clear current offset
                if (0f > currentNormalizedProgressOffset)
                {
                    // Progress is not reach base position yet
                    SetPreCacheElement(tempIndex - 1, ref m_preCacheHeadElement);
                    if (TryCalculateGapBetween2Elements(tempIndex - 1, tempIndex, out gapSize))
                    {
                        tempIndex--;
                        tempMove -= (stepSize + currentNormalizedProgressOffset) * gapSize;
                    }
                    else
                    {
                        LogError($"Error case");
                    }
                }
                else if (0f < currentNormalizedProgressOffset)
                {
                    // Progress is beyound base position
                    SetPreCacheElement(tempIndex + 1, ref m_preCacheTailElement);
                    if (TryCalculateGapBetween2Elements(tempIndex, tempIndex + 1, out gapSize))
                    {
                        tempMove -= currentNormalizedProgressOffset / stepSize * gapSize;
                    }
                    else
                    {
                        LogError($"Error case");
                    }
                }

                // LogError($"Set progress {m_scrollProgress}->{targetProgress} || Frame:{Time.frameCount}");
                // LogError($"Set base element {currentBaseIndex}->{elementIndex} || Frame:{Time.frameCount}");
                while (tempIndex < elementIndex)
                {
                    SetPreCacheElement(tempIndex + 1, ref m_preCacheTailElement);
                    if (TryCalculateGapBetween2Elements(tempIndex, tempIndex + 1, out gapSize))
                    {
                        tempMove += gapSize;
                        tempIndex++;
                    }
                    else
                    {
                        break; //
                    }
                }
                while (tempIndex > elementIndex)
                {
                    SetPreCacheElement(tempIndex - 1, ref m_preCacheHeadElement);
                    if (TryCalculateGapBetween2Elements(tempIndex - 1, tempIndex, out gapSize))
                    {
                        tempMove -= gapSize;
                        tempIndex--;
                    }
                    else
                    {
                        break; // 
                    }
                }

                int preCacheIndex = CalculateAvailabeNextHeadElementIndex();
                SetPreCacheElement(preCacheIndex, ref m_preCacheHeadElement);
                preCacheIndex = CalculateAvailabeNextTailElementIndex();
                SetPreCacheElement(preCacheIndex, ref m_preCacheTailElement);

                if (0f > normalizedScrollProgressOffset)
                {
                    if (0 >= elementIndex - 1)
                    {
                        tempMove += gapSize * (normalizedScrollProgressOffset / stepSize);
                    }
                    else if (TryCalculateGapBetween2Elements(elementIndex - 1, elementIndex, out gapSize))
                    {
                        tempMove -= gapSize * (normalizedScrollProgressOffset / stepSize);
                    }
                    else
                    {
                        LogError($"Error case"); // TODO
                    }
                }
                else if (0f < normalizedScrollProgressOffset)
                {
                    if (dataCount - 1 <= elementIndex)
                    {
                        tempMove -= gapSize * (normalizedScrollProgressOffset / stepSize);
                    }
                    else if (TryCalculateGapBetween2Elements(elementIndex, elementIndex + 1, out gapSize))
                    {
                        tempMove += gapSize * (normalizedScrollProgressOffset / stepSize);
                    }
                    else
                    {
                        LogError($"Error case"); // TODO
                    }
                }
            }

            Vector2 move = GetScrollDirectionVector(_scrollParam.scrollDirection);
            move *= -tempMove; // HACK the actual content move direction is inverse of scroll direction
            Vector3 localPosition = content.localPosition;
            localPosition += (Vector3)move;

            localPosition = ClampLocalPosForHead(localPosition);
            localPosition = ClampLocalPosForTail(localPosition);
            content.localPosition = localPosition;
            ForceRebuildContentLayout();
        }

        private void InternalJumpToNonExistElement(int elementIndex, float normalizedScrollProgressBase, float normalizedScrollProgressOffset)
        {
            // Log($"InternalJumpToNonExistElement elementIndex_{elementIndex}; normalizedScrollProgressBase_{normalizedScrollProgressBase}; normalizedScrollProgressOffset_{normalizedScrollProgressOffset} || Frame:{Time.frameCount}");
            RectTransform content = _scrollRect.content;
            RectTransform viewport = _scrollRect.viewport;

            RemoveAllCurrentElements();
            _scrollRect.StopMovement();
            AddElementToHead(elementIndex);
            SetPreCacheElement(elementIndex + 1, ref m_preCacheTailElement);
            RecycleSingleDirectionScrollElement targetElement = m_currentUsingElements[0];

            // Get the local position of directly place the element to the progress position line
            Vector2 headCheckRectPosition = Vector2.zero;
            Vector3 localPosition = content.localPosition;
            Vector2 convertedRectPosition = CalculateNormalizedRectPosition(normalizedScrollProgressBase);
            if (IsVertical)
            {
                Vector2 verticalPostion = RectTransformEx.CalulateRectPosition(viewport, new Vector2(0.5f, convertedRectPosition.y));
                if (ScrollDirection.Vertical_UpToDown == _scrollParam.scrollDirection)
                {
                    verticalPostion.y += targetElement.ElementPreferredSize.y * (1f - convertedRectPosition.y);
                }
                else // DownToUp
                {
                    verticalPostion.y -= targetElement.ElementPreferredSize.y * convertedRectPosition.y;
                }
                headCheckRectPosition = verticalPostion;
                Vector2 convertedLocalPos = RectTransformEx.TransformRectPositionToLocalPosition(viewport, verticalPostion);
                localPosition.y = convertedLocalPos.y;
            }
            else if (IsHorizontal)
            {
                Vector2 horizontalPostion = RectTransformEx.CalulateRectPosition(viewport, new Vector2(convertedRectPosition.x, 0.5f));
                if (ScrollDirection.Horizontal_LeftToRight == _scrollParam.scrollDirection)
                {
                    horizontalPostion.x -= targetElement.ElementPreferredSize.x * convertedRectPosition.x;
                }
                else // RightToLeft
                {
                    horizontalPostion.x += targetElement.ElementPreferredSize.x * (1f - convertedRectPosition.x);
                }
                headCheckRectPosition = horizontalPostion;
                Vector2 convertedLocalPos = RectTransformEx.TransformRectPositionToLocalPosition(viewport, horizontalPostion);
                localPosition.x = convertedLocalPos.x;
            }

            // TODO Add progress offset
            if (!Mathf.Approximately(0f, normalizedScrollProgressOffset))
            {
                Vector3 offsetV3 = Vector3.zero;
                float stepSize = 1f / (m_dataSource.DataElementCount - 1);
                float offset = normalizedScrollProgressOffset / stepSize;
                Vector2 contentMoveDir = -GetScrollDirectionVector(_scrollParam.scrollDirection); // HACK the actual content move direction is inverse of scroll directions
                if ((0f < offset && TryCalculateGapBetween2Elements(elementIndex, elementIndex + 1, out float gapSize)) ||
                    (0f > offset && TryCalculateGapBetween2Elements(elementIndex - 1, elementIndex, out gapSize)))
                {
                    offsetV3 = gapSize * offset * contentMoveDir;
                }
                headCheckRectPosition += (Vector2)offsetV3;
                localPosition += offsetV3;
                // PrintEdge();
            }

            // Add elements to fill the view port
            Vector2 viewportSize = viewport.rect.size;
            Vector2 headRectPosition = CalculateNormalizedRectPosition(0f);
            headRectPosition = new Vector2(viewportSize.x * headRectPosition.x, viewportSize.y * headRectPosition.y);
            int canAddIndex;
            float spacing = _scrollParam.spacing;
            while (-1 != (canAddIndex = CalculateAvailabeNextHeadElementIndex()))
            {
                AddElementToHead(canAddIndex);
                Vector2 size = m_currentUsingElements[0].ElementPreferredSize;
                bool doBreak = false;
                switch (_scrollParam.scrollDirection)
                {
                    // Vertical
                    case ScrollDirection.Vertical_UpToDown:
                        headCheckRectPosition += Vector2.up * (size.y + spacing);
                        localPosition += Vector3.up * (size.y + spacing);
                        doBreak = headCheckRectPosition.y > headRectPosition.y;
                        break;
                    case ScrollDirection.Vertical_DownToUp:
                        headCheckRectPosition += Vector2.down * (size.y + spacing);
                        localPosition += Vector3.down * (size.y + spacing);
                        doBreak = headCheckRectPosition.y < headRectPosition.y;
                        break;

                    // Horizontal
                    case ScrollDirection.Horizontal_LeftToRight:
                        headCheckRectPosition += Vector2.left * (size.x + spacing);
                        localPosition += Vector3.left * (size.x + spacing);
                        doBreak = headCheckRectPosition.x < headRectPosition.x;
                        break;
                    case ScrollDirection.Horizontal_RightToLeft:
                        headCheckRectPosition += Vector2.right * (size.x + spacing);
                        localPosition += Vector3.right * (size.x + spacing);
                        doBreak = headCheckRectPosition.x > headRectPosition.x;
                        break;
                    default:
                        break;
                }

                if (doBreak)
                {
                    break;
                }
            }

            localPosition = ClampLocalPosForHead(localPosition);
            localPosition = ClampLocalPosForTail(localPosition);
            content.localPosition = localPosition;
            InternalAdjustment();
            ForceRebuildAndStopMove();
        }

        private void ForceRebuildAndStopMove()
        {
            ForceRebuildContentLayout();
            _scrollRect.StopMovement();
            _scrollRect.CallUpdateBoundsAndPrevData();
        }

    }
}