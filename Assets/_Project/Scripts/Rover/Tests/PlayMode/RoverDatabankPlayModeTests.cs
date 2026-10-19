using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;
using MoonProject.UI;
using Object = UnityEngine.Object;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// 07's databank page on its real CRT face (docs/design/databank.md), driven by the real keyboard and mouse over
    /// Gameplay's archive (07, the Kessler–Arai card and the eclipse seeded; the Scout's story scan unlocks it): Q flips the face from the hold to the databank and the camera eases in, the Scout turns in the amber
    /// wireframe and F shows it in colour, an archive card shows as a print, the HOLD tab and Q flip back, and a new entry's line shows
    /// on 07's face once the screen is closed. Writes ultrawide captures to Logs/captures/databank/.
    /// </summary>
    public sealed class RoverDatabankPlayModeTests : InputTestFixture
    {
        private const float FrameTime = 1f / 60f;
        private const float OpenTimeout = 8f;
        private const float BootSettleSeconds = 4f;
        private const float FlipTimeout = 3f;
        private const float CameraSettleSeconds = 0.8f;
        private const int CaptureWidth = 3440;
        private const int CaptureHeight = 1440;
        private const int MaxSteps = 12;
        private const string ScoutId = "db.scout3";
        private const float DatabankShare = 0.90f / 1.08f;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "databank"));

        private readonly RealRoverScene _scene = new RealRoverScene();
        private Keyboard _keyboard;
        private Mouse _mouse;
        private RenderTexture _target;

        public override void Setup()
        {
            base.Setup();
            Time.captureDeltaTime = FrameTime;
        }

        public override void TearDown()
        {
            Time.captureDeltaTime = 0f;
            if (_scene.View != null)
            {
                _scene.View.targetTexture = null;
            }

            if (_target != null)
            {
                _target.Release();
                Object.DestroyImmediate(_target);
                _target = null;
            }

            _scene.Unload();
            base.TearDown();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator RealScene_TheDatabankPage_FlipsIn_TurnsTheScoutInWireAndColour_AndShowsAPrint()
        {
            Directory.CreateDirectory(CaptureFolder);
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
            yield return _scene.Boot();
            IDatabank archive = _scene.Context.Get<IDatabank>();
            Assert.IsTrue(archive.TryGetEntry(ScoutId, out _), "the catalogue knows the Scout");
            _scene.Context.Events.Publish(new StoryScanned(ScoutId.Substring("db.".Length), "scan.scout3.note",
                Vector3.zero));
            Assert.Contains(ScoutId, (System.Collections.ICollection)archive.UnlockedIds, "its story scan unlocked it");
            _target = new RenderTexture(CaptureWidth, CaptureHeight, 24);
            _scene.View.targetTexture = _target;
            IRoverFaceScreen face = _scene.Context.Get<IRoverFaceScreen>();

            yield return Tap(_keyboard.tabKey);
            float deadline = Time.time + OpenTimeout;
            while (!face.IsOpen || _scene.CameraRig.FaceInspectWeight < 0.99f)
            {
                Assert.Less(Time.time, deadline, "Tab opens the hold page on 07's face");
                yield return null;
            }

            yield return RealRoverScene.WaitSeconds(BootSettleSeconds);
            Transform screen = _scene.Body.ScreenTransform;
            float holdDistance = Vector3.Distance(_scene.View.transform.position, screen.position);

            yield return Tap(_keyboard.qKey);
            yield return UntilPage(FacePage.Databank);
            yield return RealRoverScene.WaitSeconds(CameraSettleSeconds);
            DatabankPage page = _scene.Ui.Databank;
            Assert.IsNotNull(page, "Q built the databank page");
            Assert.IsTrue(page.View.IsShown, "and it is the page on the tube");
            float databankDistance = Vector3.Distance(_scene.View.transform.position, screen.position);
            Assert.AreEqual(holdDistance * DatabankShare, databankDistance, holdDistance * 0.03f,
                "the camera eased in on the databank (docs/design/databank.md §4: 1.08 m to 0.90 m)");

            yield return StepTo(page, ScoutId);
            yield return RealRoverScene.WaitSeconds(1.2f);
            Assert.IsTrue(page.StageHasModel, "the Scout stands on the stage");
            Assert.IsFalse(page.IsColour, "in the tube's amber wireframe");
            StringAssert.Contains("VIS", page.View.Fields);
            StringAssert.Contains("1967", page.View.Fields);
            Assert.IsFalse(archive.IsUnread(ScoutId), "the page read the Scout's card");
            for (int i = 0; i < DatabankIndex.StripEras.Length; i++)
            {
                Assert.AreEqual(DatabankIndex.StripEras[i] == DatabankEra.Probes, page.View.IsMarkLit(i),
                    $"the era strip lights only the Scout's era (mark {i})");
            }

            Capture("databank_scout_wire");

            yield return Tap(_keyboard.fKey);
            yield return RealRoverScene.WaitSeconds(0.6f);
            Assert.IsTrue(page.IsColour, "F shows its true colours");
            Capture("databank_scout_colour");
            yield return Tap(_keyboard.fKey);

            yield return StepTo(page, "db.unit07");
            yield return RealRoverScene.WaitSeconds(0.8f);
            Assert.IsTrue(page.StageHasModel, "07's own wireframe turns on the tube");
            Capture("databank_unit07_wire");

            yield return StepTo(page, "db.card_kessler_arai");
            yield return RealRoverScene.WaitSeconds(0.6f);
            Assert.IsTrue(page.View.ShowsPrint, "an archive card shows as a print (its sheet is Art's, still to come)");
            Assert.IsFalse(page.View.ShowsModel);
            Capture("databank_archive_card");

            Vector2 holdTab = DatabankLayout.TabRect(0).center * _scene.Ui.Cargo.PanelScale;
            Assert.IsTrue(FaceScreenMapper.TryPanelToScreen(holdTab, _scene.View, face,
                FaceScreenMapper.DefaultPanelSize, out Vector2 tabOnScreen), "the HOLD tab is on the glass");
            Set(_mouse.position, tabOnScreen);
            yield return null;
            Press(_mouse.leftButton);
            yield return null;
            Release(_mouse.leftButton);
            yield return UntilPage(FacePage.Hold);
            Assert.IsFalse(page.View.IsShown, "a click on the HOLD tab flips back");
            yield return Tap(_keyboard.qKey);
            yield return UntilPage(FacePage.Databank);
            yield return Tap(_keyboard.qKey);
            yield return UntilPage(FacePage.Hold);
            Assert.IsFalse(page.View.IsShown, "and Q flips there and back");
            yield return Tap(_keyboard.escapeKey);
            yield return RealRoverScene.WaitSeconds(1f);
            Assert.IsFalse(face.IsOpen);

            _scene.Context.Events.Publish(new StoryScanned("retroreflector", "db.retroreflector.history",
                Vector3.zero));
            yield return null;
            yield return null;
            Assert.IsTrue(_scene.Ui.FaceNotice.IsShown, "the new entry's line shows on 07's face");
            StringAssert.Contains(_scene.Context.Get<ILocalization>().Get("db.retroreflector.name"),
                _scene.Ui.FaceNotice.Text);
        }

        private IEnumerator Tap(KeyControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        private IEnumerator UntilPage(FacePage page)
        {
            float deadline = Time.time + FlipTimeout;
            while (_scene.Ui.Pages.Page != page || _scene.Ui.Pages.IsFlipping)
            {
                Assert.Less(Time.time, deadline, $"the face flips to {page}");
                yield return null;
            }
        }

        /// <summary>Walks the list with the down arrow until <paramref name="entryId"/> is the card shown.</summary>
        private IEnumerator StepTo(DatabankPage page, string entryId)
        {
            for (int i = 0; i < MaxSteps && page.ShownId != entryId; i++)
            {
                yield return Tap(_keyboard.downArrowKey);
                yield return RealRoverScene.WaitSeconds(0.1f);
            }

            Assert.AreEqual(entryId, page.ShownId, "the arrows reach the card");
        }

        private void Capture(string name)
        {
            Texture2D image = FrameCapture.Render(_scene.View, CaptureWidth, CaptureHeight);
            try
            {
                FrameCapture.WritePng(image, Path.Combine(CaptureFolder, $"{name}_{CaptureWidth}x{CaptureHeight}.png"));
            }
            finally
            {
                Object.DestroyImmediate(image);
            }
        }
    }
}
