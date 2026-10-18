using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// The rover's interaction points, registered in the <see cref="GameContext"/> by the Rover domain next to
    /// <see cref="IRoverState"/>. Gameplay attaches beams and cargo here and asks 07 to look at things; the rover
    /// owns how that looks (eased gaze, body language).
    /// </summary>
    public interface IRoverRig
    {
        /// <summary>Lens centre of 07's cheek camera pod (`EyeOrigin`); +Z is the gaze. Legacy alias for <see cref="ScanOrigin"/>.</summary>
        Transform TetherOrigin { get; }

        /// <summary>Chest cutter turret nozzle (`CutterOrigin`). Salvage cutter, repair, stitch, and workshop feed beams leave from here.</summary>
        Transform CutterOrigin { get; }

        /// <summary>Cheek camera pod lens (`EyeOrigin`). Scan beam and scan ribbon leave from here.</summary>
        Transform ScanOrigin { get; }

        /// <summary>Rear winch tow hook (`WinchOrigin`). Tether rope and tow anchor attach here.</summary>
        Transform WinchOrigin { get; }

        /// <summary>Front dig scoop bucket (`ScoopOrigin`). Excavation beam leaves from here.</summary>
        Transform ScoopOrigin { get; }

        /// <summary>Where the cargo bed upgrade attaches.</summary>
        Transform CargoSocket { get; }

        /// <summary>The rover's physics body (the hidden sphere). Read it or apply reaction forces; never move it.</summary>
        Rigidbody PhysicsBody { get; }

        /// <summary>
        /// Asks 07 to look at <paramref name="worldPosition"/>. The highest-priority active request wins; ties go to
        /// the most recent. Calling again with the same owner updates its target.
        /// </summary>
        void SetGazeTarget(object owner, Vector3 worldPosition, int priority);

        /// <summary>Withdraws <paramref name="owner"/>'s gaze request (no-op if it has none).</summary>
        void ClearGazeTarget(object owner);

        /// <summary>
        /// Asks 07 to ease to a gentle stop and stay parked while any owner holds the request (e.g. the excavation
        /// beam). Throttle is ignored (not queued) while held; releasing hands control back with the normal easing.
        /// </summary>
        void SetHoldStill(object owner, bool hold);
    }

    /// <summary>
    /// Marker interface for gameplay tool owners (salvage, crew crates, sealed containers, structure welding) whose
    /// beam originates at <see cref="IRoverRig.CutterOrigin"/> in 07's chest, requiring 07 to turn its body until the
    /// chest nozzle faces the cut/weld point before firing.
    /// </summary>
    public interface ICutterAimOwner
    {
    }

    /// <summary>
    /// Optional extension implemented by <see cref="IRoverRig"/> providers that evaluate chest cutter line-of-sight and
    /// self-occlusion against 07's colliders and structural meshes.
    /// </summary>
    public interface ICutterRig
    {
        bool HasCutterLineOfSight(Vector3 worldPosition);
    }

    public static class RoverRigExtensions
    {
        private const float DefaultMaxHorizontalYawDeg = 35f;

        /// <summary>
        /// True when <paramref name="rig"/>'s <see cref="IRoverRig.CutterOrigin"/> faces <paramref name="worldPosition"/>
        /// within the cutter's firing cone and has an unoccluded line of sight clear of 07's own colliders and chassis.
        /// </summary>
        public static bool HasCutterLineOfSight(this IRoverRig rig, Vector3 worldPosition)
        {
            if (rig == null)
            {
                return true;
            }

            if (rig is ICutterRig cutterRig)
            {
                return cutterRig.HasCutterLineOfSight(worldPosition);
            }

            Transform origin = rig.CutterOrigin;
            if (origin == null)
            {
                return true;
            }

            Vector3 toTarget = worldPosition - origin.position;
            float distSq = toTarget.sqrMagnitude;
            if (distSq <= 1e-8f)
            {
                return true;
            }

            Vector3 flatFwd = new Vector3(origin.forward.x, 0f, origin.forward.z);
            Vector3 flatTo = new Vector3(toTarget.x, 0f, toTarget.z);
            if (flatFwd.sqrMagnitude > 1e-8f && flatTo.sqrMagnitude > 1e-8f)
            {
                if (Vector3.Angle(flatFwd, flatTo) > DefaultMaxHorizontalYawDeg)
                {
                    return false;
                }
            }

            return Vector3.Dot(origin.forward, toTarget.normalized) > 0.1f;
        }
    }
}
