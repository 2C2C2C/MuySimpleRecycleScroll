using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public partial class ScrollrectNavigator : MonoBehaviour
{
    [SerializeField]
    private ScrollRect m_scrollRect;

    private bool m_hasStarted;

    private void Awake()
    {
        m_hasStarted = false;
    }

    private void OnEnable()
    {
        if (m_hasStarted)
        {

        }
        //m_scrollRect.horizontalScrollbarSpacing
    }

    private void OnDisable()
    {
        
    }

    private void Start()
    {
        m_hasStarted = true;
    }

    private void ScrollToPosition(Vector2 normalizedPosition, float duration)
    {
        if (duration > 0f)
        {
            // need tween move implementation here :)
            throw new NotImplementedException();
        }
        else
        {
            m_scrollRect.normalizedPosition = normalizedPosition;
        }
    }

    /// <param name="normalizedOffset">Normalized postion in viewport;1 top; 0 bottom</param>
    private int GetNearestVerticalLayoutElementIndex(float normalizedOffset)
    {
        RectTransform scrollrectContent = m_scrollRect.content;
        float spacing = 0f, top = 0f;
        if (scrollrectContent.TryGetComponent<VerticalLayoutGroup>(out VerticalLayoutGroup layoutGroup))
        {
            spacing = layoutGroup.spacing;
            top = layoutGroup.padding.top;
        }

        int contentChildCount = scrollrectContent.childCount;
        Vector2 contentMovePosition = scrollrectContent.localPosition;
        contentMovePosition.y -= top - (1f - normalizedOffset) * m_scrollRect.viewport.rect.height;
        int i = 0, resultIndex = -1;
        float deltaAbs = float.MaxValue;

        // HACK @Hiko 先检查第一个有 top padding的
        RectTransform child = scrollrectContent.GetChild(i) as RectTransform; // may cache a RectTransform[] be a better choice?
        Vector2 elementSize = child.rect.size;
        elementSize.y += spacing;
        float tempDelta = contentMovePosition.y - elementSize.y * 0.5f; // HACK @Hiko 要和元素的中心算距离
        contentMovePosition.y -= elementSize.y;
        Debug.LogError($"index {i}; tempDelta {tempDelta}");
        if (0 > tempDelta)
        {
            tempDelta = Mathf.Abs(tempDelta);
        }
        if (deltaAbs > tempDelta)
        {
            deltaAbs = tempDelta;
            resultIndex = i;
        }

        for (i = 1; i < contentChildCount; i++)
        {
            child = scrollrectContent.GetChild(i) as RectTransform; // may cache a RectTransform[] be a better choice?
            elementSize = child.rect.size;
            elementSize.y += spacing;

            tempDelta = contentMovePosition.y - elementSize.y * 0.5f; // HACK @Hiko 要和元素的中心算距离
            contentMovePosition.y -= elementSize.y;
            Debug.LogError($"index {i}; tempDelta {tempDelta}");
            if (0 > tempDelta)
            {
                tempDelta = Mathf.Abs(tempDelta);
            }

            if (deltaAbs > tempDelta)
            {
                deltaAbs = tempDelta;
                resultIndex = i;
            }
            else
            {
                break;
            }
        }

        Debug.LogError($"index {resultIndex}; normalized pos {m_scrollRect.normalizedPosition};");
        return resultIndex;
    }

}
