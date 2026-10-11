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
using MoonProject.Rover;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// The crawl home in the real Main scene (docs/features/M3-20 §2 and §12), from a seeded save with Tilly home and
    /// the mound's mast lit: 07 drives out toward the mast, its battery runs dry and it sighs into the crawl at about a
    /// third of its pace; a few seconds later Tilly flies out to lead toward the mast and the crawl rises; 07 rolls
    /// onto the mast's 4 m ring, charges, and the crawl ends. Captures the crawl with Tilly leading and the charge on
    /// the ring.
    /// </summary>
    [Category("Battery")]
    public sealed class CrawlSession
    {
        private const float WakeTimeout = 20f;
        private const float CapSettle = 2.5f;
        private const float CapTolerance = 0.05f;
        private const float LeadTimeout = 7f;
        private const float DriveTimeout = 90f;
        private const float StartDistance = 45f;
        private const float RingArrive = 1f;
        private const float RingApproachThrottle = 0.3f;
        private const int CaptureWidth = 1280;
        private const int CaptureHeight = 720;

        // The tower at its third level, relay.0 restored (lit by the tower's reach) and Tilly home and welcomed
        // (FriendState.Awake = 3), in Core's envelope and Gameplay's section formats.
        private const string SeededSave =
            "{\"formatVersion\":1,\"contentVersion\":\"0.5.0\",\"savedAtUtc\":\"2026-10-11T00:00:00Z\"," +
            "\"sections\":[" +
            "{\"key\":\"gameplay.upgrades\",\"version\":1,\"json\":\"{\\\"upgrades\\\":[" +
            "{\\\"id\\\":\\\"radio_tower\\\",\\\"level\\\":3}]}\"}," +
            "{\"key\":\"gameplay.relays\",\"version\":1,\"json\":\"{\\\"masts\\\":[{\\\"id\\\":\\\"relay.0\\\"," +
            "\\\"part\\\":true,\\\"paid\\\":1,\\\"restored\\\":true}]}\"}," +
            "{\"key\":\"gameplay.friends\",\"version\":2,\"json\":\"{\\\"friends\\\":[{\\\"id\\\":\\\"tilly\\\"," +
            "\\\"state\\\":3,\\\"parts\\\":3,\\\"items\\\":0,\\\"discovered\\\":true,\\\"welcomed\\\":true}]}\"}]}";

        private readonly List<string> _problems = new List<string>();
        private GameContext _context;
        private GameplaySystem _gameplay;
        private RoverController _rover;
        private Autopilot _pilot;
        private bool _awake;

        [SetUp]
        public void Setup()
        {
            Application.logMessageReceived += OnLog;
        }

        [TearDown]
        public void TearDown()
        {
            Application.logMessageReceived -= OnLog;
            _rover?.SetDriveSource(null);
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
        [Timeout(600000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator AnEmptyBattery_CrawlsHome_TillyLeads_AndTheMastsRingCharges()
        {
            yield return Boot();
            BatteryModel battery = _gameplay.Battery.Model;
            Vector3 pad = MastPad("relay.0");
            Vector3 home = _context.Get<IWorldLayout>().BasePosition;
            Vector3 outward = pad - home;
            outward.y = 0f;
            outward.Normalize();
            _pilot.GoTo(pad + outward * StartDistance, 2f, 1f);
            yield return Until(() => _pilot.Arrived, DriveTimeout, "07 drives out past the mound's mast");
            Assert.IsFalse(battery.IsLowPower);

            var lowPower = new List<LowPowerChanged>();
            using (_context.Events.Subscribe<LowPowerChanged>(lowPower.Add))
            {
                battery.Restore(new BatterySaveData { cells = battery.Cells, charge = 0f, arcRevealed = true });
                _pilot.GoTo(pad, RingArrive, 1f);
                yield return Until(() => battery.IsLowPower, 1f, "the empty battery sighs into the crawl");
                yield return new WaitForSeconds(CapSettle);
                Assert.AreEqual(_gameplay.Battery.Tuning.CrawlCap, battery.DriveCap01, CapTolerance,
                    "the crawl caps the drive at about a third, eased");
                Assert.IsFalse(battery.FriendLeads);

                yield return Until(() => battery.FriendLeads, LeadTimeout, "Tilly flies out to lead within ~6 s");
                yield return new WaitForSeconds(CapSettle);
                Assert.AreEqual(_gameplay.Battery.Tuning.LeadCap, battery.DriveCap01, CapTolerance,
                    "with Tilly leading the crawl rises");
                IFriendState tilly = Tilly();
                Assert.Less(Flat(tilly.Position - pad), Flat(_rover.Position - pad), "she is ahead, toward the mast");
                Capture("crawl-tilly-leads");

                _pilot.MaxThrottle = RingApproachThrottle;
                yield return Until(() => battery.IsCharging, DriveTimeout, "07 rolls onto the mast's ring and charges");
                Assert.AreEqual(ChargerKind.Mast, battery.Charger);
                Capture("charging-at-the-mound-mast");
                yield return Until(() => !battery.IsLowPower, 2f, "the charge ends the crawl");
                Assert.AreEqual(2, lowPower.Count, "into the crawl once, out of it once");
                yield return Until(() => !battery.FriendLeads, 1f, "Tilly lets go as 07 charges");
            }

            Assert.IsEmpty(_problems, "no errors or exceptions in the log");
        }

        private IEnumerator Boot()
        {
            Directory.CreateDirectory(SaveService.DefaultDirectory);
            File.WriteAllText(Path.Combine(SaveService.DefaultDirectory,
                PlaythroughScene.SaveSlot + SaveService.Extension), SeededSave);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The crawl session loads the scene through the editor.");
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
            _rover = (RoverController)_context.Get<IRoverRig>();
            using (_context.Events.Subscribe<RoverAwoke>(_ => _awake = true))
            {
                yield return Until(() => _awake, WakeTimeout, "07 wakes");
            }

            _pilot = new Autopilot(_rover);
            _rover.SetDriveSource(_pilot);
        }

        private Vector3 MastPad(string id)
        {
            var reach = _context.Get<IStationReach>();
            for (int node = 0; node < reach.NodeCount; node++)
            {
                RelayNode relay = reach.GetNode(node);
                if (relay.Id == id)
                {
                    Assert.IsTrue(relay.Lit, $"{id} is lit in the seeded save");
                    return relay.Position;
                }
            }

            throw new AssertionException($"no mast {id}");
        }

        private IFriendState Tilly()
        {
            var roster = _context.Get<IFriendRoster>();
            for (int i = 0; i < roster.Count; i++)
            {
                if (roster.Get(i).Id == _gameplay.Battery.Tuning.LeadFriend)
                {
                    return roster.Get(i);
                }
            }

            throw new AssertionException("Tilly is in the roster");
        }

        private void Capture(string name)
        {
            FrameCapture.SavePng(_context.Get<IViewCamera>().Camera, CaptureWidth, CaptureHeight,
                Path.Combine(GameplayFixture.CaptureFolder, name + ".png"));
        }

        private static IEnumerator Until(Func<bool> condition, float timeout, string what)
        {
            float started = Time.time;
            while (!condition())
            {
                Assert.Less(Time.time - started, timeout, $"timed out waiting until {what}");
                yield return null;
            }
        }

        private static float Flat(Vector3 offset)
        {
            return new Vector2(offset.x, offset.z).magnitude;
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Log && type != LogType.Warning)
            {
                _problems.Add($"{type}: {message}");
            }
        }
    }
}
