using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SimpleScroll.Tests
{
    public class ScrollTests
    {
        private GameObject _root;
        private GameObject _prefab;
        private RectTransform _content;

        private sealed class Source : ISizedDataSource
        {
            public int Count = 100;
            public GameObject Prefab;
            public readonly Dictionary<int, GameObject> Cells = new();
            public int BindCount;
            public float[] Sizes;
            public int GetDataCount() => Count;
            public GameObject GetCellView(int index) => Prefab;
            public int GetCellViewType(int index) => 0;
            public float GetCellViewSize(int index)
            {
                Assert.That(index, Is.InRange(0, Count - 1));
                return Sizes == null ? 100f : Sizes[index];
            }
            public void SetData(int index, GameObject cell)
            {
                Assert.That(index, Is.InRange(0, Count - 1));
                Cells[index] = cell;
                BindCount++;
            }
        }

        private T Create<T>(int axis) where T : Component
        {
            _root = new GameObject("Scroll test", typeof(RectTransform));
            _root.SetActive(false);
            var viewport = (RectTransform)_root.transform;
            viewport.sizeDelta = new Vector2(300, 300);
            _content = (RectTransform)new GameObject("Content", typeof(RectTransform)).transform;
            _content.SetParent(viewport, false);
            _prefab = new GameObject("Cell", typeof(RectTransform));
            ((RectTransform)_prefab.transform).sizeDelta = new Vector2(100, 100);
            var scroll = _root.AddComponent<T>();
            var baseType = typeof(T).BaseType;
            baseType.GetField("_viewport", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scroll, viewport);
            baseType.GetField("_content", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scroll, _content);
            var scroller = baseType.GetProperty("Scroller", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scroll);
            scroller.GetType().GetField("_axis", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(scroller, (RectTransform.Axis)axis);
            _root.SetActive(true);
            return scroll;
        }

        private static void Tick<T>(BaseScroll<T> scroll) where T : IDataSource
        {
            typeof(BaseScroll<T>).GetMethod("UpdatePosition", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(scroll, new object[] { scroll.ScrollPosition });
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            if (_prefab != null) Object.DestroyImmediate(_prefab);
        }

        [TestCase(0, 0f, -100f)]
        [TestCase(0, 0.5f, 0f)]
        [TestCase(0, 1f, 100f)]
        [TestCase(1, 0f, 100f)]
        [TestCase(1, 0.5f, 0f)]
        [TestCase(1, 1f, -100f)]
        public void FixedList_SpacingAndPaddingDoNotShiftCellAlignment(int axis, float anchor, float expectedCenter)
        {
            var scroll = Create<FixedListScroll>(axis);
            scroll.Space = 20f;
            var padding = new ContentPadding();
            var boxed = (object)padding;
            typeof(ContentPadding).GetField("_start", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boxed, 35f);
            scroll.ContentPadding = (ContentPadding)boxed;
            var source = new Source { Prefab = _prefab };
            scroll.SetDataSource(source);
            Tick(scroll);
            scroll.SetPositionIndex(10, anchor, false);
            Tick(scroll);
            Assert.That(source.Cells[10].transform.localPosition[axis] + _content.localPosition[axis],
                Is.EqualTo(expectedCenter).Within(0.001f));
        }

        [TestCase(0, -200f)]
        [TestCase(1, 200f)]
        public void FixedList_AlignsCellCenterWithViewportCenter(int axis, float expectedPosition)
        {
            var scroll = Create<FixedListScroll>(axis);
            var source = new Source { Prefab = _prefab };
            scroll.SetDataSource(source);
            Tick(scroll);
            scroll.SetPositionIndex(3, 0.5f, false);
            Tick(scroll);
            Assert.That(scroll.ScrollPosition, Is.EqualTo(expectedPosition).Within(0.001f));
            Assert.That(source.Cells[3].transform.localPosition[axis] + _content.localPosition[axis],
                Is.EqualTo(0f).Within(0.001f));
        }

        [TestCase(0, 0f, -100f)]
        [TestCase(0, 0.5f, 0f)]
        [TestCase(0, 1f, 100f)]
        [TestCase(1, 0f, 100f)]
        [TestCase(1, 0.5f, 0f)]
        [TestCase(1, 1f, -100f)]
        public void Grid_SpacingAndPaddingDoNotShiftCellAlignment(int axis, float anchor, float expectedCenter)
        {
            var scroll = Create<FixedGridScroll>(axis);
            scroll.Space = new Vector2(20f, 20f);
            var boxed = (object)new ContentPadding();
            typeof(ContentPadding).GetField("_start", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(boxed, 35f);
            scroll.ContentPadding = (ContentPadding)boxed;
            var source = new Source { Prefab = _prefab };
            scroll.SetDataSource(source);
            Tick(scroll);
            scroll.SetPositionIndex(30, anchor, false);
            Tick(scroll);
            Assert.That(source.Cells[30].transform.localPosition[axis] + _content.localPosition[axis],
                Is.EqualTo(expectedCenter).Within(0.001f));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void SizedList_AlignsRequestedCell(int axis)
        {
            var scroll = Create<SizedListScroll>(axis);
            var source = new Source { Prefab = _prefab };
            scroll.SetDataSource(source);
            Tick(scroll);
            scroll.SetPositionIndex(4, 0.5f, false);
            Tick(scroll);
            Assert.That(source.Cells[4].transform.localPosition[axis] + _content.localPosition[axis],
                Is.EqualTo(0f).Within(0.001f));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void SizedList_VariableSizesNavigateUsingCumulativeOffsets(int axis)
        {
            var scroll = Create<SizedListScroll>(axis);
            var source = new Source
            {
                Prefab = _prefab, Count = 6,
                Sizes = new[] { 50f, 150f, 75f, 125f, 100f, 200f }
            };
            scroll.SetDataSource(source);
            Tick(scroll);
            scroll.SetPositionIndex(3, 0.5f, false);
            Tick(scroll);
            Assert.That(scroll.ScrollPosition,
                Is.EqualTo(axis == 0 ? -187.5f : 187.5f).Within(0.001f));
            Assert.That(source.Cells[3].transform.localPosition[axis] + _content.localPosition[axis],
                Is.EqualTo(0f).Within(0.001f));
            scroll.SetPositionIndex(100, 1f, false);
            Assert.That(scroll.NormalizedPosition, Is.EqualTo(1f).Within(0.001f));
        }

        private sealed class MeasuredSource : LazyLayoutDataSource<int>
        {
            public GameObject Prefab;
            public readonly Dictionary<int, GameObject> Cells = new();
            public override GameObject GetCellView(int index) => Prefab;
            public override int GetCellViewType(int index) => 0;
            public override void SetData(int index, GameObject cell) => Cells[index] = cell;
        }

        [TestCase(0)]
        [TestCase(1)]
        public void LazyLayout_MeasuresCellsAndFillsViewportWhenEstimatesShrink(int axis)
        {
            var scroll = Create<LazyLayoutListScroll>(axis);
            ((RectTransform)_prefab.transform).sizeDelta = new Vector2(40f, 40f);
            var source = new MeasuredSource { Prefab = _prefab };
            source.AddRange(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            scroll.SetDataSource(source);
            Tick(scroll);
            Assert.That(source.GetCellViewSize(0), Is.EqualTo(40f));
            Assert.That(source.GetCellViewOffset(1), Is.EqualTo(40f));
            Assert.That(source.Cells.ContainsKey(7), Is.True);
            var cellPosition = source.Cells[0].transform.localPosition[axis];
            Assert.That(cellPosition, Is.EqualTo(axis == 0 ? -130f : 130f).Within(0.001f));
            Assert.That(_content.childCount, Is.LessThan(source.GetDataCount()));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void Grid_AlignsRequestedRowAndDoesNotBindPastLastItem(int axis)
        {
            var scroll = Create<FixedGridScroll>(axis);
            var source = new Source { Prefab = _prefab, Count = 11 };
            scroll.SetDataSource(source);
            Tick(scroll);
            scroll.SetPositionIndex(9, 1f, false);
            Tick(scroll);
            Assert.That(scroll.NormalizedPosition, Is.EqualTo(1f).Within(0.001f));
            Assert.That(source.Cells.ContainsKey(10), Is.True);
            Assert.That(source.Cells.ContainsKey(11), Is.False);
        }

        [Test]
        public void FixedList_ReusesCellsAndRefreshRebindsVisibleItems()
        {
            var scroll = Create<FixedListScroll>(1);
            var source = new Source { Prefab = _prefab };
            scroll.SetDataSource(source);
            Tick(scroll);
            var created = _content.childCount;
            Assert.That(created, Is.LessThan(source.Count));
            var originalCells = new HashSet<GameObject>(source.Cells.Values);
            scroll.SetScrollPosition(2000f, false);
            Tick(scroll);
            Assert.That(_content.childCount, Is.EqualTo(created));
            Assert.That(originalCells.Contains(source.Cells[20]), Is.True);
            var binds = source.BindCount;
            Tick(scroll);
            Assert.That(source.BindCount, Is.EqualTo(binds));
            scroll.Refresh();
            Tick(scroll);
            Assert.That(source.BindCount, Is.GreaterThan(binds));
        }

        [Test]
        public void FixedList_EmptyDataReleasesVisibleCells()
        {
            var scroll = Create<FixedListScroll>(1);
            var source = new Source { Prefab = _prefab };
            scroll.SetDataSource(source);
            Tick(scroll);
            source.Count = 0;
            scroll.Refresh();
            Tick(scroll);
            Assert.That(scroll.VisibleRange, Is.EqualTo(Range.Empty));
            foreach (Transform cell in _content) Assert.That(cell.gameObject.activeSelf, Is.False);
        }

        [Test]
        public void SizedList_EmptyDataNavigationDoesNotReadAnItem()
        {
            var scroll = Create<SizedListScroll>(1);
            scroll.SetDataSource(new Source { Prefab = _prefab, Count = 0 });
            scroll.Refresh();
            Assert.DoesNotThrow(() => scroll.SetPositionIndex(5, smooth: false));
            Tick(scroll);
            Assert.That(scroll.VisibleRange, Is.EqualTo(Range.Empty));
        }

        [Test]
        public void LazyLayout_InspectorBindsEstimatedSizeField()
        {
            var scroll = Create<LazyLayoutListScroll>(1);
            var serialized = new SerializedObject(scroll);
            Assert.That(serialized.FindProperty("_estimatedCellSize"), Is.Not.Null);
            var editor = UnityEditor.Editor.CreateEditor(scroll);
            try
            {
                var inspector = editor.CreateInspectorGUI();
                var found = false;
                inspector.Query<UnityEditor.UIElements.PropertyField>().ForEach(field =>
                {
                    if (field.bindingPath == "_estimatedCellSize") found = true;
                });
                Assert.That(found, Is.True);
            }
            finally { Object.DestroyImmediate(editor); }
        }
    }

    public class DataSourceTests
    {
        private sealed class Source : LazyLayoutDataSource<int>
        {
            public override void SetData(int index, GameObject cell) { }
            public override GameObject GetCellView(int index) => null;
            public override int GetCellViewType(int index) => 0;
        }

        [TestCase(-1, 2)]
        [TestCase(-4, 2)]
        [TestCase(0, 0)]
        [TestCase(4, 1)]
        public void LoopPositionMapsToDataIndex(int position, int expected)
        {
            IDataSource source = new Source();
            ((Source)source).AddRange(new[] { 10, 20, 30 });
            Assert.That(source.GetDataIndex(position), Is.EqualTo(expected));
        }

        [Test]
        public void MeasuredSizesUpdateOffsetsAndTotalLength()
        {
            var source = new Source();
            source.AddRange(new[] { 10, 20, 30 });
            Assert.That(source.GetTotalContentSize(default, 100f, 10f), Is.EqualTo(320f));
            source.SetCellViewSize(0, 40f);
            source.SetCellViewSize(1, 150f);
            Assert.That(source.GetTotalContentSize(default, 100f, 10f), Is.EqualTo(310f));
            Assert.That(source.GetCellViewOffset(1), Is.EqualTo(50f));
            Assert.That(source.GetCellViewOffset(2), Is.EqualTo(210f));
            Assert.That(source.FindDataIndex(209f), Is.EqualTo(1));
            Assert.That(source.FindDataIndex(210f), Is.EqualTo(2));
        }

        [Test]
        public void CollectionChangesKeepSizesAndOffsetsAligned()
        {
            var source = new Source();
            source.AddRange(new[] { 10, 20, 30 });
            source.GetTotalContentSize(default, 100f, 0f);
            source.SetCellViewSize(1, 200f);
            source.Insert(0, 5);
            source.RemoveAt(1);
            Assert.That(source.GetTotalContentSize(default, 100f, 0f), Is.EqualTo(400f));
            Assert.That(source[1], Is.EqualTo(20));
            Assert.That(source.GetCellViewSize(1), Is.EqualTo(200f));
            source[1] = 21;
            Assert.That(source.GetTotalContentSize(default, 100f, 0f), Is.EqualTo(300f));
            source.Clear();
            Assert.That(source.GetDataCount(), Is.Zero);
            Assert.That(source.GetTotalContentSize(default, 100f, 0f), Is.Zero);
        }
    }
}
