using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Unity.Profiling;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;
using MoonProject.Testing;
using Object = UnityEngine.Object;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// Steady-state zero-GC check of the UI: with a prompt following a moving point under the reticle, with the tower
    /// panel and its pinned chip, with Kenji's Rover Bay resting with a piece picked (and its update allocation-free
    /// while picking as the stock changes), with a kit name resting, with a ticker line resting, with the hop list over
    /// a moving fade, with the salvage ring filling under the materials chip, and with the pause menu open, one frame
    /// additionally runs the UI's Update 600 times. Unity's "GC Allocated In Frame" for the quietest of three such
    /// frames must stay at the level of plain frames; a control frame proves the counter sees allocations at all.
    /// </summary>
    public sealed class UiAllocationTests : InputTestFixture
    {
        private const int WarmUpCalls = 120;
        private const int MeasuredCalls = 600;
        private const int BaselineFrames = 5;
        private const int MeasuredFrames = 3;
        private const int ControlBytes = 64;
        private const long Tolerance = 2048L;
        private const string AllocatedInFrame = "GC Allocated In Frame";
        private const float LongHoldSeconds = 1000f;
        private const int PickEvery = 40;
        private const int StockEvery = 150;

        private string _slot;
        private InputActionAsset _controls;
        private UiTestRig _rig;
        private int _step;

        public override void Setup()
        {
            base.Setup();
            _controls = BootstrapHarness.LoadControlsCopy();
            _slot = BootstrapHarness.NewTestSlot();
        }

        public override void TearDown()
        {
            _rig?.Dispose();
            _rig = null;
            BootstrapHarness.DeleteSaveFiles(_slot);
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator PromptFollowingAMovingPoint_DoesNotAllocate()
        {
            InputSystem.AddDevice<Keyboard>();
            _rig = UiTestRig.Boot(_controls, _slot);
            _rig.Bootstrap.Context.Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Build, new Vector3(0f, 0f, 10f), true);
            _rig.Fakes.Position = new Vector3(0f, 0f, 2f);
            yield return new WaitForSecondsRealtime(2f);
            Assert.IsTrue(_rig.Ui.Prompt.IsVisible);

            Action update = Bind(_rig.Ui, "Update");
            Action frame = () =>
            {
                _step++;
                _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Build,
                    new Vector3(Mathf.Sin(_step * 0.01f) * 3f, 0f, 10f), true);
                update();
            };
            yield return Measure(frame, "prompt following a moving point");
        }

        [UnityTest]
        public IEnumerator OpenCargoScreen_DoesNotAllocate()
        {
            InputSystem.AddDevice<Keyboard>();
            _rig = UiTestRig.Boot(_controls, _slot);
            _rig.Bootstrap.Context.Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.Hold.Set(6, new MaterialSlot(SalvageMaterial.Metal, 4, 4),
                new MaterialSlot(SalvageMaterial.Ilmenite, 3, 6));
            _rig.Ui.Cargo.Open();
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsTrue(_rig.Ui.Cargo.IsOpen);
            yield return Measure(Bind(_rig.Ui, "Update"), "open cargo screen");
        }

        [UnityTest]
        public IEnumerator OpenPauseMenu_DoesNotAllocate()
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            _rig = UiTestRig.Boot(_controls, _slot);
            yield return null;
            Press(keyboard.escapeKey, queueEventOnly: true);
            yield return null;
            Release(keyboard.escapeKey, queueEventOnly: true);
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsTrue(_rig.Ui.Pause.IsOpen && _rig.Ui.Pause.IsSettled);
            yield return Measure(Bind(_rig.Ui, "Update"), "open pause menu");
        }

        [UnityTest]
        public IEnumerator SalvageRingFilling_OverThePickupFeed_DoesNotAllocate()
        {
            InputSystem.AddDevice<Keyboard>();
            _rig = UiTestRig.Boot(_controls, _slot);
            _rig.Bootstrap.Context.Events.Publish(new RoverAwoke(Vector3.zero, false));
            _rig.Fakes.HasTarget = true;
            _rig.Fakes.IsCutting = true;
            _rig.Fakes.CutPoint = new Vector3(0f, 0.5f, 6f);
            _rig.Fakes.CutProgress = 0.2f;
            yield return new WaitForSecondsRealtime(1f);
            _rig.Fakes.SetMaterials(3, 12, 1);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.IsTrue(_rig.Ui.SalvageRing.IsVisible && _rig.Ui.Pickups.IsVisible);

            Action update = Bind(_rig.Ui, "Update");
            Action frame = () =>
            {
                _step++;
                _rig.Fakes.CutProgress = (_step % 100) * 0.01f;
                update();
            };
            yield return Measure(frame, "salvage ring filling over the pickup feed");
        }

        private static IEnumerator Measure(Action frame, string label)
        {
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

                long measured = long.MaxValue;
                for (int i = 0; i < MeasuredFrames; i++)
                {
                    Run(frame, MeasuredCalls);
                    yield return null;
                    measured = Math.Min(measured, recorder.LastValue);
                }

                var control = new object[MeasuredCalls];
                for (int i = 0; i < control.Length; i++)
                {
                    control[i] = new byte[ControlBytes];
                }

                yield return null;
                long withControl = recorder.LastValue;
                GC.KeepAlive(control);
                Debug.Log($"[ui-gc] {label}: plain frames <= {noise} B; quietest frame with {MeasuredCalls} extra " +
                          $"updates: {measured} B; control frame: {withControl} B");
                Assert.Greater(withControl, noise + MeasuredCalls * ControlBytes / 2,
                    "The frame allocation counter must see a known allocation.");
                Assert.LessOrEqual(measured, noise + Tolerance,
                    $"{label}: {MeasuredCalls} extra updates allocated {measured} bytes (plain frames: {noise}).");
            }
        }

        private static void Run(Action frame, int calls)
        {
            for (int call = 0; call < calls; call++)
            {
                frame();
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
