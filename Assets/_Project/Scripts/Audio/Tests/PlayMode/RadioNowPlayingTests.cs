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
    /// <summary>"Now playing" on the ticker (short tracks so each music moment ends within seconds).</summary>
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
        public IEnumerator EachTrack_IsAnnouncedOncePerSession()
        {
            _rig.Wake(true);
            for (int i = 0; i < 3; i++)
            {
                _rig.RequestMusic();
                yield return new WaitForSecondsRealtime(AudioTestRig.ShortTrackSeconds + 3f);
            }

            Assert.AreEqual(2, _lines.Count, "both tracks announced once, however often they come round");
            var arguments = new HashSet<string>();
            foreach (TickerLine line in _lines)
            {
                Assert.AreEqual(NowPlayingLog.TickerKey, line.Key);
                Assert.IsTrue(line.ArgumentIsKey);
                arguments.Add(line.Argument);
            }

            CollectionAssert.AreEquivalent(new[] { "track.t1.title", "track.t2.title" }, arguments);
        }
    }
}
