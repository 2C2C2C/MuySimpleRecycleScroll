using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

namespace RecycleScrollView
{
    [RequireComponent(typeof(UnityScrollRectExtended))]
    public partial class RecycleRadialScroll : MonoBehaviour, IRecycleScroll
    {
        [SerializeField]
        private UnityScrollRectExtended _scrollRect;
        /// <summary> If it is null, the transform of this script will be the center</summary>
        [SerializeField]
        private RectTransform _overrideRadialCenter;
        [SerializeField]
        private RectTransform _elementContainer;

        [SerializeField]
        private ScrollDirection _dragContentScrollDirection;

        private bool m_needUpdateThisFrame = false;
        private float m_totalRotateAngle = 0f;
        private float m_normalizedProgress;

        private IRecycleScrollDataSource m_dataSource;

        private UnityAction<Vector2> m_onScrollerValueChanged;
        private Action m_afterScrollrectUpdate;

        public bool IsVertical =>
            ScrollDirection.Vertical_UpToDown == _dragContentScrollDirection ||
            ScrollDirection.Vertical_DownToUp == _dragContentScrollDirection;
        public bool IsHorizontal =>
            ScrollDirection.Horizontal_LeftToRight == _dragContentScrollDirection ||
            ScrollDirection.Horizontal_RightToLeft == _dragContentScrollDirection;

        public void UnInit()
        {
            m_totalRotateAngle = 0;
            m_dataSource = null;
        }

        public void Init(IRecycleScrollDataSource dataSource)
        {
            if (null == dataSource)
            {
                return;
            }

            if (null == m_dataSource)
            {
                m_dataSource = dataSource;
                ApplyLayoutSetting();
                AdjustCachedElements();
                OnDataCountChanged(0, m_dataSource.DataElementCount);
                m_needUpdateThisFrame = true;
            }
            else
            {
                // Already regist
            }
        }

        public void AddElementTotail()
        {

        }

        public void AddElementsToTail(int count)
        {

        }

        public void InsertElement(int dataIndex)
        {

        }

        public void InsertElements(int dataIndex, int count)
        {

        }

        public void InsertElements(IReadOnlyList<int> sortedDataIndexList)
        {

        }

        public void RemoveElement(int dataIndex)
        {

        }

        public void RemoveElements(int dataIndex, int count)
        {

        }

        public void RemoveElements(IReadOnlyList<int> sortedDataIndexList)
        {
        }

        public void UpdateElement(int dataIndex)
        {
        }

        private void OnDataCountChanged(int prev, int next)
        {
            m_totalRotateAngle = _internvalAngle * (next - 1);
            // Adjust scroll content size
            RectTransform scrollContent = _scrollRect.content;
            RectTransform viewport = _scrollRect.viewport;

            float radius = _radius;
            float scrollLength = radius * m_totalRotateAngle * Mathf.Deg2Rad;
            if (IsVertical)
            {
                scrollLength += viewport.rect.size.y;
                scrollContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, scrollLength);
            }
            else if (IsHorizontal)
            {
                scrollLength += viewport.rect.size.x;
                scrollContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, scrollLength);
            }
            m_needUpdateThisFrame = true;
        }

        private void OnScrollerValueChanged(Vector2 normalizedValue)
        {
            m_needUpdateThisFrame = true;
        }

        private void AfterScrollRectUpdate()
        {
            if (m_needUpdateThisFrame)
            {
                Vector2 normalizedValue = _scrollRect.normalizedPosition;
                float nextNormalizedValue = _dragContentScrollDirection switch
                {
                    ScrollDirection.Horizontal_LeftToRight => normalizedValue.x,
                    ScrollDirection.Horizontal_RightToLeft => 1f - normalizedValue.x,
                    ScrollDirection.Vertical_UpToDown => 1f - normalizedValue.y,
                    ScrollDirection.Vertical_DownToUp => normalizedValue.y,
                    _ => 0f,
                };
                m_normalizedProgress = nextNormalizedValue;
                ApplyScrollProcess();
            }
        }

        private void Awake()
        {
            _startAngle %= 360f;
            _jumpToAngle %= 360f;
            _internvalAngle %= 360f;
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