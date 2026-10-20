using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// The opening runs on the reserve (opening v3, R2): in the real Main scene 07 is driven flat out, and its speed
    /// is read every frame. The cap holds the top speed to its tuned share and leaves the pull off the line alone; the
    /// first charge lifts it. Written as Logs/captures/reserve-drive/reserve-drive.csv.
    /// </summary>
    public sealed class ReserveDrivePlayModeTests : InputTestFixture
    {
        private const float FrameTime = 1f / 60f;
        private const float ReserveDriveSeconds = 9f;
        private const float LiftedDriveSeconds = 9f;
        private const float OffTheLineSeconds = 0.5f;
        private const float CapSlack = 0.04f;
        private const float LiftedShare = 0.9f;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "reserve-drive"));

        private readonly RealRoverScene _scene = new RealRoverScene();
        private Gamepad _gamepad;

        public override void Setup()
        {
            base.Setup();
            Time.captureDeltaTime = FrameTime;
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
            _scene.Unload();
            base.TearDown();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator OnTheReserve_TopSpeedIsCapped_TheFirstChargeLiftsIt()
        {
            Directory.CreateDirectory(CaptureFolder);
            _gamepad = InputSystem.AddDevice<Gamepad>();
            yield return _scene.Boot();
            RoverController rover = _scene.Rover;
            IRoverBattery battery = _scene.Context.Get<IRoverBattery>();
            float top = rover.Tuning.Drive.TopSpeed;
            Assert.IsTrue(battery.OnOpeningReserve, "a new game starts on the reserve");

            var rows = new List<string> { "phase,time,speed,cap,reserve,charge" };
            Set(_gamepad.leftStick, new Vector2(0f, 1f));
            float speedOffTheLine = 0f;
            float until = Time.time + ReserveDriveSeconds;
            float start = Time.time;
            float peakOnReserve = 0f;
            while (Time.time < until)
            {
                yield return null;
                peakOnReserve = Mathf.Max(peakOnReserve, rover.Speed);
                if (Time.time - start <= OffTheLineSeconds)
                {
                    speedOffTheLine = rover.Speed;
                }

                Record(rows, "reserve", rover, battery);
            }

            Assert.LessOrEqual(peakOnReserve, top * (battery.DriveCap01 + CapSlack),
                "on the reserve 07 never passes its capped top speed");
            Assert.Greater(peakOnReserve, top * (battery.DriveCap01 - CapSlack), "but it does reach it");
            Assert.Greater(speedOffTheLine, 0f, "and the pull off the line is not blunted");
            Assert.Greater(battery.Charge, 0f, "07 still has charge: the floor holds");

            _scene.Context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, false));
            until = Time.time + LiftedDriveSeconds;
            float peakLifted = 0f;
            while (Time.time < until)
            {
                yield return null;
                peakLifted = Mathf.Max(peakLifted, rover.Speed);
                Record(rows, "lifted", rover, battery);
            }

            Assert.IsFalse(battery.OnOpeningReserve, "the first charge lifts the reserve");
            Assert.Greater(peakLifted, top * LiftedShare, "and 07 reaches its full top speed");
            File.WriteAllLines(Path.Combine(CaptureFolder, "reserve-drive.csv"), rows, Encoding.UTF8);
            FrameCapture.SavePng(_scene.View, 1280, 720, Path.Combine(CaptureFolder, "reserve-drive.png"));
        }

        private static void Record(List<string> rows, string phase, RoverController rover, IRoverBattery battery)
        {
            rows.Add(string.Format(CultureInfo.InvariantCulture, "{0},{1:F3},{2:F3},{3:F3},{4},{5:F3}",
                phase, Time.time, rover.Speed, battery.DriveCap01, battery.OnOpeningReserve ? 1 : 0, battery.Charge));
        }
    }
}
