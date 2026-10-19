using NUnit.Framework;
using MoonProject.Core;

namespace MoonProject.Rover.Tests
{
    /// <summary>The long-sleep coat: full from the black, shed part by part with each check, gone after the start.</summary>
    public sealed class PostDustTests
    {
        private PostSettings _settings;
        private PostDust _dust;

        [SetUp]
        public void SetUp()
        {
            _settings = PostSequencerTests.Full();
            _dust = new PostDust(_settings);
        }

        [Test]
        public void APartWhoseCheckIsLeftOut_KeepsItsCoatUntilTheShiver_ThenIsClean()
        {
            var opening = new PostSettings();
            Assert.IsFalse(opening.Runs(BootCheckStep.Wheels));
            var dust = new PostDust(opening);
            dust.Update(OpeningPhase.Post, BootCheckStep.Cutter, 0.5f);
            Assert.AreEqual(1f, dust.Level(DustZone.Wheels), 1e-5f, "a later check does not strip it");
            dust.Update(OpeningPhase.PullBack, BootCheckStep.Begin, 1f);
            Assert.AreEqual(1f, dust.Level(DustZone.Wheels), 1e-5f, "still on as the camera pulls back");
            dust.Update(OpeningPhase.Perform, BootCheckStep.Begin, 0.8f);
            Assert.AreEqual(0f, dust.Level(DustZone.Wheels), 1e-5f, "the shiver, seen wide, shakes it all off");
        }

        [Test]
        public void TheCoat_IsFullFromTheBlackUntilTheChecks_AndOffInTheShortWake()
        {
            foreach (OpeningPhase phase in new[] { OpeningPhase.FadeOut, OpeningPhase.Dark })
            {
                _dust.Update(phase, BootCheckStep.Lens, 0f);
                for (int i = 0; i < PostDust.ZoneCount; i++)
                {
                    Assert.AreEqual(1f, _dust.Level((DustZone)i), 1e-5f, $"{phase}: zone {i}");
                }
            }

            _dust.Update(OpeningPhase.ShortWake, BootCheckStep.Lens, 0f);
            Assert.AreEqual(0f, _dust.Level(DustZone.Head), 1e-5f, "Continue's wake has no coat");
        }

        [Test]
        public void EachPart_ShedsItsCoatDuringItsOwnCheck_AndOnlyThen()
        {
            _dust.Update(OpeningPhase.Post, BootCheckStep.Lamps, 0.5f);
            Assert.AreEqual(1f, _dust.Level(DustZone.Head), 1e-5f, "not before its check");
            PostDustSegment head = _settings.DustSegmentOf(DustZone.Head);
            _dust.Update(OpeningPhase.Post, BootCheckStep.Head, head.End);
            Assert.AreEqual(head.Rest, _dust.Level(DustZone.Head), 1e-4f, "most is gone when its check has moved it");
            Assert.AreEqual(1f, _dust.Level(DustZone.Wing), 1e-5f, "the wing is still coated: its check is later");
            _dust.Update(OpeningPhase.Post, BootCheckStep.Wheels, 0.5f);
            Assert.AreEqual(head.Rest, _dust.Level(DustZone.Head), 1e-4f, "what is left stays until the start");
        }

        [Test]
        public void TheReadyShiver_ShakesOffTheRest_AndTheChaseFrameIsClean()
        {
            float rest = _settings.DustSegmentOf(DustZone.Body).Rest;
            _dust.Update(OpeningPhase.Post, BootCheckStep.Begin, 0.9f);
            Assert.AreEqual(rest, _dust.Level(DustZone.Body), 1e-5f, "the start line leaves the rest on");
            _dust.Update(OpeningPhase.PullBack, BootCheckStep.Begin, 1f);
            Assert.AreEqual(rest, _dust.Level(DustZone.Body), 1e-5f, "still on as the camera pulls back");
            _dust.Update(OpeningPhase.Perform, BootCheckStep.Begin, 0.3f);
            Assert.AreEqual(rest, _dust.Level(DustZone.Body), 1e-5f, "before the shiver the rest is on");
            _dust.Update(OpeningPhase.Perform, BootCheckStep.Begin, 0.8f);
            Assert.AreEqual(0f, _dust.Level(DustZone.Body), 1e-5f, "the shiver, seen wide, shakes it off");
            _dust.Update(OpeningPhase.MoveBehind, BootCheckStep.Begin, 1f);
            for (int i = 0; i < PostDust.ZoneCount; i++)
            {
                Assert.AreEqual(0f, _dust.Level((DustZone)i), 1e-5f);
            }
        }

        [Test]
        public void ThePuffs_CountWhatFell_ANewCoatOnlyLaysDown()
        {
            _dust.Update(OpeningPhase.Dark, BootCheckStep.Lens, 0f);
            Assert.IsFalse(_dust.TryTakePuff(out _), "laying the coat down drops nothing");
            int puffs = 0;
            for (float p = 0f; p <= 1f; p += 0.01f)
            {
                _dust.Update(OpeningPhase.Post, BootCheckStep.Head, p);
                while (_dust.TryTakePuff(out DustZone zone))
                {
                    Assert.AreEqual(DustZone.Head, zone, "the puff drops from the part that is shedding");
                    puffs++;
                }
            }

            float fallen = 1f - _settings.DustSegmentOf(DustZone.Head).Rest;
            Assert.AreEqual(fallen / _settings.DustPuffStep, puffs, 1f);
        }
    }
}
