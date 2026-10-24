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
    /// How the range ring looks (opening v3 4.2 look, agy-ringfade): a soft moonlit-amber glow line on the ground (never
    /// white, never dots or dashes), with a soft gradient falloff across its width, under bloom and below relay.1's lamp
    /// in brightness. It draws out from 07 over ~2 s, holds ~1.5 s, eases out over ~3 s and is completely gone (zero
    /// vertices, zero width, zero alpha, renderer disabled), showing only at its two moments and never lingering on load.
    /// Before/after captures from the apron at night and from the chase camera are written to Logs/captures/ringfade_*.png.
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
        private const float OldMaxPixels = 3f;
        private const float OldMinPixels = 1f;
        private const float OldStrength = 0.3f;
        private const float OldShaderIntensity = 2.6f;
        private const float ChaseStep = 1f;
        private const float PixelSlack = 0.15f;
        private const float BloomThreshold = 1.25f;
        private const float RelayLampSteadyIntensity = 1.02f;

        /// <summary>A vertex colour is stored in 8 bits: one step of rounding.</summary>
        private const float ColourStep = 1f / 255f;

        private static readonly Color OldColor = new Color(0.8f, 0.9f, 1f, 1f);
        private static readonly float[] RevealShots = { 0.4f, 1f, 1.6f, 2f };

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
        public IEnumerator TheFullRing_IsASoftMoonlitAmberGlowOnTheGround_WithASoftEdge_DrawnOutwardAndGoneAfterFade()
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
                Capture(view, "ringfade_reveal_" + shot);
            }

            while (_gameplay.RangeRing.Reveal01 < 1f)
            {
                yield return null;
            }

            yield return Wait(0.3f);
            Assert.IsTrue(ring.IsShown);
            LineRenderer line = _gameplay.transform.Find("RangeRing").GetComponent<LineRenderer>();
            AssertSoftMoonlitAmber(line, camera);
            Capture(camera, "ringfade_apron_after");
            Capture(view, "ringfade_chase_after");

            LineRenderer old = Object.Instantiate(line, line.transform.parent);
            line.enabled = false;
            ConfigureOldHardWhiteLine(old, camera);
            Capture(camera, "ringfade_apron_before");
            ConfigureOldHardWhiteLine(old, view);
            Capture(view, "ringfade_chase_before");
            Object.Destroy(old.gameObject);
            line.enabled = true;

            while (ring.IsShown)
            {
                yield return null;
            }

            yield return null;
            AssertGone(line);
            Capture(camera, "ringfade_apron_gone");
            Object.Destroy(lens);
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator TheReleasesFirstFullRing_FromTheChaseCamera_DrawsHoldsAndEasesOutToZeroVertices_Once()
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
            LineRenderer line = _gameplay.transform.Find("RangeRing").GetComponent<LineRenderer>();
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
                    Capture(view, "ringfade_chase_" + shot++);
                    next += ChaseStep;
                }
            }

            yield return null;
            float duration = Time.time - shown;
            Assert.Greater(peak, 0.95f, "it came up to its strength");
            Assert.AreEqual(_gameplay.Battery.Tuning.RangeRing.FullSeconds, duration, 0.35f,
                "~2 s draw out + ~1.5 s hold + ~3 s ease out");
            Assert.GreaterOrEqual(shot, 6, "captured across its draw, hold and ease-out");
            AssertGone(line);
            Capture(view, "ringfade_chase_gone");

            _context.Events.Publish(new RelayRelit("relay.1", false));
            yield return null;
            Assert.IsFalse(ring.IsShown, "shows only at its two moments: never re-shown when relay.1 relights");
            AssertGone(line);
        }

        /// <summary>
        /// Asserts that the line is a continuous closed loop (never dots or dashes), warm moonlit-amber (not white),
        /// under bloom and below relay.1's lamp, and has a soft gradient falloff across its width.
        /// </summary>
        private void AssertSoftMoonlitAmber(LineRenderer line, Camera camera)
        {
            RangeRingSettings settings = _gameplay.Battery.Tuning.RangeRing;
            Assert.IsTrue(line.loop, "one continuous closed loop");
            Assert.Greater(line.positionCount, 0, "vertices placed while shown");

            float focal = RangeRingWidth.FocalPixels(camera.pixelHeight, camera.fieldOfView);
            Vector3 eye = camera.transform.position;
            int count = line.positionCount;
            for (int i = 0; i < count; i++)
            {
                float width = line.widthCurve.Evaluate(i / (float)(count - 1)) * line.widthMultiplier;
                float pixels = width * focal / Vector3.Distance(eye, line.GetPosition(i));
                Assert.LessOrEqual(pixels, settings.MaxPixels + PixelSlack, "a soft glow line, never a thick band");
            }

            Color color = line.startColor;
            Assert.LessOrEqual(color.a, settings.FullStrength + ColourStep, "soft strength");
            Assert.Greater(color.r, color.g, "warm moonlit-amber: red > green");
            Assert.Greater(color.g, color.b, "warm moonlit-amber: green > blue (not cool white)");
            Assert.Greater(color.r - color.b, 0.35f, "moonlit-amber, never white");
            Assert.Less(color.b, 0.6f, "warm amber, not desaturated white");

            var block = new MaterialPropertyBlock();
            line.GetPropertyBlock(block);
            float intensity = block.GetFloat("_Intensity");
            float acrossIn = block.GetFloat("_AcrossIn");
            float acrossOut = block.GetFloat("_AcrossOut");
            float bandCount = block.GetFloat("_BandCount");
            float bandStrength = block.GetFloat("_BandStrength");
            float corePower = block.GetFloat("_CorePower");
            float coreMix = block.GetFloat("_CoreMix");

            Assert.AreEqual(0f, bandCount, 1e-4f, "never dots or dashes");
            Assert.AreEqual(0f, bandStrength, 1e-4f, "never dots or dashes");
            Assert.GreaterOrEqual(acrossIn, 0.45f, "soft falloff across the full half-width");
            Assert.GreaterOrEqual(acrossOut, 0.45f, "soft falloff across the full half-width");
            Assert.GreaterOrEqual(corePower, 1.5f, "soft core falloff exponent");
            Assert.GreaterOrEqual(coreMix, 0.5f, "soft core falloff blend");
            Assert.Less(RangeRingWidth.AcrossFalloff(0.25f, acrossIn, corePower, coreMix), 0.3f,
                "gradient across width, not a hard edge");

            float maxChannel = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            Assert.Less(maxChannel * intensity, BloomThreshold, "under bloom (< 1.25)");
            Assert.Less(maxChannel * intensity, RelayLampSteadyIntensity, "below relay.1's lamp in brightness");
        }

        /// <summary>Asserts that after the fade no visible vertex remains on the line renderer.</summary>
        private void AssertGone(LineRenderer line)
        {
            Assert.IsFalse(_gameplay.RangeRing.IsShown, "ring field is no longer shown");
            Assert.IsFalse(_gameplay.RangeRingLine.IsDrawn, "line is not drawn");
            Assert.IsFalse(line.enabled, "line renderer is disabled");
            Assert.AreEqual(0, line.positionCount, "no visible vertex after the fade");
            Assert.AreEqual(0f, line.widthMultiplier, 1e-4f, "zero width multiplier after the fade");
            Assert.AreEqual(0f, line.startColor.a, 1e-4f, "zero start vertex alpha after the fade");
            Assert.AreEqual(0f, line.endColor.a, 1e-4f, "zero end vertex alpha after the fade");
        }

        /// <summary>Configures <paramref name="old"/> to match the pre-fix hard thin white ring for before/after captures.</summary>
        private static void ConfigureOldHardWhiteLine(LineRenderer old, Camera camera)
        {
            float focal = RangeRingWidth.FocalPixels(camera.pixelHeight, camera.fieldOfView);
            Vector3 eye = camera.transform.position;
            int count = old.positionCount;
            var keys = new Keyframe[count];
            for (int i = 0; i < count; i++)
            {
                float width = RangeRingWidth.World(Vector3.Distance(eye, old.GetPosition(i)), focal,
                    OldMaxPixels, OldMinPixels, 80f);
                keys[i] = new Keyframe(i / (float)(count - 1), width);
            }

            old.widthCurve = new AnimationCurve(keys);
            old.widthMultiplier = 1f;
            Color oldColor = OldColor;
            oldColor.a = OldStrength;
            old.startColor = oldColor;
            old.endColor = oldColor;
            var block = new MaterialPropertyBlock();
            old.GetPropertyBlock(block);
            block.SetFloat("_Intensity", OldShaderIntensity);
            block.SetFloat("_AcrossIn", 0.3f);
            block.SetFloat("_AcrossOut", 0.3f);
            block.SetFloat("_CoreMix", 0f);
            old.SetPropertyBlock(block);
            old.enabled = true;
        }

        private static void Capture(Camera camera, string name)
        {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures"));
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
