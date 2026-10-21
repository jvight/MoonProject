using NUnit.Framework;

namespace MoonProject.Autoplay.Tests
{
    /// <summary>The first-time player gate's table: times to cue, and FAIL lines for a beat that shows none.</summary>
    public sealed class NaiveGuardTests
    {
        [Test]
        public void AllBeatsWithACue_PassAndListTheirTimes()
        {
            var guard = new NaiveGuard(null);
            guard.Record(new NaiveResult("B3", true, 0.4f, "chip 'E Scan'", 5f, "naive-B3-0s", "naive-B3-5s"));
            guard.Record(new NaiveResult("B4", true, 2.1f, "line 'x'", 5f, "naive-B4-0s", "naive-B4-5s"));
            Assert.IsTrue(guard.Passed);
            string table = guard.Render();
            StringAssert.Contains("| B3 | 0.4 | 5 | chip 'E Scan' | pass |", table);
            StringAssert.Contains("all beats showed one", table);
            StringAssert.DoesNotContain("FAIL B", table);
        }

        [Test]
        public void ABeatWithNoCueInTime_FailsTheGuardWithAFailLine()
        {
            var guard = new NaiveGuard(null);
            guard.Record(new NaiveResult("B3", true, 0.4f, "chip 'E Scan'", 5f, "a", "b"));
            guard.Record(new NaiveResult("B4", false, 5f, "nothing readable on screen (prompt: none)", 5f, "c", "d"));
            Assert.IsFalse(guard.Passed);
            string table = guard.Render();
            StringAssert.Contains("| B4 | - | 5 | nothing readable on screen (prompt: none) | FAIL |", table);
            StringAssert.Contains("FAIL B4: no readable next-action cue within 5 s", table);
            StringAssert.Contains("SOME BEATS SHOWED NONE", table);
        }

        [Test]
        public void TheNaiveFlag_IsOffByDefault_AndOneTurnsItOn()
        {
            AutoplayOptions.TryParse(new[] { "-autoplay", "opening", "-autoplay-out", "o" }, out AutoplayOptions plain, out _);
            Assert.IsFalse(plain.Naive);
            AutoplayOptions.TryParse(new[] { "-autoplay", "opening", "-autoplay-out", "o", AutoplayOptions.NaiveFlag, "1" },
                out AutoplayOptions naive, out _);
            Assert.IsTrue(naive.Naive);
        }
    }
}
