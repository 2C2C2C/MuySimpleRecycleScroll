using System.ComponentModel;
using UnityEngine;

namespace RecycleScrollView
{
    public partial class RecycleGridScroll
    {
        private const int INVALID_INDEX = -1;

        private bool TryGetHeadIndexOfUsingElements(out int index)
        {
            if (null != m_gridElements && 0 < m_gridElements.Count)
            {
                for (int i = 0, gridCount = m_gridElements.Count; i < gridCount; i++)
                {
                    RecycleGridScrollElement grid = m_gridElements[i];
                    if (INVALID_INDEX == grid.ElementIndex)
                    {
                        continue;
                    }
                    index = grid.ElementIndex;
                    return true;
                }
            }
            index = INVALID_INDEX;
            return false;
        }

        private bool TryGetTailIndexboundOfUsingElements(out int index)
        {
            if (null != m_gridElements && 0 < m_gridElements.Count)
            {
                for (int i = m_gridElements.Count - 1; i > -1; i--)
                {
                    RecycleGridScrollElement grid = m_gridElements[i];
                    if (INVALID_INDEX == grid.ElementIndex)
                    {
                        continue;
                    }
                    index = grid.ElementIndex;
                    return true;
                }
            }
            index = INVALID_INDEX;
            return false;
        }

        private bool IsCurrentLayoutDataInvalid()
        {
            bool isInvalid = null == _gridLayoutData ||
                0 > _gridLayoutData.gridSize.x ||
                0 > _gridLayoutData.gridSize.y ||
                0 > _gridLayoutData.constraintCount;
            return isInvalid;
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

        private void SetElementIndex(RecycleGridScrollElement element, int index)
        {
            if (null != m_dataSource)
            {
                if (INVALID_INDEX == index)
                {
                    m_dataSource.UnInitElement(element.ElementTransform);
                }
                else
                {
                    m_dataSource.ChangeElementIndex(element.ElementTransform, element.ElementIndex, index);
                }
            }
            element.SetIndex(index);
#if UNITY_EDITOR
            ChangeObjectName_EditorOnly(element, index);
#endif
        }

    }
}