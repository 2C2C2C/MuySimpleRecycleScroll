using System.Collections.Generic;
using UnityEngine;
using UnityRandom = UnityEngine.Random;

namespace RecycleScrollView.Sample
{
    public class SingleDirectionScrollSample : MonoBehaviour, IRecycleScrollDataSource
    {
        [SerializeField]
        private RecycleSingleDirectionScroll _scrollController;
        [SerializeField]
        private RectTransform _elementPrefab;

        [Header("Element params")]
        [SerializeField]
        private float _sizeMin = 80;
        [SerializeField]
        private float _sizeMax = 320;
        [SerializeField]
        private int _startDataCount = 50;

        [Header("Test parameters")]
        [SerializeField]
        private int _jumpToTestIndex = 10;
        [SerializeField, Tooltip("-1 means add to tail")]
        private int _insertIndex = -1;
        [SerializeField, Range(0, 100)]
        private int _insertCount = 1;
        [SerializeField]
        private int _removeIndex = -1;
        [SerializeField, Range(0, 100)]
        private int _removeCount = 1;

        [SerializeField] // This should be show in inspector but non serialized
        private List<float> m_elementSizeList = new List<float>();

        public int DataElementCount => null == m_elementSizeList ? 0 : m_elementSizeList.Count;

        public RectTransform RequestElement(RectTransform parent)
        {
            RectTransform newElement = RectTransform.Instantiate(_elementPrefab, parent);
            return newElement;
        }

        public void ReturnElement(RectTransform element)
        {
            element.SetParent(null);
            GameObject.Destroy(element.gameObject);
        }

        public void InitElement(RectTransform element, int dataIndex)
        {
            if (element.TryGetComponent<TextElementUI>(out TextElementUI textElement))
            {
                float tempSize = m_elementSizeList[dataIndex];
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

        public void UnInitElement(RectTransform element)
        {
            if (element.TryGetComponent<TextElementUI>(out TextElementUI textElement))
            {
                if (_scrollController.IsHorizontal)
                {
                    textElement.SetWidth(0);
                }
                else if (_scrollController.IsVertical)
                {
                    textElement.SetHeight(0);
                }
                textElement.SetText($"size: 0");
            }
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
            _scrollController.Init(this);
        }

        [ContextMenu(nameof(JumpToData))]
        private void JumpToData()
        {
            _scrollController.JumpToElementInstant(_jumpToTestIndex);
        }

        [ContextMenu(nameof(InsertData))]
        private void InsertData()
        {
            if (0 == _insertCount)
            {
                return;
            }

            if (-1 >= _insertIndex) // Add to tail
            {
                if (1 == _insertCount)
                {
                    m_elementSizeList.Add(UnityRandom.Range(_sizeMin, _sizeMax));
                    _scrollController.AddElementTotail();
                }
                else
                {
                    for (int i = 0; i < _insertCount; i++)
                    {
                        m_elementSizeList.Add(UnityRandom.Range(_sizeMin, _sizeMax));
                    }
                    _scrollController.AddElementsToTail(_insertCount);
                }
            }
            else if (m_elementSizeList.Count - 1 >= _insertIndex) // Insert
            {
                List<float> toAdd = new List<float>(_insertCount);
                for (int i = 0; i < _insertCount; i++)
                {
                    toAdd.Add(UnityRandom.Range(_sizeMin, _sizeMax));
                }
                m_elementSizeList.InsertRange(_insertIndex, toAdd);
                _scrollController.InsertElements(_insertIndex, _insertCount);
            }
        }

        [ContextMenu(nameof(RemoveData))]
        private void RemoveData()
        {
            if (0 == _removeCount)
            {
                return;
            }
            if (_removeCount > DataElementCount || DataElementCount - 1 < _removeIndex + _removeCount - 1 || -1 == _removeIndex)
            {
                LogHelper.LogError($"Out of range");
                return;
            }
            
            m_elementSizeList.RemoveRange(_removeIndex, _removeCount);
            _scrollController.RemoveElements(_removeIndex, _removeCount);
        }

    }
}