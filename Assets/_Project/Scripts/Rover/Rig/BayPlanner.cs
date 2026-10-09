using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Which turntable turn and which gantry arm fit each part of a kit piece onto a 07 parked on the bay's turntable
    /// (docs/ARCHITECTURE.md, "Contract: Kenji's Rover Bay"): of the turns (none, half round, a quarter either way) at
    /// which every part's socket, and the point above it the arm lowers from, lies within reach of a different arm,
    /// the one that keeps the parts best in view of the bay's open front (where the camera looks in from), the
    /// smaller turn when that is much the same; each part takes the arm with the most reach to spare. For a belly
    /// socket, whether the floor arm's lift axis passes under it. No plan means the bay cannot fit the piece: a
    /// contract bug, never a reason to drop it.
    /// </summary>
    public static class BayPlanner
    {
        /// <summary>Most arms a plan can weigh (it marks the arms in use as bits of an int).</summary>
        public const int MaxArms = 31;

        /// <summary>The turntable turns tried, smallest first (degrees about the turntable's up).</summary>
        private static readonly float[] Turns = { 0f, 180f, 90f, -90f };

        /// <summary>Turns that bring the parts this near (m) as far toward the open front are as good.</summary>
        private const float ViewTie = 0.1f;

        /// <summary>
        /// Plans arms for <paramref name="count"/> sockets given at the parked pose (no turn), the bay's open front
        /// lying along <paramref name="front"/> from the turntable's centre. On success <paramref name="turn"/> is the
        /// turntable turn first needed and <paramref name="armOf"/>[i] the arm for socket i.
        /// </summary>
        public static bool TryPlan(BayArm[] arms, Vector3 centre, Vector3 up, Vector3 front, Vector3[] sockets,
            int count, float hover, int[] armOf, out float turn)
        {
            int tried = 0;
            for (int attempt = 0; attempt < Turns.Length; attempt++)
            {
                int best = -1;
                float bestView = float.NegativeInfinity;
                for (int t = 0; t < Turns.Length; t++)
                {
                    float view = InView(centre, up, front, sockets, count, Turns[t]);
                    if ((tried & (1 << t)) == 0 && view > bestView + ViewTie)
                    {
                        best = t;
                        bestView = view;
                    }
                }

                tried |= 1 << best;
                if (TryTurn(arms, centre, up, sockets, count, hover, Turns[best], armOf))
                {
                    turn = Turns[best];
                    return true;
                }
            }

            turn = 0f;
            return false;
        }

        /// <summary>How far toward the open front (m) the least visible part sits after the turn.</summary>
        private static float InView(Vector3 centre, Vector3 up, Vector3 front, Vector3[] sockets, int count,
            float turn)
        {
            float least = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                least = Mathf.Min(least, Vector3.Dot(Turned(sockets[i], centre, up, turn) - centre, front));
            }

            return least;
        }

        /// <summary>
        /// The floor arm for a belly socket: the first turn at which the socket sits over the lift's axis (within
        /// <paramref name="align"/> m) and above the tip's rest, with the rise (m, along the lift's up) it needs.
        /// </summary>
        public static bool TryPlanFloor(Transform lift, Transform tip, Vector3 centre, Vector3 up, Vector3 socket,
            float align, out float turn, out float rise)
        {
            Transform frame = lift.parent;
            Vector3 tipLocal = frame.InverseTransformPoint(tip.position);
            for (int t = 0; t < Turns.Length; t++)
            {
                Vector3 local = frame.InverseTransformPoint(Turned(socket, centre, up, Turns[t]));
                Vector3 offset = local - tipLocal;
                float aside = new Vector2(offset.x, offset.z).magnitude;
                if (aside <= align && offset.y >= 0f)
                {
                    turn = Turns[t];
                    rise = offset.y;
                    return true;
                }
            }

            turn = 0f;
            rise = 0f;
            return false;
        }

        /// <summary>
        /// <paramref name="point"/> once the turntable turns <paramref name="degrees"/> about its centre.
        /// </summary>
        public static Vector3 Turned(Vector3 point, Vector3 centre, Vector3 up, float degrees)
        {
            return centre + Quaternion.AngleAxis(degrees, up) * (point - centre);
        }

        private static bool TryTurn(BayArm[] arms, Vector3 centre, Vector3 up, Vector3[] sockets, int count,
            float hover, float turn, int[] armOf)
        {
            int used = 0;
            for (int part = 0; part < count; part++)
            {
                Vector3 socket = Turned(sockets[part], centre, up, turn);
                int best = -1;
                float bestSlack = float.NegativeInfinity;
                for (int arm = 0; arm < arms.Length; arm++)
                {
                    if ((used & (1 << arm)) != 0)
                    {
                        continue;
                    }

                    float slack = Fits(arms[arm], socket, up, hover);
                    if (slack >= 0f && slack > bestSlack)
                    {
                        best = arm;
                        bestSlack = slack;
                    }
                }

                if (best < 0)
                {
                    return false;
                }

                armOf[part] = best;
                used |= 1 << best;
            }

            return true;
        }

        /// <summary>
        /// Reach to spare (m) for the socket and the point above it; negative when either is too far.
        /// </summary>
        private static float Fits(BayArm arm, Vector3 socket, Vector3 up, float hover)
        {
            Vector3 onto = arm.ToShoulder(socket);
            Vector3 above = arm.ToShoulder(socket + up * hover);
            BayArmGeometry geometry = arm.Geometry;
            if (!BayArmIk.TrySolve(geometry, onto, out _) || !BayArmIk.TrySolve(geometry, above, out _))
            {
                return -1f;
            }

            return Mathf.Min(BayArmIk.Slack(geometry, onto), BayArmIk.Slack(geometry, above));
        }
    }
}
