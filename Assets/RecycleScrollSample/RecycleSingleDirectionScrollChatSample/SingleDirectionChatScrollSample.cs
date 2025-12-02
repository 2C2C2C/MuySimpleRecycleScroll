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

        private List<ChatData> m_chatList;

        public event Action<int> OnDataElementCountChanged;

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

        [ContextMenu("AddTopTest")]
        private void AddTopTest()
        {
            RectTransform scrollContent = _scrollrect.content;
            RectTransform scrollViewport = _scrollrect.viewport;
            RectTransform newElement = RectTransform.Instantiate(_elementPrefab, scrollContent);
            Debug.LogError($"1 {_scrollrect.verticalNormalizedPosition}");
            if (newElement.TryGetComponent<TextElementUI>(out TextElementUI chatTextElement))
            {
                float heightee = UnityRandom.Range(80, 560);
                chatTextElement.SetHeight(heightee);
                chatTextElement.SetText($"ee ");
                newElement.SetAsFirstSibling();

                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollContent);
                Vector2 size = newElement.rect.size;
                float delta = heightee;
                Vector2 contentSize = scrollContent.rect.size;
                if (_scrollController.IsHorizontal)
                {
                    contentSize.x += delta;
                    Debug.LogError($"2 {_scrollrect.verticalNormalizedPosition} {size}");
                    contentSize = scrollContent.rect.size;
                    // _content.anchoredPosition += Vector2.up * delta;
                    _scrollrect.verticalNormalizedPosition -= delta / (contentSize.x - scrollViewport.rect.height);
                }
                else if (_scrollController.IsVertical)
                {
                    contentSize.y += delta;
                    Debug.LogError($"2 {_scrollrect.verticalNormalizedPosition} {size}");
                    contentSize = scrollContent.rect.size;
                    // _content.anchoredPosition += Vector2.up * delta;
                    _scrollrect.verticalNormalizedPosition -= delta / (contentSize.y - scrollViewport.rect.height);
                }
            }
        }

        [ContextMenu("RemoveTest")]
        private void RemoveTest()
        {
            RectTransform scrollContent = _scrollrect.content;
            RectTransform tempChild = scrollContent.GetChild(0) as RectTransform;
            float heightDelta = tempChild.rect.height;
            Destroy(tempChild.gameObject);
            scrollContent.anchoredPosition -= Vector2.up * heightDelta;
        }

        [ContextMenu(nameof(JumpToTest))]
        private void JumpToTest()
        {
            _scrollController.JumpToElementInstant(_jumpToTestIndex);
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
    }
}