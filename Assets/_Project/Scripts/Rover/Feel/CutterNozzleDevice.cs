using System;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;
using MoonProject.Core.Events;
using Object = UnityEngine.Object;

namespace MoonProject.Rover
{
    /// <summary>
    /// Mechanical iris, hot lens core, and warm chest/bumper spill light mounted on 07's chest cutter nozzle
    /// (<see cref="IRoverRig.CutterOrigin"/>). During charge-up the 6-blade iris opens and the hot core brightens on
    /// the lens surface before the beam fires; while cutting or welding, warm light spills across 07's chest and front
    /// bumper. Zero allocations per frame.
    /// </summary>
    public sealed class CutterNozzleDevice : IDisposable
    {
        private const int BladeCount = 6;
        private const float ClosedBladeRadius = 0.015f;
        private const float OpenBladeRadius = 0.044f;
        private const float OpenBladeTwistDeg = 42f;
        private const float CoreRestDiameter = 0.018f;
        private const float CoreHotDiameter = 0.046f;
        private const float CoreDepth = 0.006f;
        private const float ChargeSpillIntensity = 0.95f;
        private const float FireSpillIntensity = 1.55f;
        private const float SpillFlickerAmplitude = 0.25f;
        private const float SpillFlickerRate = 19f;
        private const float SpillRange = 1.95f;
        private const float ReleaseHalfLife = 0.06f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionStrengthId = Shader.PropertyToID("_EmissionStrength");

        private static readonly Color BladeColor = new Color(0.16f, 0.12f, 0.08f, 1f);
        private static readonly Color WarmCoreColor = new Color(1.0f, 0.72f, 0.26f, 1f);
        private static readonly Color HotCoreColor = new Color(1.0f, 0.94f, 0.78f, 1f);
        private static readonly Color SpillColor = new Color(1.0f, 0.68f, 0.28f, 1f);

        private readonly Transform _origin;
        private readonly Transform[] _blades = new Transform[BladeCount];
        private readonly Renderer[] _bladeRenderers = new Renderer[BladeCount];
        private readonly Transform _coreTransform;
        private readonly Renderer _coreRenderer;
        private readonly Light _spillLight;
        private readonly Material _deviceMaterial;
        private readonly bool _ownsMaterial;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private readonly IDisposable[] _subscriptions;

        private bool _charging;
        private bool _firing;
        private float _chargeElapsed;
        private float _chargeDuration = 0.25f;

        public CutterNozzleDevice(EventBus events, Transform cutterOrigin, Material templateMaterial = null)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            _origin = cutterOrigin != null ? cutterOrigin : throw new ArgumentNullException(nameof(cutterOrigin));
            SnapOriginToNozzleLensSurface(_origin);

            Shader shader = templateMaterial != null && templateMaterial.shader != null
                ? templateMaterial.shader
                : Shader.Find("Universal Render Pipeline/Lit");
            if (shader != null)
            {
                _deviceMaterial = new Material(shader) { name = "CutterNozzleDevice_Runtime" };
                _deviceMaterial.EnableKeyword("_EMISSION");
                _ownsMaterial = true;
            }
            else
            {
                _deviceMaterial = templateMaterial;
                _ownsMaterial = false;
            }

            int layer = _origin.gameObject.layer;
            for (int i = 0; i < BladeCount; i++)
            {
                var bladeGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bladeGo.name = "NozzleIrisBlade_" + i;
                Collider col = bladeGo.GetComponent<Collider>();
                if (col != null)
                {
                    Object.DestroyImmediate(col);
                }

                bladeGo.layer = layer;
                Transform tr = bladeGo.transform;
                tr.SetParent(_origin, false);
                tr.localScale = new Vector3(0.028f, 0.013f, 0.007f);
                var renderer = bladeGo.GetComponent<Renderer>();
                ConfigureRenderer(renderer, _deviceMaterial);
                _blades[i] = tr;
                _bladeRenderers[i] = renderer;
            }

            var coreGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coreGo.name = "NozzleHotCore";
            Collider coreCol = coreGo.GetComponent<Collider>();
            if (coreCol != null)
            {
                Object.DestroyImmediate(coreCol);
            }

            coreGo.layer = layer;
            _coreTransform = coreGo.transform;
            _coreTransform.SetParent(_origin, false);
            _coreTransform.localPosition = new Vector3(0f, 0f, 0.001f);
            _coreTransform.localRotation = Quaternion.identity;
            _coreRenderer = coreGo.GetComponent<Renderer>();
            ConfigureRenderer(_coreRenderer, _deviceMaterial);

            var lightGo = new GameObject("NozzleSpillLight") { layer = layer };
            lightGo.transform.SetParent(_origin, false);
            lightGo.transform.localPosition = new Vector3(0f, -0.05f, 0.20f);
            _spillLight = lightGo.AddComponent<Light>();
            _spillLight.type = LightType.Point;
            _spillLight.shadows = LightShadows.None;
            _spillLight.color = SpillColor;
            _spillLight.range = SpillRange;
            _spillLight.intensity = 0f;
            _spillLight.enabled = false;

            ApplyVisuals(0f);

            _subscriptions = new[]
            {
                events.Subscribe<CutChargeStaged>(OnChargeStaged),
                events.Subscribe<FeelBeat>(OnFeelBeat),
                events.Subscribe<BuildWeldStateChanged>(OnWeldStateChanged),
            };
        }

        /// <summary>Normalized [0..1] opening of the 6-blade mechanical iris around the brass nozzle lens.</summary>
        public float IrisOpen01 { get; private set; }

        /// <summary>Normalized [0..1] glow of the hot core on the nozzle lens surface.</summary>
        public float CoreGlow01 { get; private set; }

        /// <summary>Current intensity of the warm chest/bumper spill light (0 when idle).</summary>
        public float SpillLightIntensity => _spillLight != null && _spillLight.enabled ? _spillLight.intensity : 0f;

        /// <summary>The warm point light spilling across 07's chest and front bumper while charging/firing.</summary>
        public Light SpillLight => _spillLight;

        public bool IsCharging => _charging;

        public bool IsFiring => _firing;

        /// <summary>
        /// Snaps <paramref name="cutterOrigin"/> onto the front apex of its parent <c>CutterHead</c> mesh bounds so
        /// the charge core and beam originate directly on the visible brass lens surface rather than inside the chest.
        /// </summary>
        public static void SnapOriginToNozzleLensSurface(Transform cutterOrigin)
        {
            if (cutterOrigin == null || cutterOrigin.parent == null)
            {
                return;
            }

            if (cutterOrigin.parent.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
            {
                Bounds bounds = filter.sharedMesh.bounds;
                if (bounds.size.sqrMagnitude > 1e-6f)
                {
                    cutterOrigin.localPosition = new Vector3(bounds.center.x, bounds.center.y, bounds.max.z);
                    cutterOrigin.localRotation = Quaternion.identity;
                }
            }
        }

        public void Tick(float deltaTime, float time)
        {
            if (_charging)
            {
                _chargeElapsed += Mathf.Max(0f, deltaTime);
                float t = Mathf.Clamp01(_chargeElapsed / Mathf.Max(0.05f, _chargeDuration));
                float smooth = t * t * (3f - 2f * t);
                IrisOpen01 = Mathf.Max(IrisOpen01, smooth);
                CoreGlow01 = Mathf.Max(CoreGlow01, smooth * 0.85f);
            }
            else if (_firing)
            {
                IrisOpen01 = Smoothing.Damp(IrisOpen01, 1f, 0.025f, deltaTime);
                CoreGlow01 = Smoothing.Damp(CoreGlow01, 1f, 0.025f, deltaTime);
            }
            else
            {
                IrisOpen01 = Smoothing.Damp(IrisOpen01, 0f, ReleaseHalfLife, deltaTime);
                CoreGlow01 = Smoothing.Damp(CoreGlow01, 0f, ReleaseHalfLife, deltaTime);
                if (IrisOpen01 < 1e-3f)
                {
                    IrisOpen01 = 0f;
                }

                if (CoreGlow01 < 1e-3f)
                {
                    CoreGlow01 = 0f;
                }
            }

            ApplyVisuals(time);
        }

        public void Reset()
        {
            _charging = false;
            _firing = false;
            _chargeElapsed = 0f;
            IrisOpen01 = 0f;
            CoreGlow01 = 0f;
            ApplyVisuals(0f);
        }

        public void Dispose()
        {
            for (int i = 0; i < _subscriptions.Length; i++)
            {
                _subscriptions[i].Dispose();
            }

            if (_ownsMaterial && _deviceMaterial != null)
            {
                Object.Destroy(_deviceMaterial);
            }
        }

        private void ApplyVisuals(float time)
        {
            float open = Mathf.Clamp01(IrisOpen01);
            float glow = Mathf.Clamp01(CoreGlow01);
            float radius = Mathf.Lerp(ClosedBladeRadius, OpenBladeRadius, open);
            float twist = open * OpenBladeTwistDeg;

            Color bladeRimEmission = WarmCoreColor.linear * (glow * 0.08f);
            for (int i = 0; i < BladeCount; i++)
            {
                float angleDeg = i * (360f / BladeCount) + twist;
                float rad = angleDeg * Mathf.Deg2Rad;
                Transform blade = _blades[i];
                if (blade != null)
                {
                    blade.localPosition = new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0.004f);
                    blade.localRotation = Quaternion.Euler(0f, 0f, angleDeg + 35f);
                }

                Renderer br = _bladeRenderers[i];
                if (br != null)
                {
                    _block.Clear();
                    _block.SetColor(BaseColorId, BladeColor);
                    _block.SetColor(ColorId, BladeColor);
                    _block.SetVector(EmissionColorId, bladeRimEmission);
                    _block.SetFloat(EmissionStrengthId, Mathf.Clamp01(glow * 0.12f));
                    br.SetPropertyBlock(_block);
                }
            }

            if (_coreTransform != null)
            {
                float diameter = Mathf.Lerp(CoreRestDiameter, CoreHotDiameter, glow);
                _coreTransform.localScale = new Vector3(diameter, diameter, CoreDepth);
            }

            if (_coreRenderer != null)
            {
                Color coreTint = Color.Lerp(WarmCoreColor, HotCoreColor, glow);
                float emissionGain = _firing ? 3.2f : 2.4f * glow;
                Color linearEmission = coreTint.linear * emissionGain;
                _block.Clear();
                _block.SetColor(BaseColorId, glow > 0.01f ? coreTint : BladeColor);
                _block.SetColor(ColorId, glow > 0.01f ? coreTint : BladeColor);
                _block.SetVector(EmissionColorId, linearEmission);
                _block.SetFloat(EmissionStrengthId, glow);
                _coreRenderer.SetPropertyBlock(_block);
            }

            if (_spillLight != null)
            {
                bool lit = glow > 0.01f || _charging || _firing;
                if (_spillLight.enabled != lit)
                {
                    _spillLight.enabled = lit;
                }

                if (lit)
                {
                    float baseIntensity = _firing
                        ? FireSpillIntensity
                        : ChargeSpillIntensity * Mathf.Max(0.2f, glow);
                    float flicker = _firing
                        ? 1f + (2f * Mathf.PerlinNoise(time * SpillFlickerRate, 0.37f) - 1f) * SpillFlickerAmplitude
                        : 1f;
                    _spillLight.intensity = baseIntensity * flicker;
                }
                else
                {
                    _spillLight.intensity = 0f;
                }
            }
        }

        private static void ConfigureRenderer(Renderer renderer, Material material)
        {
            if (renderer == null)
            {
                return;
            }

            if (material != null)
            {
                renderer.sharedMaterial = material;
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private void OnChargeStaged(CutChargeStaged staged)
        {
            if (staged.Stage == 0)
            {
                _charging = true;
                _firing = false;
                _chargeElapsed = 0f;
                _chargeDuration = Mathf.Max(0.12f, staged.StagesNeeded * staged.StageSeconds);
                IrisOpen01 = Mathf.Max(IrisOpen01, 0.15f);
                CoreGlow01 = Mathf.Max(CoreGlow01, 0.18f);
                ApplyVisuals(Time.time);
            }
            else
            {
                float progress = staged.StagesNeeded > 0
                    ? Mathf.Clamp01((float)staged.Stage / staged.StagesNeeded)
                    : 1f;
                IrisOpen01 = Mathf.Max(IrisOpen01, progress);
                CoreGlow01 = Mathf.Max(CoreGlow01, progress * 0.9f);
                ApplyVisuals(Time.time);
            }
        }

        private void OnFeelBeat(FeelBeat beat)
        {
            if (beat.Kind != FeelKind.Cut)
            {
                return;
            }

            switch (beat.Phase)
            {
                case FeelPhase.Anticipate:
                    _charging = true;
                    _firing = false;
                    _chargeElapsed = 0f;
                    _chargeDuration = Mathf.Max(0.12f, beat.Seconds);
                    IrisOpen01 = Mathf.Max(IrisOpen01, 0.15f);
                    CoreGlow01 = Mathf.Max(CoreGlow01, 0.18f);
                    ApplyVisuals(Time.time);
                    break;
                case FeelPhase.Act:
                    _charging = false;
                    _firing = true;
                    IrisOpen01 = 1f;
                    CoreGlow01 = 1f;
                    ApplyVisuals(Time.time);
                    break;
                case FeelPhase.Cancel:
                case FeelPhase.Impact:
                    _charging = false;
                    _firing = false;
                    break;
            }
        }

        private void OnWeldStateChanged(BuildWeldStateChanged weld)
        {
            if (weld.Welding)
            {
                _charging = false;
                _firing = true;
                IrisOpen01 = 1f;
                CoreGlow01 = 1f;
                ApplyVisuals(Time.time);
            }
            else
            {
                _charging = false;
                _firing = false;
            }
        }
    }
}
