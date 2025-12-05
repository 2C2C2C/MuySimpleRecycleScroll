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

        public void InsertElements(int dataIndex, int count)
        {
            if (0 == m_currentUsingElements.Count || 0 == count)
            {
                return; // In LateUpdate it will automaticlly handle this
            }

            NotifySelfDataCountChange(count);
            int insertElementIndex = ElementIndexDataIndex2WayConvert(dataIndex);
            int tailIndex = m_currentUsingElements[m_currentUsingElements.Count - 1].ElementIndex;
            if (tailIndex < dataIndex)
            {
                return;
            }
            for (int i = 0, length = m_currentUsingElements.Count; i < length; i++)
            {
                RecycleSingleDirectionScrollElement element = m_currentUsingElements[i];
                if (element.ElementIndex >= insertElementIndex)
                {
                    m_dataSource.UnInitElement(element.ElementTransform);
                    m_dataSource.InitElement(element.ElementTransform, element.ElementIndex);
                    element.ClearPreferredSize();
                    element.CalculatePreferredSize();
                }
            }
        }

        public void InsertElements(IReadOnlyList<int> sortedDataIndexList)
        {
            if (0 == m_currentUsingElements.Count || 0 == sortedDataIndexList.Count)
            {
                return; // In LateUpdate it will automaticlly handle this
            }

            NotifySelfDataCountChange(sortedDataIndexList.Count);
            int insertElementIndex = ElementIndexDataIndex2WayConvert(sortedDataIndexList[0]);
            for (int i = 0, length = m_currentUsingElements.Count; i < length; i++)
            {
                RecycleSingleDirectionScrollElement element = m_currentUsingElements[i];
                if (element.ElementIndex >= insertElementIndex)
                {
                    m_dataSource.UnInitElement(element.ElementTransform);
                    m_dataSource.InitElement(element.ElementTransform, element.DataIndex);
                    element.ClearPreferredSize();
                    element.CalculatePreferredSize();
                }
            }
        }

        public void RemoveElements(int dataIndex, int count)
        {
            NotifySelfDataCountChange(-count);
            // TODO should
        }

        public void RemoveElements(IReadOnlyList<int> sortedDataIndexList)
        {
            NotifySelfDataCountChange(-sortedDataIndexList.Count);
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

        public void InsertElement(int dataIndex)
        {
            int prevDataCount = m_dataSource.DataElementCount - 1;
            int insertElementIndex = ElementIndexDataIndex2WayConvert(dataIndex, prevDataCount);
            int indexTailBound = GetCurrentShowingElementIndexTailBound();
            if (insertElementIndex > indexTailBound)
            {
                return;
            }

            int indexHeadBound = GetCurrentShowingElementIndexHeadBound();
            bool hasAdded = indexHeadBound > insertElementIndex;
            for (int i = 0, length = m_currentUsingElements.Count; i < length; i++)
            {
                RecycleSingleDirectionScrollElement element = m_currentUsingElements[i];
                int elementIndex = element.ElementIndex;
                if (elementIndex == insertElementIndex && !hasAdded)
                {
                    RecycleSingleDirectionScrollElement newElement = InternalCreateElement(insertElementIndex);
                    newElement.ElementTransform.SetSiblingIndex(element.ElementTransform.GetSiblingIndex());
                    newElement.SetIndex(insertElementIndex, dataIndex);
                    m_currentUsingElements.Insert(i, newElement);
                    length++;
                    hasAdded = true;
                }
                else if (insertElementIndex <= elementIndex && hasAdded)
                {
                    InternalChangeElementIndex(element, elementIndex + 1, false);
                }
            }
        }

        public void RemoveElement(int dataIndex)
        {
            NotifySelfDataCountChange(-1);
            int prevDataCount = m_dataSource.DataElementCount - 1;
            int removeElementIndex = ElementIndexDataIndex2WayConvert(dataIndex, prevDataCount);
            int indexTailBound = GetCurrentShowingElementIndexTailBound();
            if (removeElementIndex > indexTailBound)
            {
                return;
            }

            int indexHeadBound = GetCurrentShowingElementIndexHeadBound();
            bool hasRemoved = indexHeadBound > removeElementIndex;
            for (int i = 0, length = m_currentUsingElements.Count; i < length; i++)
            {
                RecycleSingleDirectionScrollElement element = m_currentUsingElements[i];
                if (element.ElementIndex == removeElementIndex && !hasRemoved)
                {
                    m_currentUsingElements.RemoveAt(i);
                    length--; i--;
                    InternalRemoveElement(element);
                    hasRemoved = true;
                }
                else if (dataIndex < removeElementIndex && hasRemoved)
                {
                    InternalChangeElementIndex(element, removeElementIndex - 1, false);
                }
            }

            // TODO IDK if I should also move the scroll content
            if (hasRemoved)
            {
                ForceRebuildContentLayout();
                ForceAdjustElements();
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

                int dataCount = m_dataSource.DataElementCount;
                int headElementIndex = CalculateAvailabeNextHeadElementIndex();
                SetPreCacheElement(headElementIndex, ref m_preCacheHeadElement);
                int tailElementIndex = CalculateAvailabeNextTailElementIndex();
                SetPreCacheElement(tailElementIndex, ref m_preCacheTailElement);
                _scrollRect.CallUpdateBoundsAndPrevData();
                OnDataElementCountChanged(0, m_dataSource.DataElementCount);
            }
        }

        public void RemoveCurrentElements()
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

        public void ForceAdjustElements()
        {
            InternalAdjustment();
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

        private void InternalChangeElementIndex(RecycleSingleDirectionScrollElement element, int nextElementIndex, bool needReCalculateSize)
        {
            if (needReCalculateSize)
            {
                element.ClearPreferredSize();
            }
            m_dataSource.ChangeElementIndex(element.ElementTransform, ElementIndexDataIndex2WayConvert(element.ElementIndex), ElementIndexDataIndex2WayConvert(nextElementIndex));
            element.SetIndex(nextElementIndex, ElementIndexDataIndex2WayConvert(nextElementIndex));
            if (needReCalculateSize)
            {
                element.CalculatePreferredSize();
            }
#if UNITY_EDITOR
            ChangeObjectName_EditorOnly(element, nextElementIndex);
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

        private RecycleSingleDirectionScrollElement InternalCreateElement(int elementIndex)
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

        private void OnScrollPositionChanged(Vector2 _)
        {
            InternalAdjustment();
            m_hasPositionChangeCurrentFrame = true;
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