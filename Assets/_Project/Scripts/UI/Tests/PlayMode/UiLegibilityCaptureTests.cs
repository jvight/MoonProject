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
        private const string CapturesFolder = "Logs/captures";
        private const int LegibleWidth = 1920;
        private const int LegibleHeight = 1080;
        private const float RequiredContrast = 4.5f;
        private const string TabSkinClass = "settings-tab-wrap";
        private const float FontFloor = 18f;
        private const float PlateSamplePixels = 6f;

        // A CRT plate's outer border and glow fill its first pixels; the screen interior starts beyond them.
        private const float GlassSamplePixels = 8f;

        private const float BesideTextPixels = 6f;
        private const float BigTextScale = 1.5f;
        private const int BigTextPresses = 2;
        private const int ResetTextPresses = 1;
        private const float DaySunIntensity = 1.35f;
        private const float NightSunIntensity = 0.06f;

        /// <summary>
        /// The legibility captures (docs/design/first-hour-v2 6.2, M3-24 part A, and 0.5.7 Variant C CRT) at 1920x1080:
        /// the next line over the stars, the ticker over a lit dune, the tower card, the bay list, the cell row while
        /// charging and at Low, the same cards at a 150% text size, and the Variant C suite (HUD by day and night, Tab,
        /// scan result, crew log, pause menu, toast) in Logs/captures. Every text measured in a shot logs its contrast
        /// as <c>[ui-contrast]</c>: the brightest text pixel against the plate's own pixel, both composited over the
        /// brightest dune of the scene (#A7A9CC), the worst backdrop a plate holds. Each must reach WCAG AA, 4.5:1, no
        /// text may be under 18 px, and at 150% no card may leave the screen or end in an ellipsis.
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
            var serializedTuning = new UnityEditor.SerializedObject(_tuning);
            serializedTuning.FindProperty("_guidance._teachingPatienceSeconds").floatValue = 0.5f;
            serializedTuning.ApplyModifiedPropertiesWithoutUndo();
            FreezeWorld();
            yield return null;
            Reframe(LegibleWidth, LegibleHeight);
            string folder = Path.GetFullPath(LegibleFolder);
            string capturesFolder = Path.GetFullPath(CapturesFolder);
            Directory.CreateDirectory(folder);
            Directory.CreateDirectory(capturesFolder);
            var report = new List<string>();
            UiLayout layout = ui.Layout;
            ui.Ledger.RecordLookTaught();
            ui.Ledger.RecordDriveTaught();
            ui.Ledger.RecordLookUpHinted();
            ui.Ledger.RecordHoldChipShown();
            ui.Ledger.RecordFitTaught();
            ui.Ledger.RecordHoldBooted();
            context.Events.Publish(new RoverAwoke(Vector3.zero, false));
            fakes.Fade = 0f;
            yield return new WaitForSecondsRealtime(1f);

            context.Events.Publish(new NextStepChanged("next.tower.pad"));
            yield return new WaitForSecondsRealtime(1f + _tuning.Guidance.StillSeconds +
                                                    _tuning.Guidance.Line.FadeIn + 1f);
            yield return LegibleShot(camera, folder, "70_next_line_over_stars", report, layout.NextLineText);
            context.Events.Publish(new NextStepChanged(string.Empty));

            context.Events.Publish(new TickerLine(UiTestRig.HomeLine));
            yield return new WaitForSecondsRealtime(TickerEntrance() + 0.5f);
            yield return LegibleShot(camera, folder, "71_ticker_over_dune", report, layout.TickerText);
            yield return LegibleShot(camera, capturesFolder, "toast", report, layout.TickerText);

            fakes.Cells = 4;
            fakes.Charge = 2.5f;
            fakes.IsCharging = true;
            context.Events.Publish(new BatteryArcRevealed(false));
            yield return new WaitForSecondsRealtime(_tuning.Battery.Reveal.FadeIn + 1f);
            yield return LegibleShot(camera, folder, "72_cell_row_charging", report, layout.BatteryDial);
            fakes.IsCharging = false;
            fakes.IsLowPower = true;
            fakes.Charge = 0.6f;
            yield return new WaitForSecondsRealtime(1.5f);
            yield return LegibleShot(camera, folder, "73_cell_row_low", report, layout.BatteryDial);
            fakes.IsLowPower = false;
            fakes.Charge = 4f;

            fakes.Upgrade = _tower;
            fakes.AtStation = true;
            fakes.SetMaterials(2, 6, 0);
            yield return new WaitForSecondsRealtime(2f);
            yield return LegibleShot(camera, folder, "74_tower_card", report, layout.TowerTitle,
                layout.TowerDescription, layout.TowerRecipe);
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
                layout.TowerDescription, layout.TowerRecipe);
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
            yield return new WaitForSecondsRealtime(1f);

            // Reset text size to 100% and capture the Variant C 1080p reference screens in Logs/captures/.
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(1f);
            Submit(layout.SettingsButton);
            yield return new WaitForSecondsRealtime(0.6f);
            for (int i = 0; i < ResetTextPresses; i++)
            {
                Submit(layout.TextSizeButton);
                yield return null;
            }

            Assert.AreEqual("100%", layout.TextSizeButton.text, "text size reset to 100%");
            Submit(layout.SettingsBackButton);
            yield return new WaitForSecondsRealtime(0.6f);
            yield return LegibleShot(camera, capturesFolder, "pause_menu", report, layout.PauseMain);
            yield return Tap(keyboard.escapeKey);
            yield return new WaitForSecondsRealtime(1f);

            // Scan result note (cyan header + amber body on dark CRT plate).
            context.Events.Publish(new FragmentScanned("fragment.cradle.1", "rover.cargo_cradle", 1, 3, false, 0));
            yield return new WaitForSecondsRealtime(_tuning.CrateNote.Reveal.FadeIn + 0.8f);
            yield return LegibleShot(camera, capturesFolder, "scan_result", report, layout.CrateNote);
            yield return new WaitForSecondsRealtime(_tuning.CrateNote.HoldSeconds(40) + _tuning.CrateNote.Reveal.FadeOut + 0.4f);

            // Crew log card (dark ink on warm off-white paper).
            context.Events.Publish(new CrewLogFound(UiTestRig.FirstLog, Vector3.zero));
            yield return new WaitForSecondsRealtime(CardEntrance());
            yield return LegibleShot(camera, capturesFolder, "crew_log", report, layout.MemoryCard);
            ui.Card.Dismiss();
            yield return new WaitForSecondsRealtime(_tuning.MemoryCard.Reveal.FadeOut + 0.5f);

            // Tab screen (07's hold on the left, 07 v3 socket diagram on the right).
            fakes.Hold.Set(3, new MaterialSlot(SalvageMaterial.Metal, 4, 4), new MaterialSlot(SalvageMaterial.Metal, 2, 4),
                new MaterialSlot(SalvageMaterial.Wiring, 3, 4), new MaterialSlot(SalvageMaterial.Optics, 1, 4),
                new MaterialSlot(SalvageMaterial.Ilmenite, 5, 6));
            fakes.Hold.WaitingBundles = 3;
            foreach (RoverKitPiece fitted in new[]
                     {
                         RoverKitPiece.TetherRing, RoverKitPiece.SolarCell, RoverKitPiece.LampBar,
                         RoverKitPiece.CapacitorDrums, RoverKitPiece.BatterySpine, RoverKitPiece.TapeDeck,
                         RoverKitPiece.CargoRack, RoverKitPiece.SideBins,
                     })
            {
                fakes.Equipment.Fit(fitted);
            }

            fakes.Equipment.Craft(RoverKitPiece.HoverCoils);
            fakes.Equipment.Craft(RoverKitPiece.FocusLens);
            fakes.Equipment.AtStation = false;
            ui.Cargo.Open();
            yield return new WaitForSecondsRealtime(_tuning.Cargo.Reveal.FadeIn + 0.6f);
            yield return LegibleShot(camera, capturesFolder, "tab", report, layout.Cargo);
            ui.Cargo.Close();
            yield return new WaitForSecondsRealtime(_tuning.Cargo.Reveal.FadeOut + 0.5f);

            // HUD by day and by night (battery dial, pickup feed, cyan home meter, and interaction prompt).
            var skyBehaviour = context.TryGet(out ISunlight sunlight) ? sunlight as MonoBehaviour : null;
            if (skyBehaviour != null)
            {
                skyBehaviour.enabled = false;
            }

            Light sun = RenderSettings.sun;
            float savedIntensity = sun != null ? sun.intensity : 1f;
            Color savedSky = RenderSettings.ambientSkyColor;
            Color savedEquator = RenderSettings.ambientEquatorColor;
            Color savedGround = RenderSettings.ambientGroundColor;
            Vector4 savedSunColor = Shader.GetGlobalVector("_MoonSunColor");
            Vector4 savedSunlitGround = Shader.GetGlobalVector("_MoonSunlitGround");
            try
            {
                fakes.Cells = ShapeCells;
                fakes.Charge = ShapeCells * BatteryChargeShare;
                fakes.Position = context.Get<IWorldLayout>().BasePosition + new Vector3(120f, 0f, 90f);
                Transform view = camera.transform;
                Vector3 digSite = view.position + Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized * 9f +
                                  Vector3.down * 1.5f;
                fakes.PrimaryHint = new InteractionHint(InteractionKind.Salvage, digSite, true);
                fakes.SetMaterials(5, 8, 3);
                if (sun != null)
                {
                    sun.intensity = DaySunIntensity;
                }

                Shader.SetGlobalVector("_MoonSunlitGround", new Vector4(1f, 0f, 0f, 0f));
                RenderSettings.ambientSkyColor = new Color(0.28f, 0.28f, 0.34f);
                RenderSettings.ambientEquatorColor = new Color(0.20f, 0.20f, 0.25f);
                RenderSettings.ambientGroundColor = new Color(0.14f, 0.14f, 0.18f);
                DynamicGI.UpdateEnvironment();
                yield return new WaitForSecondsRealtime(1.5f);
                yield return LegibleShot(camera, capturesFolder, "hud_day", report, layout.BatteryDial,
                    layout.PickupFeed, layout.Prompt);

                fakes.SetMaterials(6, 9, 4);
                if (sun != null)
                {
                    sun.intensity = NightSunIntensity;
                }

                Shader.SetGlobalVector("_MoonSunColor", Vector4.zero);
                Shader.SetGlobalVector("_MoonSunlitGround", Vector4.zero);
                RenderSettings.ambientSkyColor = new Color(0.03f, 0.04f, 0.07f);
                RenderSettings.ambientEquatorColor = new Color(0.02f, 0.03f, 0.05f);
                RenderSettings.ambientGroundColor = new Color(0.01f, 0.01f, 0.02f);
                DynamicGI.UpdateEnvironment();
                yield return new WaitForSecondsRealtime(1f);
                yield return LegibleShot(camera, capturesFolder, "hud_night", report, layout.BatteryDial,
                    layout.PickupFeed, layout.Prompt);

                // Vietnamese (vi) HUD capture: confirms diacritics in Chakra Petch and IBM Plex Mono on HUD.
                ui.Localization.SetLanguage("vi");
                fakes.SetMaterials(7, 10, 5);
                if (sun != null)
                {
                    sun.intensity = DaySunIntensity;
                }

                Shader.SetGlobalVector("_MoonSunColor", savedSunColor);
                Shader.SetGlobalVector("_MoonSunlitGround", new Vector4(1f, 0f, 0f, 0f));
                RenderSettings.ambientSkyColor = new Color(0.28f, 0.28f, 0.34f);
                RenderSettings.ambientEquatorColor = new Color(0.20f, 0.20f, 0.25f);
                RenderSettings.ambientGroundColor = new Color(0.14f, 0.14f, 0.18f);
                DynamicGI.UpdateEnvironment();
                yield return new WaitForSecondsRealtime(1f);
                yield return LegibleShot(camera, capturesFolder, "hud_vi", report, layout.BatteryDial,
                    layout.PickupFeed, layout.Prompt);
            }
            finally
            {
                fakes.PrimaryHint = InteractionHint.None;
                if (sun != null)
                {
                    sun.intensity = savedIntensity;
                }

                Shader.SetGlobalVector("_MoonSunColor", savedSunColor);
                Shader.SetGlobalVector("_MoonSunlitGround", savedSunlitGround);
                RenderSettings.ambientSkyColor = savedSky;
                RenderSettings.ambientEquatorColor = savedEquator;
                RenderSettings.ambientGroundColor = savedGround;
                DynamicGI.UpdateEnvironment();
                if (skyBehaviour != null)
                {
                    skyBehaviour.enabled = true;
                }
            }

            // Vietnamese (vi) Tab and crew log captures: confirms diacritics across Chakra Petch, IBM Plex Mono, and Be Vietnam Pro.
            ui.Cargo.Open();
            yield return new WaitForSecondsRealtime(_tuning.Cargo.Reveal.FadeIn + 0.6f);
            yield return LegibleShot(camera, capturesFolder, "tab_vi", report, layout.Cargo);
            ui.Cargo.Close();
            yield return new WaitForSecondsRealtime(_tuning.Cargo.Reveal.FadeOut + 0.5f);

            context.Events.Publish(new CrewLogFound(UiTestRig.FirstLog, Vector3.zero));
            yield return new WaitForSecondsRealtime(CardEntrance());
            yield return LegibleShot(camera, capturesFolder, "crew_log_vi", report, layout.MemoryCard);
            ui.Card.Dismiss();
            yield return new WaitForSecondsRealtime(_tuning.MemoryCard.Reveal.FadeOut + 0.5f);
            ui.Localization.SetLanguage("en");

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
            while (plate != null && plate.resolvedStyle.backgroundColor.a < 0.3f &&
                   plate.resolvedStyle.backgroundImage.texture == null && !plate.ClassListContains(TabSkinClass))
            {
                plate = plate.parent;
            }

            if (plate == null)
            {
                return $"{shot}: '{text.text}' sits on no plate";
            }

            Rect box = text.worldBound;
            Color32 brightest = Contrast.BrightestDune;
            Color32 darkest = Contrast.BrightestDune;
            float best = -1f;
            float least = float.MaxValue;
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

                    if (l < least)
                    {
                        least = l;
                        darkest = over;
                    }
                }
            }

            Rect rim = plate.worldBound;
            float baseInset = plate.resolvedStyle.backgroundImage.texture != null ? GlassSamplePixels : PlateSamplePixels;
            float insetX = Mathf.Min(baseInset, Mathf.Max(2f, (rim.width - 4f) * 0.2f));
            float insetY = Mathf.Min(baseInset, Mathf.Max(2f, (rim.height - 4f) * 0.2f));
            float sampleX = Mathf.Clamp(box.xMin - BesideTextPixels, rim.xMin + insetX, Mathf.Max(rim.xMin + insetX, box.xMin - 1f));
            float sampleY = Mathf.Clamp(box.yMin - BesideTextPixels, rim.yMin + insetY, Mathf.Max(rim.yMin + insetY, box.yMin - 1f));
            Color32 beside = Contrast.Over(
                ui[PixelIndex(Mathf.FloorToInt(sampleX * scale), Mathf.FloorToInt(box.center.y * scale))],
                Contrast.BrightestDune);
            Color32 above = Contrast.Over(
                ui[PixelIndex(Mathf.FloorToInt(box.center.x * scale), Mathf.FloorToInt(sampleY * scale))],
                Contrast.BrightestDune);
            Color32 resolvedText = text.resolvedStyle.color;
            bool darkInk = Contrast.Luminance(resolvedText) < 0.2f;
            Color32 background = darkInk
                ? (Contrast.Luminance(beside) > Contrast.Luminance(above) ? beside : above)
                : (Contrast.Luminance(beside) < Contrast.Luminance(above) ? beside : above);
            Color32 ink = darkInk ? darkest : brightest;
            float ratio = Mathf.Max(Contrast.Ratio(brightest, background), Contrast.Ratio(darkest, background));
            Debug.Log($"[ui-contrast] {shot} '{text.text}' {ratio:0.0}:1 (text {ink.r},{ink.g}," +
                      $"{ink.b} on plate {background.r},{background.g},{background.b})");
            return ratio >= RequiredContrast ? null
                : $"{shot}: '{text.text}' reads {ratio:0.0}:1, under {RequiredContrast:0.0}:1";
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
