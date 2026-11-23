using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SimpleScroll.Tests
{
    public class ScrollTests
    {
        private GameObject _root;
        private GameObject _prefab;
        private RectTransform _content;

        private class Source : ISizedDataSource
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

        private sealed class LazySource : Source, ILazyLayoutDataSource
        {
            public float GetTotalContentSize(ContentPadding padding, float estimatedCellSize, float space)
                => Count * 100f;
            public int FindDataIndex(float position) => Mathf.Clamp(Mathf.FloorToInt(position / 100f), 0, Count - 1);
            public float GetCellViewOffset(int index)
            {
                Assert.That(index, Is.InRange(0, Count - 1));
                return index * 100f;
            }
            public void SetCellViewSize(int index, float size)
            {
                Assert.That(index, Is.InRange(0, Count - 1));
            }
        }

        [TestCase(typeof(FixedListScroll), 0)]
        [TestCase(typeof(FixedListScroll), 1)]
        [TestCase(typeof(FixedGridScroll), 0)]
        [TestCase(typeof(FixedGridScroll), 1)]
        [TestCase(typeof(SizedListScroll), 0)]
        [TestCase(typeof(SizedListScroll), 1)]
        [TestCase(typeof(LazyLayoutListScroll), 0)]
        [TestCase(typeof(LazyLayoutListScroll), 1)]
        public void StopScroll_CancelsPendingTarget(System.Type scrollType, int axis)
        {
            var scroll = (Component)typeof(ScrollTests).GetMethod(nameof(Create), BindingFlags.Instance | BindingFlags.NonPublic)
                .MakeGenericMethod(scrollType).Invoke(this, new object[] { axis });
            Source source = scrollType == typeof(LazyLayoutListScroll) ? new LazySource() : new Source();
            source.Prefab = _prefab;
            scrollType.GetMethod("SetDataSource").Invoke(scroll, new object[] { source });
            scrollType.GetMethod("Refresh", new[] { typeof(bool) }).Invoke(scroll, new object[] { true });
            scrollType.GetMethod("SetPositionIndex").Invoke(scroll, new object[] { 10, 0.5f, true });
            scrollType.GetMethod("Stop").Invoke(scroll, null);
            var position = (float)scrollType.GetProperty("ScrollPosition").GetValue(scroll);
            for (var i = 0; i < 3; i++)
                scrollType.GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(scroll, null);
            Assert.That(scrollType.GetProperty("ScrollPosition").GetValue(scroll), Is.EqualTo(position));
            Assert.That(scrollType.GetProperty("IsIdling").GetValue(scroll), Is.True);
        }

        [TestCase(0, 0, 1f)]
        [TestCase(0, 9, 0f)]
        [TestCase(1, 0, 1f)]
        [TestCase(1, 9, 0f)]
        public void LazyLayout_ClampedTargetFinishes(int axis, int index, float anchor)
        {
            var scroll = Create<LazyLayoutListScroll>(axis);
            scroll.SetDataSource(new LazySource { Prefab = _prefab, Count = 10 });
            Tick(scroll);
            scroll.SetPositionIndex(index, anchor, false);
            Tick(scroll);
            Tick(scroll);
            Assert.That(typeof(LazyLayoutListScroll).GetField("_targetIndex", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(scroll), Is.EqualTo(-1));
            Assert.That(scroll.ScrollPosition, Is.EqualTo(index == 0 ? 0f : (axis == 0 ? -700f : 700f)));
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        public void DirectNormalizedRequestRespectsListenerInterruption(int axis, bool replace)
        {
            var scroll = Create<LazyLayoutListScroll>(axis);
            scroll.SetDataSource(new LazySource { Prefab = _prefab });
            Tick(scroll);
            var changed = new ScrollEvent();
            var scroller = typeof(BaseScroll<ILazyLayoutDataSource>).GetProperty("Scroller", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scroll);
            scroller.GetType().GetField("_onValueChanged", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scroller, changed);
            var handled = false;
            changed.AddListener(value =>
            {
                if (handled) return;
                handled = true;
                if (replace) scroll.SetNormalizedPosition(0.25f);
                else scroll.Stop();
            });
            scroll.SetNormalizedPosition(0.5f);
            Assert.That(handled, Is.True);
            var pending = typeof(BaseScroll<ILazyLayoutDataSource>).GetField("_normalizedPosition", BindingFlags.Instance | BindingFlags.NonPublic);
            if (replace) Assert.That(pending.GetValue(scroll), Is.EqualTo(0.25f));
            else Assert.That(pending.GetValue(scroll), Is.NaN);
            for (var frame = 0; frame < 3; frame++) Tick(scroll);
            Assert.That(scroll.NormalizedPosition, Is.EqualTo(replace ? 0.25f : 0.5f).Within(0.001f));
            Assert.That(pending.GetValue(scroll), Is.NaN);
        }

        [TestCase(0, 3)]
        [TestCase(0, 0)]
        [TestCase(1, 3)]
        [TestCase(1, 0)]
        public void LazyLayout_DataShrinkCancelsInvalidTarget(int axis, int count)
        {
            var scroll = Create<LazyLayoutListScroll>(axis);
            var source = new LazySource { Prefab = _prefab, Count = 10 };
            scroll.SetDataSource(source);
            Tick(scroll);
            scroll.SetPositionIndex(9);
            source.Count = count;
            Assert.DoesNotThrow(() => Tick(scroll));
            Assert.That(typeof(LazyLayoutListScroll).GetField("_targetIndex", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(scroll), Is.EqualTo(-1));
        }

        [TestCase(0, -1000f, 0f)]
        [TestCase(1, 1000f, 0f)]
        [TestCase(0, -1000f, -9700f)]
        [TestCase(1, 1000f, 9700f)]
        public void PositionEvent_ReportsClampedValueOnlyWhenPositionChanges(int axis, float start, float expected)
        {
            var scroll = Create<FixedListScroll>(axis);
            scroll.SetDataSource(new Source { Prefab = _prefab });
            scroll.Refresh();
            var scroller = typeof(BaseScroll<IDataSource>).GetProperty("Scroller", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scroll);
            var type = scroller.GetType();
            type.GetField("_overScroll", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scroller, false);
            var position = type.GetProperty("ScrollPosition", BindingFlags.Instance | BindingFlags.NonPublic);
            position.SetValue(scroller, start);
            var change = new ScrollEvent();
            type.GetField("_onValueChanged", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scroller, change);
            var values = new List<float>();
            change.AddListener(values.Add);
            position.SetValue(scroller, expected == 0f ? -start : expected * 2f);
            position.SetValue(scroller, expected == 0f ? -start * 2f : expected * 3f);
            Assert.That(values, Is.EqualTo(new[] { expected }));
            Assert.That(scroll.ScrollPosition, Is.EqualTo(expected));
        }

        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(0, 1)]
        [TestCase(1, 1)]
        [TestCase(0, 2)]
        [TestCase(1, 2)]
        [TestCase(0, 3)]
        [TestCase(1, 3)]
        public void MeasurementNotificationDoesNotRestoreCanceledOrReplacedRequest(int axis, int action)
        {
            var scroll = Create<LazyLayoutListScroll>(axis);
            ((RectTransform)_prefab.transform).sizeDelta = new Vector2(40f, 40f);
            var source = new MeasuredSource { Prefab = _prefab };
            source.AddRange(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            scroll.SetDataSource(source);
            scroll.Refresh(1f);
            var changed = new ScrollEvent();
            var scroller = typeof(BaseScroll<ILazyLayoutDataSource>).GetProperty("Scroller", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scroll);
            scroller.GetType().GetField("_onValueChanged", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scroller, changed);
            var handled = false;
            var direction = axis == 0 ? -1f : 1f;
            changed.AddListener(value =>
            {
                if (handled || Mathf.Abs(value * direction - 520f) > 0.001f) return;
                handled = true;
                if (action == 0) scroll.Stop();
                else if (action == 1) scroll.SetNormalizedPosition(0.5f);
                else if (action == 2) scroll.SetNormalizedPosition(1f);
                else scroll.SetPositionIndex(9, smooth: false);
            });
            Tick(scroll);
            Assert.That(handled, Is.True, "The listener must run during the first measured position correction.");
            var pending = typeof(BaseScroll<ILazyLayoutDataSource>).GetField("_normalizedPosition", BindingFlags.Instance | BindingFlags.NonPublic);
            if (action == 0 || action == 3) Assert.That(pending.GetValue(scroll), Is.NaN);
            else Assert.That(pending.GetValue(scroll), Is.EqualTo(action == 1 ? 0.5f : 1f));
            for (var frame = 0; frame < 20; frame++) Tick(scroll);
            Assert.That(pending.GetValue(scroll), Is.NaN);
            if (action == 0) Assert.That(scroll.ScrollPosition * direction, Is.EqualTo(520f).Within(0.001f));
            else if (action == 1 || action == 2)
            {
                var extent = Mathf.Max(0f, source.GetTotalContentSize(default, 100f, 0f) - 300f);
                Assert.That(scroll.ScrollPosition * direction, Is.EqualTo(extent * (action == 1 ? 0.5f : 1f)).Within(0.001f));
            }
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        public void LazyLayout_MeasuredSourceCanShrinkAfterNavigation(int axis, bool clear)
        {
            var scroll = Create<LazyLayoutListScroll>(axis);
            var source = new MeasuredSource { Prefab = _prefab };
            source.AddRange(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            scroll.SetDataSource(source);
            Tick(scroll);
            scroll.SetPositionIndex(9, smooth: false);
            Tick(scroll);
            Assert.That(Mathf.Abs(scroll.ScrollPosition), Is.GreaterThan(0f));
            if (clear) source.Clear();
            else for (var i = 9; i >= 5; i--) source.RemoveAt(i);
            Assert.DoesNotThrow(() => Tick(scroll));
            Assert.DoesNotThrow(() => Tick(scroll));
            Assert.That(typeof(LazyLayoutListScroll).GetField("_targetIndex", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(scroll), Is.EqualTo(-1));
            Assert.That(Mathf.Abs(scroll.ScrollPosition), Is.LessThanOrEqualTo(clear ? 0f : 200f));
            if (clear) Assert.That(scroll.VisibleRange, Is.EqualTo(Range.Empty));
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        public void PositionEvent_AllowsUnclampedPositionAndIgnoresIdenticalAssignments(int axis, bool infinite)
        {
            var scroll = Create<FixedListScroll>(axis);
            var scroller = typeof(BaseScroll<IDataSource>).GetProperty("Scroller", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scroll);
            var type = scroller.GetType();
            type.GetField("_overScroll", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scroller, !infinite);
            type.GetProperty("ScrollSize", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(scroller, infinite ? float.PositiveInfinity : 100f);
            var change = new ScrollEvent();
            type.GetField("_onValueChanged", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scroller, change);
            var values = new List<float>();
            change.AddListener(value =>
            {
                Assert.That(scroll.ScrollPosition, Is.EqualTo(value), "Stored position must be updated before notification.");
                values.Add(value);
            });
            var position = type.GetProperty("ScrollPosition", BindingFlags.Instance | BindingFlags.NonPublic);
            var expected = axis == 0 ? -200f : 200f;
            position.SetValue(scroller, expected);
            position.SetValue(scroller, expected);
            position.SetValue(scroller, 0f);
            position.SetValue(scroller, 0f);
            Assert.That(values, Is.EqualTo(new[] { expected, 0f }));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void LazyLayout_BeginDragCancelsPendingNavigation(int axis)
        {
            var scroll = Create<LazyLayoutListScroll>(axis);
            scroll.SetDataSource(new LazySource { Prefab = _prefab });
            Tick(scroll);
            scroll.SetPositionIndex(50);
            var pointer = new PointerEventData(null) { pointerDrag = _root, pointerId = 1 };
            ((IBeginDragHandler)scroll).OnBeginDrag(pointer);
            Tick(scroll);
            Assert.That(scroll.IsDragging, Is.True);
            Assert.That(scroll.ScrollPosition, Is.EqualTo(0f));
            Assert.That(typeof(LazyLayoutListScroll).GetField("_targetIndex", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(scroll), Is.EqualTo(-1));
            ((IEndDragHandler)scroll).OnEndDrag(pointer);
            Tick(scroll);
            Assert.That(scroll.ScrollPosition, Is.EqualTo(0f));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void NormalizedPositionCancelsPointerDragAndIgnoresItsRemainingEvents(int axis)
        {
            var scroll = Create<FixedListScroll>(axis);
            scroll.SetDataSource(new Source { Prefab = _prefab });
            Tick(scroll);
            var pointer = new PointerEventData(null) { pointerDrag = _root, pointerId = 1 };
            ((IBeginDragHandler)scroll).OnBeginDrag(pointer);
            Assert.That(scroll.IsDragging, Is.True);
            scroll.SetNormalizedPosition(0.5f);
            Assert.That(scroll.IsIdling, Is.True);
            var position = scroll.ScrollPosition;
            pointer.position = new Vector2(50f, 50f);
            ((IDragHandler)scroll).OnDrag(pointer);
            ((IEndDragHandler)scroll).OnEndDrag(pointer);
            Tick(scroll);
            Assert.That(scroll.IsDragging, Is.False);
            Assert.That(scroll.IsIdling, Is.True);
            Assert.That(scroll.ScrollPosition, Is.EqualTo(position));
            pointer.pointerId = 2;
            ((IBeginDragHandler)scroll).OnBeginDrag(pointer);
            Assert.That(scroll.IsDragging, Is.True, "A new gesture must still be accepted.");
            ((IEndDragHandler)scroll).OnEndDrag(pointer);
            Assert.That(scroll.IsDragging, Is.False);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void NormalizedPositionPreservesScrollbarDragUntilHandlingEnds(int axis)
        {
            var scroll = Create<FixedListScroll>(axis);
            scroll.SetDataSource(new Source { Prefab = _prefab });
            Tick(scroll);
            var listener = typeof(BaseScroll<IDataSource>).GetInterface("SimpleScroll.IScrollbarEventListener");
            var handling = listener.GetMethod("OnScrollbarHandling");
            var valueChanged = listener.GetMethod("OnScrollbarValueChanged");
            handling.Invoke(scroll, new object[] { true });
            valueChanged.Invoke(scroll, new object[] { 0.25f });
            Assert.That(scroll.IsDragging, Is.True);
            Assert.That(scroll.NormalizedPosition, Is.EqualTo(0.25f).Within(0.001f));
            valueChanged.Invoke(scroll, new object[] { 0.75f });
            Assert.That(scroll.IsDragging, Is.True);
            handling.Invoke(scroll, new object[] { false });
            Tick(scroll);
            Assert.That(scroll.IsIdling, Is.True);
            Assert.That(scroll.NormalizedPosition, Is.EqualTo(0.75f).Within(0.001f));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void LazyLayout_NormalizedPositionReplacesPendingNavigation(int axis)
        {
            var scroll = Create<LazyLayoutListScroll>(axis);
            scroll.SetDataSource(new LazySource { Prefab = _prefab });
            Tick(scroll);
            scroll.SetPositionIndex(50);
            scroll.SetNormalizedPosition(0.25f);
            Tick(scroll);
            Tick(scroll);
            Assert.That(scroll.NormalizedPosition, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(scroll.IsIdling, Is.True);
        }

        [TestCase(typeof(FixedListScroll), 0)]
        [TestCase(typeof(FixedListScroll), 1)]
        [TestCase(typeof(FixedGridScroll), 0)]
        [TestCase(typeof(FixedGridScroll), 1)]
        [TestCase(typeof(SizedListScroll), 0)]
        [TestCase(typeof(SizedListScroll), 1)]
        [TestCase(typeof(LazyLayoutListScroll), 0)]
        [TestCase(typeof(LazyLayoutListScroll), 1)]
        public void NormalizedNavigationDoesNotRebindAndCancelsMovement(System.Type scrollType, int axis)
        {
            var scroll = (Component)typeof(ScrollTests).GetMethod(nameof(Create), BindingFlags.Instance | BindingFlags.NonPublic)
                .MakeGenericMethod(scrollType).Invoke(this, new object[] { axis });
            Source source = scrollType == typeof(LazyLayoutListScroll) ? new LazySource() : new Source();
            source.Prefab = _prefab;
            scrollType.GetMethod("SetDataSource").Invoke(scroll, new object[] { source });
            scrollType.GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(scroll, null);
            scrollType.GetMethod("SetPositionIndex").Invoke(scroll, new object[] { 50, 0.5f, true });
            var binds = source.BindCount;
            scrollType.GetMethod("SetNormalizedPosition").Invoke(scroll, new object[] { 0.25f });
            Assert.That(source.BindCount, Is.EqualTo(binds));
            for (var i = 0; i < 3; i++)
                scrollType.GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(scroll, null);
            Assert.That(scrollType.GetProperty("NormalizedPosition").GetValue(scroll), Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(scrollType.GetProperty("IsIdling").GetValue(scroll), Is.True);
        }

        [TestCase(0, true, 0f)]
        [TestCase(1, true, 0f)]
        [TestCase(0, true, 0.5f)]
        [TestCase(1, true, 0.5f)]
        [TestCase(0, true, 1f)]
        [TestCase(1, true, 1f)]
        [TestCase(0, false, 0f)]
        [TestCase(1, false, 0f)]
        [TestCase(0, false, 0.5f)]
        [TestCase(1, false, 0.5f)]
        [TestCase(0, false, 1f)]
        [TestCase(1, false, 1f)]
        public void RefreshAppliesNormalizedPositionToMeasuredLayout(int axis, bool overScroll, float normalizedPosition)
        {
            var scroll = Create<LazyLayoutListScroll>(axis);
            var scroller = typeof(BaseScroll<ILazyLayoutDataSource>).GetProperty("Scroller", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(scroll);
            scroller.GetType().GetField("_overScroll", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scroller, overScroll);
            ((RectTransform)_prefab.transform).sizeDelta = new Vector2(40f, 40f);
            var source = new MeasuredSource { Prefab = _prefab };
            source.AddRange(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            scroll.SetDataSource(source);
            scroll.Refresh(normalizedPosition);
            for (var frame = 0; frame < 20; frame++) Tick(scroll);
            var extent = Mathf.Max(0f, source.GetTotalContentSize(default, 100f, 0f) - 300f);
            var direction = axis == 0 ? -1f : 1f;
            var actualOffset = scroll.ScrollPosition * direction;
            Assert.That(actualOffset, Is.InRange(0f, extent));
            Assert.That(actualOffset, Is.EqualTo(extent * normalizedPosition).Within(0.001f));
            Assert.That(_content.localPosition[axis], Is.EqualTo(scroll.ScrollPosition).Within(0.001f));
            Assert.That(typeof(BaseScroll<ILazyLayoutDataSource>).GetField("_normalizedPosition", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(scroll), Is.NaN, "The pending destination should clear after measurement stabilizes.");
            if (normalizedPosition == 1f)
            {
                var lastCell = (RectTransform)source.Cells[9].transform;
                var trailingEdge = lastCell.localPosition[axis] + _content.localPosition[axis] - direction * lastCell.rect.size[axis] * 0.5f;
                Assert.That(trailingEdge, Is.EqualTo(-direction * 150f).Within(0.001f), "The measured end must meet the viewport edge.");
            }
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        public void MeasuredNormalizedDestinationCanBeCanceled(int axis, bool navigateToCell)
        {
            var scroll = Create<LazyLayoutListScroll>(axis);
            ((RectTransform)_prefab.transform).sizeDelta = new Vector2(40f, 40f);
            var source = new MeasuredSource { Prefab = _prefab };
            source.AddRange(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            scroll.SetDataSource(source);
            scroll.Refresh(1f);
            Tick(scroll);
            var pending = typeof(BaseScroll<ILazyLayoutDataSource>).GetField("_normalizedPosition", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(pending.GetValue(scroll), Is.EqualTo(1f));
            if (navigateToCell) scroll.SetPositionIndex(9, smooth: false);
            else scroll.Stop();
            Assert.That(pending.GetValue(scroll), Is.NaN);
            for (var frame = 0; frame < 20; frame++) Tick(scroll);
            Assert.That(pending.GetValue(scroll), Is.NaN);
        }

        [TestCase(false, 0, false)]
        [TestCase(false, 1, false)]
        [TestCase(false, 0, true)]
        [TestCase(false, 1, true)]
        [TestCase(true, 0, false)]
        [TestCase(true, 1, false)]
        [TestCase(true, 0, true)]
        [TestCase(true, 1, true)]
        public void RefreshAtEndDoesNotBindCellsAtPreviousPosition(bool lazy, int axis, bool rebind)
        {
            var scrollType = lazy ? typeof(LazyLayoutListScroll) : typeof(FixedListScroll);
            var scroll = (Component)typeof(ScrollTests).GetMethod(nameof(Create), BindingFlags.Instance | BindingFlags.NonPublic)
                .MakeGenericMethod(scrollType).Invoke(this, new object[] { axis });
            Source source = lazy ? new LazySource() : new Source();
            source.Prefab = _prefab;
            source.Count = 10;
            scrollType.GetMethod("SetDataSource").Invoke(scroll, new object[] { source });
            var tick = scrollType.GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            tick.Invoke(scroll, null);
            source.Count = 20;
            source.Cells.Clear();
            var binds = source.BindCount;
            scrollType.GetMethod("Refresh", new[] { typeof(float), typeof(bool) })
                .Invoke(scroll, new object[] { 1f, rebind });
            Assert.That(source.BindCount, Is.EqualTo(binds), "Refresh must choose the destination before any cell binding.");
            tick.Invoke(scroll, null);
            Assert.That(source.Cells.Count, Is.GreaterThan(0));
            foreach (var index in source.Cells.Keys)
                Assert.That(index, Is.GreaterThanOrEqualTo(17), "Only cells at the requested destination should be bound.");
            Assert.That(scrollType.GetProperty("NormalizedPosition").GetValue(scroll), Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void LegacyStopScrollStillStopsListNavigation()
        {
            var scroll = Create<FixedListScroll>(1);
            scroll.SetDataSource(new Source { Prefab = _prefab });
            Tick(scroll);
            scroll.SetPositionIndex(50);
#pragma warning disable CS0618 // Verify the supported legacy entry point.
            scroll.StopScroll();
#pragma warning restore CS0618
            Tick(scroll);
            Assert.That(scroll.IsIdling, Is.True);
            Assert.That(scroll.ScrollPosition, Is.EqualTo(0f));
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
            typeof(BaseScroll<T>).GetMethod("UpdatePosition", BindingFlags.Instance | BindingFlags.NonPublic,
                    null, System.Type.EmptyTypes, null)
                .Invoke(scroll, null);
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            if (_prefab != null) Object.DestroyImmediate(_prefab);
        }

        [TestCase(typeof(FixedListScroll), 0)]
        [TestCase(typeof(FixedListScroll), 1)]
        [TestCase(typeof(FixedGridScroll), 0)]
        [TestCase(typeof(FixedGridScroll), 1)]
        [TestCase(typeof(SizedListScroll), 0)]
        [TestCase(typeof(SizedListScroll), 1)]
        public void IndexNavigationBeforeFirstUpdateCentersRequestedCell(System.Type scrollType, int axis)
        {
            var scroll = (Component)typeof(ScrollTests).GetMethod(nameof(Create), BindingFlags.Instance | BindingFlags.NonPublic)
                .MakeGenericMethod(scrollType).Invoke(this, new object[] { axis });
            var source = new Source { Prefab = _prefab };
            scrollType.GetMethod("SetDataSource").Invoke(scroll, new object[] { source });
            scrollType.GetMethod("SetPositionIndex").Invoke(scroll, new object[] { 50, 0.5f, false });
            Assert.That(source.BindCount, Is.EqualTo(0), "Layout initialization must not bind cells at the old position.");
            scrollType.GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(scroll, null);
            Assert.That(source.Cells[50].transform.localPosition[axis] + _content.localPosition[axis], Is.EqualTo(0f).Within(0.001f));
            Assert.That(scrollType.GetProperty("IsIdling").GetValue(scroll), Is.True);
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
