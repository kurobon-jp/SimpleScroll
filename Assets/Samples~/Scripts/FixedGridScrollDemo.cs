using UnityEngine;

namespace SimpleScroll.Samples
{
    public class FixedGridScrollDemo : MonoBehaviour, IDataSource
    {
        [SerializeField] private FixedGridScroll _gridScroll;
        [SerializeField] private CellView _cellView;
        [SerializeField] private int _dataCount = 10;

        void Start()
        {
            _gridScroll.SetDataSource(this);
            _gridScroll.Refresh();
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

        int IDataSource.GetCellViewType(int index)
        {
            return 0;
        }

        GameObject IDataSource.GetCellView(int index)
        {
            return _cellView.gameObject;
        }
    }
}