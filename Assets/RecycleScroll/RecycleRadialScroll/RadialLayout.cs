using System.Collections.Generic;

namespace UnityEngine.UI.Extension
{
    /* @Hiko
     * User need to be careful for the case dat there are too many elements so the warp each other :(
     */
    public class RadialLayout : LayoutGroup
    {
        [SerializeField]
        private float _radius;
        // start from right
        [SerializeField, Range(0f, 360f)]
        private float _startAngle;
        [SerializeField, Range(0f, 360f)]
        private float _internvalAngle;
        [SerializeField]
        private bool _antiClockwise;
        [SerializeField]
        private bool _inverse = false;

        private List<RectTransform> m_children;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nextValue">should be 0 ~ 360</param>
        public void ChangeStartAngle(float nextValue)
        {
            _startAngle = Mathf.Clamp(nextValue, 0f, 360f);
            SetElements();
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        public override void SetLayoutHorizontal() { }

        public override void SetLayoutVertical() { }

        public override void CalculateLayoutInputVertical()
        {
            SetElements();
        }

        public override void CalculateLayoutInputHorizontal()
        {
            SetElements();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetElements();
        }

        protected override void OnDisable()
        {
            ClearChildren();
            LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
        }

        private void SetElements()
        {
            RefillChildren();
            if (0 == m_children.Count)
            {
                return;
            }

            float angle = _startAngle;
            float radius = Mathf.Abs(_radius); // lul
            for (int i = 0, length = m_children.Count; i < length; i++)
            {
                RectTransform child = _inverse ? m_children[length - i - 1] : m_children[i];
                Vector3 pos = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
                child.localPosition = pos * radius;
                angle += _antiClockwise ? _internvalAngle : -_internvalAngle;
            }

        }

        private void RefillChildren()
        {
            ClearChildren();
            // only collect active elements since unity layout only do stuff for active objects
            if (null == m_children)
            {
                m_children = new List<RectTransform>();
            }
            for (int i = 0, length = transform.childCount; i < length; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.gameObject.activeSelf && child is RectTransform validChild)
                {
                    m_children.Add(validChild);
                    // Adding the elements to the tracker stops the user from modifying their positions via the editor.
                    m_Tracker.Add(this, validChild, DrivenTransformProperties.Anchors | DrivenTransformProperties.AnchoredPosition);
                }
            }
        }

        private void ClearChildren()
        {
            m_Tracker.Clear();
            if (null != m_children)
            {
                m_children.Clear();
            }
        }

#if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();
            SetElements();
        }

        public bool _debugDraw;
        [Range(0, 360)]
        public short _debugDrawElementCount;
        private void OnDrawGizmos()
        {
            if (_debugDraw) // draw debug lines
            {
                Vector3 selfWorldPos = transform.position;
                Matrix4x4 localToWorld = transform.localToWorldMatrix;
                float radius = Mathf.Abs(_radius); // lul
                float angle = _startAngle;
                Color prevColor = Gizmos.color;
                Color wireColor = Color.white;
                for (int i = 0; i < _debugDrawElementCount; i++)
                {
                    Vector3 vPos = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
                    vPos *= radius;
                    Vector3 pos = localToWorld.MultiplyPoint(vPos);
                    if (i == _debugDrawElementCount - 1)
                    {
                        wireColor = Color.gray;
                        Gizmos.color = wireColor;
                    }
                    Gizmos.DrawLine(pos, selfWorldPos);
                    angle += _antiClockwise ? _internvalAngle : -_internvalAngle;
                }
                Gizmos.color = prevColor;
            }
        }

#endif

    }
}