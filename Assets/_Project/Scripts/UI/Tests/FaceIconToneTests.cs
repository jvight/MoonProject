using System;
using System.Xml.Linq;
using NUnit.Framework;
using UnityEngine;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// Two-tone face icons (Argon's silhouettes: a white body and a second tone at 45%, tinted by the screen) stay
    /// vector: the reader turns each fill-opacity into a filled even-odd tone, the painter draws a tone at its share of
    /// the tint, and the old line icons still read as strokes.
    /// </summary>
    public sealed class FaceIconToneTests
    {
        private const string TwoTone =
            "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 256 256\">" +
            "<g fill=\"#FFFFFF\" fill-opacity=\"0.45\" fill-rule=\"evenodd\">" +
            "<path d=\"M 20 20 L 120 20 L 120 120 L 20 120 Z\" stroke=\"none\" stroke-width=\"4\" /></g>" +
            "<g fill=\"#FFFFFF\" fill-rule=\"evenodd\">" +
            "<path d=\"M 136 20 L 236 20 L 236 120 L 136 120 Z M 160 44 L 212 44 L 212 96 L 160 96 Z\" " +
            "stroke=\"none\" stroke-width=\"11.5\" /></g></svg>";

        private const string Line =
            "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 256 256\">" +
            "<path d=\"M 20 20 L 200 200\" stroke=\"#FFE29E\" stroke-width=\"5.20\" fill=\"none\" />" +
            "<circle cx=\"60\" cy=\"60\" r=\"5\" stroke=\"#FFE29E\" stroke-width=\"3.80\" fill=\"none\" /></svg>";

        [Test]
        public void ATwoToneSilhouette_ReadsAsOneFilledToneForEachOpacity_InTheOrderTheyPaint()
        {
            UiIconShape icon = FaceArtReader.ParseIcon("icon_test", XElement.Parse(TwoTone), "two-tone");
            Assert.IsNull(icon.Validate());
            Assert.AreEqual(2, icon.Tones.Length, "the 45% tone and the body");
            Assert.AreEqual(0.45f, icon.Tones[0].Opacity, 1e-4f, "the second tone first, as the SVG paints it");
            Assert.AreEqual(1f, icon.Tones[1].Opacity, 1e-4f, "the body at full strength");
            Assert.IsTrue(icon.Primary.IsEmpty, "a silhouette has no stroke");
            Assert.AreEqual(2, CountOf(icon.Tones[1].Path, WordmarkCommand.MoveTo),
                "the body keeps its window as a second subpath, cut out by the even-odd rule");
            Assert.AreEqual(5f, icon.Tones[0].Path.Values[0], 1e-4f, "256 units scale to the painter's 64");
        }

        [Test]
        public void AFortyFivePercentTone_IsPaintedAtFortyFivePercentOfTheTint()
        {
            var tint = new Color32(224, 210, 182, 248);
            Color32 tone = UiIconPainter.ToneColor(tint, 0.45f);
            Assert.AreEqual(tint.r, tone.r);
            Assert.AreEqual(tint.g, tone.g);
            Assert.AreEqual(tint.b, tone.b);
            Assert.AreEqual(Mathf.RoundToInt(248 * 0.45f), tone.a, "45% of the tint's alpha");
            Assert.AreEqual(tint, UiIconPainter.ToneColor(tint, 1f), "the body is the tint itself");
        }

        [Test]
        public void TheLineIcons_StillReadAsPrimaryAndDetailStrokes()
        {
            UiIconShape icon = FaceArtReader.ParseIcon("icon_line", XElement.Parse(Line), "line");
            Assert.IsNull(icon.Validate());
            Assert.AreEqual(0, icon.Tones.Length);
            Assert.IsFalse(icon.Primary.IsEmpty, "the 5.2-unit outline is primary");
            Assert.IsFalse(icon.Detail.IsEmpty, "the 3.8-unit circle is detail");
        }

        [Test]
        public void AFillWithoutTheEvenOddRule_FailsLoudly()
        {
            string nonZero = TwoTone.Replace("fill-rule=\"evenodd\"", string.Empty);
            Assert.Throws<InvalidOperationException>(
                () => FaceArtReader.ParseIcon("icon_bad", XElement.Parse(nonZero), "non-zero"));
        }

        private static int CountOf(UiIconPath path, WordmarkCommand command)
        {
            int count = 0;
            foreach (WordmarkCommand c in path.Commands)
            {
                count += c == command ? 1 : 0;
            }

            return count;
        }
    }
}
