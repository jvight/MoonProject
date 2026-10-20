using System;
using System.Collections;
using System.Collections.Generic;
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
    /// The answer to the call in the real Main scene (opening v3 R8): with the mast raised, night fallen, the call clear
    /// and the reply failed, holding E at the mast sends the pulse out to relay.1, relay.1 answers
    /// (<see cref="OpeningMilestone.CallAnswered"/>, the call turns Answered, two characters decode, the charge floor
    /// lifts), and the pulse runs on to glint on the Peak's dark dish. Once.
    /// </summary>
    [Category("CallAnswer")]
    public sealed class CallAnswerSessions : InputTestFixture
    {
        private const float WakeTimeout = 20f;
        private const float RaiseSlack = 1.5f;
        private const float AnswerTimeout = 40f;

        private readonly List<string> _log = new List<string>();
        private GameContext _context;
        private GameplaySystem _gameplay;
        private Keyboard _keyboard;
        private bool _awake;

        public override void Setup()
        {
            base.Setup();
            _log.Clear();
            _awake = false;
            _gameplay = null;
        }

        public override void TearDown()
        {
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
        [Timeout(300000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator HoldingAtTheRaisedMast_AtNight_AnswersTheCall_Once()
        {
            _keyboard = InputSystem.AddDevice<Keyboard>();
            yield return Boot();
            var call = _context.Get<ICallSource>();
            var answer = _context.Get<IAnswerStatus>();
            Assert.AreEqual(AnswerPhase.NotReady, answer.Phase, "a new game cannot answer yet");

            var terrain = _context.Get<ITerrainQuery>();
            Assert.IsTrue(_context.Get<IWorldAnchors>().TryGet("place.apron", out WorldAnchor apron));
            Vector3 foot = SurfaceRules.OnSurface(terrain, apron.Position.x, apron.Position.z);
            _gameplay.PlacedBase.PlaceUnchecked(_gameplay.Building.Catalog.Find(StructureIds.AntennaMast),
                new StructurePose(foot, 0f), 1f);
            _context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.FirstCharge, false));
            _context.Events.Publish(new OpeningMilestoneReached(OpeningMilestone.NightReady, false));
            _gameplay.Opening.Restore(new OpeningSaveData { nightFallen = true });
            _gameplay.Reply.Restore(new CallSaveData { replyTried = true });
            Assert.AreEqual(CallState.Clear, call.State, "the base is night-ready: the call is clear");
            yield return Wait(MastRaise.TotalSeconds(_gameplay.MastRaiseSettings) + RaiseSlack);
            Assert.AreEqual(AnswerPhase.Ready, answer.Phase, "the mast stands, it is night, the reply failed: ready");

            _context.Get<IRoverPlacement>().PlaceAt(foot + new Vector3(3f, 0f, 0f),
                Quaternion.LookRotation(-Vector3.right, Vector3.up));
            yield return Wait(1f);
            Assert.IsTrue(answer.AtTheMast, "07 stands at the mast");
            Assert.AreEqual(0f, answer.HoldProgress01, "not holding yet");

            Press(_keyboard.eKey);
            yield return Wait(_gameplay.HoldSecondsForTests * 0.5f);
            Assert.Greater(answer.HoldProgress01, 0.3f, "holding fills the ring");
            Assert.AreEqual(AnswerPhase.Ready, answer.Phase);
            float deadline = Time.time + AnswerTimeout;
            while (call.State != CallState.Answered && Time.time < deadline)
            {
                yield return null;
            }

            Release(_keyboard.eKey);
            Assert.AreEqual(CallState.Answered, call.State, "relay.1 answered: the call is answered");
            Assert.AreEqual(3, call.Decoded, "two more characters decoded (the first charge's one, then two)");
            Assert.GreaterOrEqual(_log.IndexOf("pulse0"), 0, "the pulse ran out to relay.1");
            Assert.Less(_log.IndexOf("pulse0"), _log.IndexOf("ack"), "before it answered");
            deadline = Time.time + AnswerTimeout;
            while (!_log.Contains("glint") && Time.time < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(_log.Contains("glint"), "and on to glint on the Peak's dish");
            Assert.AreEqual(1, _log.FindAll(entry => entry == "milestone").Count, "once");
            Assert.AreEqual(AnswerPhase.Done, answer.Phase);
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
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
            throw new NotSupportedException("The call answer sessions load the scene through the editor.");
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
            _context.Events.Subscribe<NetworkPulseLaunched>(pulse => _log.Add("pulse" + pulse.Leg));
            _context.Events.Subscribe<RelayAcked>(_ => _log.Add("ack"));
            _context.Events.Subscribe<PeakDishGlinted>(_ => _log.Add("glint"));
            _context.Events.Subscribe<OpeningMilestoneReached>(reached =>
            {
                if (reached.Milestone == OpeningMilestone.CallAnswered)
                {
                    _log.Add("milestone");
                }
            });
            using (_context.Events.Subscribe<RoverAwoke>(_ => _awake = true))
            {
                float deadline = Time.time + WakeTimeout;
                while (!_awake && Time.time < deadline)
                {
                    yield return null;
                }

                Assert.IsTrue(_awake, "07 wakes");
            }

            yield return null;
        }
    }
}
