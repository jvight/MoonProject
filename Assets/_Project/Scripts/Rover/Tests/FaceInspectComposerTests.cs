using NUnit.Framework;
using UnityEngine;
using MoonProject.Editor.Builders;
using MoonProject.Rover.Editor;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// EditMode unit tests for <see cref="FaceInspectComposer"/> (camera framing math, FOV fill scaling, and occlusion
    /// probe order <c>Default -&gt; Left -&gt; Right -&gt; Closer</c>), <see cref="FaceState"/> screen content mode
    /// channel-switch choreography, and <see cref="RoverPrefabBuilder"/>'s <c>Screen</c> <see cref="MeshCollider"/> wiring.
    /// </summary>
    public sealed class FaceInspectComposerTests
    {
        private const float ScreenHeight = 0.24f;
        private const float TargetFill = 0.75f;
        private const float DefaultDistance = 0.67f;
        private const float MinDistance = 0.42f;
        private const float LookOffsetY = -0.01f;
        private const float SideYaw = 28f;
        private const float ProbeRadius = 0.18f;

        private static readonly Vector3 NeckPivot = new Vector3(0f, 0.90f, 0.22f);
        private static readonly Quaternion RoverRotation = Quaternion.identity;
        private static readonly Vector3 BaseScreenPos = new Vector3(0f, 1.32f, 0.52f);
        private static readonly Quaternion BaseScreenRot = Quaternion.Euler(-10f, 0f, 0f);

        [Test]
        public void VerticalFovForFill_FramesScreenAtTargetViewportHeight_AtDefaultAndCloserDistances()
        {
            float defaultFov = FaceInspectComposer.VerticalFovForFill(ScreenHeight, TargetFill, DefaultDistance);
            float fillAtDefault = FaceInspectComposer.VerticalFill(ScreenHeight, DefaultDistance, defaultFov);
            Assert.AreEqual(TargetFill, fillAtDefault, 1e-4f, "Screen must occupy 75% of viewport height at default distance.");

            float closerFov = FaceInspectComposer.VerticalFovForFill(ScreenHeight, TargetFill, MinDistance);
            float fillAtCloser = FaceInspectComposer.VerticalFill(ScreenHeight, MinDistance, closerFov);
            Assert.Greater(closerFov, defaultFov, "Pulling closer must widen vertical FOV proportionally.");
            Assert.AreEqual(TargetFill, fillAtCloser, 1e-4f, "Screen must still occupy 75% of viewport height when pulled closer.");
        }

        [Test]
        public void Solve_SelectsDefaultLeftRightCloser_InStrictOrder()
        {
            var sight = new DirectionalSightLine();

            // 1. All clear -> Default (0 deg)
            FaceInspectShot defShot = SolveWith(sight);
            Assert.AreEqual(FaceInspectProbeChoice.Default, defShot.Choice);
            Assert.AreEqual(0f, defShot.NeckYawOffset, 1e-4f);
            Assert.AreEqual(DefaultDistance, defShot.Distance, 1e-4f);

            // 2. Default blocked, Left clear -> Left (-28 deg)
            sight.DefaultReach = 0.35f;
            FaceInspectShot leftShot = SolveWith(sight);
            Assert.AreEqual(FaceInspectProbeChoice.Left, leftShot.Choice);
            Assert.AreEqual(-SideYaw, leftShot.NeckYawOffset, 1e-4f);
            Assert.AreEqual(DefaultDistance, leftShot.Distance, 1e-4f);

            // 3. Default and Left blocked, Right clear -> Right (+28 deg)
            sight.LeftReach = 0.35f;
            FaceInspectShot rightShot = SolveWith(sight);
            Assert.AreEqual(FaceInspectProbeChoice.Right, rightShot.Choice);
            Assert.AreEqual(SideYaw, rightShot.NeckYawOffset, 1e-4f);
            Assert.AreEqual(DefaultDistance, rightShot.Distance, 1e-4f);

            // 4. All three blocked at default distance -> Closer along the clearest bearing with widened FOV
            sight.DefaultReach = 0.64f;
            sight.LeftReach = 0.40f;
            sight.RightReach = 0.45f;
            FaceInspectShot closerShot = SolveWith(sight);
            Assert.AreEqual(FaceInspectProbeChoice.Closer, closerShot.Choice);
            Assert.AreEqual(0f, closerShot.NeckYawOffset, 1e-4f, "Must pick the bearing with the greatest reach.");
            float expectedDistance = Mathf.Clamp(sight.DefaultReach - ProbeRadius, MinDistance, DefaultDistance);
            Assert.AreEqual(expectedDistance, closerShot.Distance, 1e-4f);
            Assert.AreEqual(
                TargetFill,
                FaceInspectComposer.VerticalFill(ScreenHeight, closerShot.Distance, closerShot.VerticalFov),
                1e-4f,
                "Closer fallback must widen FOV so the screen still fills 75% of viewport height.");
        }

        [Test]
        public void FaceState_ScreenModeChoreography_PinchesToHorizontalLine_RevealsContent_AndClosesWithHappyBlink()
        {
            var tuning = ScriptableObject.CreateInstance<FaceTuning>();
            try
            {
                var state = new FaceState(tuning, seed: 7);
                Assert.IsFalse(state.IsScreenModeOpen);
                Assert.AreEqual(0f, state.ScreenContentMix, 1e-5f);
                Assert.AreEqual(0f, state.ScreenVerticalPinch, 1e-5f);

                // Open screen mode: phase 1 collapses eyes into a bright horizontal phosphor line
                state.SetScreenMode(true);
                Assert.IsTrue(state.IsScreenModeOpen);
                state.Step(tuning.ScreenCollapseSeconds * 0.5f);
                Assert.AreEqual(0f, state.ScreenContentMix, 1e-5f, "During eye collapse, content mix remains 0.");
                Assert.Greater(state.ScreenVerticalPinch, 0.2f);
                Assert.Greater(state.ScreenLineGlow, 0.2f);

                // Crossing collapse boundary switches mix to 1 at peak pinch, then expands content
                state.Step(tuning.ScreenCollapseSeconds * 0.6f);
                Assert.AreEqual(1f, state.ScreenContentMix, 1e-5f, "In expand phase, content mix is 1.");
                Assert.Greater(state.ScreenVerticalPinch, 0.5f);

                // Finish opening expand
                state.Step(tuning.ScreenExpandSeconds + 0.05f);
                Assert.AreEqual(1f, state.ScreenContentMix, 1e-5f);
                Assert.AreEqual(0f, state.ScreenVerticalPinch, 1e-5f);
                Assert.AreEqual(0f, state.ScreenLineGlow, 1e-5f);

                // Close screen mode: phase 1 collapses content to horizontal line, phase 2 expands eyes + happy blink
                state.SetScreenMode(false);
                Assert.IsFalse(state.IsScreenModeOpen);
                state.Step(tuning.ScreenCloseCollapseSeconds * 0.5f);
                Assert.AreEqual(1f, state.ScreenContentMix, 1e-5f, "During close collapse, content mix stays 1.");
                Assert.Greater(state.ScreenVerticalPinch, 0.2f);

                state.Step(tuning.ScreenCloseCollapseSeconds * 0.6f + tuning.ScreenCloseExpandSeconds + 0.02f);
                Assert.AreEqual(0f, state.ScreenContentMix, 1e-5f, "After close expand, eyes are visible again.");
                Assert.AreEqual(0f, state.ScreenVerticalPinch, 1e-5f);
                Assert.AreEqual(1, state.ClosingHappyBlinkCount, "Closing must end with a happy blink (^ ^).");
                Assert.Greater(state.LeftEye.Arc, 0.5f);
                Assert.Greater(state.RightEye.Arc, 0.5f);
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }

        [Test]
        public void RoverPrefabBuilder_AddsMeshColliderOnScreen_AndWiresRoverController()
        {
            using (var scratch = new BuilderScratchScene())
            {
                GameObject rover = RoverPrefabBuilder.Assemble(scratch);
                Transform screen = rover.transform.Find("Visual/Chassis/RoverModel/Neck/NeckLift/Head/Screen");
                Assert.IsNotNull(screen, "Screen transform must exist in assembled Rover.");

                var collider = screen.GetComponent<MeshCollider>();
                var filter = screen.GetComponent<MeshFilter>();
                Assert.IsNotNull(collider, "Screen must have a MeshCollider for diegetic UI raycasting.");
                Assert.IsNotNull(filter);
                Assert.AreSame(filter.sharedMesh, collider.sharedMesh);
            }
        }

        private static FaceInspectShot SolveWith(ISightLine sight)
        {
            return FaceInspectComposer.Solve(
                NeckPivot,
                RoverRotation,
                BaseScreenPos,
                BaseScreenRot,
                ScreenHeight,
                TargetFill,
                DefaultDistance,
                MinDistance,
                LookOffsetY,
                SideYaw,
                ProbeRadius,
                sight);
        }

        private sealed class DirectionalSightLine : ISightLine
        {
            public float DefaultReach { get; set; } = 2f;
            public float LeftReach { get; set; } = 2f;
            public float RightReach { get; set; } = 2f;

            public float Clear(Vector3 from, Vector3 to, float radius)
            {
                Vector3 dir = to - from;
                if (dir.x < -0.05f)
                {
                    return LeftReach;
                }

                if (dir.x > 0.05f)
                {
                    return RightReach;
                }

                return DefaultReach;
            }

            public bool IsFree(Vector3 point, float radius)
            {
                return true;
            }
        }
    }
}
