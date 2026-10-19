using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Rover;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// The scan target mark in the real Main scene: with 07 beside the lander's open crate and the view on its first
    /// fragment, the wire box rests on it and the scan prompt is up (captured); with the view turned away there is
    /// neither the prompt's candidate nor the box (captured).
    /// </summary>
    public sealed class ScanPromptCaptureSessions : InputTestFixture
    {
        private const string FragmentId = "ruin." + ScanTuning.LanderCargoId + ".0";
        private const float StandOffMetres = 5f;
        private const float SettleSeconds = 1f;
        private const float WakeTimeout = 120f;
        private const float MarkTimeout = 5f;
        private const float BesideMetres = 2.5f;
        private const float TurnAwayDegrees = 180f;

        private GameContext _context;
        private GameplaySystem _gameplay;
        private Scene _scene;
        private bool _awake;

        public override void TearDown()
        {
            if (_scene.IsValid() && _scene.isLoaded)
            {
                foreach (GameObject root in _scene.GetRootGameObjects())
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
        public IEnumerator TheTargetIsMarked_WhenTheViewRestsOnIt_AndNothingIsWhenItTurnsAway()
        {
            yield return Boot();
            ScanFragment fragment = _gameplay.Scans.Find(FragmentId);
            Assert.IsNotNull(fragment, "the lander cargo fragment keeps its id");
            Vector3 toward = Flat(fragment.Point - _context.Get<IRoverState>().Position).normalized;
            Vector3 beside = Vector3.Cross(Vector3.up, toward);
            Vector3 stand = Flat(fragment.Point) - toward * StandOffMetres + beside * BesideMetres;
            float bearing = Quaternion.LookRotation(toward, Vector3.up).eulerAngles.y;

            yield return StandAt(stand, bearing, bearing);
            yield return Until(() => _gameplay.Scans.Candidate == fragment && _gameplay.ScanMark.Visible >= 1f,
                MarkTimeout, "the view on the fragment makes it the candidate and the box fades in");
            Assert.AreSame(fragment, _gameplay.ScanMark.Shown);
            Capture("scan-prompt-marked");

            yield return StandAt(stand, bearing, bearing + TurnAwayDegrees);
            Capture("scan-prompt-turned-away");
            yield return Until(() => _gameplay.Scans.Candidate == null && _gameplay.ScanMark.Visible <= 0f,
                MarkTimeout, () => "turned away, there is no candidate and the box has gone (candidate " +
                                   (_gameplay.Scans.Candidate != null ? _gameplay.Scans.Candidate.FragmentId : "none") +
                                   ")");
        }

        private IEnumerator StandAt(Vector3 spot, float heading, float cameraBearing)
        {
            _context.Get<IRoverPlacement>().PlaceAt(spot, Quaternion.Euler(0f, heading, 0f));
            var rig = (RoverCameraRig)_context.Get<IViewCamera>();
            rig.Orbit.AdoptBearing(cameraBearing);
            yield return new WaitForSeconds(SettleSeconds);
        }

        private void Capture(string name)
        {
            Camera lens = _context.Get<IViewCamera>().Camera;
            FrameCapture.SavePng(lens, 1280, 720, Path.Combine(GameplayFixture.CaptureFolder, name + ".png"));
        }

        private IEnumerator Boot()
        {
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The scan prompt sessions load the scene through the editor.");
#endif
            _scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            GameBootstrap bootstrap = null;
            foreach (GameObject root in _scene.GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>();
            }

            Assert.IsNotNull(bootstrap, "the scene has its bootstrap");
            Assert.IsNotNull(_gameplay, "the scene has the gameplay system");
            _context = bootstrap.Context;
            using (_context.Events.Subscribe<RoverAwoke>(_ => _awake = true))
            {
                yield return Until(() => _awake, WakeTimeout, "07 wakes");
            }
        }

        private static IEnumerator Until(Func<bool> condition, float timeout, string what)
        {
            return Until(condition, timeout, () => what);
        }

        private static IEnumerator Until(Func<bool> condition, float timeout, Func<string> what)
        {
            float deadline = Time.time + timeout;
            while (!condition() && Time.time < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(condition(), "timed out waiting for: " + what());
        }

        private static Vector3 Flat(Vector3 point)
        {
            return new Vector3(point.x, 0f, point.z);
        }
    }
}
