using UnityEngine;

namespace RecycleScrollView.Sample
{
    public class RecycleRadialScrollSample : MonoBehaviour, IRecycleScrollDataSource
    {
        [SerializeField]
        private RecycleRadialScroll _scroll;
        [SerializeField]
        private RectTransform _elementTemplate;

        [SerializeField]
        private int _startDataCount = 64;

        public int DataElementCount => _startDataCount;

        public RectTransform RequestElement(RectTransform parent)
        {
            RectTransform spawned = RectTransform.Instantiate(_elementTemplate, parent);
            spawned.gameObject.SetActive(true);
            return spawned;
        }

        public void ReturnElement(RectTransform element)
        {
            GameObject.Destroy(element.gameObject);
        }

        public void ChangeElementIndex(RectTransform element, int prevIndex, int nextIndex)
        {

        }

        public void InitElement(RectTransform element, int index)
        {

        }

        public void UnInitElement(RectTransform element)
        {

        }

        private void Start()
        {
            _scroll.Init(this);
        }

#if UNITY_EDITOR

        private void Reset()
        {
            TryGetComponent<RecycleRadialScroll>(out _scroll);
        }

#endif

    }
}