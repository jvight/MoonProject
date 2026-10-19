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
using MoonProject.Testing;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// The owner felt a jerk pressing Tab while moving, as the camera goes to 07's face and back (2026-10-19). In the
    /// real Main scene 07 is driven at speed, Tab opens the face, Tab closes it, and the camera is read every frame: its
    /// step (m and deg) from the frame before, and how much each step differs from the one before it (the jerk), against
    /// 07's own motion. Written as Logs/captures/tab-cam/tab-cam-*.csv.
    /// </summary>
    public sealed class TabCameraPlayModeTests : InputTestFixture
    {
        private const float FrameTime = 1f / 60f;
        private const float DriveUpSeconds = 5f;
        private const float OpenWatchSeconds = 6f;
        private const float ClosedWatchSeconds = 5f;
        private const float QuickCloseSeconds = 0.4f;
        private const float QuickWatchSeconds = 4f;

        // The tuned budget of one continuous eased move: the camera's step from one frame to the next (m, deg) and how
        // much that step may change in a frame (a snap shows as a step change of metres; a smooth blend peaks near 0.2).
        private const float MaxStepMetres = 1.0f;
        private const float MaxTurnDegrees = 14f;
        private const float MaxStepChangeMetres = 0.3f;
        private const float MaxTurnChangeDegrees = 3f;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "tab-cam"));

        private readonly RealRoverScene _scene = new RealRoverScene();
        private Keyboard _keyboard;
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
        public IEnumerator TabWhileDriving_TheCameraGoesToTheFaceAndBack_AsOneEasedMove()
        {
            Directory.CreateDirectory(CaptureFolder);
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            yield return _scene.Boot();
            Camera view = _scene.View;
            RoverController rover = _scene.Rover;
            RoverCameraRig rig = _scene.CameraRig;
            IRoverFaceScreen face = _scene.Context.Get<IRoverFaceScreen>();

            Set(_gamepad.leftStick, new Vector2(0f, 1f));
            yield return RealRoverScene.WaitSeconds(DriveUpSeconds);
            var rows = new List<Row>();
            Record(rows, view, rover, rig, "drive", 0.5f);

            Set(_gamepad.leftStick, new Vector2(0f, 1f));
            Press(_keyboard.tabKey);
            yield return null;
            Release(_keyboard.tabKey);
            Record(rows, view, rover, rig, "open", 0f);
            float until = Time.time + OpenWatchSeconds;
            while (Time.time < until)
            {
                yield return null;
                Record(rows, view, rover, rig, "open", 0f);
            }

            Assert.IsTrue(face.IsOpen, "Tab opened the face");
            Press(_keyboard.tabKey);
            yield return null;
            Release(_keyboard.tabKey);
            until = Time.time + ClosedWatchSeconds;
            while (Time.time < until)
            {
                Record(rows, view, rover, rig, "close", 0f);
                yield return null;
            }

            Write(rows, "tab-cam.csv");
            Report(rows);
            AssertContinuous(rows, "Tab to the face and back at speed");
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator TabAndTabAgainWhileStillMoving_NeverJerksEither()
        {
            Directory.CreateDirectory(CaptureFolder);
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            yield return _scene.Boot();
            Camera view = _scene.View;
            RoverController rover = _scene.Rover;
            RoverCameraRig rig = _scene.CameraRig;

            Set(_gamepad.leftStick, new Vector2(0f, 1f));
            yield return RealRoverScene.WaitSeconds(DriveUpSeconds);
            var rows = new List<Row>();
            Record(rows, view, rover, rig, "drive", 0.5f);
            Press(_keyboard.tabKey);
            yield return null;
            Release(_keyboard.tabKey);
            float until = Time.time + QuickCloseSeconds;
            while (Time.time < until)
            {
                Record(rows, view, rover, rig, "open", 0f);
                yield return null;
            }

            Press(_keyboard.tabKey);
            yield return null;
            Release(_keyboard.tabKey);
            until = Time.time + QuickWatchSeconds;
            while (Time.time < until)
            {
                Record(rows, view, rover, rig, "close", 0f);
                yield return null;
            }

            Write(rows, "tab-cam-quick.csv");
            Report(rows);
            AssertContinuous(rows, "Tab and Tab again while 07 is still moving");
        }

        private static void AssertContinuous(List<Row> rows, string what)
        {
            float previousStep = 0f;
            float previousTurn = 0f;
            for (int i = 2; i < rows.Count; i++)
            {
                float step = Vector3.Distance(rows[i].CameraPosition, rows[i - 1].CameraPosition);
                float turn = Quaternion.Angle(rows[i].CameraRotation, rows[i - 1].CameraRotation);
                string where = $"{what}, frame {i} ({rows[i].Phase}, face weight {rows[i].FaceWeight:F2}, speed {rows[i].Speed:F2})";
                Assert.LessOrEqual(step, MaxStepMetres, "the camera never moves more than a tuned step in one frame: " + where);
                Assert.LessOrEqual(turn, MaxTurnDegrees, "nor turns more than a tuned angle in one frame: " + where);
                if (i > 2)
                {
                    Assert.LessOrEqual(Mathf.Abs(step - previousStep), MaxStepChangeMetres,
                        $"no snap: the step changes smoothly from frame to frame: {where} (step {step:F3} after {previousStep:F3})");
                    Assert.LessOrEqual(Mathf.Abs(turn - previousTurn), MaxTurnChangeDegrees,
                        $"no flick: the turn changes smoothly from frame to frame: {where} (turn {turn:F2} after {previousTurn:F2})");
                }

                previousStep = step;
                previousTurn = turn;
            }
        }

        private void Record(List<Row> rows, Camera view, RoverController rover, RoverCameraRig rig, string phase,
            float unused)
        {
            rows.Add(new Row
            {
                Phase = phase,
                Time = Time.time,
                CameraPosition = view.transform.position,
                CameraRotation = view.transform.rotation,
                RoverPosition = rover.Position,
                Speed = rover.Speed,
                FaceWeight = rig.FaceInspectWeight,
                YawOffset = rig.Orbit.YawOffset,
                Held = rover.IsHeldStill,
                FaceOpen = rig.IsFaceInspectOpen,
                Delta = Time.deltaTime,
            });
        }

        private static void Report(List<Row> rows)
        {
            float maxStep = 0f;
            float maxTurn = 0f;
            float maxJerk = 0f;
            int jerkAt = 0;
            float previousStep = 0f;
            for (int i = 2; i < rows.Count; i++)
            {
                float step = Vector3.Distance(rows[i].CameraPosition, rows[i - 1].CameraPosition);
                float turn = Quaternion.Angle(rows[i].CameraRotation, rows[i - 1].CameraRotation);
                float jerk = Mathf.Abs(step - previousStep);
                previousStep = step;
                maxStep = Mathf.Max(maxStep, step);
                maxTurn = Mathf.Max(maxTurn, turn);
                if (jerk > maxJerk)
                {
                    maxJerk = jerk;
                    jerkAt = i;
                }
            }

            Debug.Log($"[tabcam] frames {rows.Count}, max step {maxStep * 100f:F1} cm/frame, max turn {maxTurn:F2} deg/frame, " +
                      $"max step change {maxJerk * 100f:F1} cm at frame {jerkAt} ({rows[jerkAt].Phase}, weight " +
                      $"{rows[jerkAt].FaceWeight:F2}, speed {rows[jerkAt].Speed:F2})");
        }

        private static void Write(List<Row> rows, string name)
        {
            var csv = new StringBuilder("frame,phase,time,camX,camY,camZ,camYaw,camPitch,roverX,roverZ,speed,faceWeight,yawOffset,held,open,stepM,turnDeg\n");
            for (int i = 0; i < rows.Count; i++)
            {
                Row r = rows[i];
                float step = i > 0 ? Vector3.Distance(r.CameraPosition, rows[i - 1].CameraPosition) : 0f;
                float turn = i > 0 ? Quaternion.Angle(r.CameraRotation, rows[i - 1].CameraRotation) : 0f;
                Vector3 e = r.CameraRotation.eulerAngles;
                csv.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},{1},{2:F3},{3:F3},{4:F3},{5:F3},{6:F2},{7:F2},{8:F3},{9:F3},{10:F3},{11:F3},{12:F2},{13},{14},{15:F4},{16:F3}",
                    i, r.Phase, r.Time, r.CameraPosition.x, r.CameraPosition.y, r.CameraPosition.z, e.y, e.x,
                    r.RoverPosition.x, r.RoverPosition.z, r.Speed, r.FaceWeight, r.YawOffset, r.Held ? 1 : 0,
                    r.FaceOpen ? 1 : 0, step, turn));
            }

            File.WriteAllText(Path.Combine(CaptureFolder, name), csv.ToString());
        }

        private struct Row
        {
            public string Phase;
            public float Time;
            public Vector3 CameraPosition;
            public Quaternion CameraRotation;
            public Vector3 RoverPosition;
            public float Speed;
            public float FaceWeight;
            public float YawOffset;
            public bool Held;
            public bool FaceOpen;
            public float Delta;
        }
    }
}
