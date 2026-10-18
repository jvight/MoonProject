using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The two lights of the self-test: a cool rim from Earth's side and a soft warm fill that comes off 07's own
    /// screen. Both are off outside the test. They are made here, without shadows, so nothing in the scene has to be
    /// wired for them.
    /// </summary>
    public sealed class PostLights
    {
        private readonly PostSettings _settings;
        private readonly Light _rim;
        private readonly Light _fill;

        public PostLights(Transform parent, PostSettings settings)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            _rim = Create(parent, "PostRim", LightType.Spot, settings.RimColor, settings.RimRange);
            _rim.spotAngle = settings.RimSpotAngle;
            _fill = Create(parent, "PostFill", LightType.Point, settings.FillColor, settings.FillRange);
        }

        /// <summary>Places and sets both lights for this frame; zero shares switch them off.</summary>
        public void Apply(Vector3 body, Vector3 earthDirection, Vector3 screenPosition, Vector3 screenForward,
            float rimShare, float fillShare)
        {
            ApplyRim(body, earthDirection, rimShare);
            bool fillOn = fillShare > 0f;
            _fill.enabled = fillOn;
            if (fillOn)
            {
                _fill.transform.position = screenPosition + screenForward * _settings.FillOffset;
                _fill.intensity = _settings.FillIntensity * fillShare;
            }
        }

        private void ApplyRim(Vector3 body, Vector3 earthDirection, float share)
        {
            bool on = share > 0f;
            _rim.enabled = on;
            if (!on)
            {
                return;
            }

            Vector3 flat = new Vector3(earthDirection.x, 0f, earthDirection.z);
            Vector3 side = flat.sqrMagnitude > 1e-6f ? flat.normalized : Vector3.forward;
            Vector3 aim = body + Vector3.up * _settings.RimAimHeight;
            Vector3 position = body + side * _settings.RimDistance + Vector3.up * _settings.RimHeight;
            _rim.transform.SetPositionAndRotation(position, Quaternion.LookRotation(aim - position, Vector3.up));
            _rim.intensity = _settings.RimIntensity * share;
        }

        private static Light Create(Transform parent, string name, LightType type, Color color, float range)
        {
            var host = new GameObject(name);
            host.transform.SetParent(parent, false);
            Light light = host.AddComponent<Light>();
            light.type = type;
            light.color = color;
            light.range = range;
            light.shadows = LightShadows.None;
            light.enabled = false;
            return light;
        }
    }
}
