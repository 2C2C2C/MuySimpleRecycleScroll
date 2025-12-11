using UnityEngine;

namespace RecycleScrollView.Sample
{
    public class RecycleRadialScrollSample : MonoBehaviour, IRecycleScrollDataSource
    {
        [SerializeField]
        private RecycleRadialScroll _scroll;
        [SerializeField]
        private RectTransform _elementTemplate;

        [SerializeField]
        private int _startDataCount = 64;

        [Header("Test parameters")]
        [SerializeField]
        private int _jumpToDataIndex = 10;
        [SerializeField, Tooltip("-1 means add to tail")]
        private int _insertIndex = -1;
        [SerializeField, Range(0, 100)]
        private int _insertCount = 1;
        [SerializeField]
        private int _removeIndex = -1;
        [SerializeField, Range(0, 100)]
        private int _removeCount = 1;

        private int m_currentDataCount = 0;

        public int DataElementCount => m_currentDataCount;

        public RectTransform RequestElement(RectTransform parent)
        {
            RectTransform spawned = RectTransform.Instantiate(_elementTemplate, parent);
            spawned.gameObject.SetActive(true);
            return spawned;
        }

        public void ReturnElement(RectTransform elementTransform)
        {
            GameObject.Destroy(elementTransform.gameObject);
        }

        public void ChangeElementIndex(RectTransform elementTransform, int prevIndex, int nextIndex)
        {
            if (elementTransform.TryGetComponent<RecycleRadialScrollElementSample>(out RecycleRadialScrollElementSample element))
            {
                element.SetText($"Data\n{nextIndex}");
            }
        }

        public void InitElement(RectTransform elementTransform, int index)
        {
            if (elementTransform.TryGetComponent<RecycleRadialScrollElementSample>(out RecycleRadialScrollElementSample element))
            {
                element.SetText($"Data\n{index}");
            }
        }

        public void UnInitElement(RectTransform elementTransform)
        {
            if (elementTransform.TryGetComponent<RecycleRadialScrollElementSample>(out RecycleRadialScrollElementSample element))
            {
                element.SetText($"Data\n-1");
            }
        }

        private void Start()
        {
            m_currentDataCount = _startDataCount;
            _scroll.Init(this);
        }

        [ContextMenu(nameof(JumpToData))]
        private void JumpToData()
        {
            _scroll.JumpToByDataIndex(_jumpToDataIndex);
        }

        [ContextMenu(nameof(InsertData))]
        private void InsertData()
        {
            if (0 >= _insertCount)
            {
                return;
            }

            m_currentDataCount += _insertCount;
            _scroll.InsertElements(_insertIndex, _insertCount);
        }

        [ContextMenu(nameof(RemoveData))]
        private void RemoveData()
        {
            if (0 >= _removeCount)
            {
                return;
            }
            if (_removeCount > DataElementCount || DataElementCount - 1 < _removeIndex + _removeCount - 1 || -1 == _removeIndex)
            {
                LogHelper.LogError($"Out of range");
                return;
            }
            m_currentDataCount += _removeCount;
            _scroll.RemoveElements(_removeIndex, _removeCount);
        }


#if UNITY_EDITOR

        private void Reset()
        {
            TryGetComponent<RecycleRadialScroll>(out _scroll);
        }

#endif

    }
}