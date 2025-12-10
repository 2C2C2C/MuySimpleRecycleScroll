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

        private void OnDrawGizmos()
        {
            if (_alwaysDrawGizmos)
            {
                DrawElementGizmos();
                DrawDefaultJumpToSettingGizmos();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!_alwaysDrawGizmos)
            {
                DrawElementGizmos();
                DrawDefaultJumpToSettingGizmos();
            }
        }

        private void DrawElementGizmos()
        {
            if (Application.isPlaying && null != m_dataSource && null != m_positionList)
            {
                RectTransform center = (null == _overrideRadialCenter) ? (RectTransform)transform : _overrideRadialCenter;
                Vector3 centerWorldPos = center.position;
                Matrix4x4 containerLocalToWorld = _elementContainer.localToWorldMatrix;
                Matrix4x4 containerWorldToLocal = _elementContainer.worldToLocalMatrix;
                Color prevColor = Gizmos.color;
                for (int i = 0, length = m_positionList.Count; i < length; i++)
                {
                    ElementPositionData elementPositionDat = m_positionList[i];
                    Vector3 elementWorldPosition = elementPositionDat.worldPosition;

                    Vector3 positionInsideContainer = containerWorldToLocal.MultiplyPoint(elementWorldPosition);
                    Color color = elementPositionDat.canShow ? Color.white : Color.gray;
                    // Draw element gizmo
                    DrawWireRectGizmo(positionInsideContainer, containerLocalToWorld, _previewElementSize, color);
                    Gizmos.DrawLine(elementWorldPosition, centerWorldPos);
                    // Draw index
                    GUIStyle gridIndexLableStyle = new GUIStyle()
                    {
                        fontSize = INDEX_LABEL_FONT_SIZE,
                        normal = new GUIStyleState() { textColor = color },
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                    };
                    DrawTextHandle(positionInsideContainer, elementPositionDat.elmentIndex.ToString(), containerLocalToWorld, gridIndexLableStyle);
                }
                Gizmos.color = prevColor;
            }
            else
            {
                int drawElementCount = Mathf.FloorToInt(360 / _internvalAngle);
                RectTransform center = (null == _overrideRadialCenter) ? (RectTransform)transform : _overrideRadialCenter;
                Vector3 centerWorldPos = center.position;
                Matrix4x4 localToWorld = _elementContainer.localToWorldMatrix;
                float radius = Mathf.Abs(_radius); // lul
                float angle = _startAngle;
                Color prevColor = Gizmos.color;
                for (int i = 0; i < drawElementCount; i++)
                {
                    Vector3 v3Pos = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
                    v3Pos *= radius;
                    Vector3 pos = localToWorld.MultiplyPoint(v3Pos);
                    GUIStyle gridIndexLableStyle = new GUIStyle()
                    {
                        fontSize = INDEX_LABEL_FONT_SIZE,
                        normal = new GUIStyleState() { textColor = Gizmos.color },
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                    };

                    DrawWireRectGizmo(v3Pos, localToWorld, _previewElementSize, Gizmos.color);
                    Gizmos.DrawLine(pos, centerWorldPos);
                    DrawTextHandle(v3Pos, i.ToString(), localToWorld, gridIndexLableStyle);
                    angle += _antiClockwise ? _internvalAngle : -_internvalAngle;
                }
                Gizmos.color = prevColor;
            }
        }

        private void DrawWireRectGizmo(Vector3 localPosOfCenter, Matrix4x4 localToWorld, Vector2 size, Color color)
        {
            Vector2 halfSize = 0.5f * size;
            Vector3 topLeft = localPosOfCenter + new Vector3(-halfSize.x, halfSize.y);
            topLeft = localToWorld.MultiplyPoint(topLeft);
            Vector3 topRight = localPosOfCenter + new Vector3(halfSize.x, halfSize.y);
            topRight = localToWorld.MultiplyPoint(topRight);
            Vector3 bottomLeft = localPosOfCenter + new Vector3(-halfSize.x, -halfSize.y);
            bottomLeft = localToWorld.MultiplyPoint(bottomLeft);
            Vector3 bottomRight = localPosOfCenter + new Vector3(halfSize.x, -halfSize.y);
            bottomRight = localToWorld.MultiplyPoint(bottomRight);

            Color prevColor = color;
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

        private void DrawDefaultJumpToSettingGizmos()
        {
            RectTransform center = (null == _overrideRadialCenter) ? (RectTransform)transform : _overrideRadialCenter;
            float angle = _jumpToAngle;
            Vector3 v3Pos = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
            v3Pos *= _radius;
            Vector3 pos = _elementContainer.TransformPoint(v3Pos);
            Color prevColor = Gizmos.color;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(pos, center.position);
            Gizmos.color = prevColor;
        }

        private void OnValidate()
        {
            _startAngle %= 360f;
            _jumpToAngle %= 360f;
            _internvalAngle %= 360f;
        }

#endif

    }
}