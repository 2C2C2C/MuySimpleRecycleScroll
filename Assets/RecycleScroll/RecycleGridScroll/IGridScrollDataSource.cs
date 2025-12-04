using UnityEngine;

namespace RecycleScrollView
{
    // Maybe we should separate data source and element source
    public interface IGridScrollDataSource : IRecycleElementSource
    {
        int DataElementCount { get; }

        /// <summary>
        /// Let the data source init the element when it is added to the list, index may be -1, if the element is not used yet
        /// </summary>
        /// <param name="element"></param>
        /// <param name="index">May be -1, if the element is not used yet</param>
        void InitElement(RectTransform element, int index);
        void UnInitElement(RectTransform element);
        /// <summary> Just change the view (or uninit then init again) </summary>
        void ChangeElementIndex(RectTransform element, int prevIndex, int nextIndex);
    }
}