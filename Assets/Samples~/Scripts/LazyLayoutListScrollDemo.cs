using UnityEngine;

namespace SimpleScroll.Samples
{
    public class LazyLayoutListScrollDemo : MonoBehaviour
    {
        [SerializeField] private LazyLayoutListScroll _listScroll;
        [SerializeField] private GameObject[] _cellViews;
        [SerializeField] private int _dataCount = 100;

        private DemoDataSource _dataSource;

        private readonly struct ChatMessage
        {
            private const string Characters = " abcdefghijklmnopqrstuvwxyz";

            public readonly int Id;
            public readonly string Name;
            public readonly string Text;

            public ChatMessage(int id)
            {
                Id = id;
                Name = id.ToString();
                // Keep messages deterministic without changing randomness elsewhere in the scene.
                var randomState = Random.state;
                try
                {
                    Random.InitState(id);
                    var characters = new char[Random.Range(10, 500)];
                    for (var i = 0; i < characters.Length; i++)
                    {
                        characters[i] = Characters[Random.Range(0, Characters.Length)];
                    }
                    Text = new string(characters);
                }
                finally
                {
                    Random.state = randomState;
                }
            }
        }

        private class DemoDataSource : LazyLayoutDataSource<ChatMessage>
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
                    var message = this[index];
                    cellView.Setup(message.Name, message.Text);
                }
            }

            public override int GetCellViewType(int index)
            {
                return this[index].Id % 2;
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
                _dataSource.Add(new ChatMessage(i));
            }

            _listScroll.SetDataSource(_dataSource);
            _listScroll.Refresh();
        }

    }
}
