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
    /// What the player sees during the release (opening v3 R9): in the real Main scene, after a live answer, the chase
    /// camera behind 07 as it faces each lure in turn: the dish glint when the pulse lands, then relay.1's blinking lamp,
    /// the Peak's dish and Kestrel-3 during the View stage. Captures in Logs/captures/release-view/.
    /// </summary>
    public sealed class ReleaseViewCaptureTests : InputTestFixture
    {
        private const float FrameTime = 1f / 60f;
        private const float ReadyTimeout = 40f;
        private const float StageTimeout = 90f;
        private const float SettleSeconds = 0.7f;
        private const float LampLitKeying = 0.8f;
        private const float LampWait = 8f;

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
        public IEnumerator AfterALiveAnswer_TheChaseCameraSeesTheGlintAndEachLure()
        {
            Directory.CreateDirectory(CaptureFolder);
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

            IAnswerStatus answer = context.Get<IAnswerStatus>();
            Assert.AreEqual(AnswerPhase.Ready, answer.Phase);
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
            Vector3 dish = context.Get<IWorldLayout>().PeakPosition;
            yield return RealRoverScene.WaitSeconds(0.25f);
            Aimed(dish + Vector3.up * 3f, "release-1b-dish-glint-aimed");
            yield return Look(dish, "release-1-dish-glint", 0.1f);

            deadline = Time.time + StageTimeout;
            while (!_viewOpened && Time.time < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(_viewOpened, "the view from the rise opened");
            ICallSignal signal = null;
            context.TryGet(out signal);
            float lampDeadline = Time.time + LampWait;
            while (signal != null && signal.Keying < LampLitKeying && Time.time < lampDeadline)
            {
                yield return null;
            }

            yield return Look(_view.Relay, "release-2-relay-lamp", SettleSeconds);
            yield return Look(_view.Dish, "release-3-peak-dish", SettleSeconds);
            yield return Look(_view.Kestrel, "release-4-kestrel", SettleSeconds);
        }

        /// <summary>The player's own camera position, aimed straight at <paramref name="point"/> (what a pitched-up view shows).</summary>
        private void Aimed(Vector3 point, string name)
        {
            var host = new GameObject("AimedLens");
            Camera camera = host.AddComponent<Camera>();
            camera.enabled = false;
            camera.CopyFrom(_scene.View);
            camera.enabled = false;
            host.transform.position = _scene.View.transform.position;
            host.transform.rotation = Quaternion.LookRotation(point - host.transform.position, Vector3.up);
            FrameCapture.SavePng(camera, 1920, 1080, Path.Combine(CaptureFolder, name + ".png"));
            Object.Destroy(host);
        }

        private static string LampLevels()
        {
            var text = new System.Text.StringBuilder();
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (behaviour.GetType().Name == "RelayAnswerLamp")
                {
                    object level = behaviour.GetType().GetProperty("Level").GetValue(behaviour);
                    text.Append(behaviour.name).Append('=').Append(level).Append('@').Append(behaviour.transform.position);
                }
            }

            return text.Length == 0 ? "none" : text.ToString();
        }

        /// <summary>07 faces <paramref name="point"/>, the chase camera comes round behind it, and the player's view is taken.</summary>
        private IEnumerator Look(Vector3 point, string name, float settle)
        {
            RoverController rover = _scene.Rover;
            Vector3 toward = point - rover.Position;
            toward.y = 0f;
            _scene.Context.Get<IRoverPlacement>().PlaceAt(rover.Position, Quaternion.LookRotation(toward, Vector3.up));
            _scene.CameraRig.Snap();
            yield return RealRoverScene.WaitSeconds(settle);
            FrameCapture.SavePng(_scene.View, 1920, 1080, Path.Combine(CaptureFolder, name + ".png"));
            Vector3 screen = _scene.View.WorldToScreenPoint(point);
            ICallSignal signal;
            _scene.Context.TryGet(out signal);
            Debug.Log($"[releasecap] {name}: screen ({screen.x:F0}, {screen.y:F0}, depth {screen.z:F0}) of " +
                      $"{_scene.View.pixelWidth}x{_scene.View.pixelHeight}, fov {_scene.View.fieldOfView:F0}, keying " +
                      $"{(signal != null ? signal.Keying : -1f):F2}, call {_scene.Context.Get<ICallSource>().State}, " +
                      $"lamp levels {LampLevels()}");
        }
    }
}
