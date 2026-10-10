using System.IO;
using NUnit.Framework;
using MoonProject.Core;

namespace MoonProject.App.Tests
{
    public sealed class GameFlowTests
    {
        private string _directory;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "MoonProjectBootIntent", TestContext.CurrentContext.Test.ID);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Test]
        public void WithASave_TheTitleOffersContinue_AndContinueStartsPlayOnce()
        {
            var flow = new GameFlow();
            flow.Loaded(true, false, true);
            Assert.AreEqual(GameFlowState.Title, flow.State);
            Assert.IsTrue(flow.HasSave);
            Assert.IsFalse(flow.TryStartHere(), "a new game over a save puts it away and reloads");
            Assert.IsFalse(flow.CanQuitToTitle);
            Assert.IsTrue(flow.TryContinue());
            Assert.AreEqual(GameFlowState.Playing, flow.State);
            Assert.IsFalse(flow.TryContinue(), "play is already on");
            Assert.IsTrue(flow.CanQuitToTitle);
        }

        [Test]
        public void WithoutASave_NewGameStartsRightHere_AndThereIsNothingToContinue()
        {
            var flow = new GameFlow();
            flow.Loaded(false, false, true);
            Assert.AreEqual(GameFlowState.Title, flow.State);
            Assert.IsFalse(flow.TryContinue());
            Assert.IsTrue(flow.TryStartHere());
            Assert.AreEqual(GameFlowState.Playing, flow.State);
        }

        [Test]
        public void ABootArmedByNewGame_SkipsTheTitle_WithNoSaveToContinue()
        {
            var flow = new GameFlow();
            flow.Loaded(true, true, true);
            Assert.AreEqual(GameFlowState.Playing, flow.State);
            Assert.IsFalse(flow.HasSave);
        }

        [Test]
        public void WithTheTitleOff_PlayBeginsOnTheSave()
        {
            var flow = new GameFlow();
            flow.Loaded(true, false, false);
            Assert.AreEqual(GameFlowState.Playing, flow.State);
            Assert.IsTrue(flow.HasSave);
            Assert.IsFalse(flow.ShowsTitle);
            Assert.IsFalse(flow.CanQuitToTitle, "no title to return to");
            Assert.IsFalse(flow.CanStartOver, "a new game is chosen on the title");
        }

        [Test]
        public void ABootIntent_IsReadOnce()
        {
            var intent = new BootIntent(_directory, "main");
            Assert.IsFalse(intent.TryConsume(), "nothing armed yet");
            intent.Arm();
            Assert.IsTrue(File.Exists(intent.FilePath));
            Assert.IsTrue(intent.TryConsume());
            Assert.IsFalse(intent.TryConsume(), "one boot only");
        }
    }
}
