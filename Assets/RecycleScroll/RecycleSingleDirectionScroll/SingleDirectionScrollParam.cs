using System;

namespace RecycleScrollView
{
    [Serializable]
    public struct SingleDirectionScrollParam
    {
        public enum ScrollDirection
        {
            None = 0,
            /// <summary> 
            /// In this case content pivot must be (0, 0.5)
            /// Horizontal layout from left to right, Alignment MiddleLeft
            /// </summary>
            Horizontal_LeftToRight = 1,
            /// <summary> 
            /// In this case content pivot must be (1, 0.5) 
            /// Horizontal layout from right to left(reverse arrangement), Alignment MiddleRight
            /// </summary>
            Horizontal_RightToLeft = 2,
            /// <summary> 
            /// In this case content pivot must be (0.5, 1)
            /// Vertical layout from up to down, Alignment UpperCenter 
            /// </summary>
            Vertical_UpToDown = 3,
            /// <summary> 
            /// In this case content pivot must be (0.5, 0)
            /// Vertical layout from down to up(reverse arrangement), Alignment LowerCenter
            ///  </summary>
            Vertical_DownToUp = 4,
        }

        public ScrollDirection scrollDirection;

        // Reverse data elements into UI elment
        public bool reverseArrangement;

        public float spacing;
        // TODO padding?

        public readonly bool IsHorizontal => ScrollDirection.Horizontal_LeftToRight == scrollDirection || ScrollDirection.Horizontal_RightToLeft == scrollDirection;
        public readonly bool IsVertical => ScrollDirection.Vertical_UpToDown == scrollDirection || ScrollDirection.Vertical_DownToUp == scrollDirection;

    }
}