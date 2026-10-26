using UnityEngine;

namespace MoonProject.Art.PlayModeTests
{
    /// <summary>
    /// Look spike only: the light the sunlit regolith throws back up, as one unshadowed directional fill from below,
    /// leaning toward the sun's side (the ground there is lit; the ground on the far side lies in the thing's own
    /// shadow). Its strength follows the sun's: the ground's irradiance falls with the sine of the sun's elevation,
    /// times the regolith's albedo. It lights the undersides and shadow sides of 07, the lander and the panels; the
    /// ground itself, facing up, gets none of it, so shadows on the floor stay as deep as the airless sky makes them.
    /// </summary>
    public sealed class LookMaxBounceLight : MonoBehaviour
    {
        private const float RegolithAlbedo = 0.32f;
        private const float TowardSunLean = 0.45f;
        private static readonly Color RegolithTint = new Color(0.82f, 0.77f, 0.70f);

        private Light _light;

        public static LookMaxBounceLight Create()
        {
            var host = new GameObject("LookMaxBounceLight");
            var bounce = host.AddComponent<LookMaxBounceLight>();
            bounce._light = host.AddComponent<Light>();
            bounce._light.type = LightType.Directional;
            bounce._light.shadows = LightShadows.None;
            bounce.Follow();
            return bounce;
        }

        private void LateUpdate()
        {
            Follow();
        }

        /// <summary>Matches the current sun; call after moving the clock so the very next render is right.</summary>
        public void Follow()
        {
            Light sun = RenderSettings.sun;
            if (sun == null || !sun.isActiveAndEnabled)
            {
                _light.enabled = false;
                return;
            }

            Vector3 towardSun = -sun.transform.forward;
            float groundIrradiance = Mathf.Max(0f, towardSun.y);
            var fromBelow = new Vector3(towardSun.x * TowardSunLean, -1f, towardSun.z * TowardSunLean);
            _light.enabled = groundIrradiance > 0f;
            _light.transform.rotation = Quaternion.LookRotation(-fromBelow.normalized);
            _light.color = sun.color * RegolithTint;
            _light.intensity = sun.intensity * groundIrradiance * RegolithAlbedo;
        }
    }
}
