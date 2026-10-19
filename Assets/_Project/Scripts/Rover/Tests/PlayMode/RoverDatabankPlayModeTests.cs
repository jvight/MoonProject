using System;
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
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// 07's databank page on its real CRT face (docs/design/databank.md), driven by the real keyboard over a scripted
    /// archive: Q flips the face from the hold to the databank and the camera eases in, the Scout turns in the amber
    /// wireframe and F shows it in colour, an archive card shows as a print, E flips back, and a new entry's line shows
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
        private const float DatabankShare = 0.90f / 1.08f;
        private const string RoverPrefab = "Assets/_Project/Generated/Rover/Rover.prefab";
        private const string ScoutPrefab = "Assets/_Project/Generated/Art/R1/Models/Props/Prop_OldLander.prefab";
        private const string ArrayPrefab = "Assets/_Project/Generated/Art/R1/Models/Ruins/FallenSolarArray.prefab";
        private const string PrintStandIn = "Assets/_Project/Art/R1/Textures/T_R1_CargoCrateLabel_Albedo.png";

        /// <summary>The parts the '71 crew took home: the page leaves them off, as the world does.</summary>
        private static readonly string[] ScoutRemoved = { "TvCamera", "ScoopArm" };

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "databank"));

        private readonly RealRoverScene _scene = new RealRoverScene();
        private Keyboard _keyboard;
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
#if UNITY_EDITOR
            Directory.CreateDirectory(CaptureFolder);
            _keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            yield return _scene.Boot();
            var archive = new FakeDatabank();
            archive.Unlock(Model("db.unit07", DatabankCategory.Machines, DatabankMaker.KesslerArai, "1983",
                DatabankEra.Lumen, RoverPrefab, 200f));
            archive.Unlock(Model("db.earth_eclipse", DatabankCategory.Phenomena, DatabankMaker.None, string.Empty,
                DatabankEra.None, null, 0f));
            archive.Unlock(Model("db.solar_array", DatabankCategory.Structures, DatabankMaker.VIS, "1984",
                DatabankEra.Lumen, ArrayPrefab, 30f), 1, 3);
            archive.Unlock(Model("db.scout3", DatabankCategory.Machines, DatabankMaker.VIS, "1967", DatabankEra.Probes,
                ScoutPrefab, 35f, ScoutRemoved));
            archive.Unlock(new DatabankEntryInfo("db.card_kessler_arai", DatabankCategory.Archive,
                "db.card_kessler_arai.name", DatabankMaker.KesslerArai, "1983", DatabankEra.Lumen,
                "db.card_kessler_arai.readout", "db.card_kessler_arai.history", DatabankViewKind.Print, null, null, 0f,
                AssetDatabase.LoadAssetAtPath<Texture2D>(PrintStandIn)));
            _scene.Context.Register<IDatabank>(archive);
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

            yield return StepTo(page, "db.scout3");
            yield return RealRoverScene.WaitSeconds(1.2f);
            Assert.IsTrue(page.StageHasModel, "the Scout stands on the stage");
            Assert.IsFalse(page.IsColour, "in the tube's amber wireframe");
            StringAssert.Contains("VIS", page.View.Fields);
            StringAssert.Contains("1967", page.View.Fields);
            Assert.IsFalse(archive.IsUnread("db.scout3"), "the page read the Scout's card");
            Capture("databank_scout_wire");

            yield return Tap(_keyboard.fKey);
            yield return RealRoverScene.WaitSeconds(0.6f);
            Assert.IsTrue(page.IsColour, "F shows its true colours");
            Capture("databank_scout_colour");
            yield return Tap(_keyboard.fKey);

            yield return StepTo(page, "db.solar_array");
            yield return RealRoverScene.WaitSeconds(0.8f);
            StringAssert.Contains("1/3", page.View.SetLineText, "a set at its first piece counts its scans");
            Capture("databank_array_1of3");

            yield return StepTo(page, "db.card_kessler_arai");
            yield return RealRoverScene.WaitSeconds(0.6f);
            Assert.IsTrue(page.View.ShowsPrint, "an archive card shows as a print");
            Assert.IsFalse(page.View.ShowsModel);
            Capture("databank_archive_card");

            yield return Tap(_keyboard.eKey);
            yield return UntilPage(FacePage.Hold);
            Assert.IsFalse(page.View.IsShown, "E flips back to the hold");
            yield return Tap(_keyboard.escapeKey);
            yield return RealRoverScene.WaitSeconds(1f);
            Assert.IsFalse(face.IsOpen);

            _scene.Context.Events.Publish(new DatabankEntryAdded("db.solar_array", DatabankCategory.Structures));
            yield return null;
            yield return null;
            Assert.IsTrue(_scene.Ui.FaceNotice.IsShown, "the new entry's line shows on 07's face");
            StringAssert.Contains(_scene.Context.Get<ILocalization>().Get("db.solar_array.name"),
                _scene.Ui.FaceNotice.Text);
#else
            throw new NotSupportedException("RoverDatabankPlayModeTests loads assets through the editor.");
#endif
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

#if UNITY_EDITOR
        private static DatabankEntryInfo Model(string id, DatabankCategory category, DatabankMaker maker, string year,
            DatabankEra era, string prefab, float yaw, string[] hidden = null)
        {
            string name = id.Substring("db.".Length);
            return new DatabankEntryInfo(id, category, $"db.{name}.name", maker, year, era, $"db.{name}.readout",
                $"db.{name}.history", DatabankViewKind.Model,
                prefab != null ? AssetDatabase.LoadAssetAtPath<GameObject>(prefab) : null, hidden, yaw, null);
        }
#endif
    }
}
