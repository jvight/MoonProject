using UnityEngine;

namespace MoonProject.Showcase
{
    /// <summary>
    /// Plain C# helper that drives <c>_EmissionStrength</c> on <c>R1_LampAmber</c> renderers via a
    /// <see cref="MaterialPropertyBlock"/> without touching the authored amber color, plus optional companion point or
    /// spot lights. Constructed by <see cref="ShowcaseExhibitRig"/> at runtime (never in a field initializer).
    /// </summary>
    public sealed class ShowcaseLampDriver
    {
        public const string EmissionStrengthProperty = "_EmissionStrength";

        private readonly MaterialPropertyBlock _propertyBlock;
        private readonly int _emissionStrengthId;
        private float _currentState01;

        public ShowcaseLampDriver(bool initiallyLit)
        {
            _propertyBlock = new MaterialPropertyBlock();
            _emissionStrengthId = Shader.PropertyToID(EmissionStrengthProperty);
            _currentState01 = initiallyLit ? 1f : 0f;
        }

        public float CurrentState01 => _currentState01;

        public float EasedState01 => ShowcaseEasing.SmootherStep01(_currentState01);

        public void StepTowards(bool lit, float easeSeconds, float deltaTime)
        {
            float target = lit ? 1f : 0f;
            _currentState01 = ShowcaseEasing.AdvanceNormalized(_currentState01, target, easeSeconds, deltaTime);
        }

        public void ApplyUniform(
            MeshRenderer[] renderers,
            float offEmission,
            float onEmission,
            Light[] lights = null,
            float[] lightMaxIntensities = null)
        {
            float eased = EasedState01;
            float emission = Mathf.Lerp(offEmission, onEmission, eased);
            WriteEmission(renderers, emission);
            WriteLights(lights, lightMaxIntensities, eased);
        }

        public void ApplyPulsing(
            MeshRenderer[] barRenderers,
            MeshRenderer[] stripRenderers,
            float offEmission,
            float pulseMinEmission,
            float pulseMaxEmission,
            float pulseSpeed,
            float timeSeconds,
            Light[] lights = null,
            float[] lightMaxIntensities = null)
        {
            float master = EasedState01;
            if (barRenderers != null)
            {
                for (int i = 0; i < barRenderers.Length; i++)
                {
                    MeshRenderer renderer = barRenderers[i];
                    if (renderer == null)
                    {
                        continue;
                    }

                    float phase = (timeSeconds * pulseSpeed - i * 0.22f) * Mathf.PI * 2f;
                    float wave01 = 0.5f + 0.5f * Mathf.Sin(phase);
                    float pulsed = Mathf.Lerp(pulseMinEmission, pulseMaxEmission, ShowcaseEasing.SmoothStep01(wave01));
                    float finalEmission = Mathf.Lerp(offEmission, pulsed, master);
                    WriteSingleRenderer(renderer, finalEmission);
                }
            }

            if (stripRenderers != null)
            {
                float stripWave = 0.5f + 0.5f * Mathf.Sin(timeSeconds * pulseSpeed * Mathf.PI * 2f);
                float stripEmission = Mathf.Lerp(
                    offEmission,
                    Mathf.Lerp(pulseMinEmission * 0.8f, pulseMaxEmission * 0.85f, stripWave),
                    master);
                WriteEmission(stripRenderers, stripEmission);
            }

            WriteLights(lights, lightMaxIntensities, master);
        }

        private void WriteEmission(MeshRenderer[] renderers, float emissionStrength)
        {
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    WriteSingleRenderer(renderers[i], emissionStrength);
                }
            }
        }

        private void WriteSingleRenderer(MeshRenderer renderer, float emissionStrength)
        {
            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(_emissionStrengthId, emissionStrength);
            renderer.SetPropertyBlock(_propertyBlock);
        }

        private static void WriteLights(Light[] lights, float[] maxIntensities, float factor01)
        {
            if (lights == null)
            {
                return;
            }

            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light == null)
                {
                    continue;
                }

                float maxIntensity = (maxIntensities != null && i < maxIntensities.Length)
                    ? maxIntensities[i]
                    : 1.8f;
                light.intensity = maxIntensity * factor01;
                light.enabled = factor01 > 0.001f;
            }
        }
    }
}
