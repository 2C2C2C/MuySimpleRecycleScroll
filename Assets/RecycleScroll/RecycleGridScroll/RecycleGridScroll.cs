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

        private IGridScrollDataSource m_dataSource = null;
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

        public void AddElementTotail()
        {
            AddRangeToTail(1);
        }

        public void AddRangeToTail(int count)
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
            if (TryGetTailIndexboundOfUsingElements(out int tailIndex) && TryGetHeadIndexOfUsingElements(out _))
            {
                if (dataIndex > tailIndex)
                {
                    return;
                }

                for (int i = 0, length = m_gridElements.Count; i < length; i++)
                {
                    RecycleGridScrollElement gridScrollElement = m_gridElements[i];
                    int elementIndex = gridScrollElement.ElementIndex;
                    if (INVALID_INDEX == elementIndex || dataIndex > elementIndex)
                    {
                        continue;
                    }
                    m_dataSource.UnInitElement(gridScrollElement.ElementTransform);
                    m_dataSource.InitElement(gridScrollElement.ElementTransform, elementIndex);
                }
                // HACK Only update exist grids, new add grids will show after grid update
                m_needUpdateGridsThisFrame = true;
                m_needUpdateContentSizeThisFrame = true;
            }
        }

        public void InsertRange(int dataIndex, int count)
        {
            int prevDataCount = m_dataSource.DataElementCount - 1;
            // Only unbind data to remove, then rebind data for element after the index
            for (int i = 0, length = m_gridElements.Count; i < length; i++)
            {
                RecycleGridScrollElement element = m_gridElements[i];
                int elementIndex = element.ElementIndex;
                if (INVALID_INDEX == elementIndex || dataIndex > elementIndex)
                {
                    continue;
                }
                m_dataSource.UnInitElement(element.ElementTransform);
                element.SetIndex(INVALID_INDEX);
                if (prevDataCount - 1 >= elementIndex)
                {
                    m_dataSource.InitElement(element.ElementTransform, elementIndex);
                }
            }
            m_needUpdateGridsThisFrame = true;
            m_needUpdateContentSizeThisFrame = true;
        }

        public void RemoveElement(int dataIndex)
        {
            int prevDataCount = m_dataSource.DataElementCount - 1;
            // Only unbind data to remove, then rebind data for element after the index
            for (int i = 0, length = m_gridElements.Count; i < length; i++)
            {
                RecycleGridScrollElement element = m_gridElements[i];
                int elementIndex = element.ElementIndex;
                if (INVALID_INDEX == elementIndex || dataIndex > elementIndex)
                {
                    continue;
                }
                m_dataSource.UnInitElement(element.ElementTransform);
                if (prevDataCount - 1 >= elementIndex)
                {
                    m_dataSource.InitElement(element.ElementTransform, elementIndex);
                }
                else
                {
                    element.SetIndex(INVALID_INDEX);
                }
            }
            m_needUpdateGridsThisFrame = true;
            m_needUpdateContentSizeThisFrame = true;
        }

        public void RemoveRange(int dataIndex, int count)
        {
            m_needUpdateGridsThisFrame = true;
            m_needUpdateContentSizeThisFrame = true;
            // HACK Unbind data and index, later do refresh
            for (int i = 0, length = m_gridElements.Count; i < length; i++)
            {
                RecycleGridScrollElement element = m_gridElements[i];
                if (element.ElementIndex >= dataIndex)
                {
                    m_dataSource.UnInitElement(element.ElementTransform);
                    element.SetIndex(INVALID_INDEX);
                    break;
                }
            }
        }

        public void InsertElements(IReadOnlyList<int> sortedDataIndexList)
        {
            m_needUpdateGridsThisFrame = true;
            m_needUpdateContentSizeThisFrame = true;
            if (TryGetTailIndexboundOfUsingElements(out int tailIndex))
            {
                int unbindStartIndex = INVALID_INDEX;
                // HACK Unbind data and index, later do refresh
                for (int i = 0, length = sortedDataIndexList.Count; i < length; i++)
                {
                    int tempIndex = sortedDataIndexList[i];
                    if (tempIndex <= tailIndex)
                    {
                        unbindStartIndex = tempIndex;
                        break;
                    }
                }

                if (INVALID_INDEX != unbindStartIndex)
                {
                    for (int i = 0, length = m_gridElements.Count; i < length; i++)
                    {
                        RecycleGridScrollElement element = m_gridElements[i];
                        if (element.ElementIndex >= unbindStartIndex)
                        {
                            m_dataSource.UnInitElement(element.ElementTransform);
                            element.SetIndex(INVALID_INDEX);
                            break;
                        }
                    }
                }
            }
        }

        public void RemoveElements(IReadOnlyList<int> sortedDataIndexList)
        {
            m_needUpdateContentSizeThisFrame = true;
            m_needUpdateGridsThisFrame = true;
            if (TryGetTailIndexboundOfUsingElements(out int tailIndex))
            {
                int unbindStartIndex = INVALID_INDEX;
                // HACK Unbind data and index, later do refresh
                for (int i = 0, length = sortedDataIndexList.Count; i < length; i++)
                {
                    int tempIndex = sortedDataIndexList[i];
                    if (tempIndex <= tailIndex)
                    {
                        unbindStartIndex = tempIndex;
                        break;
                    }
                }

                if (INVALID_INDEX != unbindStartIndex)
                {
                    for (int i = 0, length = m_gridElements.Count; i < length; i++)
                    {
                        RecycleGridScrollElement element = m_gridElements[i];
                        if (element.ElementIndex >= unbindStartIndex)
                        {
                            m_dataSource.UnInitElement(element.ElementTransform);
                            element.SetIndex(INVALID_INDEX);
                            break;
                        }
                    }
                }
            }
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

        public void Init(IGridScrollDataSource source)
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

        public void Uninit()
        {
            if (HasDataSource)
            {
                for (int i = 0, length = m_gridElements.Count; i < length; i++)
                {
                    RectTransform gridRectTransform = m_gridElements[i].ElementTransform;
                    m_dataSource.UnInitElement(gridRectTransform);
                    m_dataSource.RemoveElement(gridRectTransform);
                }
                m_gridElements.Clear();
                m_dataSource = null;
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

        [ContextMenu("Adjust Cached Items")]
        private int CalculateCurrentViewportShowCount()
        {
            m_viewElementCountInRow = 0;
            m_viewElementCountInColumn = 0;
            Vector2 gridSize = new Vector2(_gridLayoutData.gridSize.x, _gridLayoutData.gridSize.y);

            Vector2 spacing = _gridLayoutData.Spacing;
            RectTransform viewport = _scrollRect.viewport;
            float viewportHeight = Mathf.Abs(viewport.rect.height);
            float viewportWidth = Mathf.Abs(viewport.rect.width);
            m_viewElementCountInColumn = Mathf.FloorToInt(viewportHeight / (gridSize.y + spacing.y));
            m_viewElementCountInRow = Mathf.FloorToInt(viewportWidth / (gridSize.x + spacing.x));

            m_viewElementCountInColumn += (0 < viewportHeight % (gridSize.y + spacing.y)) ? 2 : 1;
            m_viewElementCountInRow += (0 > viewportWidth % (gridSize.x + spacing.x)) ? 2 : 1;

            if (SimpleGridLayoutData.Constraint.FixedColumnCount == _gridLayoutData.constraint)
            {
                m_viewElementCountInRow = Mathf.Clamp(m_viewElementCountInRow, 1, _gridLayoutData.constraintCount);
            }
            else
            {
                m_viewElementCountInColumn = Mathf.Clamp(m_viewElementCountInColumn, 1, _gridLayoutData.constraintCount);
            }

            int result = (1 + m_viewElementCountInRow) * (1 + m_viewElementCountInColumn);
            return result;
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

        private void OnScrollRectValueChanged(Vector2 position)
        {
            m_needUpdateGridsThisFrame = true;
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
                AddElements(deltaCount);
            }
            if (0 > deltaCount && currentElementCount > 0)
            {
                RemoveElements(deltaCount);
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

        private Vector2 CalculateContentSize(int dataCount)
        {
            RectOffset m_padding = _gridLayoutData.RectPadding;
            Vector2 gridSize = _gridLayoutData.gridSize;
            Vector2 spacing = _gridLayoutData.Spacing;
            Vector2 result = default;

            int constraintCount = _gridLayoutData.constraintCount;
            int groupCount = (dataCount % constraintCount > 0) ? (dataCount / constraintCount) + 1 : (dataCount / constraintCount);
            if (_gridLayoutData.constraint == SimpleGridLayoutData.Constraint.FixedColumnCount)
            {
                result.x = (constraintCount * gridSize.x) + ((constraintCount - 1) * spacing.x);
                result.y = groupCount * gridSize.y + (groupCount - 1) * spacing.y;
            }
            else if (_gridLayoutData.constraint == SimpleGridLayoutData.Constraint.FixedRowCount)
            {
                result.y = (constraintCount * gridSize.y) + ((constraintCount - 1) * spacing.y);
                result.x = groupCount * gridSize.x + (groupCount - 1) * spacing.x;
            }

            result += new Vector2(m_padding.horizontal, m_padding.vertical);
            return result;
        }

        private void AddElements(int count)
        {
            Vector2 gridSize = _gridLayoutData.gridSize;
            if (HasDataSource)
            {
                for (int i = 0; i < count; i++)
                {
                    RectTransform target = m_dataSource.AddElement(_gridContainer);
                    if (!target.gameObject.TryGetComponent<RecycleGridScrollElement>(out RecycleGridScrollElement added))
                    {
                        Debug.LogError("[RecycleScrollGrid] The element prefab does not have RecycleScrollGridElement component", target.gameObject);
                        return;
                    }
                    added.SetElementSize(gridSize);
                    m_gridElements.Add(added);
                    m_dataSource.UnInitElement(target);
                }
            }
        }

        private void RemoveElements(int count)
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
                    m_dataSource.RemoveElement(m_gridElements[elementIndex].ElementTransform);
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
                    RemoveElements(m_gridElements.Count);
                    m_gridElements.Clear();
                }
            }
        }

    }
}