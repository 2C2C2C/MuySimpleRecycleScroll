using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extend;

namespace RecycleScrollView
{
    public partial class RecycleGridScroll
    {
        [System.Serializable]
        public struct RecycleScrollGridElementNavigationParams
        {
            public Vector2 normalizedPositionInViewPort;
            public Vector2 normalizedElementRectPositionOffset;
        }

        [Header("Navigation params")]
        [SerializeField]
        private RecycleScrollGridElementNavigationParams _defaultNavigationParams;

        /// <summary> Jump to element instantly </summary>
        public void JumpTo(int dataIndex)
        {
            if (HasDataSource)
            {
                if (0 > dataIndex || m_dataSource.DataElementCount <= dataIndex)
                {
                    return;
                }

                RectTransform viewport = _scrollRect.viewport;
                Vector2 viewportSize = viewport.rect.size;
                Vector2 gridSize = _gridLayoutData.gridSize;
                Vector2 elementInViewportPos = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, _defaultNavigationParams.normalizedPositionInViewPort);
                Vector2 elementOffset = _defaultNavigationParams.normalizedElementRectPositionOffset;
                // Move to bottom left
                elementInViewportPos.x -= gridSize.x * (1f - elementOffset.x);
                elementInViewportPos.y -= gridSize.y * (1f - elementOffset.y);

                RectTransform content = _scrollRect.content;
                Vector2 contentSize = content.rect.size;
                // Calculate current position
                Vector2 elementRectPosition = TransferElementRectPositionToContentRectPosition(dataIndex, Vector2.zero);
                Vector3 worldPos = RectTransformEx.TransformNormalizedRectPositionToWorldPosition(content, new Vector2(elementRectPosition.x / contentSize.x, elementRectPosition.y / contentSize.y));
                Vector2 elementPosInViewport = (Vector2)viewport.InverseTransformPoint(worldPos);

                Vector2 delta = elementPosInViewport - elementInViewportPos;
                Vector2 scrollSize = new Vector2(contentSize.x - viewportSize.x, contentSize.y - viewportSize.y);
                Vector2 normalizedMove = new Vector2(delta.x / scrollSize.x, delta.y / scrollSize.y);
                if (!_scrollRect.horizontal)
                {
                    normalizedMove.x = 0f;
                }
                if (!_scrollRect.vertical)
                {
                    normalizedMove.y = 0f;
                }
                Vector2 nextNormalizedPos = _scrollRect.normalizedPosition + normalizedMove;
                nextNormalizedPos.x = Mathf.Clamp01(nextNormalizedPos.x);
                nextNormalizedPos.y = Mathf.Clamp01(nextNormalizedPos.y);
                _scrollRect.normalizedPosition = nextNormalizedPos;
            }
        }

        private Vector2 TransferElementRectPositionToContentRectPosition(int dataIndex, Vector2 normalizedElementRectPosition)
        {
            dataIndex = Mathf.Clamp(dataIndex, 0, SimulatedDataCount - 1);
            int primaryCount = Mathf.Max(1, _gridLayoutData.constraintCount);
            int indexInGroup = dataIndex % primaryCount;
            int groupIndex = dataIndex / primaryCount;

            int rowIndex, columnIndex;
            if (_gridLayoutData.startAxis == GridLayoutGroup.Axis.Horizontal)
            {
                columnIndex = indexInGroup;
                rowIndex = groupIndex;
            }
            else // Vertical primary
            {
                rowIndex = indexInGroup;
                columnIndex = groupIndex;
            }

            Vector2 gridSize = _gridLayoutData.gridSize;
            Vector2 spacing = _gridLayoutData.Spacing;
            // Grid stepping
            float stepX = gridSize.x + spacing.x;
            float stepY = gridSize.y + spacing.y;

            RectOffset padding = _gridLayoutData.RectPadding;
            Vector2 contentSize = _scrollRect.content.rect.size;
            // Default is bottom left
            Vector2 rectPosition = new Vector2(normalizedElementRectPosition.x * gridSize.x, normalizedElementRectPosition.y * gridSize.y);
            switch (_gridLayoutData.startCorner)
            {
                case GridLayoutGroup.Corner.UpperLeft:
                    rectPosition.y = contentSize.y - padding.top - gridSize.y - rowIndex * stepY;
                    rectPosition.x = padding.left + columnIndex * stepX;
                    break;
                case GridLayoutGroup.Corner.UpperRight:
                    rectPosition.y = contentSize.y - padding.top - gridSize.y;
                    rectPosition.x = contentSize.x - padding.right - gridSize.x - columnIndex * stepX;
                    break;
                case GridLayoutGroup.Corner.LowerLeft:
                    rectPosition.x = padding.left + columnIndex * stepX;
                    rectPosition.y = padding.bottom + rowIndex * stepY;
                    break;
                case GridLayoutGroup.Corner.LowerRight:
                    rectPosition.x = contentSize.x - padding.right - gridSize.x - columnIndex * stepX;
                    rectPosition.y = padding.bottom + rowIndex * stepY;
                    break;
                default:
                    break;
            }

            return rectPosition;
        }

    }
}