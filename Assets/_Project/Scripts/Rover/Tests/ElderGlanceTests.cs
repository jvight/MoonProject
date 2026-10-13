using NUnit.Framework;
using UnityEngine;
using MoonProject.Rover;

namespace MoonProject.Rover.Tests
{
    /// <summary>07 notices its elder "06" once, the first time it comes near, and a saved meeting is never replayed.
    /// </summary>
    public sealed class ElderGlanceTests
    {
        private readonly ElderGlanceSettings _settings = new ElderGlanceSettings();

        [Test]
        public void ItNotices06_OnceTheFirstTimeItComesNear()
        {
            var glance = new ElderGlance(_settings);
            Vector3 elder = new Vector3(100f, 3f, 50f);
            Assert.IsFalse(glance.Step(elder + Vector3.right * (_settings.NoticeRadius + 1f), elder), "Still too far.");
            Assert.IsTrue(glance.Step(elder + Vector3.right * (_settings.NoticeRadius - 1f) + Vector3.up * 5f, elder),
                "Near (flat distance): it looks.");
            Assert.IsFalse(glance.Step(elder, elder), "Only once.");
            Assert.IsTrue(glance.Greeted);
        }

        [Test]
        public void ASavedMeeting_IsNeverReplayed()
        {
            var glance = new ElderGlance(_settings);
            glance.Restore(true);
            Assert.IsFalse(glance.Step(Vector3.zero, Vector3.zero));
        }
    }
}
