namespace RecycleScrollView
{
    public partial class RecycleGridScroll
    {
        private const int INVALID_INDEX = -1;

        private bool TryGetHeadIndexOfUsingElements(out int index)
        {
            if (null != m_gridElements && 0 < m_gridElements.Count)
            {
                for (int i = 0, gridCount = m_gridElements.Count; i < gridCount; i++)
                {
                    RecycleGridScrollElement grid = m_gridElements[i];
                    if (INVALID_INDEX == grid.ElementIndex)
                    {
                        continue;
                    }
                    index = grid.ElementIndex;
                    return true;
                }
            }
            index = INVALID_INDEX;
            return false;
        }

        private bool TryGetTailIndexboundOfUsingElements(out int index)
        {
            if (null != m_gridElements && 0 < m_gridElements.Count)
            {
                for (int i = m_gridElements.Count - 1; i > -1; i--)
                {
                    RecycleGridScrollElement grid = m_gridElements[i];
                    if (INVALID_INDEX == grid.ElementIndex)
                    {
                        continue;
                    }
                    index = grid.ElementIndex;
                    return true;
                }
            }
            index = INVALID_INDEX;
            return false;
        }

        private bool IsCurrentLayoutDataInvalid()
        {
            bool isInvalid = null == _gridLayoutData ||
                0 > _gridLayoutData.gridSize.x ||
                0 > _gridLayoutData.gridSize.y ||
                0 > _gridLayoutData.constraintCount;
            return isInvalid;
        }

    }
}