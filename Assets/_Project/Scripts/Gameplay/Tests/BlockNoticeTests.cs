using NUnit.Framework;

namespace MoonProject.Gameplay.Tests
{
    public sealed class BlockNoticeTests
    {
        [Test]
        public void AHold_ReportsItsFirstBlockedFrame_Once()
        {
            var notice = new BlockNotice();
            Assert.IsFalse(notice.Report(true, false), "held but free: nothing to say");
            Assert.IsTrue(notice.Report(true, true), "blocked: said once");
            Assert.IsFalse(notice.Report(true, true), "still blocked in the same hold: quiet");
            Assert.IsFalse(notice.Report(true, false));
            Assert.IsFalse(notice.Report(true, true), "blocked again in the same hold: still quiet");
        }

        [Test]
        public void LettingGo_ArmsItAgain()
        {
            var notice = new BlockNotice();
            Assert.IsTrue(notice.Report(true, true));
            Assert.IsFalse(notice.Report(false, true), "not held: never blocked");
            Assert.IsTrue(notice.Report(true, true), "a new hold says it again");
        }
    }
}
