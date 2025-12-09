using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RecycleScrollView
{
    public partial class RecycleRadialScroll
    {
#if UNITY_EDITOR
        private const int INDEX_LABEL_FONT_SIZE = 16;

        [Header("Debug params")]
        [SerializeField]
        private bool _alwaysDrawGizmos;
        [SerializeField]
        private bool _enableLog = false;
        [SerializeField]
        private Vector2 _previewElementSize = 100f * Vector2.one;

        private void OnDrawGizmos()
        {
            if (_alwaysDrawGizmos)
            {
                DrawGizmos();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!_alwaysDrawGizmos)
            {
                DrawGizmos();
            }
        }

        private void DrawGizmos()
        {
            // TODO if it is in PlayMode, it should draw the actual scoll position
            int drawElementCount = Mathf.FloorToInt(360 / _internvalAngle);
            RectTransform center = (null == _overrideRadialCenter) ? (RectTransform)transform : _overrideRadialCenter;
            Vector3 selfWorldPos = center.position;
            Matrix4x4 localToWorld = _elementContainer.localToWorldMatrix;
            float radius = Mathf.Abs(_radius); // lul
            float angle = _startAngle;
            Color prevColor = Gizmos.color;
            for (int i = 0; i < drawElementCount; i++)
            {
                Vector3 v3Pos = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
                v3Pos *= radius;
                Vector3 pos = localToWorld.MultiplyPoint(v3Pos);
                if (i == drawElementCount - 1)
                {
                    Color wireColor = Color.gray;
                    Gizmos.color = wireColor;
                }

                GUIStyle gridIndexLableStyle = new GUIStyle()
                {
                    fontSize = INDEX_LABEL_FONT_SIZE,
                    normal = new GUIStyleState() { textColor = Gizmos.color },
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                };

                DrawWireRectGizmo(v3Pos, localToWorld, _previewElementSize, Gizmos.color);
                Gizmos.DrawLine(pos, selfWorldPos);
                DrawTextHandle(v3Pos, i.ToString(), localToWorld, gridIndexLableStyle);
                angle += _antiClockwise ? _internvalAngle : -_internvalAngle;
            }
            Gizmos.color = prevColor;
        }

        private void DrawWireRectGizmo(Vector3 localPosOfCenter, Matrix4x4 localToWorld, Vector2 size, Color color)
        {
            Vector3 topLeft = localPosOfCenter + new Vector3(-size.x, size.y);
            topLeft = localToWorld.MultiplyPoint(topLeft);
            Vector3 topRight = localPosOfCenter + new Vector3(size.x, size.y);
            topRight = localToWorld.MultiplyPoint(topRight);
            Vector3 bottomLeft = localPosOfCenter + new Vector3(-size.x, -size.y);
            bottomLeft = localToWorld.MultiplyPoint(bottomLeft);
            Vector3 bottomRight = localPosOfCenter + new Vector3(size.x, -size.y);
            bottomRight = localToWorld.MultiplyPoint(bottomRight);

            Color prevColor = Gizmos.color;
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
            Gizmos.color = prevColor;
        }

        private void DrawTextHandle(Vector3 center, string text, Matrix4x4 toWorldMatrix, GUIStyle labelStyle = null)
        {
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
            Handles.Label(drawPosition, text, labelStyle);
        }

#endif
    }
}