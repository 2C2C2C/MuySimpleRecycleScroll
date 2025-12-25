using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace RecycleScrollView
{
    // TODO Figure it out how to sync scroll position to bar properly 
    public partial class Scroller
    {
        [SerializeField]
        private Scrollbar _horizontalScrollBar;
        [SerializeField]
        private Scrollbar _verticalScrollBar;

        private float m_horizontalScrollBarSize = 0.1f;
        private float m_verticalScrollBarSize = 0.1f;

        private UnityAction<float> m_onHorizontalScrollBarValueChanged;
        private UnityAction<float> m_onVerticalScrollBarValueChanged;

        private void BindScrollBars()
        {
            if (null != _horizontalScrollBar)
            {
                if (Horizontal)
                {
                    if (null == m_onHorizontalScrollBarValueChanged)
                    {
                        m_onHorizontalScrollBarValueChanged = new UnityAction<float>(OnHorizontalBarValueChanged);
                    }
                    _horizontalScrollBar.onValueChanged.AddListener(m_onHorizontalScrollBarValueChanged);
                    _horizontalScrollBar.gameObject.SetActive(true);
                }
                else
                {
                    _horizontalScrollBar.gameObject.SetActive(false);
                }
            }

            if (null != _verticalScrollBar)
            {
                if (Vertical)
                {
                    if (null == m_onVerticalScrollBarValueChanged)
                    {
                        m_onVerticalScrollBarValueChanged = new UnityAction<float>(OnVerticalBarValueChanged);
                    }
                    _verticalScrollBar.onValueChanged.AddListener(m_onVerticalScrollBarValueChanged);
                    _verticalScrollBar.gameObject.SetActive(true);
                }
                else
                {
                    _verticalScrollBar.gameObject.SetActive(false);
                }
            }
        }

        private void UnbindScrollBars()
        {
            if (null != m_onHorizontalScrollBarValueChanged && null != _horizontalScrollBar)
            {
                _horizontalScrollBar.onValueChanged.RemoveListener(m_onHorizontalScrollBarValueChanged);
            }
            if (null != m_onVerticalScrollBarValueChanged && null != _verticalScrollBar)
            {
                _verticalScrollBar.onValueChanged.RemoveListener(m_onVerticalScrollBarValueChanged);
            }
        }

        private void SyncValueToScrollBar()
        {
            Vector2 normalizedPosition = NormalizedPosition;
            if (null != _horizontalScrollBar)
            {
                if (Horizontal)
                {
                    _horizontalScrollBar.SetValueWithoutNotify(normalizedPosition.x);
                    if (!_horizontalScrollBar.gameObject.activeSelf)
                    {
                        _horizontalScrollBar.gameObject.SetActive(true);
                    }
                }
                else
                {
                    _horizontalScrollBar.gameObject.SetActive(false);
                }
            }

            if (null != _verticalScrollBar)
            {
                if (Vertical)
                {
                    _verticalScrollBar.SetValueWithoutNotify(normalizedPosition.y);
                    if (!_verticalScrollBar.gameObject.activeSelf)
                    {
                        _verticalScrollBar.gameObject.SetActive(false);
                    }
                }
                else
                {
                    _verticalScrollBar.gameObject.SetActive(false);
                }
            }

            ApplyOffsetToScrollBars(BeyoudEdgeOffset);
        }

        private void ApplyOffsetToScrollBars(Vector2 offset)
        {
            if (null != _horizontalScrollBar)
            {
                float offsetX = Mathf.Abs(offset.x);
                if (0 < m_contentBounds.size.x && 0 < offsetX)
                {
                    _horizontalScrollBar.size = (1f - Mathf.Clamp01(offsetX / m_viewportBounds.size.x)) * m_horizontalScrollBarSize;
                }
                else
                {
                    _horizontalScrollBar.size = m_horizontalScrollBarSize;
                }
            }

            if (null != _verticalScrollBar)
            {
                float offsetY = Mathf.Abs(offset.y);
                if (0 < m_contentBounds.size.y && 0 < offsetY)
                {
                    _verticalScrollBar.size = (1f - Mathf.Clamp01(offsetY / m_viewportBounds.size.y)) * m_verticalScrollBarSize;
                }
                else
                {
                    _verticalScrollBar.size = m_verticalScrollBarSize;
                }
            }
        }

        private void OnHorizontalBarValueChanged(float value)
        {
            if (Horizontal)
            {
                SetNormalizedPosition(Mathf.Clamp01(value), (int)RectTransform.Axis.Horizontal);
            }
        }

        private void OnVerticalBarValueChanged(float value)
        {
            if (Vertical)
            {
                SetNormalizedPosition(Mathf.Clamp01(value), (int)RectTransform.Axis.Vertical);
            }
        }

    }
}