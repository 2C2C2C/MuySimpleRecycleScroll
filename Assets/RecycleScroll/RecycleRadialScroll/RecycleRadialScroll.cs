using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityMathf = UnityEngine.Mathf;
using System.Collections.Generic;

namespace RecycleScrollView
{
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
        private ScrollRect _scroller;
        [SerializeField]
        private RadialLayout _radiaLayout;

       
        /// <summary>
        /// When normalized postion is 0
        /// </summary>
        [SerializeField]
        private float _totalRotateAngle = 0f;

        [SerializeField]
        private ReceiveScrollPositionType _scrollType;

        [NonSerialized]
        private float m_normalizedProgress;

        private IRecycleScrollDataSource m_dataSource;

        private UnityAction<Vector2> m_onScrollerValueChanged;

        public void UnInit()
        {
            throw new NotImplementedException();
        }

        public void Init(IRecycleScrollDataSource dataSource)
        {
            throw new NotImplementedException();
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

    }
}