using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RecycleScrollView
{
    [ExecuteAlways]
    [RequireComponent(typeof(UnityScrollRectExtended))]
    public partial class RecycleGridScroll : MonoBehaviour, IRecycleScroll
    {
        private static Comparison<RecycleGridScrollElement> s_gridElementCompare;

        public static Comparison<RecycleGridScrollElement> GridElementCompare
        {
            get
            {
                if (null == s_gridElementCompare)
                {
                    s_gridElementCompare = new Comparison<RecycleGridScrollElement>((x, y) =>
                    {
                        int xIndex = x.ElementIndex, yIndex = y.ElementIndex;
                        if (xIndex == yIndex)
                        {
                            return 0;
                        }

                        // Minus value need to be on the back
                        if (0 > xIndex && 0 <= yIndex)
                        {
                            return 1;
                        }
                        else if (0 <= xIndex && 0 > yIndex)
                        {
                            return -1;
                        }

                        return xIndex.CompareTo(yIndex);
                    });
                }
                return s_gridElementCompare;
            }
        }

        [SerializeField]
        private UnityScrollRectExtended _scrollRect = null;
        [SerializeField]
        private RectTransform _gridContainer = null;

        [SerializeField]
        private bool _showActualGridElements = true;

        [Space, Header("Grid Layout Setting"), SerializeField]
        private SimpleGridLayoutData _gridLayoutData = new SimpleGridLayoutData();

        [SerializeField] // This value should be NonSerialized but better to show it in inspector
        /// <summary> The value should greater than 0 </summary>
        private int m_simulatedDataCount = 0;

        // <summary>
        // The actual element count may show in the viewport
        // </summary>
        private int m_viewElementCount = -1;
        private int m_viewElementCountInRow = 0;
        private int m_viewElementCountInColumn = 0;

        private IRecycleScrollDataSource m_dataSource = null;
        [SerializeField]
        private List<RecycleGridScrollElement> m_gridElements; // TODO should be Nonserialized but show in inspector
        private UnityAction<Vector2> m_onScrollRectValueChanged;

        private bool m_needUpdateGridsThisFrame = false;
        private bool m_needUpdateContentSizeThisFrame = false;

        public int ViewItemCount => m_viewElementCount;
        public int ViewItemCountInRow => m_viewElementCountInRow;
        public int ViewItemCountInColumn => m_viewElementCountInColumn;

        public IReadOnlyList<RecycleGridScrollElement> ElementList => m_gridElements ??= new List<RecycleGridScrollElement>();
        public SimpleGridLayoutData GridLayoutData => _gridLayoutData;
        public int SimulatedDataCount => HasDataSource ? m_dataSource.DataElementCount : m_simulatedDataCount;
        public bool HasDataSource => null != m_dataSource;

        public void UnInit()
        {
            if (HasDataSource)
            {
                for (int i = 0, length = m_gridElements.Count; i < length; i++)
                {
                    RectTransform gridRectTransform = m_gridElements[i].ElementTransform;
                    m_dataSource.UnInitElement(gridRectTransform);
                    m_dataSource.ReturnElement(gridRectTransform);
                }
                m_gridElements.Clear();
                m_dataSource = null;
            }
        }

        public void Init(IRecycleScrollDataSource source)
        {
            if (HasDataSource)
            {
                Debug.LogError($"[RecycleScrollGrid] Init failed, the already has data source");
            }
            else
            {
                if (null == source)
                {
                    Debug.LogError("[RecycleScrollGrid] Init failed, the listview is null", context: this);
                    return;
                }
                m_dataSource = source;
                RefreshLayoutChanges();
            }
        }

        public void AddElementTotail()
        {
            AddElementsToTail(1);
        }

        public void AddElementsToTail(int count)
        {
            if (0 == count)
            {
                return;
            }
            // HACK Do force refresh for now
            m_needUpdateGridsThisFrame = true;
            m_needUpdateContentSizeThisFrame = true;
        }

        public void InsertElement(int dataIndex)
        {
            InsertElements(dataIndex, 1);
        }

        public void InsertElements(int dataIndex, int count)
        {
            for (int i = 0, length = m_gridElements.Count; i < length; i++)
            {
                RecycleGridScrollElement element = m_gridElements[i];
                int elementIndex = element.ElementIndex;
                if (INVALID_INDEX == elementIndex || dataIndex > elementIndex)
                {
                    continue;
                }
                // Refresh element view
                m_dataSource.UnInitElement(element.ElementTransform);
                m_dataSource.InitElement(element.ElementTransform, elementIndex);
            }
            m_needUpdateGridsThisFrame = true;
            m_needUpdateContentSizeThisFrame = true;
        }

        public void InsertElements(IReadOnlyList<int> sortedDataIndexList)
        {
            InsertElements(sortedDataIndexList[0], sortedDataIndexList.Count); // HACK
        }

        public void RemoveElement(int dataIndex)
        {
            RemoveElements(dataIndex, 1);
        }

        public void RemoveElements(int dataIndex, int count)
        {
            int dataCount = m_dataSource.DataElementCount;
            m_needUpdateGridsThisFrame = true;
            m_needUpdateContentSizeThisFrame = true;
            // HACK Unbind data and index, later do refresh
            for (int i = 0, length = m_gridElements.Count; i < length; i++)
            {
                RecycleGridScrollElement element = m_gridElements[i];
                int elementIndex = element.ElementIndex;
                if (elementIndex >= dataIndex && INVALID_INDEX != elementIndex)
                {
                    if (dataCount - 1 >= elementIndex)
                    {
                        // Element still valid 
                        SetElementIndex(element, elementIndex);
                    }
                    else
                    {
                        // Element invalid
                        SetElementIndex(element, INVALID_INDEX);
                    }
                }
            }
            m_needUpdateGridsThisFrame = true;
            m_needUpdateContentSizeThisFrame = true;
        }

        public void RemoveElements(IReadOnlyList<int> sortedDataIndexList)
        {
            RemoveElements(sortedDataIndexList[0], sortedDataIndexList.Count); // HACK
        }

        public void UpdateElement(int dataIndex)
        {
            m_needUpdateContentSizeThisFrame = true;
            if (TryGetTailIndexboundOfUsingElements(out int tailIndex) &&
                TryGetHeadIndexOfUsingElements(out int headIndex) &&
                dataIndex >= headIndex && dataIndex <= tailIndex)
            {
                for (int i = 0, length = m_gridElements.Count; i < length; i++)
                {
                    RecycleGridScrollElement element = m_gridElements[i];
                    if (element.ElementIndex == dataIndex)
                    {
                        m_dataSource.UnInitElement(element.ElementTransform);
                        m_dataSource.InitElement(element.ElementTransform, dataIndex);
                        break;
                    }
                }
            }
        }

        public void UpdateConstraintWithAutoFit()
        {
            float viewportHeight, viewportWidth;
            RectTransform viewport = _scrollRect.viewport;
            Vector2 spacing = _gridLayoutData.Spacing;
            viewportHeight = viewport.rect.height;
            viewportWidth = viewport.rect.width;
            Vector2 itemSize = new Vector2(_gridLayoutData.gridSize.x, _gridLayoutData.gridSize.y);

            int constraintCount;
            if (SimpleGridLayoutData.Constraint.FixedColumnCount == _gridLayoutData.constraint)
            {
                constraintCount = Mathf.FloorToInt(viewportWidth / (itemSize.x + spacing.x));
            }
            else
            {
                constraintCount = Mathf.FloorToInt(viewportHeight / (itemSize.y + spacing.y));
            }

            constraintCount = Mathf.Clamp(constraintCount, 1, int.MaxValue);
            _gridLayoutData.constraintCount = constraintCount;
        }

        public void RefreshLayoutChanges()
        {
            ApplySizeToScrollContent();
            AdjustCachedGrids();
            ApplySizeOnElements();
            OnScrollRectValueChanged(Vector2.zero);
        }

        private void AdjustCachedGrids()
        {
            m_viewElementCount = CalculateCurrentViewportShowCount();
            AdjustElementArray(m_viewElementCount);
            ApplySizeOnElements();
        }

        private void ApplySizeToScrollContent()
        {
            if (HasDataSource)
            {
                m_simulatedDataCount = m_dataSource.DataElementCount;
                int dataCount = m_simulatedDataCount;
                Vector2 contentSize = CalculateContentSize(dataCount);
                RectTransform content = _scrollRect.content;
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, contentSize.x);
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentSize.y);
            }
        }

        private void AdjustGrids()
        {
            IReadOnlyList<RecycleGridScrollElement> elementList = ElementList;
            if (_showActualGridElements)
            {
                if (elementList.Count != m_viewElementCount)
                {
                    AdjustCachedGrids();
                }
                UpdateGridPositionData();
                SortPositionData();
                ApplyGridPosition();
            }
            else
            {
                // Hide all Items
                for (int i = 0; i < elementList.Count; i++)
                {
                    elementList[i].SetObjectDeactive();
                }
            }
        }

        private void AdjustElementArray(int size)
        {
            int currentElementCount = ElementList.Count;
            int deltaCount = size - currentElementCount;
            if (0 < deltaCount)
            {
                // Need to add element
                InternalAddElements(deltaCount);
            }
            if (0 > deltaCount && currentElementCount > 0)
            {
                InternalRemoveElements(deltaCount);
            }
            if (0 == m_dataNeed2Show.Count)
            {
                m_dataNeed2Show.Capacity = currentElementCount;
            }
        }

        private void ApplySizeOnElements()
        {
            if (HasDataSource)
            {
                // sync the size form grid data
                Vector2 itemAcutalSize = GridLayoutData.gridSize;
                IReadOnlyList<RecycleGridScrollElement> elementList = ElementList;
                for (int i = 0; i < elementList.Count; i++)
                {
                    RectTransform element = elementList[i].ElementTransform;
                    element.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, itemAcutalSize.x);
                    element.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, itemAcutalSize.y);
                }
            }
        }

        private void InternalAddElements(int count)
        {
            Vector2 gridSize = _gridLayoutData.gridSize;
            if (HasDataSource)
            {
                for (int i = 0; i < count; i++)
                {
                    RectTransform target = m_dataSource.RequestElement(_gridContainer);
                    if (!target.gameObject.TryGetComponent<RecycleGridScrollElement>(out RecycleGridScrollElement added))
                    {
                        Debug.LogError("[RecycleScrollGrid] The element prefab does not have RecycleScrollGridElement component", target.gameObject);
                        return;
                    }
                    added.SetElementSize(gridSize);
                    m_gridElements.Add(added);
                    m_dataSource.UnInitElement(target);
                    SetElementIndex(added, INVALID_INDEX);
                }
            }
        }

        private void InternalRemoveElements(int count)
        {
            // Make sure non-used elements on the back
            m_gridElements.Sort(GridElementCompare);
            int elementCount = m_gridElements.Count;
            // Try remove non-used elements first
            if (HasDataSource)
            {
                for (int i = 0; i < count; i++)
                {
                    int elementIndex = elementCount - i - 1;
                    m_dataSource.ReturnElement(m_gridElements[elementIndex].ElementTransform);
                }
            }

            if (count == elementCount)
            {
                m_gridElements.Clear();
            }
            else
            {
                m_gridElements.RemoveRange(elementCount - count, count);
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying && null == m_onScrollRectValueChanged)
            {
                m_onScrollRectValueChanged = new UnityAction<Vector2>(OnScrollRectValueChanged);
            }
            _scrollRect.onValueChanged.AddListener(m_onScrollRectValueChanged);
        }

        private void OnDisable()
        {
            if (Application.isPlaying && null != m_onScrollRectValueChanged)
            {
                _scrollRect.onValueChanged.RemoveListener(m_onScrollRectValueChanged);
            }
        }

        private void OnScrollRectValueChanged(Vector2 position)
        {
            m_needUpdateGridsThisFrame = true;
        }

        // TODO Subscribe to AfterScrollRectLateUpdate
        private void LateUpdate()
        {
            if (m_needUpdateContentSizeThisFrame)
            {
                ApplySizeToScrollContent();
            }
            m_needUpdateContentSizeThisFrame = false;
            if (m_needUpdateGridsThisFrame)
            {
                AdjustGrids();
            }
            m_needUpdateGridsThisFrame = false;
        }

        private void OnDestroy()
        {
            if (Application.isPlaying)
            {
                if (null != m_gridElements && 0 < m_gridElements.Count)
                {
                    InternalRemoveElements(m_gridElements.Count);
                    m_gridElements.Clear();
                }
            }
        }

    }
}