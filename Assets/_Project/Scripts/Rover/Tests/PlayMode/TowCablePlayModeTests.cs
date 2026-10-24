using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// 07's tow cable as seen (opening v3 3.2b), on a rover in a test world with a stand-in <see cref="ITowLine"/>: the
    /// rope runs from the winch to the load's hitch with a clamp there, lies sagged while slack and straight when taut,
    /// the winch drum turns out and in with the rope's extension, and the rope winds back in when the load is let go.
    /// </summary>
    public sealed class TowCablePlayModeTests : InputTestFixture
    {
        private const float CableLength = 6f;
        private const float FarHitchScale = 2.5f;
        private const float MinRopePixels = 4f;
        private const float DragMetresPerSecond = 4.5f;
        private const float Tolerance = 0.02f;

        /// <summary>How far past the rope's length the taut test's hitch stands: more than the winch's sway.</summary>
        private const float TautStretchMetres = 0.1f;

        /// <summary>A taut rope has no sag at all; this is only the float's dust.</summary>
        private const float TautSagMetres = 1e-3f;

        private sealed class FakeTowLine : ITowLine
        {
            public bool Towing { get; set; }

            public Vector3 Hitch { get; set; }

            public float Strain { get; set; }

            public bool IsTowing => Towing;

            public Vector3 LoadHitch => Hitch;

            public float Length => CableLength;

            public float Strain01 => Strain;

            public bool TryFindLoad(string towId, out Transform root)
            {
                root = null;
                return false;
            }
        }

        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;
        private FakeTowLine _line;

        public override void Setup()
        {
            base.Setup();
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
            _line = new FakeTowLine();
            _rover = TestRover.Spawn(_world, TestWorld.Point(0f, -300f), 0f, false, 0f,
                rover => rover.Context.Register<ITowLine>(_line));
        }

        public override void TearDown()
        {
            _rover?.Dispose();
            _rover = null;
            _world.Dispose();
            _physics.Dispose();
            base.TearDown();
        }

        private RoverTowCable Cable => _rover.Controller.TowCable;

        private Vector3 Winch => _rover.Controller.WinchOrigin.position;

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator NotTowing_ThereIsNoRope()
        {
            yield return Wait(0.3f);
            Assert.IsFalse(Cable.IsVisible);
            Assert.AreEqual(0f, Cable.Strain01);
        }

        [UnityTest]
        public IEnumerator ASlackRope_SagsDownBetweenTheWinchAndTheHitch_WithTheClampAtTheHitch()
        {
            yield return Wait(0.2f);
            _line.Hitch = Winch + new Vector3(0f, 0f, -3f);
            _line.Towing = true;
            yield return Wait(0.2f);
            Assert.IsTrue(Cable.IsVisible, "the rope shows");
            Assert.Greater(Cable.Sag, 0.5f, "slack, it sags");
            Assert.AreEqual(0f, Vector3.Distance(Cable.HookPoint, _line.Hitch), Tolerance, "the clamp sits on the hitch");
            Assert.AreEqual(3f, Cable.Extension, 0.1f);
        }

        [UnityTest]
        public IEnumerator ATautRope_RunsStraight_AndLeans07IntoItsStrain()
        {
            yield return Wait(0.2f);
            // Taut means stretched past its length: a hitch exactly one length away is slack by whatever the winch
            // sways in a frame (a rope's sag goes with the square root of its slack, so half a millimetre of slack
            // already sags 3 cm), and a loaded editor sways it more.
            _line.Hitch = Winch + new Vector3(0f, 0f, -(CableLength + TautStretchMetres));
            _line.Strain = 0.7f;
            _line.Towing = true;
            yield return Wait(0.2f);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.Less(Cable.Sag, TautSagMetres, "taut, it runs straight");
            Assert.AreEqual(0.7f, Cable.Strain01, Tolerance);
        }

        [UnityTest]
        public IEnumerator FarFromTheLens_TheRopeIsAtLeastAFewPixelsWideAt1440p()
        {
            yield return Wait(0.2f);
            _line.Hitch = Winch + new Vector3(0f, 0f, -CableLength * FarHitchScale);
            _line.Strain = 0.5f;
            _line.Towing = true;
            yield return Wait(0.3f);
            Camera lens = _rover.Context.Get<IViewCamera>().Camera;
            float distance = Vector3.Distance(lens.transform.position, _line.Hitch);
            float visibleHeight = 2f * distance * Mathf.Tan(lens.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float pixels = Cable.HitchWidth / visibleHeight * RopeWidth.ReferenceHeight;
            Assert.GreaterOrEqual(pixels, MinRopePixels - 0.05f, $"{distance:0} m from the lens the rope is {pixels:0.0} px at 1440p");
        }

        [UnityTest]
        public IEnumerator ADraggedLoad_KicksUpDust_AndAStandingOneKicksNone()
        {
            yield return Wait(0.2f);
            _line.Hitch = Winch + new Vector3(0f, 0f, -CableLength);
            _line.Towing = true;
            float fastest = 0f;
            float until = Time.time + 1f;
            while (Time.time < until)
            {
                _line.Hitch += new Vector3(0f, 0f, -DragMetresPerSecond * Time.deltaTime);
                yield return null;
                fastest = Mathf.Max(fastest, Cable.SkidRate);
            }

            Assert.Greater(fastest, 1f, "a load dragged along kicks up dust");
            yield return Wait(0.8f);
            Assert.AreEqual(0f, Cable.SkidRate, 1e-3f, "a load that stands kicks up none");
        }

        [UnityTest]
        public IEnumerator TheWinchDrum_TurnsOutAsTheLoadFallsBehind_AndInAsItClosesUp()
        {
            yield return Wait(0.2f);
            _line.Hitch = Winch + new Vector3(0f, 0f, -1.5f);
            _line.Towing = true;
            yield return Wait(0.2f);
            float start = Cable.SpoolDegrees;
            _line.Hitch = Winch + new Vector3(0f, 0f, -4.5f);
            yield return Wait(0.2f);
            float out01 = Cable.SpoolDegrees;
            Assert.Greater(out01, start + 90f, "the load drops back: the drum spools out");
            _line.Hitch = Winch + new Vector3(0f, 0f, -2.5f);
            yield return Wait(0.2f);
            Assert.Less(Cable.SpoolDegrees, out01 - 45f, "it closes up: the drum takes in");
        }

        [UnityTest]
        public IEnumerator LettingGo_WindsTheRopeBackInToTheWinch_ThenHidesIt()
        {
            yield return Wait(0.2f);
            _line.Hitch = Winch + new Vector3(0f, 0f, -4f);
            _line.Towing = true;
            yield return Wait(0.2f);
            _line.Towing = false;
            yield return Wait(0.3f);
            Assert.IsTrue(Cable.IsVisible, "still winding in");
            Assert.Less(Vector3.Distance(Cable.HookPoint, Winch), 4f, "the clamp comes in toward the winch");
            yield return Wait(1.6f);
            Assert.IsFalse(Cable.IsVisible, "and then the rope is gone");
        }
    }
}
