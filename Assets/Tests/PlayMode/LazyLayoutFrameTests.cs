using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SimpleScroll.Tests
{
    public class LazyLayoutFrameTests
    {
        private GameObject _root;
        private GameObject _prefab;

        private sealed class Source : LazyLayoutDataSource<int>
        {
            public GameObject Prefab;
            public readonly Dictionary<int, RectTransform> Cells = new();
            public override int GetCellViewType(int index) => 0;
            public override GameObject GetCellView(int index) => Prefab;
            public override void SetData(int index, GameObject cell)
            {
                var rect = (RectTransform)cell.transform;
                rect.sizeDelta = new Vector2(40f, 40f);
                Cells[index] = rect;
            }
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_root);
            Object.Destroy(_prefab);
            yield return null;
        }

        [UnityTest]
        public IEnumerator NavigationAndResizeKeepTheMeasuredCellCenteredOnBothAxes()
        {
            foreach (var axis in new[] { 0, 1 })
            {
                _root = new GameObject("Lazy layout frame test", typeof(RectTransform));
                _root.SetActive(false);
                var viewport = (RectTransform)_root.transform;
                viewport.sizeDelta = new Vector2(300f, 300f);
                var content = (RectTransform)new GameObject("Content", typeof(RectTransform)).transform;
                content.SetParent(viewport, false);
                _prefab = new GameObject("Cell", typeof(RectTransform));
                var scroll = _root.AddComponent<LazyLayoutListScroll>();
                var baseType = typeof(BaseScroll<ILazyLayoutDataSource>);
                baseType.GetField("_viewport", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scroll, viewport);
                baseType.GetField("_content", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(scroll, content);
                var scroller = baseType.GetProperty("Scroller", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scroll);
                scroller.GetType().GetField("_axis", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(scroller, (RectTransform.Axis)axis);
                var source = new Source { Prefab = _prefab };
                for (var i = 0; i < 30; i++) source.Add(i);
                scroll.SetDataSource(source);
                _root.SetActive(true);
                scroll.Refresh();
                yield return null;
                scroll.SetPositionIndex(12, 0.5f, false);
                for (var frame = 0; frame < 8; frame++) yield return null;
                Assert.That(source.Cells[12].gameObject.activeSelf, Is.True);
                Assert.That(source.Cells[12].localPosition[axis] + content.localPosition[axis],
                    Is.EqualTo(0f).Within(0.01f));
                Assert.That(content.localPosition[axis], Is.EqualTo(scroll.ScrollPosition).Within(0.01f));
                viewport.sizeDelta = new Vector2(500f, 500f);
                scroll.Refresh();
                scroll.SetPositionIndex(12, 0.5f, false);
                for (var frame = 0; frame < 8; frame++) yield return null;
                Assert.That(source.Cells[12].localPosition[axis] + content.localPosition[axis],
                    Is.EqualTo(0f).Within(0.01f));
                Object.Destroy(_root);
                Object.Destroy(_prefab);
                yield return null;
            }
        }
    }
}
