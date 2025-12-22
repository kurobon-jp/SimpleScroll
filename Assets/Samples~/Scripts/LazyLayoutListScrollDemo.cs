using UnityEngine;

namespace SimpleScroll.Samples
{
    public class LazyLayoutListScrollDemo : MonoBehaviour
    {
        [SerializeField] private LazyLayoutListScroll _listScroll;
        [SerializeField] private GameObject[] _cellViews;
        [SerializeField] private int _dataCount = 100;

        private DemoDataSource _dataSource;
        private int _ids;

        private class DemoDataSource : LazyLayoutDataSource<int>
        {
            private readonly GameObject[] _cellViews;

            public DemoDataSource(GameObject[] cellView)
            {
                _cellViews = cellView;
            }

            public override void SetData(int index, GameObject go)
            {
                if (go.TryGetComponent<ChatView>(out var cellView))
                {
                    cellView.Setup(this[index]);
                }
            }

            public override int GetCellViewType(int index)
            {
                return this[index] % 2;
            }

            public override GameObject GetCellView(int index)
            {
                return _cellViews[GetCellViewType(index)];
            }
        }

        void Start()
        {
            _dataSource = new DemoDataSource(_cellViews);
            for (var i = 0; i < _dataCount; i++)
            {
                _dataSource.Add(_ids++);
            }

            _listScroll.SetDataSource(_dataSource);
            _listScroll.SetNormalizedPosition(1f);
        }

    }
}