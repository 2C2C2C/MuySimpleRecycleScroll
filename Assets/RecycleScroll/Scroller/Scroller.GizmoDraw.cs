#if UNITY_EDITOR || DEBUG

using UnityEngine;

namespace RecycleScrollView
{
    public partial class Scroller
    {
        private void OnDrawGizmos()
        {
            // TODO Draw virtual content
            if (!Application.isPlaying)
            {
                InitDefaultBounds();
            }
            GizmoDrawBounds(m_viewportBounds, _viewport.localToWorldMatrix, Color.blue);
            GizmoDrawBounds(m_contentBounds, _viewport.localToWorldMatrix, Color.magenta);
        }

        private void GizmoDrawBounds(in Bounds bounds, in Matrix4x4 localToWorld, Color color)
        {
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            Vector3 size = bounds.size;
            Color prevColor = Gizmos.color;
            Gizmos.color = color;

            Gizmos.DrawLine(localToWorld.MultiplyPoint(min), localToWorld.MultiplyPoint(min + size.y * Vector3.up));
            Gizmos.DrawLine(localToWorld.MultiplyPoint(min), localToWorld.MultiplyPoint(min + size.x * Vector3.right));
            Gizmos.DrawLine(localToWorld.MultiplyPoint(max), localToWorld.MultiplyPoint(min + size.y * Vector3.up));
            Gizmos.DrawLine(localToWorld.MultiplyPoint(max), localToWorld.MultiplyPoint(min + size.x * Vector3.right));

            Gizmos.color = prevColor;
        }
    }
}

#endif