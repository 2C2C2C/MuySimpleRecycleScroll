using System;
using UnityEngine.Events;
using UnityMathf = UnityEngine.Mathf;

namespace UnityEngine.UI.Extension
{
    public class RadialLayoutScroll : MonoBehaviour
    {
        [Serializable]
        public enum ReceiveScrollPositionType
        {
            None = 0,
            Vertical = 1,
            Horizontal = 2,
        }

        [SerializeField]
        private ScrollRect _scroller;
        [SerializeField]
        private RadialLayout _radiaLayout;

        /// <summary>
        /// When normalized postion is 1
        /// </summary>
        [SerializeField]
        private float _startAngle = 180f;
        /// <summary>
        /// When normalized postion is 0
        /// </summary>
        [SerializeField]
        private float _totalRotateAngle = 0f;

        [SerializeField]
        private ReceiveScrollPositionType _scrollType;

        [NonSerialized]
        private float m_normalizedValue;

        private UnityAction<Vector2> m_onScrollerValueChanged;

        private void OnScrollerValueChanged(Vector2 normalizedValue)
        {
            float nextNormalizedValue = _scrollType switch
            {
                ReceiveScrollPositionType.Horizontal => normalizedValue.x,
                ReceiveScrollPositionType.Vertical => normalizedValue.y,
                _ => 0f,
            };

            m_normalizedValue = nextNormalizedValue;
            float nextAngle = _startAngle + _totalRotateAngle * (1f - nextNormalizedValue);
            //Debug.Log(nextAngle);
            if (0 > nextAngle)
            {
                nextAngle %= 360f;
                nextAngle += 360f;
            }
            _radiaLayout.ChangeStartAngle(nextAngle % 360f);
        }

        private void Awake()
        {
            m_onScrollerValueChanged = new UnityAction<Vector2>(OnScrollerValueChanged);
        }

        private void OnEnable()
        {
            _scroller.onValueChanged.AddListener(m_onScrollerValueChanged);
        }

        private void OnDisable()
        {
            _scroller.onValueChanged.RemoveListener(m_onScrollerValueChanged);
        }

#if UNITY_EDITOR

        [SerializeField, Range(0f, 1f)]
        private float _debugSetPosition;

        [ContextMenu(nameof(SetDebugPosition_EditorOnly))]
        private void SetDebugPosition_EditorOnly()
        {
            _debugSetPosition = UnityMathf.Clamp01(_debugSetPosition);
        }
#endif

    }
}