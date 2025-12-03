using System;
using UnityEngine;

namespace RecycleScrollView
{
    public interface IGridScrollDataSource
    {
        int DataElementCount { get; }

        RectTransform AddElement(RectTransform parent);
        void RemoveElement(RectTransform element);

        /// <summary>
        /// Let the data source init the element when it is added to the list, index may be -1, if the element is not used yet
        /// </summary>
        /// <param name="element"></param>
        /// <param name="index">May be -1, if the element is not used yet</param>
        void InitElement(RectTransform element, int index);
        void UnInitElement(RectTransform element);
        void ChangeElementIndex(RectTransform element, int prevIndex, int nextIndex);

        event Action<int> OnDataElementCountChanged;
    }
}