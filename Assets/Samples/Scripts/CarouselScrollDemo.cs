using UnityEngine;

namespace SimpleScroll.Samples
{
    public class CarouselScrollDemo : MonoBehaviour, IDataSource
    {
        [SerializeField] private CarouselScroll _carouselScroll;
        [SerializeField] private CellView _cellView;
        [SerializeField] private int _dataCount = 10;

        void Start()
        {
            _carouselScroll.OnSelected += i => Debug.Log($"selected data index: {i}");
            _carouselScroll.OnReposition += (cell, i, f) =>
            {
                var scale = Mathf.Lerp(1f, 0.5f, f);
                cell.localScale = new Vector3(scale, scale, 1f);
            };

            _carouselScroll.SetDataSource(this);
            _carouselScroll.Refresh();
        }

        int IDataSource.GetDataCount()
        {
            return _dataCount;
        }

        void IDataSource.SetData(int position, GameObject go)
        {
            if (!go.TryGetComponent<CellView>(out var cellView)) return;
            var dataIndex = _carouselScroll.GetDataIndex(position);
            cellView.Setup(dataIndex, position, onClick: _ => _carouselScroll.SetPositionIndex(position));
        }

        public int GetCellViewType(int index)
        {
            return 0;
        }

        GameObject IDataSource.GetCellView(int index)
        {
            return _cellView.gameObject;
        }
    }
}