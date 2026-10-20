using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The tow cable's drag (ruling 4: a rope from the winch, no beam): slack it does nothing; taut it draws the load
    /// toward the winch, harder the more it is stretched and the faster 07 pulls away, never pushing, never closing in
    /// faster than its catch-up and never past its cap; it lifts the load's front by a share of the flat pull; and the snag rule lifts a caught load free
    /// after its grace, or lets a load that fell far behind go.
    /// </summary>
    public sealed class TowCableTests
    {
        private const float Length = 4f;

        /// <summary>The Moon's gravity (m/s²).</summary>
        private const float Gravity = 1.62f;
        private static readonly Vector3 Winch = new Vector3(0f, 0.6f, 0f);

        private TowSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new TowSettings();
        }

        [Test]
        public void ASlackCable_DoesNothing()
        {
            Vector3 hitch = new Vector3(0f, 0.4f, -Length + 0.5f);
            Assert.AreEqual(Vector3.zero, Pull(hitch, Vector3.zero, Vector3.forward * 3f), "inside its length");
            Assert.AreEqual(0f, TowCable.Strain01(Winch, hitch, Length, _settings));
        }

        [Test]
        public void ATautCable_DrawsTheLoadTowardTheWinch_HarderTheMoreItIsStretched()
        {
            Vector3 little = Pull(Behind(Length + 0.1f), Vector3.zero, Vector3.zero);
            Vector3 more = Pull(Behind(Length + 0.3f), Vector3.zero, Vector3.zero);
            Assert.Greater(little.z, 0f, "toward the winch, ahead of the load");
            Assert.Greater(more.z, little.z, "more stretch, more pull");
            Assert.Greater(TowCable.Strain01(Winch, Behind(Length + 0.3f), Length, _settings),
                TowCable.Strain01(Winch, Behind(Length + 0.1f), Length, _settings));
        }

        [Test]
        public void ThePull_MatchesTheLoadTo07_AndNeverPushes()
        {
            Vector3 hitch = Behind(Length + 0.05f);
            Vector3 parting = Pull(hitch, Vector3.zero, Vector3.forward * 2f);
            Vector3 still = Pull(hitch, Vector3.zero, Vector3.zero);
            Assert.Greater(parting.z, still.z, "07 drives away: the rope takes up harder");

            Vector3 closing = Pull(hitch, Vector3.forward * 3f, Vector3.zero);
            Assert.AreEqual(Vector3.zero, closing, "the load runs up on 07: a rope never pushes it back");
        }

        [Test]
        public void ALoadFarBehind_ClosesInNoFasterThanItsCatchUp()
        {
            Vector3 hitch = Behind(Length + 3f);
            Vector3 winchSpeed = Vector3.forward * 4f;
            Vector3 catchingUp = Pull(hitch, winchSpeed + Vector3.forward * (_settings.CatchUp + 0.05f), winchSpeed);
            Assert.AreEqual(Vector3.zero, catchingUp,
                "already closing faster than the catch-up: the rope lets it come, it never surges it on");
            Vector3 lagging = Pull(hitch, winchSpeed, winchSpeed);
            Assert.Greater(lagging.z, 0f, "level with 07's speed but far behind: the rope draws it in");
        }

        [Test]
        public void ThePull_IsCapped_SoAStretchNeverYanks()
        {
            Vector3 pull = Pull(Behind(Length + 20f), Vector3.zero, Vector3.forward * 10f);
            Vector2 flat = new Vector2(pull.x, pull.z);
            Assert.AreEqual(_settings.MaxPull, new Vector3(flat.x, 0f, flat.y).magnitude, 0.5f,
                "the cap holds (the rope runs nearly flat here)");
            Assert.AreEqual(1f, TowCable.Strain01(Winch, Behind(Length + 20f), Length, _settings));
        }

        [Test]
        public void ThePull_LiftsTheLoadsFront_ByItsShareOfTheFlatPull()
        {
            Vector3 hitch = new Vector3(0f, Winch.y, -(Length + 0.02f));
            Vector3 pull = Pull(hitch, Vector3.zero, Vector3.zero);
            Assert.Less(pull.z * _settings.LiftShare, Gravity * _settings.MaxLift, "a light pull, under the lift cap");
            Assert.AreEqual(pull.z * _settings.LiftShare, pull.y, 1e-4f,
                "a level rope lifts by the share alone, so the load rides over bumps like a sled");
        }

        [Test]
        public void ThePull_NeverLiftsTheLoadOffTheGround()
        {
            Vector3 under = new Vector3(0f, Winch.y - 3f, -(Length + 1f));
            Vector3 pull = Pull(under, Vector3.zero, Vector3.forward * 4f);
            Assert.AreEqual(Gravity * _settings.MaxLift, pull.y, 1e-4f,
                "even up a steep bank the cable takes only part of the load's weight");
            Assert.Less(_settings.MaxLift, 1f, "so it always keeps weight on its skids");
        }

        [Test]
        public void TheSkids_BiteOnlyWhenTheLoadRunsFasterThan07AndItsCatchUp()
        {
            float rover = 4f;
            float behind = TowCable.CatchUp(Length + 3f, Length, _settings);
            Assert.AreEqual(_settings.CatchUp, behind, "far behind: the whole catch-up");
            Assert.AreEqual(0f, TowCable.CatchUp(Length - 1f, Length, _settings), "slack: no catch-up at all");
            Assert.AreEqual(Vector3.zero,
                TowCable.SkidBite(Vector3.forward * (rover + behind), rover, behind, _settings), "keeping pace");
            Vector3 running = Vector3.forward * (rover + behind + 0.5f) + Vector3.down * 3f;
            Vector3 bite = TowCable.SkidBite(running, rover, behind, _settings);
            Assert.Less(bite.z, 0f, "running on down a slope: the skids hold it back");
            Assert.AreEqual(0f, bite.y, "flat only: they never pin it down or lift it");
            Assert.LessOrEqual(bite.magnitude, _settings.MaxPull + 1e-4f);
            Assert.Less(TowCable.SkidBite(Vector3.forward * (rover + 0.2f), rover, 0f, _settings).z, 0f,
                "slack, a load never comes in on 07 faster than 07 goes: it never bumps it");
        }

        [Test]
        public void ACaughtLoad_IsLiftedFree_AfterItsGrace_ThenTheCountStartsAgain()
        {
            var snag = new TowSnag();
            float caught = Length + _settings.SnagStretch + 0.1f;
            float frame = 0.02f;
            int frames = Mathf.CeilToInt(_settings.SnagSeconds / frame) - 1;
            for (int i = 0; i < frames; i++)
            {
                Assert.AreEqual(TowSnagAction.None, snag.Step(caught, Length, 0f, frame, _settings), "within the grace");
            }

            Assert.AreEqual(TowSnagAction.LiftFree, snag.Step(caught, Length, 0f, frame * 2f, _settings));
            Assert.AreEqual(1, snag.Lifts);
            Assert.AreEqual(TowSnagAction.None, snag.Step(caught, Length, 0f, frame, _settings),
                "the count starts again");
            Assert.AreEqual(TowSnagAction.None, snag.Step(Length, Length, 0f, _settings.SnagSeconds * 2f, _settings),
                "a load that follows is never caught");
        }

        [Test]
        public void AStretchedLoadStillComing_OnlyLags_AndIsNeverCaught()
        {
            var snag = new TowSnag();
            float stretched = Length + _settings.SnagStretch + 1f;
            Assert.AreEqual(TowSnagAction.None, snag.Step(stretched, Length, _settings.SnagSpeed + 0.5f,
                _settings.SnagSeconds * 4f, _settings), "it is coming toward the winch: 07 only pulled away fast");
            Assert.AreEqual(0, snag.Lifts);
        }

        [Test]
        public void ALoadFarBehind_IsLetGo_NeverDraggedOn()
        {
            var snag = new TowSnag();
            Assert.AreEqual(TowSnagAction.LetGo,
                snag.Step(_settings.LetGoDistance + 0.1f, Length, 0f, 0.02f, _settings));
        }

        private Vector3 Pull(Vector3 hitch, Vector3 hitchVelocity, Vector3 winchVelocity)
        {
            return TowCable.Pull(Winch, hitch, hitchVelocity, winchVelocity, Length, Gravity, _settings);
        }

        /// <summary>A hitch straight behind the winch, <paramref name="distance"/> metres from it, at load height.</summary>
        private static Vector3 Behind(float distance)
        {
            const float hitchHeight = 0.4f;
            float drop = Winch.y - hitchHeight;
            return new Vector3(0f, hitchHeight, -Mathf.Sqrt(distance * distance - drop * drop));
        }
    }
}
