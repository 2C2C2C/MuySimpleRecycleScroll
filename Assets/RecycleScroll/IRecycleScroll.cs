using System.Collections.Generic;

namespace RecycleScrollView
{
    public interface IRecycleScroll
    {
        public void InsertElement(int dataIndex);
        public void RemoveElement(int dataIndex);

        public void InsertRange(int dataIndex, int count);
        public void RemoveRange(int dataIndex, int count);

        public void InsertElements(IReadOnlyList<int> sortedDataIndexList);
        public void RemoveElements(IReadOnlyList<int> sortedDataIndexList);
    }
}