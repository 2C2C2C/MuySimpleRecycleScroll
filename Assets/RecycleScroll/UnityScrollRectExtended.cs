using System;

namespace UnityEngine.UI
{
    [AddComponentMenu("UI/UnityScrollRectExtended", 1)]
    [SelectionBase]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class UnityScrollRectExtended : ScrollRect
    {
        [SerializeField]
        private float _velocityStopSqrMagThreshold = 7f;
        [SerializeField]
        private float _velocityMaxSqrMag = 1000f;

        public Vector2 ContentStartPos
        {
            get => m_ContentStartPosition;
            set
            {
                m_ContentStartPosition = value;
            }
        }

        public event Action BeforeLateUpdate;
        public event Action AfterLateUpdate;

        public void CallUpdateBoundsAndPrevData()
        {
            SetDirtyCaching();
            base.Rebuild(CanvasUpdate.PostLayout);
        }

        public Vector2 CalculateCurrentOffset(Vector2 delta)
        {
            var viewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);
            Bounds contentBounds = m_ContentBounds;
            Vector2 offset = Vector2.zero;
            if (movementType == MovementType.Unrestricted)
                return offset;

            Vector2 min = contentBounds.min;
            Vector2 max = contentBounds.max;

            // min/max offset extracted to check if approximately 0 and avoid recalculating layout every frame (case 1010178)

            if (horizontal)
            {
                min.x += delta.x;
                max.x += delta.x;

                float maxOffset = viewBounds.max.x - max.x;
                float minOffset = viewBounds.min.x - min.x;

                if (minOffset < -0.001f)
                    offset.x = minOffset;
                else if (maxOffset > 0.001f)
                    offset.x = maxOffset;
            }

            if (vertical)
            {
                min.y += delta.y;
                max.y += delta.y;

                float maxOffset = viewBounds.max.y - max.y;
                float minOffset = viewBounds.min.y - min.y;

                if (maxOffset > 0.001f)
                    offset.y = maxOffset;
                else if (minOffset < -0.001f)
                    offset.y = minOffset;
            }

            return offset;
        }

        protected override void LateUpdate()
        {
            BeforeLateUpdate?.Invoke();
            base.LateUpdate();
            AdjustVelocity();
            AfterLateUpdate?.Invoke();
        }

        private void AdjustVelocity()
        {
            Vector2 currentVelocity = velocity;
            // To prevent moving with very low velocity
            if (_velocityStopSqrMagThreshold * _velocityStopSqrMagThreshold > currentVelocity.sqrMagnitude)
            {
                base.StopMovement();
            }
            else if (_velocityMaxSqrMag * _velocityMaxSqrMag < currentVelocity.sqrMagnitude)
            {
                currentVelocity = _velocityMaxSqrMag * currentVelocity.normalized;
                base.velocity = currentVelocity;
            }
        }

#if UNITY_EDITOR

        protected override void Reset()
        {
            base.Reset();
            _velocityStopSqrMagThreshold = 7f;
            _velocityMaxSqrMag = 1000f;
        }

#endif

    }
}