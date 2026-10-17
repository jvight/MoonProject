using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// Real <c>Main.unity</c> PlayMode feel-capture scenarios for the rover domain (Part 1 of CORE FEEL AUDIT):
    /// <list type="number">
    /// <item><description><c>driving</c> — flat ground: stop -> cruise -> full stop; reverse; tight turn at low speed,
    /// turn at full speed; letting go of the stick mid-turn.</description></item>
    /// <item><description><c>slopes</c> — climb at 10 deg / 20 deg / max; descend, over a crest, across a side slope.</description></item>
    /// <item><description><c>leap</c> — charge, jump, airtime and landing on regolith; landing on a rock.</description></item>
    /// <item><description><c>collisions</c> — driving into a boulder at cruise and full speed; scraping along a ruin wall.</description></item>
    /// <item><description><c>camera</c> — free orbit with mouse and pad; auto-recenter; camera against a wall and in a
    /// narrow gap; downhill framing; zoom (speed FOV).</description></item>
    /// <item><description><c>night_dawn</c> — night drive with headlamp, and dawn sweep.</description></item>
    /// </list>
    /// Every scenario drives 07 exclusively through <see cref="InputTestFixture"/> virtual devices.
    /// </summary>
    [Explicit("Automated feel capture harness for Rover scenarios; invoked by tools/feel_capture.py.")]
    [Category("FeelCapture")]
    public sealed class FeelCaptureRoverTests : InputTestFixture
    {
        private const float FrameTime = 1f / 60f;
        private const float WakeTimeout = 25f;
        private const float WakeHandoverTimeout = 8f;
        private const float SunlitDaySeconds = 210f;
        private const float EarthlitNightSeconds = 4816f;
        private const float PostEclipseDawnSeconds = 6f;

        private GameContext _context;
        private RoverController _rover;
        private RoverCameraRig _cameraRig;
        private Camera _view;
        private ITerrainQuery _terrain;
        private IRoverPlacement _placement;
        private IRoverAbilities _abilities;
        private ISunlight _sunlight;
        private UIDocument _uiDocument;
        private Gamepad _gamepad;
        private Keyboard _keyboard;
        private Mouse _mouse;
        private ScriptedDrive _drive;
        private IDisposable _awokeSub;
        private bool _awake;

        public override void Setup()
        {
            base.Setup();
            Time.captureDeltaTime = FrameTime;
            _gamepad = InputSystem.AddDevice<Gamepad>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
            _drive = new ScriptedDrive();
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
            ResetInputs();
            _rover?.SetDriveSource(null);
            base.TearDown();
        }

        [OneTimeTearDown]
        public void UnloadSceneOnce()
        {
            _awokeSub?.Dispose();
            _awokeSub = null;
            Scene scene = SceneManager.GetSceneByPath(RoverSessionScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(RoverSessionScene.SaveSlot);
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator Scenario01_Driving()
        {
            yield return EnsureBootedAndAwake();
            SetSkyClock(SunlitDaySeconds, holdingEclipse: false);

            Vector3 flatSpot = FindFlatOpenSpot(new Vector3(-45f, 0f, -110f), 40f);
            PlaceRover(flatSpot, 0f);
            yield return SettleFrames(15);

            using var rec = CreateRecorder("driving");
            rec.CaptureFrame("00_rest_flat");

            // 1. Stop -> cruise (hold forward on virtual stick)
            SetDriveStick(new Vector2(0f, 1f));
            yield return StepForSeconds(rec, 0.35f);
            rec.CaptureFrame("01_accel_squat");
            yield return StepForSeconds(rec, 2.4f);
            float cruiseSpeed = _rover.Speed;
            rec.SetMetric("cruiseSpeedMetersPerSec", cruiseSpeed);
            rec.CaptureFrame("02_cruise_speed");

            // 2. Release stick -> coast to full stop
            SetDriveStick(Vector2.zero);
            yield return StepForSeconds(rec, 0.6f);
            rec.CaptureFrame("03_coasting_rollout");
            yield return StepForSeconds(rec, 1.8f);
            rec.SetMetric("postCoastSpeedMetersPerSec", _rover.Speed);
            rec.CaptureFrame("04_full_stop");

            // 3. Reverse -> brake to stop
            SetDriveStick(new Vector2(0f, -1f));
            yield return StepForSeconds(rec, 2.2f);
            float reverseSpeed = _rover.ForwardSpeed;
            rec.SetMetric("reverseSpeedMetersPerSec", Mathf.Abs(reverseSpeed));
            rec.CaptureFrame("05_reverse_cruise");

            SetDriveStick(Vector2.zero);
            yield return StepForSeconds(rec, 1.2f);

            // 4. Tight turn at standstill & low speed
            float headingBeforePivot = _rover.Heading;
            SetDriveStick(new Vector2(1f, 0f));
            yield return StepForSeconds(rec, 1.1f);
            rec.SetMetric("pivotTurnDegIn1s", Mathf.Abs(Mathf.DeltaAngle(headingBeforePivot, _rover.Heading)));
            rec.CaptureFrame("06_pivot_turn_in_place");

            SetDriveStick(new Vector2(-1f, 0.35f));
            yield return StepForSeconds(rec, 1.4f);
            rec.CaptureFrame("07_tight_turn_low_speed");

            // 5. Turn at full speed, then let go of steering stick mid-turn
            SetDriveStick(new Vector2(0f, 1f));
            yield return StepForSeconds(rec, 2.2f);
            SetDriveStick(new Vector2(1f, 1f));
            yield return StepForSeconds(rec, 1.5f);
            rec.CaptureFrame("08_full_speed_turn_lean");

            float headingAtRelease = _rover.Heading;
            SetDriveStick(new Vector2(0f, 1f));
            yield return StepForSeconds(rec, 0.8f);
            rec.SetMetric("midTurnReleaseHeadingContinuationDeg",
                Mathf.Abs(Mathf.DeltaAngle(headingAtRelease, _rover.Heading)));
            rec.CaptureFrame("09_release_stick_mid_turn");

            SetDriveStick(Vector2.zero);
            yield return StepForSeconds(rec, 1.4f);
            rec.CaptureFrame("10_settled");

            // Orbit camera to look back along the persistent 4-wheel track trail across the crater
            Set(_gamepad.rightStick, new Vector2(1f, 0.15f));
            yield return StepForSeconds(rec, 1.15f);
            Set(_gamepad.rightStick, Vector2.zero);
            yield return StepForSeconds(rec, 0.25f);
            rec.CaptureFrame("11_track_trail_looking_back");
            rec.SaveReport();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator Scenario02_Slopes()
        {
            yield return EnsureBootedAndAwake();
            SetSkyClock(SunlitDaySeconds, holdingEclipse: false);

            using var rec = CreateRecorder("slopes");

            // 1. ~10 deg slope climb
            Vector3 spot10 = FindSlopeSpot(8f, 13f, out float angle10, out float uphillYaw10);
            PlaceRover(spot10, uphillYaw10);
            yield return SettleFrames(10);
            rec.ResetBaseline();
            SetDriveStick(new Vector2(0f, 1f));
            yield return StepForSeconds(rec, 2.4f);
            rec.SetMetric("slope10AngleDeg", angle10);
            rec.SetMetric("climbSpeed10DegMetersPerSec", _rover.Speed);
            rec.CaptureFrame("00_climb_10deg");

            // 2. ~20 deg slope climb
            SetDriveStick(Vector2.zero);
            Vector3 spot20 = FindSlopeSpot(17f, 23f, out float angle20, out float uphillYaw20);
            PlaceRover(spot20, uphillYaw20);
            yield return SettleFrames(10);
            rec.ResetBaseline();
            SetDriveStick(new Vector2(0f, 1f));
            yield return StepForSeconds(rec, 2.4f);
            rec.SetMetric("slope20AngleDeg", angle20);
            rec.SetMetric("climbSpeed20DegMetersPerSec", _rover.Speed);
            rec.CaptureFrame("01_climb_20deg");

            // 3. Max (~27-34 deg) slope climb + parked creep check
            SetDriveStick(Vector2.zero);
            Vector3 spotMax = FindSlopeSpot(26f, 35f, out float angleMax, out float uphillYawMax);
            PlaceRover(spotMax, uphillYawMax);
            yield return SettleFrames(10);
            rec.ResetBaseline();
            Vector3 parkStart = _rover.Position;
            yield return StepForSeconds(rec, 1.2f);
            rec.SetMetric("parkedCreepMaxSlopeMeters", Vector3.Distance(parkStart, _rover.Position));
            rec.CaptureFrame("02_parked_steep_slope");

            SetDriveStick(new Vector2(0f, 1f));
            yield return StepForSeconds(rec, 2.6f);
            rec.SetMetric("slopeMaxAngleDeg", angleMax);
            rec.SetMetric("climbSpeedMaxDegMetersPerSec", _rover.Speed);
            rec.CaptureFrame("03_climb_max_slope");

            // 4. Descend steep slope & crest a ridge
            SetDriveStick(Vector2.zero);
            float downhillYaw = Mathf.Repeat(uphillYaw20 + 180f, 360f);
            Vector3 descendStart = spot20 + Quaternion.Euler(0f, uphillYaw20, 0f) * Vector3.forward * 18f;
            PlaceRover(descendStart, downhillYaw);
            yield return SettleFrames(10);
            rec.ResetBaseline();
            SetDriveStick(new Vector2(0f, 1f));
            yield return StepForSeconds(rec, 1.5f);
            rec.CaptureFrame("04_descend_slope");
            yield return StepForSeconds(rec, 1.5f);
            rec.SetMetric("descendPeakSpeedMetersPerSec", _rover.Speed);
            rec.CaptureFrame("05_over_crest");

            // 5. Traverse across a steep side-slope (90 deg to uphill)
            SetDriveStick(Vector2.zero);
            float crossSlopeYaw = Mathf.Repeat(uphillYawMax + 90f, 360f);
            PlaceRover(spotMax, crossSlopeYaw);
            yield return SettleFrames(10);
            rec.ResetBaseline();
            Vector3 crossStart = _rover.Position;
            Vector3 crossRight = Quaternion.Euler(0f, crossSlopeYaw, 0f) * Vector3.right;
            SetDriveStick(new Vector2(0f, 0.85f));
            yield return StepForSeconds(rec, 2.6f);
            float downhillDrift = Mathf.Abs(Vector3.Dot(_rover.Position - crossStart, crossRight));
            rec.SetMetric("sideSlopeDownhillDriftMeters", downhillDrift);
            rec.CaptureFrame("06_side_slope_traverse");

            SetDriveStick(Vector2.zero);
            yield return StepForSeconds(rec, 1.0f);
            rec.CaptureFrame("07_side_slope_stopped");
            rec.SaveReport();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator Scenario03_Leap()
        {
            yield return EnsureBootedAndAwake();
            SetSkyClock(SunlitDaySeconds, holdingEclipse: false);
            _abilities.Grant(RoverAbility.HoverJump);

            Vector3 flatSpot = FindFlatOpenSpot(new Vector3(-55f, 0f, -95f), 35f);
            PlaceRover(flatSpot, 20f);
            yield return SettleFrames(12);

            using var rec = CreateRecorder("leap");
            float lastImpactSpeed = 0f;
            float lastAirTime = 0f;
            using IDisposable landSub = _context.Events.Subscribe<RoverLanded>(e =>
            {
                lastImpactSpeed = e.ImpactSpeed;
                lastAirTime = e.AirTime;
            });

            rec.CaptureFrame("00_pre_charge");

            // 1. Hold Space + forward stick to charge jump on regolith
            SetDriveStick(new Vector2(0f, 0.65f));
            SetJumpHeld(true);
            yield return StepForSeconds(rec, 0.30f);
            rec.CaptureFrame("01_charge_half_crouch");
            yield return StepForSeconds(rec, 0.42f);
            rec.SetMetric("jumpChargeLevel", _rover.JumpCharge);
            rec.CaptureFrame("02_charge_full_crouch");

            // 2. Release Space -> leap, measure apex, airtime, and cushioned landing on regolith
            SetJumpHeld(false);
            yield return StepForSeconds(rec, 0.18f);
            rec.CaptureFrame("03_liftoff_stretch");

            float maxApexAboveGround = 0f;
            float elapsed = 0f;
            bool apexCaptured = false;
            bool descentCaptured = false;
            while (elapsed < 4.8f)
            {
                float heightAboveGround = _rover.Position.y - _terrain.SampleHeight(_rover.Position.x, _rover.Position.z);
                if (heightAboveGround > maxApexAboveGround)
                {
                    maxApexAboveGround = heightAboveGround;
                }

                if (!apexCaptured && elapsed >= 1.15f)
                {
                    apexCaptured = true;
                    rec.CaptureFrame("04_airtime_apex");
                }

                if (!descentCaptured && elapsed >= 2.2f && _rover.Velocity.y < -0.5f)
                {
                    descentCaptured = true;
                    rec.SetMetric("descentVerticalSpeedInCushionZone", _rover.Velocity.y);
                    rec.CaptureFrame("05_cushioned_descent");
                }

                if (elapsed > 0.6f && _rover.IsGrounded)
                {
                    break;
                }

                yield return StepOneFrame(rec);
                elapsed += FrameTime;
            }

            yield return StepForSeconds(rec, 0.06f);
            rec.CaptureFrame("06_regolith_touchdown_puff");
            rec.SetMetric("regolithLandingSurface", (float)_rover.ContactSurface);
            yield return StepForSeconds(rec, 0.74f);
            rec.SetMetric("regolithLeapApexMeters", maxApexAboveGround);
            rec.SetMetric("regolithAirTimeSeconds", lastAirTime);
            rec.SetMetric("regolithLandingImpactSpeed", lastImpactSpeed);

            // 3. Leap onto a boulder / rock collider on Layers.Prop
            SetDriveStick(Vector2.zero);
            if (TryFindBoulderTarget(out Bounds boulderBounds))
            {
                Vector3 toBoulder = Vector3.forward;
                float boulderExtent = Mathf.Max(boulderBounds.extents.x, boulderBounds.extents.z);
                Vector3 launchPos = boulderBounds.center - toBoulder * (boulderExtent + 3.6f);
                launchPos.y = _terrain.SampleHeight(launchPos.x, launchPos.z);
                PlaceRover(launchPos, 0f);
                yield return SettleFrames(10);
                Set(_gamepad.rightStick, new Vector2(0f, -0.55f));
                yield return SettleFrames(22);
                Set(_gamepad.rightStick, Vector2.zero);
                rec.ResetBaseline();

                SetDriveStick(new Vector2(0f, 0.45f));
                SetJumpHeld(true);
                yield return StepForSeconds(rec, 0.62f);
                SetJumpHeld(false);

                float rockFlight = 0f;
                bool capturedLeapTowardRock = false;
                while (rockFlight < 4.5f)
                {
                    float remDist = Vector3.Dot(boulderBounds.center - _rover.Position, toBoulder);
                    float desiredSpeed = Mathf.Clamp(remDist * 0.9f, -2.5f, 2.5f);
                    float stickY = Mathf.Clamp((desiredSpeed - _rover.ForwardSpeed) * 0.7f, -1f, 0.55f);
                    SetDriveStick(new Vector2(0f, stickY));

                    if (!capturedLeapTowardRock && rockFlight >= 1.15f)
                    {
                        capturedLeapTowardRock = true;
                        rec.CaptureFrame("07_leap_toward_rock");
                    }

                    if (rockFlight > 0.6f && _rover.IsGrounded)
                    {
                        break;
                    }

                    yield return StepOneFrame(rec);
                    rockFlight += FrameTime;
                }

                SetDriveStick(Vector2.zero);
                yield return StepForSeconds(rec, 0.06f);
                float rockTilt = Vector3.Angle(_rover.Rotation * Vector3.up, Vector3.up);
                rec.SetMetric("rockLandingTiltDeg", rockTilt);
                rec.SetMetric("rockLandingSurface", (float)_rover.ContactSurface);
                rec.CaptureFrame("08_landed_on_rock");
                yield return StepForSeconds(rec, 0.74f);
            }

            SetDriveStick(Vector2.zero);
            rec.SaveReport();
            _abilities.Revoke(RoverAbility.HoverJump);
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator Scenario04_Collisions()
        {
            yield return EnsureBootedAndAwake();
            SetSkyClock(SunlitDaySeconds, holdingEclipse: false);

            using var rec = CreateRecorder("collisions");

            bool foundBoulder = TryFindBoulderTarget(out Bounds boulder);
            Assert.IsTrue(foundBoulder, "Main scene must contain a prop boulder/collider for collision testing.");

            // 1. Drive into boulder at cruise speed
            float boulderRadius = Mathf.Max(boulder.extents.x, boulder.extents.z);
            Vector3 dir = Vector3.forward;
            Vector3 startCruise = boulder.center - dir * (boulderRadius + 8.5f);
            startCruise.y = _terrain.SampleHeight(startCruise.x, startCruise.z);
            PlaceRover(startCruise, 0f);
            yield return SettleFrames(10);
            rec.ResetBaseline();

            SetDriveStick(new Vector2(0f, 0.85f));
            yield return StepForSeconds(rec, 1.25f);
            float preImpactCruise = _rover.Speed;
            rec.SetMetric("boulderPreImpactCruiseSpeed", preImpactCruise);
            rec.CaptureFrame("00_boulder_cruise_approach");

            yield return StepForSeconds(rec, 0.9f);
            rec.SetMetric("boulderPostImpactCruiseSpeed", _rover.Speed);
            rec.CaptureFrame("01_boulder_cruise_impact");
            yield return StepForSeconds(rec, 0.8f);
            rec.CaptureFrame("02_boulder_cruise_pinned");

            // 2. Drive into boulder at full / boost speed
            SetDriveStick(Vector2.zero);
            _abilities.Grant(RoverAbility.BoostCoils);
            Vector3 startFull = boulder.center - dir * (boulderRadius + 13.5f);
            startFull.y = _terrain.SampleHeight(startFull.x, startFull.z);
            PlaceRover(startFull, 0f);
            yield return SettleFrames(10);
            rec.ResetBaseline();

            SetDriveStick(new Vector2(0f, 1f));
            yield return StepForSeconds(rec, 1.85f);
            float preImpactFull = _rover.Speed;
            rec.SetMetric("boulderPreImpactFullSpeed", preImpactFull);
            rec.CaptureFrame("03_boulder_full_speed_approach");

            yield return StepForSeconds(rec, 0.85f);
            rec.SetMetric("boulderPostImpactFullSpeed", _rover.Speed);
            rec.CaptureFrame("04_boulder_full_speed_impact");
            _abilities.Revoke(RoverAbility.BoostCoils);

            // 3. Scrape along a ruin / structure wall at a glancing angle
            SetDriveStick(Vector2.zero);
            if (TryFindWallTarget(out Bounds wallBounds, out Vector3 wallAlong, out Vector3 wallOutward))
            {
                Vector3 scrapeStart = wallBounds.center
                    - wallAlong * (Mathf.Max(wallBounds.extents.x, wallBounds.extents.z) + 3.5f)
                    + wallOutward * 1.15f;
                scrapeStart.y = _terrain.SampleHeight(scrapeStart.x, scrapeStart.z);
                Vector3 angledDir = (wallAlong - wallOutward * 0.38f).normalized;
                float scrapeYaw = Quaternion.LookRotation(angledDir, Vector3.up).eulerAngles.y;
                PlaceRover(scrapeStart, scrapeYaw);
                yield return SettleFrames(10);
                rec.ResetBaseline();

                SetDriveStick(new Vector2(0f, 1f));
                yield return StepForSeconds(rec, 1.1f);
                rec.CaptureFrame("05_wall_scrape_contact");
                yield return StepForSeconds(rec, 1.4f);
                rec.SetMetric("wallScrapeSpeedMetersPerSec", _rover.Speed);
                rec.CaptureFrame("06_wall_scrape_pinning");
                yield return StepForSeconds(rec, 1.2f);
                rec.CaptureFrame("07_wall_scrape_after_2_5s");
            }

            SetDriveStick(Vector2.zero);
            rec.SaveReport();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator Scenario05_Camera()
        {
            yield return EnsureBootedAndAwake();
            SetSkyClock(SunlitDaySeconds, holdingEclipse: false);

            Vector3 flatSpot = FindFlatOpenSpot(new Vector3(-40f, 0f, -105f), 35f);
            PlaceRover(flatSpot, 0f);
            yield return SettleFrames(12);

            using var rec = CreateRecorder("camera");
            rec.CaptureFrame("00_chase_default");

            // 1. Free orbit with Mouse while driving
            float yawBeforeMouse = _view.transform.eulerAngles.y;
            SetDriveStick(new Vector2(0f, 0.75f));
            for (int i = 0; i < 60; i++)
            {
                Set(_mouse.delta, new Vector2(18f, -3f));
                yield return StepOneFrame(rec);
            }

            Set(_mouse.delta, Vector2.zero);
            float mouseOrbited = Mathf.Abs(Mathf.DeltaAngle(yawBeforeMouse, _view.transform.eulerAngles.y));
            rec.SetMetric("mouseOrbitDeltaDeg", mouseOrbited);
            rec.CaptureFrame("01_mouse_free_orbit");

            // 2. Free orbit with Gamepad Right Stick
            float yawBeforePad = _view.transform.eulerAngles.y;
            Set(_gamepad.rightStick, new Vector2(-0.85f, 0.35f));
            yield return StepForSeconds(rec, 1.2f);
            Set(_gamepad.rightStick, Vector2.zero);
            float padOrbited = Mathf.Abs(Mathf.DeltaAngle(yawBeforePad, _view.transform.eulerAngles.y));
            rec.SetMetric("padOrbitDeltaDeg", padOrbited);
            rec.CaptureFrame("02_gamepad_free_orbit");

            // 3. Auto-recenter while driving forward with look stick released
            float recenterWaitStart = Time.time;
            yield return StepForSeconds(rec, 3.8f);
            float yawLagMidRecenter = Mathf.Abs(Mathf.DeltaAngle(_rover.Heading, _view.transform.eulerAngles.y));
            rec.SetMetric("yawLagAfter3_8sRecenterDeg", yawLagMidRecenter);
            rec.CaptureFrame("03_auto_recenter_swing");
            yield return StepForSeconds(rec, 1.2f);
            rec.SetMetric("recenterElapsedSeconds", Time.time - recenterWaitStart);
            rec.CaptureFrame("04_auto_recenter_complete");

            // 4. Camera against a wall / prop and in a narrow gap
            SetDriveStick(Vector2.zero);
            if (TryFindWallTarget(out Bounds wall, out Vector3 along, out Vector3 outward))
            {
                float wallExtent = Mathf.Max(wall.extents.x, wall.extents.z);
                Vector3 nearWall = wall.center + outward * (wallExtent + 1.4f);
                nearWall.y = _terrain.SampleHeight(nearWall.x, nearWall.z);
                float faceAwayFromWall = Quaternion.LookRotation(outward, Vector3.up).eulerAngles.y;
                PlaceRover(nearWall, faceAwayFromWall);
                yield return SettleFrames(12);
                rec.ResetBaseline();

                float distAgainstWall = Vector3.Distance(_view.transform.position, _rover.Position);
                rec.SetMetric("cameraDistanceAgainstWallMeters", distAgainstWall);
                rec.CaptureFrame("05_camera_against_wall");

                Set(_gamepad.rightStick, new Vector2(0.9f, 0f));
                yield return StepForSeconds(rec, 1.1f);
                Set(_gamepad.rightStick, Vector2.zero);
                rec.CaptureFrame("06_camera_narrow_gap_sweep");
            }

            // 5. Downhill framing + stopping on downhill slope
            Vector3 slopeSpot = FindSlopeSpot(18f, 26f, out _, out float uphillYaw);
            float downhillYaw = Mathf.Repeat(uphillYaw + 180f, 360f);
            PlaceRover(slopeSpot, downhillYaw);
            yield return SettleFrames(10);
            rec.ResetBaseline();
            SetDriveStick(new Vector2(0f, 0.9f));
            yield return StepForSeconds(rec, 1.8f);
            rec.SetMetric("downhillCameraPitchDrivingDeg", _view.transform.eulerAngles.x);
            rec.CaptureFrame("07_downhill_framing_driving");

            SetDriveStick(Vector2.zero);
            yield return StepForSeconds(rec, 1.4f);
            rec.SetMetric("downhillCameraPitchStoppedDeg", _view.transform.eulerAngles.x);
            rec.CaptureFrame("08_downhill_framing_stopped");

            // 6. Speed FOV widening (zoom) during boost cruise
            _abilities.Grant(RoverAbility.BoostCoils);
            PlaceRover(flatSpot, 0f);
            yield return SettleFrames(8);
            rec.ResetBaseline();
            float restFov = _view.fieldOfView;
            SetDriveStick(new Vector2(0f, 1f));
            yield return StepForSeconds(rec, 2.5f);
            rec.SetMetric("restFovDeg", restFov);
            rec.SetMetric("boostCruiseFovDeg", _view.fieldOfView);
            rec.CaptureFrame("09_boost_speed_fov_zoom");

            SetDriveStick(Vector2.zero);
            _abilities.Revoke(RoverAbility.BoostCoils);
            rec.SaveReport();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator Scenario08_NightDawn()
        {
            yield return EnsureBootedAndAwake();

            IWorldLayout layout = _context.Get<IWorldLayout>();
            Vector3 toEarth = Vector3.ProjectOnPlane(layout.EarthDirection, Vector3.up).normalized;
            float earthYaw = Quaternion.LookRotation(toEarth, Vector3.up).eulerAngles.y;
            Vector3 nightSpot = FindFlatOpenSpot(layout.BasePosition - toEarth * 45f, 35f);

            // 1. Earthlit Night drive with headlamp
            SetSkyClock(EarthlitNightSeconds, holdingEclipse: false);
            PlaceRover(nightSpot, earthYaw);
            yield return SettleFrames(18);

            using var rec = CreateRecorder("night_dawn");
            float nightSun = _sunlight != null ? _sunlight.SunlightAt(_rover.Position) : 0f;
            rec.SetMetric("nightSunlight", nightSun);
            rec.CaptureFrame("00_night_parked_headlamp_motes");

            SetDriveStick(new Vector2(0f, 0.9f));
            yield return StepForSeconds(rec, 2.0f);
            rec.CaptureFrame("01_night_drive_headlamp_cone");

            SetDriveStick(new Vector2(0.55f, 0.9f));
            yield return StepForSeconds(rec, 1.8f);
            rec.CaptureFrame("02_night_turn_dust_and_rear_vent");

            SetDriveStick(new Vector2(0f, 0.85f));
            yield return StepForSeconds(rec, 1.6f);
            rec.CaptureFrame("03_night_earthlit_horizon");

            // 2. Post-eclipse Dawn sweep & sunlit = 0.5 transition
            SetDriveStick(Vector2.zero);
            PlaceRover(nightSpot, earthYaw);
            SetSkyClock(PostEclipseDawnSeconds, holdingEclipse: false);
            yield return SettleFrames(8);
            rec.ResetBaseline();
            rec.CaptureFrame("04_dawn_diamond_ring_breakout");

            SetDriveStick(new Vector2(0f, 0.85f));
            yield return StepForSeconds(rec, 2.2f);
            rec.CaptureFrame("05_dawn_sweep_transition");

            yield return StepForSeconds(rec, 2.5f);
            rec.CaptureFrame("06_dawn_long_shadows");

            SetSkyClock(SunlitDaySeconds, holdingEclipse: false);
            yield return StepForSeconds(rec, 2.0f);
            float daySun = _sunlight != null ? _sunlight.SunlightAt(_rover.Position) : 1f;
            rec.SetMetric("daySunlight", daySun);
            rec.CaptureFrame("07_full_low_sun_after_dawn");

            SetDriveStick(Vector2.zero);
            rec.SaveReport();
        }

        private IEnumerator EnsureBootedAndAwake()
        {
            Scene existing = SceneManager.GetSceneByPath(RoverSessionScene.ScenePath);
            if (!existing.IsValid() || !existing.isLoaded || _rover == null)
            {
                BootstrapHarness.DeleteSaveFiles(RoverSessionScene.SaveSlot);
#if UNITY_EDITOR
                AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(
                    RoverSessionScene.ScenePath,
                    new LoadSceneParameters(LoadSceneMode.Single));
                while (!loading.isDone)
                {
                    yield return null;
                }
#else
                throw new NotSupportedException("FeelCaptureRoverTests requires the Unity Editor.");
#endif
                Scene scene = SceneManager.GetSceneByPath(RoverSessionScene.ScenePath);
                GameBootstrap bootstrap = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                    if (_uiDocument == null)
                    {
                        _uiDocument = root.GetComponentInChildren<UIDocument>(true);
                    }

                    foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (mb != null && mb.GetType().Name == "BaseProgression")
                        {
                            mb.enabled = false;
                        }
                    }
                }

                Assert.IsNotNull(bootstrap, "Main.unity must contain GameBootstrap.");
                _context = bootstrap.Context;
                _rover = (RoverController)_context.Get<IRoverState>();
                _cameraRig = (RoverCameraRig)_context.Get<IViewCamera>();
                _view = _cameraRig.Camera;
                _terrain = _context.Get<ITerrainQuery>();
                _placement = _context.Get<IRoverPlacement>();
                _abilities = _context.Get<IRoverAbilities>();
                _context.TryGet(out _sunlight);

                _awake = false;
                _awokeSub?.Dispose();
                _awokeSub = _context.Events.Subscribe<RoverAwoke>(_ => _awake = true);

                float deadline = Time.time + WakeTimeout;
                while (!_awake && Time.time < deadline)
                {
                    yield return null;
                }
            }

            _context.Input.Disable();
            _context.Input.Enable();
            _context.Input.Menu.Disable();
            _context.Input.Menu.Enable();
            _drive ??= new ScriptedDrive();
            _rover.SetDriveSource(_drive);
            ResetInputs();
            if (_cameraRig.Opening != null && _cameraRig.Opening.IsActive)
            {
                Set(_gamepad.rightStick, new Vector2(0.8f, 0f));
                yield return WaitSeconds(0.3f);
                Set(_gamepad.rightStick, Vector2.zero);
                float handoverDeadline = Time.time + WakeHandoverTimeout;
                while (_cameraRig.Opening.IsActive && Time.time < handoverDeadline)
                {
                    yield return null;
                }
            }
        }

        private FeelCaptureRecorder CreateRecorder(string scenario)
        {
            return new FeelCaptureRecorder(
                scenario,
                _view,
                _rover,
                _terrain,
                _uiDocument);
        }

        private void PlaceRover(Vector3 position, float yawDeg)
        {
            ResetInputs();
            Vector3 p = position;
            p.y = _terrain.SampleHeight(p.x, p.z);
            _placement.PlaceAt(p, Quaternion.Euler(0f, yawDeg, 0f));
            _cameraRig.Snap();
        }

        private void SetDriveStick(Vector2 stick)
        {
            if (_gamepad != null)
            {
                Set(_gamepad.leftStick, stick);
            }

            if (_drive != null)
            {
                _drive.Drive = stick;
            }
        }

        private void SetJumpHeld(bool held)
        {
            if (_keyboard != null)
            {
                if (held)
                {
                    Press(_keyboard.spaceKey);
                }
                else
                {
                    Release(_keyboard.spaceKey);
                }
            }

            if (_drive != null)
            {
                _drive.JumpHeld = held;
            }
        }

        private void ResetInputs()
        {
            if (_drive != null)
            {
                _drive.Drive = Vector2.zero;
                _drive.JumpHeld = false;
            }

            if (_gamepad != null)
            {
                Set(_gamepad.leftStick, Vector2.zero);
                Set(_gamepad.rightStick, Vector2.zero);
            }

            if (_mouse != null)
            {
                Set(_mouse.delta, Vector2.zero);
            }

            if (_keyboard != null)
            {
                Release(_keyboard.spaceKey);
                Release(_keyboard.wKey);
                Release(_keyboard.aKey);
                Release(_keyboard.sKey);
                Release(_keyboard.dKey);
            }
        }

        private void SetSkyClock(float timeSeconds, bool holdingEclipse)
        {
            if (_sunlight == null)
            {
                return;
            }

            MethodInfo method = _sunlight.GetType().GetMethod(
                "SetClockTime",
                BindingFlags.Instance | BindingFlags.Public);
            method?.Invoke(_sunlight, new object[] { timeSeconds, holdingEclipse, true });
        }

        private Vector3 FindFlatOpenSpot(Vector3 center, float searchRadius)
        {
            Vector3 best = center;
            float bestSlope = 90f;
            for (float r = 0f; r <= searchRadius; r += 8f)
            {
                int steps = r < 1f ? 1 : 12;
                for (int i = 0; i < steps; i++)
                {
                    float ang = i * Mathf.PI * 2f / steps;
                    float x = center.x + Mathf.Cos(ang) * r;
                    float z = center.z + Mathf.Sin(ang) * r;
                    if (!_terrain.IsDrivable(x, z))
                    {
                        continue;
                    }

                    float slope = Vector3.Angle(_terrain.SampleNormal(x, z), Vector3.up);
                    float y = _terrain.SampleHeight(x, z);
                    Vector3 probe = new Vector3(x, y + 1.2f, z);
                    if (Physics.CheckSphere(probe, 6f, Layers.PropMask, QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }

                    if (slope < bestSlope)
                    {
                        bestSlope = slope;
                        best = new Vector3(x, y, z);
                        if (slope < 3.5f)
                        {
                            return best;
                        }
                    }
                }
            }

            best.y = _terrain.SampleHeight(best.x, best.z);
            return best;
        }

        private Vector3 FindSlopeSpot(float minDeg, float maxDeg, out float foundAngle, out float uphillYaw)
        {
            float targetMid = (minDeg + maxDeg) * 0.5f;
            Vector3 best = new Vector3(120f, 0f, 120f);
            float bestDiff = 999f;
            foundAngle = targetMid;
            uphillYaw = 0f;

            for (float radius = 80f; radius <= 335f; radius += 12f)
            {
                for (int b = 0; b < 24; b++)
                {
                    float rad = b * Mathf.PI * 2f / 24f;
                    float x = Mathf.Cos(rad) * radius;
                    float z = Mathf.Sin(rad) * radius;
                    if (!_terrain.IsDrivable(x, z))
                    {
                        continue;
                    }

                    Vector3 normal = _terrain.SampleNormal(x, z);
                    float slope = Vector3.Angle(normal, Vector3.up);
                    if (slope < minDeg - 2f || slope > maxDeg + 2f)
                    {
                        continue;
                    }

                    float y = _terrain.SampleHeight(x, z);
                    if (Physics.CheckSphere(new Vector3(x, y + 1.2f, z), 4.5f, Layers.PropMask,
                            QueryTriggerInteraction.Ignore))
                    {
                        continue;
                    }

                    float diff = Mathf.Abs(slope - targetMid);
                    if (diff < bestDiff)
                    {
                        bestDiff = diff;
                        best = new Vector3(x, y, z);
                        foundAngle = slope;
                        Vector3 downhill = Vector3.ProjectOnPlane(normal, Vector3.up);
                        Vector3 uphill = downhill.sqrMagnitude > 1e-4f ? -downhill.normalized : Vector3.forward;
                        uphillYaw = Quaternion.LookRotation(uphill, Vector3.up).eulerAngles.y;
                        if (diff < 1.2f)
                        {
                            return best;
                        }
                    }
                }
            }

            best.y = _terrain.SampleHeight(best.x, best.z);
            return best;
        }

        private bool TryFindBoulderTarget(out Bounds bestBounds)
        {
            bestBounds = default;
            float bestScore = float.MaxValue;
            Scene scene = SceneManager.GetSceneByPath(RoverSessionScene.ScenePath);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Collider[] colliders = root.GetComponentsInChildren<Collider>(false);
                for (int i = 0; i < colliders.Length; i++)
                {
                    Collider col = colliders[i];
                    if (col == null || col.isTrigger || col.gameObject.layer != Layers.Prop)
                    {
                        continue;
                    }

                    if (GroundModel.ClassifySurface(col) != GroundSurface.Rock
                        || col.gameObject.name.IndexOf("Boulder", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    Bounds b = col.bounds;
                    float maxHoriz = Mathf.Max(b.size.x, b.size.z);
                    if (maxHoriz < 1.8f || maxHoriz > 6.5f || b.size.y < 0.6f || b.size.y > 3.5f)
                    {
                        continue;
                    }

                    if (!_terrain.IsDrivable(b.center.x, b.center.z))
                    {
                        continue;
                    }

                    if (_sunlight != null && _sunlight.SunlightAt(b.center) < 0.5f)
                    {
                        continue;
                    }

                    float slope = Vector3.Angle(_terrain.SampleNormal(b.center.x, b.center.z), Vector3.up);
                    if (slope > 8f)
                    {
                        continue;
                    }

                    float distFromOrigin = new Vector2(b.center.x, b.center.z).magnitude;
                    if (distFromOrigin < 48f)
                    {
                        continue;
                    }

                    Vector3 topProbe = new Vector3(b.center.x, b.max.y + 1.5f, b.center.z);
                    if (!Physics.SphereCast(topProbe, 0.25f, Vector3.down, out RaycastHit topHit, b.size.y + 2.5f,
                            Layers.PropMask, QueryTriggerInteraction.Ignore)
                        || topHit.collider != col)
                    {
                        continue;
                    }

                    float topSlope = Vector3.Angle(topHit.normal, Vector3.up);
                    if (topSlope > 28f)
                    {
                        continue;
                    }

                    Collider[] backHits = Physics.OverlapSphere(
                        b.center - Vector3.forward * 5.2f,
                        5.0f,
                        Layers.PropMask,
                        QueryTriggerInteraction.Ignore);
                    bool backClear = true;
                    for (int h = 0; h < backHits.Length; h++)
                    {
                        if (backHits[h] != null && backHits[h] != col)
                        {
                            backClear = false;
                            break;
                        }
                    }

                    if (!backClear)
                    {
                        continue;
                    }

                    float score = topSlope * 4f + slope * 6f - maxHoriz * 8f + distFromOrigin * 0.05f;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestBounds = b;
                    }
                }
            }

            return bestScore < float.MaxValue;
        }

        private bool TryFindWallTarget(out Bounds wallBounds, out Vector3 wallAlong, out Vector3 wallOutward)
        {
            wallBounds = default;
            wallAlong = Vector3.forward;
            wallOutward = Vector3.right;
            float bestScore = float.MaxValue;

            Scene scene = SceneManager.GetSceneByPath(RoverSessionScene.ScenePath);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Collider[] colliders = root.GetComponentsInChildren<Collider>(false);
                for (int i = 0; i < colliders.Length; i++)
                {
                    Collider col = colliders[i];
                    if (col == null || col.isTrigger || col.gameObject.layer != Layers.Prop)
                    {
                        continue;
                    }

                    Bounds b = col.bounds;
                    float horiz = Mathf.Max(b.size.x, b.size.z);
                    if (horiz < 2.8f || b.size.y < 2.0f)
                    {
                        continue;
                    }

                    if (!_terrain.IsDrivable(b.center.x, b.center.z))
                    {
                        continue;
                    }

                    float slope = Vector3.Angle(_terrain.SampleNormal(b.center.x, b.center.z), Vector3.up);
                    if (slope > 9f)
                    {
                        continue;
                    }

                    Transform t = col.transform;
                    Vector3 along = Vector3.ProjectOnPlane(t.forward, Vector3.up);
                    if (along.sqrMagnitude < 1e-4f)
                    {
                        along = Vector3.forward;
                    }
                    else
                    {
                        along.Normalize();
                    }

                    Vector3 outward = Vector3.Cross(Vector3.up, along).normalized;
                    float score = slope * 10f + Mathf.Abs(horiz - 5f);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        wallBounds = b;
                        wallAlong = along;
                        wallOutward = outward;
                    }
                }
            }

            return bestScore < float.MaxValue;
        }

        private static IEnumerator SettleFrames(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                yield return null;
            }
        }

        private static IEnumerator WaitSeconds(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        private static IEnumerator StepOneFrame(FeelCaptureRecorder rec)
        {
            yield return null;
            rec.StepFrame();
        }

        private static IEnumerator StepForSeconds(FeelCaptureRecorder rec, float seconds)
        {
            int frames = Mathf.Max(1, Mathf.RoundToInt(seconds / FrameTime));
            for (int i = 0; i < frames; i++)
            {
                yield return null;
                rec.StepFrame();
            }
        }
    }
}
