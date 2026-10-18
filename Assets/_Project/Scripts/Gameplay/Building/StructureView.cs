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
        private const float StageHeatHalfLife = 0.52f;
        private const float SettleDuration = 0.44f;

        /// <summary>The invalid placement ghost's hologram fade (the valid one is whole).</summary>
        private const float InvalidGhostFade = 0.95f;
        private const float LampCascadeStep = 0.065f;

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
        private Collider[][] _stageColliders;
        private bool[] _stageIsSolid;
        private float[] _stageScale;
        private float[] _stageHeat;
        private Vector3[] _stageBaseLocalPos;
        private Quaternion[] _stageBaseLocalRot;
        private Vector3[] _stageBaseScale;
        private Bounds[] _stageLocalBounds;
        private EmissionGlow[] _lampGlows;
        private Color[] _lampBaseColors;
        private MaterialPropertyBlock _mpb;

        private int _propBuildPrint;
        private int _propBuildRevealProgress;
        private int _propBuildStageHeat;
        private int _propBuildBoundsY;
        private int _propBuildSeamPointWS;
        private int _propBlueprintMode;
        private int _propStageFade;

        private Transform _printLayerRoot;
        private Transform _printLayerBar;
        private Transform _printLayerBead;
        private Renderer[] _printLayerRenderers;
        private Material _hologramInvalidMat;

        private bool _liveBuild;
        private float _settleThumpMeters = 0.042f;
        private float _settleElapsed = -1f;
        private float _lampCascadeElapsed = -1f;
        private float _localMinY;
        private float _localMaxY = 1.55f;

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

            _hologramInvalidMat = hologramInvalid;
            _mpb = new MaterialPropertyBlock();
            _propBuildPrint = Shader.PropertyToID("_BuildPrint");
            _propBuildRevealProgress = Shader.PropertyToID("_BuildRevealProgress");
            _propBuildStageHeat = Shader.PropertyToID("_BuildStageHeat");
            _propBuildBoundsY = Shader.PropertyToID("_BuildBoundsY");
            _propBuildSeamPointWS = Shader.PropertyToID("_BuildSeamPointWS");
            _propBlueprintMode = Shader.PropertyToID("_BlueprintMode");
            _propStageFade = Shader.PropertyToID("_StageFade");

            _stageRenderers = new StageRendererCache[stageCount][];
            _stageColliders = new Collider[stageCount][];
            _stageIsSolid = new bool[stageCount];
            _stageScale = new float[stageCount];
            _stageHeat = new float[stageCount];
            _stageBaseLocalPos = new Vector3[stageCount];
            _stageBaseLocalRot = new Quaternion[stageCount];
            _stageBaseScale = new Vector3[stageCount];
            _stageLocalBounds = new Bounds[stageCount];
            _stageSeams = BuildStageSeams(_stageRoots, transform);

            bool hasAnyBounds = false;
            Bounds fullLocalBounds = default;

            for (int s = 0; s < stageCount; s++)
            {
                Transform stageRoot = _stageRoots[s];
                _stageScale[s] = 1f;
                _stageHeat[s] = 0f;
                _stageBaseLocalPos[s] = stageRoot.localPosition;
                _stageBaseLocalRot[s] = stageRoot.localRotation;
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

                bool hasStageBounds = false;
                Bounds stageBounds = default;
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

                    if (renderer.TryGetComponent(out MeshFilter mf) && mf.sharedMesh != null)
                    {
                        EncapsulateCorners(mf.sharedMesh.bounds, renderer.transform, transform, true, ref stageBounds, ref hasStageBounds);
                        EncapsulateCorners(mf.sharedMesh.bounds, renderer.transform, transform, true, ref fullLocalBounds, ref hasAnyBounds);
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

                _stageLocalBounds[s] = stageBounds;

                if (stageRoot.Find("WeldSeam") == null || !_stageSeams[s].IsValid)
                {
                    if (!TryResolveStageSeam(stageRoot, transform, out _stageSeams[s]))
                    {
                        Debug.LogError(
                            $"{nameof(StructureView)} ({name}): failed to resolve weld seam for _stageRoots[{s}] ('{stageRoot.name}').",
                            this);
                        enabled = false;
                        return false;
                    }
                }

                _stageRenderers[s] = caches;
                _stageColliders[s] = CollidersUnder(stageRoot);
                _stageIsSolid[s] = true;
            }

            if (hasAnyBounds)
            {
                _localMinY = fullLocalBounds.min.y;
                _localMaxY = Mathf.Max(fullLocalBounds.max.y, _localMinY + 0.2f);
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
        /// A placed structure 07 builds in play (<paramref name="liveBuild"/>) is printed in place: it rises layer by
        /// layer under a bright layer line the weld beam rides, and settles with a thump of
        /// <paramref name="settleThumpMeters"/>. A restored one simply stands.
        /// </summary>
        public void ConfigureLiveBuild(bool liveBuild, float settleThumpMeters)
        {
            _liveBuild = liveBuild;
            _settleThumpMeters = settleThumpMeters;
            if (_initialized && !IsGhost && _liveBuild)
            {
                RefreshPrint();
            }
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
            if (_printLayerRoot != null)
            {
                _printLayerRoot.gameObject.SetActive(false);
            }

            if (IsGhost && IsGhostValid == valid && SolidStageCount == 0)
            {
                ApplyGhostProperties(valid);
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
                _stageHeat[s] = 0f;
                ResetStagePoseToBase(s);
                if (_stageRoots[s] != null)
                {
                    _stageRoots[s].gameObject.SetActive(true);
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

            ApplyGhostProperties(valid);
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
                        _stageHeat[s] = 1f;
                        if (_stageRoots[s] != null)
                        {
                            _stageRoots[s].localScale = _stageBaseScale[s] * StagePopStartScale;
                        }

                        newlySolidCount++;
                    }
                    else if (instant)
                    {
                        _stageScale[s] = 1f;
                        _stageHeat[s] = 0f;
                        ResetStagePoseToBase(s);
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
                    _settleElapsed = -1f;
                    _lampCascadeElapsed = -1f;
                    LampIntensity = 1f;
                    ApplyLampGlow(1f);
                    for (int s = 0; s < stageCount; s++)
                    {
                        _stageHeat[s] = 0f;
                        ResetStagePoseToBase(s);
                        ClearStagePropertyBlock(s);
                    }

                    if (_printLayerRoot != null)
                    {
                        _printLayerRoot.gameObject.SetActive(false);
                    }
                }
                else if (!wasBuilt)
                {
                    _settleElapsed = 0f;
                    _lampCascadeElapsed = 0f;
                    LampIntensity = CompletionLampFlash;
                    ApplyLampGlow(CompletionLampFlash);
                }
            }
            else
            {
                _targetLampIntensity = 0f;
                LampIntensity = 0f;
                _lampCascadeElapsed = -1f;
                if (_statusLight != null)
                {
                    _statusLight.enabled = false;
                }
            }

            if (_liveBuild && !instant)
            {
                RefreshPrint();
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

        /// <summary>Steps per-stage scale settle eases, variant unfolding/thermal visuals, and standby lamp <see cref="EmissionGlow"/>.</summary>
        public void StepVisuals(float deltaTime)
        {
            if (!_initialized || IsGhost || deltaTime <= 0f)
            {
                return;
            }

            int stageCount = _stageRoots.Length;
            if (_deconstructProgress01 <= 0f)
            {
                for (int s = 0; s < stageCount; s++)
                {
                    if (Mathf.Abs(_stageScale[s] - 1f) > 1e-4f)
                    {
                        _stageScale[s] = Damp.Toward(_stageScale[s], 1f, StageScaleHalfLife, deltaTime);
                        if (!_liveBuild && _stageRoots[s] != null)
                        {
                            _stageRoots[s].localScale = _stageBaseScale[s] * _stageScale[s];
                        }
                    }

                    if (s < SolidStageCount && _stageHeat[s] > 1e-3f)
                    {
                        _stageHeat[s] = Damp.Toward(_stageHeat[s], 0f, StageHeatHalfLife, deltaTime);
                    }
                }
            }

            if (_settleElapsed >= 0f)
            {
                _settleElapsed += deltaTime;
                if (_settleElapsed >= SettleDuration)
                {
                    _settleElapsed = -1f;
                }
            }

            if (_liveBuild && _deconstructProgress01 <= 0f)
            {
                RefreshPrint();
            }

            if (SolidStageCount == stageCount && (_lampGlows.Length > 0 || _statusLight != null))
            {
                LampIntensity = Damp.Toward(LampIntensity, _targetLampIntensity, LampWarmHalfLife, deltaTime);
                if (_lampCascadeElapsed >= 0f)
                {
                    _lampCascadeElapsed += deltaTime;
                    ApplySequentialLampBlink(_lampCascadeElapsed, LampIntensity);
                    float totalCascade = _lampGlows.Length * LampCascadeStep + 0.45f;
                    if (_lampCascadeElapsed >= totalCascade)
                    {
                        _lampCascadeElapsed = -1f;
                        ApplyLampGlow(LampIntensity);
                    }
                }
                else
                {
                    ApplyLampGlow(LampIntensity);
                }
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

        /// <summary>
        /// Where 07's weld beam from <paramref name="nozzle"/> meets the stage being welded: on its parts' own
        /// colliders, through its seam point as the weld runs along it, else through the stage's middle as its parts
        /// unfold, else on the part nearest the seam (a stage whose middle is air). False (and the seam point) only
        /// when no ray meets any of its parts.
        /// </summary>
        public bool TryWeldContact(Vector3 nozzle, out Vector3 point, out Vector3 normal)
        {
            Vector3 seam = GetActiveWeldPoint();
            int stage = Mathf.Clamp(SolidStageCount, 0, StageCount - 1);
            Collider[] colliders = _stageColliders[stage];
            if (SurfaceAim.TryHit(colliders, nozzle, seam, out point, out normal) ||
                SurfaceAim.TryHit(colliders, nozzle, StageMiddle(stage), out point, out normal) ||
                SurfaceAim.TryNearestPart(colliders, nozzle, seam, out point, out normal))
            {
                return true;
            }

            point = seam;
            normal = Vector3.up;
            return false;
        }

        /// <summary>The middle of stage <paramref name="stage"/>'s parts as they stand now (world).</summary>
        private Vector3 StageMiddle(int stage)
        {
            StageRendererCache[] caches = _stageRenderers[stage];
            bool any = false;
            Bounds bounds = default;
            for (int r = 0; r < caches.Length; r++)
            {
                Renderer renderer = caches[r].Renderer;
                if (renderer == null || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (any)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
                else
                {
                    bounds = renderer.bounds;
                    any = true;
                }
            }

            return any ? bounds.center : _stageRoots[stage].position;
        }

        /// <summary>The structure's colliders on <paramref name="stageRoot"/>'s parts.</summary>
        private Collider[] CollidersUnder(Transform stageRoot)
        {
            var under = new List<Collider>();
            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null && _colliders[i].transform.IsChildOf(stageRoot))
                {
                    under.Add(_colliders[i]);
                }
            }

            return under.ToArray();
        }

        /// <summary>World-space focus point for 07's weld beam on the active stage, progressing along its seam.</summary>
        public Vector3 GetActiveWeldPoint()
        {
            if (_liveBuild && !IsGhost)
            {
                float prog = Mathf.Clamp01(_buildProgress01);
                float slicedProg = Mathf.Ceil(Mathf.Max(0.025f, prog) * 40f) / 40f;
                GetPrintCrossSection(slicedProg, out float centerX, out float centerZ, out float halfSpanX);
                float localY = Mathf.Lerp(_localMinY + 0.03f, _localMaxY - 0.03f, slicedProg);
                float sweep = Mathf.Sin(prog * 12f * Mathf.PI);
                float localX = centerX + sweep * halfSpanX * 0.85f;
                return transform.TransformPoint(new Vector3(localX, localY, centerZ));
            }

            GetStageWeldSeam(SolidStageCount, _activeSeamPoints, out _);
            float stageT = GetActiveStageProgress01(_buildProgress01);
            Vector3 local = stageT <= 0.5f
                ? Vector3.Lerp(_activeSeamPoints[0], _activeSeamPoints[1], stageT * 2f)
                : Vector3.Lerp(_activeSeamPoints[1], _activeSeamPoints[2], (stageT - 0.5f) * 2f);
            return transform.TransformPoint(local);
        }

        private void GetPrintCrossSection(float height01, out float centerX, out float centerZ, out float halfSpanX)
        {
            if (_stageLocalBounds == null || _stageLocalBounds.Length == 0)
            {
                centerX = 0f;
                centerZ = 0f;
                halfSpanX = 0.45f;
                return;
            }

            Bounds deckBounds = _stageLocalBounds[0];
            Bounds upperBounds = _stageLocalBounds.Length > 2 ? _stageLocalBounds[2] : _stageLocalBounds[_stageLocalBounds.Length - 1];
            float blendToUpper = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((height01 - 0.18f) / 0.14f));
            centerX = Mathf.Lerp(deckBounds.center.x, upperBounds.center.x, blendToUpper);
            float deckZ = Mathf.Lerp(deckBounds.min.z + 0.28f, deckBounds.max.z - 0.28f, Mathf.Clamp01(height01 / 0.24f));
            float upperZ = upperBounds.min.z + 0.06f;
            centerZ = Mathf.Lerp(deckZ, upperZ, blendToUpper);
            float deckHalfW = Mathf.Clamp(deckBounds.extents.x * 0.68f, 0.24f, 0.68f);
            float upperHalfW = Mathf.Clamp(upperBounds.extents.x * 0.82f, 0.12f, 0.28f);
            halfSpanX = Mathf.Lerp(deckHalfW, upperHalfW, blendToUpper);
        }

        /// <summary>The placement ghost's hologram: its plain calm look, a touch fainter while invalid.</summary>
        private void ApplyGhostProperties(bool valid)
        {
            int stageCount = _stageRenderers.Length;
            for (int s = 0; s < stageCount; s++)
            {
                StageRendererCache[] caches = _stageRenderers[s];
                for (int r = 0; r < caches.Length; r++)
                {
                    Renderer renderer = caches[r].Renderer;
                    if (renderer == null)
                    {
                        continue;
                    }

                    renderer.GetPropertyBlock(_mpb);
                    _mpb.SetFloat(_propBlueprintMode, 0f);
                    _mpb.SetFloat(_propStageFade, valid ? 1f : InvalidGhostFade);
                    renderer.SetPropertyBlock(_mpb);
                }
            }
        }

        /// <summary>
        /// Printed in place: every stage stands in its R1 metal clipped at the rising print plane, with a bright layer
        /// line at the plane and the print guide's bar and bead riding it; the settle thump shakes the finished
        /// structure.
        /// </summary>
        private void RefreshPrint()
        {
            int stageCount = _stageRoots.Length;
            int targetSolid = SolidStageCount;
            bool isBuilt = targetSolid >= stageCount;
            Vector3 activeWeldWS = GetActiveWeldPoint();

            float settleOffsetY = 0f;
            Vector3 settleScaleMul = Vector3.one;
            if (_settleElapsed >= 0f && _settleElapsed < SettleDuration)
            {
                float u = _settleElapsed / SettleDuration;
                float damp = Mathf.Exp(-u * 4.8f);
                float wave = Mathf.Sin(u * Mathf.PI * 2.2f);
                settleOffsetY = -_settleThumpMeters * wave * damp;
                float squash = 1f - 0.032f * wave * damp;
                float stretch = 1f + 0.018f * wave * damp;
                settleScaleMul = new Vector3(stretch, squash, stretch);
            }

            float printProg = isBuilt ? 1f : Mathf.Clamp(_buildProgress01, 0.03f, 0.995f);
            StepPrintLayerGuide(!isBuilt && _buildProgress01 > 0.001f, printProg, settleOffsetY);

            float worldMinY = transform.position.y + _localMinY + settleOffsetY;
            float worldMaxY = transform.position.y + _localMaxY + settleOffsetY;
            float sliceWorldY = Mathf.Lerp(worldMinY, worldMaxY, Mathf.Ceil(printProg * 40f) / 40f);

            for (int s = 0; s < stageCount; s++)
            {
                ResetStagePoseToBase(s);
                if (_stageRoots[s] != null)
                {
                    if (!_stageRoots[s].gameObject.activeSelf)
                    {
                        _stageRoots[s].gameObject.SetActive(true);
                    }

                    _stageRoots[s].localPosition = _stageBaseLocalPos[s] + new Vector3(0f, settleOffsetY, 0f);
                    _stageRoots[s].localScale = Vector3.Scale(_stageBaseScale[s] * _stageScale[s], settleScaleMul);
                }

                float residualCapHeat = (isBuilt && s == stageCount - 1) ? _stageHeat[s] * 0.45f : 0f;
                ApplyStageMaterials(s, solid: true);
                ApplyStageShaderBlock(
                    s,
                    printProgress: printProg,
                    stageHeat: residualCapHeat,
                    minY: worldMinY,
                    maxY: worldMaxY,
                    seamRef: sliceWorldY,
                    seamPointWS: activeWeldWS,
                    seamRadius: 0.38f);
            }
        }

        private void EnsurePrintLayerGuide()
        {
            if (_printLayerRoot != null)
            {
                return;
            }

            var rootGo = new GameObject("PrintLayerGuide");
            rootGo.transform.SetParent(transform, false);
            _printLayerRoot = rootGo.transform;

            var barRootGo = new GameObject("PrintLayerBar");
            barRootGo.transform.SetParent(_printLayerRoot, false);
            _printLayerBar = barRootGo.transform;

            var renderers = new List<Renderer>(6);

            // Horizontal additive layer line filament
            AddPrintGuidePart(
                renderers,
                _printLayerBar,
                "LayerFilament",
                Vector3.zero,
                new Vector3(1.52f, 0.015f, 0.018f));
            AddPrintGuidePart(
                renderers,
                _printLayerBar,
                "LayerTickL",
                new Vector3(-0.77f, 0f, 0f),
                new Vector3(0.024f, 0.048f, 0.14f));
            AddPrintGuidePart(
                renderers,
                _printLayerBar,
                "LayerTickR",
                new Vector3(0.77f, 0f, 0f),
                new Vector3(0.024f, 0.048f, 0.14f));

            // Travelling molten weld bead along the layer line
            GameObject beadGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beadGo.name = "LayerWeldBead";
            if (beadGo.TryGetComponent(out Collider col))
            {
                DestroySafe(col);
            }

            beadGo.transform.SetParent(_printLayerBar, false);
            beadGo.transform.localScale = new Vector3(0.18f, 0.026f, 0.032f);
            _printLayerBead = beadGo.transform;
            if (beadGo.TryGetComponent(out Renderer beadRenderer))
            {
                beadRenderer.sharedMaterial = _hologramInvalidMat;
                renderers.Add(beadRenderer);
            }

            _printLayerRenderers = renderers.ToArray();
        }

        private void AddPrintGuidePart(
            List<Renderer> list,
            Transform parent,
            string partName,
            Vector3 localPos,
            Vector3 localScale)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = partName;
            if (go.TryGetComponent(out Collider col))
            {
                DestroySafe(col);
            }

            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            if (go.TryGetComponent(out Renderer r))
            {
                r.sharedMaterial = _hologramInvalidMat;
                list.Add(r);
            }
        }

        private void StepPrintLayerGuide(bool visible, float progress01, float settleOffsetY)
        {
            if (!visible)
            {
                if (_printLayerRoot != null && _printLayerRoot.gameObject.activeSelf)
                {
                    _printLayerRoot.gameObject.SetActive(false);
                }

                return;
            }

            EnsurePrintLayerGuide();
            if (!_printLayerRoot.gameObject.activeSelf)
            {
                _printLayerRoot.gameObject.SetActive(true);
            }

            float slicedProg = Mathf.Ceil(Mathf.Clamp01(progress01) * 40f) / 40f;
            GetPrintCrossSection(slicedProg, out float centerX, out float centerZ, out float halfSpanX);
            float localY = Mathf.Lerp(_localMinY + 0.03f, _localMaxY - 0.03f, slicedProg) + settleOffsetY;
            float widthScale = Mathf.Clamp((halfSpanX * 2f) / 1.48f, 0.28f, 1.08f);

            if (_printLayerBar != null)
            {
                _printLayerBar.localPosition = new Vector3(centerX, localY, centerZ);
                _printLayerBar.localScale = new Vector3(widthScale, 1f, 1f);
            }

            if (_printLayerBead != null)
            {
                float sweep = Mathf.Sin(progress01 * 12f * Mathf.PI);
                _printLayerBead.localPosition = new Vector3(sweep * 0.56f, 0f, -0.01f);
            }

            for (int i = 0; i < _printLayerRenderers.Length; i++)
            {
                Renderer r = _printLayerRenderers[i];
                if (r == null)
                {
                    continue;
                }

                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat(_propBlueprintMode, 1f);
                _mpb.SetFloat(_propStageFade, 1.35f);
                r.SetPropertyBlock(_mpb);
            }
        }

        private void ResetStagePoseToBase(int stageIndex)
        {
            if (_stageRoots[stageIndex] != null)
            {
                _stageRoots[stageIndex].localPosition = _stageBaseLocalPos[stageIndex];
                _stageRoots[stageIndex].localRotation = _stageBaseLocalRot[stageIndex];
                _stageRoots[stageIndex].localScale = _stageBaseScale[stageIndex];
            }

        }

        private static void DestroySafe(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }

        private void ApplyStageShaderBlock(
            int stageIndex,
            float printProgress,
            float stageHeat,
            float minY,
            float maxY,
            float seamRef,
            Vector3 seamPointWS,
            float seamRadius)
        {
            StageRendererCache[] caches = _stageRenderers[stageIndex];
            var boundsVec = new Vector4(minY, maxY, seamRef, 0f);
            var seamVec = new Vector4(seamPointWS.x, seamPointWS.y, seamPointWS.z, seamRadius);
            for (int r = 0; r < caches.Length; r++)
            {
                Renderer renderer = caches[r].Renderer;
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat(_propBuildPrint, 1f);
                _mpb.SetFloat(_propBuildRevealProgress, printProgress);
                _mpb.SetFloat(_propBuildStageHeat, stageHeat);
                _mpb.SetVector(_propBuildBoundsY, boundsVec);
                _mpb.SetVector(_propBuildSeamPointWS, seamVec);
                renderer.SetPropertyBlock(_mpb);
            }
        }

        private void ClearStagePropertyBlock(int stageIndex)
        {
            StageRendererCache[] caches = _stageRenderers[stageIndex];
            for (int r = 0; r < caches.Length; r++)
            {
                Renderer renderer = caches[r].Renderer;
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat(_propBuildPrint, 0f);
                _mpb.SetFloat(_propBuildRevealProgress, 1f);
                _mpb.SetFloat(_propBuildStageHeat, 0f);
                renderer.SetPropertyBlock(_mpb);
            }
        }

        private void ApplySequentialLampBlink(float elapsed, float masterIntensity)
        {
            for (int i = 0; i < _lampGlows.Length; i++)
            {
                EmissionGlow glow = _lampGlows[i];
                if (glow == null)
                {
                    continue;
                }

                float localT = elapsed - i * LampCascadeStep;
                float lampLevel;
                if (localT < 0f)
                {
                    lampLevel = 0.08f;
                }
                else if (localT < 0.08f)
                {
                    lampLevel = 1.65f;
                }
                else if (localT < 0.16f)
                {
                    lampLevel = 0.42f;
                }
                else
                {
                    lampLevel = Mathf.Lerp(1.35f, masterIntensity, Mathf.Clamp01((localT - 0.16f) / 0.25f));
                }

                glow.Apply(_lampBaseColors[i], lampLevel);
            }

            if (_statusLight != null)
            {
                bool lit = !IsGhost && masterIntensity > 0.01f;
                if (_statusLight.enabled != lit)
                {
                    _statusLight.enabled = lit;
                }

                if (lit)
                {
                    _statusLight.intensity = _statusLightIntensity * masterIntensity;
                }
            }
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
            float bestArea = -1f;
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                if (renderer == null)
                {
                    continue;
                }

                bool rHas = false;
                Bounds rBounds = default;
                if (renderer.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                {
                    Bounds meshBounds = filter.sharedMesh.bounds;
                    EncapsulateCorners(
                        meshBounds,
                        renderer.transform,
                        structureRoot,
                        localSpaceInput: true,
                        ref rBounds,
                        ref rHas);
                }
                else
                {
                    Bounds worldBounds = renderer.bounds;
                    EncapsulateCorners(
                        worldBounds,
                        renderer.transform,
                        structureRoot,
                        localSpaceInput: false,
                        ref rBounds,
                        ref rHas);
                }

                if (!rHas)
                {
                    continue;
                }

                Vector3 sz = rBounds.size;
                float area = sz.x * sz.z + sz.x * sz.y;
                if (area > bestArea)
                {
                    bestArea = area;
                    stageBounds = rBounds;
                    hasBounds = true;
                }
            }

            if (!hasBounds || stageBounds.size.sqrMagnitude <= 1e-6f)
            {
                return false;
            }

            Vector3 min = stageBounds.min;
            Vector3 center = stageBounds.center;
            Vector3 max = stageBounds.max;
            float halfSpanX = Mathf.Clamp((max.x - min.x) * 0.30f, 0.08f, 0.36f);
            float seamY = Mathf.Lerp(center.y, max.y, 0.58f) + 0.015f;
            float seamZ = Mathf.Lerp(min.z, center.z, 0.48f);
            Vector3 seamNormal = new Vector3(0f, 0.86f, -0.51f).normalized;
            seam = new StageWeldSeam(
                new Vector3(center.x - halfSpanX, seamY, seamZ),
                new Vector3(center.x, seamY + 0.018f, seamZ - 0.03f),
                new Vector3(center.x + halfSpanX, seamY, seamZ),
                seamNormal);
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
