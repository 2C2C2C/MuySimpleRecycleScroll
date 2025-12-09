using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;

namespace RecycleScrollView
{
    public partial class RecycleRadialScroll
    {
        internal struct ElementPositionData
        {
            public int elmentIndex;
            public bool canShow;
            public Vector3 worldPosition;
            public ElementPositionData(int i, bool b, Vector3 pos)
            {
                elmentIndex = i;
                canShow = b;
                worldPosition = pos;
            }
        }

        [Header("Layout params")]
        [SerializeField]
        private float _radius;
        [SerializeField, Range(0f, 360f)]
        private float _startAngle;
        [SerializeField, Range(0f, 360f)]
        private float _internvalAngle;
        [SerializeField]
        private bool _antiClockwise;
        [SerializeField]
        private bool _reverseArrangment = false;

        private List<ElementPositionData> m_positionList;
        private List<RecycleRadialScrollElement> m_usingElements;

        private void ApplyLayoutSetting()
        {
            RectTransform content = _scrollRect.content;
            _scrollRect.vertical = IsVertical;
            _scrollRect.horizontal = IsHorizontal;
            switch (_dragContentScrollDirection)
            {
                case ScrollDirection.Vertical_UpToDown:
                    content.pivot = new Vector2(0.5f, 1f);
                    break;
                case ScrollDirection.Vertical_DownToUp:
                    content.pivot = new Vector2(0.5f, 0f);
                    break;
                case ScrollDirection.Horizontal_LeftToRight:
                    content.pivot = new Vector2(0f, 0.5f);
                    break;
                case ScrollDirection.Horizontal_RightToLeft:
                    content.pivot = new Vector2(1f, 0.5f);
                    break;
            }
        }

        private void ApplyScrollProcess()
        {
            CalculatePositionData();
            ApplyPositionToElements();
        }

        private void CalculatePositionData()
        {
            if (null == m_positionList)
            {
                m_positionList = new List<ElementPositionData>();
            }
            int elementCount = Mathf.FloorToInt(360 / _internvalAngle);
            while (m_positionList.Count > elementCount)
            {
                m_positionList.RemoveAt(m_positionList.Count - 1);
            }
            while (m_positionList.Count < elementCount)
            {
                m_positionList.Add(default);
            }

            int dataCount = m_dataSource.DataElementCount;
            float moveAngle = m_totalRotateAngle * m_normalizedProgress;
            float temp = moveAngle % 360f;

            Matrix4x4 containerLocalToWorld = _elementContainer.localToWorldMatrix;
            float radius = Mathf.Abs(_radius);
            float angle = _startAngle - temp;
            int prevValiid0RoundElementIndex = -1; // HACK only for 0 round case
            for (int i = 0; i < elementCount; i++)
            {
                Vector3 positionInsideContainer = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad), 0);
                positionInsideContainer *= radius;
                Vector3 elementWorldPosition = containerLocalToWorld.MultiplyPoint(positionInsideContainer);
                float testAngle = moveAngle - i * _internvalAngle;
                int round = 0;
                if (180f < testAngle) // HACK
                {
                    int testHalfRoundCount = Mathf.FloorToInt(testAngle / 180f);
                    round = Mathf.CeilToInt(testHalfRoundCount / 2f);
                }
                int elementIndex = i + round * elementCount;

                // Check if element will show
                bool isValid = dataCount - 1 >= elementIndex;
                if (0 == round) // HACK only for 0 round case
                {
                    bool currentInterestedWithViewport = IsElementRectInterestedWithViewport(elementWorldPosition, _previewElementSize);
                    if (-1 == prevValiid0RoundElementIndex)
                    {
                        if (currentInterestedWithViewport)
                        {
                            prevValiid0RoundElementIndex = i;
                        }
                    }
                    else
                    {
                        if (prevValiid0RoundElementIndex + 1 == i)
                        {
                            if (currentInterestedWithViewport)
                            {
                                prevValiid0RoundElementIndex = i;
                            }
                            else
                            {
                                isValid = false;
                            }
                        }
                        else
                        {
                            isValid = false;
                        }
                    }
                }

                m_positionList[i] = new ElementPositionData(elementIndex, isValid, elementWorldPosition);
                angle += _antiClockwise ? _internvalAngle : -_internvalAngle;
            }
        }

        private void AdjustCachedElements()
        {
            if (null == m_usingElements)
            {
                m_usingElements = new List<RecycleRadialScrollElement>();
            }

            RectTransform container = (null == _overrideRadialCenter) ? (RectTransform)transform : _overrideRadialCenter;
            int expectedCount = Mathf.FloorToInt(360 / _internvalAngle);
            int currentCount = m_usingElements.Count;
            while (currentCount < expectedCount)
            {
                RectTransform addedTransform = m_dataSource.RequestElement(container);
                if (addedTransform.TryGetComponent<RecycleRadialScrollElement>(out RecycleRadialScrollElement addedElement))
                {
                    m_usingElements.Add(addedElement);
                }
                ++currentCount;
            }
            while (currentCount > expectedCount)
            {
                RecycleRadialScrollElement element = m_usingElements[--currentCount];
                m_usingElements.RemoveAt(currentCount);
                m_dataSource.ReturnElement(element.ElementTransform);
            }
        }

        private void ApplyPositionToElements()
        {
            m_usingElements.Sort((x, y) =>
            {
                bool xValid = WillElementShow(x.ElementIndex);
                bool yValid = WillElementShow(y.ElementIndex);

                if (xValid && yValid)
                {
                    return 0;
                }
                else if (xValid)
                {
                    return 1;
                }
                else if (yValid)
                {
                    return -1;
                }

                return x.ElementIndex.CompareTo(y.ElementIndex);
            });

            for (int i = 0, length = m_positionList.Count; i < length; i++)
            {
                ElementPositionData positionData = m_positionList[i];
                RecycleRadialScrollElement element = m_usingElements[i];
                element.ElementTransform.position = positionData.worldPosition;
                if (element.ElementIndex != positionData.elmentIndex)
                {
                    ChangeElementIndex(element, positionData.elmentIndex);
                }
                if (positionData.canShow)
                {
                    element.ShowElement();
                }
                else
                {
                    element.HideElement();
                }
            }
        }

        private void ChangeElementIndex(RecycleRadialScrollElement element, int index)
        {
            m_dataSource.ChangeElementIndex(element.ElementTransform, element.ElementIndex, index);

#if UNITY_EDITOR

            ChangeObjectName_EditorOnly(element, index);

#endif
        }

    }
}