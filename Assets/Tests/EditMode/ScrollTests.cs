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

        private sealed class MeasuredSource : LazyLayoutDataSource<int>, ILazyLayoutDataSource
        {
            public GameObject Prefab;
            public int RecalculationCount;
            public float[] ActualSizes;
            public readonly Dictionary<int, GameObject> Cells = new();
            public override GameObject GetCellView(int index) => Prefab;
            public override int GetCellViewType(int index) => 0;
            public override void SetData(int index, GameObject cell)
            {
                Cells[index] = cell;
                if (ActualSizes != null)
                    ((RectTransform)cell.transform).sizeDelta = Vector2.one * ActualSizes[index];
            }
            float ILazyLayoutDataSource.GetTotalContentSize(ContentPadding padding, float estimate, float space)
            {
                RecalculationCount++;
                return GetTotalContentSize(padding, estimate, space);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        public void LazyLayout_RecalculatesOffsetsOnceForAMeasurementBatch(int axis)
        {
            var scroll = Create<LazyLayoutListScroll>(axis);
            ((RectTransform)_prefab.transform).sizeDelta = new Vector2(40f, 40f);
            var source = new MeasuredSource { Prefab = _prefab };
            source.AddRange(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            scroll.SetDataSource(source);
            Tick(scroll);
            Assert.That(source.Cells.Count, Is.EqualTo(8));
            // One calculation initializes the estimates; one incorporates all measured sizes.
            Assert.That(source.RecalculationCount, Is.EqualTo(2));
            Tick(scroll);
            Assert.That(source.RecalculationCount, Is.EqualTo(2));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void LazyLayout_RecalculatesWhenSizeChangesCancelEachOther(int axis)
        {
            var scroll = Create<LazyLayoutListScroll>(axis);
            var source = new MeasuredSource { Prefab = _prefab, ActualSizes = new[] { 150f, 50f, 100f, 100f } };
            source.AddRange(new[] { 0, 1, 2, 3 });
            scroll.SetDataSource(source);
            Tick(scroll);
            Assert.That(source.GetCellViewOffset(1), Is.EqualTo(150f));
            Assert.That(source.GetCellViewOffset(2), Is.EqualTo(200f));
            Assert.That(source.RecalculationCount, Is.EqualTo(2));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void LazyLayout_PrependMeasurementKeepsContentAndScrollPositionConsistent(int axis)
        {
            var scroll = Create<LazyLayoutListScroll>(axis);
            var source = new MeasuredSource { Prefab = _prefab, ActualSizes = new float[20] };
            for (var i = 0; i < 20; i++) { source.Add(i); source.ActualSizes[i] = 100f; }
            scroll.SetDataSource(source);
            Tick(scroll);
            scroll.Refresh(450f / 1700f, false);
            Tick(scroll);
            Tick(scroll);
            var oldScreenEnd = source.GetCellViewOffset(7) + source.GetCellViewSize(7) -
                               (axis == 0 ? -scroll.ScrollPosition : scroll.ScrollPosition);
            source.ActualSizes[2] = source.ActualSizes[3] = 40f;
            for (var i = 4; i <= 7; i++)
                ((RectTransform)source.Cells[i].transform).sizeDelta = new Vector2(40f, 40f);
            typeof(LazyLayoutListScroll).GetMethod("Reposition", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(scroll, new object[] { axis == 0 ? 10f : -10f, false });
            Assert.That(source.GetCellViewSize(3), Is.EqualTo(40f));
            Assert.That(source.GetCellViewSize(2), Is.EqualTo(40f));
            Assert.That(source.GetCellViewOffset(7) + source.GetCellViewSize(7) -
                        (axis == 0 ? -scroll.ScrollPosition : scroll.ScrollPosition),
                Is.EqualTo(oldScreenEnd).Within(0.001f));
            Assert.That(_content.localPosition[axis], Is.EqualTo(scroll.ScrollPosition).Within(0.001f));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void LazyLayout_LargerCellsAreMeasuredAndPositionedWithoutOverlap(int axis)
        {
            var scroll = Create<LazyLayoutListScroll>(axis);
            var source = new MeasuredSource { Prefab = _prefab, ActualSizes = new[] { 180f, 140f, 220f, 100f } };
            source.AddRange(new[] { 0, 1, 2, 3 });
            scroll.SetDataSource(source);
            Tick(scroll);
            Assert.That(source.GetCellViewOffset(1), Is.EqualTo(180f));
            Assert.That(source.GetCellViewOffset(2), Is.EqualTo(320f));
            var direction = axis == 0 ? -1f : 1f;
            Assert.That(source.Cells[0].transform.localPosition[axis], Is.EqualTo(60f * direction));
            Assert.That(source.Cells[1].transform.localPosition[axis], Is.EqualTo(-100f * direction));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void LazyLayout_ResizeRepositionsMeasuredCells(int axis)
        {
            var scroll = Create<LazyLayoutListScroll>(axis);
            var source = new MeasuredSource { Prefab = _prefab };
            source.AddRange(new[] { 0, 1, 2, 3, 4, 5 });
            scroll.SetDataSource(source);
            Tick(scroll);
            ((RectTransform)_root.transform).sizeDelta = new Vector2(500f, 500f);
            scroll.Refresh();
            Tick(scroll);
            Assert.That(source.Cells.ContainsKey(4), Is.True);
            Assert.That(source.Cells[0].transform.localPosition[axis],
                Is.EqualTo(axis == 0 ? -200f : 200f).Within(0.001f));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void LazyLayout_ReverseScrollKeepsVisibleTrailingEdgeStableWhenSizesChange(int axis)
        {
            var scroll = Create<LazyLayoutListScroll>(axis);
            var source = new MeasuredSource { Prefab = _prefab };
            source.AddRange(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            scroll.SetDataSource(source);
            Tick(scroll);
            scroll.Refresh(0.5f, false);
            Tick(scroll);
            Tick(scroll);
            var start = source.FindDataIndex(axis == 0 ? -scroll.ScrollPosition : scroll.ScrollPosition);
            var last = start + 2;
            var oldTrailingOffset = source.GetCellViewOffset(last) + source.GetCellViewSize(last);
            var oldScreenOffset = oldTrailingOffset - (axis == 0 ? -scroll.ScrollPosition : scroll.ScrollPosition);
            for (var i = start; i <= last; i++)
                ((RectTransform)source.Cells[i].transform).sizeDelta = new Vector2(150f, 150f);
            typeof(LazyLayoutListScroll).GetMethod("Reposition", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(scroll, new object[] { axis == 0 ? 10f : -10f, false });
            var newTrailingOffset = source.GetCellViewOffset(last) + source.GetCellViewSize(last);
            Assert.That(newTrailingOffset - (axis == 0 ? -scroll.ScrollPosition : scroll.ScrollPosition),
                Is.EqualTo(oldScreenOffset).Within(0.001f));
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
