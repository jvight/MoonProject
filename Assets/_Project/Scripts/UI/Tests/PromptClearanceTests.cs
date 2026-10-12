using NUnit.Framework;
using UnityEngine;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// A prompt never draws over a card: it rests on top of the card it would sit under, or under it when there is
    /// no room above, and does not move when it is clear.
    /// </summary>
    public sealed class PromptClearanceTests
    {
        private const float Gap = 18f;
        private const float Margin = 72f;
        private static readonly Rect Screen = new Rect(0f, 0f, 1920f, 1080f);
        private static readonly Rect Card = new Rect(1100f, 400f, 500f, 300f);

        [Test]
        public void AChipAwayFromTheCard_StaysWhereItIs()
        {
            Assert.AreEqual(0f, PromptClearance.Lift(new Rect(200f, 500f, 300f, 50f), Card, Gap, Screen, Margin));
            Assert.AreEqual(0f, PromptClearance.Lift(new Rect(1200f, 100f, 300f, 50f), Card, Gap, Screen, Margin));
        }

        [Test]
        public void AChipOverTheCard_RestsOnTopOfItWithTheGap()
        {
            var chip = new Rect(1200f, 500f, 300f, 50f);
            float lift = PromptClearance.Lift(chip, Card, Gap, Screen, Margin);
            Assert.AreEqual(chip.yMax - (Card.yMin - Gap), lift, 0.001f);
            Assert.AreEqual(Card.yMin - Gap, chip.yMax - lift, 0.001f, "its foot is a gap above the card");
        }

        [Test]
        public void AChipWithNoRoomAbove_GoesUnderTheCard()
        {
            var tall = new Rect(1200f, 100f, 300f, 120f);
            var card = new Rect(1100f, 130f, 500f, 300f);
            float lift = PromptClearance.Lift(tall, card, Gap, Screen, Margin);
            Assert.Less(lift, 0f);
            Assert.AreEqual(card.yMax + Gap, tall.yMin - lift, 0.001f, "its head is a gap below the card");
        }

        [Test]
        public void AChipJustBesideTheCardWithinTheGap_StillMakesWay()
        {
            var chip = new Rect(Card.xMax + Gap * 0.5f - 300f, 500f, 300f, 50f);
            Assert.Greater(PromptClearance.Lift(chip, Card, Gap, Screen, Margin), 0f);
        }
    }
}
