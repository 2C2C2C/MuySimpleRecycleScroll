using UnityEngine;

namespace RecycleScrollView
{
    // Layout part
    public partial class RecycleRadialScroll
    {
        [Header("Layout params")]
        [SerializeField]
        private float _radius;
        [SerializeField, Range(0f, 360f)]
        private float _startAngle;
        [SerializeField, Range(0f, 360f)]
        private float _internvalAngle;
        [SerializeField]
        private bool _antiClockwise;
        [SerializeField]
        private bool _reverseArrangment = false;


    }
}