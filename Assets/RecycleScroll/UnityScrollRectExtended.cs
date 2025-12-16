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