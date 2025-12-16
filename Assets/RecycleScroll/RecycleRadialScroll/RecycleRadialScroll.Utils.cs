using UnityEngine;
using UnityEngine.UI.Extend;

namespace RecycleScrollView
{
    public partial class RecycleRadialScroll
    {
        private const int INVALID_INDEX = -1;

        public int GetCurrentShowingElementIndexTailBound()
        {
            int result = INVALID_INDEX;
            int validMaxIndex = m_dataSource.DataElementCount - 1;
            for (int i = 0, length = m_currentUsingElements.Count; i < length; i++)
            {
                RecycleRadialScrollElement element = m_currentUsingElements[i];
                int elementIndex = element.ElementIndex;
                if (INVALID_INDEX == result)
                {
                    if (element.IsElementShowing && INVALID_INDEX != elementIndex && elementIndex <= validMaxIndex)
                    {
                        result = elementIndex;
                    }
                }
                else
                {
                    if (element.IsElementShowing && INVALID_INDEX != elementIndex && elementIndex <= validMaxIndex)
                    {
                        result = elementIndex;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            return result;
        }

        public int GetCurrentShowingElementIndexHeadBound()
        {
            int validMaxIndex = m_dataSource.DataElementCount - 1;
            for (int i = 0, length = m_currentUsingElements.Count; i < length; i++)
            {
                RecycleRadialScrollElement element = m_currentUsingElements[i];
                int elementIndex = element.ElementIndex;
                if (element.IsElementShowing && INVALID_INDEX != elementIndex && elementIndex <= validMaxIndex)
                {
                    return elementIndex;
                }
            }
            return INVALID_INDEX;
        }

        private bool IsElementRectInterestedWithViewport(Vector3 elementWorldPos, Vector2 elementSize)
        {
            RectTransform viewport = _scrollRect.viewport;
            Matrix4x4 viewportWorldToLocal = viewport.worldToLocalMatrix;
            Vector2 elementCenterInViewport = viewportWorldToLocal.MultiplyPoint(elementWorldPos);
            Vector2 elementCenterRectPositionInViewport = RectTransformEx.TransformLocalPositionToRectPosition(viewport, elementCenterInViewport);
            Vector2 viewportSize = viewport.rect.size;

            Vector2 halfSize = 0.5f * elementSize;
            Vector3 topLeft = elementCenterRectPositionInViewport + new Vector2(-halfSize.x, halfSize.y);
            Vector3 topRight = elementCenterRectPositionInViewport + new Vector2(halfSize.x, halfSize.y);
            Vector3 bottomLeft = elementCenterRectPositionInViewport + new Vector2(-halfSize.x, -halfSize.y);
            Vector3 bottomRight = elementCenterRectPositionInViewport + new Vector2(halfSize.x, -halfSize.y);

            bool result = false;
            result = result || (0f <= topLeft.x && viewportSize.x >= topLeft.x && 0f <= topLeft.y && viewportSize.y >= topLeft.y);
            result = result || (0f <= topRight.x && viewportSize.x >= topRight.x && 0f <= topRight.y && viewportSize.y >= topRight.y);
            result = result || (0f <= bottomLeft.x && viewportSize.x >= topLeft.x && 0f <= bottomLeft.y && viewportSize.y >= bottomLeft.y);
            result = result || (0f <= bottomRight.x && viewportSize.x >= bottomRight.x && 0f <= bottomRight.y && viewportSize.y >= bottomRight.y);

            return result;
        }

        private bool WillElementShow(int elementIndex)
        {
            for (int i = 0, length = m_positionList.Count; i < length; i++)
            {
                ElementPositionData positionData = m_positionList[i];
                if (elementIndex == positionData.elmentIndex)
                {
                    return positionData.canShow;
                }
            }
            return false;
        }

        private int ElementIndexDataIndex2WayConvert(int index)
        {
            if (null == m_dataSource || INVALID_INDEX == index)
            {
                return INVALID_INDEX;
            }
            return ElementIndexDataIndex2WayConvert(index, m_dataSource.DataElementCount);
        }

        // To solve reverse arrangement issues
        private int ElementIndexDataIndex2WayConvert(int index, int dataCount)
        {
            if (null == m_dataSource || INVALID_INDEX == index)
            {
                return INVALID_INDEX;
            }

            int result = _reverseArrangement ?
                dataCount - index - 1 :
                index;
            return result;
        }

        private void ChangeObjectName_EditorOnly(MonoBehaviour behaviour, int elementIndex)
        {
            behaviour.name = $"Element {elementIndex}; DataIndex {ElementIndexDataIndex2WayConvert(elementIndex)}";
        }

    }
}