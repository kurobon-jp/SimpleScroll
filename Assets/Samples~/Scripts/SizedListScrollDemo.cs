using UnityEngine;

namespace SimpleScroll.Samples
{
    public class SizedListScrollDemo : MonoBehaviour, ISizedDataSource
    {
        [SerializeField] private SizedListScroll _listScroll;
        [SerializeField] private CellView[] _cellViews;
        [SerializeField] private float[] _cellViewSizes;
        [SerializeField] private int _dataCount = 10;

        void Start()
        {
            _listScroll.SetDataSource(this);
            _listScroll.Refresh();
        }

        int IDataSource.GetDataCount()
        {
            return _dataCount;
        }

        void IDataSource.SetData(int index, GameObject go)
        {
            if (go.TryGetComponent<CellView>(out var cellView))
            {
                cellView.Setup(index);
            }
        }

       public int GetCellViewType(int index)
        {
            return index % _cellViews.Length;
        }

        GameObject IDataSource.GetCellView(int index)
        {
            return _cellViews[GetCellViewType(index)].gameObject;
        }

        float ISizedDataSource.GetCellViewSize(int index)
        {
            return _cellViewSizes[GetCellViewType(index)];
        }
    }
}