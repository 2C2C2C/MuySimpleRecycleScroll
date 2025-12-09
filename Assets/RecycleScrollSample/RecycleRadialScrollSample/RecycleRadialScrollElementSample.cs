using UnityEngine;
using UnityEngine.UI;

namespace RecycleScrollView.Sample
{
    public class RecycleRadialScrollElementSample : MonoBehaviour
    {
        [SerializeField]
        private Text _text;

        public void SetText(string str)
        {
            _text.text = str;
        }

    }
}