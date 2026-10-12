using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// The dock lock in the real Main scene (docs/design/feel-pass.md §7.1), for review: with home powered and 07's
    /// battery at half a cell, 07 leaves its dock and comes back to rest on it; the walls rise and clamp, the strips
    /// light per cell (captured mid-fill), then sweep to gold as 07 fills (captured) and the walls lower. Its beats
    /// are checked in order. Run on demand with --category DockLockSession.
    /// </summary>
    [Explicit("Look session in the real Main scene; run on demand with --category DockLockSession.")]
    [Category("DockLockSession")]
    public sealed class DockLockSession
    {
        private const int CaptureWidth = 1600;
        private const int CaptureHeight = 900;
        private const float WakeTimeout = 20f;
        private const float PhaseTimeout = 20f;

        /// <summary>07 steps this far (m) off its dock, then back onto it.</summary>
        private const float StepOff = 5f;

        /// <summary>The review camera stands this far (m) out from the dock, aside and up, looking at it.</summary>
        private static readonly Vector3 ViewOffset = new Vector3(3.2f, 2.4f, -3.6f);

        // The tower at its first level (home powered) and 07's battery at half a cell of four, in Core's envelope
        // and Gameplay's upgrades and battery section formats (version 1 each).
        private const string LowBatterySave =
            "{\"formatVersion\":1,\"contentVersion\":\"0.5.0\",\"savedAtUtc\":\"2026-10-12T00:00:00Z\"," +
            "\"sections\":[{\"key\":\"gameplay.upgrades\",\"version\":1,\"json\":\"{\\\"upgrades\\\":[" +
            "{\\\"id\\\":\\\"radio_tower\\\",\\\"level\\\":1}]}\"},{\"key\":\"gameplay.battery\",\"version\":1," +
            "\"json\":\"{\\\"cells\\\":4,\\\"charge\\\":0.5,\\\"drums\\\":0,\\\"lowPower\\\":false," +
            "\\\"arcRevealed\\\":true,\\\"lowLinesShown\\\":0}\"}]}";

        private readonly List<FeelBeat> _beats = new List<FeelBeat>();
        private GameContext _context;
        private GameplaySystem _gameplay;
        private IDisposable _feel;
        private IDisposable _awoke;
        private bool _awake;

        [TearDown]
        public void TearDown()
        {
            _feel?.Dispose();
            _awoke?.Dispose();
            Scene scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator TheDock_LocksTheWallsRound07_LightsThemPerCell_AndSweepsToGoldWhenFull()
        {
            yield return Boot();
            ChargingDock dock = _gameplay.Home.Dock;
            DockLock lockState = dock.Lock;
            var placement = _context.Get<IRoverPlacement>();
            Vector3 heading = dock.Rotation * Vector3.forward;
            placement.PlaceAt(dock.Position - heading * StepOff, dock.Rotation);
            yield return Until(() => !lockState.Raised, "the walls are down with 07 off the pad");
            _beats.Clear();
            placement.PlaceAt(dock.Position, dock.Rotation);
            yield return Until(() => lockState.Locked, "07 rests on the dock and the walls lock round it");
            Assert.AreEqual(FeelPhase.Impact, Last(FeelPhase.Impact).Phase, "the clamp");
            Assert.Greater(Last(FeelPhase.Impact).Strength, 0.6f, "07 arrived low on charge");
            var battery = _context.Get<IRoverBattery>();
            yield return Until(() => battery.Charge >= 2f, "two cells charged");
            Review(dock, "dock-lock-mid-fill");
            Assert.AreEqual(1f, lockState.StripFill(0), "the first wall's strip is lit");
            Assert.AreEqual(0f, lockState.StripFill(3), "the last one is not yet");

            yield return Until(() => lockState.Sweep >= 0.6f, "full: the strips sweep to gold");
            Review(dock, "dock-lock-full-sweep");
            Assert.IsTrue(_beats.Exists(beat => beat.Phase == FeelPhase.Reward), "the full Reward");
            yield return Until(() => !lockState.Raised, "then the walls lower");
            Assert.IsTrue(_beats.Exists(beat => beat.Phase == FeelPhase.Act), "an Act per cell on the way");
        }

        private FeelBeat Last(FeelPhase phase)
        {
            return _beats.FindLast(beat => beat.Phase == phase);
        }

        private IEnumerator Boot()
        {
            Directory.CreateDirectory(SaveService.DefaultDirectory);
            File.WriteAllText(Path.Combine(SaveService.DefaultDirectory,
                PlaythroughScene.SaveSlot + SaveService.Extension), LowBatterySave);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The dock lock session loads the scene through the editor.");
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
            _feel = _context.Events.Subscribe<FeelBeat>(beat =>
            {
                if (beat.Kind == FeelKind.Dock)
                {
                    _beats.Add(beat);
                }
            });
            _awoke = _context.Events.Subscribe<RoverAwoke>(_ => _awake = true);
            yield return Until(() => _awake, "07 wakes up");
        }

        private static IEnumerator Until(Func<bool> done, string what)
        {
            float deadline = Time.time + PhaseTimeout;
            while (!done())
            {
                Assert.Less(Time.time, deadline, what);
                yield return null;
            }
        }

        /// <summary>Renders the dock from a review camera with the game camera's lens and post-processing.</summary>
        private void Review(ChargingDock dock, string name)
        {
            Camera view = _context.Get<IViewCamera>().Camera;
            var host = new GameObject("DockLockReviewCamera");
            try
            {
                var camera = host.AddComponent<Camera>();
                camera.CopyFrom(view);
                camera.enabled = false;
                UniversalAdditionalCameraData source = view.GetUniversalAdditionalCameraData();
                UniversalAdditionalCameraData target = camera.GetUniversalAdditionalCameraData();
                target.renderPostProcessing = source.renderPostProcessing;
                target.antialiasing = source.antialiasing;
                target.renderShadows = source.renderShadows;
                Vector3 eye = dock.Position + dock.Rotation * ViewOffset;
                host.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(dock.Position - eye));
                FrameCapture.SavePng(camera, CaptureWidth, CaptureHeight,
                    Path.Combine(GameplayFixture.CaptureFolder, name + ".png"));
            }
            finally
            {
                Object.Destroy(host);
            }
        }
    }
}
