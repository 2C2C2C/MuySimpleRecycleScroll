using System.Collections.Generic;
using UnityEngine;

namespace RecycleScrollView.Sample
{
    public class RecycleGridScrollSample : MonoBehaviour
    {
        [Range(4, 800)]
        [SerializeField]
        private int _startDataCount = 10;
        [SerializeField]
        private GuidElementListUI _gridListUI = null;
        [SerializeField]
        private RecycleGridScroll _scrollRectController;

        [SerializeField]
        private int _jumpToIndex = 55;

        [SerializeField]
        private List<GuidElementData> m_dataList = null;
        [SerializeField]
        private List<string> m_dataNames = null;

        private void Start()
        {
            SetupData();
        }

        [ContextMenu("setup data")]
        private void SetupData()
        {
            m_dataList = new List<GuidElementData>(_startDataCount);
            m_dataNames = new List<string>(_startDataCount);
            for (int i = 0; i < _startDataCount; i++)
            {
                GuidElementData data = new GuidElementData();
                m_dataList.Add(data);
                m_dataNames.Add(data.ItemName);
            }

            _gridListUI.Setup(m_dataList);
        }

        [ContextMenu(nameof(JumpToTest))]
        private void JumpToTest()
        {
            _scrollRectController.JumpTo(_jumpToIndex);
        }
    }
}