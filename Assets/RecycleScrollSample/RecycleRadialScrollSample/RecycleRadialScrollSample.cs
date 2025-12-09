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

        public int DataElementCount => _startDataCount;

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
            _scroll.Init(this);
        }

        [ContextMenu(nameof(DoJumpToTest))]
        private void DoJumpToTest()
        {
            _scroll.JumpToByDataIndex(_jumpToDataIndex);
        }

        [ContextMenu(nameof(DoInsertTest))]
        private void DoInsertTest()
        {
            if (0 == _insertCount)
            {
                return;
            }

            // if (-1 >= _insertIndex) // Add to tail
            // {
            //     if (1 == _insertCount)
            //     {
            //         m_elementSizeList.Add(UnityRandom.Range(_sizeMin, _sizeMax));
            //         _scrollController.AddElementTotail();
            //     }
            //     else
            //     {
            //         for (int i = 0; i < _insertCount; i++)
            //         {
            //             m_elementSizeList.Add(UnityRandom.Range(_sizeMin, _sizeMax));
            //         }
            //         _scrollController.AddElementsToTail(_insertCount);
            //     }
            // }
            // else if (m_elementSizeList.Count - 1 >= _insertIndex) // Insert
            // {
            //     List<float> toAdd = new List<float>(_insertCount);
            //     for (int i = 0; i < _insertCount; i++)
            //     {
            //         toAdd.Add(UnityRandom.Range(_sizeMin, _sizeMax));
            //     }
            //     m_elementSizeList.InsertRange(_insertIndex, toAdd);
            //     _scrollController.InsertElements(_insertIndex, _insertCount);
            // }
        }

        [ContextMenu(nameof(DoRemoveTest))]
        private void DoRemoveTest()
        {
            if (0 == _removeCount)
            {
                return;
            }
            if (_removeCount > DataElementCount || DataElementCount - 1 < _removeIndex + _removeCount - 1 || -1 == _removeIndex)
            {
                Debug.LogError($"Out of range");
                return;
            }

            // m_elementSizeList.RemoveRange(_removeIndex, _removeCount);
            // _scrollController.RemoveElements(_removeIndex, _removeCount);
        }


#if UNITY_EDITOR

        private void Reset()
        {
            TryGetComponent<RecycleRadialScroll>(out _scroll);
        }

#endif

    }
}