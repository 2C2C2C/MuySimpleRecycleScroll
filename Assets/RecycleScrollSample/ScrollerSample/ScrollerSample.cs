using System;
using UnityEngine;
using UnityEngine.UI;

namespace RecycleScrollView.Sample
{
    public class ScrollerSample : MonoBehaviour, IScrollMoveTarget
    {
        [SerializeField]
        private ScrollRect _unityScrollrect;
        [SerializeField]
        private Scroller _scroller;
        [SerializeField]
        private Vector2 _startSize = Vector2.zero;

        private Vector2 m_noramlizedProgress = Vector2.one;
        private Vector2 m_progress = Vector2.one;

        public Vector2 ConvertToNormalizedMoveFromCurrentPosition(Vector2 move, out Vector2 offset)
        {
            Vector2 result = Vector2.one;
            result.x = (_scroller.Horizontal && _startSize.x > 0f) ? move.x / _startSize.x : 0f;
            result.y = (_scroller.Vertical && _startSize.y > 0f) ? move.y / _startSize.y : 0f;
            // Debug.LogError($"Convert {move} to {result} by size {_startSize}");

            offset = Vector2.zero;
            for (int i = 0; i < 2; i++)
            {
                int axis = i;
                float tempValue = m_noramlizedProgress[axis] + result[axis];
                if (0f > tempValue)
                {
                    offset[axis] = tempValue * _startSize[axis];
                }
                else if (1f < tempValue)
                {
                    offset[axis] = (tempValue - 1f) * _startSize[axis];
                }
            }
            return result;
        }

        public void SetNormalizedPosition(Vector2 normalizedPosition)
        {
            m_noramlizedProgress = normalizedPosition;
            m_progress = new Vector2(_startSize.x * m_noramlizedProgress.x, _startSize.y * m_noramlizedProgress.y);
        }

        [ContextMenu(nameof(PrintUnityScrollRectScrollPosition))]
        private void PrintUnityScrollRectScrollPosition()
        {
            Debug.LogError($"Unity ScrollRect scroll position {_unityScrollrect.normalizedPosition}");
        }

        private void OnScrollerValueChanged(Vector2 arg0, Vector2 arg1)
        {
            m_noramlizedProgress = arg0;
        }

        private void Start()
        {
            m_noramlizedProgress = Vector2.zero;
            _scroller.Setup(this);
        }

        private void OnEnable()
        {
            _scroller.OnScrollerValueChanged.AddListener(OnScrollerValueChanged);
        }

        private void OnDisable()
        {
            _scroller.OnScrollerValueChanged.RemoveListener(OnScrollerValueChanged);
        }

    }
}