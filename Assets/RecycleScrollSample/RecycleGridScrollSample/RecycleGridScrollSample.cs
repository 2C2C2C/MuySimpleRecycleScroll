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
        private GuidElementData[] m_dataArr = null;
        [SerializeField]
        private string[] m_dataNames = null;

        private void Start()
        {
            SetupData();
        }

        [ContextMenu("setup data")]
        private void SetupData()
        {
            m_dataArr = new GuidElementData[_startDataCount];
            m_dataNames = new string[_startDataCount];
            for (int i = 0; i < _startDataCount; i++)
            {
                m_dataArr[i] = new GuidElementData();
                m_dataNames[i] = m_dataArr[i].ItemName;
            }

            _gridListUI.Setup(new List<GuidElementData>(m_dataArr));
        }

        [ContextMenu(nameof(JumpToTest))]
        private void JumpToTest()
        {
            _scrollRectController.JumpTo(_jumpToIndex);
        }
    }
}