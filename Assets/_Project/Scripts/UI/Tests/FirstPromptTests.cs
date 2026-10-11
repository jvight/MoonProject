using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The opening's first prompt (cutscenes O14): after the chase frame and a short rest, "Look up" if the player has
    /// not looked yet, otherwise "Drive"; once per save, gone as soon as its verb is done.
    /// </summary>
    public sealed class FirstPromptTests
    {
        private const float Frame = 1f / 60f;

        private StargazeSettings _settings;
        private PromptLedger _ledger;
        private FirstPrompt _prompt;

        [SetUp]
        public void Build()
        {
            _settings = new StargazeSettings();
            _ledger = new PromptLedger(new PromptSettings());
            _prompt = new FirstPrompt(_settings, _ledger);
        }

        [Test]
        public void Untouched_ItAsksToLookUp_OnlyAfterTheChaseFrameAndTheRest()
        {
            _prompt.OpeningBegan();
            Run(10f, false, false);
            Assert.AreEqual(HintWord.None, _prompt.Word, "nothing before the chase frame");
            _prompt.ChaseFrameReached();
            Run(_settings.FirstPromptRestSeconds - 0.1f, false, false);
            Assert.AreEqual(HintWord.None, _prompt.Word, "07 rests a moment first");
            Run(0.2f, false, false);
            Assert.AreEqual(HintWord.LookUp, _prompt.Word);
            Assert.IsTrue(_ledger.LookTaught, "remembered in the save");
        }

        [Test]
        public void LookingDuringTheOpening_MakesTheFirstPromptDrive()
        {
            _prompt.OpeningBegan();
            Run(4f, true, false);
            _prompt.ChaseFrameReached();
            Run(_settings.FirstPromptRestSeconds + 0.1f, false, false);
            Assert.AreEqual(HintWord.Drive, _prompt.Word);
        }

        [Test]
        public void DoingTheVerb_SendsThePromptAway_AndTheNextWordWaitsForAnotherRest()
        {
            _prompt.OpeningBegan();
            _prompt.ChaseFrameReached();
            Run(_settings.FirstPromptRestSeconds + 0.1f, false, false);
            Assert.AreEqual(HintWord.LookUp, _prompt.Word);
            Run(Frame, true, false);
            Assert.AreEqual(HintWord.None, _prompt.Word, "looking up sends it");
            Run(_settings.FirstPromptRestSeconds - 0.1f, false, false);
            Assert.AreEqual(HintWord.None, _prompt.Word);
            Run(0.2f, false, false);
            Assert.AreEqual(HintWord.Drive, _prompt.Word, "then the drive, after another rest");
            Run(Frame, false, true);
            Assert.AreEqual(HintWord.None, _prompt.Word, "driving sends it");
            Run(30f, false, false);
            Assert.AreEqual(HintWord.None, _prompt.Word, "both verbs found: nothing more to teach");
        }

        [Test]
        public void APromptNobodyActsOn_LeavesAfterItsHold_AndNeverReturnsInThisSave()
        {
            _prompt.OpeningBegan();
            _prompt.ChaseFrameReached();
            Run(_settings.FirstPromptRestSeconds + 0.1f, false, false);
            Run(_settings.FirstPromptHoldSeconds + 0.1f, false, false);
            Assert.AreNotEqual(HintWord.LookUp, _prompt.Word, "the hold ran out");

            var again = new FirstPrompt(_settings, _ledger);
            again.OpeningBegan();
            again.ChaseFrameReached();
            for (float t = 0f; t < 30f; t += Frame)
            {
                again.Tick(Frame, true, false, true);
                Assert.AreNotEqual(HintWord.LookUp, again.Word, "Look up is taught once per save");
            }
        }

        [Test]
        public void ItWaitsWhileTheScreenIsBusy_AndNeverShowsWithoutTheOpening()
        {
            _prompt.ChaseFrameReached();
            Run(10f, false, false);
            Assert.AreEqual(HintWord.None, _prompt.Word, "the short wake's chase frame alone teaches nothing");

            _prompt.OpeningBegan();
            _prompt.ChaseFrameReached();
            for (float t = 0f; t < 10f; t += Frame)
            {
                _prompt.Tick(Frame, false, false, false);
            }

            Assert.AreEqual(HintWord.None, _prompt.Word, "a card or panel holds the screen");
            Assert.IsFalse(_ledger.LookTaught, "a prompt nobody saw is not spent");
            Run(Frame, false, false);
            Assert.AreEqual(HintWord.LookUp, _prompt.Word, "it shows once the screen is free");
        }

        private void Run(float seconds, bool looking, bool driving)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _prompt.Tick(Frame, true, looking, driving);
            }
        }
    }
}
