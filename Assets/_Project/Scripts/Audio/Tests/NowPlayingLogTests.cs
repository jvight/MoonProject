using NUnit.Framework;

namespace MoonProject.Audio.Tests
{
    public sealed class NowPlayingLogTests
    {
        [Test]
        public void TitleKeys_UseTheTrackAndCassetteTables()
        {
            Assert.AreEqual("track.earthrise_static.title", NowPlayingLog.TrackTitleKey("earthrise_static"));
            Assert.AreEqual("cassette.slow_orbit.title", NowPlayingLog.CassetteTitleKey("slow_orbit"));
            Assert.AreEqual("ticker.radio.now_playing", NowPlayingLog.TickerKey);
        }

        [Test]
        public void EachTrack_IsAnnouncedOncePerSession()
        {
            var log = new NowPlayingLog();
            log.Register("earthrise_static", NowPlayingLog.TrackTitleKey("earthrise_static"));
            log.Register("slow_orbit", NowPlayingLog.CassetteTitleKey("slow_orbit"));

            Assert.IsTrue(log.TryAnnounce("earthrise_static", out string key));
            Assert.AreEqual("track.earthrise_static.title", key);
            Assert.IsTrue(log.TryAnnounce("slow_orbit", out key));
            Assert.AreEqual("cassette.slow_orbit.title", key);

            Assert.IsFalse(log.TryAnnounce("earthrise_static", out key), "second time round: no ticker");
            Assert.IsNull(key);
            Assert.AreEqual("track.earthrise_static.title", log.TitleKey("earthrise_static"));
        }

        [Test]
        public void UnknownTracks_AreNeverAnnounced_AndRegisteringTwiceKeepsTheFirstKey()
        {
            var log = new NowPlayingLog();
            Assert.IsFalse(log.TryAnnounce("ghost", out _));
            Assert.IsFalse(log.TryAnnounce(null, out _));
            Assert.IsNull(log.TitleKey("ghost"));

            log.Register("a", "track.a.title");
            log.Register("a", "cassette.a.title");
            Assert.AreEqual("track.a.title", log.TitleKey("a"));
        }
    }
}
