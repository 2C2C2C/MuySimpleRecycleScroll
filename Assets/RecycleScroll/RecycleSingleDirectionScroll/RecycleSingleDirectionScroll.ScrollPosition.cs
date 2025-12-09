using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extend;
using ScrollbarDirection = UnityEngine.UI.Scrollbar.Direction;
using System;

namespace RecycleScrollView
{
    public partial class RecycleSingleDirectionScroll
    {
        internal readonly struct ProgressResultPack
        {
            public readonly int elementIndex;
            public readonly float result;
            public ProgressResultPack(int i, float r)
            {
                elementIndex = i;
                result = r;
            }
        }

        private const float MIN_BAR_SIZE = 0.1f;

        [Header("ScrollBar params")]
        [SerializeField]
        private Scrollbar _scrollBar = null;

        [SerializeField]
        /// <summary> It is from 0 to 1 (As Scroll direction's head to tail) </summary>
        private float m_scrollProgress; // TODO Should be non-serialized but show in inspector
        [SerializeField]
        /// <summary> Damn, it is from 1 to 0 (As Scroll direction's head to tail) </summary>
        private float m_virtualNormalizedScrollBarValue; // TODO Should be non-serialized but show in inspector

        private int m_hasSetScrollBarValueThisFrame = 0;
        private List<ProgressResultPack> m_tempList = new List<ProgressResultPack>(20);
        private Comparison<ProgressResultPack> m_packSort = null;

        private void BindScrollBar()
        {
            if (null != _scrollBar)
            {
                _scrollBar.onValueChanged.AddListener(OnScrollBarValueChanged);
                _scrollBar.SetValueWithoutNotify(m_virtualNormalizedScrollBarValue = 1f - Mathf.Clamp01(m_scrollProgress));
            }
        }

        private void UnBindScrollBar()
        {
            if (null != _scrollBar)
            {
                _scrollBar.onValueChanged.RemoveListener(OnScrollBarValueChanged);
            }
        }

        private void ApplyLayoutSettingToScrollBar()
        {
            if (null != _scrollBar)
            {
                ScrollbarDirection barDirection = _scrollParam.scrollDirection switch
                {
                    ScrollDirection.Horizontal_LeftToRight => ScrollbarDirection.RightToLeft,
                    ScrollDirection.Horizontal_RightToLeft => ScrollbarDirection.LeftToRight,
                    ScrollDirection.Vertical_UpToDown => ScrollbarDirection.BottomToTop,
                    ScrollDirection.Vertical_DownToUp => ScrollbarDirection.TopToBottom,
                    _ => ScrollbarDirection.BottomToTop
                };
                _scrollBar.SetDirection(barDirection, false);
            }
        }

        private void AdjustScrollBarSize()
        {
            if (null == _scrollBar || !HasDataSource)
            {
                return;
            }

            // Adjust scroll bar size
            int dataCount = m_dataSource.DataElementCount;
            int currentShowingCount = m_currentUsingElements.Count;
            if (currentShowingCount >= dataCount)
            {
                _scrollBar.size = 1f;
            }
            else
            {
                float barSize = currentShowingCount / (float)dataCount;
                if (barSize < MIN_BAR_SIZE)
                {
                    barSize = MIN_BAR_SIZE;
                }
                _scrollBar.size = barSize;
            }
        }

        private void UpdateScrollProgress()
        {
            if (null == _scrollBar || !HasDataSource)
            {
                return;
            }

            int dataCount = m_dataSource.DataElementCount;
            int currentShowingCount = m_currentUsingElements.Count;
            if (2 > dataCount || currentShowingCount >= dataCount)
            {
                m_scrollProgress = 0f;
                _scrollBar.size = 1f;
                _scrollBar.SetValueWithoutNotify(m_virtualNormalizedScrollBarValue = 1f);
                return;
            }

            if (CalculateCurrentScrollProgress(out float scrollPogress) &&
                !Mathf.Approximately(scrollPogress, m_scrollProgress))
            {
                Log($"Sync scroll progress from {m_scrollProgress} to {scrollPogress} by scroll content.");
                m_scrollProgress = scrollPogress;
                float scrollBarValue = 1f - m_scrollProgress;
                _scrollBar.SetValueWithoutNotify(m_virtualNormalizedScrollBarValue = scrollBarValue);
            }
            else
            {
                if (scrollPogress < m_scrollProgress)
                {
                    CalculateCurrentScrollProgress(out float eee);
                }
            }
        }

        /// <param name="scrollProgress"> 0 ~ 1 (head to tail)</param>
        private bool TryGetRefElementFormScrollProgress(float scrollProgress, out int elementIndex, out float normalizedScrollProgressBase, out float normalizedScrollProgressOffset)
        {
            if (null != m_dataSource)
            {
                int dataCount = m_dataSource.DataElementCount;
                if (Mathf.Approximately(0f, scrollProgress))
                {
                    elementIndex = 0;
                    normalizedScrollProgressOffset = 0f;
                    normalizedScrollProgressBase = 0f;
                }
                else if (Mathf.Approximately(1f, scrollProgress))
                {
                    elementIndex = dataCount - 1;
                    normalizedScrollProgressBase = 1f;
                    normalizedScrollProgressOffset = 0f;
                }
                else
                {
                    float stepSize = 1f / (dataCount - 1);
                    int stepLowBoundElementIndex = Mathf.FloorToInt(scrollProgress / stepSize);
                    normalizedScrollProgressBase = stepLowBoundElementIndex * stepSize;
                    elementIndex = stepLowBoundElementIndex;
                    normalizedScrollProgressOffset = scrollProgress - normalizedScrollProgressBase;
                }
                return true;
            }

            elementIndex = -1;
            normalizedScrollProgressBase = 0f;
            normalizedScrollProgressOffset = 0f;
            return false;
        }

        /// <returns> Nomralized value (0~1) </returns>
        private bool CalculateCurrentScrollProgress(out float result)
        {
            if (null == m_dataSource)
            {
                result = 0f;
                return false;
            }

            int elementCount = m_currentUsingElements.Count;
            bool canCalculatValidPos = false;
            // string debugMsg = " \n";
            for (int i = 0; i < elementCount; i++)
            {
                RecycleSingleDirectionScrollElement element = m_currentUsingElements[i];
                canCalculatValidPos = TryCalculateScrollProgressFromElement(element, out float expectedNormalizedBasePosition, out float finalizedPosition);
                // debugMsg += $"Element_{element.ElementIndex}_{canCalculatValidPos}; expectedNormalizedBasePosition {expectedNormalizedBasePosition}; finalizedPosition {finalizedPosition} \n";
                if (canCalculatValidPos)
                {
                    m_tempList.Add(new ProgressResultPack(element.ElementIndex, finalizedPosition));
                }
            }
            // Log(debugMsg);

            if (0 == m_tempList.Count)
            {
                LogError($"Can not calculate progress.");
                result = m_scrollProgress;
                return false;
            }

            if (null == m_packSort)
            {
                // Should use elements the nearer to the head/ tail edge
                m_packSort = (x, y) =>
                {
                    int dataCount = m_dataSource.DataElementCount;
                    float half = (0 == dataCount % 2) ? dataCount / 2 - 0.5f : dataCount / 2;
                    float deltaX = Mathf.Abs(x.elementIndex - half);
                    float deltaY = Mathf.Abs(y.elementIndex - half);
                    return deltaY.CompareTo(deltaX);
                };
            }

            m_tempList.Sort(m_packSort);
            result = m_tempList[0].result;
            m_tempList.Clear();
            result = Mathf.Clamp01(result);
            return true;
        }

        /// <param name="elementIndex"></param>
        /// <returns> 0 ~ 1 (head ~ tail)</returns>
        private float CalculateExpectedPositionForData(int elementIndex)
        {
            int dataCount = m_dataSource.DataElementCount;
            float step = 1f / (dataCount - 1);
            float result = step * elementIndex;
            return result;
        }

        private bool TryCalculateScrollProgressFromElement(RecycleSingleDirectionScrollElement element, out float expectedNormalizedBaseProgress, out float finalizedNormalizedProgress)
        {
            finalizedNormalizedProgress = expectedNormalizedBaseProgress = 0f;
            if (null == m_dataSource)
            {
                return false;
            }

            int elementCount = m_dataSource.DataElementCount;
            int elementIndex = element.ElementIndex;
            float stepSize = 1f / (elementCount - 1);

            if (0 == elementIndex)
            {
                finalizedNormalizedProgress = expectedNormalizedBaseProgress = 0f;
            }
            else if (elementCount - 1 == elementIndex)
            {
                finalizedNormalizedProgress = expectedNormalizedBaseProgress = 1f;
            }
            else
            {
                finalizedNormalizedProgress = expectedNormalizedBaseProgress = stepSize * elementIndex;
            }

            Vector2 convertedNormalizedRectPosition = CalculateNormalizedRectPosition(finalizedNormalizedProgress);
            Vector2 elementTempLocalPositionInViewport = convertedNormalizedRectPosition;
            Vector3 tempV3 = RectTransformEx.TransformNormalizedRectPositionToWorldPosition(element.ElementTransform, elementTempLocalPositionInViewport);
            RectTransform viewport = _scrollRect.viewport;
            elementTempLocalPositionInViewport = viewport.InverseTransformPoint(tempV3);

            Vector2 viewportExpectedLocalPosition = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, convertedNormalizedRectPosition);
            float deltaToExpectedPosition = 0f;
            bool greaterIfPositive = false;

            if (IsHorizontal)
            {
                deltaToExpectedPosition = viewportExpectedLocalPosition.x - elementTempLocalPositionInViewport.x;
                // For LeftToRight, positive delta means "greater than base progress"
                // For RightToLeft, negative delta means "greater than base progress"
                greaterIfPositive = ScrollDirection.Horizontal_LeftToRight == _scrollParam.scrollDirection;
            }
            else if (IsVertical)
            {
                deltaToExpectedPosition = viewportExpectedLocalPosition.y - elementTempLocalPositionInViewport.y;
                // For UpToDown, negative delta means "greater than base progress"
                // For DownToUp, positive delta means "greater than base progress"
                greaterIfPositive = ScrollDirection.Vertical_DownToUp == _scrollParam.scrollDirection;
            }

            if (Mathf.Approximately(0f, deltaToExpectedPosition))
            {
                return true;
            }

            // Greater than pre-calculated base position -> try gap to next element
            if ((greaterIfPositive && deltaToExpectedPosition > 0f) || (!greaterIfPositive && deltaToExpectedPosition < 0f))
            {
                if (TryCalculateGapBetween2Elements(elementIndex, elementIndex + 1, out float gapSize))
                {
                    float tempDelta = Mathf.Abs(deltaToExpectedPosition);
                    if (tempDelta <= gapSize)
                    {
                        float normalizedDelta = tempDelta / gapSize;
                        finalizedNormalizedProgress = expectedNormalizedBaseProgress + stepSize * normalizedDelta;
                        return true;
                    }
                    else
                    {
                        if (TryCalculateGapBetween2Elements(elementIndex + 1, elementIndex + 2, out float nextGapSize) && tempDelta - gapSize <= nextGapSize)
                        {
                            tempDelta -= gapSize;
                            tempDelta = stepSize * (tempDelta / nextGapSize);
                            expectedNormalizedBaseProgress += stepSize;
                            finalizedNormalizedProgress = expectedNormalizedBaseProgress + tempDelta;
                            return true;
                        }
                    }
                    return false;
                }
                else if (elementCount - 1 == elementIndex)
                {
                    finalizedNormalizedProgress = expectedNormalizedBaseProgress = 1f;
                    return true;
                }
                else
                {
                    LogError($"Wrong case"); // Should not get this case
                }
            }
            // Less than pre-calculated base position -> try gap to previous element
            else if ((greaterIfPositive && deltaToExpectedPosition < 0f) || (!greaterIfPositive && deltaToExpectedPosition > 0f))
            {
                if (TryCalculateGapBetween2Elements(elementIndex - 1, elementIndex, out float gapSize))
                {
                    float tempDelta = Mathf.Abs(deltaToExpectedPosition);
                    if (tempDelta <= gapSize)
                    {
                        finalizedNormalizedProgress = expectedNormalizedBaseProgress - stepSize * Mathf.Abs(deltaToExpectedPosition / gapSize);
                        return true;
                    }
                    else
                    {
                        // Log($"Check {elementIndex} {expectedNormalizedBasePosition} {deltaToExpectedPosition}");
                        if (TryCalculateGapBetween2Elements(elementIndex - 2, elementIndex - 1, out float nextGapSize) &&
                            tempDelta - gapSize <= nextGapSize)
                        {
                            tempDelta -= gapSize;
                            tempDelta = stepSize * (tempDelta / nextGapSize);
                            expectedNormalizedBaseProgress -= stepSize;
                            finalizedNormalizedProgress = expectedNormalizedBaseProgress - tempDelta;
                            return true;
                        }
                    }
                }
                else if (0 == elementIndex)
                {
                    finalizedNormalizedProgress = expectedNormalizedBaseProgress = 0f;
                    return true;
                }
                else
                {
                    LogError($"Wrong case"); // Should not get this case
                }
            }
            return false;
        }

        /// <param name="normalizedPosition"> 1 ~ 0 (1 means at the start)</param>
        private bool TryApplyNormalizedPosition(float normalizedPosition)
        {
            if (TryGetRefElementFormScrollProgress(normalizedPosition, out int refElementIndex, out float normalizedScrollProgressBase, out float normalizedScrollProgressOffset))
            {
                JumpToElementInstant(refElementIndex, normalizedScrollProgressBase, normalizedScrollProgressOffset);
                return true;
            }
            return false;
        }

        private void OnScrollBarValueChanged(float scrollbarValue)
        {
            float clampedBarValue = Mathf.Clamp01(scrollbarValue);
            float normalizedProgress = 1f - clampedBarValue;

            m_hasSetScrollBarValueThisFrame = 1;
            if (Mathf.Approximately(clampedBarValue, m_virtualNormalizedScrollBarValue))
            {
                _scrollBar.SetValueWithoutNotify(m_virtualNormalizedScrollBarValue);
                return;
            }

            if (TryApplyNormalizedPosition(normalizedProgress))
            {
                m_hasAdjustElementsCurrentFrame = true;
                Log($"Apply scroll progress from {m_scrollProgress} to {normalizedProgress} by scrollbar");
                m_scrollProgress = normalizedProgress;
                _scrollBar.SetValueWithoutNotify(m_virtualNormalizedScrollBarValue = clampedBarValue);
            }
            else
            {
                LogError($"Apply scroll progress from {m_scrollProgress} to {normalizedProgress} by scrollbar FAIL!!!");
                _scrollBar.SetValueWithoutNotify(m_virtualNormalizedScrollBarValue);
            }
        }

    }
}