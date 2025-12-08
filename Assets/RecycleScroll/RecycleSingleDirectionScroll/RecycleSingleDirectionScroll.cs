using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using ScrollDirection = RecycleScrollView.SingleDirectionScrollParam.ScrollDirection;

namespace RecycleScrollView
{
    [RequireComponent(typeof(UnityScrollRectExtended))]
    public partial class RecycleSingleDirectionScroll : MonoBehaviour, IRecycleScroll
    {
        [Header("Main params")]
        [SerializeField]
        private UnityScrollRectExtended _scrollRect;
        [SerializeField]
        private HorizontalOrVerticalLayoutGroup _contentLayoutGroup;

        [SerializeField]
        private SingleDirectionScrollParam _scrollParam; // Simple layout param

        [SerializeField] // HACK Cache the element dat used to calculate size
        private RectTransform _preCacheContainer;

        private RecycleSingleDirectionScrollElement m_preCacheHeadElement;
        private RecycleSingleDirectionScrollElement m_preCacheTailElement;

        private bool m_hasAdjustElementsCurrentFrame = false;
        private bool m_hasPositionChangeCurrentFrame = false;

        public bool IsVertical => _scrollParam.IsVertical;
        public bool IsHorizontal => _scrollParam.IsHorizontal;
        public bool IsReverseArrangement => _scrollParam.reverseArrangement;

        public bool HasDataSource => null != m_dataSource;

        /// <summary> Stores current using elements, ELEMENT INDEX from low to high</summary>
#if UNITY_EDITOR
        [SerializeField] // TODO Should not be serialized, but show in inspector
#endif
        private List<RecycleSingleDirectionScrollElement> m_currentUsingElements = new List<RecycleSingleDirectionScrollElement>();
        private IRecycleScrollDataSource m_dataSource;

        public IReadOnlyList<RecycleSingleDirectionScrollElement> CurrentUsingElements => m_currentUsingElements;
        private UnityAction<Vector2> m_onScrollPositionChanged;
        private Action m_onLateUpdated;
        private Action<int, int> m_onDataElementCountChanged;

        public void AddElementTotail()
        {
            NotifySelfDataCountChange(1);
            AddElementsToTailIfNeed();
        }

        public void AddElementsToTail(int count)
        {
            NotifySelfDataCountChange(count);
            AddElementsToTailIfNeed();
        }

        public void InsertElement(int dataIndex)
        {
            // HACK For insert case, just need to update the view or elements
            InsertElements(dataIndex, 1);
        }

        public void InsertElements(int dataIndex, int count)
        {
            if (0 == m_currentUsingElements.Count || 0 == count)
            {
                return; // Will handle this case in AdjustElements from LateUpdate
            }

            int prevDataCount = m_dataSource.DataElementCount - 1;
            int insertElementIndex = ElementIndexDataIndex2WayConvert(dataIndex, prevDataCount);
            if (_scrollParam.reverseArrangement)
            {
                // Convert to non-reverse case
                insertElementIndex -= count - 1;
            }

            int indexTailBound = GetCurrentShowingElementIndexTailBound();
            if (insertElementIndex <= indexTailBound) // Need to refresh view for current using elements
            {
                for (int i = 0, length = m_currentUsingElements.Count; i < length; i++)
                {
                    RecycleSingleDirectionScrollElement element = m_currentUsingElements[i];
                    if (element.DataIndex >= dataIndex)
                    {
                        // Refresh element
                        InternalChangeElementIndex(element, element.ElementIndex, true);
                    }
                }
            }
            NotifySelfDataCountChange(count);
        }

        public void InsertElements(IReadOnlyList<int> sortedDataIndexList)
        {
            if (0 == m_currentUsingElements.Count || 0 == sortedDataIndexList.Count)
            {
                return; // In LateUpdate it will automaticlly handle this
            }

            // HACK For insert case, just need to update the view or elements
            InsertElements(sortedDataIndexList[0], sortedDataIndexList.Count);
        }

        public void RemoveElement(int dataIndex)
        {
            RemoveElements(dataIndex, 1);
        }

        public void RemoveElements(int dataIndex, int count)
        {
            if (0 == m_currentUsingElements.Count)
            {
                return;
            }
            if (dataIndex + 1 - count < 0)
            {
                LogError($"Remove data From index {dataIndex} count {count} will caused out of range issue");
                return;
            }

            int currentDataCount = m_dataSource.DataElementCount;
            int prevDataCount = currentDataCount - count;
            int removeElementStartIndex = ElementIndexDataIndex2WayConvert(dataIndex, prevDataCount);
            if (_scrollParam.reverseArrangement)
            {
                // Convert to non-reverse case
                removeElementStartIndex -= count - 1;
            }

            int indexTailBound = GetCurrentShowingElementIndexTailBound();
            int fallbackElementIndex = 0;
            bool canApplyFallbackIndex = true;
            if (removeElementStartIndex <= indexTailBound)
            {
                for (int i = 0, length = m_currentUsingElements.Count; i < length; i++)
                {
                    RecycleSingleDirectionScrollElement element = m_currentUsingElements[i];
                    int elementIndex = element.ElementIndex;
                    int newElementIndex = elementIndex - count;
                    if (currentDataCount - 1 >= newElementIndex && 0 <= newElementIndex)
                    {
                        // Valid element
                        InternalChangeElementIndex(element, newElementIndex, true);
                        if (0 == i)
                        {
                            canApplyFallbackIndex = false;
                        }
                    }
                    else
                    {
                        if (currentDataCount - 1 >= fallbackElementIndex && canApplyFallbackIndex)
                        {
                            // Edge case
                            InternalChangeElementIndex(element, fallbackElementIndex, true);
                            ++fallbackElementIndex;
                        }
                        else
                        {
                            // Invalid element
                            InternalChangeElementIndex(element, INVALID_INDEX, false);
                        }
                    }
                }
            }

            // TODO Deal with invalid elements
            bool findValidElement;
            int index = m_currentUsingElements.Count - 1;
            do
            {
                RecycleSingleDirectionScrollElement element = m_currentUsingElements[index];
                findValidElement = INVALID_INDEX != element.ElementIndex;
                if (!findValidElement)
                {
                    m_currentUsingElements.RemoveAt(index);
                    InternalRemoveElement(element);
                    index--;
                }
            } while (!findValidElement || 0 == m_currentUsingElements.Count);

            int usingElementCount = m_currentUsingElements.Count;
            if (0 != usingElementCount)
            {
                SetPreCacheElement(CalculateAvailabeNextHeadElementIndex(), ref m_preCacheHeadElement);
                SetPreCacheElement(CalculateAvailabeNextTailElementIndex(), ref m_preCacheTailElement);
            }
            AdjustElementsIfNeed();
        }

        public void RemoveElements(IReadOnlyList<int> sortedDataIndexList)
        {
            RemoveElements(sortedDataIndexList[0], sortedDataIndexList.Count); // Hack
        }

        public void UpdateElement(int dataIndex)
        {
            int elementIndex = ElementIndexDataIndex2WayConvert(dataIndex);
            if (TryGetShowingElement(elementIndex, out RecycleSingleDirectionScrollElement element))
            {
                Vector2 prevSize = element.ElementPreferredSize;
                element.ClearPreferredSize();
                element.CalculatePreferredSize();
                Vector2 nextSize = element.ElementPreferredSize;
                if (prevSize == nextSize)
                {
                    return;
                }
                // IDK if it is necessary to adjust content position
            }
        }

        public void UnInit()
        {
            if (HasDataSource)
            {
                for (int i = 0, length = m_currentUsingElements.Count; i < length; i++)
                {
                    m_dataSource.ReturnElement(m_currentUsingElements[i].ElementTransform);
                }
                m_currentUsingElements.Clear();
                m_dataSource = null;
            }
        }

        public void Init(IRecycleScrollDataSource dataSource)
        {
            if (HasDataSource)
            {
                LogError($"Already register a datasource");
            }
            else
            {
                m_dataSource = dataSource;
                ApplyLayoutSetting();
                ApplyLayoutSettingToScrollBar();
                while (SIDE_STATUS_NEEDADD == CheckTailSideStatus())
                {
                    if (!AddElementsToTailIfNeed())
                    {
                        break;
                    }
                }

                int headElementIndex = CalculateAvailabeNextHeadElementIndex();
                SetPreCacheElement(headElementIndex, ref m_preCacheHeadElement);
                int tailElementIndex = CalculateAvailabeNextTailElementIndex();
                SetPreCacheElement(tailElementIndex, ref m_preCacheTailElement);
                _scrollRect.CallUpdateBoundsAndPrevData();
                OnDataElementCountChanged(0, m_dataSource.DataElementCount);
            }
        }

        private void RemoveAllCurrentElements()
        {
            for (int i = 0, length = m_currentUsingElements.Count; i < length; i++)
            {
                InternalRemoveElement(m_currentUsingElements[i]);
            }
            m_currentUsingElements.Clear();

            if (null != m_preCacheHeadElement)
            {
                m_dataSource.ReturnElement(m_preCacheHeadElement.ElementTransform);
                m_preCacheHeadElement = null;
            }
            if (null != m_preCacheTailElement)
            {
                m_dataSource.ReturnElement(m_preCacheTailElement.ElementTransform);
                m_preCacheTailElement = null;
            }
        }

        private void ApplyLayoutSetting()
        {
            RectTransform content = _scrollRect.content;
            _scrollRect.vertical = IsVertical;
            _scrollRect.horizontal = IsHorizontal;
            _contentLayoutGroup.spacing = _scrollParam.spacing;
            if (IsVertical)
            {
                _scrollRect.horizontal = false;
                if (_contentLayoutGroup is VerticalLayoutGroup)
                {
                    switch (_scrollParam.scrollDirection)
                    {
                        case ScrollDirection.Vertical_UpToDown:
                            content.pivot = new Vector2(0.5f, 1f);
                            _contentLayoutGroup.childAlignment = TextAnchor.UpperCenter;
                            _contentLayoutGroup.reverseArrangement = false;
                            break;
                        case ScrollDirection.Vertical_DownToUp:
                            content.pivot = new Vector2(0.5f, 0f);
                            _contentLayoutGroup.childAlignment = TextAnchor.LowerCenter;
                            _contentLayoutGroup.reverseArrangement = true;
                            break;
                    }
                }
                else
                {
                    LogError($"Vertical scroll need a VerticalLayoutGroup on content");
                }
            }
            else if (IsHorizontal)
            {
                if (_contentLayoutGroup is HorizontalLayoutGroup)
                {
                    switch (_scrollParam.scrollDirection)
                    {
                        case ScrollDirection.Horizontal_LeftToRight:
                            content.pivot = new Vector2(0f, 0.5f);
                            _contentLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
                            _contentLayoutGroup.reverseArrangement = false;
                            break;
                        case ScrollDirection.Horizontal_RightToLeft:
                            content.pivot = new Vector2(1f, 0.5f);
                            _contentLayoutGroup.childAlignment = TextAnchor.MiddleRight;
                            _contentLayoutGroup.reverseArrangement = true;
                            break;
                    }
                }
                else
                {
                    LogError($"Horizontal scroll need a HorizontalLayoutGroup on content");
                }
            }
        }

        /// <summary> Change element index, call update view, also update object name for editor view </summary>
        private void InternalChangeElementIndex(RecycleSingleDirectionScrollElement element, int elementIndex, bool needReCalculateSize)
        {
            if (needReCalculateSize)
            {
                element.ClearPreferredSize();
            }
            if (INVALID_INDEX == elementIndex)
            {
                m_dataSource.UnInitElement(element.ElementTransform);
            }
            else
            {
                m_dataSource.ChangeElementIndex(element.ElementTransform, ElementIndexDataIndex2WayConvert(element.ElementIndex), ElementIndexDataIndex2WayConvert(elementIndex));
                element.SetIndex(elementIndex, ElementIndexDataIndex2WayConvert(elementIndex));
                if (needReCalculateSize)
                {
                    element.CalculatePreferredSize();
                }
            }

#if UNITY_EDITOR
            ChangeObjectName_EditorOnly(element, elementIndex);
#endif
        }

        private void InternalAdjustment()
        {
            // TODO Since I can calculate the virtual size after add/remove, this can be optimized to avoid multiple rebuild.
            RectTransform content = _scrollRect.content;
            Vector2 prevContentStartPos = _scrollRect.ContentStartPos;
            Vector2 anchorPositionDelta = content.anchoredPosition - prevContentStartPos;

            bool hasAdjustedElements = AdjustElementsIfNeed();
            if (hasAdjustedElements)
            {
                Log($"InternalAdjustment once");
                // HACK Becuz I change the anchored position of drag content, so I need to adjust the prev value here. 
                Vector2 newStartPos = content.anchoredPosition - anchorPositionDelta;
                _scrollRect.ContentStartPos = newStartPos;
                m_hasAdjustElementsCurrentFrame = true;
            }
        }

        private bool AdjustElementsIfNeed()
        {
            bool hasRemoved = RemoveElementsIfNeed();
            bool hasAdded = AddElemensIfNeed();
            bool hasAdjusted = hasRemoved || hasAdded;
            if (hasAdjusted)
            {
                ForceRebuildContentLayout();
                _scrollRect.CallUpdateBoundsAndPrevData();
            }
            return hasAdjusted;
        }

        private bool RemoveElementsIfNeed()
        {
            bool hasRemoveHeadElements = RemoveElementsFromHeadIfNeed();
            bool hasRemoveTailElements = RemoveElementsFromTailIfNeed();
            return hasRemoveHeadElements || hasRemoveTailElements;
        }

        private bool AddElemensIfNeed()
        {
            bool hasAddToHead = AddElementsToHeadIfNeed();
            bool hasAddToTail = AddElementsToTailIfNeed();
            return hasAddToHead || hasAddToTail;
        }

        private RecycleSingleDirectionScrollElement InternalAddElement(int elementIndex)
        {
            RectTransform content = _scrollRect.content;
            RecycleSingleDirectionScrollElement newElement;
            RectTransform requestedElement = m_dataSource.RequestElement(content);
            m_dataSource.InitElement(requestedElement, ElementIndexDataIndex2WayConvert(elementIndex));
            if (!requestedElement.TryGetComponent<RecycleSingleDirectionScrollElement>(out newElement))
            {
                LogError($"Receive wrong element");
            }
            newElement.CalculatePreferredSize();

#if UNITY_EDITOR
            ChangeObjectName_EditorOnly(newElement, elementIndex);
#endif
            return newElement;
        }

        private void InternalRemoveElement(RecycleSingleDirectionScrollElement element)
        {
            element.ClearPreferredSize();
            if (null == m_dataSource)
            {
                GameObject.Destroy(element.gameObject);
            }
            else
            {
                m_dataSource.ReturnElement(element.transform as RectTransform);
            }
        }

        /// <param name="delta"> Positive if current count is greater than previous count </param>
        private void NotifySelfDataCountChange(int delta)
        {
            int currentCount = m_dataSource.DataElementCount;
            int prevCount = currentCount - delta;
            OnDataElementCountChanged(prevCount, currentCount);
        }

        private void OnDataElementCountChanged(int prevCount, int nextCount)
        {
            AdjustScrollBarSize();
            UpdateScrollProgress();
        }

        private void OnScrollPositionChanged(Vector2 _)
        {
            InternalAdjustment();
            m_hasPositionChangeCurrentFrame = true;
        }

        private void OnLateUpdated()
        {
            if (!m_hasAdjustElementsCurrentFrame)
            {
                InternalAdjustment();
            }
            if (m_hasPositionChangeCurrentFrame || m_hasAdjustElementsCurrentFrame)
            {
                // HACK The layout has not fully refreshed at the 1st frame :(
                if (0 == m_hasSetScrollBarValueThisFrame)
                {
                    UpdateScrollProgress();
                }
                else
                {
                    --m_hasSetScrollBarValueThisFrame;
                    Log($"Skip scroll progress sync once");
                }
            }
            m_hasAdjustElementsCurrentFrame = false;
            m_hasPositionChangeCurrentFrame = false;
        }

        private void OnEnable()
        {
            if (null == m_onScrollPositionChanged)
            {
                m_onScrollPositionChanged = new UnityAction<Vector2>(OnScrollPositionChanged);
            }
            _scrollRect.onValueChanged.AddListener(m_onScrollPositionChanged);
            if (null == m_onLateUpdated)
            {
                m_onLateUpdated = new Action(OnLateUpdated);
            }
            _scrollRect.AfterLateUpdate += m_onLateUpdated;

            BindScrollBar();
        }

        private void OnDisable()
        {
            UnBindScrollBar();

            if (null != m_onScrollPositionChanged)
            {
                _scrollRect.onValueChanged.RemoveListener(m_onScrollPositionChanged);
            }
            if (null != m_onLateUpdated)
            {
                _scrollRect.AfterLateUpdate -= m_onLateUpdated;
            }
        }

    }
}