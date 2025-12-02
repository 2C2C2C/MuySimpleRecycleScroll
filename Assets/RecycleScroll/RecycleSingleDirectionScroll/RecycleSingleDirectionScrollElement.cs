using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RecycleScrollView
{
    [RequireComponent(typeof(LayoutElement))]
    public class RecycleSingleDirectionScrollElement : MonoBehaviour
    {
        private static Func<LayoutGroup, int, float> GetTotalPreferredSizeDelegate
        {
            get
            {
                if (null == s_getTotalPreferredSizeDelegate)
                {
                    try
                    {
                        MethodInfo methodInfo = typeof(LayoutGroup).GetMethod("GetTotalPreferredSize", BindingFlags.Instance | BindingFlags.NonPublic);
                        // create an open instance delegate: (LayoutGroup lg, int axis) => float
                        s_getTotalPreferredSizeDelegate = (Func<LayoutGroup, int, float>)Delegate.CreateDelegate
                        (
                            typeof(Func<LayoutGroup, int, float>),
                            null,
                            methodInfo
                        );
                    }
                    catch
                    {
                        s_getTotalPreferredSizeDelegate = null; // fallback to Invoke if delegate creation fails
                    }
                }
                return s_getTotalPreferredSizeDelegate;
            }
        }
        private static Func<LayoutGroup, int, float> s_getTotalPreferredSizeDelegate;

        [SerializeField]
        private LayoutElement _layoutElement;
        [SerializeField]
        private bool _directlyUseSizeFromLayoutElement;
        [SerializeField]
        private bool _forceConvertFlexiableToPreferred;
        [SerializeField]
        private LayoutElementSizeSetter _elementSizeSetter;

        /// <summary> The element index in the scroll content </summary>
        [SerializeField] // TODO This value should be NonSerialized but better to show it in inspector
        private int m_elementIndex = -1;
        /// <summary> The actual data index in the scroll content </summary>
        [SerializeField] // TODO This value should be NonSerialized but better to show it in inspector
        private int m_dataIndex = -1;

        private RectTransform m_rectTransform;

        public RectTransform ElementTransform
        {
            get
            {
                if (null == m_rectTransform)
                {
                    m_rectTransform = transform as RectTransform;
                }
                return m_rectTransform;
            }
        }
        public int ElementIndex => m_elementIndex;
        public int DataIndex => m_dataIndex;
        public Vector2 ElementPreferredSize { get; private set; }

        public void SetIndex(int elementIndex, int dataIndex)
        {
            m_elementIndex = elementIndex;
            m_dataIndex = dataIndex;
        }

        public void ClearPreferredSize()
        {
            // HACK
            if (_forceConvertFlexiableToPreferred)
            {
                const int LAYOUT_SIZE_DISABLE = -1;
                if (Mathf.Approximately(1f, _layoutElement.flexibleHeight))
                {
                    _layoutElement.preferredHeight = LAYOUT_SIZE_DISABLE;
                }
                if (Mathf.Approximately(1f, _layoutElement.flexibleWidth))
                {
                    _layoutElement.preferredWidth = LAYOUT_SIZE_DISABLE;
                }
            }
        }

        [ContextMenu("ForceCalculateSize")]
        public void CalculatePreferredSize()
        {
            Vector2 rectSize = ElementTransform.rect.size;
            if (TryGetComponent<HorizontalOrVerticalLayoutGroup>(out HorizontalOrVerticalLayoutGroup layoutGroup))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(ElementTransform);
                if (_forceConvertFlexiableToPreferred)
                {
                    const int LAYOUT_SIZE_DISABLE = -1;
                    /*
                        HACK 
                        IDK if this is cheap or not. 
                        If do this, remember to reset actual values when return to pool.
                        If it is expensive, then consider to inherit LayoutElement and create pubic get method.
                    */
                    if (Mathf.Approximately(1f, _layoutElement.flexibleHeight))
                    {
                        _layoutElement.preferredHeight = LAYOUT_SIZE_DISABLE;
                        layoutGroup.CalculateLayoutInputVertical();
                        layoutGroup.SetLayoutVertical();
                        rectSize.y = GetTotalPreferredSizeDelegate.Invoke(layoutGroup, (int)RectTransform.Axis.Vertical);
                        _layoutElement.preferredHeight = rectSize.y;
                    }
                    if (Mathf.Approximately(1f, _layoutElement.flexibleWidth))
                    {
                        _layoutElement.preferredWidth = LAYOUT_SIZE_DISABLE;
                        layoutGroup.CalculateLayoutInputHorizontal();
                        layoutGroup.SetLayoutHorizontal();
                        rectSize.x = GetTotalPreferredSizeDelegate.Invoke(layoutGroup, (int)RectTransform.Axis.Horizontal);
                        _layoutElement.preferredWidth = rectSize.x;
                    }
                }
            }

            float width, height;
            if (_directlyUseSizeFromLayoutElement)
            {
                width = 1f <= _layoutElement.flexibleWidth ? rectSize.x : _layoutElement.preferredWidth;
                height = 1f <= _layoutElement.flexibleHeight ? rectSize.y : _layoutElement.preferredHeight;
                ElementPreferredSize = new Vector2(width, height);
                return;
            }

            // TODO Maybe remove this
            if (null != _elementSizeSetter)
            {
                _elementSizeSetter.ForceSetSize();
            }
            width = 1f <= _layoutElement.flexibleWidth ? ElementTransform.rect.width : _layoutElement.preferredWidth;
            height = 1f <= _layoutElement.flexibleHeight ? ElementTransform.rect.height : _layoutElement.preferredHeight;
            ElementPreferredSize = new Vector2(width, height);
        }

#if UNITY_EDITOR

        private void Reset()
        {
            TryGetComponent<LayoutElement>(out _layoutElement);
        }

#endif
    }
}