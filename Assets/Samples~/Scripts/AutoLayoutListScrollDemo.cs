using UnityEngine;

namespace SimpleScroll.Samples
{
    public class AutoLayoutListScrollDemo : MonoBehaviour, IDataSource
    {
        [SerializeField] private AutoLayoutListScroll _listScroll;
        [SerializeField] private ChatView[] _cellViews;
        [SerializeField] private int _dataCount = 100;

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
            if (go.TryGetComponent<ChatView>(out var cellView))
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
    }
}