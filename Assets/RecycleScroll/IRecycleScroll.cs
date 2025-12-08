using System.Collections.Generic;

namespace RecycleScrollView
{
    /// <summary> Some basic add/remove methods </summary>
    public interface IRecycleScroll
    {
        public void Init(IRecycleScrollDataSource dataSource);
        public void UnInit();

        public void AddElementTotail();
        public void AddElementsToTail(int count);

        /// <param name="dataIndex"> -1 means add element to the top </param>
        public void InsertElement(int dataIndex);
        /// <param name="dataIndex"> -1 means add elements to the top </param>
        public void InsertElements(int dataIndex, int count);
        public void InsertElements(IReadOnlyList<int> sortedDataIndexList);

        public void RemoveElement(int dataIndex);
        public void RemoveElements(int dataIndex, int count);
        public void RemoveElements(IReadOnlyList<int> sortedDataIndexList);

        /// <summary>
        /// When data updated, we may need to reculate the element size and also refresh view
        /// </summary>
        /// <param name="dataIndex"></param>
        public void UpdateElement(int dataIndex);
    }
}