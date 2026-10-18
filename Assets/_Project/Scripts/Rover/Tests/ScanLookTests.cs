using NUnit.Framework;
using UnityEngine;
using MoonProject.Rover;

namespace MoonProject.Rover.Tests
{
    /// <summary>07's eye through a scan (0.5.5 §1.2): the lid lifts fully, the fan sweeps and swells, a pulse per
    /// quarter, the single flare at the end, and everything settles on letting go.</summary>
    public sealed class ScanLookTests
    {
        private const float Frame = 1f / 120f;

        private readonly ScanLookSettings _settings = new ScanLookSettings();

        private static void Run(ScanLook look, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                look.Step(Frame);
            }
        }

        [Test]
        public void AScan_LiftsTheLidFully_AndSweepsTheFan_Eased()
        {
            var look = new ScanLook(_settings);
            look.Begin();
            look.Step(Frame);
            Assert.Less(look.Open, 0.2f, "The eye wakes, not snaps.");
            Run(look, 1f);
            Assert.Greater(look.Open, 0.95f);
            Assert.Greater(look.Fan, 0.95f);
            Assert.Greater(look.EyeGlow, 1f, "Awake while learning.");

            float least = float.PositiveInfinity;
            float most = float.NegativeInfinity;
            for (float t = 0f; t < _settings.SweepSeconds; t += Frame)
            {
                look.Step(Frame);
                least = Mathf.Min(least, look.Sweep);
                most = Mathf.Max(most, look.Sweep);
            }

            Assert.AreEqual(_settings.SweepDegrees, most, 0.2f, "The fan sweeps across the fragment...");
            Assert.AreEqual(-_settings.SweepDegrees, least, 0.2f, "...and back.");
        }

        [Test]
        public void EachQuarter_PulsesTheEye_AndTheFanSwellsTowardTheEnd()
        {
            var look = new ScanLook(_settings);
            look.Begin();
            Run(look, 3f);
            float steady = look.EyeGlow;
            float early = look.Fan;
            look.Quarter(0.75f);
            look.Step(Frame);
            Assert.Greater(look.EyeGlow, steady, "A pulse.");
            Assert.Greater(look.Fan, early, "Brighter as the drawing nears complete.");
            Run(look, _settings.PulseSeconds + 0.05f);
            Assert.AreEqual(steady, look.EyeGlow, 1e-3f, "The pulse fades.");
        }

        [Test]
        public void LettingGo_FadesTheFan_AndSettlesTheLid_WithoutAFlare()
        {
            var look = new ScanLook(_settings);
            look.Begin();
            Run(look, 1f);
            look.LetGo();
            float peak = 0f;
            for (float t = 0f; t < 1.5f; t += Frame)
            {
                look.Step(Frame);
                peak = Mathf.Max(peak, look.EyeGlow);
            }

            Assert.LessOrEqual(peak, _settings.EyeGlow + 1e-3f, "No flare on a let-go.");
            Assert.Less(look.Open, 0.01f);
            Assert.Less(look.Fan, 0.01f);
            Assert.IsFalse(look.IsScanning);
        }

        [Test]
        public void Learning_FlaresTheLensOnce_ForATenthOfASecond()
        {
            var look = new ScanLook(_settings);
            look.Begin();
            Run(look, 1f);
            look.Learned();
            float peak = 0f;
            for (float t = 0f; t < _settings.FlareSeconds; t += Frame)
            {
                look.Step(Frame);
                peak = Mathf.Max(peak, look.EyeGlow);
            }

            Assert.Greater(peak, _settings.EyeGlow + 0.5f * _settings.Flare, "The lens flares.");
            look.Step(Frame);
            Assert.LessOrEqual(look.EyeGlow, _settings.EyeGlow, "Once, and briefly.");
            Run(look, 1.5f);
            Assert.Less(look.Open, 0.01f, "Then the eye settles back.");
        }

        [Test]
        public void Scanning_OscillatesScannerHeadYawAndPitchInStepWithSweep_AndSettlesOnLetGo()
        {
            var look = new ScanLook(_settings);
            look.Begin(0f, 2.0f);
            Run(look, 0.6f);

            float minYaw = float.PositiveInfinity;
            float maxYaw = float.NegativeInfinity;
            float minPitch = float.PositiveInfinity;
            float maxPitch = float.NegativeInfinity;

            for (float t = 0f; t < _settings.SweepSeconds; t += Frame)
            {
                if (Mathf.Abs(t - 0.5f) < Frame * 0.5f)
                {
                    look.Quarter(0.5f);
                }

                look.Step(Frame);
                minYaw = Mathf.Min(minYaw, look.HeadSweepYaw);
                maxYaw = Mathf.Max(maxYaw, look.HeadSweepYaw);
                minPitch = Mathf.Min(minPitch, look.HeadSweepPitch);
                maxPitch = Mathf.Max(maxPitch, look.HeadSweepPitch);
            }

            Assert.Greater(maxYaw - minYaw, 2.0f, "Scanner head yaw must mechanically oscillate in step with the sweep.");
            Assert.Greater(maxPitch - minPitch, 1.2f, "Scanner head pitch must nod/tick in step with the sweep.");
            Assert.Less(Mathf.Max(Mathf.Abs(minYaw), Mathf.Abs(maxYaw)), _settings.AimToleranceDegrees * 0.6f,
                "Head oscillation must stay well inside line-of-sight aim tolerance.");

            look.LetGo();
            Run(look, 1.5f);
            Assert.AreEqual(0f, look.HeadSweepYaw, 0.02f, "Head yaw oscillation settles to zero on let-go.");
            Assert.AreEqual(0f, look.HeadSweepPitch, 0.02f, "Head pitch oscillation settles to zero on let-go.");
        }

        [Test]
        public void ScanAim_LensAngles_CompensatesCheekParallax_SoLensForwardHitsTarget()
        {
            var lensOffset = new Vector3(-0.34f, 0.04f, 0.25f);
            Vector3[] targets =
            {
                new Vector3(0f, 0f, 3.2f),
                new Vector3(1.8f, 0.6f, 2.5f),
                new Vector3(-2.1f, -0.4f, 2.9f),
                new Vector3(0.4f, 1.1f, 1.9f),
            };

            foreach (Vector3 target in targets)
            {
                Vector2 angles = ScanAim.UnclampedLensAngles(target, lensOffset);
                Quaternion headRot = Quaternion.Euler(-angles.y, angles.x, 0f);
                Vector3 lensPos = headRot * lensOffset;
                Vector3 lensForward = headRot * Vector3.forward;
                Vector3 toTarget = (target - lensPos).normalized;
                Assert.Less(Vector3.Angle(lensForward, toTarget), 0.05f,
                    $"Cheek lens forward must point directly at {target}");
            }
        }

        [Test]
        public void ScanAim_HeadReachVsBodyTurn_UsesHeadInsideReach_AndTurnsBodyOutsideReach()
        {
            const float headYawReach = 105f;
            const float bodyStopYaw = 22f;
            const float pitchUpLimit = 45f;
            const float pitchDownLimit = 45f;
            var lensOffset = new Vector3(-0.34f, 0.04f, 0.25f);

            // Target in front (yaw ~13 deg, within 22 deg stop threshold): head alone reaches it, no body turn needed.
            var frontTarget = new Vector3(0.4f, 0.1f, 3.2f);
            bool frontTurn = ScanAim.NeedsBodyTurn(
                frontTarget, lensOffset, headYawReach, bodyStopYaw, pitchUpLimit, pitchDownLimit, false, out float frontDelta);
            Assert.IsFalse(frontTurn, "Target in front within neck limits must not trigger a body turn.");

            // Target beside (yaw ~75 deg, within 105 deg head reach): head alone reaches it.
            var sideTarget = new Vector3(3.0f, 0.0f, 0.9f);
            bool sideTurn = ScanAim.NeedsBodyTurn(
                sideTarget, lensOffset, headYawReach, bodyStopYaw, pitchUpLimit, pitchDownLimit, false, out _);
            Assert.IsFalse(sideTurn, "Target beside within head reach must be handled by the head.");

            // Target behind (yaw ~155 deg, outside 105 deg head reach): triggers whole-body turn in place.
            var behindTarget = new Vector3(1.4f, 0.0f, -3.2f);
            bool behindTurn = ScanAim.NeedsBodyTurn(
                behindTarget, lensOffset, headYawReach, bodyStopYaw, pitchUpLimit, pitchDownLimit, false, out float behindDelta);
            Assert.IsTrue(behindTurn, "Target behind 07 outside head reach must trigger a whole-body turn.");
            Assert.Greater(behindDelta, 100f);

            // Hysteresis: while turning, body continues until within BodyStopYaw (22 deg), then hands off to head.
            var midTurnTarget = new Vector3(2.2f, 0.0f, 2.2f); // ~51 deg yaw
            Assert.IsTrue(ScanAim.NeedsBodyTurn(
                    midTurnTarget, lensOffset, headYawReach, bodyStopYaw, pitchUpLimit, pitchDownLimit, true, out _),
                "Active body turn continues through mid-angles until within stop threshold.");
            Assert.IsFalse(ScanAim.NeedsBodyTurn(
                    frontTarget, lensOffset, headYawReach, bodyStopYaw, pitchUpLimit, pitchDownLimit, true, out _),
                "Body turn stops once target enters the head handoff zone.");
            Assert.Greater(Mathf.Abs(frontDelta), 0f);
        }

        [Test]
        public void ScanAim_LineOfSight_RequiresAimAlignment_AndRejectsSelfOccludedRays()
        {
            var root = new GameObject("TestRoverRoot");
            try
            {
                var head = new GameObject("Head").transform;
                head.SetParent(root.transform, false);
                head.localPosition = new Vector3(0f, 1.2f, 0.7f);

                var lens = new GameObject("ScanOrigin").transform;
                lens.SetParent(head, false);
                lens.localPosition = new Vector3(-0.34f, 0.04f, 0.25f);

                GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chassis.name = "Chassis";
                chassis.transform.SetParent(root.transform, false);
                chassis.transform.localPosition = new Vector3(0f, 0.6f, -0.3f);
                chassis.transform.localScale = new Vector3(1.4f, 0.9f, 1.8f);

                Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
                MeshFilter[] meshes = root.GetComponentsInChildren<MeshFilter>(true);

                // 1. Unobstructed target in front, lens aimed at it -> true
                Vector3 frontPoint = new Vector3(-0.34f, 1.24f, 4.0f);
                lens.rotation = Quaternion.LookRotation((frontPoint - lens.position).normalized, Vector3.up);
                Assert.IsTrue(ScanAim.HasLineOfSight(lens, frontPoint, 8f, head, colliders, meshes),
                    "Unobstructed ray aligned with ScanOrigin.forward has line of sight.");

                // 2. Unobstructed target in front, but lens aimed 25 deg away -> false
                lens.rotation = Quaternion.Euler(0f, 25f, 0f);
                Assert.IsFalse(ScanAim.HasLineOfSight(lens, frontPoint, 8f, head, colliders, meshes),
                    "Misaligned lens must not report line of sight.");

                // 3. Target behind chassis so ray from cheek lens passes through 07's head/chassis -> false
                Vector3 occludedPoint = new Vector3(0.5f, 0.5f, -2.5f);
                lens.rotation = Quaternion.LookRotation((occludedPoint - lens.position).normalized, Vector3.up);
                Assert.IsFalse(ScanAim.HasLineOfSight(lens, occludedPoint, 8f, head, colliders, meshes),
                    "Ray passing through 07's own head or chassis must be rejected.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CutterAim_FrontBesideAndBehind_TurnsBodyUntilAligned_AndRejectsSelfOccludedRays()
        {
            var root = new GameObject("TestCutterRoverRoot");
            try
            {
                var sphere = new GameObject("PhysicsSphere");
                sphere.transform.SetParent(root.transform, false);
                sphere.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                var sphereCol = sphere.AddComponent<SphereCollider>();
                sphereCol.radius = 0.5f;

                var turret = new GameObject("CutterTurret").transform;
                turret.SetParent(root.transform, false);
                turret.localPosition = new Vector3(0f, 0.55f, 0.65f);

                var cutterHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cutterHead.name = "CutterHead";
                Object.DestroyImmediate(cutterHead.GetComponent<Collider>());
                cutterHead.transform.SetParent(turret, false);
                cutterHead.transform.localScale = new Vector3(0.14f, 0.14f, 0.18f);

                var cutterOrigin = new GameObject("CutterOrigin").transform;
                cutterOrigin.SetParent(cutterHead.transform, false);
                cutterOrigin.localPosition = new Vector3(0f, 0f, 0.5f);

                Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
                MeshFilter[] meshes = root.GetComponentsInChildren<MeshFilter>(true);

                // 1. Target directly in front: no body turn needed, has clear cutter line of sight.
                var frontTarget = new Vector3(0.1f, 0.55f, 3.5f);
                bool frontTurn = ScanAim.NeedsCutterBodyTurn(
                    cutterOrigin,
                    frontTarget,
                    ScanAim.DefaultCutterStartTurnYawDeg,
                    ScanAim.DefaultCutterStopTurnYawDeg,
                    currentlyTurningBody: false,
                    out float frontYaw);
                Assert.IsFalse(frontTurn, "Target directly in front of the chest nozzle needs no body turn.");
                Assert.Less(Mathf.Abs(frontYaw), ScanAim.DefaultCutterStartTurnYawDeg);
                Assert.IsTrue(
                    ScanAim.HasCutterLineOfSight(cutterOrigin, frontTarget, ScanAim.DefaultCutterFiringConeDeg, colliders, meshes),
                    "Target in front of the chest nozzle has clear line of sight.");

                // 2. Target beside 07 (~80 deg right): requires body turn first; no line of sight until turned.
                var besideTarget = new Vector3(3.0f, 0.55f, 1.1f);
                bool besideTurn = ScanAim.NeedsCutterBodyTurn(
                    cutterOrigin,
                    besideTarget,
                    ScanAim.DefaultCutterStartTurnYawDeg,
                    ScanAim.DefaultCutterStopTurnYawDeg,
                    currentlyTurningBody: false,
                    out float besideYaw);
                Assert.IsTrue(besideTurn, "Target beside 07 requires a body turn because the cutter is fixed in the chest.");
                Assert.Greater(besideYaw, 60f);
                Assert.IsFalse(
                    ScanAim.HasCutterLineOfSight(cutterOrigin, besideTarget, ScanAim.DefaultCutterFiringConeDeg, colliders, meshes),
                    "Target beside 07 is outside the chest nozzle's firing cone until 07 turns.");

                // 3. Target behind 07 (~160 deg): requires body turn; ray also intersects 07's SphereCollider if fired early.
                var behindTarget = new Vector3(1.1f, 0.55f, -3.0f);
                bool behindTurn = ScanAim.NeedsCutterBodyTurn(
                    cutterOrigin,
                    behindTarget,
                    ScanAim.DefaultCutterStartTurnYawDeg,
                    ScanAim.DefaultCutterStopTurnYawDeg,
                    currentlyTurningBody: false,
                    out float behindYaw);
                Assert.IsTrue(behindTurn, "Target behind 07 must trigger a whole-body turn.");
                Assert.Greater(Mathf.Abs(behindYaw), 140f);
                Assert.IsFalse(
                    ScanAim.HasCutterLineOfSight(cutterOrigin, behindTarget, ScanAim.DefaultCutterFiringConeDeg, colliders, meshes),
                    "Target behind 07 must not have cutter line of sight before turning.");

                // Once 07 turns its body to face the behind target, NeedsCutterBodyTurn becomes false and HasCutterLineOfSight becomes true.
                root.transform.rotation = Quaternion.LookRotation(new Vector3(behindTarget.x, 0f, behindTarget.z), Vector3.up);
                Assert.IsFalse(
                    ScanAim.NeedsCutterBodyTurn(
                        cutterOrigin,
                        behindTarget,
                        ScanAim.DefaultCutterStartTurnYawDeg,
                        ScanAim.DefaultCutterStopTurnYawDeg,
                        currentlyTurningBody: true,
                        out _),
                    "Once 07's chest faces the target within the stop threshold, the body turn finishes.");
                Assert.IsTrue(
                    ScanAim.HasCutterLineOfSight(cutterOrigin, behindTarget, ScanAim.DefaultCutterFiringConeDeg, colliders, meshes),
                    "Once 07's body has turned to face the target, the chest nozzle has clear line of sight.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
