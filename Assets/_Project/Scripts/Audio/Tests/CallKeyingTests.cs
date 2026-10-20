using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Audio.Tests
{
    /// <summary>The call sign the game reads matches the one baked into the call's loops, and keys only inside them.
    /// </summary>
    public sealed class CallKeyingTests
    {
        private static float PythonConstant(string source, string name)
        {
            Match match = Regex.Match(source, "^" + name + @"\s*=\s*([0-9.]+)\s*$", RegexOptions.Multiline);
            Assert.IsTrue(match.Success, $"call.py has no {name}");
            return float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        [Test]
        public void TheSign_MatchesCallPy()
        {
            string path = Path.Combine(Application.dataPath, "..", "tools", "audio", "sampled", "call.py");
            string source = File.ReadAllText(path);
            Assert.AreEqual(PythonConstant(source, "LEAD_S"), CallKeying.Lead, 1e-6f);
            Assert.AreEqual(PythonConstant(source, "SHORT_S"), CallKeying.Short, 1e-6f);
            Assert.AreEqual(PythonConstant(source, "LONG_S"), CallKeying.Long, 1e-6f);
            Assert.AreEqual(PythonConstant(source, "GAP_S"), CallKeying.Gap, 1e-6f);
            StringAssert.Contains("edge = min(0.03, length * 0.3)", source, "the soft edges match MaxEdge / EdgeShare");
        }

        [Test]
        public void TheSign_KeysThreeShortAndOneLong_AndIsSilentAtTheSeam()
        {
            Assert.AreEqual(0f, CallKeying.Envelope(0f), "silent at the loop's seam");
            Assert.AreEqual(0f, CallKeying.Envelope(3.999f));
            Assert.AreEqual(0f, CallKeying.Envelope(5.999f), "the answered loop's seam too");
            float step = CallKeying.Short + CallKeying.Gap;
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(1f, CallKeying.Envelope(CallKeying.Lead + i * step + CallKeying.Short * 0.5f), 1e-5f);
                float between = CallKeying.Lead + i * step + CallKeying.Short + CallKeying.Gap * 0.5f;
                Assert.AreEqual(0f, CallKeying.Envelope(between));
            }

            Assert.AreEqual(1f, CallKeying.Envelope(CallKeying.Lead + 3 * step + CallKeying.Long * 0.5f), 1e-5f);
            float edge = CallKeying.Envelope(CallKeying.Lead + 0.01f);
            Assert.Greater(edge, 0f);
            Assert.Less(edge, 1f, "soft edges, no clicks of light");
        }
    }
}
