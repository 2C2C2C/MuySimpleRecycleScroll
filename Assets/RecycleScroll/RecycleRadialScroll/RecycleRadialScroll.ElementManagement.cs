using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RecycleScrollView
{
    public partial class RecycleRadialScroll
    {
        private List<RecycleRadialScrollElement> m_usingElements;

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
                GameObject obj = new GameObject();
                RectTransform addedTransform = obj.AddComponent<RectTransform>();
                addedTransform.SetParent(container);
                m_usingElements.Add(obj.AddComponent<RecycleRadialScrollElement>());
                ++currentCount;
            }
            while (currentCount > expectedCount)
            {
                RecycleRadialScrollElement element = m_usingElements[--currentCount];
                m_usingElements.RemoveAt(currentCount);
                GameObject.Destroy(element.gameObject);
            }
        }

    }
}