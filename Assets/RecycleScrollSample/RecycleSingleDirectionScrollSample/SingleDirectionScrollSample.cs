using System;
using System.Collections.Generic;
using UnityEngine;
using UnityRandom = UnityEngine.Random;

namespace RecycleScrollView.Sample
{
    public class SingleDirectionScrollSample : MonoBehaviour, ISingleDirectionScrollDataSource
    {
        [SerializeField]
        private RecycleSingleDirectionScroll _scrollController;
        [SerializeField]
        private RectTransform _elementPrefab;

        [SerializeField]
        private float _sizeMin = 80;
        [SerializeField]
        private float _sizeMax = 320;
        [SerializeField]
        private int _startDataCount = 50;

        [Header("Test parameters")]
        [SerializeField]
        private int _jumpToTestIndex = 10;
        [SerializeField]
        private int _addOrRemoveIndex = -1;

        private int m_currentDataCount = 0;

        private List<float> m_elementSizeList = new List<float>();
        public event Action<int, int> OnDataElementCountChanged;

        public int DataElementCount => null == m_elementSizeList ? 0 : m_elementSizeList.Count;

        public RectTransform RequestElement(RectTransform parent, int index)
        {
            if (DataElementCount <= index)
            {
                Debug.LogError($"RequestElement index {index} exceed data count {DataElementCount}");
                return null;
            }

            RectTransform newElement = RectTransform.Instantiate(_elementPrefab, parent);
            if (newElement.TryGetComponent<TextElementUI>(out TextElementUI textElement))
            {
                float tempSize = m_elementSizeList[index];
                if (_scrollController.IsHorizontal)
                {
                    textElement.SetWidth(tempSize);
                }
                else if (_scrollController.IsVertical)
                {
                    textElement.SetHeight(tempSize);
                }
                textElement.SetText($"size: {tempSize}");
            }
            return newElement;
        }

        public void ReturnElement(RectTransform element)
        {
            element.SetParent(null);
            GameObject.Destroy(element.gameObject);
        }

        public void ChangeElementIndex(RectTransform element, int prevIndex, int nextIndex)
        {
            if (element.TryGetComponent<TextElementUI>(out TextElementUI textElement))
            {
                int dataCount = m_elementSizeList.Count;
                if (0 > nextIndex || dataCount <= nextIndex)
                {
                    // 
                }
                else
                {
                    float tempSize = m_elementSizeList[nextIndex];
                    if (_scrollController.IsHorizontal)
                    {
                        textElement.SetWidth(tempSize);
                    }
                    else if (_scrollController.IsVertical)
                    {
                        textElement.SetHeight(tempSize);
                    }
                    textElement.SetText($"size: {tempSize}");
                }
            }
        }

        private void Start()
        {
            m_elementSizeList = new List<float>();
            for (int i = 0; i < _startDataCount; i++)
            {
                m_elementSizeList.Add(UnityRandom.Range(_sizeMin, _sizeMax));
            }
            m_currentDataCount = _startDataCount;
            _scrollController.Init(this);
        }

        [ContextMenu(nameof(JumpToTest))]
        private void JumpToTest()
        {
            _scrollController.JumpToElementInstant(_jumpToTestIndex);
        }

        [ContextMenu(nameof(AddTest))]
        private void AddTest()
        {
            int prevCount = DataElementCount;
            int addIndex = _addOrRemoveIndex;
            if (-1 != _addOrRemoveIndex && _addOrRemoveIndex <= DataElementCount - 1)
            {
                // Add to specific index
                m_elementSizeList.Insert(_addOrRemoveIndex, UnityRandom.Range(_sizeMin, _sizeMax));
            }
            else
            {
                // Add to tail
                addIndex = DataElementCount;
                m_elementSizeList.Add(UnityRandom.Range(_sizeMin, _sizeMax));
            }
            m_currentDataCount = m_elementSizeList.Count;
            _scrollController.InsertElement(addIndex);
            OnDataElementCountChanged?.Invoke(prevCount, DataElementCount);
        }

        [ContextMenu(nameof(RemoveTest))]
        private void RemoveTest()
        {
            int prevCount = DataElementCount;
            int removeIndex = _addOrRemoveIndex;
            if (-1 != _addOrRemoveIndex && _addOrRemoveIndex <= DataElementCount - 1)
            {
                // Remove from specific index
                m_elementSizeList.RemoveAt(removeIndex);
            }
            else
            {
                // Remove from tail
                removeIndex = DataElementCount - 1;
                m_elementSizeList.RemoveAt(removeIndex);
            }
            m_currentDataCount = m_elementSizeList.Count;
            OnDataElementCountChanged?.Invoke(prevCount, DataElementCount);
            _scrollController.RemoveElement(removeIndex);
        }

        [ContextMenu(nameof(RemoveRangeTest))]
        private void RemoveRangeTest()
        {
            int prevCount = DataElementCount;
            m_elementSizeList.RemoveRange(0, 6);
            OnDataElementCountChanged?.Invoke(prevCount, DataElementCount);
        }
    }
}