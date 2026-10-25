using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The scan sweep's laser line as <c>LofiScanInput.hlsl</c> draws it on a target's renderer (pure; EditMode-tested):
    /// a plane climbing the renderer's world bounds, tilted toward the camera so tops and faces both sweep. The scan
    /// beam aims where the shader draws its line, so the two meet on the surface.
    /// </summary>
    public static class ScanLine
    {
        /// <summary>The line's tilt toward the camera (MoonScanSurfaceCoords' 0.58): keep in step with the shader.
        /// </summary>
        public const float Tilt = 0.58f;

        /// <summary>How far past the bounds' top and bottom the tilted sweep runs (the shader's 0.82).</summary>
        public const float TiltReach = 0.82f;

        /// <summary>Share of the target's height the beam's aim keeps clear of its very top and bottom.</summary>
        public const float AimInset = 0.04f;

        /// <summary>
        /// The diameter (m) of the beam's patch of light on a target <paramref name="objectHeight"/> tall: the most it may
        /// be, and no more than <paramref name="objectShare"/> of the target's height, so a small target is never swamped.
        /// </summary>
        public static float PatchSize(float objectHeight, float maxMetres, float objectShare)
        {
            return Mathf.Min(maxMetres, objectHeight * objectShare);
        }

        /// <summary>
        /// <paramref name="point"/> pulled inside <paramref name="bounds"/>, at least <paramref name="inset"/> from its
        /// faces on each axis it is deep enough for (else at its middle): a point there, and a patch of that radius
        /// round it, never leave the target's projected bounds on any camera.
        /// </summary>
        public static Vector3 ClampInside(Bounds bounds, float inset, Vector3 point)
        {
            return new Vector3(ClampAxis(point.x, bounds.min.x, bounds.max.x, inset),
                ClampAxis(point.y, bounds.min.y, bounds.max.y, inset),
                ClampAxis(point.z, bounds.min.z, bounds.max.z, inset));
        }

        private static float ClampAxis(float value, float min, float max, float inset)
        {
            return min + inset >= max - inset ? 0.5f * (min + max) : Mathf.Clamp(value, min + inset, max - inset);
        }

        /// <summary>The flat direction from <paramref name="centre"/> toward <paramref name="camera"/>.</summary>
        public static Vector2 TowardCamera(Vector3 centre, Vector3 camera)
        {
            var toward = new Vector2(camera.x - centre.x, camera.z - centre.z);
            return toward.sqrMagnitude > 1e-8f ? toward.normalized : Vector2.up;
        }

        /// <summary>The tilted sweep height of <paramref name="point"/> on a renderer centred at
        /// <paramref name="centre"/>.</summary>
        public static float SweepHeightOf(Vector3 point, Vector3 centre, Vector2 towardCamera)
        {
            return point.y - ((point.x - centre.x) * towardCamera.x + (point.z - centre.z) * towardCamera.y) * Tilt;
        }

        /// <summary>The sweep height the line stands at, <paramref name="progress"/> (0..1) up <paramref name="bounds"/>.
        /// </summary>
        public static float Height(Bounds bounds, Vector2 towardCamera, float progress)
        {
            float tiltExtent = (bounds.extents.x * Mathf.Abs(towardCamera.x) +
                                bounds.extents.z * Mathf.Abs(towardCamera.y)) * Tilt * TiltReach;
            return Mathf.Lerp(bounds.min.y - tiltExtent, bounds.max.y + tiltExtent, Mathf.Clamp01(progress));
        }

        /// <summary>
        /// Where the beam aims for a line at sweep height <paramref name="height"/>: on the tilted line itself, at a
        /// height kept inside the target's own top and bottom and shifted toward or away from the camera by the rest
        /// (so on a low, flat target the aim travels across its top as the line does), inside its footprint.
        /// </summary>
        public static Vector3 AimPoint(Bounds bounds, float height, Vector2 towardCamera)
        {
            float inset = bounds.size.y * AimInset;
            float y = Mathf.Clamp(height, bounds.min.y + inset, bounds.max.y - inset);
            float reach = (bounds.extents.x * Mathf.Abs(towardCamera.x) + bounds.extents.z * Mathf.Abs(towardCamera.y)) *
                          (1f - AimInset);
            float along = Mathf.Clamp((y - height) / Tilt, -reach, reach);
            return new Vector3(bounds.center.x + towardCamera.x * along, y, bounds.center.z + towardCamera.y * along);
        }
    }
}
