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
            _carouselScroll.OnSelected += i =>
            {
                _selected.text = i.ToString();
                Debug.Log($"Selected data index: {i}");
            };
            _carouselScroll.OnReposition += (cell, i, f) =>
            {
                var scale = Mathf.Lerp(1f, 0.75f, f);
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
            var dataIndex = this.GetDataIndex(position);
            cellView.Setup(dataIndex, position, onClick: _ => _carouselScroll.SetPositionIndex(position));
        }

        GameObject IDataSource.GetCellView(int index)
        {
            return _cellView;
        }
    }
}