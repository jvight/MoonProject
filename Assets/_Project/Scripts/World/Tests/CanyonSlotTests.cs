using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The canyon's shadowed slot beside relay.2 (base-home-and-crater-exits §4): drivable from the basin floor all the
    /// way into its bay, and past its turn in permanent shadow, which no low polar Sun and no earthlight reaches.
    /// </summary>
    public sealed class CanyonSlotTests
    {
        // The rover's steepest drivable slope along a floor, and the step the slot's floor is walked at.
        private const float MaxDrivableSlope = 20f;
        private const float WalkStep = 1f;

        // Points on the dark leg, from this far past the turn's end to the bay; and the eye on the floor they stand for.
        private const float DarkFromTurnEnd = 6f;
        private const float DarkStep = 4f;
        private const float FloorEye = 0.3f;

        // Light from every bearing at this step, its rays marched this far at this step; the Sun's disc adds its
        // angular radius on top of the highest elevation it climbs to.
        private const float BearingStep = 5f;
        private const float RayReach = 600f;
        private const float RayStep = 1f;
        private const float SunDiscRadius = 0.27f;

        private MoonSurface _surface;
        private Canyon _canyon;

        [SetUp]
        public void SetUp()
        {
            _surface = new MoonSurface(new SurfaceSettings(), WorldSettings.DefaultSeed);
            _canyon = _surface.Canyon;
        }

        [Test]
        public void TheSlot_IsDrivableFromTheBasinFloor_IntoItsBay()
        {
            CanyonPath slot = _canyon.SlotPath;
            Vector2 previous = slot.PointAt(-WalkStep);
            for (float arc = 0f; arc <= slot.EndArc; arc += WalkStep)
            {
                Vector2 p = slot.PointAt(arc);
                Assert.IsTrue(_surface.IsDrivable(p.x, p.y), $"the slot's floor is drivable {arc:F0} m in");
                float rise = _surface.SampleHeight(p.x, p.y) - _surface.SampleHeight(previous.x, previous.y);
                Assert.Less(Mathf.Atan2(Mathf.Abs(rise), WalkStep) * Mathf.Rad2Deg, MaxDrivableSlope,
                    $"a gentle floor {arc:F0} m in");
                previous = p;
            }

            Assert.IsTrue(_canyon.TryFloor(_canyon.SlotBayCentre.x, _canyon.SlotBayCentre.y,
                out CanyonCorridor corridor, out float _, out float _), "the bay is the slot's floor");
            Assert.AreEqual(CanyonCorridor.Slot, corridor);
        }

        [Test]
        public void PastItsTurn_NoSunlightEverReachesTheSlotsFloor()
        {
            float highest = new SkyCycleSettings().MaxElevation + SunDiscRadius;
            for (float bearing = 0f; bearing < 360f; bearing += BearingStep)
            {
                Vector2 flat = MoonSurface.BearingToDirection(bearing);
                float rad = highest * Mathf.Deg2Rad;
                var toSun = new Vector3(flat.x * Mathf.Cos(rad), Mathf.Sin(rad), flat.y * Mathf.Cos(rad));
                AssertDark(toSun, $"the Sun at {bearing} deg, {highest:F1} deg up");
            }
        }

        [Test]
        public void PastItsTurn_TheEarthlightNeverReachesTheSlotsFloor()
        {
            AssertDark(new SkySettings().EarthDirection.normalized, "the earthlight");
        }

        // Every point on the dark leg sees the light blocked: a lower light from the same bearing is blocked too, as
        // its ray runs everywhere below this one.
        private void AssertDark(Vector3 toLight, string light)
        {
            CanyonPath slot = _canyon.SlotPath;
            for (float arc = _canyon.SlotTurnEndArc + DarkFromTurnEnd; arc <= slot.EndArc; arc += DarkStep)
            {
                Vector2 p = slot.PointAt(arc);
                var floor = new Vector3(p.x, _surface.SampleHeight(p.x, p.y) + FloorEye, p.y);
                Assert.IsTrue(Blocked(floor, toLight), $"{light} reaches the slot's floor {arc:F0} m in at {p}");
            }
        }

        private bool Blocked(Vector3 from, Vector3 toLight)
        {
            float flat = new Vector2(toLight.x, toLight.z).magnitude;
            if (flat < 1e-4f)
            {
                return false;
            }

            float climb = toLight.y / flat;
            var step = new Vector2(toLight.x, toLight.z) / flat;
            for (float d = RayStep; d <= RayReach; d += RayStep)
            {
                Vector2 p = new Vector2(from.x, from.z) + step * d;
                if (_surface.SampleHeight(p.x, p.y) > from.y + climb * d)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
