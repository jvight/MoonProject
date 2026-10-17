using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;

namespace MoonProject.UI.PlayModeTests
{
    public sealed partial class UiCaptureTests
    {
        private const string ShapeFolder = OutputFolder + "/shapes";
        private const float ShapeSettle = 0.5f;
        private const float StageSettle = 2f;
        private const float BatteryChargeShare = 0.55f;
        private const int ShapeCells = 4;

        /// <summary>
        /// The title, the title's settings, the driving HUD, the pause menu, its settings and the lab's board at every
        /// screen shape in <see cref="ScreenShapes"/> (M3-24 part A), over the real Main scene. Every shot is also
        /// checked: nothing shown leaves the screen and the panels that share a screen do not overlap. PNGs land in
        /// Logs/ui-captures/shapes as &lt;shape&gt;_&lt;state&gt;.png. Run on its own:
        /// <c>python tools/unity_batch.py tests --platform playmode --filter CaptureEveryScreenShape</c>.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureEveryScreenShape()
        {
#if UNITY_EDITOR
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            yield return LoadMain();
            GameContext context = FindBootstrap().Context;
            Camera camera = context.Get<IViewCamera>().Camera;
            UISystem ui = BuildUi(context, out FakeGameServices fakes, out _);
            TuneQuickPatience();
            yield return null;
            string folder = Path.GetFullPath(ShapeFolder);
            Directory.CreateDirectory(folder);
            var problems = new List<string>();
            UiLayout layout = ui.Layout;

            context.Events.Publish(new TitleShown(true));
            yield return new WaitForSecondsRealtime(_tuning.Title.Curtain.FadeOut + _tuning.Title.Reveal.FadeIn + 0.5f);
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                yield return ShapeShot(camera, folder, shape, "title", problems, null, (shot, found) =>
                {
                    ScreenFit.CheckApart(layout.TitleWordmark, layout.PauseMain, shot, found);
                });
            }

            Submit(layout.SettingsButton);
            yield return new WaitForSecondsRealtime(1f);
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                yield return ShapeShot(camera, folder, shape, "title_settings", problems, null, (shot, found) =>
                {
                    ScreenFit.CheckApart(layout.PauseMain, layout.PauseSettings, shot, found);
                    ScreenFit.CheckApart(layout.TitleWordmark, layout.PauseSettings, shot, found);
                });
            }

            Submit(layout.SettingsBackButton);
            yield return new WaitForSecondsRealtime(0.8f);
            Submit(layout.ResumeButton);
            context.Events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return new WaitForSecondsRealtime(_tuning.Title.Reveal.FadeOut + 1f);

            fakes.Cells = ShapeCells;
            fakes.Charge = ShapeCells * BatteryChargeShare;
            context.Events.Publish(new BatteryArcRevealed(false));
            Transform view = camera.transform;
            Vector3 site = view.position + Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized * 9f +
                           Vector3.down * 1.5f;
            fakes.PrimaryHint = new InteractionHint(InteractionKind.Excavate, site, true);
            fakes.Position = context.Get<IWorldLayout>().BasePosition + new Vector3(120f, 0f, 90f);
            yield return new WaitForSecondsRealtime(1.5f);
            int gain = 0;
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                gain++;
                yield return ShapeShot(camera, folder, shape, "hud", problems,
                    () => fakes.SetMaterials(gain, gain + 1, gain + 2), (shot, found) =>
                    {
                        ScreenFit.CheckApart(layout.BatteryDial, layout.NextLine, shot, found);
                        ScreenFit.CheckApart(layout.NextLine, layout.PickupFeed, shot, found);
                        ScreenFit.CheckApart(layout.BatteryDial, layout.PickupFeed, shot, found);
                    });
            }

            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(1.2f);
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                yield return ShapeShot(camera, folder, shape, "pause", problems, null, (shot, found) => { });
            }

            Submit(layout.SettingsButton);
            yield return new WaitForSecondsRealtime(1f);
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                yield return ShapeShot(camera, folder, shape, "pause_settings", problems, null, (shot, found) =>
                {
                    ScreenFit.CheckApart(layout.PauseMain, layout.PauseSettings, shot, found);
                });
            }

            Submit(layout.SettingsBackButton);
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(1f);

            Assert.IsEmpty(problems, "the UI fits every screen shape:\n" + string.Join("\n", problems));
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

#if UNITY_EDITOR
        private IEnumerator ShapeShot(Camera camera, string folder, ScreenShape shape, string state,
            List<string> problems, Action stage, Action<string, List<string>> pairChecks)
        {
            Reframe(shape.Width, shape.Height);
            yield return new WaitForSecondsRealtime(ShapeSettle);
            stage?.Invoke();
            yield return new WaitForSecondsRealtime(stage == null ? 0f : StageSettle);
            yield return Capture(camera, folder, shape.Name + "_" + state);
            string shot = shape.Name + " " + state;
            UiLayout layout = _uiHost.GetComponent<UISystem>().Layout;
            yield return ScreenFit.Probe(found =>
            {
                ScreenFit.CheckOnScreen(layout.Root, shot, found);
                pairChecks(shot, found);
            }, problems);
        }
#endif
    }
}
