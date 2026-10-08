using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// One of the Rover Bay's gantry arms as Rover drives it (through <see cref="IRoverBay"/>): its shoulder frame,
    /// link lengths and folded rest pose read from the joints, poses written back to them. The joints follow the art
    /// contract: Yaw turns about local Y, Upper sits at the Yaw's origin, and Upper, Lower and Tip pitch about local X
    /// with Lower, Tip and the tip's end hanging along the parent's local -Y.
    /// </summary>
    public sealed class BayArm
    {
        /// <summary>Joint offsets off the contract's -Y hang, or rotations off pure X, beyond this break it.</summary>
        private const float ContractTolerance = 1e-3f;

        /// <summary>Where the art contract the bay must meet is written down (for error messages).</summary>
        public const string Contract = "docs/ARCHITECTURE.md, \"Contract: Kenji's Rover Bay\"";

        private readonly Transform _yaw;
        private readonly Transform _upper;
        private readonly Transform _lower;
        private readonly Transform _tip;
        private readonly Quaternion _yawRest;

        private BayArm(Transform yaw, Transform upper, Transform lower, Transform tip, BayArmGeometry geometry,
            BayArmPose rest)
        {
            _yaw = yaw;
            _upper = upper;
            _lower = lower;
            _tip = tip;
            _yawRest = yaw.localRotation;
            Geometry = geometry;
            Rest = rest;
        }

        public BayArmGeometry Geometry { get; }

        /// <summary>The folded pose the arm hangs in between fittings.</summary>
        public BayArmPose Rest { get; }

        /// <summary>The tip's end, where a carried piece is held (world).</summary>
        public Vector3 TipEnd => _tip.TransformPoint(new Vector3(0f, -Geometry.Tip, 0f));

        /// <summary>
        /// Reads arm <paramref name="index"/> of <paramref name="bay"/>; null (with <paramref name="problem"/> saying
        /// why) when its joints break the art contract.
        /// </summary>
        public static BayArm Read(IRoverBay bay, int index, out string problem)
        {
            if (bay == null)
            {
                throw new ArgumentNullException(nameof(bay));
            }

            Transform yaw = bay.GetArmJoint(index, RoverBayJoint.Yaw);
            Transform upper = bay.GetArmJoint(index, RoverBayJoint.Upper);
            Transform lower = bay.GetArmJoint(index, RoverBayJoint.Lower);
            Transform tip = bay.GetArmJoint(index, RoverBayJoint.Tip);
            Transform end = bay.GetArmJoint(index, RoverBayJoint.SparkSocket);
            if (yaw == null || upper == null || lower == null || tip == null || end == null)
            {
                problem = $"arm {index} lacks a joint (Yaw/Upper/Lower/Tip/SparkSocket).";
                return null;
            }

            if (upper.parent != yaw || lower.parent != upper || tip.parent != lower || end.parent != tip)
            {
                problem = $"arm {index}'s joints are not chained Yaw/Upper/Lower/Tip/SparkSocket.";
                return null;
            }

            if (upper.localPosition.sqrMagnitude > ContractTolerance * ContractTolerance
                || !Hangs(lower) || !Hangs(tip) || !PureX(upper) || !PureX(lower) || !PureX(tip))
            {
                problem = $"arm {index}'s links do not hang along -Y from pure X pitch joints at the shoulder.";
                return null;
            }

            var geometry = new BayArmGeometry(-lower.localPosition.y, -tip.localPosition.y, -end.localPosition.y);
            var rest = new BayArmPose(0f, PitchOf(upper.localRotation), PitchOf(lower.localRotation),
                PitchOf(tip.localRotation));
            problem = null;
            return new BayArm(yaw, upper, lower, tip, geometry, rest);
        }

        /// <summary>
        /// <paramref name="world"/> in this arm's shoulder frame (origin at the shoulder, +Z at zero yaw).
        /// </summary>
        public Vector3 ToShoulder(Vector3 world)
        {
            Vector3 local = _yaw.parent.InverseTransformPoint(world) - _yaw.localPosition;
            return Quaternion.Inverse(_yawRest) * local;
        }

        /// <summary>Writes <paramref name="pose"/> to the joints.</summary>
        public void Apply(BayArmPose pose)
        {
            _yaw.localRotation = _yawRest * Quaternion.Euler(0f, pose.Yaw, 0f);
            _upper.localRotation = Quaternion.Euler(pose.Upper, 0f, 0f);
            _lower.localRotation = Quaternion.Euler(pose.Lower, 0f, 0f);
            _tip.localRotation = Quaternion.Euler(pose.Tip, 0f, 0f);
        }

        private static bool Hangs(Transform joint)
        {
            Vector3 offset = joint.localPosition;
            return offset.y < 0f && Mathf.Abs(offset.x) < ContractTolerance && Mathf.Abs(offset.z) < ContractTolerance;
        }

        private static bool PureX(Transform joint)
        {
            Quaternion rotation = joint.localRotation;
            return Mathf.Abs(rotation.y) < ContractTolerance && Mathf.Abs(rotation.z) < ContractTolerance;
        }

        /// <summary>The angle (deg) of a rotation purely about local X.</summary>
        private static float PitchOf(Quaternion rotation)
        {
            return Mathf.DeltaAngle(0f, 2f * Mathf.Atan2(rotation.x, rotation.w) * Mathf.Rad2Deg);
        }
    }
}
