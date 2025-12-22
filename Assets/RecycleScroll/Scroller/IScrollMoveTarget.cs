using UnityEngine;

namespace RecycleScrollView
{
    /// <summary> The target of a scroller </summary>
    public interface IScrollMoveTarget
    {
        /// <summary>
        /// Convert units move into normalized move for current target position.
        /// If this movement will make the actual content beyoud the viewport edge, the extraOffset will be the actual units off the edge.
        /// </summary>
        /// <param name="move"> Units move</param>
        /// <param name="extraOffset"> Actual units off the edge</param>
        /// <returns> Normalized move(not clamped) </returns>
        Vector2 ConvertToNormalizedMoveFromCurrentPosition(Vector2 move, out Vector2 extraOffset);
    }
}