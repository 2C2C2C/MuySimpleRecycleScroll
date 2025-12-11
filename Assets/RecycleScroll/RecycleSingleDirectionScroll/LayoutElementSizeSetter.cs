using UnityEngine;
using UnityEngine.UI;

namespace RecycleScrollView
{
    [RequireComponent(typeof(LayoutElement))]
    public class LayoutElementSizeSetter : MonoBehaviour
    {
        [SerializeField]
        private LayoutElement _layoutElement;
        [SerializeField]
        private Behaviour _sizeSource;

        [SerializeField]
        private bool _controlWidth = false;
        [SerializeField]
        private float _horizontalPadding = 0f;
        [SerializeField]
        private float _horizontalMinSize = -1f;
        [SerializeField]
        private float _horizontalMaxSize = -1f;

        [SerializeField]
        private bool _controlHeight = false;
        [SerializeField]
        private float _verticalPadding = 0f;
        [SerializeField]
        private float _verticalMinSize = -1f;
        [SerializeField]
        private float _verticalMaxSize = -1f;

        public float CurrentWidth => _layoutElement.preferredWidth;
        public float CurrentHeight => _layoutElement.preferredHeight;

        public void ForceSetSize()
        {
            if (_sizeSource is ILayoutElement sourceElement)
            {
                if (_controlHeight)
                {
                    float height = sourceElement.preferredHeight;
                    height += _verticalPadding;
                    float min = 0f > _verticalMinSize ? 0f : _verticalMinSize;
                    float max = 0f > _verticalMaxSize ? float.MaxValue : _verticalMaxSize;
                    height = Mathf.Clamp(height, min, max);
                    _layoutElement.preferredHeight = height;
                }
                if (_controlWidth)
                {
                    float width = sourceElement.preferredWidth;
                    width += _horizontalPadding;
                    float min = 0f > _horizontalMinSize ? 0f : _horizontalMinSize;
                    float max = 0f > _horizontalMaxSize ? float.MaxValue : _horizontalMaxSize;
                    width = Mathf.Clamp(width, min, max);
                    _layoutElement.preferredWidth = width;
                }
            }
            else
            {
                LogHelper.LogError($"SizeSource should be ILayoutElement", context: this);
            }
        }

#if UNITY_EDITOR

        private void Reset()
        {
            TryGetComponent<LayoutElement>(out _layoutElement);
        }

#endif

    }
}