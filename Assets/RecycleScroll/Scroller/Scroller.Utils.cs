using UnityEngine;
using MovementType = UnityEngine.UI.ScrollRect.MovementType;

namespace RecycleScrollView
{
    // Some utils methods
    public partial class Scroller
    {
        /// <summary> Init bounds with special size(scroll size will be the as viewport size) just for visualized scroll progress </summary>
        private void InitDefaultBounds()
        {
            m_viewportBounds = new Bounds(_viewport.rect.center, _viewport.rect.size);
            Vector2 contentSize = _viewport.rect.size;
            if (Horizontal)
            {
                contentSize.x = 2f * contentSize.x;
            }
            if (Vertical)
            {
                contentSize.y = 2f * contentSize.y;
            }
            m_contentBounds = new Bounds(_viewport.rect.center, contentSize);
        }

        /// <summary> Update bounds for virtual content </summary>
        private void UpdateBounds()
        {
            if (MovementType.Clamped == _movementType)
            {
                Vector2 nextClampedPosition = NormalizedPosition;
                nextClampedPosition.x = Mathf.Clamp01(nextClampedPosition.x);
                nextClampedPosition.y = Mathf.Clamp01(nextClampedPosition.y);
                SetNormalizedPositionWithNotifyIfNeed(nextClampedPosition, Vector2.zero);
            }
            Vector2 contentCenter = CenterFromNormalized(NormalizedPosition);
            Vector2 extraOffset = BeyoudEdgeOffset;
            if (Vector2.zero != extraOffset)
            {
                // HACK For content center
                contentCenter -= extraOffset;
            }
            SetContentBoundCenterPosition(contentCenter);
        }

        private Vector2 CenterFromNormalized(in Vector2 normalized)
        {
            Vector2 minCenter = GetMinCenter();
            Vector2 range = GetMovementRange();
            Vector2 result = new Vector2(
                    // Invert X mapping so normalized.y == 1 corresponds to content at the TOP
                    range.x > 0f ? minCenter.x + (1f - normalized.x) * range.x : m_viewportBounds.center.x,
                    // Invert Y mapping so normalized.y == 1 corresponds to content at the TOP
                    range.y > 0f ? minCenter.y + (1f - normalized.y) * range.y : m_viewportBounds.center.y
                );
            return result;
        }

        private Vector2 GetMinCenter()
        {
            // min center = viewport center - range / 2
            Vector2 range = GetMovementRange();
            Vector2 viewportCenter = (Vector2)m_viewportBounds.center;
            return viewportCenter - range * 0.5f;
        }

        private Vector2 GetMovementRange()
        {
            Vector2 range = Vector2.zero;
            range.x = Mathf.Max(0f, m_contentBounds.size.x - m_viewportBounds.size.x);
            range.y = Mathf.Max(0f, m_contentBounds.size.y - m_viewportBounds.size.y);
            return range;
        }

        /// <param name="localPosition">The local position inside viewport</param>
        private void SetContentBoundCenterPosition(Vector2 localPosition)
        {
            Vector2 contentCenter = m_contentBounds.center;
            if (!Horizontal)
            {
                localPosition.x = contentCenter.x;
            }
            if (!Vertical)
            {
                localPosition.y = contentCenter.y;
            }
            if (localPosition != contentCenter)
            {
                m_contentBounds.center = localPosition;
            }
        }

        private Vector2 ClampVelocity(Vector2 input)
        {
            Vector2 result = input;
            // To prevent moving with very low velocity
            if (_velocityStopSqrMagThreshold * _velocityStopSqrMagThreshold > result.sqrMagnitude)
            {
                result = Vector2.zero;
            }
            else if (_velocityMaxSqrMag * _velocityMaxSqrMag < result.sqrMagnitude)
            {
                result = _velocityMaxSqrMag * result.normalized;
            }
            return result;
        }

        public void ClampNormalizedPositionAndOffset(Vector2 basePosition, Vector2 offset, out Vector2 finalPosition, out Vector2 finalOffset)
        {
            Vector2 normalizedMove = m_source.ConvertToNormalizedMoveFromCurrentPosition(offset, out finalOffset);
            finalPosition = basePosition + normalizedMove;
            finalPosition.x = Mathf.Clamp01(finalPosition.x);
            finalPosition.y = Mathf.Clamp01(finalPosition.y);
        }

        private static float RubberDelta(float overStretching, float viewSize)
        {
            return (1 - (1 / ((Mathf.Abs(overStretching) * 0.55f / viewSize) + 1))) * viewSize * Mathf.Sign(overStretching);
        }

    }
}