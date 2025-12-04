using UnityEngine;

namespace RecycleScrollView
{
    public interface IRecycleElementSource
    {
        RectTransform AddElement(RectTransform parent);
        void RemoveElement(RectTransform element);
    }
}