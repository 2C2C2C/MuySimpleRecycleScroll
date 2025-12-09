using System;

namespace RecycleScrollView
{
    [Serializable]
    public struct SingleDirectionScrollParam
    {
        public ScrollDirection scrollDirection;

        // Reverse data elements into UI elment
        public bool reverseArrangement;

        public float spacing;
        // TODO padding?

        public readonly bool IsHorizontal => ScrollDirection.Horizontal_LeftToRight == scrollDirection || ScrollDirection.Horizontal_RightToLeft == scrollDirection;
        public readonly bool IsVertical => ScrollDirection.Vertical_UpToDown == scrollDirection || ScrollDirection.Vertical_DownToUp == scrollDirection;

    }
}