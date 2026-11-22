using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SimpleScroll.Tests
{
    public class CarouselTests
    {
        private GameObject _root;
        private GameObject _prefab;
        private RectTransform _content;

        private sealed class Source : IDataSource
        {
            public GameObject Prefab;
            public int Count = 10;
            public int GetDataCount() => Count;
            public GameObject GetCellView(int index) => Prefab;
            public int GetCellViewType(int index) => 0;
            public void SetData(int index, GameObject cell) { }
        }

        private CarouselScroll Create(int axis, bool loop)
        {
            _root = new GameObject("Carousel test", typeof(RectTransform));
            _root.SetActive(false);
            var viewport = (RectTransform)_root.transform;
            viewport.sizeDelta = new Vector2(300, 300);
            _content = (RectTransform)new GameObject("Content", typeof(RectTransform)).transform;
            _content.SetParent(viewport, false);
            _prefab = new GameObject("Cell", typeof(RectTransform));
            ((RectTransform)_prefab.transform).sizeDelta = new Vector2(100, 100);
            var scroll = _root.AddComponent<CarouselScroll>();
            var baseType = typeof(BaseScroll<IDataSource>);
            baseType.GetField("_viewport", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scroll, viewport);
            baseType.GetField("_content", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scroll, _content);
            var scroller = baseType.GetProperty("Scroller", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scroll);
            scroller.GetType().GetField("_axis", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(scroller, (RectTransform.Axis)axis);
            scroll.Loop = loop;
            scroll.SetDataSource(new Source { Prefab = _prefab });
            _root.SetActive(true);
            scroll.Refresh();
            return scroll;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_root);
            Object.Destroy(_prefab);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ImmediateNavigationRemainsAtTargetOnBothAxes()
        {
            foreach (var axis in new[] { 0, 1 })
            {
                var scroll = Create(axis, true);
                yield return null;
                scroll.SetPositionIndex(3, false);
                var expected = axis == 0 ? -300f : 300f;
                Assert.That(scroll.ScrollPosition, Is.EqualTo(expected).Within(0.001f));
                yield return null;
                yield return null;
                Assert.That(scroll.ScrollPosition, Is.EqualTo(expected).Within(0.001f));
                Assert.That(scroll.IsIdling, Is.True);
                Assert.That(_content.localPosition[axis], Is.EqualTo(expected).Within(0.001f));
                Object.Destroy(_root);
                Object.Destroy(_prefab);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator StopPreservesMidCellPositionAndSnappingIsExplicit()
        {
            foreach (var axis in new[] { 0, 1 })
            {
                var scroll = Create(axis, false);
                yield return null;
                scroll.SetPositionIndex(3, false);
                yield return null;
                scroll.SetNormalizedPosition(340f / 900f);
                scroll.Stop();
                var expected = axis == 0 ? -340f : 340f;
                for (var frame = 0; frame < 5; frame++)
                {
                    yield return null;
                    Assert.That(scroll.ScrollPosition, Is.EqualTo(expected).Within(0.001f));
                    Assert.That(scroll.IsIdling, Is.True);
                }
                // The nearest index is already selected: snapping must still move to its center.
                scroll.SnapToNearest();
                var deadline = Time.realtimeSinceStartup + 3f;
                while (!scroll.IsIdling && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(scroll.IsIdling, Is.True);
                Assert.That(scroll.ScrollPosition, Is.EqualTo(axis == 0 ? -300f : 300f).Within(0.001f));
                Object.Destroy(_root);
                Object.Destroy(_prefab);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator LegacyStopScrollRetainsCarouselSnapBehavior()
        {
            var scroll = Create(1, false);
            yield return null;
            scroll.SetPositionIndex(3, false);
            yield return null;
            scroll.SetNormalizedPosition(340f / 900f);
#pragma warning disable CS0618 // Verify legacy snapping even when the selected index is unchanged.
            scroll.StopScroll();
#pragma warning restore CS0618
            var deadline = Time.realtimeSinceStartup + 3f;
            while (!scroll.IsIdling && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(scroll.IsIdling, Is.True);
            Assert.That(scroll.ScrollPosition, Is.EqualTo(300f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator NonLoopingNavigationClampsAndLoopingAllowsNegativePositions()
        {
            var scroll = Create(1, false);
            yield return null;
            scroll.SetPositionIndex(100, false);
            yield return null;
            Assert.That(scroll.ScrollPosition, Is.EqualTo(900f).Within(0.001f));
            scroll.SetPositionIndex(-1, false);
            yield return null;
            Assert.That(scroll.ScrollPosition, Is.Zero);
            scroll.Loop = true;
            scroll.Refresh();
            scroll.SetPositionIndex(-2, false);
            yield return null;
            Assert.That(scroll.ScrollPosition, Is.EqualTo(-200f).Within(0.001f));
        }
    }
}
