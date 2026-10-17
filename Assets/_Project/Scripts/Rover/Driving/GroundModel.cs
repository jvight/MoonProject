using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Ground-contact forces for the physics sphere, expressed as accelerations so they are independent of mass.
    /// </summary>
    public static class GroundModel
    {
        /// <summary>1 on gentle slopes, easing to 0 between the full and zero assist angles.</summary>
        public static float SlopeAssist(GroundSettings settings, float slopeAngle)
        {
            return 1f - Smoothing.SmoothStep(settings.SlopeAssistFullAngle, settings.SlopeAssistZeroAngle, slopeAngle);
        }

        /// <summary>
        /// Acceleration that cancels the along-slope pull of gravity (scaled by slope assist) and then gives back a
        /// fraction of it along the heading while rolling, so hills keep some character but a parked rover stays put.
        /// </summary>
        public static Vector3 SlopeAcceleration(GroundSettings settings, Vector3 gravity, Vector3 normal,
            Vector3 forward, float forwardSpeed)
        {
            Vector3 tangential = gravity - normal * Vector3.Dot(gravity, normal);
            float assist = SlopeAssist(settings, Vector3.Angle(normal, Vector3.up));
            float rolling = Smoothing.SmoothStep(0f, settings.SlopeRollSpeed, Mathf.Abs(forwardSpeed));
            Vector3 felt = forward * (Vector3.Dot(tangential, forward) * settings.SlopeInfluence * rolling);
            return (felt - tangential) * assist;
        }

        /// <summary>
        /// Velocity change that swings the in-plane velocity toward the heading (forward or backward, whichever is
        /// closer) without changing its magnitude: turns keep their speed, and a sideways landing straightens out.
        /// </summary>
        public static Vector3 GripVelocityChange(Vector3 velocity, Vector3 normal, Vector3 forward, float gripRate,
            float deltaTime)
        {
            Vector3 planar = velocity - normal * Vector3.Dot(velocity, normal);
            float speed = planar.magnitude;
            if (speed < 1e-4f)
            {
                return Vector3.zero;
            }

            Vector3 heading = Vector3.Dot(planar, forward) >= 0f ? forward : -forward;
            float angle = Vector3.Angle(planar, heading) * Mathf.Deg2Rad;
            float turn = angle * (1f - Mathf.Exp(-gripRate * deltaTime));
            Vector3 aligned = Vector3.RotateTowards(planar, heading * speed, turn, 0f);
            return aligned - planar;
        }

        /// <summary>Extra downward acceleration (m/s^2) on top of world gravity while airborne.</summary>
        public static float ExtraAirGravity(GroundSettings settings, float verticalVelocity, float worldGravity)
        {
            float total = verticalVelocity > 0f ? settings.AirRiseGravity : settings.AirFallGravity;
            return total - Mathf.Abs(worldGravity);
        }
    }
}
