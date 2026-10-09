using UnityEngine;
using UnityEngine.UI;

namespace SimpleScroll.Samples
{
    public class CarouselScrollDemo : MonoBehaviour, IDataSource
    {
        [SerializeField] private CarouselScroll _carouselScroll;
        [SerializeField] private GameObject _cellView;
        [SerializeField] private int _dataCount = 10;
        [SerializeField] private Text _selected;

        void Start()
        {
            _carouselScroll.OnSelected += pos =>
            {
                _selected.text = pos.ToString();
                Debug.Log($"Selected position {pos}");
            };

            _carouselScroll.OnReposition += (cell, pos, distance) =>
            {
                var scale = Mathf.Lerp(1f, 0.75f, Mathf.Abs(distance));
                cell.localScale = new Vector3(scale, scale, 1f);
            };

            _carouselScroll.SetDataSource(this);
            _carouselScroll.SetPositionIndex(0, false);
            _carouselScroll.Refresh();
        }

        int IDataSource.GetDataCount()
        {
            return _dataCount;
        }

        void IDataSource.SetData(int pos, GameObject go)
        {
            if (!go.TryGetComponent<CellView>(out var cellView)) return;
            var dataIndex = this.GetDataIndex(pos);
            cellView.Setup(dataIndex, pos, onClick: _ => _carouselScroll.SetPositionIndex(pos));
        }

        GameObject IDataSource.GetCellView(int index)
        {
            return _cellView;
        }
    }
}