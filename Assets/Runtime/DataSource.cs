using UnityEngine;

namespace SimpleScroll
{
    /// <summary>
    /// Data source.
    /// </summary>
    public interface IDataSource
    {
        int GetDataCount();
        void SetData(int index, GameObject go);
        int GetCellViewType(int index);
        GameObject GetCellView(int index);
    }

    public interface ISizedDataSource : IDataSource
    {
        float GetCellViewSize(int index);
    }
}