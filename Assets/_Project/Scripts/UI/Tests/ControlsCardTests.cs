using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The first controls card: shown at the full opening's chase frame, gone once the player drove and looked or
    /// after a while, once per new game, never on a loaded save.
    /// </summary>
    public sealed class ControlsCardTests
    {
        private const float Frame = 1f / 60f;

        private ControlsCardSettings _settings;
        private ControlsCard _card;

        [SetUp]
        public void SetUp()
        {
            _settings = new ControlsCardSettings();
            _card = new ControlsCard(_settings);
        }

        private void Run(float seconds, float speed, bool looking, bool open = true, bool guidance = true)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _card.Tick(Frame, open, speed * Frame, looking, guidance);
            }
        }

        [Test]
        public void TheFullOpening_ShowsTheCard_AtItsChaseFrame_NotBefore()
        {
            _card.OpeningBegan();
            Run(5f, 0f, false);
            Assert.IsFalse(_card.WantsShown, "not during the wake");
            _card.ChaseFrameReached();
            Assert.IsTrue(_card.WantsShown, "at the hand-over");
        }

        [Test]
        public void TheCard_StaysUntilThePlayerDroveAFewMetres_AndLookedAround()
        {
            _card.OpeningBegan();
            _card.ChaseFrameReached();
            Run(_settings.DriveMetres / 2f + 1f, 2f, false);
            Assert.IsTrue(_card.WantsShown, "driving alone leaves the look still to learn");
            _card.OpeningBegan();
            _card.ChaseFrameReached();
            Run(_settings.LookSeconds + 1f, 0f, true);
            Assert.IsTrue(_card.WantsShown, "looking alone leaves the drive still to learn");
            Run(_settings.DriveMetres / 2f + 1f, 2f, false);
            Assert.IsFalse(_card.WantsShown, "both done: the card has done its job");
        }

        [Test]
        public void TheCard_GoesAfterItsLongestStay_CountedOnlyWhileItCanBeSeen()
        {
            _card.OpeningBegan();
            _card.ChaseFrameReached();
            Run(_settings.MaxSeconds + 5f, 0f, false, false);
            Assert.IsTrue(_card.WantsShown, "the time behind 07's face does not count");
            Run(_settings.MaxSeconds - 1f, 0f, false);
            Assert.IsTrue(_card.WantsShown);
            Run(1f + 2f * Frame, 0f, false);
            Assert.IsFalse(_card.WantsShown, "an untouched card leaves after its longest stay");
        }

        [Test]
        public void ALoadedSave_NeverShowsTheCard()
        {
            _card.ChaseFrameReached();
            Run(1f, 0f, false);
            Assert.IsFalse(_card.WantsShown, "the short wake's chase frame alone does not arm it");
        }

        [Test]
        public void OnceGone_TheCardNeverComesBack_InThatGame()
        {
            _card.OpeningBegan();
            _card.ChaseFrameReached();
            Run(_settings.MaxSeconds + 1f, 0f, false);
            Assert.IsFalse(_card.WantsShown);
            _card.ChaseFrameReached();
            Run(1f, 0f, false);
            Assert.IsFalse(_card.WantsShown, "a later chase frame (a reload's short wake) does not bring it back");
        }

        [Test]
        public void WithGuidanceOff_TheCardNeverShows()
        {
            _card.OpeningBegan();
            _card.ChaseFrameReached();
            Run(Frame, 0f, false, true, false);
            Assert.IsFalse(_card.WantsShown);
        }
    }
}
