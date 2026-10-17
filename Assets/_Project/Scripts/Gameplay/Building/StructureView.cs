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

        [Tooltip("Per-stage local-space weld seams resolved from each stage's WeldSeam child or renderer/mesh bounds.")]
        [SerializeField] private StageWeldSeam[] _stageSeams = Array.Empty<StageWeldSeam>();

        [Tooltip("Renderers using R1_LampAmber that warm up via EmissionGlow when the structure completes.")]
        [SerializeField] private Renderer[] _lampRenderers = Array.Empty<Renderer>();

        [Tooltip("Solid colliders on Layers.Prop (disabled while this instance is a placement ghost).")]
        [SerializeField] private Collider[] _colliders = Array.Empty<Collider>();

        [Tooltip("Optional warm amber point light enabled when the structure is completely built.")]
        [SerializeField] private Light _statusLight;

        [Tooltip("Peak intensity for the optional warm amber point light.")]
        [SerializeField] private float _statusLightIntensity = 1.6f;

        private readonly Vector3[] _activeSeamPoints = new Vector3[3];
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

        public IReadOnlyList<StageWeldSeam> StageSeams => _stageSeams;

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
            Populate(string.Empty, stageRoots, lampRenderers, colliders, statusLight, statusLightIntensity, null);
        }

        /// <summary>Populates serialized references at build time (<c>GameplayContentBuilder</c>).</summary>
        public void Populate(
            string structureId,
            Transform[] stageRoots,
            Renderer[] lampRenderers,
            Collider[] colliders,
            Light statusLight = null,
            float statusLightIntensity = 1.6f,
            StageWeldSeam[] stageSeams = null)
        {
            _structureId = structureId ?? string.Empty;
            _stageRoots = stageRoots ?? Array.Empty<Transform>();
            _lampRenderers = lampRenderers ?? Array.Empty<Renderer>();
            _colliders = colliders ?? Array.Empty<Collider>();
            _statusLight = statusLight;
            _statusLightIntensity = statusLightIntensity;
            _stageSeams = stageSeams != null && stageSeams.Length == _stageRoots.Length
                ? stageSeams
                : BuildStageSeams(_stageRoots, transform);
        }

        /// <summary>
        /// Pre-allocates material swap arrays for every renderer in <c>Stage_1..N</c> and <see cref="EmissionGlow"/>
        /// wrappers for standby lamps so runtime updates never allocate. Requires <c>_stageRoots</c> and stage
        /// renderers to be wired at build time; logs an error and returns <c>false</c> when missing or empty.
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

            if (_stageSeams == null || _stageSeams.Length != stageCount)
            {
                _stageSeams = new StageWeldSeam[stageCount];
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
                if (renderers == null || renderers.Length == 0)
                {
                    Debug.LogError(
                        $"{nameof(StructureView)} ({name}): _stageRoots[{s}] ('{stageRoot.name}') has no Renderers.",
                        this);
                    enabled = false;
                    return false;
                }

                var caches = new StageRendererCache[renderers.Length];
                for (int r = 0; r < renderers.Length; r++)
                {
                    Renderer renderer = renderers[r];
                    if (renderer == null)
                    {
                        Debug.LogError(
                            $"{nameof(StructureView)} ({name}): _stageRoots[{s}] ('{stageRoot.name}') has a null Renderer at index {r}.",
                            this);
                        enabled = false;
                        return false;
                    }

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

                if (!_stageSeams[s].IsValid && !TryResolveStageSeam(stageRoot, transform, out _stageSeams[s]))
                {
                    Debug.LogError(
                        $"{nameof(StructureView)} ({name}): failed to resolve weld seam for _stageRoots[{s}] ('{stageRoot.name}').",
                        this);
                    enabled = false;
                    return false;
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
                    : Color.white;
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

        /// <summary>Progress (0..1) within the currently active weld stage.</summary>
        public float GetActiveStageProgress01(float buildProgress01)
        {
            int count = StageCount;
            if (count <= 0)
            {
                return Mathf.Clamp01(buildProgress01);
            }

            int solid = ComputeSolidStages(buildProgress01, count);
            if (solid >= count)
            {
                return 1f;
            }

            return Mathf.Clamp01(buildProgress01 * count - solid);
        }

        /// <summary>
        /// Populates <paramref name="localPoints"/> (length 3) with a local-space weld seam across the active stage
        /// so <see cref="HeatSeam"/> and the weld beam sweep progressively across the stage as it welds.
        /// </summary>
        public void GetStageWeldSeam(int stageIndex, Vector3[] localPoints, out Vector3 localNormal)
        {
            if (localPoints == null || localPoints.Length < 3)
            {
                throw new ArgumentException("Seam point buffer must have at least 3 elements.", nameof(localPoints));
            }

            StageWeldSeam seam = ResolveStageSeam(stageIndex);
            localPoints[0] = seam.Start;
            localPoints[1] = seam.Middle;
            localPoints[2] = seam.End;
            localNormal = seam.Normal;
        }

        /// <summary>World-space focus point for 07's weld beam on the active stage, progressing along its seam.</summary>
        public Vector3 GetActiveWeldPoint()
        {
            GetStageWeldSeam(SolidStageCount, _activeSeamPoints, out _);
            float stageT = GetActiveStageProgress01(_buildProgress01);
            Vector3 local = stageT <= 0.5f
                ? Vector3.Lerp(_activeSeamPoints[0], _activeSeamPoints[1], stageT * 2f)
                : Vector3.Lerp(_activeSeamPoints[1], _activeSeamPoints[2], (stageT - 0.5f) * 2f);
            return transform.TransformPoint(local);
        }

        /// <summary>
        /// Resolves local-space <see cref="StageWeldSeam"/> entries for every stage root in <paramref name="stageRoots"/>
        /// relative to <paramref name="structureRoot"/>.
        /// </summary>
        public static StageWeldSeam[] BuildStageSeams(Transform[] stageRoots, Transform structureRoot)
        {
            if (stageRoots == null || stageRoots.Length == 0 || structureRoot == null)
            {
                return Array.Empty<StageWeldSeam>();
            }

            var seams = new StageWeldSeam[stageRoots.Length];
            for (int i = 0; i < stageRoots.Length; i++)
            {
                TryResolveStageSeam(stageRoots[i], structureRoot, out seams[i]);
            }

            return seams;
        }

        /// <summary>
        /// Resolves the local-space weld seam for <paramref name="stageRoot"/> from an authored <c>WeldSeam</c> child
        /// if present, or from the stage's renderer/mesh bounds relative to <paramref name="structureRoot"/>.
        /// </summary>
        public static bool TryResolveStageSeam(
            Transform stageRoot,
            Transform structureRoot,
            out StageWeldSeam seam)
        {
            seam = default;
            if (stageRoot == null || structureRoot == null)
            {
                return false;
            }

            Transform weldSeam = stageRoot.Find("WeldSeam");
            if (weldSeam != null)
            {
                Vector3 normal = structureRoot.InverseTransformDirection(weldSeam.up);
                normal = normal.sqrMagnitude > 1e-6f ? normal.normalized : Vector3.up;
                if (weldSeam.childCount >= 3)
                {
                    Vector3 p0 = structureRoot.InverseTransformPoint(weldSeam.GetChild(0).position);
                    Vector3 p1 = structureRoot.InverseTransformPoint(weldSeam.GetChild(1).position);
                    Vector3 p2 = structureRoot.InverseTransformPoint(weldSeam.GetChild(2).position);
                    seam = new StageWeldSeam(p0, p1, p2, normal);
                    return seam.IsValid;
                }

                if (weldSeam.childCount == 2)
                {
                    Vector3 p0 = structureRoot.InverseTransformPoint(weldSeam.GetChild(0).position);
                    Vector3 p2 = structureRoot.InverseTransformPoint(weldSeam.GetChild(1).position);
                    Vector3 p1 = Vector3.Lerp(p0, p2, 0.5f);
                    seam = new StageWeldSeam(p0, p1, p2, normal);
                    return seam.IsValid;
                }

                Vector3 seamCenter = structureRoot.InverseTransformPoint(weldSeam.position);
                Vector3 span = structureRoot.InverseTransformVector(weldSeam.right * weldSeam.lossyScale.x);
                if (span.sqrMagnitude <= 1e-6f)
                {
                    span = Vector3.right;
                }

                seam = new StageWeldSeam(seamCenter - span, seamCenter, seamCenter + span, normal);
                return seam.IsValid;
            }

            Renderer[] renderers = stageRoot.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return false;
            }

            bool hasBounds = false;
            Bounds stageBounds = default;
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                if (renderer == null)
                {
                    continue;
                }

                if (renderer.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                {
                    Bounds meshBounds = filter.sharedMesh.bounds;
                    EncapsulateCorners(
                        meshBounds,
                        renderer.transform,
                        structureRoot,
                        localSpaceInput: true,
                        ref stageBounds,
                        ref hasBounds);
                }
                else
                {
                    Bounds worldBounds = renderer.bounds;
                    EncapsulateCorners(
                        worldBounds,
                        renderer.transform,
                        structureRoot,
                        localSpaceInput: false,
                        ref stageBounds,
                        ref hasBounds);
                }
            }

            if (!hasBounds || stageBounds.size.sqrMagnitude <= 1e-6f)
            {
                return false;
            }

            Vector3 min = stageBounds.min;
            Vector3 center = stageBounds.center;
            Vector3 max = stageBounds.max;
            seam = new StageWeldSeam(
                new Vector3(min.x, center.y, center.z),
                new Vector3(center.x, max.y, max.z),
                new Vector3(max.x, center.y, center.z),
                Vector3.up);
            return seam.IsValid;
        }

        private StageWeldSeam ResolveStageSeam(int stageIndex)
        {
            if (_stageRoots == null || _stageRoots.Length == 0)
            {
                Debug.LogError(
                    $"{nameof(StructureView)} ({name}): cannot resolve weld seam because _stageRoots is missing or empty.",
                    this);
                return default;
            }

            int idx = Mathf.Clamp(stageIndex, 0, _stageRoots.Length - 1);
            if (_stageSeams != null && idx < _stageSeams.Length && _stageSeams[idx].IsValid)
            {
                return _stageSeams[idx];
            }

            if (TryResolveStageSeam(_stageRoots[idx], transform, out StageWeldSeam resolved))
            {
                if (_stageSeams != null && idx < _stageSeams.Length)
                {
                    _stageSeams[idx] = resolved;
                }

                return resolved;
            }

            Debug.LogError(
                $"{nameof(StructureView)} ({name}): failed to resolve weld seam for stage index {idx}.",
                this);
            return default;
        }

        private static void EncapsulateCorners(
            Bounds sourceBounds,
            Transform rendererTransform,
            Transform structureRoot,
            bool localSpaceInput,
            ref Bounds targetBounds,
            ref bool hasBounds)
        {
            Vector3 min = sourceBounds.min;
            Vector3 max = sourceBounds.max;
            for (int corner = 0; corner < 8; corner++)
            {
                var pt = new Vector3(
                    (corner & 1) == 0 ? min.x : max.x,
                    (corner & 2) == 0 ? min.y : max.y,
                    (corner & 4) == 0 ? min.z : max.z);
                Vector3 world = localSpaceInput ? rendererTransform.TransformPoint(pt) : pt;
                Vector3 local = structureRoot.InverseTransformPoint(world);
                if (!hasBounds)
                {
                    targetBounds = new Bounds(local, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    targetBounds.Encapsulate(local);
                }
            }
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

        [Serializable]
        public struct StageWeldSeam
        {
            public Vector3 Start;
            public Vector3 Middle;
            public Vector3 End;
            public Vector3 Normal;

            public StageWeldSeam(Vector3 start, Vector3 middle, Vector3 end, Vector3 normal)
            {
                Start = start;
                Middle = middle;
                End = end;
                Normal = normal;
            }

            public bool IsValid =>
                (Middle - Start).sqrMagnitude > 1e-6f ||
                (End - Middle).sqrMagnitude > 1e-6f ||
                (End - Start).sqrMagnitude > 1e-6f;
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
