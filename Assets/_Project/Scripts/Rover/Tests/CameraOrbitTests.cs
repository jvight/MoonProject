using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MoonProject.Rover.Tests
{
    /// <summary>The camera comfort rules of docs/design/first-hour-v2.md §7.3, §7.4 and §9.</summary>
    public sealed class CameraOrbitTests
    {
        private const float Frame = 1f / 60f;
        private const float Cruise = 5f;
        private const float RateSlack = 0.01f;

        /// <summary>What the look's glide still adds after it has settled for half a second (deg).</summary>
        private const float Held = 0.01f;
        private static readonly Vector2 Right = new Vector2(1f, 0f);

        private RoverCameraTuning _tuning;
        private CameraOrbit _orbit;
        private float _roverYaw;
        private float _turnRate;
        private float _speed;
        private float _throttle;
        private float _descent;
        private bool _grounded;
        private bool _stick;
        private bool _mayRecenter;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverCameraTuning>();
            _orbit = new CameraOrbit(_tuning);
            _roverYaw = 0f;
            _turnRate = 0f;
            _descent = 0f;
            _grounded = true;
            _stick = false;
            _mayRecenter = true;
            Park();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        /// <summary>Gives the tuning a front limit short of 180 degrees (the default lets the view go all the way round).
        /// </summary>
        private void LimitTheFrontTo(float degrees)
        {
            var serialized = new SerializedObject(_tuning);
            serialized.FindProperty("_maxLookYaw").floatValue = degrees;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private void Park()
        {
            _speed = 0f;
            _throttle = 0f;
        }

        private void DriveForward(float speed)
        {
            _speed = speed;
            _throttle = 1f;
        }

        private void StepOnce(Vector2 look, bool recenter = false)
        {
            _roverYaw += _turnRate * Frame;
            _orbit.Step(new CameraOrbitInput(look, _stick, recenter, _roverYaw, _speed, _throttle,
                Mathf.Abs(_speed) / 8f, _descent, _grounded, _mayRecenter), Frame);
        }

        private void Run(float seconds, Vector2 look)
        {
            int frames = Mathf.RoundToInt(seconds / Frame);
            for (int i = 0; i < frames; i++)
            {
                StepOnce(look);
            }
        }

        /// <summary>Looks right at 60 deg/s for <paramref name="seconds"/>, then lets the glide settle.</summary>
        private void LookAway(float seconds)
        {
            Run(seconds, Right);
            Run(0.5f, Vector2.zero);
        }

        /// <summary>The largest bearing change per second over <paramref name="seconds"/> of idle steps.</summary>
        private float FastestTurn(float seconds)
        {
            float fastest = 0f;
            int frames = Mathf.RoundToInt(seconds / Frame);
            for (int i = 0; i < frames; i++)
            {
                float before = _orbit.Bearing;
                StepOnce(Vector2.zero);
                fastest = Mathf.Max(fastest, Mathf.Abs(Mathf.DeltaAngle(before, _orbit.Bearing)) / Frame);
            }

            return fastest;
        }

        [Test]
        public void TunedDefaults_AreTheComfortRules()
        {
            Assert.AreEqual(1.5f, _tuning.RecenterDelay);
            Assert.AreEqual(1.5f, _tuning.StickRecenterDelay);
            Assert.AreEqual(180f, _tuning.MaxLookYaw, "the view goes all the way round 07");
            Assert.AreEqual(2f, _tuning.RecenterMinSpeed);
            Assert.AreEqual(1f, _tuning.RecenterDriveSeconds);
            Assert.AreEqual(25f, _tuning.RecenterDeadzone);
            Assert.AreEqual(2f, _tuning.RecenterRampTime);
            Assert.AreEqual(30f, _tuning.RecenterMaxRate);
            Assert.AreEqual(20f, _tuning.RecenterEaseAngle);
            Assert.AreEqual(10f, _tuning.RecenterPitchRate);
            Assert.AreEqual(0.8f, _tuning.RecenterButtonSeconds);
            Assert.AreEqual(45f, _tuning.FollowMaxRate);
            Assert.AreEqual(3f, _tuning.SpeedFovBoost);
            Assert.LessOrEqual(_tuning.MaxBumpKick, 0.6f);
            Assert.GreaterOrEqual(_tuning.MinShotBlend, 1.2f);
            Assert.GreaterOrEqual(_tuning.GentleMinShotBlend, 2f);
            Assert.AreEqual(0.15f, _tuning.GentleMinSensitivity);
        }

        [Test]
        public void StartsBehindAtTheLowOpeningElevation_Following()
        {
            _orbit.Reset(70f);
            Assert.AreEqual(70f, _orbit.Bearing, 1e-4f);
            Assert.AreEqual(0f, _orbit.YawOffset, 1e-4f);
            Assert.IsTrue(_orbit.IsFollowing);
            Assert.AreEqual(_tuning.OpeningPitch, _orbit.Elevation);
            Assert.Less(_tuning.OpeningPitch, _tuning.DefaultPitch);
            Assert.AreEqual(_tuning.BaseFov, _orbit.FieldOfView);
        }

        [Test]
        public void OpeningShot_HoldsWhileParked_AndEasesToRestingElevationOnceDriving()
        {
            Run(10f, Vector2.zero);
            Assert.AreEqual(_tuning.OpeningPitch, _orbit.Elevation, 1e-3f, "The opening shot holds while 07 waits.");

            DriveForward(Cruise);
            StepOnce(Vector2.zero);
            Assert.Less(_orbit.Elevation, _tuning.OpeningPitch + 0.5f, "No jump when driving starts.");
            Run(10f, Vector2.zero);
            Assert.AreEqual(_tuning.DefaultPitch, _orbit.Elevation, 0.5f);
        }

        [Test]
        public void LookInput_OrbitsByTheRequestedAmount()
        {
            Run(1f, Right);
            Run(1f, Vector2.zero);
            Assert.AreEqual(60f, _orbit.Bearing, 1.5f, "Smoothing delays but never loses input.");
        }

        [Test]
        public void LookInput_GlidesInsteadOfStepping()
        {
            StepOnce(new Vector2(10f, 0f));
            Assert.Less(_orbit.Bearing, 10f);
            Assert.Greater(_orbit.Bearing, 0f);
        }

        [Test]
        public void Elevation_IsClamped()
        {
            Run(2f, new Vector2(0f, 5f));
            Assert.AreEqual(_tuning.MaxPitch, _orbit.Elevation, 1e-3f);
            Run(4f, new Vector2(0f, -5f));
            Assert.AreEqual(_tuning.MinPitch, _orbit.Elevation, 1e-3f);
        }

        [Test]
        public void ViewHeld120ToTheSide_A90TurnBy07_MovesTheBearingUnder2Degrees()
        {
            LookAway(2f);
            Assert.AreEqual(120f, _orbit.YawOffset, 6f);
            float bearing = _orbit.Bearing;

            DriveForward(Cruise);
            _turnRate = 60f;
            Run(1.5f, Vector2.zero);
            _turnRate = 0f;
            Assert.AreEqual(90f, _roverYaw, 1e-3f);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(bearing, _orbit.Bearing)), 2f, "07 turned under the view.");
        }

        [Test]
        public void TurnUnderAHeldView_WhileParked_ChangesNoBearing()
        {
            LookAway(1f);
            Run(10f, Vector2.zero);
            float bearing = _orbit.Bearing;
            _turnRate = -70f;
            Run(3f, Vector2.zero);
            Assert.AreEqual(bearing, _orbit.Bearing, 1e-3f);
        }

        [Test]
        public void IdleClock_Mouse_WaitsFiveSecondsAfterTheMouseStops()
        {
            DriveForward(Cruise);
            LookAway(1f);
            float offset = _orbit.YawOffset;
            Run(_tuning.RecenterDelay - 0.6f, Vector2.zero);
            Assert.AreEqual(0f, _orbit.RecenterWeight, "No recentering before the idle delay.");
            Assert.AreEqual(offset, _orbit.YawOffset, Held);

            Run(0.3f, Vector2.zero);
            Assert.Greater(_orbit.RecenterWeight, 0f);
        }

        [Test]
        public void IdleClock_Stick_WaitsFourSecondsAfterTheStickCentres()
        {
            DriveForward(Cruise);
            _stick = true;
            LookAway(1f);
            Run(_tuning.StickRecenterDelay - 0.6f, Vector2.zero);
            Assert.AreEqual(0f, _orbit.RecenterWeight);

            Run(0.3f, Vector2.zero);
            Assert.Greater(_orbit.RecenterWeight, 0f);
            Assert.LessOrEqual(_tuning.StickRecenterDelay, _tuning.RecenterDelay);
        }

        [Test]
        public void IdleClock_AHeldStickCountsAsLooking()
        {
            DriveForward(Cruise);
            _stick = true;
            Run(_tuning.RecenterDelay + 3f, new Vector2(0.05f, 0f));
            Assert.AreEqual(0f, _orbit.RecenterWeight);
        }

        [TestCase(1.9f, 1f, TestName = "Recenter_NeverBelowTheMinimumSpeed")]
        [TestCase(-5f, -1f, TestName = "Recenter_NeverWhileReversing")]
        [TestCase(5f, 0f, TestName = "Recenter_NeverWhileCoastingWithoutThrottle")]
        [TestCase(0f, 0f, TestName = "Recenter_NeverWhileParked")]
        public void Recenter_NeedsDrivingForward(float speed, float throttle)
        {
            LookAway(1f);
            _speed = speed;
            _throttle = throttle;
            float offset = _orbit.YawOffset;
            Run(20f, Vector2.zero);
            Assert.AreEqual(offset, _orbit.YawOffset, Held);
            Assert.AreEqual(0f, _orbit.RecenterWeight);
        }

        [Test]
        public void Recenter_NeedsASecondOfDrivingForward()
        {
            LookAway(1f);
            Run(_tuning.RecenterDelay + 1f, Vector2.zero);
            DriveForward(Cruise);
            Run(_tuning.RecenterDriveSeconds - 0.1f, Vector2.zero);
            Assert.AreEqual(0f, _orbit.RecenterWeight);
            Run(0.2f, Vector2.zero);
            Assert.Greater(_orbit.RecenterWeight, 0f);
        }

        [Test]
        public void Recenter_NeverDuringAMoment()
        {
            _mayRecenter = false;
            DriveForward(Cruise);
            LookAway(1f);
            float offset = _orbit.YawOffset;
            Run(20f, Vector2.zero);
            Assert.AreEqual(offset, _orbit.YawOffset, Held);
        }

        [Test]
        public void Recenter_LeavesSmallOffsetsAlone()
        {
            DriveForward(Cruise);
            LookAway(0.3f);
            float offset = _orbit.YawOffset;
            Assert.Less(offset, _tuning.RecenterDeadzone);
            Run(20f, Vector2.zero);
            Assert.AreEqual(offset, _orbit.YawOffset, Held, "No micro-swings inside the deadzone.");
        }

        [Test]
        public void Recenter_FadesIn_TurnsAtMost30DegreesASecond_AndComesHome()
        {
            DriveForward(Cruise);
            LookAway(2.5f);
            Assert.Greater(Mathf.Abs(_orbit.YawOffset), 120f);
            Run(_tuning.RecenterDelay - 0.5f, Vector2.zero);

            Run(0.5f, Vector2.zero);
            Assert.Less(_orbit.RecenterWeight, 0.2f, "Recentering fades in, it does not yank.");
            Assert.LessOrEqual(FastestTurn(10f), _tuning.RecenterMaxRate + RateSlack);
            Assert.AreEqual(0f, _orbit.YawOffset, 0.05f);
            Assert.IsTrue(_orbit.IsFollowing);
        }

        [Test]
        public void Recenter_FromATunedFrontLimit_TakesAboutFiveSeconds()
        {
            LimitTheFrontTo(135f);
            DriveForward(Cruise);
            Run(2.5f, Right);
            Run(0.5f, Vector2.zero);
            Assert.AreEqual(_tuning.MaxLookYaw, _orbit.YawOffset, 10f);
            Run(_tuning.RecenterDelay - 0.5f, Vector2.zero);
            Run(5f, Vector2.zero);
            Assert.Greater(Mathf.Abs(_orbit.YawOffset), 5f, "Still on its way after 5 s.");
            Run(3f, Vector2.zero);
            Assert.AreEqual(0f, _orbit.YawOffset, 0.05f);
        }

        [Test]
        public void Look_WithATunedFrontLimit_StopsShortOfFacing07HeadOn()
        {
            LimitTheFrontTo(135f);
            Run(8f, Right);
            Run(1f, Vector2.zero);

            Assert.LessOrEqual(Mathf.Abs(_orbit.YawOffset), _tuning.MaxLookYaw);
            Assert.Greater(Mathf.Abs(_orbit.YawOffset), _tuning.MaxLookYaw - _tuning.LookYawSoftBand,
                "it swings well round before the limit holds it");
        }

        [Test]
        public void Look_SlowsOverTheSoftBand_NeverStopsDead()
        {
            LimitTheFrontTo(135f);
            Run(2f, Right);
            float before = _orbit.YawOffset;
            Run(Frame, Right);
            float nearLimitStep = Mathf.Abs(_orbit.YawOffset - before);

            _orbit.Reset(0f);
            Run(0.5f, Right);
            float early = _orbit.YawOffset;
            Run(Frame, Right);
            float openStep = Mathf.Abs(_orbit.YawOffset - early);

            Assert.Less(nearLimitStep, openStep, "the look slows against the limit");
        }

        [Test]
        public void FreeLook_GoesAllRound07_ItsFaceIncluded_AndOnPastIt()
        {
            float travelled = 0f;
            bool atTheFront = false;
            float last = _orbit.Bearing;
            for (int i = 0; i < Mathf.RoundToInt(12f / Frame); i++)
            {
                StepOnce(Right);
                travelled += Mathf.DeltaAngle(last, _orbit.Bearing);
                last = _orbit.Bearing;
                atTheFront |= Mathf.Abs(_orbit.YawOffset) > 179f;
            }

            Assert.IsTrue(atTheFront, "sustained look brings the view round to 07's face (180 degrees)");
            Assert.Greater(travelled, 360f, "and on round the other side: a full turn and more, with no stop");
        }

        [Test]
        public void FreeLook_AtTheFront_StaysPutWhileParked_OrWhileReversing()
        {
            Run(3f, Right);
            Run(0.5f, Vector2.zero);
            float offset = _orbit.YawOffset;
            Assert.Greater(Mathf.Abs(offset), 90f, "the view is well round 07");
            Park();
            Run(30f, Vector2.zero);
            Assert.AreEqual(offset, _orbit.YawOffset, Held, "parked, the view stays where the player put it");

            _speed = -3f;
            _throttle = -1f;
            Run(30f, Vector2.zero);
            Assert.AreEqual(offset, _orbit.YawOffset, Held, "reversing, the recenter never fights the player");
        }

        [Test]
        public void FreeLook_Recenter_StartsOnlyAfterTheDelay_WhileDriving_AndNeverWhileLooking()
        {
            Run(3f, Right);
            Run(0.5f, Vector2.zero);
            DriveForward(Cruise);
            Run(_tuning.RecenterDriveSeconds, Right);
            float held = _orbit.YawOffset;
            Run(_tuning.RecenterDelay * 4f, new Vector2(0.01f, 0f));
            Assert.AreEqual(0f, _orbit.RecenterWeight, "while the player is looking there is no recenter");

            Run(_tuning.RecenterDelay * 0.5f, Vector2.zero);
            Assert.AreEqual(0f, _orbit.RecenterWeight, "nor before the delay has run");
            Run(_tuning.RecenterDelay * 2f, Vector2.zero);
            Assert.Greater(_orbit.RecenterWeight, 0f, "once driving and not looking for the delay, it starts");
            Assert.AreNotEqual(Mathf.Abs(held), Mathf.Abs(_orbit.YawOffset), "and the view is on its way back");
        }

        [Test]
        public void FreeLook_Recenter_FromTheFront_NeverSnaps()
        {
            Run(3.2f, Right);
            Run(0.5f, Vector2.zero);
            Assert.Greater(Mathf.Abs(_orbit.YawOffset), 150f, "07 is seen from the front");
            DriveForward(Cruise);
            Run(_tuning.RecenterDelay + _tuning.RecenterDriveSeconds, Vector2.zero);
            Assert.LessOrEqual(FastestTurn(20f), _tuning.RecenterMaxRate + RateSlack, "never faster than the cap, never a snap");
            Run(15f, Vector2.zero);
            Assert.AreEqual(0f, _orbit.YawOffset, 0.05f, "it comes home behind 07");
        }

        [Test]
        public void Look_BackTowardTheRear_IsNeverHeldByTheLimit()
        {
            Run(8f, Right);
            float atLimit = _orbit.YawOffset;
            Run(1f, -Right);

            Assert.Less(_orbit.YawOffset, atLimit - 5f, "the view comes back round freely");
        }

        [Test]
        public void ATurnBy07UnderAHeldView_MayLeaveTheLimit_AndTheBearingStillHolds()
        {
            Run(2.2f, Right);
            Run(0.5f, Vector2.zero);
            float bearing = _orbit.Bearing;
            _roverYaw += 80f;
            Run(Frame, Vector2.zero);

            Assert.AreEqual(bearing, _orbit.Bearing, 0.5f, "07 turning never turns a view the player chose");
        }

        [Test]
        public void Recenter_EasesOutOverTheLastDegrees()
        {
            DriveForward(Cruise);
            LookAway(2f);
            Run(_tuning.RecenterDelay + _tuning.RecenterRampTime, Vector2.zero);
            while (Mathf.Abs(_orbit.YawOffset) > _tuning.RecenterEaseAngle)
            {
                StepOnce(Vector2.zero);
            }

            float atEase = FastestTurn(Frame);
            while (Mathf.Abs(_orbit.YawOffset) > 2f)
            {
                StepOnce(Vector2.zero);
            }

            Assert.Less(FastestTurn(Frame), atEase * 0.5f);
        }

        [Test]
        public void Recenter_ReturnsPitchAtMost10DegreesASecond()
        {
            DriveForward(Cruise);
            Run(1f, new Vector2(0f, 0.6f));
            Run(0.5f, Vector2.zero);
            Run(_tuning.RecenterDelay, Vector2.zero);
            float fastest = 0f;
            for (int i = 0; i < 600; i++)
            {
                float before = _orbit.Pitch;
                StepOnce(Vector2.zero);
                fastest = Mathf.Max(fastest, Mathf.Abs(_orbit.Pitch - before) / Frame);
            }

            Assert.LessOrEqual(fastest, _tuning.RecenterPitchRate + RateSlack);
            Assert.AreEqual(_tuning.DefaultPitch, _orbit.Pitch, 0.05f);
        }

        [Test]
        public void Recenter_IsCancelledByLookInput_AndNothingJumps()
        {
            DriveForward(Cruise);
            LookAway(2f);
            Run(_tuning.RecenterDelay + 1f, Vector2.zero);
            Assert.Greater(_orbit.RecenterWeight, 0f);
            float before = _orbit.Bearing;
            StepOnce(new Vector2(0.1f, 0f));
            Assert.AreEqual(0f, _orbit.RecenterWeight);
            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(before, _orbit.Bearing)), 0.5f);
            Assert.IsFalse(_orbit.IsRecentering);
        }

        [Test]
        public void Recenter_TakesTheShortWayRound()
        {
            DriveForward(Cruise);
            LookAway(3f);
            Assert.Greater(Mathf.Abs(_orbit.YawOffset), 120f);
            float before = Mathf.Abs(_orbit.YawOffset);
            Run(_tuning.RecenterDelay + 1.5f, Vector2.zero);
            Assert.Less(Mathf.Abs(_orbit.YawOffset), before);
        }

        [Test]
        public void Following_APivotSlidesIntoViewAtMost45DegreesASecond()
        {
            _turnRate = 70f;
            Assert.LessOrEqual(FastestTurn(2f), _tuning.FollowMaxRate + RateSlack);
            Assert.Greater(Mathf.Abs(_orbit.YawOffset), 10f, "The pivot outran the camera, which never whips round.");
            _turnRate = 0f;
            Run(5f, Vector2.zero);
            Assert.AreEqual(0f, _orbit.YawOffset, 0.05f, "Then it settles behind 07.");
        }

        [Test]
        public void Following_EndsWithAnyLook()
        {
            StepOnce(new Vector2(0.5f, 0f));
            Run(1f, Vector2.zero);
            Assert.IsFalse(_orbit.IsFollowing);
            float bearing = _orbit.Bearing;
            _turnRate = 60f;
            Run(1.5f, Vector2.zero);
            Assert.AreEqual(bearing, _orbit.Bearing, 1e-3f);
        }

        [Test]
        public void Following_ResumesOnceARecenterArrives()
        {
            DriveForward(Cruise);
            LookAway(1f);
            Run(_tuning.RecenterDelay + 8f, Vector2.zero);
            Assert.IsTrue(_orbit.IsFollowing);
            _turnRate = 30f;
            Run(3f, Vector2.zero);
            Assert.Less(Mathf.Abs(_orbit.YawOffset), 15f, "The camera stays behind 07 as it turns.");
        }

        [Test]
        public void RecenterButton_EasesBehindIn0Point8Seconds_EvenParked()
        {
            LookAway(2f);
            Run(0.5f, new Vector2(0f, 0.5f));
            Run(0.5f, Vector2.zero);
            float from = _orbit.YawOffset;
            StepOnce(Vector2.zero, true);
            Run(_tuning.RecenterButtonSeconds * 0.5f - Frame, Vector2.zero);
            Assert.AreEqual(from * 0.5f, _orbit.YawOffset, 3f, "Eased in and out: halfway at half time.");
            Run(_tuning.RecenterButtonSeconds * 0.5f + Frame, Vector2.zero);
            Assert.AreEqual(0f, _orbit.YawOffset, 1e-3f);
            Assert.AreEqual(_tuning.DefaultPitch, _orbit.Pitch, 1e-3f);
            Assert.IsTrue(_orbit.IsFollowing);
        }

        [Test]
        public void RecenterButton_IsNotUndoneByTheStickNudgeOfItsClick()
        {
            _stick = true;
            LookAway(2f);
            StepOnce(new Vector2(0.3f, 0f), true);
            Run(_tuning.RecenterButtonSeconds - 2f * Frame, new Vector2(0.3f, 0f));
            Assert.AreEqual(0f, _orbit.YawOffset, 0.5f, "Nearly home, nudged or not.");
        }

        [Test]
        public void Gentle_NeverRecentersByItself_ButTheButtonDoes()
        {
            _orbit.Gentle = true;
            DriveForward(Cruise);
            LookAway(2f);
            float offset = _orbit.YawOffset;
            Run(30f, Vector2.zero);
            Assert.AreEqual(offset, _orbit.YawOffset, Held);
            Assert.AreEqual(0f, _orbit.RecenterWeight);

            StepOnce(Vector2.zero, true);
            Run(_tuning.RecenterButtonSeconds, Vector2.zero);
            Assert.AreEqual(0f, _orbit.YawOffset, 1e-3f);
        }

        [Test]
        public void Gentle_KeepsTheFieldOfViewStill()
        {
            _orbit.Gentle = true;
            DriveForward(8f);
            Run(5f, Vector2.zero);
            Assert.AreEqual(_tuning.BaseFov, _orbit.FieldOfView, 1e-3f);
        }

        [Test]
        public void AdoptBearing_HoldsTheShotsBearing()
        {
            _orbit.AdoptBearing(200f);
            Assert.AreEqual(-160f, _orbit.Bearing, 1e-3f);
            Assert.IsFalse(_orbit.IsFollowing);
            _turnRate = 50f;
            Run(1f, Vector2.zero);
            Assert.AreEqual(-160f, _orbit.Bearing, 1e-3f);
        }

        [Test]
        public void Downhill_LiftsTheCameraGently()
        {
            DriveForward(6f);
            _descent = _tuning.DownhillFullAngle;
            StepOnce(Vector2.zero);
            Assert.Less(_orbit.Lift, 1f);
            Run(5f, Vector2.zero);
            Assert.AreEqual(_tuning.DownhillLift, _orbit.Lift, 0.1f);
            _descent = -10f;
            Run(5f, Vector2.zero);
            Assert.AreEqual(0f, _orbit.Lift, 0.1f, "Climbing never lowers the camera below its normal orbit.");
        }

        [Test]
        public void Downhill_StoppingOnASlope_KeepsDownhillFraming()
        {
            DriveForward(6f);
            _descent = _tuning.DownhillFullAngle;
            Run(5f, Vector2.zero);
            float drivingLift = _orbit.Lift;

            Park();
            _descent = 8f;
            Run(3f, Vector2.zero);
            Assert.AreEqual(drivingLift, _orbit.Lift, 0.05f,
                "Stopping on a downhill slope keeps the downhill framing instead of tilting up (CTRL-09).");
        }

        [Test]
        public void Lift_BlendsWithVerticalVelocityWhileAirborne()
        {
            DriveForward(6f);
            _descent = _tuning.DownhillFullAngle;
            Run(5f, Vector2.zero);
            float lift = _orbit.Lift;
            _descent = 0f;
            _grounded = false;
            Run(0.5f, Vector2.zero);
            Assert.Less(_orbit.Lift, lift - 0.5f, "Airborne lift is not frozen mid-air over a ridge crest (CTRL-09).");
            Assert.Greater(_orbit.Lift, 0.5f, "Airborne lift blends smoothly rather than snapping.");

            _descent = _tuning.DownhillFullAngle;
            Run(2f, Vector2.zero);
            Assert.Greater(_orbit.Lift, _tuning.DownhillLift - 0.5f, "Falling mid-air tilts down toward the landing.");
        }

        [Test]
        public void SlowSightseeing_WaitsLongerAndRecentersMoreGentlyThanCruise()
        {
            DriveForward(2.3f);
            LookAway(1.5f);
            float slowOffset = _orbit.YawOffset;
            Run(_tuning.RecenterDelay, Vector2.zero);
            Assert.AreEqual(0f, _orbit.RecenterWeight, 1e-4f,
                "Slow sightseeing waits longer than the cruise recenter delay (CTRL-09).");
            Assert.AreEqual(slowOffset, _orbit.YawOffset, Held);

            Run(_tuning.RecenterDelay * (_tuning.RecenterSlowDelayScale - 1f) + 0.5f, Vector2.zero);
            Assert.Greater(_orbit.RecenterWeight, 0f);
            Assert.LessOrEqual(FastestTurn(4f), _tuning.RecenterMaxRate * 0.6f,
                "Slow sightseeing recenters more gently than full cruise speed (CTRL-09).");
        }

        [Test]
        public void FieldOfView_WidensSlightlyWithSpeed()
        {
            DriveForward(8f);
            Run(5f, Vector2.zero);
            Assert.AreEqual(_tuning.BaseFov + _tuning.SpeedFovBoost, _orbit.FieldOfView, 0.1f);
            Park();
            Run(5f, Vector2.zero);
            Assert.AreEqual(_tuning.BaseFov, _orbit.FieldOfView, 0.1f);
        }
    }
}
