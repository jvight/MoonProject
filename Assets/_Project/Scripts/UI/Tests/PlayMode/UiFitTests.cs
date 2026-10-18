using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// The UI fits every screen shape (docs/features/M3-24 part A): at 16:9, 16:10, 21:9, 32:9 and 4:3, from 1280x720
    /// to 3840x2160, the title, its settings, the HUD, the pause menu, its settings and the lab's screen leave nothing
    /// off the screen and no two panels over each other, and the title's wordmark keeps its place against the picture.
    /// </summary>
    public sealed class UiFitTests : InputTestFixture
    {
        private const float Settle = 0.4f;
        private const float PlaceTolerance = 0.01f;
        private const int BigTextPresses = 2;
        private const int CellWidth = 480;
        private const int CellHeight = 270;
        private const int CellPad = 6;
        private const string FitSheetPath = "Logs/fit_sheet.png";
        private const string TitleFitDir = "Logs/title-fit";

        private static readonly Dictionary<string, Color32[][]> s_SheetRows = new Dictionary<string, Color32[][]>();

        private readonly List<string> _problems = new List<string>();
        private string _slot;
        private InputActionAsset _controls;
        private UiTestRig _rig;

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
            UiTestRig.DeleteSlotFiles(_slot);
            Object.Destroy(_controls);
            _problems.Clear();
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator EveryScreenShape_KeepsEveryShownPanelOnScreen_AndApart()
        {
            yield return Fit(0);
        }

        [UnityTest]
        public IEnumerator EveryScreenShape_AtOneAndAHalfTimesTheText_StillFits()
        {
            yield return Fit(BigTextPresses);
        }

        private IEnumerator Fit(int textSizePresses)
        {
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            ScreenShape first = ScreenShapes.All[0];
            _rig = UiTestRig.Boot(_controls, _slot, first.Width, first.Height);
            UiLayout layout = _rig.Ui.Layout;
            EventBus events = _rig.Bootstrap.Context.Events;
            Vector2 refSize = _rig.Tuning.Title.MenuReferenceSize;
            float wideAspect = refSize.x / refSize.y;
            yield return null;

            events.Publish(new TitleShown(true));
            yield return Seconds(1f);
            if (textSizePresses > 0)
            {
                Submit(layout.SettingsButton);
                yield return Seconds(0.6f);
                for (int i = 0; i < textSizePresses; i++)
                {
                    Submit(layout.TextSizeButton);
                    yield return null;
                }

                Submit(layout.SettingsBackButton);
                yield return Seconds(0.8f);
            }

            _rig.Ui.Pause.SetTitleAnimationTime(10f);
            yield return null;

            int closedCol = textSizePresses > 0 ? 2 : 0;
            int openCol = textSizePresses > 0 ? 3 : 1;
            var wordmarkPlaces = new List<float>();
            var closedWordmarkRects = new Dictionary<string, Rect>();
            var closedHintRects = new Dictionary<string, Rect>();
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                yield return Reframe(shape);
                yield return Check(shape, "title", (label, found) =>
                {
                    ScreenFit.CheckInsideCrtFrame(layout, _rig.Tuning.Title, label, found);
                    ScreenFit.CheckTitleArtApart(layout, _rig.Ui.Pause, label, found);
                    ScreenFit.CheckApart(layout.TitleScreen, layout.PauseMain, label, found);
                    ScreenFit.CheckApart(layout.TitleWordmark, layout.PauseMain, label, found);
                    ScreenFit.CheckApart(layout.PauseMain, layout.TitleKeyHint, label, found);
                    ScreenFit.CheckApart(layout.TitleScreen, layout.TitleKeyHint, label, found);
                    ScreenFit.CheckChildrenApart(layout.PageMain, label, found);
                });
                closedWordmarkRects[shape.Name] = layout.TitleScreen.worldBound;
                closedHintRects[shape.Name] = layout.TitleKeyHint.worldBound;
                CaptureTitleFitAndSheetCell(shape, closedCol);
                if ((float)shape.Width / shape.Height >= wideAspect - PlaceTolerance)
                {
                    wordmarkPlaces.Add(WordmarkPlace(layout));
                }
            }

            foreach (float place in wordmarkPlaces)
            {
                Assert.AreEqual(wordmarkPlaces[0], place, PlaceTolerance,
                    "the wordmark keeps its place against the picture on every shape at 16:9 or wider");
            }

            Submit(layout.SettingsButton);
            yield return Seconds(1f);
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                yield return Reframe(shape);
                yield return Check(shape, "title settings", (label, found) =>
                {
                    ScreenFit.CheckInsideCrtFrame(layout, _rig.Tuning.Title, label, found);
                    ScreenFit.CheckTitleArtApart(layout, _rig.Ui.Pause, label, found);
                    ScreenFit.CheckSettingsLegibility(layout, label, found);
                    ScreenFit.CheckApart(layout.PauseMain, layout.PauseSettings, label, found);
                    ScreenFit.CheckApart(layout.TitleScreen, layout.PauseSettings, label, found);
                    ScreenFit.CheckApart(layout.TitleWordmark, layout.PauseSettings, label, found);
                    ScreenFit.CheckApart(layout.PauseSettings, layout.TitleKeyHint, label, found);
                    ScreenFit.CheckApart(layout.TitleScreen, layout.TitleKeyHint, label, found);
                    ScreenFit.CheckChildrenApart(layout.PageSound, label, found);
                });
                Rect openWordmark = layout.TitleScreen.worldBound;
                Rect openHint = layout.TitleKeyHint.worldBound;
                Assert.AreEqual(closedWordmarkRects[shape.Name].yMin, openWordmark.yMin, 1f,
                    $"{shape.Name}: opening Settings must not reflow #title-screen");
                Assert.AreEqual(closedHintRects[shape.Name].yMin, openHint.yMin, 1f,
                    $"{shape.Name}: opening Settings must not reflow #title-key-hint");
                CaptureTitleFitAndSheetCell(shape, openCol);
            }

            // Also verify Camera & feel and Display tabs and Vietnamese localization on title settings.
            yield return Reframe(ScreenShapes.All[0]);
            _rig.Ui.Pause.SelectTab(1);
            yield return Seconds(Settle);
            yield return Check(ScreenShapes.All[0], "title settings camera-en", (label, found) =>
            {
                ScreenFit.CheckInsideCrtFrame(layout, _rig.Tuning.Title, label, found);
                ScreenFit.CheckTitleArtApart(layout, _rig.Ui.Pause, label, found);
                ScreenFit.CheckSettingsLegibility(layout, label, found);
                ScreenFit.CheckChildrenApart(layout.PageCamera, label, found);
            });

            _rig.Ui.Pause.SelectTab(2);
            yield return Seconds(Settle);
            yield return Check(ScreenShapes.All[0], "title settings display-en", (label, found) =>
            {
                ScreenFit.CheckInsideCrtFrame(layout, _rig.Tuning.Title, label, found);
                ScreenFit.CheckTitleArtApart(layout, _rig.Ui.Pause, label, found);
                ScreenFit.CheckSettingsLegibility(layout, label, found);
                ScreenFit.CheckChildrenApart(layout.PageDisplay, label, found);
            });

            Submit(layout.LanguageButton);
            yield return Seconds(Settle);
            yield return Check(ScreenShapes.All[0], "title settings display-vi", (label, found) =>
            {
                ScreenFit.CheckInsideCrtFrame(layout, _rig.Tuning.Title, label, found);
                ScreenFit.CheckTitleArtApart(layout, _rig.Ui.Pause, label, found);
                ScreenFit.CheckSettingsLegibility(layout, label, found);
                ScreenFit.CheckChildrenApart(layout.PageDisplay, label, found);
            });

            _rig.Ui.Pause.SelectTab(1);
            yield return Seconds(Settle);
            yield return Check(ScreenShapes.All[0], "title settings camera-vi", (label, found) =>
            {
                ScreenFit.CheckInsideCrtFrame(layout, _rig.Tuning.Title, label, found);
                ScreenFit.CheckTitleArtApart(layout, _rig.Ui.Pause, label, found);
                ScreenFit.CheckSettingsLegibility(layout, label, found);
                ScreenFit.CheckChildrenApart(layout.PageCamera, label, found);
            });

            _rig.Ui.Pause.SelectTab(2);
            yield return null;
            Submit(layout.LanguageButton);
            _rig.Ui.Pause.SelectTab(0);
            yield return Seconds(Settle);

            WriteFitSheet();

            Submit(layout.SettingsBackButton);
            yield return Seconds(1f);
            Submit(layout.ResumeButton);
            events.Publish(new RoverAwoke(Vector3.zero, false));
            yield return Seconds(1f);

            _rig.Fakes.Cells = 4;
            _rig.Fakes.Charge = 2f;
            events.Publish(new BatteryArcRevealed(false));
            events.Publish(new NextStepChanged("next.v2.build_pad"));
            _rig.Fakes.PrimaryHint = new InteractionHint(InteractionKind.Salvage, new Vector3(0f, 0f, 12f), true);
            int gain = 0;
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                yield return Reframe(shape);
                gain++;
                _rig.Fakes.SetMaterials(gain, gain, gain);
                yield return Seconds(Settle);
                yield return Check(shape, "hud", (label, found) =>
                {
                    ScreenFit.CheckApart(layout.BatteryDial, layout.NextLine, label, found);
                    ScreenFit.CheckApart(layout.BatteryDial, layout.PickupFeed, label, found);
                });
            }

            // 07's readout (the boot diagnostics, longest in Vietnamese) beside the Sun's arc and the battery dial.
            _rig.Fakes.PrimaryHint = InteractionHint.None;
            _rig.Tune("_guidance._bootHoldSeconds", 60f);
            events.Publish(new OpeningMilestoneReached(OpeningMilestone.PowerOn, true));
            string language = _rig.Ui.Localization.Language;
            _rig.Ui.Localization.SetLanguage("vi");
            _rig.Fakes.Sun.SecondsToSunset = 25f * 60f;
            events.Publish(new RoverWakeBeatReached(WakeBeat.Crackle));
            events.Publish(new RoverWakeBeatReached(WakeBeat.ChaseFrame));
            yield return Seconds(3f);
            Assert.IsTrue(_rig.Ui.Readout.IsVisible, "the boot diagnostics are up for the fit check");
            Assert.IsTrue(_rig.Ui.SunArc.IsVisible, "and so is the Sun's arc");
            Assert.AreEqual(25, _rig.Ui.SunArc.ShownMinutes, "with the time the Sun has left");
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                yield return Reframe(shape);
                yield return Seconds(Settle);
                yield return Check(shape, "readout and sun arc", (label, found) =>
                {
                    ScreenFit.CheckApart(_rig.Ui.Readout.Panel, layout.SunArc, label, found);
                    ScreenFit.CheckApart(_rig.Ui.Readout.Panel, layout.BatteryDial, label, found);
                    ScreenFit.CheckApart(layout.SunArc, layout.BatteryDial, label, found);
                    ScreenFit.CheckApart(_rig.Ui.Readout.Panel, layout.SunArcTime, label, found);
                    ScreenFit.CheckApart(layout.SunArcTime, layout.BatteryDial, label, found);
                });
            }

            _rig.Ui.Localization.SetLanguage(language);
            _rig.Fakes.Sun.SecondsToSunset = float.PositiveInfinity;
            _rig.Fakes.Hold.Set(14, new MaterialSlot(SalvageMaterial.Metal, 4, 4),
                new MaterialSlot(SalvageMaterial.Ilmenite, 3, 6));
            _rig.Fakes.Hold.WaitingBundles = 2;
            yield return Tap(keyboard.tabKey);
            yield return Seconds(1f);
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                yield return Reframe(shape);
                yield return Check(shape, "cargo screen", null);
            }

            yield return Tap(keyboard.tabKey);
            yield return Seconds(1f);
            yield return Tap(keyboard.escapeKey);
            yield return Seconds(1f);
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                yield return Reframe(shape);
                yield return Check(shape, "pause", null);
            }

            Submit(layout.SettingsButton);
            yield return Seconds(1f);
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                yield return Reframe(shape);
                yield return Check(shape, "pause settings", (label, found) =>
                {
                    ScreenFit.CheckApart(layout.PauseMain, layout.PauseSettings, label, found);
                    ScreenFit.CheckSettingsLegibility(layout, label, found);
                });
            }

            Submit(layout.SettingsBackButton);
            yield return Tap(keyboard.escapeKey);
            yield return Seconds(1f);

            Assert.IsEmpty(_problems, "the UI fits every screen shape at " + (textSizePresses > 0 ? "150" : "100") +
                                      "% text:\n" + string.Join("\n", _problems));
        }

        private void CaptureTitleFitAndSheetCell(ScreenShape shape, int colIndex)
        {
            if (_rig.Frame == null)
            {
                return;
            }

            RenderTexture rt = _rig.Frame;
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            var src = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            src.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            src.Apply();
            RenderTexture.active = prev;

            Color32[] srcPixels = src.GetPixels32();
            for (int i = 0; i < srcPixels.Length; i++)
            {
                Color32 p = srcPixels[i];
                p.a = 255;
                srcPixels[i] = p;
            }

            src.SetPixels32(srcPixels);
            src.Apply();
            byte[] nativePng = src.EncodeToPNG();

            string scaleTag = colIndex >= 2 ? "150" : "100";
            string stateTag = (colIndex & 1) == 1 ? "settings" : "title";
            string fitDir = Path.GetFullPath(TitleFitDir);
            Directory.CreateDirectory(fitDir);
            string nativePath = Path.Combine(fitDir, $"{shape.Name}_{scaleTag}_{stateTag}.png");
            File.WriteAllBytes(nativePath, nativePng);

            int srcW = rt.width;
            int srcH = rt.height;
            Object.DestroyImmediate(src);

            bool inSheet = false;
            for (int i = 0; i < ScreenShapes.TitleSheet.Length; i++)
            {
                if (ScreenShapes.TitleSheet[i].Name == shape.Name)
                {
                    inSheet = true;
                    break;
                }
            }

            if (!inSheet)
            {
                return;
            }

            var cell = new Color32[CellWidth * CellHeight];
            Color32 bg = new Color32(6, 8, 12, 255);
            Color32 border = new Color32(36, 72, 92, 255);
            for (int i = 0; i < cell.Length; i++)
            {
                cell[i] = bg;
            }

            int availW = CellWidth - CellPad * 2;
            int availH = CellHeight - CellPad * 2;
            float scale = Mathf.Min((float)availW / srcW, (float)availH / srcH);
            int dstW = Mathf.Max(1, Mathf.RoundToInt(srcW * scale));
            int dstH = Mathf.Max(1, Mathf.RoundToInt(srcH * scale));
            int offX = (CellWidth - dstW) / 2;
            int offY = (CellHeight - dstH) / 2;

            for (int y = 0; y < dstH; y++)
            {
                int sy = Mathf.Clamp((int)((y + 0.5f) * srcH / dstH), 0, srcH - 1);
                int dstRow = (offY + y) * CellWidth + offX;
                int srcRow = sy * srcW;
                for (int x = 0; x < dstW; x++)
                {
                    int sx = Mathf.Clamp((int)((x + 0.5f) * srcW / dstW), 0, srcW - 1);
                    Color32 c = srcPixels[srcRow + sx];
                    c.a = 255;
                    cell[dstRow + x] = c;
                }
            }

            for (int x = offX - 1; x <= offX + dstW; x++)
            {
                if (x >= 0 && x < CellWidth)
                {
                    if (offY - 1 >= 0)
                    {
                        cell[(offY - 1) * CellWidth + x] = border;
                    }

                    if (offY + dstH < CellHeight)
                    {
                        cell[(offY + dstH) * CellWidth + x] = border;
                    }
                }
            }

            for (int y = offY - 1; y <= offY + dstH; y++)
            {
                if (y >= 0 && y < CellHeight)
                {
                    if (offX - 1 >= 0)
                    {
                        cell[y * CellWidth + (offX - 1)] = border;
                    }

                    if (offX + dstW < CellWidth)
                    {
                        cell[y * CellWidth + (offX + dstW)] = border;
                    }
                }
            }

            if (!s_SheetRows.TryGetValue(shape.Name, out Color32[][] cols))
            {
                cols = new Color32[4][];
                s_SheetRows[shape.Name] = cols;
            }

            cols[colIndex] = cell;
        }

        private static void WriteFitSheet()
        {
            int rows = ScreenShapes.TitleSheet.Length;
            int cols = 4;
            int sheetW = cols * CellWidth;
            int sheetH = rows * CellHeight;
            var sheetPixels = new Color32[sheetW * sheetH];
            Color32 bg = new Color32(6, 8, 12, 255);
            for (int i = 0; i < sheetPixels.Length; i++)
            {
                sheetPixels[i] = bg;
            }

            for (int r = 0; r < rows; r++)
            {
                string shapeName = ScreenShapes.TitleSheet[r].Name;
                if (!s_SheetRows.TryGetValue(shapeName, out Color32[][] rowCells))
                {
                    continue;
                }

                int destRowY = (rows - 1 - r) * CellHeight;
                for (int c = 0; c < cols; c++)
                {
                    Color32[] cell = rowCells[c];
                    if (cell == null)
                    {
                        continue;
                    }

                    int destColX = c * CellWidth;
                    for (int y = 0; y < CellHeight; y++)
                    {
                        Array.Copy(cell, y * CellWidth, sheetPixels, (destRowY + y) * sheetW + destColX, CellWidth);
                    }
                }
            }

            var sheetTex = new Texture2D(sheetW, sheetH, TextureFormat.RGBA32, false);
            sheetTex.SetPixels32(sheetPixels);
            sheetTex.Apply();
            byte[] png = sheetTex.EncodeToPNG();
            Object.DestroyImmediate(sheetTex);

            string fullPath = Path.GetFullPath(FitSheetPath);
            string dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllBytes(fullPath, png);
        }

        /// <summary>
        /// Where the wordmark's centre sits, in screen heights from the screen's centre: the picture is the same
        /// height on every shape, so a wordmark that keeps its place keeps this number.
        /// </summary>
        private static float WordmarkPlace(UiLayout layout)
        {
            Rect screen = layout.Root.worldBound;
            Rect wordmark = layout.TitleWordmark.worldBound;
            return (wordmark.center.x - screen.center.x) / screen.height;
        }

        private IEnumerator Reframe(ScreenShape shape)
        {
            _rig.Reframe(shape.Width, shape.Height);
            yield return Seconds(Settle);
        }

        private IEnumerator Check(ScreenShape shape, string state, Action<string, List<string>> pairs)
        {
            string label = Label(shape, state);
            yield return ScreenFit.Probe(found =>
            {
                ScreenFit.CheckOnScreen(_rig.Ui.Layout.Root, label, found);
                pairs?.Invoke(label, found);
            }, _problems);
        }

        private static string Label(ScreenShape shape, string state)
        {
            return shape.Name + " " + state;
        }

        private static IEnumerator Seconds(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        private static void Submit(VisualElement target)
        {
            using (NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = target;
                target.SendEvent(submit);
            }
        }

        private IEnumerator Tap(ButtonControl button)
        {
            Press(button, queueEventOnly: true);
            yield return null;
            Release(button, queueEventOnly: true);
            yield return null;
        }
    }
}
