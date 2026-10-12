using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    /// <summary>The reward card is read from the upgrade's data: only what a level changed makes a line.</summary>
    public sealed class RewardLinesTests
    {
        private readonly RewardLine[] _lines = new RewardLine[RewardLines.Max];

        [Test]
        public void TheFirstLevel_GivesPower_TheRadio_AndTheReach()
        {
            int count = RewardLines.Compose(0f, 150f, 1f, 1.6f, _lines);
            Assert.AreEqual(3, count);
            Assert.AreEqual(RewardLineKind.LightOn, _lines[0].Kind);
            Assert.AreEqual(RewardLineKind.RadioOn, _lines[1].Kind);
            Assert.AreEqual(RewardLineKind.Reach, _lines[2].Kind);
            Assert.AreEqual(150, _lines[2].Number);
        }

        [Test]
        public void ALaterLevel_BrightensHome_AndExtendsTheReachByTheDifference()
        {
            int count = RewardLines.Compose(150f, 400f, 1.6f, 2.2f, _lines);
            Assert.AreEqual(2, count);
            Assert.AreEqual(RewardLineKind.LightUp, _lines[0].Kind);
            Assert.AreEqual(RewardLineKind.Reach, _lines[1].Kind);
            Assert.AreEqual(250, _lines[1].Number);
        }

        [Test]
        public void ALevelThatChangesNothingTheTowerGives_HasNoLines()
        {
            Assert.AreEqual(0, RewardLines.Compose(150f, 150f, 1.6f, 1.6f, _lines));
        }

        [Test]
        public void EveryLineKind_HasAKey()
        {
            foreach (RewardLineKind kind in System.Enum.GetValues(typeof(RewardLineKind)))
            {
                StringAssert.StartsWith("ui.reward.", UiKeys.RewardLine(kind));
            }
        }
    }
}
