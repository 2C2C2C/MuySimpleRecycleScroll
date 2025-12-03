using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extend;
using UnityRandom = UnityEngine.Random;

namespace RecycleScrollView.Sample
{
    public struct ChatData
    {
        public string mainContent;
        public string quoteContent;
        public static ChatData CreateRandomOne()
        {
            ChatData data = default;

            string main = "";
            int wordCount = UnityRandom.Range(1, 32);
            for (int i = 0; i < wordCount; i++)
            {
                main += "Test ";
            }
            data.mainContent = main;

            wordCount = UnityRandom.Range(0, 2);
            if (0 < wordCount)
            {
                string quote = "";
                wordCount = UnityRandom.Range(1, 8);
                for (int i = 0; i < wordCount; i++)
                {
                    quote += "Test ";
                }
                data.quoteContent = quote;
            }

            return data;
        }
    }

    public class SingleDirectionChatScrollSample : MonoBehaviour, ISingleDirectionScrollDataSource
    {
        [SerializeField]
        private RecycleSingleDirectionScroll _scrollController;
        [SerializeField]
        private RectTransform _elementPrefab;
        [SerializeField]
        private ScrollRect _scrollrect;
        [SerializeField]
        private int _dataCount = 50;

        [SerializeField]
        private int _jumpToTestIndex = 10;
        [SerializeField]
        private int _addDataIndex = -1;
        [SerializeField]
        private int _removeDataIndex = -1;

        private List<ChatData> m_chatList;

        public event Action<int, int> OnDataElementCountChanged;

        public int DataElementCount => _dataCount;

        public RectTransform RequestElement(RectTransform parent, int index)
        {
            RectTransform newElement = RectTransform.Instantiate(_elementPrefab, parent);
            if (newElement.TryGetComponent<ChatElementUI>(out ChatElementUI chatTextElement))
            {
                ChatData data = m_chatList[index];
                chatTextElement.SetText(data.mainContent, data.quoteContent);
                newElement.ForceUpdateRectTransforms();
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
            int dataCount = DataElementCount;
            if (element.TryGetComponent<ChatElementUI>(out ChatElementUI chatElementUI) &&
                -1 < nextIndex &&
                dataCount > nextIndex)
            {
                ChatData data = m_chatList[nextIndex];
                chatElementUI.SetText(data.mainContent, data.quoteContent);
                element.ForceUpdateRectTransforms();
            }
        }

        private void Awake()
        {
            m_chatList = new List<ChatData>();
            for (int i = 0; i < _dataCount; i++)
            {
                m_chatList.Add(ChatData.CreateRandomOne());
            }
        }

        private void Start()
        {
            _scrollController.Init(this);
        }

        [ContextMenu(nameof(JumpToTest))]
        private void JumpToTest()
        {
            _scrollController.JumpToElementInstant(_jumpToTestIndex);
        }

        [ContextMenu(nameof(AddDataTest))]
        private void AddDataTest()
        {

        }

        [ContextMenu(nameof(RemoveDataTest))]
        private void RemoveDataTest()
        {

        }

    }

}