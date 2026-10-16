using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Thin runtime view component on each generated structure prefab (<c>Generated/Gameplay/Structures/</c>, 0.5.7
    /// slice 6, BUILD-MODE). Caches the <c>Stage_1..N</c> renderer groups, their authored <c>R1_*</c> material arrays,
    /// any <c>R1_LampAmber</c> standby lamp renderers (driven via <see cref="EmissionGlow"/>), and the structure's
    /// <c>Layers.Prop</c> colliders. Allocation-free after <see cref="Initialize"/>.
    /// </summary>
    public sealed class StructureView : MonoBehaviour
    {
        private const float StagePopStartScale = 0.91f;
        private const float StageScaleHalfLife = 0.065f;
        private const float LampWarmHalfLife = 0.14f;
        private const float CompletionLampFlash = 1.45f;

        [Tooltip("Stable structure definition id (e.g. charging_pad, solar_array, power_pylon).")]
        [SerializeField] private string _structureId = string.Empty;

        [Tooltip("Ordered Stage_1..Stage_N root transforms for stage-by-stage assembly.")]
        [SerializeField] private Transform[] _stageRoots = Array.Empty<Transform>();

        [Tooltip("Renderers using R1_LampAmber that warm up via EmissionGlow when the structure completes.")]
        [SerializeField] private Renderer[] _lampRenderers = Array.Empty<Renderer>();

        [Tooltip("Solid colliders on Layers.Prop (disabled while this instance is a placement ghost).")]
        [SerializeField] private Collider[] _colliders = Array.Empty<Collider>();

        [Tooltip("Optional warm amber point light enabled when the structure is completely built.")]
        [SerializeField] private Light _statusLight;

        [Tooltip("Peak intensity for the optional warm amber point light.")]
        [SerializeField] private float _statusLightIntensity = 1.6f;

        private StageRendererCache[][] _stageRenderers;
        private bool[] _stageIsSolid;
        private float[] _stageScale;
        private Vector3[] _stageBaseScale;
        private EmissionGlow[] _lampGlows;
        private Color[] _lampBaseColors;
        private bool _initialized;
        private float _buildProgress01;
        private float _deconstructProgress01;
        private float _targetLampIntensity;

        public string StructureId => _structureId;

        public int StageCount => _stageRoots != null ? _stageRoots.Length : 0;

        public IReadOnlyList<Transform> StageRoots => _stageRoots;

        public int SolidStageCount { get; private set; }

        public bool IsGhost { get; private set; }

        public bool IsGhostValid { get; private set; }

        public float LampIntensity { get; private set; }

        public bool LampsLit => LampIntensity > 0.01f;

        public IReadOnlyList<Collider> Colliders => _colliders;

        /// <summary>Populates serialized references at build time (<c>GameplayContentBuilder</c>).</summary>
        public void Populate(
            Transform[] stageRoots,
            Renderer[] lampRenderers,
            Collider[] colliders,
            Light statusLight = null,
            float statusLightIntensity = 1.6f)
        {
            Populate(string.Empty, stageRoots, lampRenderers, colliders, statusLight, statusLightIntensity);
        }

        /// <summary>Populates serialized references at build time (<c>GameplayContentBuilder</c>).</summary>
        public void Populate(
            string structureId,
            Transform[] stageRoots,
            Renderer[] lampRenderers,
            Collider[] colliders,
            Light statusLight = null,
            float statusLightIntensity = 1.6f)
        {
            _structureId = structureId ?? string.Empty;
            _stageRoots = stageRoots ?? Array.Empty<Transform>();
            _lampRenderers = lampRenderers ?? Array.Empty<Renderer>();
            _colliders = colliders ?? Array.Empty<Collider>();
            _statusLight = statusLight;
            _statusLightIntensity = statusLightIntensity;
        }

        /// <summary>
        /// Pre-allocates material swap arrays for every renderer in <c>Stage_1..N</c> and <see cref="EmissionGlow"/>
        /// wrappers for standby lamps so runtime updates never allocate. Requires <c>_stageRoots</c> to be wired at
        /// build time; logs an error and returns <c>false</c> when missing or empty.
        /// </summary>
        public bool Initialize(Material hologramValid, Material hologramInvalid)
        {
            if (hologramValid == null)
            {
                Debug.LogError($"{nameof(StructureView)} ({name}): hologramValid material is not assigned.", this);
                enabled = false;
                return false;
            }

            if (hologramInvalid == null)
            {
                Debug.LogError($"{nameof(StructureView)} ({name}): hologramInvalid material is not assigned.", this);
                enabled = false;
                return false;
            }

            if (_stageRoots == null || _stageRoots.Length == 0)
            {
                Debug.LogError(
                    $"{nameof(StructureView)} ({name}): _stageRoots is missing or empty (run Gameplay/Content).",
                    this);
                enabled = false;
                return false;
            }

            int stageCount = _stageRoots.Length;
            for (int s = 0; s < stageCount; s++)
            {
                if (_stageRoots[s] == null)
                {
                    Debug.LogError(
                        $"{nameof(StructureView)} ({name}): _stageRoots[{s}] is null (run Gameplay/Content).",
                        this);
                    enabled = false;
                    return false;
                }
            }

            _stageRenderers = new StageRendererCache[stageCount][];
            _stageIsSolid = new bool[stageCount];
            _stageScale = new float[stageCount];
            _stageBaseScale = new Vector3[stageCount];

            for (int s = 0; s < stageCount; s++)
            {
                Transform stageRoot = _stageRoots[s];
                _stageScale[s] = 1f;
                _stageBaseScale[s] = stageRoot.localScale;

                Renderer[] renderers = stageRoot.GetComponentsInChildren<Renderer>(true);
                var caches = new StageRendererCache[renderers.Length];
                for (int r = 0; r < renderers.Length; r++)
                {
                    Renderer renderer = renderers[r];
                    Material[] solidMats = renderer.sharedMaterials;
                    int slotCount = solidMats != null && solidMats.Length > 0 ? solidMats.Length : 1;
                    var validMats = new Material[slotCount];
                    var invalidMats = new Material[slotCount];
                    for (int m = 0; m < slotCount; m++)
                    {
                        validMats[m] = hologramValid;
                        invalidMats[m] = hologramInvalid;
                    }

                    caches[r] = new StageRendererCache(renderer, solidMats, validMats, invalidMats);
                }

                _stageRenderers[s] = caches;
                _stageIsSolid[s] = true;
            }

            int lampCount = _lampRenderers != null ? _lampRenderers.Length : 0;
            _lampGlows = new EmissionGlow[lampCount];
            _lampBaseColors = new Color[lampCount];
            for (int i = 0; i < lampCount; i++)
            {
                Renderer lamp = _lampRenderers[i];
                if (lamp == null)
                {
                    continue;
                }

                _lampGlows[i] = new EmissionGlow(lamp);
                Material mat = lamp.sharedMaterial;
                Color authored = mat != null && mat.HasProperty("_EmissionColor")
                    ? mat.GetColor("_EmissionColor").linear
                    : new Color(1f, 0.68f, 0.25f, 1f);
                _lampBaseColors[i] = authored;
            }

            _initialized = true;
            SolidStageCount = stageCount;
            return true;
        }

        /// <summary>
        /// Configures this instance as the placement ghost (colliders disabled, all stages rendered with the valid or
        /// invalid hologram material).
        /// </summary>
        public void SetGhostMode(bool valid)
        {
            if (!_initialized)
            {
                return;
            }

            SetCollidersEnabled(false);
            if (IsGhost && IsGhostValid == valid && SolidStageCount == 0)
            {
                return;
            }

            IsGhost = true;
            IsGhostValid = valid;
            SolidStageCount = 0;
            _targetLampIntensity = 0f;
            LampIntensity = 0f;
            if (_statusLight != null)
            {
                _statusLight.enabled = false;
            }

            int stageCount = _stageRenderers.Length;
            for (int s = 0; s < stageCount; s++)
            {
                _stageIsSolid[s] = false;
                _stageScale[s] = 1f;
                if (_stageRoots[s] != null)
                {
                    _stageRoots[s].gameObject.SetActive(true);
                    _stageRoots[s].localScale = _stageBaseScale[s];
                }

                StageRendererCache[] caches = _stageRenderers[s];
                for (int r = 0; r < caches.Length; r++)
                {
                    StageRendererCache cache = caches[r];
                    if (cache.Renderer != null)
                    {
                        cache.Renderer.sharedMaterials = valid ? cache.ValidHologramMaterials : cache.InvalidHologramMaterials;
                    }
                }
            }
        }

        /// <summary>Sets build progress immediately for tests and restore flows.</summary>
        public void SetBuildProgress(float progress01, bool isBuilt)
        {
            SetPlacedState(isBuilt ? 1f : progress01, instant: true);
        }

        /// <summary>
        /// Configures this instance as a placed world structure at <paramref name="buildProgress01"/> (0..1). Stages
        /// <c>0..K-1</c> use their solid <c>R1_*</c> materials, stage <c>K</c> shows the hologram outline of the next
        /// stage being welded, and stages <c>&gt;K</c> stay hidden until reached.
        /// </summary>
        public int SetPlacedState(float buildProgress01, bool instant)
        {
            if (!_initialized)
            {
                return 0;
            }

            SetCollidersEnabled(true);
            bool wasGhost = IsGhost;
            IsGhost = false;
            IsGhostValid = true;

            _buildProgress01 = Mathf.Clamp01(buildProgress01);
            _deconstructProgress01 = 0f;

            int stageCount = _stageRenderers.Length;
            int targetSolid = ComputeSolidStages(_buildProgress01, stageCount);
            int newlySolidCount = 0;

            for (int s = 0; s < stageCount; s++)
            {
                bool shouldBeVisible = s <= targetSolid;
                if (_stageRoots[s] != null && _stageRoots[s].gameObject.activeSelf != shouldBeVisible)
                {
                    _stageRoots[s].gameObject.SetActive(shouldBeVisible);
                }

                bool shouldBeSolid = s < targetSolid;
                if (wasGhost || _stageIsSolid[s] != shouldBeSolid)
                {
                    bool poppedNow = !wasGhost && !_stageIsSolid[s] && shouldBeSolid && !instant;
                    _stageIsSolid[s] = shouldBeSolid;
                    ApplyStageMaterials(s, shouldBeSolid);

                    if (poppedNow)
                    {
                        _stageScale[s] = StagePopStartScale;
                        if (_stageRoots[s] != null)
                        {
                            _stageRoots[s].localScale = _stageBaseScale[s] * StagePopStartScale;
                        }

                        newlySolidCount++;
                    }
                    else if (instant)
                    {
                        _stageScale[s] = 1f;
                        if (_stageRoots[s] != null)
                        {
                            _stageRoots[s].localScale = _stageBaseScale[s];
                        }
                    }
                }
            }

            bool wasBuilt = SolidStageCount == stageCount && !wasGhost;
            SolidStageCount = targetSolid;
            bool isBuilt = targetSolid == stageCount;

            if (isBuilt)
            {
                _targetLampIntensity = 1f;
                if (instant)
                {
                    LampIntensity = 1f;
                    ApplyLampGlow(1f);
                }
                else if (!wasBuilt)
                {
                    LampIntensity = CompletionLampFlash;
                    ApplyLampGlow(CompletionLampFlash);
                }
            }
            else
            {
                _targetLampIntensity = 0f;
                LampIntensity = 0f;
                if (_statusLight != null)
                {
                    _statusLight.enabled = false;
                }
            }

            return newlySolidCount;
        }

        /// <summary>
        /// Visualizes hold-to-deconstruct progress (0..1) by folding solid stages back into hologram from top to
        /// bottom. Passing 0 restores the current <see cref="_buildProgress01"/> state.
        /// </summary>
        public void SetDeconstructProgress(float deconstruct01)
        {
            if (!_initialized || IsGhost)
            {
                return;
            }

            float clamped = Mathf.Clamp01(deconstruct01);
            if (Mathf.Abs(clamped - _deconstructProgress01) < 1e-4f)
            {
                return;
            }

            _deconstructProgress01 = clamped;
            int stageCount = _stageRenderers.Length;
            int baseSolid = ComputeSolidStages(_buildProgress01, stageCount);
            int remainingSolid = clamped <= 0f
                ? baseSolid
                : Mathf.Clamp(Mathf.CeilToInt(baseSolid * (1f - clamped)), 0, baseSolid);

            float foldScale = Mathf.Lerp(1f, 0.93f, clamped);
            for (int s = 0; s < stageCount; s++)
            {
                bool shouldBeSolid = s < remainingSolid;
                if (_stageIsSolid[s] != shouldBeSolid)
                {
                    _stageIsSolid[s] = shouldBeSolid;
                    ApplyStageMaterials(s, shouldBeSolid);
                }

                if (_stageRoots[s] != null)
                {
                    _stageRoots[s].localScale = _stageBaseScale[s] * foldScale;
                }
            }

            SolidStageCount = remainingSolid;
        }

        /// <summary>Steps per-stage scale settle eases and standby lamp <see cref="EmissionGlow"/>.</summary>
        public void StepVisuals(float deltaTime)
        {
            if (!_initialized || IsGhost || deltaTime <= 0f)
            {
                return;
            }

            if (_deconstructProgress01 <= 0f)
            {
                int stageCount = _stageRoots.Length;
                for (int s = 0; s < stageCount; s++)
                {
                    if (Mathf.Abs(_stageScale[s] - 1f) > 1e-4f)
                    {
                        _stageScale[s] = Damp.Toward(_stageScale[s], 1f, StageScaleHalfLife, deltaTime);
                        if (_stageRoots[s] != null)
                        {
                            _stageRoots[s].localScale = _stageBaseScale[s] * _stageScale[s];
                        }
                    }
                }
            }

            if (SolidStageCount == _stageRoots.Length && (_lampGlows.Length > 0 || _statusLight != null))
            {
                LampIntensity = Damp.Toward(LampIntensity, _targetLampIntensity, LampWarmHalfLife, deltaTime);
                ApplyLampGlow(LampIntensity);
            }
        }

        /// <summary>True when stage <paramref name="stageIndex"/> (0-based) is currently rendered with its solid R1 material.</summary>
        public bool IsStageSolid(int stageIndex)
        {
            if (!_initialized || stageIndex < 0 || stageIndex >= _stageIsSolid.Length)
            {
                return false;
            }

            return _stageIsSolid[stageIndex];
        }

        /// <summary>World-space focus point for 07's weld beam on the active stage.</summary>
        public Vector3 GetActiveWeldPoint()
        {
            if (_stageRoots == null || _stageRoots.Length == 0)
            {
                return transform.position + Vector3.up * 0.6f;
            }

            int activeIndex = Mathf.Clamp(SolidStageCount, 0, _stageRoots.Length - 1);
            if (_stageRenderers != null && activeIndex < _stageRenderers.Length)
            {
                StageRendererCache[] caches = _stageRenderers[activeIndex];
                if (caches != null && caches.Length > 0 && caches[0].Renderer != null)
                {
                    return caches[0].Renderer.bounds.center;
                }
            }

            Transform stage = _stageRoots[activeIndex];
            if (stage != null)
            {
                return stage.position + Vector3.up * (0.3f + activeIndex * 0.25f);
            }

            return transform.position + Vector3.up * 0.6f;
        }

        public static int ComputeSolidStages(float progress01, int stageCount)
        {
            if (stageCount <= 0)
            {
                return 0;
            }

            if (progress01 >= 1f - 1e-4f)
            {
                return stageCount;
            }

            return Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(progress01) * stageCount + 1e-5f), 0, stageCount);
        }

        private void ApplyStageMaterials(int stageIndex, bool solid)
        {
            StageRendererCache[] caches = _stageRenderers[stageIndex];
            for (int r = 0; r < caches.Length; r++)
            {
                StageRendererCache cache = caches[r];
                if (cache.Renderer != null)
                {
                    cache.Renderer.sharedMaterials = solid ? cache.SolidMaterials : cache.ValidHologramMaterials;
                }
            }
        }

        private void ApplyLampGlow(float intensity)
        {
            for (int i = 0; i < _lampGlows.Length; i++)
            {
                EmissionGlow glow = _lampGlows[i];
                if (glow != null)
                {
                    glow.Apply(_lampBaseColors[i], intensity);
                }
            }

            if (_statusLight != null)
            {
                bool lit = !IsGhost && intensity > 0.01f;
                if (_statusLight.enabled != lit)
                {
                    _statusLight.enabled = lit;
                }

                if (lit)
                {
                    _statusLight.intensity = _statusLightIntensity * intensity;
                }
            }
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (_colliders == null)
            {
                return;
            }

            for (int i = 0; i < _colliders.Length; i++)
            {
                Collider col = _colliders[i];
                if (col != null && col.enabled != enabled)
                {
                    col.enabled = enabled;
                }
            }
        }

        private readonly struct StageRendererCache
        {
            public StageRendererCache(
                Renderer renderer,
                Material[] solidMaterials,
                Material[] validHologramMaterials,
                Material[] invalidHologramMaterials)
            {
                Renderer = renderer;
                SolidMaterials = solidMaterials;
                ValidHologramMaterials = validHologramMaterials;
                InvalidHologramMaterials = invalidHologramMaterials;
            }

            public Renderer Renderer { get; }

            public Material[] SolidMaterials { get; }

            public Material[] ValidHologramMaterials { get; }

            public Material[] InvalidHologramMaterials { get; }
        }
    }
}
