using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace SimpleScroll.Tests
{
    public class ScrollerTests
    {
        private const BindingFlags InternalInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly Type ScrollerType = typeof(FixedListScroll).Assembly.GetType("SimpleScroll.Scroller");
        private object _scroller;

        private void Create(int axis)
        {
            _scroller = Activator.CreateInstance(ScrollerType, true);
            ScrollerType.GetField("_axis", InternalInstance).SetValue(_scroller, (RectTransform.Axis)axis);
            Call("Initialize", 1000f);
        }

        private object Get(string name) => ScrollerType.GetProperty(name, InternalInstance).GetValue(_scroller);
        private void Set(string name, object value) => ScrollerType.GetProperty(name, InternalInstance).SetValue(_scroller, value);
        private object Call(string name, params object[] args) => ScrollerType.GetMethod(name, InternalInstance).Invoke(_scroller, args);

        [TestCase(0, ScrollState.Snapping)]
        [TestCase(1, ScrollState.Snapping)]
        [TestCase(0, ScrollState.Coasting)]
        [TestCase(1, ScrollState.Coasting)]
        public void StopClearsMovementAndIdleUpdatesDoNotMove(int axis, ScrollState state)
        {
            Create(axis);
            var direction = axis == 0 ? -1f : 1f;
            Call("MoveTo", 200f * direction, false);
            Call("MoveTo", 800f * direction, true);
            Set("State", state);
            ScrollerType.GetField("_velocity", InternalInstance).SetValue(_scroller, 300f * direction);
            Call("Stop");
            for (var frame = 0; frame < 5; frame++)
            {
                Assert.That(Call("Update", 0.02f), Is.EqualTo(0f));
                Assert.That(Get("ScrollPosition"), Is.EqualTo(200f * direction));
                Assert.That(Get("TargetPosition"), Is.EqualTo(200f * direction));
                Assert.That(Get("Velocity"), Is.EqualTo(0f));
                Assert.That(Get("State"), Is.EqualTo(ScrollState.Idle));
            }
            Call("MoveTo", 500f * direction, false);
            Assert.That(Get("ScrollPosition"), Is.EqualTo(500f * direction));
            Assert.That(Call("Update", 0.02f), Is.EqualTo(0f));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void LayoutShiftPreservesDistanceToTarget(int axis)
        {
            Create(axis);
            var direction = axis == 0 ? -1f : 1f;
            Call("MoveTo", 200f * direction, false);
            Call("MoveTo", 800f * direction, true);
            Call("ShiftPosition", 50f * direction);
            Assert.That(Get("ScrollPosition"), Is.EqualTo(250f * direction));
            Assert.That(Get("TargetPosition"), Is.EqualTo(850f * direction));
            Assert.That(Get("State"), Is.EqualTo(ScrollState.Snapping));
            Call("Stop");
            Call("ShiftPosition", -50f * direction);
            Assert.That(Get("TargetPosition"), Is.EqualTo(Get("ScrollPosition")));
            Assert.That(Call("Update", 0.02f), Is.EqualTo(0f));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RetargetingCoastingPreservesVelocity(int axis)
        {
            Create(axis);
            Set("State", ScrollState.Coasting);
            ScrollerType.GetField("_velocity", InternalInstance).SetValue(_scroller, 300f);
            var velocity = Get("Velocity");
            Call("SetTargetPosition", axis == 0 ? -800f : 800f);
            Assert.That(Get("Velocity"), Is.EqualTo(velocity));
            Assert.That(Get("State"), Is.EqualTo(ScrollState.Coasting));
        }

        [TestCase(0, 1f / 30f)]
        [TestCase(1, 1f / 30f)]
        [TestCase(0, 1f / 120f)]
        [TestCase(1, 1f / 120f)]
        public void SnappingFinishesAtDestinationAtDifferentFrameRates(int axis, float deltaTime)
        {
            Create(axis);
            var expected = axis == 0 ? -800f : 800f;
            Call("MoveTo", expected, true);
            for (var frame = 0; frame < 3600 && !(bool)Get("IsIdling"); frame++) Call("Update", deltaTime);
            Assert.That(Get("ScrollPosition"), Is.EqualTo(expected));
            Assert.That(Get("State"), Is.EqualTo(ScrollState.Idle));
            Assert.That(Call("Update", deltaTime), Is.EqualTo(0f));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ZeroDeltaTimeDoesNotCompletePendingMovement(int axis)
        {
            Create(axis);
            Call("MoveTo", axis == 0 ? -800f : 800f, true);
            Assert.That(Call("Update", 0f), Is.EqualTo(0f));
            Assert.That(Get("ScrollPosition"), Is.EqualTo(0f));
            Assert.That(Get("State"), Is.EqualTo(ScrollState.Snapping));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void CoastingReturnsFromOverscrollAndFinishesWithinBounds(int axis)
        {
            Create(axis);
            var direction = axis == 0 ? -1f : 1f;
            Call("MoveTo", 900f * direction, false);
            Set("State", ScrollState.Coasting);
            ScrollerType.GetField("_velocity", InternalInstance).SetValue(_scroller, 6000f * direction);
            Call("SetTargetPosition", 1900f * direction);
            for (var frame = 0; frame < 1000 && !(bool)Get("IsIdling"); frame++) Call("Update", 0.02f);
            Assert.That(Get("ScrollPosition"), Is.EqualTo(1000f * direction));
            Assert.That(Get("State"), Is.EqualTo(ScrollState.Idle));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void InfiniteScrollAllowsMovementInBothDirections(int axis)
        {
            Create(axis);
            Call("Initialize", float.PositiveInfinity);
            Call("MoveTo", -200f, false);
            Assert.That(Get("ScrollPosition"), Is.EqualTo(-200f));
            Call("MoveTo", 200f, false);
            Assert.That(Get("ScrollPosition"), Is.EqualTo(200f));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ResizeConstrainsActiveTarget(int axis)
        {
            Create(axis);
            var direction = axis == 0 ? -1f : 1f;
            Call("MoveTo", 200f * direction, false);
            Call("MoveTo", 800f * direction, true);
            Call("Initialize", 100f);
            Assert.That(Get("ScrollPosition"), Is.EqualTo(100f * direction));
            Assert.That(Get("TargetPosition"), Is.EqualTo(100f * direction));
            Call("Update", 0.02f);
            Assert.That(Get("State"), Is.EqualTo(ScrollState.Idle));
        }
    }
}
