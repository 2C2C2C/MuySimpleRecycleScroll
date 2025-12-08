using UnityEngine;

namespace RecycleScrollView
{
    public interface IRecycleElementSource
    {
        RectTransform RequestElement(RectTransform parent);
        void ReturnElement(RectTransform element);
    }
}