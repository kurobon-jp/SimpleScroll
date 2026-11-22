using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SimpleScroll.Tests
{
    public class ScrollNavigationTests
    {
        private GameObject _root;
        private GameObject _prefab;
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private sealed class Source : LazyLayoutDataSource<int>
        {
            public GameObject Prefab;
            public override GameObject GetCellView(int index) => Prefab;
            public override int GetCellViewType(int index) => 0;
            public override void SetData(int index, GameObject cell)
                => Assert.That(index, Is.InRange(0, GetDataCount() - 1));
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(_root);
            Object.Destroy(_prefab);
            yield return null;
        }

        [UnityTest]
        public IEnumerator FixedList_StopsDuringMovementAndCanNavigateAgain()
            => VerifyStop(typeof(FixedListScroll));

        [UnityTest]
        public IEnumerator FixedGrid_StopsDuringMovementAndCanNavigateAgain()
            => VerifyStop(typeof(FixedGridScroll));

        [UnityTest]
        public IEnumerator SizedList_StopsDuringMovementAndCanNavigateAgain()
            => VerifyStop(typeof(SizedListScroll));

        [UnityTest]
        public IEnumerator LazyLayout_StopsDuringMovementAndCanNavigateAgain()
            => VerifyStop(typeof(LazyLayoutListScroll));

        private IEnumerator VerifyStop(Type scrollType)
        {
            foreach (var axis in new[] { 0, 1 })
            {
                _root = new GameObject("Navigation test", typeof(RectTransform));
                _root.SetActive(false);
                var viewport = (RectTransform)_root.transform;
                viewport.sizeDelta = new Vector2(300, 300);
                var content = (RectTransform)new GameObject("Content", typeof(RectTransform)).transform;
                content.SetParent(viewport, false);
                _prefab = new GameObject("Cell", typeof(RectTransform));
                ((RectTransform)_prefab.transform).sizeDelta = new Vector2(100, 100);
                var scroll = _root.AddComponent(scrollType);
                var baseType = scrollType.BaseType;
                baseType.GetField("_viewport", PrivateInstance).SetValue(scroll, viewport);
                baseType.GetField("_content", PrivateInstance).SetValue(scroll, content);
                var scroller = baseType.GetProperty("Scroller", PrivateInstance).GetValue(scroll);
                scroller.GetType().GetField("_axis", PrivateInstance).SetValue(scroller, (RectTransform.Axis)axis);
                var source = new Source { Prefab = _prefab };
                for (var i = 0; i < 100; i++) source.Add(i);
                source.GetTotalContentSize(default, 100f, 0f);
                scrollType.GetMethod("SetDataSource").Invoke(scroll, new object[] { source });
                _root.SetActive(true);
                yield return null;
                var navigate = scrollType.GetMethod("SetPositionIndex");
                var position = scrollType.GetProperty("ScrollPosition");
                navigate.Invoke(scroll, new object[] { 60, 0.5f, true });
                var deadline = Time.realtimeSinceStartup + 2f;
                while (Mathf.Abs((float)position.GetValue(scroll)) < 1f && Time.realtimeSinceStartup < deadline)
                    yield return null;
                var stoppedPosition = (float)position.GetValue(scroll);
                Assert.That(Mathf.Abs(stoppedPosition), Is.GreaterThan(1f), "Navigation must move before stopping.");
                Assert.That(scrollType.GetProperty("IsSnapping").GetValue(scroll), Is.True);
                scrollType.GetMethod("StopScroll").Invoke(scroll, null);
                for (var frame = 0; frame < 5; frame++)
                {
                    yield return null;
                    Assert.That((float)position.GetValue(scroll), Is.EqualTo(stoppedPosition).Within(0.001f));
                    Assert.That(content.localPosition[axis], Is.EqualTo(stoppedPosition).Within(0.001f));
                    Assert.That(scrollType.GetProperty("IsIdling").GetValue(scroll), Is.True);
                }
                navigate.Invoke(scroll, new object[] { 0, 0.5f, false });
                yield return null;
                yield return null;
                Assert.That((float)position.GetValue(scroll), Is.EqualTo(0f).Within(0.001f));
                navigate.Invoke(scroll, new object[] { 60, 0.5f, true });
                _root.SetActive(false);
                _root.SetActive(true);
                for (var frame = 0; frame < 3; frame++)
                {
                    yield return null;
                    Assert.That((float)position.GetValue(scroll), Is.EqualTo(0f).Within(0.001f), "Re-enabling must not resume a canceled movement.");
                    Assert.That(scrollType.GetProperty("IsIdling").GetValue(scroll), Is.True);
                }
                Object.Destroy(_root);
                Object.Destroy(_prefab);
                yield return null;
            }
        }
    }
}
