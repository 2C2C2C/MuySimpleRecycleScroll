using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ScrollDirection = RecycleScrollView.SingleDirectionScrollParam.ScrollDirection;

namespace RecycleScrollView
{
    [RequireComponent(typeof(UnityScrollRectExtended))]
    public partial class RecycleSingleDirectionScroll : UIBehaviour
    {
        [Header("Main params")]
        [SerializeField]
        private UnityScrollRectExtended _scrollRect;
        [SerializeField]
        private HorizontalOrVerticalLayoutGroup _contentLayoutGroup;

        // Simple layout param
        [SerializeField]
        private SingleDirectionScrollParam _scrollParam;

        // HACK
        [SerializeField]
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
        private ISingleDirectionScrollDataSource m_dataSource;

        public IReadOnlyList<RecycleSingleDirectionScrollElement> CurrentUsingElements => m_currentUsingElements;
        private UnityAction<Vector2> m_onScrollPositionChanged;
        private Action m_onLateUpdated;
        private Action<int> m_onDataElementCountChanged;

        public void ForceRebuildContentLayout()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(_scrollRect.content);
        }

        public void MarkSelfForLayoutRebuild()
        {
            LayoutRebuilder.MarkLayoutForRebuild(_scrollRect.content);
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
                m_dataSource.OnDataElementCountChanged -= m_onDataElementCountChanged;
                m_dataSource = null;
            }
        }

        public void Init(ISingleDirectionScrollDataSource dataSource)
        {
            if (HasDataSource)
            {
                Debug.LogError($"[RecycleSingleDirectionScroll] Has already register a datasource");
            }
            else
            {
                m_dataSource = dataSource;
                if (null == m_onDataElementCountChanged)
                {
                    m_onDataElementCountChanged = new Action<int>(OnDataElementCountChanged);
                }
                m_dataSource.OnDataElementCountChanged += m_onDataElementCountChanged;
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
                OnDataElementCountChanged(m_dataSource.DataElementCount);
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

        public void NotifyElementSizeChange(int dataIndex, bool forceRebuild)
        {
            int indexLowerBound = GetCurrentShowingElementIndexLowerBound();
            int indexUpperBound = GetCurrentShowingElementIndexUpperBound();
            if (indexLowerBound <= dataIndex && dataIndex <= indexUpperBound)
            {
                for (int i = 0, length = m_currentUsingElements.Count; i < length; i++)
                {
                    RecycleSingleDirectionScrollElement element = m_currentUsingElements[i];
                    if (dataIndex == element.DataIndex)
                    {
                        element.CalculatePreferredSize();
                        if (forceRebuild)
                        {
                            MarkSelfForLayoutRebuild();
                        }
                        break;
                    }
                }
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
                    Debug.LogError($"[RecycleScrollView] Vertical scroll need a VerticalLayoutGroup on content");
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
                    Debug.LogError($"[RecycleScrollView] Horizontal scroll need a HorizontalLayoutGroup on content");
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
            RectTransform requestedElement = m_dataSource.RequestElement(content, ElementIndexDataIndex2WayConvert(elementIndex));
            if (!requestedElement.TryGetComponent<RecycleSingleDirectionScrollElement>(out newElement))
            {
                Debug.LogError($"[RecycleScrollView] receive wrong element");
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

        private void OnScrollPositionChanged(Vector2 noramlizedPosition)
        {
            // Debug.LogError("OnScrollPositionChanged");
            InternalAdjustment();
            m_hasPositionChangeCurrentFrame = true;
        }

        private void OnDataElementCountChanged(int count)
        {
            AdjustScrollBarSize();
        }

        private void OnLateUpdated()
        {
            if (!m_hasAdjustElementsCurrentFrame)
            {
                InternalAdjustment();
            }
            if (m_hasPositionChangeCurrentFrame || m_hasAdjustElementsCurrentFrame)
            {
                // UpdateScrollBar if needed
            }
            m_hasAdjustElementsCurrentFrame = false;
            m_hasPositionChangeCurrentFrame = false;

            // HACK The layout has not fully refreshed at the 1st frame :(
            if (0 == m_hasSetScrollBarValueThisFrame)
            {
                UpdateScrollProgress();
            }
            else
            {
                --m_hasSetScrollBarValueThisFrame;
                // Debug.LogError($"skip once; Frame {Time.frameCount}");
            }
            // Debug.LogError($"Check content localpos {_scrollRect.content.localPosition} || Frame:{Time.frameCount}");
        }

        protected override void OnEnable()
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

        protected override void OnDisable()
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

#if UNITY_EDITOR

        private void ChangeObjectName_EditorOnly(MonoBehaviour behaviour, int elementIndex)
        {
            behaviour.name = $"Element {elementIndex}; DataIndex {ElementIndexDataIndex2WayConvert(elementIndex)}";
        }

        protected override void Reset()
        {
            if (TryGetComponent<UnityScrollRectExtended>(out _scrollRect))
            {
                _scrollRect.content.TryGetComponent<HorizontalOrVerticalLayoutGroup>(out _contentLayoutGroup);
            }
        }

        protected override void OnValidate()
        {
            ApplyLayoutSetting();
        }

#endif

    }
}