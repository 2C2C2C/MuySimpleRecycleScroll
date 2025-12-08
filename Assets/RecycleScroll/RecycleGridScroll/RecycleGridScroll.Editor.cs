#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEngine.UI.Extend;
using Microsoft.Unity.VisualStudio.Editor;

namespace RecycleScrollView
{
    public partial class RecycleGridScroll
    {
        private const int INDEX_LABEL_FONT_SIZE = 16;

        [Space, Header("Debug draw")]
        [SerializeField]
        private bool _alwaysDrawGizmos;
        [SerializeField]
        private bool _drawContentSize = true;
        [SerializeField]
        private bool _drawGrids = true;

        private void OnDrawGizmosSelected()
        {
            if (_alwaysDrawGizmos)
            {
                GizmoDrawStuff();
            }
        }

        private void OnDrawGizmos()
        {
            if (!_alwaysDrawGizmos)
            {
                GizmoDrawStuff();
            }
        }

        private void GizmoDrawStuff()
        {
            if (_drawGrids)
            {
                GizmoDrawContentGrids(m_simulatedDataCount);
            }
            if (_drawContentSize)
            {
                DrawDebugContentSize(m_simulatedDataCount);
            }
            GizmoDrawDefaultNavigationPosition();
        }

        private void DrawDebugContentSize(int dataCount)
        {
            if (0 == dataCount)
            {
                return;
            }

            Vector2 rawSize = CalculateContentSize(dataCount);
            RectTransform scrollContent = _scrollRect.content;
            Vector2 contentPivot = scrollContent.pivot;
            Vector2 pivotLocalPosition = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(scrollContent, contentPivot);
            Vector2 rectBottomLeftLocalPosition = pivotLocalPosition;
            rectBottomLeftLocalPosition.x = pivotLocalPosition.x - rawSize.x * contentPivot.x;
            rectBottomLeftLocalPosition.y = pivotLocalPosition.y - rawSize.y * contentPivot.y;

            Vector2 rectBottomRightLocalPosition = rectBottomLeftLocalPosition + Vector2.right * rawSize.x;
            Vector2 rectTopLeftLocalPosition = rectBottomLeftLocalPosition + Vector2.up * rawSize.y;
            Vector2 rectTopRightLocalPosition = rectBottomRightLocalPosition + Vector2.up * rawSize.y;

            Matrix4x4 localToWorld = scrollContent.localToWorldMatrix;
            Vector3 bottomLeft = localToWorld.MultiplyPoint(rectBottomLeftLocalPosition);
            Vector3 BottomRight = localToWorld.MultiplyPoint(rectBottomRightLocalPosition);
            Vector3 topLeft = localToWorld.MultiplyPoint(rectTopLeftLocalPosition);
            Vector3 topRight = localToWorld.MultiplyPoint(rectTopRightLocalPosition);

            Color prevColor = Gizmos.color;
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topLeft, bottomLeft);
            Gizmos.DrawLine(topRight, BottomRight);
            Gizmos.DrawLine(bottomLeft, BottomRight);
            Gizmos.color = prevColor;
        }

        private void GizmoDrawContentGrids(int dataCount)
        {
            if (IsCurrentLayoutDataInvalid())
            {
                return;
            }

            Color gridColor = Color.blue;
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

            GUIStyle gridIndexLableStyle = new GUIStyle()
            {
                fontSize = INDEX_LABEL_FONT_SIZE,
                normal = new GUIStyleState() { textColor = gridColor },
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };

            Rect viewportRect = _scrollRect.viewport.rect;
            Matrix4x4 worldToViewportLocal = _scrollRect.viewport.worldToLocalMatrix;

            RectTransform content = _scrollRect.content;
            Matrix4x4 localToWorld = content.localToWorldMatrix;
            int constraintCount = _gridLayoutData.constraintCount;
            int groupCount = (dataCount % constraintCount > 0) ? (dataCount / constraintCount) + 1 : (dataCount / constraintCount);
            Vector2 groupStartPos = startLocalPosition;
            int gridDataIndex = 0;
            for (int i = 0; i < groupCount; i++)
            {
                Vector2 gridGroupStartPos = groupStartPos + (i * new Vector2(gridGroupMoveDirection.x * gridGroupSpacing.x, gridGroupMoveDirection.y * gridGroupSpacing.y));
                gridGroupStartPos += i * new Vector2(gridGroupMoveDirection.x * gridSize.x, gridGroupMoveDirection.y * gridSize.y);
                for (int j = 0; j < constraintCount; j++)
                {
                    Vector2 gridStartPos = gridGroupStartPos + (j * new Vector2(girdMoveDirection.x * gridSpacing.x, girdMoveDirection.y * gridSpacing.y));
                    gridStartPos += j * new Vector2(girdMoveDirection.x * gridSize.x, girdMoveDirection.y * gridSize.y);

                    // HACK Have to covert the rect to viewport's local space
                    Vector2 rectMinPoint = worldToViewportLocal.MultiplyPoint(localToWorld.MultiplyPoint(gridStartPos));
                    Rect gridRect = new Rect(rectMinPoint, gridSize);
                    bool isInterestedWithViewport = viewportRect.Overlaps(gridRect);

                    bool isOutOfDataCount = gridDataIndex >= dataCount;
                    Color gizmoColor = isOutOfDataCount ? Color.yellow * 0.5f : (isInterestedWithViewport ? gridColor : 0.75f * gridColor);
                    gridIndexLableStyle.normal.textColor = gizmoColor;
                    DrawRectGizmo(gridStartPos, gridSize, localToWorld, gizmoColor);
                    DrawGridIndexHandle(gridStartPos, gridSize, gridDataIndex, localToWorld, gridIndexLableStyle);
                    ++gridDataIndex;
                }
            }
        }

        private void DrawGridIndexHandle(Vector3 bottomLeft, Vector3 itemSize, int index, Matrix4x4 toWorldMatrix, GUIStyle labelStyle = null)
        {
            Vector3 center = bottomLeft;
            center += new Vector3(0.5f * itemSize.x, 0.5f * itemSize.y, 0f);
            string labelText = index.ToString();
            Vector3 drawPosition = toWorldMatrix.MultiplyPoint(center);
            if (null == labelStyle)
            {
                labelStyle = new GUIStyle()
                {
                    fontSize = INDEX_LABEL_FONT_SIZE,
                    normal = new GUIStyleState() { textColor = Color.green },
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                };
            }
            Handles.Label(drawPosition, labelText, labelStyle);
        }

        private void GizmoDrawDefaultNavigationPosition()
        {
            if (null == _scrollRect || null == _scrollRect.viewport)
            {
                return;
            }

            Color tempColor = Color.green;
            Color prevColor = Gizmos.color;
            Gizmos.color = tempColor;
            RectTransform viewport = _scrollRect.viewport;
            Vector2 normalizedPositionInViewPort = _defaultNavigationParams.normalizedPositionInViewPort;
            // Draw vertical ref line
            Vector2 viewPortLocalLeft = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, new Vector2(-0.1f, normalizedPositionInViewPort.y));
            Vector2 viewPortLocalRight = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, new Vector2(1.1f, normalizedPositionInViewPort.y));
            Vector3 viewPortLocalLeftWorld = viewport.TransformPoint(viewPortLocalLeft);
            Vector3 viewPortLocalRightWorld = viewport.TransformPoint(viewPortLocalRight);
            Gizmos.DrawLine(viewPortLocalLeftWorld, viewPortLocalRightWorld);

            // Draw horizontal ref line
            Vector2 viewPortLocalTop = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, new Vector2(normalizedPositionInViewPort.x, 1.1f));
            Vector2 viewPortLocalBottom = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, new Vector2(normalizedPositionInViewPort.x, -0.1f));
            Vector3 viewPortLocalTopWorld = viewport.TransformPoint(viewPortLocalTop);
            Vector3 viewPortLocalBottomWorld = viewport.TransformPoint(viewPortLocalBottom);
            Gizmos.DrawLine(viewPortLocalBottomWorld, viewPortLocalTopWorld);

            // Draw element
            Vector2 elementSize = _gridLayoutData.gridSize;
            Vector2 elementInViewportPos = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, normalizedPositionInViewPort);
            Vector2 elementOffset = _defaultNavigationParams.normalizedElementRectPositionOffset;
            elementInViewportPos.x -= elementSize.x * (1f - elementOffset.x);
            elementInViewportPos.y -= elementSize.y * (1f - elementOffset.y);
            DrawRectGizmo(elementInViewportPos, elementSize, viewport.localToWorldMatrix, tempColor);

            Gizmos.color = prevColor;
        }

        private void DrawRectGizmo(Vector2 bottomLeft, Vector2 size, Matrix4x4 toWorldMatrix, Color color)
        {
            Vector2 topLeft = bottomLeft + new Vector2(0f, size.y);
            Vector2 topRight = topLeft + new Vector2(size.x, 0f);
            Vector2 bottomRight = bottomLeft + new Vector2(size.x, 0f);

            Vector3 bottomLeftWorld = toWorldMatrix.MultiplyPoint(bottomLeft);
            Vector3 bottomRightWorld = toWorldMatrix.MultiplyPoint(bottomRight);
            Vector3 topLeftWorld = toWorldMatrix.MultiplyPoint(topLeft);
            Vector3 topRightWorld = toWorldMatrix.MultiplyPoint(topRight);

            Color prevColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawLine(bottomLeftWorld, bottomRightWorld);
            Gizmos.DrawLine(topLeftWorld, topRightWorld);

            Gizmos.DrawLine(bottomLeftWorld, topLeftWorld);
            Gizmos.DrawLine(bottomRightWorld, topRightWorld);

            Gizmos.color = prevColor;
        }

        private void ChangeObjectName_EditorOnly(MonoBehaviour behaviour, int dataIndex)
        {
            if (INVALID_INDEX == dataIndex)
            {
                behaviour.name = $"Element NonUsed";
            }
            else
            {
                behaviour.name = $"Element {dataIndex}";
            }
        }

        private void Reset()
        {
            if (TryGetComponent<UnityScrollRectExtended>(out _scrollRect))
            {
                _scrollRect.StopMovement();
                return;
            }
            Debug.LogWarning("[RecycleScrollGrid] should be on the same GameObject with ScrollRect, please remove this component and add RecycleScrollGrid to ScrollRect GameObject", this.gameObject);
        }

    }
}
#endif