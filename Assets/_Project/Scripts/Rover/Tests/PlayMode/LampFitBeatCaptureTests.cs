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
    /// The headlamp fitted live at the rack gets its beat, in the real Main scene: the camera eases round to a front
    /// three-quarter frame of 07's head and back, 07 chirps once (happy) after the pods clamp, and its head looks out
    /// to each side so the lamp's first beam sweeps across the ground. Never twice. A capture strip goes to
    /// Logs/captures/lamp-fit/.
    /// </summary>
    public sealed class LampFitBeatCaptureTests
    {
        private const float FrameTime = 1f / 60f;
        private const float SettleSeconds = 1.2f;
        private const float BeatSeconds = 6.5f;
        private const float StripStep = 0.4f;
        private const float InFrontDot = 0.2f;
        private const float BackAgain = 0.05f;

        private static readonly string Folder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "lamp-fit"));

        private readonly RealRoverScene _scene = new RealRoverScene();
        private int _happyChirps;

        [SetUp]
        public void SetUp()
        {
            Time.captureDeltaTime = FrameTime;
            _happyChirps = 0;
        }

        [TearDown]
        public void TearDown()
        {
            Time.captureDeltaTime = 0f;
            _scene.Unload();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator AFittedHeadlamp_ShowsItself_FromTheFront_ThenTheCameraGoesBack()
        {
            Directory.CreateDirectory(Folder);
            yield return _scene.Boot();
            _scene.Context.Events.Subscribe<RoverChirpRequested>(chirp =>
            {
                if (chirp.Mood == ChirpMood.Happy)
                {
                    _happyChirps++;
                }
            });
            yield return RealRoverScene.WaitSeconds(SettleSeconds);
            FrameCapture.SavePng(_scene.View, 1920, 1080, Path.Combine(Folder, "strip-00-before.png"));

            _scene.Context.Events.Publish(new PartFitted(RoverSocket.Lamp, RoverKitPiece.LampBar, false, false,
                default, 0));
            float start = Time.time;
            float nextShot = StripStep;
            int shot = 1;
            float peak = 0f;
            float mostInFront = -1f;
            while (Time.time - start < BeatSeconds)
            {
                yield return null;
                peak = Mathf.Max(peak, _scene.CameraRig.MomentWeight);
                Vector3 toLens = _scene.View.transform.position - _scene.Rover.Position;
                toLens.y = 0f;
                float front = Vector3.Dot(toLens.normalized, _scene.Rover.transform.forward);
                if (_scene.CameraRig.MomentWeight > 0.95f)
                {
                    mostInFront = Mathf.Max(mostInFront, front);
                }

                if (Time.time - start >= nextShot)
                {
                    FrameCapture.SavePng(_scene.View, 1920, 1080,
                        Path.Combine(Folder, $"strip-{shot++:D2}-t{Time.time - start:F1}.png"));
                    nextShot += StripStep;
                }
            }

            Debug.Log($"[lampfit] camera weight peak {peak:F2}, most in front {mostInFront:F2}, " +
                      $"happy chirps {_happyChirps}, weight now {_scene.CameraRig.MomentWeight:F2}");
            Assert.Greater(peak, 0.95f, "the camera eased to the head");
            Assert.Greater(mostInFront, InFrontDot, "from in front of 07, not behind it");
            Assert.AreEqual(1, _happyChirps, "one small happy chirp");
            Assert.Less(_scene.CameraRig.MomentWeight, BackAgain, "and the camera is back behind 07");
        }
    }
}
