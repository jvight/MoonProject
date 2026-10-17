using NUnit.Framework;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI.Tests
{
    /// <summary>Every ability has an unlock card row, and a fitted kit says which ability it gave.</summary>
    public sealed class AbilityTableTests
    {
        [Test]
        public void EveryAbility_HasAnUnlockCardRow()
        {
            foreach (RoverAbility ability in System.Enum.GetValues(typeof(RoverAbility)))
            {
                Assert.IsTrue(AbilityTable.TryFind(ability, out _), ability + " has no unlock card row");
            }
        }

        [Test]
        public void TheKeys_AreNamedFromTheAbility()
        {
            Assert.IsTrue(AbilityTable.TryFind(RoverAbility.HoverJump, out AbilityRow row));
            Assert.AreEqual("ui.can.hover_jump.verb", row.VerbKey);
            Assert.IsTrue(row.HasAction, "Hover-Jump has its own key");
            Assert.IsTrue(AbilityTable.TryFind(RoverAbility.WideSonar, out row));
            Assert.IsFalse(row.HasAction, "the sonar just works");
        }

        [Test]
        public void AFittedPiece_NamesTheAbilityItGave()
        {
            Assert.IsTrue(AbilityTable.TryAbilityOf(RoverKitPiece.TetherRing, out RoverAbility ability));
            Assert.AreEqual(RoverAbility.Tether, ability);
            Assert.IsTrue(AbilityTable.TryAbilityOf(RoverKitPiece.HoverCoils, out ability));
            Assert.AreEqual(RoverAbility.HoverJump, ability);
            Assert.IsFalse(AbilityTable.TryAbilityOf(RoverKitPiece.FreshPaint, out _), "paint gives nothing to do");
        }
    }
}
