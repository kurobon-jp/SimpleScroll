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
        GameObject GetCellView(int index);

        public int GetCellViewType(int index)
        {
            return 0;
        }
    }

    public interface ISizedDataSource : IDataSource
    {
        float GetCellViewSize(int index);
    }

    public static class DataSourceExtensions
    {
        public static int GetDataIndex(this IDataSource source, int position)
        {
            var count = source.GetDataCount();
            if (count == 0) return 0;
            return (position % count + count) % count;
        }
    }
}