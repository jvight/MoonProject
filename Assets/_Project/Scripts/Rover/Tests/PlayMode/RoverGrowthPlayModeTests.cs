using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// The growth parts on 07 in the real Main scene (opening v3 2.6, 5.2a): the antenna's second segment slides out
    /// at the first charge, the igniter cartridge seats when the cutter turns OK, the SSR-3 headlamp pods on
    /// BrowSocket (and the flank cell pods on PodSocket_L/R) unfold/clamp in ~0.6 s on PartFitted with warm R1 lamp
    /// lenses that follow 07's lights, and a loaded game with the rack bay Filled shows them at rest with no motion.
    /// Captures land in Logs/captures/growth/ and Logs/captures/lampvisual_*.png.
    /// </summary>
    public sealed class RoverGrowthPlayModeTests
    {
        private const float FrameTime = 1f / 60f;
        private const float Tolerance = 1e-3f;
        private const int CaptureWidth = 960;
        private const int CaptureHeight = 540;

        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "growth"));

        private static readonly string LampCaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures"));

        private readonly RealRoverScene _scene = new RealRoverScene();
        private Transform _segment;
        private Transform _igniter;
        private Transform _headlampPod;
        private Transform _housingLeft;
        private Transform _housingRight;
        private Renderer _lensLeft;
        private Renderer _lensRight;
        private Transform _cellPodLeft;
        private Transform _cellPodRight;
        private Renderer _cellBarLeft;
        private GameObject _lens;

        [SetUp]
        public void SetUp()
        {
            Time.captureDeltaTime = FrameTime;
        }

        [TearDown]
        public void TearDown()
        {
            Time.captureDeltaTime = 0f;
            if (_lens != null)
            {
                Object.Destroy(_lens);
            }

            _scene.Unload();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator ANewGame_ShowsTheAntennaRetractedAndNoIgniter_ThenEachGrowsOnItsEvent()
        {
            Directory.CreateDirectory(CaptureFolder);
            Directory.CreateDirectory(LampCaptureFolder);
            yield return _scene.Boot();
            Find();
            RoverGrowth growth = _scene.Rover.Growth;
            GrowthSettings settings = growth.Settings;
            Vector3 retracted = _segment.localPosition;
            Vector3 leftHousingRest = _housingLeft.localPosition;
            Quaternion leftHousingRotRest = _housingLeft.localRotation;
            Vector3 rightHousingRest = _housingRight.localPosition;
            Quaternion rightHousingRotRest = _housingRight.localRotation;

            Assert.IsFalse(growth.AntennaExtended, "a new game starts with the antenna's second segment in");
            Assert.IsFalse(growth.IgniterFitted);
            Assert.IsFalse(_igniter.gameObject.activeSelf, "and no igniter cartridge in the socket");
            Assert.IsFalse(growth.HeadlampFitted, "the SSR-3 headlamp pods are not fitted before assembly");
            Assert.IsFalse(_headlampPod.gameObject.activeSelf, "BrowSocket's headlamp pods start hidden");
            Assert.IsFalse(growth.CellPodsFitted, "the flank cell pods are not fitted before assembly");
            Assert.IsFalse(_cellPodLeft.gameObject.activeSelf, "PodSocket_L's cell pod starts hidden");
            Assert.IsFalse(_cellPodRight.gameObject.activeSelf, "PodSocket_R's cell pod starts hidden");
            Capture("new-game-retracted-antenna", _segment, new Vector3(0.9f, 0.3f, 1.4f));
            Capture("new-game-no-igniter", _igniter.parent, new Vector3(0.5f, 0.2f, 0.9f));

            _scene.Context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, false));
            yield return RealRoverScene.WaitSeconds(settings.AntennaSeconds * 0.5f);
            float halfway = (_segment.localPosition - retracted).y;
            Assert.Greater(halfway, 0f, "the segment is on its way out");
            Assert.Less(halfway, settings.AntennaExtendMetres, "but not there yet: the slide is slow");
            yield return RealRoverScene.WaitSeconds(settings.AntennaSeconds + settings.AntennaSettleSeconds);
            Assert.AreEqual(settings.AntennaExtendMetres, (_segment.localPosition - retracted).y, Tolerance,
                "it rests out by the extend distance");
            Assert.AreEqual(retracted.x, _segment.localPosition.x, Tolerance, "straight along the mast");
            Assert.AreEqual(retracted.z, _segment.localPosition.z, Tolerance);
            Capture("first-charge-antenna-extended", _segment, new Vector3(0.9f, 0.3f, 1.4f));

            // Snap the sky to golden-hour day and capture chase cam before fitting the headlamp.
            _scene.Context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, true));
            yield return null;
            CaptureChaseCam("lampvisual_before_day");

            Vector3 seated = _igniter.localPosition;
            _scene.Context.Events.Publish(new RoverSystemChanged(RoverSystem.Cutter, RoverSystemState.Ok, false));
            yield return null;
            Assert.IsTrue(_igniter.gameObject.activeSelf, "the cartridge shows as the cutter is fitted");
            Assert.Greater(Vector3.Distance(_igniter.localPosition, seated), settings.IgniterSeatMetres * 0.5f,
                "starting out of its socket");
            yield return RealRoverScene.WaitSeconds(settings.IgniterSeatSeconds + settings.IgniterClickSeconds + 0.2f);
            Assert.AreEqual(0f, Vector3.Distance(_igniter.localPosition, seated), Tolerance, "seated at home");
            Capture("fitted-igniter-seated", _igniter.parent, new Vector3(0.5f, 0.2f, 0.9f));

            // Fit the SSR-3 headlamp: appears on BrowSocket, unfolds/clamps over ~0.6 s, and lights its lenses warm.
            _scene.Context.Events.Publish(
                new PartFitted(RoverSocket.Lamp, RoverKitPiece.LampBar, false, false, default, 0));
            Assert.IsTrue(growth.HeadlampFitted, "PartFitted marks the headlamp fitted");
            Assert.IsTrue(growth.HeadlampMoving, "live PartFitted starts the ~0.6 s unfold/clamp motion");
            Assert.IsTrue(_headlampPod.gameObject.activeSelf, "the headlamp pods become visible on BrowSocket");
            Assert.Greater(Vector3.Distance(_housingLeft.localPosition, leftHousingRest),
                settings.HeadlampClampMetres * 0.5f, "Housing_L starts outboard before clamping in");
            Assert.Greater(Quaternion.Angle(_housingLeft.localRotation, leftHousingRotRest),
                settings.HeadlampUnfoldDegrees * 0.4f, "Housing_L starts unfolded outward");
            Assert.Greater(EmissionOf(_lensLeft), 0f, "Lens_L catches warm right away via _EmissionStrength");
            Assert.Greater(EmissionOf(_lensRight), 0f, "Lens_R catches warm right away via _EmissionStrength");

            yield return RealRoverScene.WaitSeconds(settings.HeadlampFitSeconds * 0.4f);
            Assert.Greater(Vector3.Distance(_housingLeft.localPosition, leftHousingRest), 0.002f,
                "still clamping inward mid-way through the ~0.6 s fit");

            yield return RealRoverScene.WaitSeconds(settings.HeadlampFitSeconds * 0.6f + 0.1f);
            Assert.IsFalse(growth.HeadlampMoving, "the ~0.6 s unfold/clamp motion finishes");
            Assert.AreEqual(0f, Vector3.Distance(_housingLeft.localPosition, leftHousingRest), Tolerance,
                "Housing_L rests at its bracket pose");
            Assert.AreEqual(0f, Quaternion.Angle(_housingLeft.localRotation, leftHousingRotRest), 0.05f,
                "Housing_L rests folded shut");
            Assert.AreEqual(0f, Vector3.Distance(_housingRight.localPosition, rightHousingRest), Tolerance,
                "Housing_R rests at its bracket pose");
            Assert.AreEqual(0f, Quaternion.Angle(_housingRight.localRotation, rightHousingRotRest), 0.05f,
                "Housing_R rests folded shut");
            var roadLight = _scene.Rover.GetComponentInChildren<RoverHeadlamp>();
            float fullLensGlow = EmissionOf(_lensLeft);
            Assert.Greater(fullLensGlow, 0.5f, "Lens_L glows at R1_LampAmber's warm emission strength");
            Assert.AreEqual(1.6f * roadLight.LampLevel, fullLensGlow, 0.05f,
                "Lens_L matches R1_LampAmber emission scaled by RoverHeadlamp's active LampLevel");
            Assert.AreEqual(fullLensGlow, EmissionOf(_lensRight), Tolerance, "Lens_R matches Lens_L");

            // The pods work as lamps: switching 07's lights off and on switches the pod lenses with them.
            roadLight.SetLightsOn(false, true);
            Assert.AreEqual(0f, EmissionOf(_lensLeft), Tolerance, "pod lenses go dark when 07's lights are off");
            Assert.AreEqual(0f, EmissionOf(_lensRight), Tolerance);
            roadLight.SetLightsOn(true, true);
            Assert.AreEqual(fullLensGlow, EmissionOf(_lensLeft), Tolerance,
                "pod lenses glow warm again when 07's lights are on");

            CaptureChaseCam("lampvisual_after_day");
            CaptureLampBrow("lampvisual_brow_after_day", _headlampPod, new Vector3(0.45f, 0.15f, 0.85f));

            // Snap the sky to earthlit night and capture chase cam after fit at night.
            _scene.Context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.NightReady, true));
            yield return null;
            CaptureChaseCam("lampvisual_after_night");
            CaptureLampBrow("lampvisual_brow_after_night", _headlampPod, new Vector3(0.45f, 0.15f, 0.85f));

            // Cell pods on PodSocket_L/R also clamp in and light warm when BatterySpine is fitted.
            _scene.Context.Events.Publish(
                new PartFitted(RoverSocket.Back, RoverKitPiece.BatterySpine, false, false, default, 0));
            Assert.IsTrue(growth.CellPodsFitted);
            Assert.IsTrue(_cellPodLeft.gameObject.activeSelf);
            Assert.IsTrue(_cellPodRight.gameObject.activeSelf);
            yield return RealRoverScene.WaitSeconds(settings.HeadlampFitSeconds + 0.1f);
            Assert.IsFalse(growth.CellPodsMoving);
            Assert.Greater(EmissionOf(_cellBarLeft), 1f, "the cell pod charge bars light warm once fitted");
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator ALoadedGame_ShowsBothPartsAtOnce_FromTheSavedEvents()
        {
            Directory.CreateDirectory(CaptureFolder);
            Directory.CreateDirectory(LampCaptureFolder);
            yield return _scene.Boot();
            Find();
            RoverGrowth growth = _scene.Rover.Growth;
            GrowthSettings settings = growth.Settings;
            Vector3 retracted = _segment.localPosition;
            Vector3 seated = _igniter.localPosition;
            Vector3 leftHousingRest = _housingLeft.localPosition;
            Quaternion leftHousingRotRest = _housingLeft.localRotation;
            Vector3 rightHousingRest = _housingRight.localPosition;
            Quaternion rightHousingRotRest = _housingRight.localRotation;

            // Snap to earthlit night before restoring the headlamp bay so before_night matches after_night's sky.
            _scene.Context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.NightReady, true));
            yield return null;
            Assert.IsFalse(_headlampPod.gameObject.activeSelf, "headlamp pods are still hidden before load restore");
            CaptureChaseCam("lampvisual_before_night");

            _scene.Context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, true));
            _scene.Context.Events.Publish(new RoverSystemChanged(RoverSystem.Cutter, RoverSystemState.Ok, true));
            _scene.Context.Events.Publish(new RackChanged(0, RackBayState.Filled, true));
            _scene.Context.Events.Publish(new RackChanged(1, RackBayState.Filled, true));

            Assert.AreEqual(settings.AntennaExtendMetres, (_segment.localPosition - retracted).y, Tolerance,
                "the segment is out in the same frame, with no slide");
            Assert.IsTrue(_igniter.gameObject.activeSelf, "the cartridge is shown at once");
            Assert.AreEqual(0f, Vector3.Distance(_igniter.localPosition, seated), Tolerance, "seated, with no click");
            Assert.IsTrue(growth.HeadlampFitted, "loaded Filled headlamp bay marks the headlamp fitted");
            Assert.IsFalse(growth.HeadlampMoving, "loaded Filled headlamp bay has no unfold/clamp motion");
            Assert.IsTrue(_headlampPod.gameObject.activeSelf, "headlamp pods are immediately visible on load");
            Assert.AreEqual(0f, Vector3.Distance(_housingLeft.localPosition, leftHousingRest), Tolerance,
                "Housing_L is already at rest on the load frame");
            Assert.AreEqual(0f, Quaternion.Angle(_housingLeft.localRotation, leftHousingRotRest), 0.05f,
                "Housing_L has no unfold angle on load");
            Assert.AreEqual(0f, Vector3.Distance(_housingRight.localPosition, rightHousingRest), Tolerance,
                "Housing_R is already at rest on the load frame");
            Assert.AreEqual(0f, Quaternion.Angle(_housingRight.localRotation, rightHousingRotRest), 0.05f,
                "Housing_R has no unfold angle on load");
            Assert.Greater(EmissionOf(_lensLeft), 0.5f, "Lens_L is immediately lit warm on load");
            Assert.Greater(EmissionOf(_lensRight), 0.5f, "Lens_R is immediately lit warm on load");
            Assert.IsTrue(growth.CellPodsFitted, "loaded Filled cell_pack bay marks the cell pods fitted");
            Assert.IsFalse(growth.CellPodsMoving, "loaded Filled cell_pack bay has no clamp motion");
            Assert.IsTrue(_cellPodLeft.gameObject.activeSelf);
            Assert.IsTrue(_cellPodRight.gameObject.activeSelf);

            Capture("loaded-antenna-extended", _segment, new Vector3(0.9f, 0.3f, 1.4f));
            Capture("loaded-igniter-seated", _igniter.parent, new Vector3(0.5f, 0.2f, 0.9f));
            yield return RealRoverScene.WaitSeconds(0.5f);
            Assert.AreEqual(settings.AntennaExtendMetres, (_segment.localPosition - retracted).y, Tolerance,
                "and nothing moves afterwards");
            Assert.AreEqual(0f, Vector3.Distance(_housingLeft.localPosition, leftHousingRest), Tolerance,
                "the headlamp housings stay still after load");
        }

        private void Find()
        {
            _segment = Find(_scene.Rover.transform, RoverModelNodes.AntennaSegment2);
            _igniter = Find(_scene.Rover.transform, RoverModelNodes.CutterIgniter);
            Transform browSocket = Find(_scene.Rover.transform, RoverModelNodes.BrowSocket);
            Assert.Greater(browSocket.childCount, 0, "BrowSocket must have the mounted Kit_SSR3Headlamp child.");
            _headlampPod = browSocket.GetChild(0);
            _housingLeft = Find(_headlampPod, RoverModelNodes.HeadlampHousingLeft);
            _housingRight = Find(_headlampPod, RoverModelNodes.HeadlampHousingRight);
            _lensLeft = Find(_headlampPod, RoverModelNodes.HeadlampLensLeft).GetComponent<Renderer>();
            _lensRight = Find(_headlampPod, RoverModelNodes.HeadlampLensRight).GetComponent<Renderer>();
            Transform podLeftSocket = Find(_scene.Rover.transform, RoverModelNodes.PodSocketLeft);
            Transform podRightSocket = Find(_scene.Rover.transform, RoverModelNodes.PodSocketRight);
            _cellPodLeft = podLeftSocket.GetChild(0);
            _cellPodRight = podRightSocket.GetChild(0);
            _cellBarLeft = Find(_cellPodLeft, RoverModelNodes.CellPodBar(0)).GetComponent<Renderer>();
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == name)
                {
                    return node;
                }
            }

            Assert.Fail($"07 has no '{name}' node.");
            return null;
        }

        private static float EmissionOf(Renderer renderer)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            return block.GetFloat(EmissionStrengthId);
        }

        private void CaptureChaseCam(string name)
        {
            FrameCapture.SavePng(_scene.View, CaptureWidth, CaptureHeight,
                Path.Combine(LampCaptureFolder, name + ".png"));
        }

        private void CaptureLampBrow(string name, Transform node, Vector3 offset)
        {
            EnsureLens();
            Transform rover = _scene.Rover.transform;
            _lens.transform.position = node.position + rover.rotation * offset;
            _lens.transform.rotation = Quaternion.LookRotation(node.position - _lens.transform.position, Vector3.up);
            FrameCapture.SavePng(_lens.GetComponent<Camera>(), CaptureWidth, CaptureHeight,
                Path.Combine(LampCaptureFolder, name + ".png"));
        }

        /// <summary>A close shot of <paramref name="node"/> from <paramref name="offset"/> in 07's own frame.</summary>
        private void Capture(string name, Transform node, Vector3 offset)
        {
            EnsureLens();
            Transform rover = _scene.Rover.transform;
            _lens.transform.position = node.position + rover.rotation * offset;
            _lens.transform.rotation = Quaternion.LookRotation(node.position - _lens.transform.position, Vector3.up);
            FrameCapture.SavePng(_lens.GetComponent<Camera>(), CaptureWidth, CaptureHeight,
                Path.Combine(CaptureFolder, name + ".png"));
        }

        private void EnsureLens()
        {
            if (_lens != null)
            {
                return;
            }

            _lens = new GameObject("GrowthLens");
            Camera camera = _lens.AddComponent<Camera>();
            camera.enabled = false;
            camera.fieldOfView = 35f;
            camera.nearClipPlane = 0.05f;
        }
    }
}
