using UnityEngine;

namespace RecycleScrollView
{
    // Maybe we should separate data source and element source
    public interface IRecycleScrollDataSource : IRecycleElementSource
    {
        int DataElementCount { get; }

        /// <summary>
        /// Let the data source init the element when it is added to the list, index may be -1, if the element is not used yet
        /// </summary>
        /// <param name="elementTransform"></param>
        /// <param name="dataIndex"> Can be -1, if the element is not used yet</param>
        void InitElement(RectTransform elementTransform, int dataIndex);
        void UnInitElement(RectTransform elementTransform);

        /// <summary> Used to (force)update view (basically uninit then init again should be enough) </summary>
        void ChangeElementIndex(RectTransform elementTransform, int prevDataIndex, int nextDataIndex);
    }
}