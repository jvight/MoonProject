using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    /// <summary>The blueprint card makes way for the next step's line: a short rest, shorter still once 07 moves on.</summary>
    public sealed class UnlockCardTests
    {
        private UnlockCardSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new UnlockCardSettings();
        }

        [Test]
        public void WhileSevenStaysPut_TheCardRestsItsFullHold()
        {
            Assert.IsFalse(UnlockCard.Ends(_settings.HoldSeconds - 0.1f, false, _settings));
            Assert.IsTrue(UnlockCard.Ends(_settings.HoldSeconds, false, _settings));
            Assert.LessOrEqual(_settings.HoldSeconds, 4f, "the next step's line follows within seconds");
        }

        [Test]
        public void DrivingOffOrOpeningBuildMode_SendsTheCardAway_AfterItsShortRead()
        {
            Assert.IsFalse(UnlockCard.Ends(_settings.MinReadSeconds - 0.1f, true, _settings), "it is read first");
            Assert.IsTrue(UnlockCard.Ends(_settings.MinReadSeconds, true, _settings));
            Assert.Less(_settings.MinReadSeconds, _settings.HoldSeconds);
        }
    }
}
