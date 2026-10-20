using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The fallen array's glint (<see cref="SunGlint"/>): full on the Sun's mirror in the cells, falling away from it,
    /// nothing with the Sun or the eye behind the face, either side of a wing alike; its shimmer stays within its depth.
    /// </summary>
    public sealed class SunGlintTests
    {
        private const float Sharpness = 12f;

        [Test]
        public void OnTheSunsMirror_TheGlintIsFull_AndFallsAwayFromIt()
        {
            Vector3 normal = Vector3.up;
            Vector3 towardSun = new Vector3(0f, 0.5f, 1f).normalized;
            Vector3 mirror = Vector3.Reflect(-towardSun, normal);
            Assert.AreEqual(1f, SunGlint.Level(towardSun, normal, mirror, Sharpness), 1e-4f);

            Vector3 aside = Quaternion.AngleAxis(20f, Vector3.up) * mirror;
            float off = SunGlint.Level(towardSun, normal, aside, Sharpness);
            Assert.That(off, Is.InRange(0.01f, 0.95f), "20 degrees off the mirror it is a fainter catch of light");
        }

        [Test]
        public void WithTheSunOrTheEyeBehindTheFace_ThereIsNoGlint()
        {
            Vector3 normal = Vector3.up;
            Vector3 towardSun = new Vector3(0f, 0.5f, 1f).normalized;
            Vector3 below = new Vector3(0f, -0.5f, -1f).normalized;
            Assert.AreEqual(0f, SunGlint.Level(towardSun, normal, below, Sharpness), "the eye under the face");
            Assert.AreEqual(0f, SunGlint.Level(Vector3.down, Vector3.right, Vector3.up, Sharpness),
                "the Sun edge-on behind the face");
        }

        [Test]
        public void EitherSideOfAWing_CatchesTheLight()
        {
            Vector3 towardSun = new Vector3(0f, 0.5f, 1f).normalized;
            Vector3 mirror = Vector3.Reflect(-towardSun, Vector3.up);
            Assert.AreEqual(SunGlint.Level(towardSun, Vector3.up, mirror, Sharpness),
                SunGlint.Level(towardSun, Vector3.down, mirror, Sharpness), 1e-5f);
        }

        [Test]
        public void TheFirstSliverOfTheSun_AlreadyCatchesInFull()
        {
            Assert.AreEqual(0f, SunGlint.Catch(0f, 0.08f), "nothing in the eclipse or the shade");
            Assert.AreEqual(0.5f, SunGlint.Catch(0.04f, 0.08f), 1e-5f, "half caught at half the sliver");
            Assert.AreEqual(1f, SunGlint.Catch(0.08f, 0.08f), 1e-5f);
            Assert.AreEqual(1f, SunGlint.Catch(1f, 0.08f), "and never more in the full Sun");
        }

        [Test]
        public void TheShimmer_StaysWithinItsDepth()
        {
            for (float t = 0f; t < 10f; t += 0.05f)
            {
                Assert.That(SunGlint.Shimmer(t, 1.3f, 0.35f, 0.4f), Is.InRange(0.65f - 1e-4f, 1f + 1e-4f));
            }
        }
    }
}
