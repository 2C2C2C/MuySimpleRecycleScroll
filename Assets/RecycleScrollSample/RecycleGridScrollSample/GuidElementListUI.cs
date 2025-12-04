using System;
using System.Collections.Generic;
using UnityEngine;

namespace RecycleScrollView.Sample
{
    public class GuidElementListUI : MonoBehaviour, IGridScrollDataSource
    {
        [SerializeField]
        private RecycleGridScroll _gridScroll;
        [SerializeField]
        private RectTransform _elementPrefab;

        private List<GuidElementData> m_dataList = new List<GuidElementData>();
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


        [ContextMenu(nameof(AddDataTest))]
        private void AddDataTest()
        {
            GuidElementData addData = new GuidElementData();
            m_dataList.Insert(5, addData);
            _gridScroll.InsertElement(5);
        }

        [ContextMenu(nameof(RemoveRangeTest))]
        private void RemoveRangeTest()
        {
            m_dataList.RemoveRange(5, 10);
            _gridScroll.RemoveRange(5, 10);
        }

        private void Awake()
        {
            _elementPrefab.gameObject.SetActive(false);
        }

    }
}