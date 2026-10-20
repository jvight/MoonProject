using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;
using MoonProject.Testing;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// What the player sees during the release (opening v3 4.4): in the real Main scene, after a live answer, the chase
    /// camera through the whole view: one slow pan from relay.1's lamp to the Peak's dish to Kestrel-3 with 07 in the
    /// foreground, then a run where the player drives mid-pan and the camera eases back. Captures (a sheet every
    /// 0.75 s) in Logs/captures/release-view/.
    /// </summary>
    public sealed class ReleaseViewCaptureTests : InputTestFixture
    {
        private const float FrameTime = 1f / 60f;
        private const float ReadyTimeout = 40f;
        private const float StageTimeout = 90f;
        private const float ViewSeconds = 10f;
        private const float SheetStep = 0.75f;
        private const float InterruptAt = 2.5f;
        private const float EaseBackSeconds = 2f;
        private const float MaxTurnDegreesPerSecond = 60f;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "release-view"));

        private readonly RealRoverScene _scene = new RealRoverScene();
        private Keyboard _keyboard;
        private bool _glinted;
        private bool _viewOpened;
        private ReleaseViewOpened _view;

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
        [Timeout(600000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator TheViewIsOneSlowPan_FromTheRelayToTheDishToKestrel_With07AlwaysInFrame()
        {
            yield return RunToTheView();
            float start = Time.time;
            float lastYaw = _scene.View.transform.eulerAngles.y;
            float fastest = 0f;
            float lowestFraming = 1f;
            float highestFraming = 0f;
            float peakWeight = 0f;
            float nextShot = 0f;
            int shot = 0;
            while (Time.time - start < ViewSeconds)
            {
                yield return null;
                float yaw = _scene.View.transform.eulerAngles.y;
                float rate = Mathf.Abs(Mathf.DeltaAngle(lastYaw, yaw)) / Time.deltaTime;
                fastest = Mathf.Max(fastest, rate);
                lastYaw = yaw;
                Vector3 rover = _scene.View.WorldToViewportPoint(_scene.Rover.Position + Vector3.up * 0.5f);
                lowestFraming = Mathf.Min(lowestFraming, rover.x);
                highestFraming = Mathf.Max(highestFraming, rover.x);
                peakWeight = Mathf.Max(peakWeight, _scene.CameraRig.MomentWeight);
                if (Time.time - start >= nextShot)
                {
                    FrameCapture.SavePng(_scene.View, 1920, 1080,
                        Path.Combine(CaptureFolder, $"pan-{shot++:D2}-t{Time.time - start:F1}.png"));
                    nextShot += SheetStep;
                }
            }

            Debug.Log($"[releasecap] pan: fastest camera turn {fastest:F0} deg/s, 07 across x " +
                      $"{lowestFraming:F2}..{highestFraming:F2}, peak weight {peakWeight:F2}, weight now " +
                      $"{_scene.CameraRig.MomentWeight:F2}");
            Assert.Greater(peakWeight, 0.9f, "the pan held the camera");
            Assert.LessOrEqual(fastest, MaxTurnDegreesPerSecond, "the camera never whips round");
            Assert.Greater(lowestFraming, 0.15f, "07 stays in the frame, in the foreground");
            Assert.Less(highestFraming, 0.85f, "07 stays in the frame, in the foreground");
            Assert.Less(_scene.CameraRig.MomentWeight, 0.05f, "the camera is back behind 07 when the nine seconds end");
        }

        [UnityTest]
        [Timeout(600000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator DrivingDuringTheView_EndsThePanAtOnce_WithASoftEaseBack()
        {
            yield return RunToTheView();
            float start = Time.time;
            while (Time.time - start < InterruptAt)
            {
                yield return null;
            }

            float before = _scene.CameraRig.MomentWeight;
            FrameCapture.SavePng(_scene.View, 1920, 1080, Path.Combine(CaptureFolder, "interrupt-0-before.png"));
            Press(_keyboard.wKey);
            float pressed = Time.time;
            float lastWeight = before;
            float biggestDrop = 0f;
            int shot = 1;
            float nextShot = 0.3f;
            while (Time.time - pressed < EaseBackSeconds)
            {
                yield return null;
                float weight = _scene.CameraRig.MomentWeight;
                biggestDrop = Mathf.Max(biggestDrop, lastWeight - weight);
                lastWeight = weight;
                if (Time.time - pressed >= nextShot)
                {
                    FrameCapture.SavePng(_scene.View, 1920, 1080,
                        Path.Combine(CaptureFolder, $"interrupt-{shot++}-t{Time.time - pressed:F1}.png"));
                    nextShot += 0.3f;
                }
            }

            Release(_keyboard.wKey);
            Debug.Log($"[releasecap] interrupt: weight {before:F2} -> {lastWeight:F2}, biggest drop in one frame " +
                      $"{biggestDrop:F3}");
            Assert.Greater(before, 0.5f, "the pan was holding the camera when the player drove");
            Assert.Less(lastWeight, 0.1f, "driving ended the pan");
            Assert.Less(biggestDrop, 0.1f, "and the camera eased back, it did not cut");
        }

        /// <summary>Boots the real scene, answers the call live, and waits for the release's view to open.</summary>
        private IEnumerator RunToTheView()
        {
            Directory.CreateDirectory(CaptureFolder);
            _glinted = false;
            _viewOpened = false;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            yield return _scene.Boot();
            GameplaySystem gameplay = Object.FindFirstObjectByType<GameplaySystem>();
            Assert.IsNotNull(gameplay);
            GameContext context = _scene.Context;
            context.Events.Subscribe<PeakDishGlinted>(_ => _glinted = true);
            context.Events.Subscribe<ReleaseViewOpened>(opened =>
            {
                _view = opened;
                _viewOpened = true;
            });

            var terrain = context.Get<ITerrainQuery>();
            Assert.IsTrue(context.Get<IWorldAnchors>().TryGet("place.apron", out WorldAnchor apron));
            Vector3 foot = SurfaceRules.OnSurface(terrain, apron.Position.x, apron.Position.z);
            gameplay.PlacedBase.PlaceUnchecked(gameplay.Building.Catalog.Find(StructureIds.AntennaMast),
                new StructurePose(foot, 0f), 1f);
            context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, false));
            context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.NightReady, false));
            gameplay.Opening.Restore(new OpeningSaveData { nightFallen = true });
            gameplay.Reply.Restore(new CallSaveData { replyTried = true });
            yield return RealRoverScene.WaitSeconds(MastRaise.TotalSeconds(gameplay.MastRaiseSettings) + 1.5f);

            Assert.AreEqual(AnswerPhase.Ready, context.Get<IAnswerStatus>().Phase);
            context.Get<IRoverPlacement>().PlaceAt(foot + new Vector3(3f, 0f, 0f), Quaternion.identity);
            yield return RealRoverScene.WaitSeconds(1f);
            Press(_keyboard.eKey);
            float deadline = Time.time + ReadyTimeout;
            while (!_glinted && Time.time < deadline)
            {
                yield return null;
            }

            Release(_keyboard.eKey);
            Assert.IsTrue(_glinted, "the answer ran to the glint on the dish");
            deadline = Time.time + StageTimeout;
            while (!_viewOpened && Time.time < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(_viewOpened, "the view from the rise opened");
            Assert.AreNotEqual(Vector3.zero, _view.Dish);
        }
    }
}
