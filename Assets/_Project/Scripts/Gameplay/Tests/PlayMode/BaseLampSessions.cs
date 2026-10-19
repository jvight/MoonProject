using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// The base's lamps follow the Sun (the first nightfall's payoff), in the real Main scene with the four opening
    /// builds standing: in full sun none is lit and none raises <see cref="BaseLampLit"/>; as dusk falls they come on
    /// one by one in the order pad, array, bank, mast, each easing up; at sunrise they all ease off; with nothing
    /// built no lamp lights at dusk. A dusk frame with two lamps lit is captured
    /// (Logs/gameplay-captures/base-lamps-dusk-2.png).
    /// </summary>
    [Category("BaseLamps")]
    public sealed class BaseLampSessions : InputTestFixture
    {
        private static readonly string[] Order =
        {
            StructureIds.ChargingPad, StructureIds.SolarArray, StructureIds.BatteryBank, StructureIds.AntennaMast,
        };

        private const float Spacing = 7f;
        private const float WakeTimeout = 20f;
        private const float SettleSeconds = 0.6f;

        /// <summary>Degrees above last light the Sun still counts as day: past the dusk threshold and its margin.
        /// </summary>
        private const float DayAboveLastLight = 12f;

        /// <summary>Degrees above last light the Sun already counts as dusk.</summary>
        private const float DuskAboveLastLight = 2f;

        private const float SearchStep = 10f;
        private const float SearchLimit = 3000f;
        private const float NightSearchLimit = 12000f;

        /// <summary>Seconds after the second lamp begins to come on that the dusk frame is captured.</summary>
        private const float ShotAfterSecondLamp = 0.8f;

        private const float DuskWatchSeconds = 8f;

        /// <summary>The most lamp lights the base may burn (BaseTuning.LampLightBudget).</summary>
        private const int MaxLampLights = 4;

        private const float NightShotMetres = 15f;
        private const float NightShotRise = 2.2f;
        private const float DawnWatchSeconds = 4f;
        private const float WatchStep = 0.1f;

        private readonly List<BaseLampLit> _lit = new List<BaseLampLit>();
        private GameContext _context;
        private GameplaySystem _gameplay;
        private ISunlight _sunlight;
        private IDisposable _litSubscription;
        private bool _awake;
        private float _daySeconds;
        private float _duskSeconds;
        private float _nightSeconds;

        public override void Setup()
        {
            base.Setup();
            _lit.Clear();
            _awake = false;
            _gameplay = null;
        }

        public override void TearDown()
        {
            _litSubscription?.Dispose();
            Scene scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            base.TearDown();
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator InFullSun_NoLampIsLit()
        {
            yield return Boot();
            Build();
            SetSky(_daySeconds);
            yield return new WaitForSeconds(SettleSeconds);
            Assert.IsFalse(_gameplay.BaseLamps.Dusk, "the Sun is high: it is not dusk");
            Assert.AreEqual(Order.Length, _gameplay.BaseLamps.Count, "every piece carries a lamp");
            yield return new WaitForSeconds(DawnWatchSeconds);
            AssertAllLevels(0f, "in full sun every lamp is dark");
            Assert.IsEmpty(_lit, "no lamp lights in full sun");
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator AsDuskFalls_TheLampsComeOnOneByOne_InOrder_AndEase()
        {
            yield return Boot();
            int[] ids = Build();
            SetSky(_daySeconds);
            yield return new WaitForSeconds(SettleSeconds);
            PlaceRoverLookingAtTheBase();
            SetSky(_duskSeconds);

            bool shot = false;
            float secondAt = -1f;
            float until = Time.time + DuskWatchSeconds;
            var seenLevels = new List<float>();
            while (Time.time < until && _gameplay.BaseLamps.LevelOf(ids[Order.Length - 1]) < 1f)
            {
                yield return new WaitForSeconds(WatchStep);
                seenLevels.Add(_gameplay.BaseLamps.LevelOf(ids[0]));
                if (_lit.Count >= 2 && secondAt < 0f)
                {
                    secondAt = Time.time;
                }

                if (!shot && secondAt >= 0f && Time.time - secondAt >= ShotAfterSecondLamp)
                {
                    shot = true;
                    Assert.AreEqual(2, _lit.Count, "two lamps are lit when the dusk frame is taken");
                    FrameCapture.SavePng(_context.Get<IViewCamera>().Camera, 1920, 1080,
                        Path.Combine(GameplayFixture.CaptureFolder, "base-lamps-dusk-2.png"));
                }
            }

            Assert.AreEqual(Order.Length, _lit.Count, "every lamp raised its beat once");
            for (int i = 0; i < Order.Length; i++)
            {
                Assert.AreEqual(ids[i], _lit[i].InstanceId, $"lamp {i} is the {Order[i]}'s");
                Assert.AreEqual(i, _lit[i].Index, $"the {Order[i]}'s lamp is {i}th in the lighting order");
            }

            Assert.IsTrue(shot, "the dusk frame with two lamps lit was captured");
            AssertAllLevels(1f, "every lamp is at full once dusk has run");
            Assert.IsTrue(seenLevels.Exists(level => level > 0.05f && level < 0.95f),
                "the first lamp was seen part way up: it eases, it does not snap");
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator AtSunrise_EveryLampEasesOff()
        {
            yield return Boot();
            int[] ids = Build();
            SetSky(_duskSeconds);
            yield return new WaitForSeconds(DuskWatchSeconds);
            AssertAllLevels(1f, "the lamps are lit in the dusk");
            _lit.Clear();

            SetSky(_daySeconds);
            float last = 1f;
            bool partWay = false;
            float until = Time.time + DawnWatchSeconds;
            while (Time.time < until)
            {
                yield return new WaitForSeconds(WatchStep);
                float level = _gameplay.BaseLamps.LevelOf(ids[0]);
                Assert.LessOrEqual(level, last + 1e-3f, "at sunrise the lamps only dim");
                partWay |= level > 0.05f && level < 0.95f;
                last = level;
            }

            Assert.IsTrue(partWay, "the lamps ease off: they were seen part way down");
            AssertAllLevels(0f, "every lamp is dark once the Sun is up");
            Assert.IsEmpty(_lit, "sunrise lights nothing");
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator WithNothingBuilt_DuskLightsNothing()
        {
            yield return Boot();
            SetSky(_duskSeconds);
            yield return new WaitForSeconds(DuskWatchSeconds / 2f);
            Assert.IsTrue(_gameplay.BaseLamps.Dusk, "the Sun is low: it is dusk");
            Assert.AreEqual(0, _gameplay.BaseLamps.Count, "no piece, no lamp");
            Assert.IsEmpty(_lit, "no base, no lamps at dusk");
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator TheLampsThrowWarmLight_NoMoreThanTheBudget_NeverWithShadows_AndOnlyWhenLit()
        {
            yield return Boot();
            Build();
            SetSky(_daySeconds);
            yield return new WaitForSeconds(SettleSeconds);
            Assert.AreEqual(0, LitLights().Count, "in full sun no lamp throws light");

            SetSky(_nightSeconds);
            yield return new WaitForSeconds(DuskWatchSeconds);
            List<Light> lights = LitLights();
            Assert.AreEqual(Order.Length, lights.Count, "each lamp of the four throws a warm light at night");
            Assert.LessOrEqual(lights.Count, MaxLampLights, "the base burns no more lights than its budget");
            foreach (Light light in lights)
            {
                Assert.AreEqual(LightShadows.None, light.shadows, "a lamp light casts no shadow");
                Assert.Greater(light.color.r, light.color.b, "and is a warm amber");
            }

            Light spot = lights.Find(light => light.type == LightType.Spot);
            Assert.IsNotNull(spot, "the pad's light is a spot");
            Assert.AreEqual(1, lights.FindAll(light => light.type == LightType.Spot).Count, "and only the pad's");
            Assert.Less(spot.transform.forward.y, -0.9f, "pointing straight down at the pad");
        }

        [UnityTest]
        [Timeout(200000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator ANightFrame_FromAbout15Metres_ShowsTheBaseLit()
        {
            yield return Boot();
            Build();
            SetSky(_nightSeconds);
            yield return new WaitForSeconds(DuskWatchSeconds);
            Camera lens = _context.Get<IViewCamera>().Camera;
            var terrain = _context.Get<ITerrainQuery>();
            _context.Get<IWorldAnchors>().TryGet("place.apron", out WorldAnchor apron);
            Vector3 middle = SurfaceRules.OnSurface(terrain, apron.Position.x, apron.Position.z) + Vector3.up * 1.2f;
            Vector3 eye = middle + new Vector3(0f, NightShotRise, -NightShotMetres);
            Vector3 position = lens.transform.position;
            Quaternion rotation = lens.transform.rotation;
            lens.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(middle - eye, Vector3.up));
            FrameCapture.SavePng(lens, 1920, 1080, Path.Combine(GameplayFixture.CaptureFolder, "base-lamps-night-15m.png"));
            lens.transform.SetPositionAndRotation(position, rotation);
            Assert.AreEqual(Order.Length, LitLights().Count, "the base is lit in the night frame");
        }

        private static List<Light> LitLights()
        {
            var lit = new List<Light>();
            foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.InstanceID))
            {
                if (light.name == "BaseLampLight" && light.enabled && light.intensity > 0.01f)
                {
                    lit.Add(light);
                }
            }

            return lit;
        }

        private void AssertAllLevels(float expected, string why)
        {
            foreach (PlacedStructure placed in _gameplay.PlacedBase.Structures)
            {
                Assert.AreEqual(expected, _gameplay.BaseLamps.LevelOf(placed.InstanceId), 1e-3f,
                    $"{why} ({placed.Definition.Id})");
            }
        }

        /// <summary>The four opening builds, finished, in a row on the apron; their instance ids in lamp order.</summary>
        private int[] Build()
        {
            var terrain = _context.Get<ITerrainQuery>();
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet("place.apron", out WorldAnchor apron),
                "the World has the apron");
            var ids = new int[Order.Length];
            for (int i = 0; i < Order.Length; i++)
            {
                float x = apron.Position.x + (i - 1.5f) * Spacing;
                Vector3 at = SurfaceRules.OnSurface(terrain, x, apron.Position.z);
                ids[i] = _gameplay.PlacedBase.PlaceUnchecked(_gameplay.Building.Catalog.Find(Order[i]),
                    new StructurePose(at, 0f), 1f);
            }

            return ids;
        }

        private void PlaceRoverLookingAtTheBase()
        {
            var terrain = _context.Get<ITerrainQuery>();
            _context.Get<IWorldAnchors>().TryGet("place.apron", out WorldAnchor apron);
            Vector3 at = SurfaceRules.OnSurface(terrain, apron.Position.x, apron.Position.z - Spacing);
            _context.Get<IRoverPlacement>().PlaceAt(at, Quaternion.LookRotation(Vector3.forward, Vector3.up));
        }

        private void SetSky(float seconds)
        {
            MethodInfo set = _sunlight.GetType().GetMethod("SetClockTime", BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(set, "World's sky clock can be set for the tests");
            set.Invoke(_sunlight, new object[] { seconds, false, true });
        }

        private float Elevation()
        {
            return Mathf.Asin(Mathf.Clamp(_sunlight.SunDirection.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        /// <summary>The clock times at which the Sun stands high (day) and low (dusk), found by setting the clock.
        /// </summary>
        private void FindTimes()
        {
            float last = _sunlight.LastLightElevationDegrees;
            _daySeconds = -1f;
            _duskSeconds = -1f;
            _nightSeconds = -1f;
            for (float t = 0f; t <= SearchLimit && (_daySeconds < 0f || _duskSeconds < 0f); t += SearchStep)
            {
                SetSky(t);
                float elevation = Elevation();
                if (_daySeconds < 0f && elevation >= last + DayAboveLastLight)
                {
                    _daySeconds = t;
                }
                else if (_daySeconds >= 0f && _duskSeconds < 0f && elevation <= last + DuskAboveLastLight)
                {
                    _duskSeconds = t;
                }
            }

            Assert.GreaterOrEqual(_daySeconds, 0f, "the first day has a high Sun");
            Assert.Greater(_duskSeconds, _daySeconds, "and a dusk after it");
            for (float t = _duskSeconds; t <= NightSearchLimit && _nightSeconds <= _duskSeconds; t += SearchStep)
            {
                SetSky(t);
                if (_sunlight.Phase == SkyPhase.Earthlit)
                {
                    _nightSeconds = t;
                }
            }

            Assert.Greater(_nightSeconds, _duskSeconds, "and a night after the dusk");
        }

        private IEnumerator Boot()
        {
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The base lamp sessions load the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>();
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(_gameplay, "the scene has the gameplay system");
            Assert.IsNotNull(_gameplay.BaseLamps, "the base's lamps are wired");
            _context = bootstrap.Context;
            _sunlight = _context.Get<ISunlight>();
            _litSubscription = _context.Events.Subscribe<BaseLampLit>(_lit.Add);
            using (_context.Events.Subscribe<RoverAwoke>(_ => _awake = true))
            {
                float deadline = Time.time + WakeTimeout;
                while (!_awake && Time.time < deadline)
                {
                    yield return null;
                }

                Assert.IsTrue(_awake, "07 wakes");
            }

            FindTimes();
            SetSky(_daySeconds);
            yield return null;
            yield return null;
            _lit.Clear();
        }
    }
}
