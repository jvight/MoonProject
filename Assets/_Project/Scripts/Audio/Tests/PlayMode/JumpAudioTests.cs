using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>The Hover-Jump and the workbench, published on the bus, sound as designed.</summary>
    public sealed class JumpAudioTests
    {
        private AudioTestRig _rig;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _rig = new AudioTestRig();
            yield return null;
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
        }

        private string LastClip => _rig.Director.LastClip != null ? _rig.Director.LastClip.name : string.Empty;

        private IEnumerator ChargeTo(float strength)
        {
            for (int step = 0; step <= Mathf.RoundToInt(strength * 4f); step++)
            {
                _rig.Events.Publish(new RoverJumpCharged(step / 4f));
                yield return new WaitForSeconds(0.2f);
            }
        }

        [UnityTest]
        public IEnumerator Charge_ClimbsThePentatonic_ThenTheLeapBoingsAndTwangs()
        {
            AudioSource hum = _rig.Jump.ChargeSource;
            _rig.Events.Publish(new RoverJumpCharged(0f));
            yield return new WaitForSeconds(0.25f);
            Assert.IsTrue(hum.isPlaying);
            Assert.AreEqual(1f, hum.pitch, 1e-3f, "D");

            for (int step = 1; step <= 4; step++)
            {
                _rig.Events.Publish(new RoverJumpCharged(step / 4f));
                yield return new WaitForSeconds(0.25f);
            }

            Assert.AreEqual(Mathf.Pow(2f, 9f / 12f), hum.pitch, 2e-3f, "B at full charge");

            _rig.Events.Publish(new RoverJumped(1f));
            Assert.IsTrue(PlayedRecently("jump_leap_leap"), "the big leap's boing + whoosh");
            StringAssert.StartsWith("coil_twang_", LastClip, "and the coils' twang");
            Assert.IsTrue(_rig.Jump.Leaping);
            yield return new WaitForSeconds(0.15f);
            Assert.IsFalse(hum.isPlaying, "the hum hands over to the leap");
        }

        [Test]
        public void ATap_IsATinyHop_AFullCharge_TheBigLeap()
        {
            _rig.Events.Publish(new RoverJumped(0.1f));
            Assert.IsTrue(PlayedRecently("jump_leap_hop"));
            _rig.Events.Publish(new RoverJumped(1f));
            Assert.IsTrue(PlayedRecently("jump_leap_leap"));
        }

        [UnityTest]
        public IEnumerator ACancelledCharge_FadesTheHumSoftly_WithNoLeap()
        {
            yield return ChargeTo(0.5f);
            AudioSource hum = _rig.Jump.ChargeSource;
            Assert.IsTrue(hum.isPlaying);
            int plays = _rig.Director.PlayCount;

            _rig.Events.Publish(new RoverJumpCancelled());
            Assert.IsFalse(_rig.Jump.Charge.IsCharging);
            yield return null;
            Assert.IsTrue(hum.isPlaying, "a soft fade, not a cut");
            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(hum.isPlaying);
            Assert.AreEqual(plays, _rig.Director.PlayCount, "no boing for a cancelled charge");
            Assert.IsFalse(_rig.Jump.Leaping);
        }

        [UnityTest]
        public IEnumerator Airborne_WindSwells_ThenTheLandingIsCushioned_NotThumped()
        {
            _rig.Events.Publish(new RoverJumped(1f));
            _rig.Rover.IsGrounded = false;
            _rig.Rover.Velocity = new Vector3(8f, 4f, 0f);
            float air = 0f;
            while (air < 1.5f)
            {
                air += Time.deltaTime;
                _rig.Rover.AirTime = air;
                yield return null;
            }

            AudioSource wind = _rig.Jump.WindSource;
            Assert.IsTrue(wind.isPlaying);
            float airborne = wind.volume;
            Assert.Greater(airborne, 0f);

            _rig.Rover.IsGrounded = true;
            _rig.Rover.AirTime = 0f;
            _rig.Events.Publish(new RoverLanded(new Vector3(2f, 0f, 3f), 3f, 1.5f));
            StringAssert.StartsWith("jump_land_", LastClip);
            Assert.IsFalse(_rig.VoicePlaying("landing_thump"), "the cushion replaces the hard thump");
            Assert.IsFalse(_rig.Jump.Leaping);
            yield return new WaitForSeconds(0.5f);
            Assert.Less(wind.volume, 0.1f * airborne, "quickly faded");
            yield return new WaitForSeconds(1.3f);
            Assert.IsFalse(wind.isPlaying, "the wind resolves to silence on touchdown and stops");
        }

        [Test]
        public void AnOrdinaryLanding_StillThumps()
        {
            _rig.Events.Publish(new RoverLanded(Vector3.zero, 3f, 1f));
            StringAssert.StartsWith("landing_thump", LastClip);
        }

        [Test]
        public void WorkbenchPurchase_SparksRattlesResolves_AndTheCoilsPopIn()
        {
            _rig.Events.Publish(new UpgradePurchased("rover.hover_jump", 1));
            Assert.AreEqual("coil_pop", LastClip);
            Assert.IsTrue(PlayedRecently("workbench_upgrade"));
            Assert.IsFalse(PlayedRecently("upgrade_arpeggio"), "distinct from the tower");
        }

        [Test]
        public void TowerPurchase_KeepsItsArpeggio()
        {
            _rig.Events.Publish(new UpgradePurchased("radio_tower", 2));
            Assert.AreEqual("upgrade_arpeggio", LastClip);
            Assert.IsFalse(PlayedRecently("workbench_upgrade"));
        }

        private bool PlayedRecently(string clipName)
        {
            for (int ago = 0; ago < 4; ago++)
            {
                AudioClip clip = _rig.Director.RecentClip(ago);
                if (clip != null && clip.name == clipName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
