using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityMathf = UnityEngine.Mathf;
using System.Collections.Generic;

namespace RecycleScrollView
{
    [RequireComponent(typeof(UnityScrollRectExtended))]
    public partial class RecycleRadialScroll : MonoBehaviour, IRecycleScroll
    {
        [Serializable]
        public enum ReceiveScrollPositionType
        {
            None = 0,
            Vertical = 1,
            Horizontal = 2,
        }

        [SerializeField]
        private UnityScrollRectExtended _scrollRect;
        [SerializeField]
        private ReceiveScrollPositionType _scrollType;
        /// <summary> If it is null, the transform of this script will be the center</summary>
        [SerializeField]
        private RectTransform _overrideRadialCenter;
        [SerializeField]
        private RectTransform _elementContainer;

        /// <summary>
        /// When normalized postion is 0
        /// </summary>
        [SerializeField]
        private float _totalRotateAngle = 0f;

        private float m_normalizedProgress;

        private IRecycleScrollDataSource m_dataSource;

        private UnityAction<Vector2> m_onScrollerValueChanged;
        private Action m_afterScrollrectUpdate;

        public void UnInit()
        {
            throw new NotImplementedException();
        }

        public void Init(IRecycleScrollDataSource dataSource)
        {

        }

        public void AddElementTotail()
        {
            throw new NotImplementedException();
        }

        public void AddElementsToTail(int count)
        {
            throw new NotImplementedException();
        }

        public void InsertElement(int dataIndex)
        {
            throw new NotImplementedException();
        }

        public void InsertElements(int dataIndex, int count)
        {
            throw new NotImplementedException();
        }

        public void InsertElements(IReadOnlyList<int> sortedDataIndexList)
        {
            throw new NotImplementedException();
        }

        public void RemoveElement(int dataIndex)
        {
            throw new NotImplementedException();
        }

        public void RemoveElements(int dataIndex, int count)
        {
            throw new NotImplementedException();
        }

        public void RemoveElements(IReadOnlyList<int> sortedDataIndexList)
        {
            throw new NotImplementedException();
        }

        public void UpdateElement(int dataIndex)
        {
            throw new NotImplementedException();
        }

        private void OnScrollerValueChanged(Vector2 normalizedValue)
        {
            float nextNormalizedValue = _scrollType switch
            {
                ReceiveScrollPositionType.Horizontal => normalizedValue.x,
                ReceiveScrollPositionType.Vertical => normalizedValue.y,
                _ => 0f,
            };

            m_normalizedProgress = nextNormalizedValue;
            float nextAngle = _startAngle + _totalRotateAngle * (1f - nextNormalizedValue);
            //Debug.Log(nextAngle);
            if (0 > nextAngle)
            {
                nextAngle %= 360f;
                nextAngle += 360f;
            }
            // _radiaLayout.ChangeStartAngle(nextAngle % 360f);
        }

        private void AfterScrollRectUpdate()
        {

        }

        private void OnEnable()
        {
            if (null == m_onScrollerValueChanged)
            {
                m_onScrollerValueChanged = new UnityAction<Vector2>(OnScrollerValueChanged);
            }
            _scrollRect.onValueChanged.AddListener(m_onScrollerValueChanged);

            if (null == m_afterScrollrectUpdate)
            {
                m_afterScrollrectUpdate = new Action(AfterScrollRectUpdate);
            }
            _scrollRect.AfterLateUpdate += m_afterScrollrectUpdate;
        }

        private void OnDisable()
        {
            if (null != m_onScrollerValueChanged)
            {
                _scrollRect.onValueChanged.RemoveListener(m_onScrollerValueChanged);
            }

            if (null != m_afterScrollrectUpdate)
            {
                _scrollRect.AfterLateUpdate -= m_afterScrollrectUpdate;
            }
        }

    }
}