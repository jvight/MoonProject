using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// Which rack bay 07 reads (<see cref="RackAim"/>): an outline only, within reach, in view, the one nearest the
    /// centre of the view.
    /// </summary>
    public sealed class RackAimTests
    {
        private const float Reach = 4f;
        private const float Margin = 0.1f;

        private readonly Vector3[] _bays = { new Vector3(-0.5f, 0.7f, 3f), new Vector3(0f, 0.7f, 3f),
            new Vector3(0.5f, 0.7f, 3f) };

        [Test]
        public void TheOutlineNearestTheCentreOfTheView_IsRead()
        {
            var rack = new FakeRack(RackBayState.Outline, RackBayState.Outline, RackBayState.Hidden);
            Vector3[] view = { new Vector3(0.35f, 0.5f, 3f), new Vector3(0.52f, 0.5f, 3f), new Vector3(0.5f, 0.5f, 3f) };
            Assert.AreEqual(1, RackAim.Pick(rack, _bays, view, Vector3.zero, Reach, Margin),
                "the hidden bay at the very centre says nothing");
        }

        [Test]
        public void AFilledBay_OneOutOfReach_OrOneOffScreen_IsNotRead()
        {
            Vector3[] view = { new Vector3(0.5f, 0.5f, 3f), new Vector3(0.5f, 0.5f, 3f), new Vector3(0.5f, 0.5f, 3f) };
            var filled = new FakeRack(RackBayState.Filled, RackBayState.Filled, RackBayState.Filled);
            Assert.AreEqual(-1, RackAim.Pick(filled, _bays, view, Vector3.zero, Reach, Margin), "a fitted part is done");

            var outlines = new FakeRack(RackBayState.Outline, RackBayState.Outline, RackBayState.Outline);
            Assert.AreEqual(-1, RackAim.Pick(outlines, _bays, view, new Vector3(0f, 0f, -10f), Reach, Margin),
                "too far away");

            Vector3[] away = { new Vector3(0.05f, 0.5f, 3f), new Vector3(0.5f, 0.5f, -3f), new Vector3(0.5f, 0.97f, 3f) };
            Assert.AreEqual(-1, RackAim.Pick(outlines, _bays, away, Vector3.zero, Reach, Margin),
                "at the edge, behind the camera, at the top edge");
        }

        private sealed class FakeRack : IRackStatus
        {
            private readonly RackBayState[] _states;

            public FakeRack(params RackBayState[] states)
            {
                _states = states;
            }

            public bool Shown => true;

            public Vector3 Position => Vector3.zero;

            public Quaternion Rotation => Quaternion.identity;

            public int Count => _states.Length;

            public string IdOf(int bay)
            {
                return "rack." + bay;
            }

            public RackBayState StateOf(int bay)
            {
                return _states[bay];
            }
        }
    }
}
