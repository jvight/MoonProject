using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.Autoplay.Tests
{
    /// <summary>
    /// What the bot watches for as a player would: one guidance element at a time (a seeded frame with two must fail
    /// the run), a world cue that counts only once it is lit and in view, and the flag that turns the objective
    /// lines into fallbacks.
    /// </summary>
    public sealed class GuidanceWatchTests
    {
        private const float Frame = 0.1f;

        [Test]
        public void ASeededFrameWithTwoElements_FailsTheCounterOnceTheyOutlastAHandOver()
        {
            var counter = new GuidanceCounter(1f);
            var two = new List<string> { "next-line", "prompt" };
            for (int i = 0; i < 9; i++)
            {
                counter.Step(Frame, two);
            }

            Assert.IsFalse(counter.Failed, "a cross-fade shorter than the hand-over is the design");
            for (int i = 0; i < 3; i++)
            {
                counter.Step(Frame, two);
            }

            Assert.IsTrue(counter.Failed);
            StringAssert.Contains("next-line", counter.Message);
            StringAssert.Contains("prompt", counter.Message);
            Assert.AreEqual(2, counter.MostAtOnce);
        }

        [Test]
        public void OneElementAtATime_NeverFails_AndAGapResetsTheCount()
        {
            var counter = new GuidanceCounter(1f);
            var one = new List<string> { "prompt" };
            var two = new List<string> { "prompt", "controls-card" };
            var none = new List<string>();
            for (int round = 0; round < 5; round++)
            {
                for (int i = 0; i < 8; i++)
                {
                    counter.Step(Frame, two);
                }

                counter.Step(Frame, one);
            }

            counter.Step(Frame, none);
            Assert.IsFalse(counter.Failed, "short overlaps broken by a single element never add up");
        }

        [Test]
        public void TheProbe_ListsTheGuidanceElementsOnScreen_ForTheCounterToJudge()
        {
            var root = new VisualElement();
            var line = new VisualElement { name = "next-line" };
            line.Add(new Label("Build a charging pad beside the lander.") { name = "next-line-text" });
            var chip = new VisualElement { name = "prompt" };
            chip.Add(new Label("E") { name = "prompt-glyph-label" });
            chip.Add(new Label("Scan") { name = "prompt-word" });
            root.Add(line);
            root.Add(chip);
            var probe = new UiProbe(root, null);
            var shown = new List<string>();

            probe.GuidanceShown(shown);
            Assert.AreEqual(2, shown.Count, "a seeded frame with two elements");
            StringAssert.Contains("Build a charging pad", shown[0]);
            StringAssert.Contains("E Scan", shown[1]);

            var counter = new GuidanceCounter(0.5f);
            for (int i = 0; i < 10; i++)
            {
                probe.GuidanceShown(shown);
                counter.Step(Frame, shown);
            }

            Assert.IsTrue(counter.Failed, "the counter fails the seeded frame");

            root.Remove(chip);
            probe.GuidanceShown(shown);
            Assert.AreEqual(1, shown.Count, "with one element the frame is clean");
        }

        [Test]
        public void ThePromptAndLookHintShells_AreOneChipToThePlayer()
        {
            var root = new VisualElement();
            var prompt = new VisualElement { name = "prompt" };
            prompt.Add(new Label("E") { name = "prompt-glyph-label" });
            prompt.Add(new Label("Scan") { name = "prompt-word" });
            var hint = new VisualElement { name = "look-hint" };
            hint.Add(new Label("E") { name = "look-hint-glyph-label" });
            hint.Add(new Label("Scan") { name = "look-hint-word" });
            root.Add(prompt);
            root.Add(hint);
            var shown = new List<string>();
            new UiProbe(root, null).GuidanceShown(shown);
            Assert.AreEqual(1, shown.Count);
        }

        [Test]
        public void AnObjectiveLineWithNoWords_IsNotOnScreen()
        {
            var root = new VisualElement();
            var line = new VisualElement { name = "next-line" };
            line.Add(new Label(string.Empty) { name = "next-line-text" });
            root.Add(line);
            var shown = new List<string>();
            new UiProbe(root, null).GuidanceShown(shown);
            Assert.IsEmpty(shown);
        }

        [Test]
        public void AWorldCue_IsOnScreenOnlyInFrontOfTheCameraAndInsideTheMiddleOfTheView()
        {
            var host = new GameObject("CueCamera");
            try
            {
                Camera camera = host.AddComponent<Camera>();
                host.transform.position = Vector3.zero;
                host.transform.rotation = Quaternion.identity;
                Assert.IsTrue(WorldCue.OnScreen(camera, new Vector3(0f, 0f, 10f), 0.05f), "dead ahead");
                Assert.IsFalse(WorldCue.OnScreen(camera, new Vector3(0f, 0f, -10f), 0.05f), "behind the camera");
                Assert.IsFalse(WorldCue.OnScreen(camera, new Vector3(40f, 0f, 10f), 0.05f), "far off to the side");
                Assert.IsFalse(WorldCue.OnScreen(camera, new Vector3(0f, 40f, 10f), 0.05f), "far above");
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void TheDefaults_ExpectNoLinesAndStrictGuidance_AndTheFlagsTurnThemBack()
        {
            AutoplayOptions.TryParse(new[] { "-autoplay", "opening", "-autoplay-out", "o" }, out AutoplayOptions plain, out _);
            Assert.IsFalse(plain.LinesExpected, "a line on screen is a stall reminder: a failure");
            Assert.IsTrue(plain.StrictGuidance, "two elements together fail the run");
            AutoplayOptions.TryParse(new[] { "-autoplay", "opening", "-autoplay-out", "o", AutoplayOptions.LinesFlag, "1",
                AutoplayOptions.StrictGuidanceFlag, "0" }, out AutoplayOptions old, out _);
            Assert.IsTrue(old.LinesExpected);
            Assert.IsFalse(old.StrictGuidance);
        }
    }
}
