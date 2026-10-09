using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Composes the camera's close shot of a Rover Bay fitting (<see cref="BayShotSettings"/>) once, as the fitting
    /// begins: of the bearings round the socket, the one nearest a three-quarter view of the working arm (or, under
    /// the belly, the low view in from the open front) where the camera stands between the bay's side walls (or out
    /// through its open front), clear of the walls with nothing solid looming in front of the lens, under the crane
    /// rail, reachable from the bay's view along a clear line and with the piece in sight past 07's bulk. Pure and
    /// allocation-free; the scenery is asked through <see cref="ISightLine"/>.
    /// </summary>
    public static class BayShotComposer
    {
        /// <summary>Bearings tried round the socket (every 15 degrees).</summary>
        private const int Bearings = 24;

        /// <summary>Points tested along a sight line for 07's bulk.</summary>
        private const int SightSamples = 12;

        /// <summary>Directions shorter than this have no bearing.</summary>
        private const float Flatness = 1e-4f;

        /// <summary>A sight line counts as clear when it reaches within this (m) of its end.</summary>
        private const float ClearSlack = 1e-3f;

        /// <summary>
        /// The shot for <paramref name="fit"/>, reached from the camera at <paramref name="approach"/> (the bay's
        /// view) and standing no higher than <paramref name="ceiling"/> (world y). False when the bay leaves no room
        /// for one: the bay's view holds.
        /// </summary>
        public static bool TrySolve(BayShotSettings settings, IBayFitView fit, Vector3 approach, float ceiling,
            ISightLine sight, out BayShot shot)
        {
            bool belly = fit.Belly;
            Vector3 focus = fit.Socket + Vector3.up * (belly ? settings.BellyFocusLift : settings.ArmFocusLift);
            float distance = belly ? settings.BellyDistance : settings.ArmDistance;
            float elevation = belly ? settings.BellyElevation : settings.ArmElevation;
            Vector3 front = Flat(fit.Front, Vector3.forward);
            Vector3 across = Vector3.Cross(Vector3.up, front);
            Ideals(settings, fit, front, out Vector3 ideal, out Vector3 mirrored);

            shot = default;
            bool found = false;
            float best = float.NegativeInfinity;
            for (int i = 0; i < Bearings; i++)
            {
                Vector3 away = Quaternion.AngleAxis(i * 360f / Bearings, Vector3.up) * Vector3.forward;
                if (!TryPlace(settings, focus, away, distance, elevation, ceiling, sight, out Vector3 eye,
                        out float reached)
                    || Mathf.Abs(Vector3.Dot(eye - fit.Centre, across)) > settings.SideReach
                    || !sight.IsFree(eye, settings.WallClearance)
                    || !FrameClear(settings, focus, eye, sight)
                    || (!belly && Hidden(settings, fit, focus, eye))
                    || sight.Clear(approach, eye, settings.SightRadius)
                    < Vector3.Distance(approach, eye) - ClearSlack)
                {
                    continue;
                }

                float score = -Mathf.Min(Vector3.Angle(away, ideal), Vector3.Angle(away, mirrored))
                    + settings.FrontPreference * Vector3.Dot(away, front)
                    - settings.ShortfallPenalty * (distance - reached);
                if (score > best)
                {
                    best = score;
                    shot = new BayShot(focus, eye);
                    found = true;
                }
            }

            return found;
        }

        /// <summary>
        /// The ideal directions from the socket to the camera: three-quarter on the working arm, from either side of
        /// it, turned toward the arm's own side; for a belly piece, low in from the open front, the same twice.
        /// </summary>
        private static void Ideals(BayShotSettings settings, IBayFitView fit, Vector3 front, out Vector3 ideal,
            out Vector3 mirrored)
        {
            if (fit.Belly)
            {
                ideal = Quaternion.AngleAxis(settings.BellyBearing, Vector3.up) * front;
                mirrored = ideal;
                return;
            }

            Vector3 outward = Flat(fit.Socket - fit.Centre, front);
            Vector3 toArm = Flat(fit.Shoulder - fit.Socket, outward);
            Vector3 side = Vector3.Cross(Vector3.up, toArm);
            float quarter = settings.ArmQuarter * Mathf.Deg2Rad;
            float along = Mathf.Sin(quarter);
            float across = Mathf.Cos(quarter);
            ideal = side * across + toArm * along;
            mirrored = -side * across + toArm * along;
        }

        /// <summary>
        /// Where the camera stands looking back along <paramref name="away"/> at the focus: at the shot's distance, or
        /// as near as the walls leave room for (no nearer than the minimum), pitched down no further than the crane
        /// rail allows.
        /// </summary>
        private static bool TryPlace(BayShotSettings settings, Vector3 focus, Vector3 away, float distance,
            float elevation, float ceiling, ISightLine sight, out Vector3 eye, out float reached)
        {
            eye = focus;
            reached = 0f;
            float room = ceiling - focus.y;
            if (room <= 0f)
            {
                return false;
            }

            float pitch = Mathf.Min(elevation * Mathf.Deg2Rad, Mathf.Asin(Mathf.Clamp01(room / distance)));
            Vector3 direction = away * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch);
            float reach = distance + settings.WallClearance;
            reached = Mathf.Min(distance,
                sight.Clear(focus, focus + direction * reach, settings.SightRadius) - settings.WallClearance);
            if (reached < settings.MinDistance)
            {
                return false;
            }

            eye = focus + direction * reached;
            return true;
        }

        /// <summary>
        /// True when lines from the camera through the corners of the middle of the frame run clear most of the way to
        /// the focus's distance: no wall edge or post looms in front of the lens.
        /// </summary>
        private static bool FrameClear(BayShotSettings settings, Vector3 focus, Vector3 eye, ISightLine sight)
        {
            Vector3 look = focus - eye;
            float distance = look.magnitude;
            Vector3 forward = look / distance;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 up = Vector3.Cross(forward, right);
            float width = distance * Mathf.Tan(settings.FrameWidth * Mathf.Deg2Rad);
            float height = distance * Mathf.Tan(settings.FrameHeight * Mathf.Deg2Rad);
            for (int corner = 0; corner < 4; corner++)
            {
                float across = (corner & 1) == 0 ? -width : width;
                float upward = (corner & 2) == 0 ? -height : height;
                Vector3 point = focus + right * across + up * upward;
                if (sight.Clear(eye, point, 0f) < settings.FrameClearShare * Vector3.Distance(eye, point))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// True when 07's bulk stands between the camera and the piece coming down on its socket (the focus, just
        /// above the socket; short of it).
        /// </summary>
        private static bool Hidden(BayShotSettings settings, IBayFitView fit, Vector3 focus, Vector3 eye)
        {
            Vector3 facing = Flat(fit.Facing, Vector3.forward);
            Vector3 right = Vector3.Cross(Vector3.up, facing);
            Vector3 line = focus - eye;
            float length = line.magnitude;
            float open = Mathf.Max(0f, length - settings.SocketMargin) / length;
            for (int i = 0; i < SightSamples; i++)
            {
                Vector3 local = eye + line * (open * i / (SightSamples - 1)) - fit.Centre;
                if (Mathf.Abs(Vector3.Dot(local, right)) < settings.BodyHalfWidth
                    && Mathf.Abs(Vector3.Dot(local, facing)) < settings.BodyHalfLength
                    && local.y > 0f && local.y < settings.BodyHeight)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// <paramref name="direction"/> flattened onto the ground and normalised, or the fallback if it is vertical.
        /// </summary>
        private static Vector3 Flat(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > Flatness * Flatness ? direction.normalized : fallback;
        }
    }
}
