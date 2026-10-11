using System;
using System.Collections.Generic;
using NUnit.Framework;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The wordmark's SVG path parser: absolute moves, cubics and closes as written, repeated cubics, circular arcs
    /// resolved to a centre and angles in the right direction, and anything unsupported refused rather than skipped.
    /// </summary>
    public sealed class SvgPathParserTests
    {
        private const float Tolerance = 0.001f;

        private List<WordmarkCommand> _commands;
        private List<float> _values;

        [SetUp]
        public void SetUp()
        {
            _commands = new List<WordmarkCommand>();
            _values = new List<float>();
        }

        [Test]
        public void MoveCubicsAndClose_KeepTheirNumbers()
        {
            SvgPathParser.Parse("M18 70.67C18 53.8 1e1,-2 3 4 5 6 7 8 9 10Z", _commands, _values);

            Assert.That(_commands, Is.EqualTo(new[]
            {
                WordmarkCommand.MoveTo, WordmarkCommand.CubicTo, WordmarkCommand.CubicTo, WordmarkCommand.Close,
            }));
            Assert.That(_values, Is.EqualTo(new[] { 18f, 70.67f, 18f, 53.8f, 10f, -2f, 3f, 4f, 5f, 6f, 7f, 8f, 9f, 10f })
                .Within(Tolerance));
        }

        [Test]
        public void AHalfCircle_FindsItsCentreAndSweepsClockwise()
        {
            SvgPathParser.Parse("M0 0A10 10 0 0 1 20 0", _commands, _values);

            Assert.That(_commands[1], Is.EqualTo(WordmarkCommand.ArcTo));
            float[] arc = _values.GetRange(2, 6).ToArray();
            Assert.That(arc, Is.EqualTo(new[] { 10f, 0f, 10f, 180f, 360f, 1f }).Within(Tolerance));
        }

        [Test]
        public void TheLargeArcFlag_TakesTheLongWayRound()
        {
            SvgPathParser.Parse("M0 0A10 10 0 1 0 10 10", _commands, _values);

            float[] arc = _values.GetRange(2, 6).ToArray();
            Assert.That(arc[0], Is.EqualTo(0f).Within(Tolerance));
            Assert.That(arc[1], Is.EqualTo(10f).Within(Tolerance));
            Assert.That(arc[4] - arc[3], Is.EqualTo(-270f).Within(Tolerance));
            Assert.That(arc[5], Is.EqualTo(0f));
        }

        [TestCase("M0 0L10 10")]
        [TestCase("m0 0")]
        [TestCase("M0 0 5 5")]
        [TestCase("M0 0A10 5 0 0 1 20 0")]
        [TestCase("M0 0C1 2 3")]
        public void UnsupportedPaths_AreFormatErrors(string data)
        {
            Assert.Throws<FormatException>(() => SvgPathParser.Parse(data, _commands, _values));
        }
    }
}
