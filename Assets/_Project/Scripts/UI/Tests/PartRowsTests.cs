using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    /// <summary>The pickup rows of story parts: a free row first, the same part feeds its own row, the oldest gives way.</summary>
    public sealed class PartRowsTests
    {
        private const float Linger = 4f;

        private PartRows _rows;

        [SetUp]
        public void SetUp()
        {
            _rows = new PartRows(2, Linger);
        }

        [Test]
        public void ThePartsTaken_OpenOnFreeRows_InOrder()
        {
            int first = _rows.Claim("part.igniter", out bool firstOpened);
            int second = _rows.Claim("part.icecore", out bool secondOpened);
            Assert.IsTrue(firstOpened && secondOpened);
            Assert.AreNotEqual(first, second);
            Assert.AreEqual("part.igniter", _rows.PartOf(first));
            Assert.AreEqual("part.icecore", _rows.PartOf(second));
        }

        [Test]
        public void ThePartTakenAgain_FeedsItsOwnRow_AndRestartsItsRest()
        {
            int row = _rows.Claim("part.igniter", out _);
            _rows.Tick(Linger - 0.5f);
            int again = _rows.Claim("part.igniter", out bool opened);
            Assert.AreEqual(row, again);
            Assert.IsFalse(opened, "no second row for the same part");
            _rows.Tick(Linger - 0.5f);
            Assert.IsFalse(_rows.ShouldFade(row), "the rest began again");
        }

        [Test]
        public void ARowFades_AfterItsRest_AndIsFreeOnceReleased()
        {
            int row = _rows.Claim("part.igniter", out _);
            _rows.Tick(Linger - 0.1f);
            Assert.IsFalse(_rows.ShouldFade(row));
            _rows.Tick(0.2f);
            Assert.IsTrue(_rows.ShouldFade(row));

            _rows.Release(row);
            Assert.IsFalse(_rows.IsActive(row));
            Assert.IsFalse(_rows.ShouldFade(row));
            Assert.AreEqual(row, _rows.Claim("part.icecore", out bool opened));
            Assert.IsTrue(opened);
        }

        [Test]
        public void WhenEveryRowIsUp_ANewPartTakesTheOneThatHasRestedLongest()
        {
            int old = _rows.Claim("part.igniter", out _);
            _rows.Tick(2f);
            int young = _rows.Claim("part.icecore", out _);
            _rows.Tick(1f);

            int row = _rows.Claim("part.third", out bool opened);
            Assert.IsTrue(opened);
            Assert.AreEqual(old, row);
            Assert.AreEqual("part.icecore", _rows.PartOf(young), "the younger row stays");
        }

        [Test]
        public void APartRow_NeedsAPartId()
        {
            Assert.Throws<System.ArgumentException>(() => _rows.Claim(string.Empty, out _));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new PartRows(0, Linger));
        }
    }
}
