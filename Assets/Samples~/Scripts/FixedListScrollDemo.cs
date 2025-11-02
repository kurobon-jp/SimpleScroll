using UnityEngine;

namespace SimpleScroll.Samples
{
    public class FixedListScrollDemo : MonoBehaviour, IDataSource
    {
        [SerializeField] private FixedListScroll _listScroll;
        [SerializeField] private GameObject _cellView;
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

        GameObject IDataSource.GetCellView(int index)
        {
            return _cellView;
        }
    }
}