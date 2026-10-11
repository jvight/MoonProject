using NUnit.Framework;

namespace MoonProject.Audio.Tests
{
    /// <summary>
    /// The save's station under the title (docs/features/M3-09): soft, then full on Continue, silent on New Game.
    /// </summary>
    public sealed class TitleRadioTests
    {
        private const float Frame = 1f / 60f;
        private const float Soft = 0.35f;
        private const float Rise = 0.8f;
        private const float Fall = 0.5f;

        [Test]
        public void OutsideTheTitle_TheLevelStaysFull()
        {
            var title = new TitleRadio();
            title.StartSession();
            Run(title, 2f);
            Assert.AreEqual(1f, title.Level);
            title.PutAway();
            Run(title, 2f);
            Assert.AreEqual(1f, title.Level, "New Game without the title's save changes nothing here");
        }

        [Test]
        public void OverASave_TheStationEasesInSoftly_AndNeverPassesItsSoftLevel()
        {
            var title = new TitleRadio();
            title.ShowOverSave(Soft);
            Assert.AreEqual(0f, title.Level, "it comes up from silence");
            float last = 0f;
            for (float t = 0f; t < 6f; t += Frame)
            {
                float level = title.Step(Frame, Rise, Fall);
                Assert.GreaterOrEqual(level, last, "a soft rise, never a dip");
                Assert.LessOrEqual(level, Soft + 1e-5f, "soft under the menu");
                last = level;
            }

            Assert.AreEqual(Soft, title.Level, 0.01f);
            Assert.IsTrue(title.HoldsSave);
        }

        [Test]
        public void Continue_BringsTheStationUpToFull_WithinTheCameraBlend()
        {
            var title = new TitleRadio();
            title.ShowOverSave(Soft);
            Run(title, 6f);
            title.StartSession();
            Assert.IsFalse(title.HoldsSave);
            Run(title, Rise);
            Assert.Greater(title.Level, Soft, "it rises as the camera eases out of the title frame");
            Assert.Less(title.Level, 1f, "eased, not switched");
            Run(title, 3f * Rise);
            Assert.AreEqual(1f, title.Level, 0.05f, "full by the end of the ~2.5 s blend");
        }

        [Test]
        public void NewGame_OverTheSave_LetsTheStationFallSilent()
        {
            var title = new TitleRadio();
            title.ShowOverSave(Soft);
            Run(title, 6f);
            title.PutAway();
            Run(title, 5f * Fall);
            Assert.AreEqual(0f, title.Level, 0.01f);
        }

        private static void Run(TitleRadio title, float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                title.Step(Frame, Rise, Fall);
            }
        }
    }
}
