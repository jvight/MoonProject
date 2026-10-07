using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using Unity.Profiling;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// Steady-state zero-GC check: while 07 drives (tracks, dust, jelly, gaze all live) charging a Hover-Jump (coils
    /// squashing and glowing), and again while it rests in the opening wide shot (stillness, daydream, lamp motes), one
    /// frame additionally runs every per-frame method of the rover, rig, effects, body language and camera 600 times.
    /// Unity's "GC Allocated In Frame" counter for the quietest of three such frames must stay at the level of plain
    /// frames; a control frame proves the counter sees allocations at all.
    /// </summary>
    public sealed class RoverAllocationTests : InputTestFixture
    {
        private const int WarmUpCalls = 120;
        private const int MeasuredCalls = 600;
        private const int BaselineFrames = 5;

        /// <summary>A real per-update allocation shows in every loaded frame; one-off engine noise does not.</summary>
        private const int MeasuredFrames = 3;
        private const int ControlBytes = 64;

        /// <summary>Allowed frame jitter; one allocation per simulated frame would add 600 x 20+ bytes.</summary>
        private const long Tolerance = 2048L;

        private const string AllocatedInFrame = "GC Allocated In Frame";

        /// <summary>A shortened rest before the wide shot opens (s).</summary>
        private const float WideShotDelay = 1f;

        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;

        public override void Setup()
        {
            base.Setup();
            _physics = new LunarTestPhysics();
        }

        public override void TearDown()
        {
            _rover?.Dispose();
            _rover = null;
            _world?.Dispose();
            _world = null;
            _physics.Dispose();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator PerFrameMethods_DoNotAllocate()
        {
            _world = new TestWorld();
            _rover = TestRover.Spawn(_world, TestWorld.Point(0f, -100f), 0f);
            TestRover rover = _rover;
            rover.Context.Get<IRoverAbilities>().Grant(RoverAbility.HoverJump);
            rover.Drive.Drive = new Vector2(0.4f, 1f);
            float until = Time.time + 3f;
            while (Time.time < until)
            {
                yield return null;
            }

            Assert.Greater(rover.Controller.Speed, 1f, "Measure while driving, laying tracks and raising dust.");
            rover.Drive.JumpHeld = true;
            yield return MeasurePerFrameMethods(rover);
        }

        /// <summary>
        /// 07 resting: the stillness count, the lonely wide shot opening and breathing, the daydream and its sigh, and
        /// the lamp motes all live.
        /// </summary>
        [UnityTest]
        public IEnumerator PerFrameMethods_WhileResting_InTheWideShot_DoNotAllocate()
        {
            _world = new TestWorld();
            _rover = TestRover.Spawn(_world, TestWorld.Point(0f, -100f), 0f, wideShotDelay: WideShotDelay);
            float until = Time.time + WideShotDelay + 2f;
            while (Time.time < until)
            {
                yield return null;
            }

            Assert.IsTrue(_rover.CameraRig.WideShot.IsOpen, "Measure while the wide shot opens.");
            Assert.Greater(_rover.LampMotes.System.particleCount, 0, "And motes hang in the lamp.");
            yield return MeasurePerFrameMethods(_rover);
        }

        private IEnumerator MeasurePerFrameMethods(TestRover rover)
        {
            Action[] frame =
            {
                Bind(rover.Controller, "FixedUpdate"),
                Bind(rover.Controller, "Update"),
                Bind(rover.BodyLanguage, "Update"),
                Bind(rover.CameraRig, "Update"),
            };

            Run(frame, WarmUpCalls);
            using (ProfilerRecorder recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, AllocatedInFrame))
            {
                yield return null;
                long noise = 0L;
                for (int i = 0; i < BaselineFrames; i++)
                {
                    yield return null;
                    noise = Math.Max(noise, recorder.LastValue);
                }

                long withRover = long.MaxValue;
                for (int i = 0; i < MeasuredFrames; i++)
                {
                    Run(frame, MeasuredCalls);
                    yield return null;
                    withRover = Math.Min(withRover, recorder.LastValue);
                }

                var control = new object[MeasuredCalls];
                for (int i = 0; i < control.Length; i++)
                {
                    control[i] = new byte[ControlBytes];
                }

                yield return null;
                long withControl = recorder.LastValue;
                GC.KeepAlive(control);
                Debug.Log($"[rover-gc] plain frames <= {noise} B; quietest frame with {MeasuredCalls} extra updates: "
                    + $"{withRover} B; control frame: {withControl} B");

                Assert.Greater(withControl, noise + MeasuredCalls * ControlBytes / 2,
                    "The frame allocation counter must see a known allocation.");
                Assert.LessOrEqual(withRover, noise + Tolerance,
                    $"{MeasuredCalls} extra simulated frames allocated {withRover} bytes (plain frames: {noise}).");
            }
        }

        private static void Run(Action[] frame, int calls)
        {
            for (int call = 0; call < calls; call++)
            {
                for (int i = 0; i < frame.Length; i++)
                {
                    frame[i]();
                }
            }
        }

        private static Action Bind(Object target, string method)
        {
            MethodInfo info = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(info, $"{target.GetType().Name}.{method} not found.");
            return (Action)Delegate.CreateDelegate(typeof(Action), target, info);
        }
    }
}
