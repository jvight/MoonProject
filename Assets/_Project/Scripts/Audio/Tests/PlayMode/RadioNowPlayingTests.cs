using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>"Now playing" on the ticker (short tracks so the show moves on within seconds).</summary>
    public sealed class RadioNowPlayingTests
    {
        private readonly List<TickerLine> _lines = new List<TickerLine>();
        private AudioTestRig _rig;
        private IDisposable _subscription;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _rig = new AudioTestRig(AudioTestRig.ShortTrackSeconds);
            yield return null;
            _lines.Clear();
            _subscription = _rig.Events.Subscribe<TickerLine>(line => _lines.Add(line));
        }

        [TearDown]
        public void TearDown()
        {
            _subscription.Dispose();
            _rig.Dispose();
        }

        [UnityTest]
        public IEnumerator EachShowTrack_IsAnnouncedOncePerSession_TapesUnderTheirCassetteTitle_NeverOnTheTapeDeck()
        {
            _rig.Wake(true);
            yield return new WaitForSecondsRealtime(3f * AudioTestRig.ShortTrackSeconds + 1f);
            Assert.AreEqual(2, _lines.Count, "both base tracks announced once, however often they come round");
            var arguments = new HashSet<string>();
            foreach (TickerLine line in _lines)
            {
                Assert.AreEqual(NowPlayingLog.TickerKey, line.Key);
                Assert.IsTrue(line.ArgumentIsKey);
                arguments.Add(line.Argument);
            }

            CollectionAssert.AreEquivalent(new[] { "track.t1.title", "track.t2.title" }, arguments);

            _rig.Program.Own(AudioTestRig.TapeA);
            _rig.ProgramChanged();
            float deadline = Time.realtimeSinceStartup + 4f * AudioTestRig.ShortTrackSeconds + 2f;
            while (_lines.Count < 3 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(3, _lines.Count, "the new tape comes round within a round of the shuffle");
            Assert.AreEqual("cassette.after_dark_1.title", _lines[2].Argument);

            _rig.Program.Own(AudioTestRig.TapeB);
            _rig.Program.DialUnlocked = true;
            _rig.Program.SelectedTape = AudioTestRig.TapeB;
            _rig.Program.Channel = RadioChannel.TapeDeck;
            _rig.ProgramChanged();
            yield return new WaitForSecondsRealtime(2f * AudioTestRig.ShortTrackSeconds);
            Assert.AreEqual(AudioTestRig.TapeB, _rig.Radio.CurrentTrack.TapeId);
            Assert.AreEqual(3, _lines.Count, "the Tape Deck is the player's own choice: no ticker");
        }
    }
}
