using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The close shot of a Rover Bay fitting, composed against a box bay of art's size (walls 2.2 m either side of the
    /// turntable's axis, the back wall 2.15 m behind its centre, the roof at 3.4 m, open at the front, +Z): close and
    /// three-quarter on the working arm, inside the walls, under the crane rail, reached from the bay's view, seeing
    /// the socket past 07; low in from the front for the floor arm; refused when the bay leaves no room.
    /// </summary>
    public sealed class BayShotComposerTests
    {
        private const float Rail = 3.08f;
        private const float HalfWidth = 2.2f;
        private const float Back = -2.5f;
        private const float Front = 1.9f;
        private const float Roof = 3.4f;

        private static readonly Vector3 Centre = new Vector3(0f, 0.15f, -0.35f);
        private static readonly Vector3 BayView = new Vector3(0f, 2.4f, 8f);

        private BayShotSettings _settings;
        private float _ceiling;

        [SetUp]
        public void SetUp()
        {
            _settings = new BayShotSettings();
            _ceiling = Rail - 0.5f;
        }

        [Test]
        public void AFlankSocket_IsFramedCloseAndThreeQuarter_InsideTheBay_SeenPast07()
        {
            var fit = new Fit
            {
                Socket = new Vector3(-1.05f, 0.62f, -0.35f),
                Shoulder = new Vector3(-1.3f, Rail, -0.35f),
                Facing = Vector3.left,
            };

            Assert.IsTrue(BayShotComposer.TrySolve(_settings, fit, BayView, _ceiling, new BoxBay(HalfWidth),
                out BayShot shot));
            Vector3 eye = shot.Eye;
            float distance = Vector3.Distance(eye, shot.Focus);
            Assert.That(distance, Is.InRange(_settings.MinDistance, _settings.ArmDistance + 1e-3f),
                "A few metres off.");
            Assert.Less(Mathf.Abs(eye.x), HalfWidth - 0.1f, "Inside the side walls.");
            Assert.Greater(eye.z, Back + 0.1f, "In front of the back wall.");
            Assert.LessOrEqual(eye.y, _ceiling + 1e-3f, "Under the crane rail.");
            Assert.Greater(eye.y, shot.Focus.y, "Looking down at the socket.");
            Assert.Greater(Vector3.Dot(Flat(eye - Centre), Flat(fit.Socket - Centre)), 0f,
                "On the socket's side of 07, never looking through it.");
            Vector3 side = Vector3.Cross(Vector3.up, Flat(fit.Shoulder - fit.Socket));
            Vector3 near = Vector3.Dot(eye - fit.Socket, side) > 0f ? side : -side;
            float offSide = Vector3.Angle(Flat(eye - fit.Socket), near);
            Assert.Less(offSide, 60f, "Side-on to three-quarter on the arm's links, never looking along them.");
        }

        [Test]
        public void ABellySocket_IsFramedLow_InFromTheOpenFront()
        {
            var fit = new Fit
            {
                Belly = true,
                Socket = new Vector3(0f, 0.43f, -0.32f),
                Shoulder = new Vector3(0f, 0.13f, -0.35f),
                Facing = Vector3.back,
            };

            Assert.IsTrue(BayShotComposer.TrySolve(_settings, fit, BayView, _ceiling, new BoxBay(HalfWidth),
                out BayShot shot));
            Vector3 eye = shot.Eye;
            Assert.Less(eye.y - fit.Socket.y, 0.5f, "Low, to see under 07.");
            Assert.Less(Vector3.Angle(Flat(eye - fit.Socket), Vector3.forward), 45f, "In from the open front.");
            Assert.Less(Mathf.Abs(eye.x), HalfWidth - 0.1f);
        }

        [Test]
        public void AlwaysUnderTheRail_EvenWhenTheShotWouldLookDownSteeply()
        {
            var fit = new Fit
            {
                Socket = new Vector3(-1.05f, 0.62f, -0.35f),
                Shoulder = new Vector3(-1.3f, Rail, -0.35f),
                Facing = Vector3.left,
            };

            float low = fit.Socket.y + 0.9f;
            Assert.IsTrue(BayShotComposer.TrySolve(_settings, fit, BayView, low, new BoxBay(HalfWidth),
                out BayShot shot));
            Assert.LessOrEqual(shot.Eye.y, low + 1e-3f);
        }

        [Test]
        public void ABayTooTightForTheShot_LeavesTheBaysViewHolding()
        {
            var fit = new Fit
            {
                Socket = new Vector3(-1.05f, 0.62f, -0.35f),
                Shoulder = new Vector3(-1.3f, Rail, -0.35f),
                Facing = Vector3.left,
            };

            Assert.IsFalse(BayShotComposer.TrySolve(_settings, fit, BayView, _ceiling, new Closet(), out _));
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.normalized;
        }

        private sealed class Fit : IBayFitView
        {
            public bool Active => true;

            public bool Working => true;

            public bool Belly { get; set; }

            public Vector3 Socket { get; set; }

            public Vector3 Shoulder { get; set; }

            public Vector3 Centre => BayShotComposerTests.Centre;

            public Vector3 Facing { get; set; }

            public Vector3 Front => Vector3.forward;
        }

        /// <summary>Art's bay as solid walls, back and roof, open at the front (sampled along each line).</summary>
        private sealed class BoxBay : ISightLine
        {
            private const int Steps = 400;
            private readonly float _halfWidth;

            public BoxBay(float halfWidth)
            {
                _halfWidth = halfWidth;
            }

            public float Clear(Vector3 from, Vector3 to, float radius)
            {
                float length = Vector3.Distance(from, to);
                for (int i = 1; i <= Steps; i++)
                {
                    Vector3 p = Vector3.Lerp(from, to, (float)i / Steps);
                    bool underRoof = p.z >= Back && p.z <= Front;
                    if ((underRoof && (Mathf.Abs(p.x) > _halfWidth || p.y > Roof))
                        || (p.z < Back && Mathf.Abs(p.x) <= _halfWidth + 0.2f))
                    {
                        return length * (i - 1) / Steps;
                    }
                }

                return length;
            }

            public bool IsFree(Vector3 point, float radius)
            {
                bool underRoof = point.z >= Back - radius && point.z <= Front + radius;
                return !(underRoof && (Mathf.Abs(point.x) > _halfWidth - radius || point.y > Roof - radius))
                    && !(point.z < Back + radius && Mathf.Abs(point.x) <= _halfWidth + 0.2f);
            }
        }

        /// <summary>A bay so tight that nothing is ever more than a metre clear.</summary>
        private sealed class Closet : ISightLine
        {
            public float Clear(Vector3 from, Vector3 to, float radius)
            {
                return Mathf.Min(1f, Vector3.Distance(from, to));
            }

            public bool IsFree(Vector3 point, float radius)
            {
                return true;
            }
        }
    }
}
