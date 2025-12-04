using System.Collections.Generic;

namespace RecycleScrollView
{
    public interface IRecycleScroll
    {
        /// <param name="dataIndex"> -1 means add element to the top </param>
        public void InsertElement(int dataIndex);
        /// <param name="dataIndex"> -1 means add elements to the top </param>
        public void InsertRange(int dataIndex, int count);
        public void AddElementTotail();

        public void RemoveElement(int dataIndex);
        public void RemoveRange(int dataIndex, int count);

        public void InsertElements(IReadOnlyList<int> sortedDataIndexList);
        public void RemoveElements(IReadOnlyList<int> sortedDataIndexList);

        /// <summary>
        /// When data updated, we may need to reculate the element size and also refresh view
        /// </summary>
        /// <param name="dataIndex"></param>
        public void UpdateElement(int dataIndex);
    }
}