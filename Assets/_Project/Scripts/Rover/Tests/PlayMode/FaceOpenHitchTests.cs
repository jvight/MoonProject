using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Profiling;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Testing;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// The hitch when Tab opens and closes the hold on 07's face (owner playtest): real frame times (no fixed capture
    /// step) of the frames around each press, over several opens and closes in the real Main scene, with the managed
    /// memory each frame grew by. Writes Logs/captures/face_hitch/report.md; the first open is reported apart since a
    /// first use pays for what a later one does not.
    /// </summary>
    public sealed class FaceOpenHitchTests : InputTestFixture
    {
        private const float OpenTimeout = 8f;
        private const float SettleSeconds = 2f;
        private const float WatchSeconds = 1.6f;
        private const float HitchMilliseconds = 20f;

        // The editor's frames carry noise a player build does not (a second Unity, asset import): a later open or close
        // over this is a regression worth failing for; the 20 ms target is read off the report and the IL2CPP bot.
        private const float EditorBudgetMilliseconds = 100f;
        private const int Cycles = 4;
        private const long GrowthBytes = 4096;

        private static readonly string Folder =
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "captures", "face_hitch"));

        private readonly RealRoverScene _scene = new RealRoverScene();
        private readonly StringBuilder _report = new StringBuilder();
        private Keyboard _keyboard;

        public override void TearDown()
        {
            _scene.Unload();
            base.TearDown();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(RoverSessionScene))]
        [PostBuildCleanup(typeof(RoverSessionScene))]
        public IEnumerator OpeningAndClosingTheFace_HasNoFrameOverTheEditorBudget_AfterTheFirstOpen()
        {
            Directory.CreateDirectory(Folder);
            _keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();
            yield return _scene.Boot();
            IRoverFaceScreen face = _scene.Context.Get<IRoverFaceScreen>();
            yield return RealRoverScene.WaitSeconds(SettleSeconds);

            _report.AppendLine("# Face open/close frame times (ms), real time");
            _report.AppendLine();
            _report.AppendLine("| cycle | phase | worst | frames over " + HitchMilliseconds + " ms (ms after the press) | " +
                               "frames that grew managed memory by >" + GrowthBytes + " B |");
            _report.AppendLine("|---|---|---|---|---|");
            float worstLater = 0f;
            for (int cycle = 0; cycle < Cycles; cycle++)
            {
                float open = 0f;
                yield return Watch(_keyboard.tabKey, face, true, cycle, "open", result => open = result);
                yield return RealRoverScene.WaitSeconds(SettleSeconds);
                if (cycle < 2)
                {
                    float flip = 0f;
                    yield return Flip(_keyboard.qKey, face, cycle, "databank (Q)", result => flip = result);
                    yield return RealRoverScene.WaitSeconds(SettleSeconds);
                    yield return Flip(_keyboard.qKey, face, cycle, "hold (Q)", result => flip = result);
                    yield return RealRoverScene.WaitSeconds(SettleSeconds);
                    if (cycle > 0)
                    {
                        worstLater = Mathf.Max(worstLater, flip);
                    }
                }

                float close = 0f;
                yield return Watch(_keyboard.tabKey, face, false, cycle, "close", result => close = result);
                yield return RealRoverScene.WaitSeconds(SettleSeconds);
                if (cycle > 0)
                {
                    worstLater = Mathf.Max(worstLater, Mathf.Max(open, close));
                }
            }

            File.WriteAllText(Path.Combine(Folder, "report.md"), _report.ToString());
            Debug.Log("[facehitch]\n" + _report);
            Assert.LessOrEqual(worstLater, EditorBudgetMilliseconds,
                "no frame over the editor budget on a later open or close");
        }

        private IEnumerator Flip(KeyControl key, IRoverFaceScreen face, int cycle, string phase, System.Action<float> worst)
        {
            var slow = new List<string>();
            float worstMs = 0f;
            Press(key);
            float start = Time.realtimeSinceStartup;
            yield return null;
            Release(key);
            while (Time.realtimeSinceStartup - start < WatchSeconds)
            {
                float ms = Time.unscaledDeltaTime * 1000f;
                worstMs = Mathf.Max(worstMs, ms);
                if (ms > HitchMilliseconds)
                {
                    slow.Add($"{ms:F0} ({(Time.realtimeSinceStartup - start) * 1000f:F0})");
                }

                yield return null;
            }

            _report.AppendLine($"| {cycle} | {phase} | {worstMs:F0} | {string.Join(", ", slow)} | |");
            worst(worstMs);
        }

        private IEnumerator Watch(KeyControl key, IRoverFaceScreen face, bool opening, int cycle, string phase,
            System.Action<float> worst)
        {
            var slow = new List<string>();
            var grew = new List<string>();
            float worstMs = 0f;
            long last = Profiler.GetMonoUsedSizeLong();
            Press(key);
            float start = Time.realtimeSinceStartup;
            yield return null;
            Release(key);
            while (Time.realtimeSinceStartup - start < WatchSeconds)
            {
                float ms = Time.unscaledDeltaTime * 1000f;
                float at = (Time.realtimeSinceStartup - start) * 1000f;
                worstMs = Mathf.Max(worstMs, ms);
                if (ms > HitchMilliseconds)
                {
                    slow.Add($"{ms:F0} ({at:F0})");
                }

                long used = Profiler.GetMonoUsedSizeLong();
                if (used - last > GrowthBytes)
                {
                    grew.Add($"{(used - last) / 1024}K ({at:F0})");
                }

                last = used;
                yield return null;
            }

            Assert.AreEqual(opening, face.IsOpen, $"cycle {cycle}: the face is {(opening ? "open" : "closed")}");
            _report.AppendLine($"| {cycle} | {phase} | {worstMs:F0} | {string.Join(", ", slow)} | " +
                               $"{string.Join(", ", grew)} |");
            worst(worstMs);
        }
    }
}
