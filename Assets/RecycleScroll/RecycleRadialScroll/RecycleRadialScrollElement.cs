using UnityEngine;

namespace RecycleScrollView
{
    [RequireComponent(typeof(CanvasGroup))]
    public class RecycleRadialScrollElement : MonoBehaviour
    {
        /// <summary> The element index in the scroll content </summary>
#if UNITY_EDITOR
        [SerializeField] // TODO This value should be NonSerialized but better to show it in inspector
#endif
        private int m_elementIndex = -1;
        /// <summary> The actual data index in the scroll content </summary>
#if UNITY_EDITOR
        [SerializeField] // TODO This value should be NonSerialized but better to show it in inspector
#endif
        private int m_dataIndex = -1;

        private Vector2 m_size;
        private CanvasGroup m_canvasGroup;
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

        public void SetSize(Vector2 size)
        {
            m_size = size;
            ElementTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, m_size.x);
            ElementTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, m_size.y);
        }

        public void SetIndex(int elementIndex, int dataIndex)
        {
            m_elementIndex = elementIndex;
            m_dataIndex = dataIndex;
        }

        public void ShowElement()
        {
            m_canvasGroup.alpha = 1f;
            m_canvasGroup.interactable =
            m_canvasGroup.blocksRaycasts = true;
        }

        public void HideElement()
        {
            m_canvasGroup.alpha = 0f;
            m_canvasGroup.interactable =
            m_canvasGroup.blocksRaycasts = false;
        }

        private void Awake()
        {
            if (!TryGetComponent<CanvasGroup>(out m_canvasGroup))
            {
                m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

#if UNITY_EDITOR

        private void Reset()
        {
            TryGetComponent<CanvasGroup>(out m_canvasGroup);
        }

#endif

    }
}