using UnityEngine;
using UnityEngine.EventSystems;
using MovementType = UnityEngine.UI.ScrollRect.MovementType;

namespace RecycleScrollView
{
    // Handle inputs for scroller
    public partial class Scroller : IInitializePotentialDragHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler
    {
        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (PointerEventData.InputButton.Left != eventData.button || !IsActive())
            {
                return;
            }

            if (m_isDragging)
            {
                if (eventData.pointerId == m_dragPointerId)
                {
                    // Same pointer Id init a new drag???
                }
                return;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (PointerEventData.InputButton.Left != eventData.button || !IsActive())
            {
                return;
            }

            if (m_isDragging)
            {
                if (eventData.pointerId == m_dragPointerId)
                {
                    // Same pointer Id start a new drag???
                }
                return;
            }

            m_pointerStartLocalCursor = Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, eventData.pressEventCamera, out m_pointerStartLocalCursor);

            m_dragStartNormalizedPosition = NormalizedPosition;
            m_dragStartOffset = m_beyoudEdgeOffset;

            m_isDragging = true;
            m_dragPointerId = eventData.pointerId;
            StopMovement();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!m_isDragging || PointerEventData.InputButton.Left != eventData.button || !IsActive())
            {
                return;
            }

            if (eventData.pointerId == m_dragPointerId)
            {
                m_currentPointerDelta = Vector2.zero;
                m_dragPointerId = int.MinValue;
                m_isDragging = false;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, eventData.pressEventCamera, out _);
            }
        }

        // TODO clarify the meaning of move delta
        public void OnDrag(PointerEventData eventData)
        {
            if (!m_isDragging || PointerEventData.InputButton.Left != eventData.button || !IsActive())
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_viewport, eventData.position, eventData.pressEventCamera, out Vector2 cursorLocalPos))
            {
                /* HACK
                *  The actual content move is reverse from pointer delta
                *  (e.g Delta x greater than 0 means content move to right side,
                *  and the content's left side will get close to viewport left side which means the normalized position get close to 0)
                */
                UpdateBounds();
                m_currentPointerDelta = cursorLocalPos - m_pointerStartLocalCursor;

                // Drag move expected result
                Vector2 tempMoveDelta = -m_currentPointerDelta + m_dragStartOffset;
                ClampNormalizedPositionAndOffset(m_dragStartNormalizedPosition, tempMoveDelta, out Vector2 nextNormalizedPos, out Vector2 nextOffset);
                if (Vector2.zero != nextOffset)
                {
                    switch (_movementType)
                    {
                        case MovementType.Elastic:
                            if (nextOffset.x != 0)
                            {
                                nextOffset.x -= RubberDelta(nextOffset.x, m_viewportBounds.size.x);
                            }
                            if (nextOffset.y != 0)
                            {
                                nextOffset.y -= RubberDelta(nextOffset.y, m_viewportBounds.size.y);
                            }
                            break;
                        case MovementType.Clamped:
                            nextOffset = Vector2.zero;
                            break;
                        default:
                            break;
                    }
                    // RecycleScrollLogger.Log($"move {tempMoveDelta}; base position {m_dragStartNormalizedPosition} ;normalizedMove {nextNormalizedPos - m_dragStartNormalizedPosition}; result pos {nextNormalizedPos}; offset {m_beyoudEdgeOffset}");
                }
                else
                {
                    nextOffset = Vector2.zero;
                }

                InternalSetNormalizedPosition(nextNormalizedPos, nextOffset);
            }
        }

        // TODO clarify the meaning of move delta
        public void OnScroll(PointerEventData eventData)
        {
            if (!IsActive())
            {
                return;
            }

            UpdateBounds();
            Vector2 delta = eventData.scrollDelta;
            // Down is positive for scroll events, while in UI system up is positive.
            if (Vertical && !Horizontal)
            {
                if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                    delta.y = delta.x;
                delta.x = 0;
            }
            if (Horizontal && !Vertical)
            {
                if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x))
                    delta.x = delta.y;
                delta.y = 0;
            }

            if (eventData.IsScrolling())
            {
                m_isScrolling = true;
            }

            Vector2 move = delta * _scrollSensitivity;
            ClampNormalizedPositionAndOffset(NormalizedPosition, move, out Vector2 nextNormalizedPos, out Vector2 nextOffset);
            if (MovementType.Clamped == _movementType)
            {
                nextOffset = Vector2.zero;
            }
            SetNormalizedPositionWithNotifyIfNeed(nextNormalizedPos, nextOffset);
            UpdateBounds();
        }

    }
}