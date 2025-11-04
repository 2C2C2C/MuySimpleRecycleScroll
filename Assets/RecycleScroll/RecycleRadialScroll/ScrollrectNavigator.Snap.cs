using System;
using UnityEngine;
using UnityEngine.UI;

public partial class ScrollrectNavigator
{
    [Serializable]
    public enum SnapPositionType
    {
        Center = 0,
        Above = 1,
        Below = 2,
    }

    [Serializable]
    public struct SnapOption
    {
        public SnapPositionType snapPosition;
        public float snapOffset;
        public float snapDuration;
        public float normalizedSnapReferencePosition;
    }

    [SerializeField]
    private SnapOption _snapOption;

    public Vector2 prevNormalizedPosition { get; private set; }

    public void SnapToElementForVerticalLayout(RectTransform item, SnapOption snapOption, bool forceRebuildLayout = false)
    {
        RectTransform scrollrectContent = m_scrollRect.content;
        float normalizedSnapPosition = snapOption.normalizedSnapReferencePosition;
        if (item.parent != scrollrectContent) // can not navi to a item dat is not a child of scroll content
        {
            return;
        }
        if (forceRebuildLayout)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollrectContent);
        }

        RectTransform scrollrectViewport = m_scrollRect.viewport;
        // TODO @Hiko redo this calculation is just too bad :(
        float spacing = 0f, top = 0f;
        if (scrollrectContent.TryGetComponent<VerticalLayoutGroup>(out VerticalLayoutGroup layoutGroup))
        {
            spacing = layoutGroup.spacing;
            top = layoutGroup.padding.top;
        }
        Vector2 contentMovePosition = Vector2.zero;
        contentMovePosition.y -= top - (1f - normalizedSnapPosition) * scrollrectViewport.rect.height;

        int targetIndex = item.GetSiblingIndex();
        RectTransform child;
        Vector2 elementSize;
        if (0 < targetIndex)
        {
            child = scrollrectContent.GetChild(0) as RectTransform; // may cache a RectTransform[] be a better choice?
            contentMovePosition.y -= child.rect.height + spacing;
            int i;
            for (i = 1; i < targetIndex; i++)
            {
                child = scrollrectContent.GetChild(i) as RectTransform; // may cache a RectTransform[] be a better choice?
                elementSize = child.rect.size;
                elementSize.y += spacing;
                contentMovePosition.y -= elementSize.y;
            }

            child = scrollrectContent.GetChild(i) as RectTransform; // may cache a RectTransform[] be a better choice?
            elementSize = child.rect.size;
            switch (_snapOption.snapPosition)
            {
                case SnapPositionType.Center:
                    contentMovePosition.y -= elementSize.y * 0.5f;
                    break;
                case SnapPositionType.Above:
                    contentMovePosition.y -= elementSize.y;
                    break;
                case SnapPositionType.Below:
                    contentMovePosition.y += elementSize.y;
                    break;
                default:
                    break;
            }
        }
        else
        {
            child = scrollrectContent.GetChild(0) as RectTransform; // may cache a RectTransform[] be a better choice?
            elementSize = child.rect.size;
            switch (_snapOption.snapPosition)
            {
                case SnapPositionType.Center:
                    contentMovePosition.y -= elementSize.y * 0.5f;
                    break;
                case SnapPositionType.Above:
                    contentMovePosition.y -= elementSize.y;
                    break;
                case SnapPositionType.Below:
                    contentMovePosition.y += elementSize.y;
                    break;
                default:
                    break;
            }
        }

        Vector2 normalizedResult = m_scrollRect.normalizedPosition;
        normalizedResult.y = 1f - (Mathf.Abs(contentMovePosition.y - _snapOption.snapOffset) / (scrollrectContent.rect.height - scrollrectViewport.rect.height));
        Debug.LogError($"calculated result {normalizedResult}");
        ScrollToPosition(normalizedResult, snapOption.snapDuration);
    }

#if UNITY_EDITOR

    [ContextMenu(nameof(SnapTest_EditorOnly))]
    private void SnapTest_EditorOnly()
    {
        //GetNearestVerticalLayoutElementIndex(_snapOption.normalizedSnapReferencePosition);
    }

#endif
}
