using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extend;

namespace RecycleScrollView
{
    public partial class RecycleGridScroll
    {
        internal struct GridPositionData
        {
            public int dataIndex;
            public Vector3 gridPositionInContent;
            public GridPositionData(int i, Vector3 pos)
            {
                dataIndex = i;
                gridPositionInContent = pos;
            }
        }

        private List<GridPositionData> m_dataNeed2Show = new List<GridPositionData>();
        private HashSet<int> m_dataIndex2Show = new HashSet<int>();

        private Comparison<GridPositionData> m_positionDataComparsion;
        private Comparison<RecycleGridScrollElement> m_elementComparsion;

        private void UpdateGridPositionData()
        {
            if (IsCurrentLayoutDataInvalid())
            {
                return;
            }

            bool hasDataSource = HasDataSource;
            int dataCount = hasDataSource ? m_dataSource.DataElementCount : SimulatedDataCount;
            SimpleGridLayoutData gridLayoutData = _gridLayoutData;
            RectTransform scrollContent = _scrollRect.content;
            Vector2 rawSize = CalculateContentSize(dataCount);
            Vector2 contentPivot = scrollContent.pivot;
            Vector2 pivotLocalPosition = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(scrollContent, contentPivot);
            Vector2 startLocalPosition = pivotLocalPosition;  // The start position of the first grid(bottom left)
            startLocalPosition.x = pivotLocalPosition.x - rawSize.x * contentPivot.x;
            startLocalPosition.y = pivotLocalPosition.y - rawSize.y * contentPivot.y;

            Vector2 gridSize = _gridLayoutData.gridSize;
            switch (_gridLayoutData.startCorner)
            {
                case GridLayoutGroup.Corner.LowerRight:
                    startLocalPosition.x += rawSize.x;
                    startLocalPosition.x -= gridLayoutData.RectPadding.right;
                    startLocalPosition.y += gridLayoutData.RectPadding.bottom;
                    startLocalPosition.x -= gridSize.x;
                    break;
                case GridLayoutGroup.Corner.UpperRight:
                    startLocalPosition += rawSize;
                    startLocalPosition.x -= gridLayoutData.RectPadding.right;
                    startLocalPosition.y -= gridLayoutData.RectPadding.top;
                    startLocalPosition.x -= gridSize.x;
                    startLocalPosition.y -= gridSize.y;
                    break;
                case GridLayoutGroup.Corner.UpperLeft:
                    startLocalPosition.y += rawSize.y;
                    startLocalPosition.x += gridLayoutData.RectPadding.left;
                    startLocalPosition.y -= gridLayoutData.RectPadding.top;
                    startLocalPosition.y -= gridSize.y;
                    break;
                case GridLayoutGroup.Corner.LowerLeft:
                default:
                    startLocalPosition.x += gridLayoutData.RectPadding.left;
                    startLocalPosition.y += gridLayoutData.RectPadding.bottom;
                    break;
            }

            Vector2 gridGroupMoveDirection = CalculateGridGroupMoveDirection();
            Vector2 gridGroupSpacing = Mathf.Approximately(0f, gridGroupMoveDirection.x) ? _gridLayoutData.Spacing.y * Vector2.up : _gridLayoutData.Spacing.x * Vector2.right;
            Vector2 girdMoveDirection = CalculateGridMoveDirectionInGroup();
            Vector2 gridSpacing = Mathf.Approximately(0f, girdMoveDirection.x) ? _gridLayoutData.Spacing.y * Vector2.up : _gridLayoutData.Spacing.x * Vector2.right;

            Rect viewportRect = _scrollRect.viewport.rect;
            Matrix4x4 worldToViewportLocal = _scrollRect.viewport.worldToLocalMatrix;

            RectTransform content = _scrollRect.content;
            Matrix4x4 localToWorld = content.localToWorldMatrix;
            int constraintCount = _gridLayoutData.constraintCount;
            int groupCount = (dataCount % constraintCount > 0) ? (dataCount / constraintCount) + 1 : (dataCount / constraintCount);
            Vector2 groupStartPos = startLocalPosition;
            int gridDataIndex = 0;
            int usedElementIndex = 0;
            int gridElementCount = m_gridElements.Count;
            m_dataNeed2Show.Clear();
            m_dataIndex2Show.Clear();
            for (int i = 0; i < groupCount; i++)
            {
                Vector2 gridGroupStartPos = groupStartPos + (i * new Vector2(gridGroupMoveDirection.x * gridGroupSpacing.x, gridGroupMoveDirection.y * gridGroupSpacing.y));
                gridGroupStartPos += i * new Vector2(gridGroupMoveDirection.x * gridSize.x, gridGroupMoveDirection.y * gridSize.y);
                for (int j = 0; j < constraintCount; j++)
                {
                    bool isOutOfDataCount = gridDataIndex >= dataCount;
                    if (isOutOfDataCount || usedElementIndex >= gridElementCount)
                    {
                        ++gridDataIndex;
                        continue;
                    }

                    Vector2 gridStartPos = gridGroupStartPos + (j * new Vector2(girdMoveDirection.x * gridSpacing.x, girdMoveDirection.y * gridSpacing.y));
                    gridStartPos += j * new Vector2(girdMoveDirection.x * gridSize.x, girdMoveDirection.y * gridSize.y);

                    // HACK Have to covert the rect to viewport's local space
                    Vector3 worldPos = localToWorld.MultiplyPoint(gridStartPos);
                    Vector2 rectMinPoint = worldToViewportLocal.MultiplyPoint(worldPos);
                    Rect gridRect = new Rect(rectMinPoint, gridSize);
                    bool isInterestedWithViewport = viewportRect.Overlaps(gridRect);
                    if (isInterestedWithViewport)
                    {
                        m_dataIndex2Show.Add(gridDataIndex);
                        m_dataNeed2Show.Add(new GridPositionData(gridDataIndex, gridStartPos));
                        ++usedElementIndex;
                    }
                    ++gridDataIndex;
                }
            }
        }

        private Vector2 CalculateGridGroupMoveDirection()
        {
            Vector2 gridGroupMoveDirection = default;
            SimpleGridLayoutData layoutData = _gridLayoutData;
            if (GridLayoutGroup.Axis.Horizontal == layoutData.startAxis)
            {
                if (GridLayoutGroup.Corner.LowerLeft == _gridLayoutData.startCorner ||
                    GridLayoutGroup.Corner.LowerRight == _gridLayoutData.startCorner)
                {
                    gridGroupMoveDirection.y = 1f;
                }
                else
                {
                    gridGroupMoveDirection.y = -1f;
                }
            }
            else // GridLayoutGroup.Axis.Vertical == layoutData.startAxis
            {
                if (GridLayoutGroup.Corner.UpperLeft == _gridLayoutData.startCorner ||
                    GridLayoutGroup.Corner.LowerLeft == _gridLayoutData.startCorner)
                {
                    gridGroupMoveDirection.x = 1f;
                }
                else
                {
                    gridGroupMoveDirection.x = -1f;
                }
            }
            return gridGroupMoveDirection;
        }

        private Vector2 CalculateGridMoveDirectionInGroup()
        {
            Vector2 girdMoveDirection = default;
            SimpleGridLayoutData layoutData = _gridLayoutData;
            if (GridLayoutGroup.Axis.Horizontal == layoutData.startAxis)
            {
                if (GridLayoutGroup.Corner.LowerLeft == _gridLayoutData.startCorner ||
                    GridLayoutGroup.Corner.UpperLeft == _gridLayoutData.startCorner)
                {
                    girdMoveDirection.x = 1f;
                }
                else
                {
                    girdMoveDirection.x = -1f;
                }
            }
            else // GridLayoutGroup.Axis.Vertical == layoutData.startAxis
            {
                if (GridLayoutGroup.Corner.LowerLeft == _gridLayoutData.startCorner ||
                    GridLayoutGroup.Corner.LowerRight == _gridLayoutData.startCorner)
                {
                    girdMoveDirection.y = 1f;
                }
                else
                {
                    girdMoveDirection.y = -1f;
                }
            }
            return girdMoveDirection;
        }

        private void ApplyGridPosition()
        {
            int dataCount = m_dataNeed2Show.Count;
            int usedElementIndex = 0;
            int gridElementIndexMax = m_gridElements.Count - 1;
            for (int i = 0; i < dataCount; i++)
            {
                GridPositionData gridPositionData = m_dataNeed2Show[i];
                int dataIndex = gridPositionData.dataIndex;
                if (0 <= usedElementIndex && usedElementIndex <= gridElementIndexMax)
                {
                    RecycleGridScrollElement gridElement = m_gridElements[usedElementIndex];
                    if (dataIndex != gridElement.ElementIndex)
                    {
                        if (0 > gridElement.ElementIndex)
                        {
                            m_dataSource.InitElement(gridElement.ElementTransform, dataIndex);
                        }
                        else
                        {
                            m_dataSource.ChangeElementIndex(gridElement.ElementTransform, gridElement.ElementIndex, dataIndex);
                        }
                        gridElement.SetIndex(dataIndex);
#if UNITY_EDITOR
                        ChangeObjectName_EditorOnly(gridElement, dataIndex);
#endif
                    }
                    gridElement.ElementTransform.localPosition = gridPositionData.gridPositionInContent;
                    gridElement.SetObjectActive();
                    ++usedElementIndex;
                }
            }

            for (int i = usedElementIndex; i <= gridElementIndexMax; i++)
            {
                RecycleGridScrollElement gridElement = m_gridElements[i];
                if (0 <= gridElement.ElementIndex) // prevIndexValidprevIndexValid
                {
                    m_dataSource.UnInitElement(gridElement.ElementTransform);
                    gridElement.SetIndex(INVALID_INDEX);
                }
                gridElement.SetObjectDeactive();
            }
        }

        private void SortPositionData()
        {
            if (0 == m_dataNeed2Show.Count)
            {
                return;
            }
            if (null == m_positionDataComparsion)
            {
                m_positionDataComparsion = new Comparison<GridPositionData>(PositionDataComparer);
            }
            m_dataNeed2Show.Sort(m_positionDataComparsion);

            if (null == m_elementComparsion)
            {
                m_elementComparsion = new Comparison<RecycleGridScrollElement>(ElementComparer);
            }
            m_gridElements.Sort(m_elementComparsion);
        }

        private int PositionDataComparer(GridPositionData x, GridPositionData y)
        {
            return x.dataIndex.CompareTo(y.dataIndex);
        }

        private int ElementComparer(RecycleGridScrollElement x, RecycleGridScrollElement y)
        {
            const int INVALID_INDEX = -1;
            int xIndex = x.ElementIndex, yIndex = y.ElementIndex;
            bool xInvalid = INVALID_INDEX == xIndex || !m_dataIndex2Show.Contains(xIndex);
            bool yInvalid = INVALID_INDEX == yIndex || !m_dataIndex2Show.Contains(yIndex);
            if (xInvalid || yInvalid)
            {
                if (xInvalid == yInvalid)
                {
                    return 0;
                }
                return xInvalid ? 1 : -1;
            }
            return xIndex.CompareTo(yIndex);
        }
    }
}