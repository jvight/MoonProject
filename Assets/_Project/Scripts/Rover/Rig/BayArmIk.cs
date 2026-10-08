using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Analytic inverse kinematics for one Rover Bay gantry arm (docs/ARCHITECTURE.md, "Contract: Kenji's Rover Bay"),
    /// in the arm's shoulder frame: origin at the shoulder pivot, +Y up, the shoulder's zero yaw facing +Z. The
    /// shoulder turns about Y so the target lies ahead in the arm's pitch plane; the upper and lower links (pitch about
    /// local X, each hanging along its local -Y at zero) put the wrist straight above the target; the wrist keeps the
    /// tip pointing straight down, so a piece held at the tip's end arrives level. The elbow bends back and up, like
    /// the folded rest pose, so it never swings through 07.
    /// </summary>
    public static class BayArmIk
    {
        /// <summary>
        /// The pose that puts the tip's end exactly on <paramref name="target"/> (shoulder frame), tip straight down.
        /// False when the target is out of reach (or so close under the shoulder that the links cannot fold to it).
        /// </summary>
        public static bool TrySolve(BayArmGeometry arm, Vector3 target, out BayArmPose pose)
        {
            float yaw = Mathf.Atan2(target.x, target.z) * Mathf.Rad2Deg;
            float ahead = Mathf.Sqrt(target.x * target.x + target.z * target.z);
            var wrist = new Vector2(ahead, target.y + arm.Tip);
            float distance = wrist.magnitude;
            if (distance > arm.Reach || distance < Mathf.Abs(arm.Upper - arm.Lower) || distance <= 0f)
            {
                pose = default;
                return false;
            }

            float toWrist = Pitch(wrist);
            float cosine = (arm.Upper * arm.Upper + distance * distance - arm.Lower * arm.Lower)
                / (2f * arm.Upper * distance);
            float upper = toWrist + Mathf.Acos(Mathf.Clamp(cosine, -1f, 1f)) * Mathf.Rad2Deg;
            Vector2 elbow = Link(upper, arm.Upper);
            float lower = Pitch(wrist - elbow);
            pose = new BayArmPose(yaw, upper, lower - upper, -lower);
            return true;
        }

        /// <summary>Where the tip's end of an arm in <paramref name="pose"/> is (shoulder frame).</summary>
        public static Vector3 EndOf(BayArmGeometry arm, BayArmPose pose)
        {
            float upper = pose.Upper;
            float lower = upper + pose.Lower;
            float tip = lower + pose.Tip;
            Vector2 end = Link(upper, arm.Upper) + Link(lower, arm.Lower) + Link(tip, arm.Tip);
            return Quaternion.Euler(0f, pose.Yaw, 0f) * new Vector3(0f, end.y, end.x);
        }

        /// <summary>
        /// How far (m) the wrist could still stretch toward <paramref name="target"/>: positive when reachable, the
        /// larger the more comfortably; negative when out of reach by that much.
        /// </summary>
        public static float Slack(BayArmGeometry arm, Vector3 target)
        {
            float ahead = Mathf.Sqrt(target.x * target.x + target.z * target.z);
            return arm.Reach - new Vector2(ahead, target.y + arm.Tip).magnitude;
        }

        /// <summary>Shortest-way blend of two poses (joint space), <paramref name="t"/> 0..1.</summary>
        public static BayArmPose Blend(BayArmPose from, BayArmPose to, float t)
        {
            return new BayArmPose(Mathf.LerpAngle(from.Yaw, to.Yaw, t), Mathf.LerpAngle(from.Upper, to.Upper, t),
                Mathf.LerpAngle(from.Lower, to.Lower, t), Mathf.LerpAngle(from.Tip, to.Tip, t));
        }

        /// <summary>
        /// A link of <paramref name="length"/> at a cumulative pitch (deg), in the arm plane (ahead, up).
        /// </summary>
        private static Vector2 Link(float pitch, float length)
        {
            float radians = pitch * Mathf.Deg2Rad;
            return new Vector2(-Mathf.Sin(radians), -Mathf.Cos(radians)) * length;
        }

        /// <summary>The cumulative pitch (deg) of a link along <paramref name="direction"/> (ahead, up).</summary>
        private static float Pitch(Vector2 direction)
        {
            return Mathf.Atan2(-direction.x, -direction.y) * Mathf.Rad2Deg;
        }
    }
}
