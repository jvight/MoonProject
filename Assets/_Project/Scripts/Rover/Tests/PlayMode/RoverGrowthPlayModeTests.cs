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
    /// The growth parts on 07 in the real Main scene (opening v3 2.6): the antenna's second segment slides out at the
    /// first charge, the igniter cartridge seats when the cutter turns OK, and a loaded game shows both silently.
    /// Captures land in Logs/captures/growth/.
    /// </summary>
    public sealed class RoverGrowthPlayModeTests
    {
        private const float FrameTime = 1f / 60f;
        private const float Tolerance = 1e-3f;
        private const int CaptureWidth = 960;
        private const int CaptureHeight = 540;

        private static readonly string CaptureFolder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "growth"));

        private readonly RealRoverScene _scene = new RealRoverScene();
        private Transform _segment;
        private Transform _igniter;
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
            yield return _scene.Boot();
            Find();
            RoverGrowth growth = _scene.Rover.Growth;
            GrowthSettings settings = growth.Settings;
            Vector3 retracted = _segment.localPosition;

            Assert.IsFalse(growth.AntennaExtended, "a new game starts with the antenna's second segment in");
            Assert.IsFalse(growth.IgniterFitted);
            Assert.IsFalse(_igniter.gameObject.activeSelf, "and no igniter cartridge in the socket");
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

            Vector3 seated = _igniter.localPosition;
            _scene.Context.Events.Publish(new RoverSystemChanged(RoverSystem.Cutter, RoverSystemState.Ok, false));
            yield return null;
            Assert.IsTrue(_igniter.gameObject.activeSelf, "the cartridge shows as the cutter is fitted");
            Assert.Greater(Vector3.Distance(_igniter.localPosition, seated), settings.IgniterSeatMetres * 0.5f,
                "starting out of its socket");
            yield return RealRoverScene.WaitSeconds(settings.IgniterSeatSeconds + settings.IgniterClickSeconds + 0.2f);
            Assert.AreEqual(0f, Vector3.Distance(_igniter.localPosition, seated), Tolerance, "seated at home");
            Capture("fitted-igniter-seated", _igniter.parent, new Vector3(0.5f, 0.2f, 0.9f));
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator ALoadedGame_ShowsBothPartsAtOnce_FromTheSavedEvents()
        {
            Directory.CreateDirectory(CaptureFolder);
            yield return _scene.Boot();
            Find();
            RoverGrowth growth = _scene.Rover.Growth;
            GrowthSettings settings = growth.Settings;
            Vector3 retracted = _segment.localPosition;
            Vector3 seated = _igniter.localPosition;

            _scene.Context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, true));
            _scene.Context.Events.Publish(new RoverSystemChanged(RoverSystem.Cutter, RoverSystemState.Ok, true));

            Assert.AreEqual(settings.AntennaExtendMetres, (_segment.localPosition - retracted).y, Tolerance,
                "the segment is out in the same frame, with no slide");
            Assert.IsTrue(_igniter.gameObject.activeSelf, "the cartridge is shown at once");
            Assert.AreEqual(0f, Vector3.Distance(_igniter.localPosition, seated), Tolerance, "seated, with no click");
            Capture("loaded-antenna-extended", _segment, new Vector3(0.9f, 0.3f, 1.4f));
            Capture("loaded-igniter-seated", _igniter.parent, new Vector3(0.5f, 0.2f, 0.9f));
            yield return RealRoverScene.WaitSeconds(0.5f);
            Assert.AreEqual(settings.AntennaExtendMetres, (_segment.localPosition - retracted).y, Tolerance,
                "and nothing moves afterwards");
        }

        private void Find()
        {
            _segment = Find(_scene.Rover.transform, RoverModelNodes.AntennaSegment2);
            _igniter = Find(_scene.Rover.transform, RoverModelNodes.CutterIgniter);
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

        /// <summary>A close shot of <paramref name="node"/> from <paramref name="offset"/> in 07's own frame.</summary>
        private void Capture(string name, Transform node, Vector3 offset)
        {
            if (_lens == null)
            {
                _lens = new GameObject("GrowthLens");
                Camera camera = _lens.AddComponent<Camera>();
                camera.enabled = false;
                camera.fieldOfView = 35f;
                camera.nearClipPlane = 0.05f;
            }

            Transform rover = _scene.Rover.transform;
            _lens.transform.position = node.position + rover.rotation * offset;
            _lens.transform.rotation = Quaternion.LookRotation(node.position - _lens.transform.position, Vector3.up);
            FrameCapture.SavePng(_lens.GetComponent<Camera>(), CaptureWidth, CaptureHeight,
                Path.Combine(CaptureFolder, name + ".png"));
        }
    }
}
