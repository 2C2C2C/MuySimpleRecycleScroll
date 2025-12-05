using System;
using System.Collections.Generic;
using UnityEngine;

namespace RecycleScrollView.Sample
{
    public class RecycleGridScrollSample : MonoBehaviour, IGridScrollDataSource
    {
        [SerializeField]
        private RecycleGridScroll _gridScroll;
        [SerializeField]
        private RectTransform _elementPrefab;

        [SerializeField]
        private List<GuidElementData> m_dataList = new List<GuidElementData>();

        [Header("Test params")]
        [SerializeField, Range(4, 800)]
        private int _startDataCount = 10;

        [SerializeField]
        private int _jumpToIndex = 55;

        [SerializeField]
        private int _insertIndex;
        [SerializeField]
        private int _insertOrAddCount;

        [SerializeField]
        private int _removeIndex;
        [SerializeField]
        private int _removeCount;

        private Dictionary<RectTransform, GuidElementUI> m_viewElementMap = new Dictionary<RectTransform, GuidElementUI>();

        public event Action<int> OnDataElementCountChanged;

        public int DataElementCount => m_dataList.Count;

        public void Setup(List<GuidElementData> dataList)
        {
            m_dataList.Clear();
            m_dataList.AddRange(dataList);
            _gridScroll.Uninit();
            _gridScroll.Init(this);
        }

        public RectTransform AddElement(RectTransform parent)
        {
            RectTransform element = RectTransform.Instantiate(_elementPrefab, parent);
            if (element.TryGetComponent<GuidElementUI>(out GuidElementUI viewElement))
            {
                m_viewElementMap.Add(element, viewElement);
            }
            element.gameObject.SetActive(true);
            return element;
        }

        public void RemoveElement(RectTransform element)
        {
            m_viewElementMap.Remove(element);
            GameObject.Destroy(element.gameObject);
            element = null;
        }

        public void InitElement(RectTransform element, int index)
        {
            if (m_viewElementMap.TryGetValue(element, out GuidElementUI viewElement))
            {
                viewElement.Setup(m_dataList[index]);
            }
        }

        public void UnInitElement(RectTransform element)
        {
            if (m_viewElementMap.TryGetValue(element, out GuidElementUI viewElement))
            {
                viewElement.Clear();
            }
        }

        public void ChangeElementIndex(RectTransform element, int prevIndex, int nextIndex)
        {
            if (m_viewElementMap.TryGetValue(element, out GuidElementUI viewElement))
            {
                viewElement.Setup(m_dataList[nextIndex]);
            }
        }

        [ContextMenu(nameof(DoInsertRange))]
        private void DoInsertRange()
        {
            if (0 >= _insertOrAddCount)
            {
                return;
            }
            else if (1 == _insertOrAddCount)
            {
                GuidElementData addData = new GuidElementData();
                m_dataList.Insert(_insertIndex, addData);
                _gridScroll.InsertElement(_insertIndex);
            }
            else
            {
                List<GuidElementData> tempToAdd = new List<GuidElementData>();
                for (int i = 0; i < _insertOrAddCount; i++)
                {
                    tempToAdd.Add(new GuidElementData());
                }
                m_dataList.InsertRange(_insertIndex, tempToAdd);
                _gridScroll.InsertRange(_insertIndex, _insertOrAddCount);
            }
        }

        [ContextMenu(nameof(DoRemoveRange))]
        private void DoRemoveRange()
        {
            m_dataList.RemoveRange(5, 10);
            _gridScroll.RemoveRange(5, 10);
        }

        private void Awake()
        {
            _elementPrefab.gameObject.SetActive(false);
        }

        private void Start()
        {
            List<GuidElementData> tempData = new List<GuidElementData>(_startDataCount);
            for (int i = 0; i < _startDataCount; i++)
            {
                tempData.Add(new GuidElementData());
            }
            Setup(tempData);
        }

    }
}