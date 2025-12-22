using UnityEngine;
using UnityEngine.UI;

namespace RecycleScrollView
{
    // TODO Figure it out how to sync scroll position to bar properly 
    public partial class Scroller
    {
        // private Scrollbar _horizontalScrollBar;
        // private Scrollbar _verticalScrollBar;

        private void SyncValueToScrollBar()
        {
            // Vector2 normalizedPosition = NormalizedPosition;
            // if (null != _horizontalScrollBar)
            // {
            //     if (Horizontal)
            //     {
            //         _horizontalScrollBar.SetValueWithoutNotify(normalizedPosition.x);
            //     }
            //     else
            //     {
            //         _horizontalScrollBar.gameObject.SetActive(false);
            //     }
            // }

            // if (null != _verticalScrollBar)
            // {
            //     if (Vertical)
            //     {
            //         _verticalScrollBar.SetValueWithoutNotify(normalizedPosition.y);
            //     }
            //     else
            //     {
            //         _verticalScrollBar.gameObject.SetActive(false);
            //     }
            // }
        }
    }
}