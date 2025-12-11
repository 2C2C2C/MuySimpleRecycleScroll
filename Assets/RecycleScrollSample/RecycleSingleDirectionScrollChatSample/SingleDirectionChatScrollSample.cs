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

    public class SingleDirectionChatScrollSample : MonoBehaviour, IRecycleScrollDataSource
    {
        [SerializeField]
        private RecycleSingleDirectionScroll _scrollController;
        [SerializeField]
        private RectTransform _elementPrefab;
        [SerializeField]
        private ScrollRect _scrollrect;
        [SerializeField]
        private int _startDataCount = 50;

        [Header("Test Params")]
        [SerializeField, Min(0)]
        private int _jumpToTestIndex = 10;

        [SerializeField, Min(-1)]
        private int _insertDataIndex = -1;
        [SerializeField, Min(0)]
        private int _insertDataCount = -1;

        [SerializeField, Min(-1)]
        private int _removeDataIndex = -1;
        [SerializeField, Min(0)]
        private int _removeDataCount = -1;

        private List<ChatData> m_chatList;

        public int DataElementCount => (null == m_chatList) ? 0 : m_chatList.Count;

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
            if (element.TryGetComponent<ChatElementUI>(out ChatElementUI chatTextElement))
            {
                ChatData data = m_chatList[dataIndex];
                chatTextElement.SetText(data.mainContent, data.quoteContent);
                element.ForceUpdateRectTransforms();
            }
        }

        public void UnInitElement(RectTransform element)
        {
            if (element.TryGetComponent<ChatElementUI>(out ChatElementUI chatTextElement))
            {
                chatTextElement.SetText(string.Empty, string.Empty);
                element.ForceUpdateRectTransforms();
            }
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
            for (int i = 0; i < _startDataCount; i++)
            {
                m_chatList.Add(ChatData.CreateRandomOne());
            }
        }

        private void Start()
        {
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
            if (0 >= _insertDataCount)
            {
                return;
            }

            List<ChatData> toAdd = new List<ChatData>();
            for (int i = 0; i < _insertDataCount; i++)
            {
                toAdd.Add(ChatData.CreateRandomOne());
            }
            int insertIndex = Mathf.Clamp(_insertDataIndex, 0, m_chatList.Count);
            m_chatList.InsertRange(insertIndex, toAdd);
            _scrollController.InsertElements(insertIndex, toAdd.Count);
        }

        [ContextMenu(nameof(RemoveData))]
        private void RemoveData()
        {
            if (0 >= _removeDataCount)
            {
                return;
            }

            int removeIndex = Mathf.Clamp(_removeDataIndex, 0, m_chatList.Count - 1);
            int removeCount = Mathf.Min(_removeDataCount, m_chatList.Count - removeIndex);
            m_chatList.RemoveRange(removeIndex, removeCount);
            _scrollController.RemoveElements(removeIndex, removeCount);
        }

    }

}