using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extend;
using UnityObject = UnityEngine.Object;

namespace RecycleScrollView
{
    public partial class RecycleSingleDirectionScroll
    {
        [Header("Debug params")]
        [SerializeField]
        private bool _enableLog = false;

#if UNITY_EDITOR

        [SerializeField]
        private bool _alwaysDrawGizmos;

        private void OnDrawGizmos()
        {
            if (_alwaysDrawGizmos)
            {
                GizmoDrawDefaultNavigationPosition();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!_alwaysDrawGizmos)
            {
                GizmoDrawDefaultNavigationPosition();
            }
        }

        private void GizmoDrawDefaultNavigationPosition()
        {
            if (null == _scrollRect || null == _scrollRect.viewport)
            {
                return;
            }

            Vector2 refElementSize = new Vector2(100, 100);
            Color prevColor = Gizmos.color;
            Gizmos.color = Color.green;
            RectTransform viewport = _scrollRect.viewport;
            if (IsVertical)
            {
                // Draw ref line
                Vector2 viewPortLocalLeft = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, new Vector2(-0.1f, _defaultNavigationParams.normalizedPositionInViewPort));
                Vector2 viewPortLocalRight = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, new Vector2(1.1f, _defaultNavigationParams.normalizedPositionInViewPort));
                Vector3 viewPortLocalLeftWorld = viewport.TransformPoint(viewPortLocalLeft);
                Vector3 viewPortLocalRightWorld = viewport.TransformPoint(viewPortLocalRight);
                Gizmos.DrawLine(viewPortLocalLeftWorld, viewPortLocalRightWorld);
                // Draw ref element position
                Vector2 elementLocalPos = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, new Vector2(0.5f, _defaultNavigationParams.normalizedPositionInViewPort));
                elementLocalPos.x -= refElementSize.x * 0.5f;
                elementLocalPos.y += refElementSize.y * (_defaultNavigationParams.normalizedElementPositionAdjustment - 1f);
                GizmoDrawRect(elementLocalPos, refElementSize, viewport.localToWorldMatrix, Color.yellow);
            }
            else if (IsHorizontal)
            {
                // Draw ref line
                Vector2 viewPortLocalTop = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, new Vector2(_defaultNavigationParams.normalizedPositionInViewPort, 1.1f));
                Vector2 viewPortLocalBottom = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, new Vector2(_defaultNavigationParams.normalizedPositionInViewPort, -0.1f));
                Vector3 viewPortLocalTopWorld = viewport.TransformPoint(viewPortLocalTop);
                Vector3 viewPortLocalBottomWorld = viewport.TransformPoint(viewPortLocalBottom);
                Gizmos.DrawLine(viewPortLocalBottomWorld, viewPortLocalTopWorld);
                // Draw ref element position
                Vector2 elementLocalPos = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, new Vector2(_defaultNavigationParams.normalizedPositionInViewPort, 0.5f));
                elementLocalPos.y -= 0.5f * refElementSize.y;
                elementLocalPos.x -= refElementSize.x * _defaultNavigationParams.normalizedElementPositionAdjustment;
                GizmoDrawRect(elementLocalPos, refElementSize, viewport.localToWorldMatrix, Color.yellow);
            }
            Gizmos.color = prevColor;
        }

        private void GizmoDrawRect(Vector2 localBottomLeftPosition, Vector2 size, Matrix4x4 localToWorldMatrix, Color color)
        {
            Vector2 bottomLeft = new Vector3(localBottomLeftPosition.x, localBottomLeftPosition.y);
            Vector2 bottomRight = new Vector3(localBottomLeftPosition.x + size.x, localBottomLeftPosition.y);
            Vector2 topRight = new Vector3(localBottomLeftPosition.x + size.x, localBottomLeftPosition.y + size.y);
            Vector2 topLeft = new Vector3(localBottomLeftPosition.x, localBottomLeftPosition.y + size.y);
            bottomLeft = localToWorldMatrix.MultiplyPoint(bottomLeft);
            bottomRight = localToWorldMatrix.MultiplyPoint(bottomRight);
            topLeft = localToWorldMatrix.MultiplyPoint(topLeft);
            topRight = localToWorldMatrix.MultiplyPoint(topRight);

            Color prevColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
            Gizmos.color = prevColor;
        }

        // private void PrintEdge()
        // {
        //     RectTransform viewport = _scrollRect.viewport;
        //     Vector2 edgeHead = CalculateNormalizedRectPosition(0f);
        //     Vector2 edgeTail = CalculateNormalizedRectPosition(1f);
        //     Vector3 edgeHeadLocalPos = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, edgeHead);
        //     Vector3 edgeTailLocalPos = RectTransformEx.TransformNormalizedRectPositionToLocalPosition(viewport, edgeTail);
        //     Log($"Check edge local pos; Head {edgeHeadLocalPos}; Tail {edgeTailLocalPos}");
        // }

        private void ChangeObjectName_EditorOnly(MonoBehaviour behaviour, int elementIndex)
        {
            behaviour.name = $"Element {elementIndex}; DataIndex {ElementIndexDataIndex2WayConvert(elementIndex)}";
        }

        private void Reset()
        {
            if (TryGetComponent<UnityScrollRectExtended>(out _scrollRect))
            {
                _scrollRect.content.TryGetComponent<HorizontalOrVerticalLayoutGroup>(out _contentLayoutGroup);
            }
        }

        private void OnValidate()
        {
            ApplyLayoutSetting();
        }

#endif
        private void Log(string msg, UnityObject context = null)
        {
            if (_enableLog)
            {
                string formatedMsg = $"[RecycleScrollView] {msg} | Frame:{Time.frameCount}";
                Debug.Log(formatedMsg, context: context);
            }
        }

        private void LogError(string msg, UnityObject context = null)
        {
            if (_enableLog)
            {
                string formatedMsg = $"[RecycleScrollView] {msg} | Frame:{Time.frameCount}";
                Debug.LogError(formatedMsg, context: context);
            }
        }

    }
}