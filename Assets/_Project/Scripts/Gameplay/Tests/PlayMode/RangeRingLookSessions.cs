using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// How the range ring looks (opening v3 4.2 look): a quiet line on the ground, a few pixels wide at most, dim and
    /// desaturated toward moonlit white, drawn outward from 07 and faded slowly away. The apron at night, the ring
    /// beside the old look it replaces, and the release's first full ring from the chase camera, in
    /// Logs/gameplay-captures/ring-look-*.png.
    /// </summary>
    [Category("RangeRing")]
    public sealed class RangeRingLookSessions : InputTestFixture
    {
        private const float WakeTimeout = 20f;
        private const float RaiseSlack = 1.5f;
        private const float AnswerTimeout = 40f;
        private const float RingTimeout = 60f;
        private const int Width = 1600;
        private const int Tall = 900;
        private const float EyeHeight = 1.6f;
        private const float LampHeight = 4f;
        private const float OldWidthMetres = 4f;
        private const float OldStrength = 0.85f;
        private const float ChaseStep = 1f;
        private const float PixelSlack = 0.15f;

        private static readonly Color OldColor = new Color(0.55f, 0.9f, 1.4f, 1f);
        private static readonly float[] RevealShots = { 0.4f, 1f, 1.6f, 2.4f };

        private GameContext _context;
        private GameplaySystem _gameplay;
        private Keyboard _keyboard;
        private bool _awake;

        public override void Setup()
        {
            base.Setup();
            _gameplay = null;
            _awake = false;
        }

        public override void TearDown()
        {
            Scene scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            base.TearDown();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator TheFullRing_IsAQuietLineOnTheGround_AFewPixelsWide_DimAndDesaturated_DrawnOutward()
        {
            yield return Boot();
            var terrain = _context.Get<ITerrainQuery>();
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet("place.apron", out WorldAnchor apron));
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet("relay.1", out WorldAnchor relay));
            _context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.NightReady, false));
            _gameplay.Opening.Restore(new OpeningSaveData { nightFallen = true });
            Vector3 foot = SurfaceRules.OnSurface(terrain, apron.Position.x, apron.Position.z);
            _context.Get<IRoverPlacement>().PlaceAt(foot, Quaternion.LookRotation(relay.Position - foot, Vector3.up));
            yield return Wait(1.5f);

            var ring = _context.Get<IRangeRing>();
            _gameplay.RangeRing.Show(RangeRingMode.Full);
            Camera view = _context.Get<IViewCamera>().Camera;
            var lens = new GameObject("RingLookLens");
            Camera camera = lens.AddComponent<Camera>();
            camera.CopyFrom(view);
            camera.enabled = false;
            lens.transform.position = foot + Vector3.up * EyeHeight;
            lens.transform.rotation = Quaternion.LookRotation(
                relay.Position + Vector3.up * LampHeight - lens.transform.position, Vector3.up);

            float started = Time.time;
            float lastReveal = 0f;
            for (int shot = 0; shot < RevealShots.Length; shot++)
            {
                while (Time.time - started < RevealShots[shot])
                {
                    yield return null;
                    Assert.GreaterOrEqual(_gameplay.RangeRing.Reveal01, lastReveal - 1e-4f, "it only draws outward");
                    lastReveal = _gameplay.RangeRing.Reveal01;
                }

                Assert.IsTrue(_gameplay.RangeRingLine.IsDrawn, "the line is on show as it draws");
                Capture(view, "ring-look-reveal-" + shot);
            }

            while (_gameplay.RangeRing.Reveal01 < 1f)
            {
                yield return null;
            }

            yield return Wait(0.3f);
            Assert.IsTrue(ring.IsShown);
            LineRenderer line = _gameplay.transform.Find("RangeRing").GetComponent<LineRenderer>();
            AssertQuiet(line, camera);
            Capture(camera, "ring-look-apron-after");

            LineRenderer old = Object.Instantiate(line, line.transform.parent);
            line.enabled = false;
            old.widthCurve = new AnimationCurve(new Keyframe(0f, OldWidthMetres), new Keyframe(1f, OldWidthMetres));
            Color oldColor = OldColor;
            oldColor.a = OldStrength;
            old.startColor = oldColor;
            old.endColor = oldColor;
            old.enabled = true;
            Capture(camera, "ring-look-apron-before");
            Object.Destroy(old.gameObject);
            Object.Destroy(lens);
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator TheReleasesFirstFullRing_FromTheChaseCamera_IsDrawnOutAndFadedSlowly()
        {
            _keyboard = InputSystem.AddDevice<Keyboard>();
            yield return Boot();
            var terrain = _context.Get<ITerrainQuery>();
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet("place.apron", out WorldAnchor apron));
            Vector3 foot = SurfaceRules.OnSurface(terrain, apron.Position.x, apron.Position.z);
            _gameplay.PlacedBase.PlaceUnchecked(_gameplay.Building.Catalog.Find(StructureIds.AntennaMast),
                new StructurePose(foot, 0f), 1f);
            _context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, false));
            _context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.NightReady, false));
            _gameplay.Opening.Restore(new OpeningSaveData { nightFallen = true });
            _gameplay.Reply.Restore(new CallSaveData { replyTried = true });
            yield return Wait(MastRaise.TotalSeconds(_gameplay.MastRaiseSettings) + RaiseSlack);
            _context.Get<IRoverPlacement>().PlaceAt(foot + new Vector3(3f, 0f, 0f),
                Quaternion.LookRotation(-Vector3.right, Vector3.up));
            yield return Wait(1f);
            Press(_keyboard.eKey);
            var ring = _context.Get<IRangeRing>();
            float deadline = Time.time + AnswerTimeout + RingTimeout;
            while (!(ring.IsShown && ring.Mode == RangeRingMode.Full) && Time.time < deadline)
            {
                yield return null;
            }

            Release(_keyboard.eKey);
            Assert.IsTrue(ring.IsShown && ring.Mode == RangeRingMode.Full, "the release shows the full ring");
            Camera view = _context.Get<IViewCamera>().Camera;
            float shown = Time.time;
            float peak = 0f;
            float next = 0f;
            int shot = 0;
            while (ring.IsShown)
            {
                yield return null;
                peak = Mathf.Max(peak, ring.Visibility01);
                if (Time.time - shown >= next)
                {
                    Capture(view, "ring-look-chase-" + shot++);
                    next += ChaseStep;
                }
            }

            Assert.Greater(peak, 0.95f, "it came up to its strength");
            Assert.Greater(shot, 5, "and stayed a while");
            Assert.IsFalse(_gameplay.RangeRingLine.IsDrawn, "then it was gone: never kept lit");
        }

        /// <summary>No vertex is wider than a few pixels from the camera; the line is dim and moonlit white.</summary>
        private void AssertQuiet(LineRenderer line, Camera camera)
        {
            RangeRingSettings settings = _gameplay.Battery.Tuning.RangeRing;
            float focal = RangeRingWidth.FocalPixels(camera.pixelHeight, camera.fieldOfView);
            Vector3 eye = camera.transform.position;
            int count = line.positionCount;
            for (int i = 0; i < count; i++)
            {
                float width = line.widthCurve.Evaluate(i / (float)(count - 1)) * line.widthMultiplier;
                float pixels = width * focal / Vector3.Distance(eye, line.GetPosition(i));
                Assert.LessOrEqual(pixels, settings.MaxPixels + PixelSlack, "a few pixels at most, never a band");
            }

            Color color = line.startColor;
            Assert.LessOrEqual(color.a, settings.FullStrength + 1e-3f, "dim");
            Assert.LessOrEqual(Mathf.Max(color.r, color.g, color.b), 1.05f, "under bloom: nothing hotter than white");
            Assert.Greater(Mathf.Min(color.r, color.g, color.b), 0.6f, "desaturated toward moonlit white");
        }

        private static void Capture(Camera camera, string name)
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "gameplay-captures"));
            Directory.CreateDirectory(folder);
            FrameCapture.SavePng(camera, Width, Tall, Path.Combine(folder, name + ".png"));
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        private IEnumerator Boot()
        {
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The range ring look sessions load the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>();
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(_gameplay, "the scene has the gameplay system");
            _context = bootstrap.Context;
            using (_context.Events.Subscribe<RoverAwoke>(_ => _awake = true))
            {
                float deadline = Time.time + WakeTimeout;
                while (!_awake && Time.time < deadline)
                {
                    yield return null;
                }

                Assert.IsTrue(_awake, "07 wakes");
            }

            yield return null;
        }
    }
}
