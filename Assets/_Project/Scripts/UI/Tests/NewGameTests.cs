using NUnit.Framework;
using MoonProject.Core.Events;
using MoonProject.Core.Save;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// Starting over is calm and hard to do by accident: the question opens on Keep going, Esc keeps going, and a
    /// confirmed new game is asked for once. A save put away on load is mentioned once, and only then.
    /// </summary>
    public sealed class NewGameTests
    {
        [Test]
        public void TheNewGameQuestion_OpensOnKeepGoing_AndEscKeepsGoing()
        {
            var pages = new PausePages();
            pages.Reset();
            Assert.AreEqual(PauseFocus.Resume, pages.DefaultFocus);

            Assert.IsTrue(pages.Ask(PausePage.NewGame));
            Assert.AreEqual(PausePage.NewGame, pages.Current);
            Assert.AreEqual(PauseFocus.NewGameKeep, pages.DefaultFocus, "the safe answer has the focus");
            Assert.IsFalse(pages.Ask(PausePage.Quit), "one question at a time");

            Assert.IsTrue(pages.Back(out PauseFocus focus), "Esc is Keep going");
            Assert.AreEqual(PausePage.Main, pages.Current);
            Assert.AreEqual(PauseFocus.NewGame, focus, "back on the button that asked");
            Assert.IsFalse(pages.StartingOver);
            Assert.IsFalse(pages.Back(out _), "on the main buttons Esc leaves the menu instead");
        }

        [Test]
        public void StartingOver_IsConfirmedOnce_AndOnlyFromItsQuestion()
        {
            var pages = new PausePages();
            pages.Reset();
            Assert.IsFalse(pages.ConfirmNewGame(), "not from the main buttons");
            pages.Ask(PausePage.Quit);
            Assert.IsFalse(pages.ConfirmNewGame(), "not from the quit question");
            Assert.AreEqual(PauseFocus.QuitStay, pages.DefaultFocus);
            pages.Back(out _);

            pages.Ask(PausePage.NewGame);
            Assert.IsTrue(pages.ConfirmNewGame());
            Assert.IsTrue(pages.StartingOver);
            Assert.IsFalse(pages.ConfirmNewGame(), "a second press asks for nothing more");
        }

        [Test]
        public void TheOlderJourneyLine_ShowsOnce_AndOnlyWhenOneWasPutAway()
        {
            foreach (SaveLoadResult result in new[]
                     {
                         SaveLoadResult.NotLoaded, SaveLoadResult.NoSave, SaveLoadResult.Loaded,
                         SaveLoadResult.RecoveredFromBackup, SaveLoadResult.Unreadable, SaveLoadResult.NewerFormat,
                     })
            {
                Assert.IsFalse(new JourneyNotice().TryTake(result, out _), result.ToString());
            }

            var notice = new JourneyNotice();
            Assert.IsTrue(notice.TryTake(SaveLoadResult.PutAwayOlder, out TickerLine line));
            Assert.AreEqual(UiKeys.JourneyPutAway, line.Key);
            Assert.IsFalse(notice.TryTake(SaveLoadResult.PutAwayOlder, out _), "once per boot");
        }
    }
}
