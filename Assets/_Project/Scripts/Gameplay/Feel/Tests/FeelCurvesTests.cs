using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Gameplay.Feel.Tests
{
    /// <summary>The cut feel kit's curves (docs/design/feel-pass.md §3.1, R11, R12).</summary>
    public sealed class FeelCurvesTests
    {
        private const float Orange = 1f;
        private const float Red = 3f;
        private const float Dark = 6f;
        private const float Lunar = 1.62f;

        [Test]
        public void Seam_CoolsFromWhiteThroughOrangeAndRedToDark_ByAge()
        {
            Assert.AreEqual(1f, FeelCurves.Heat(0f, Orange, Red, Dark), 1e-5f, "just cut: white-yellow");
            Assert.AreEqual(2f / 3f, FeelCurves.Heat(Orange, Orange, Red, Dark), 1e-4f, "orange after 1 s");
            Assert.AreEqual(1f / 3f, FeelCurves.Heat(Red, Orange, Red, Dark), 1e-4f, "dull red after 3 s");
            Assert.AreEqual(0f, FeelCurves.Heat(Dark, Orange, Red, Dark), 1e-5f, "dark after 6 s");
            Assert.AreEqual(0f, FeelCurves.Heat(60f, Orange, Red, Dark), 1e-5f, "and stays a dark line");
            float last = 2f;
            for (float age = 0f; age < Dark; age += 0.1f)
            {
                float heat = FeelCurves.Heat(age, Orange, Red, Dark);
                Assert.LessOrEqual(heat, last, $"never warms back up by itself (age {age})");
                last = heat;
            }
        }

        [Test]
        public void ScanRibbon_LeavesTheFragment_EasesIn_AndArrivesWhole()
        {
            const float length = 0.45f;
            Assert.AreEqual(Vector2.zero, FeelCurves.RibbonSpan(0f, length), "starts at the fragment");
            Assert.AreEqual(Vector2.one, FeelCurves.RibbonSpan(1f, length), "head and tail both reach the lens");
            Vector2 early = FeelCurves.RibbonSpan(0.25f, length);
            Vector2 late = FeelCurves.RibbonSpan(0.75f, length);
            Assert.Less(early.y, 0.25f, "an ease-in: slow to leave the fragment");
            Assert.Greater(late.y - early.y, early.y, "rushing into the lens at the end");
            for (float t = 0f; t <= 1f; t += 0.05f)
            {
                Vector2 span = FeelCurves.RibbonSpan(t, length);
                Assert.LessOrEqual(span.x, span.y, $"the tail never passes the head (t {t})");
            }
        }

        [Test]
        public void ScanRibbon_ArcsUpBetweenFragmentAndLens()
        {
            var from = new Vector3(0f, 0.2f, 3f);
            var to = new Vector3(0f, 1.1f, 0f);
            Assert.AreEqual(from, FeelCurves.RibbonPoint(from, to, 0.18f, 0f));
            Assert.AreEqual(to, FeelCurves.RibbonPoint(from, to, 0.18f, 1f));
            Vector3 middle = FeelCurves.RibbonPoint(from, to, 0.18f, 0.5f);
            Assert.Greater(middle.y, (from.y + to.y) * 0.5f, "lifted at the middle");
            Assert.AreEqual((from + to) * 0.5f, FeelCurves.RibbonPoint(from, to, 0f, 0.5f), "no arc: straight");
        }

        [Test]
        public void Motes_StartOnTheSphere_CurlIn_AndArriveAtTheEmitter()
        {
            var start = new Vector3(0f, 0f, 0.6f);
            Assert.AreEqual(start, FeelCurves.MoteOffset(start, Vector3.up, 0f, 2.4f, 0.3f), "starts on the sphere");
            Assert.Less(FeelCurves.MoteOffset(start, Vector3.up, 1f, 2.4f, 0.3f).magnitude, 1e-5f, "arrives");
            Vector3 early = FeelCurves.MoteOffset(start, Vector3.up, 0.3f, 2.4f, 0.3f);
            Vector3 late = FeelCurves.MoteOffset(start, Vector3.up, 0.8f, 2.4f, 0.3f);
            Assert.Greater(early.magnitude, 0.55f, "held out early: an ease-in, drawn in, not pushed");
            Assert.Less(late.magnitude, 0.3f, "rushing in at the end");
            Assert.Greater(Mathf.Abs(late.x), 1e-3f, "its path curls round the emitter");
        }

        [Test]
        public void Beam_ExtendsOnAFastEaseOut_NeverInstantly()
        {
            Assert.AreEqual(0f, FeelCurves.Extend(0f), 1e-5f);
            Assert.AreEqual(1f, FeelCurves.Extend(1f), 1e-5f);
            Assert.Greater(FeelCurves.Extend(0.3f), 0.5f, "fast at first");
            Assert.Less(FeelCurves.Extend(0.3f), 1f, "but it travels");
        }

        [Test]
        public void Debris_FlyUnderLunarGravityWithNoDrag_AndBounceAtThreeTenths()
        {
            Vector3 position = Vector3.zero;
            var velocity = new Vector3(2f, 3f, 0f);
            const float dt = 1f / 120f;
            float flown = 0f;
            bool landed = false;
            while (!landed && flown < 10f)
            {
                landed = FeelCurves.Ballistic(ref position, ref velocity, dt, Lunar, -0.5f, 0.3f);
                flown += dt;
            }

            Assert.IsTrue(landed);
            Assert.AreEqual(2f * 0.3f, velocity.x, 1e-3f, "no drag in flight; the bounce keeps three tenths");
            Assert.Greater(velocity.y, 0f, "it bounces up");
            float expected = (3f + Mathf.Sqrt(9f + 2f * Lunar * 0.5f)) / Lunar;
            Assert.AreEqual(expected, flown, 0.02f, "a clean ballistic arc under lunar gravity");
        }
    }
}
