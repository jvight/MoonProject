using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
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

        /// <summary>The walls clear 07's wheels by at least this much (m) on every side.</summary>
        private const float WheelClearance = 0.1f;

        /// <summary>07 steps this far (m) off its dock, then back onto it.</summary>
        private const float StepOff = 5f;

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
            AssertWallsClearTheWheels(dock);
            Assert.AreEqual(FeelPhase.Impact, Last(FeelPhase.Impact).Phase, "the clamp");
            Assert.Greater(Last(FeelPhase.Impact).Strength, 0.6f, "07 arrived low on charge");
            var battery = _context.Get<IRoverBattery>();
            yield return Until(() => battery.Charge >= 2f, "two cells charged");
            Chase("dock-lock-mid-fill");
            Assert.AreEqual(1f, lockState.StripFill(0), "the first wall's strip is lit");
            Assert.AreEqual(0f, lockState.StripFill(3), "the last one is not yet");

            yield return Until(() => lockState.Sweep >= 0.6f, "full: the strips sweep to gold");
            Chase("dock-lock-full-sweep");
            Assert.IsTrue(_beats.Exists(beat => beat.Phase == FeelPhase.Reward), "the full Reward");
            yield return Until(() => !lockState.Raised, "then the walls lower");
            Assert.IsTrue(_beats.Exists(beat => beat.Phase == FeelPhase.Act), "an Act per cell on the way");
        }

        /// <summary>
        /// The raised walls' inner box (dock space, from their meshes) holds 07's wheels (from the 07 mesh's Wheel_
        /// nodes, as docked) with <see cref="WheelClearance"/> to spare on every side.
        /// </summary>
        private void AssertWallsClearTheWheels(ChargingDock dock)
        {
            Matrix4x4 toDock = Matrix4x4.TRS(dock.Position, dock.Rotation, Vector3.one).inverse;
            var wheels = new Bounds();
            bool any = false;
            foreach (MeshFilter filter in ((Component)_context.Get<IRoverRig>()).GetComponentsInChildren<MeshFilter>())
            {
                if (filter.name.StartsWith("Wheel_", StringComparison.Ordinal) && filter.sharedMesh != null)
                {
                    Bounds box = InDock(filter, toDock);
                    if (any)
                    {
                        wheels.Encapsulate(box);
                    }
                    else
                    {
                        wheels = box;
                        any = true;
                    }
                }
            }

            Assert.IsTrue(any, "07's wheels are found");
            Bounds front = InDock(dock.Wall(0).GetComponent<MeshFilter>(), toDock);
            Bounds right = InDock(dock.Wall(1).GetComponent<MeshFilter>(), toDock);
            Bounds back = InDock(dock.Wall(2).GetComponent<MeshFilter>(), toDock);
            Bounds left = InDock(dock.Wall(3).GetComponent<MeshFilter>(), toDock);
            string fit = $"wheels {wheels.min:F2}..{wheels.max:F2}, walls x {left.max.x:F2}..{right.min.x:F2} " +
                         $"z {back.max.z:F2}..{front.min.z:F2}";
            Assert.LessOrEqual(wheels.max.x + WheelClearance, right.min.x, "the right wall clears 07: " + fit);
            Assert.GreaterOrEqual(wheels.min.x - WheelClearance, left.max.x, "the left wall clears 07: " + fit);
            Assert.LessOrEqual(wheels.max.z + WheelClearance, front.min.z, "the front wall clears 07: " + fit);
            Assert.GreaterOrEqual(wheels.min.z - WheelClearance, back.max.z, "the back wall clears 07: " + fit);
        }

        /// <summary>A mesh's bounds, corner by corner, in the dock's space.</summary>
        private static Bounds InDock(MeshFilter filter, Matrix4x4 toDock)
        {
            Bounds local = filter.sharedMesh.bounds;
            var box = new Bounds(toDock.MultiplyPoint3x4(filter.transform.TransformPoint(local.min)), Vector3.zero);
            for (int corner = 0; corner < 8; corner++)
            {
                var point = new Vector3(
                    (corner & 1) == 0 ? local.min.x : local.max.x,
                    (corner & 2) == 0 ? local.min.y : local.max.y,
                    (corner & 4) == 0 ? local.min.z : local.max.z);
                box.Encapsulate(toDock.MultiplyPoint3x4(filter.transform.TransformPoint(point)));
            }

            return box;
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

        /// <summary>Captures what the game's chase camera sees now.</summary>
        private void Chase(string name)
        {
            FrameCapture.SavePng(_context.Get<IViewCamera>().Camera, CaptureWidth, CaptureHeight,
                Path.Combine(GameplayFixture.CaptureFolder, name + ".png"));
        }
    }
}
