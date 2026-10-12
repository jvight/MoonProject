using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;

namespace MoonProject.UI.PlayModeTests
{
    public sealed partial class UiCaptureTests
    {
        private const string LegibleFolder = OutputFolder + "/legible";
        private const int LegibleWidth = 1920;
        private const int LegibleHeight = 1080;
        private const float RequiredContrast = 7f;
        private const float FontFloor = 18f;
        private const float PlateSamplePixels = 6f;
        private const float BigTextScale = 1.5f;
        private const int BigTextPresses = 2;

        /// <summary>
        /// The legibility captures (docs/design/first-hour-v2 6.2, M3-24 part A) at 1920x1080: the next line over the
        /// stars, the ticker over a lit dune, the tower card, the bay list, the cell row while charging and at Low, and
        /// the same cards at a 150% text size. Every text measured in a shot logs its contrast as
        /// <c>[ui-contrast]</c>: the brightest text pixel against the plate's own pixel, both composited over the
        /// brightest dune of the scene (#A7A9CC), the worst backdrop a plate holds. Each must reach 7:1, no text may
        /// be under 18 px, and at 150% no card may leave the screen or end in an ellipsis. PNGs land in
        /// Logs/ui-captures/legible. Run on its own:
        /// <c>python tools/unity_batch.py tests --platform playmode --filter CaptureLegibility</c>.
        /// </summary>
        [UnityTest]
        public IEnumerator CaptureLegibility_AtFullHdAndAtOneAndAHalfTimesTheText()
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
            Reframe(LegibleWidth, LegibleHeight);
            string folder = Path.GetFullPath(LegibleFolder);
            Directory.CreateDirectory(folder);
            var report = new List<string>();
            UiLayout layout = ui.Layout;
            context.Events.Publish(new RoverAwoke(Vector3.zero, false));
            fakes.Fade = 0f;
            yield return new WaitForSecondsRealtime(1f);

            context.Events.Publish(new NextStepChanged("next.tower"));
            yield return new WaitForSecondsRealtime(1f + _tuning.Guidance.StillSeconds +
                                                    _tuning.Guidance.Line.FadeIn + 1f);
            yield return LegibleShot(camera, folder, "70_next_line_over_stars", report, layout.NextLineText);

            context.Events.Publish(new TickerLine(UiTestRig.HomeLine));
            yield return new WaitForSecondsRealtime(TickerEntrance() + 0.5f);
            yield return LegibleShot(camera, folder, "71_ticker_over_dune", report, layout.TickerText);

            fakes.Cells = 4;
            fakes.Charge = 2.5f;
            fakes.IsCharging = true;
            context.Events.Publish(new BatteryArcRevealed(false));
            yield return new WaitForSecondsRealtime(_tuning.Battery.Reveal.FadeIn + 1f);
            yield return LegibleShot(camera, folder, "72_cell_row_charging", report);
            fakes.IsCharging = false;
            fakes.IsLowPower = true;
            fakes.Charge = 0.6f;
            yield return new WaitForSecondsRealtime(1.5f);
            yield return LegibleShot(camera, folder, "73_cell_row_low", report);
            fakes.IsLowPower = false;
            fakes.Charge = 4f;

            fakes.Upgrade = _tower;
            fakes.AtStation = true;
            fakes.SetMaterials(2, 6, 0);
            yield return new WaitForSecondsRealtime(2f);
            yield return LegibleShot(camera, folder, "74_tower_card", report, layout.TowerTitle,
                layout.TowerDescription, layout.TowerNeed, layout.TowerRecipe);
            fakes.AtStation = false;
            yield return new WaitForSecondsRealtime(1f);

            fakes.Bay = _bayKit;
            fakes.SetMaterials(12, 7, 0);
            fakes.AtStation = true;
            yield return new WaitForSecondsRealtime(1.8f);
            yield return Tap(keyboard.eKey);
            yield return new WaitForSecondsRealtime(0.6f);
            yield return LegibleShot(camera, folder, "75_bay_list", report, layout.TowerChoices);
            fakes.AtStation = false;
            fakes.Bay = null;
            yield return new WaitForSecondsRealtime(1f);

            ui.Ledger.RecordUsed(InteractionKind.Excavate);
            ui.Ledger.RecordUsed(InteractionKind.Salvage);
            ui.Ledger.RecordUsed(InteractionKind.Stow);
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(1f);
            yield return LegibleShot(camera, folder, "76_pause_07_can", report, layout.PauseCan);
            Assert.AreEqual(3, ui.Verbs.ShownCount, "07 can lists the verbs used so far, and no others");
            Submit(layout.SettingsButton);
            yield return new WaitForSecondsRealtime(1f);
            for (int i = 0; i < BigTextPresses; i++)
            {
                Submit(layout.TextSizeButton);
                yield return null;
            }

            Assert.AreEqual("150%", layout.TextSizeButton.text, "the Settings row reads the size");
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(Mathf.RoundToInt(1080f / BigTextScale), _panel.referenceResolution.y,
                "150% shrinks the reference height, so every size grows by half");
            yield return LegibleShot(camera, folder, "77_settings_at_150", report, layout.PauseSettings);
            Submit(layout.SettingsBackButton);
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(1f);

            fakes.Upgrade = _tower;
            fakes.AtStation = true;
            yield return new WaitForSecondsRealtime(2f);
            yield return LegibleShot(camera, folder, "78_tower_card_at_150", report, layout.TowerTitle,
                layout.TowerDescription, layout.TowerNeed, layout.TowerRecipe);
            var problems = new List<string>();
            yield return ScreenFit.Probe(found => ScreenFit.CheckOnScreen(layout.Root, "150% tower card", found),
                problems);
            Assert.IsEmpty(problems, "at 150% nothing leaves the screen:\n" + string.Join("\n", problems));
            foreach (TextElement text in layout.TowerPanel.Query<TextElement>().ToList())
            {
                Assert.IsFalse(text.text.EndsWith("…") || text.text.EndsWith("..."),
                    $"'{text.text}' is cut short at 150%");
            }

            fakes.AtStation = false;
            Assert.IsEmpty(report, "legibility:\n" + string.Join("\n", report));
#else
            Assert.Ignore("Captures need the editor.");
            yield break;
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Captures the shot, then measures every text under <paramref name="texts"/> and the font floor over the
        /// whole screen. Problems are added to <paramref name="report"/>.
        /// </summary>
        private IEnumerator LegibleShot(Camera camera, string folder, string name, List<string> report,
            params VisualElement[] texts)
        {
            yield return Capture(camera, folder, name);
            UiLayout layout = _uiHost.GetComponent<UISystem>().Layout;
            Color32[] pixels = ReadUiPixels();
            float scale = _target.height / layout.Root.layout.height;
            foreach (VisualElement root in texts)
            {
                foreach (TextElement text in TextsUnder(root))
                {
                    if (!ScreenFit.IsShown(text) || string.IsNullOrEmpty(text.text))
                    {
                        continue;
                    }

                    string problem = MeasureContrast(name, text, pixels, scale);
                    if (problem != null)
                    {
                        report.Add(problem);
                    }
                }
            }

            foreach (TextElement text in layout.Root.Query<TextElement>().ToList())
            {
                if (ScreenFit.IsShown(text) && !string.IsNullOrEmpty(text.text) &&
                    text.resolvedStyle.fontSize < FontFloor)
                {
                    report.Add($"{name}: '{text.text}' is {text.resolvedStyle.fontSize:0} px, under {FontFloor:0}");
                }
            }
        }

        private static IEnumerable<TextElement> TextsUnder(VisualElement root)
        {
            if (root is TextElement own)
            {
                yield return own;
            }

            foreach (TextElement text in root.Query<TextElement>().ToList())
            {
                yield return text;
            }
        }

        private Color32[] ReadUiPixels()
        {
            var ui = new Texture2D(_target.width, _target.height, TextureFormat.RGBA32, false, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _target;
            ui.ReadPixels(new Rect(0, 0, _target.width, _target.height), 0, 0, false);
            ui.Apply(false);
            RenderTexture.active = previous;
            Color32[] pixels = ui.GetPixels32();
            Object.Destroy(ui);
            return pixels;
        }

        private string MeasureContrast(string shot, TextElement text, Color32[] ui, float scale)
        {
            VisualElement plate = text;
            while (plate != null && plate.resolvedStyle.backgroundColor.a < 0.3f)
            {
                plate = plate.parent;
            }

            if (plate == null)
            {
                return $"{shot}: '{text.text}' sits on no plate";
            }

            Rect box = text.worldBound;
            Color32 brightest = Contrast.BrightestDune;
            float best = -1f;
            for (int y = Mathf.FloorToInt(box.yMin * scale); y < Mathf.CeilToInt(box.yMax * scale); y++)
            {
                for (int x = Mathf.FloorToInt(box.xMin * scale); x < Mathf.CeilToInt(box.xMax * scale); x++)
                {
                    Color32 over = Contrast.Over(ui[PixelIndex(x, y)], Contrast.BrightestDune);
                    float l = Contrast.Luminance(over);
                    if (l > best)
                    {
                        best = l;
                        brightest = over;
                    }
                }
            }

            Rect rim = plate.worldBound;
            int rimX = Mathf.FloorToInt(rim.xMin * scale + PlateSamplePixels);
            int rimY = Mathf.FloorToInt(rim.center.y * scale);
            Color32 background = Contrast.Over(ui[PixelIndex(rimX, rimY)], Contrast.BrightestDune);
            float ratio = Contrast.Ratio(brightest, background);
            Debug.Log($"[ui-contrast] {shot} '{text.text}' {ratio:0.0}:1 (text {brightest.r},{brightest.g}," +
                      $"{brightest.b} on plate {background.r},{background.g},{background.b})");
            bool dim = text.resolvedStyle.color.a < 0.95f;
            return ratio >= RequiredContrast || dim ? null
                : $"{shot}: '{text.text}' reads {ratio:0.0}:1, under {RequiredContrast:0}:1";
        }

        /// <summary>The ReadPixels row of panel pixel (x, y), y counted from the top.</summary>
        private int PixelIndex(int x, int y)
        {
            int row = Mathf.Clamp(_target.height - 1 - y, 0, _target.height - 1);
            return row * _target.width + Mathf.Clamp(x, 0, _target.width - 1);
        }
#endif
    }
}
