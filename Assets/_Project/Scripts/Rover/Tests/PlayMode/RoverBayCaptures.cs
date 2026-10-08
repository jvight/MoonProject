using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// The install moment on the stand-in bay (<see cref="TestRoverBay"/>, until art's arm rig lands): 07 parks on the
    /// turntable, the Warm Headlamp is bought and fitted, and the game camera's bay view is captured as an arm brings
    /// the lamp bar down, holds it over its socket, sets it on, and 07 shows it, proud
    /// (Logs/rover-captures/bay-standin-*.png, bay-standin.md). Game time advances a fixed 1/60 s per frame, so slow
    /// captures never skip game time. Needs a GPU: run on demand with --category RoverBayCaptures.
    /// </summary>
    [Explicit("Capture session; run on demand with --category RoverBayCaptures.")]
    [Category("RoverBayCaptures")]
    public sealed class RoverBayCaptures : InputTestFixture
    {
        private const float FrameTime = 1f / 60f;
        private const float StripTimeout = 12f;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "rover-captures"));

        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;
        private TestRoverBay _bay;

        public override void Setup()
        {
            base.Setup();
            Time.captureDeltaTime = FrameTime;
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
            _rover?.Dispose();
            _bay?.Dispose();
            _world.Dispose();
            _physics.Dispose();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator AnArmFitsTheLampBar_SeenThroughTheBaysView()
        {
            Vector3 centre = TestWorld.Point(0f, -150f);
            _bay = TestRoverBay.Build(centre, 0f, _world.Material);
            _rover = TestRover.Spawn(_world, centre + new Vector3(0.2f, 0f, 0.4f), 6f);
            _rover.Context.Register<IRoverBay>(_bay);
            float until = Time.time + 3f;
            while (Time.time < until)
            {
                yield return null;
            }

            var report = new StringBuilder();
            report.AppendLine("# The Rover Bay fits the lamp bar (stand-in bay, contract arms)");
            report.AppendLine();
            BayStrip.Header(report);
            var strip = new BayStrip(_rover.Context.Events, _rover.Controller, _rover.Camera, CaptureFolder);
            _rover.Context.Get<IRoverAbilities>().Grant(RoverAbility.WarmHeadlamp);
            _rover.Context.Events.Publish(new UpgradePurchased("rover.warm_headlamp", 1));
            _rover.Context.Events.Publish(new RoverBayFitting("rover.warm_headlamp"));
            yield return strip.Capture(RoverKitPiece.LampBar, _rover.LampBar, "bay-standin-lampbar", report,
                StripTimeout);

            Directory.CreateDirectory(CaptureFolder);
            File.WriteAllText(Path.Combine(CaptureFolder, "bay-standin.md"), report.ToString());
            Debug.Log("[rover-bay] " + report);
        }
    }
}
