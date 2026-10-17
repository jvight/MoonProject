using System;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's 1980s instrument scan VFX (Task SCAN v2): a thin vertical fan of light (laser line scanner) from the lens
    /// that sweeps across the target, projecting a fine horizontal cyan/amber phosphor scan raster onto the target's
    /// surface, a faint edge outline that completes at 100% with one clean completion "ping" flash before fading, a
    /// soft aperture glow at the lens, and small calm motes. Zero allocations per frame.
    /// </summary>
    public sealed class ScanBeam
    {
        private const int RasterLineCount = 10;
        private const int MoteCount = 8;
        private const int PointSlots = 4 + MoteCount;
        private const float VisibleThreshold = 1e-4f;
        private const float MinFadeSeconds = 1e-3f;
        private const float MinDistance = 1e-4f;
        private const float MaxContactShare = 0.42f;
        private const float ApertureStartMultiplier = 1.48f;
        private const float ApertureEndMultiplier = 1.02f;
        private const float MinWidthShare = 0.6f;
        private const float ContactEndShare = 0.75f;
        private const float LineIntensity = 1f;
        private const float PingDuration = 0.36f;

        private static readonly Color CyanRasterColor = new Color(0.16f, 0.82f, 0.92f, 0.52f);
        private static readonly Color AmberRasterColor = new Color(0.82f, 0.54f, 0.16f, 0.54f);
        private static readonly Color OutlineBaseColor = new Color(0.20f, 0.80f, 0.88f, 0.34f);
        private static readonly Color OutlinePingColor = new Color(0.92f, 0.78f, 0.38f, 0.78f);

        private readonly FeelVfxTuning _tuning;
        private readonly Material _lineMaterial;
        private readonly Material _fanMaterial;
        private readonly LineRenderer _core;
        private readonly LineRenderer _sheath;
        private readonly LineRenderer _fanUpper;
        private readonly LineRenderer _fanLower;
        private readonly LineRenderer _laserContact;
        private readonly LineRenderer _outlineLoop;
        private readonly LineRenderer[] _rasterLines = new LineRenderer[RasterLineCount];
        private readonly Mesh _fanMesh;
        private readonly MeshRenderer _fanRenderer;
        private readonly Vector3[] _fanVertices = new Vector3[3];
        private readonly Color[] _fanColors = new Color[3];
        private readonly FeelSprites _points;

        private Vector3 _target;
        private Vector3 _span;
        private float _progress;
        private float _rate;
        private float _level;
        private float _elapsed;
        private float _pingRemaining;
        private bool _active;

        public ScanBeam(Transform parent, FeelVfxTuning tuning, Material beamMaterial, Material spriteMaterial)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            if (beamMaterial == null)
            {
                throw new ArgumentNullException(nameof(beamMaterial), "ScanBeam: the scan beam material is missing.");
            }

            _lineMaterial = CreateMaterial(beamMaterial, "M_ScanBeamLine", (int)RenderQueue.Transparent - 1);
            _fanMaterial = CreateMaterial(beamMaterial, "M_ScanBeamFanSheet", (int)RenderQueue.Transparent - 2);

            _sheath = Line("ScanBeamSheath", parent, _lineMaterial, tuning.ScanSheathWidth, 2, false);
            _core = Line("ScanBeamCore", parent, _lineMaterial, tuning.ScanCoreWidth, 2, false);
            _fanUpper = Line("ScanBeamFanUpper", parent, _lineMaterial, tuning.ScanCoreWidth * 0.7f, 2, false);
            _fanLower = Line("ScanBeamFanLower", parent, _lineMaterial, tuning.ScanCoreWidth * 0.7f, 2, false);
            _laserContact = Line("ScanBeamLaserLine", parent, _lineMaterial, tuning.ScanCoreWidth * 1.15f, 2, false);
            _outlineLoop = Line("ScanBeamOutline", parent, _lineMaterial, tuning.ScanCoreWidth * 0.85f, 5, true);

            for (int i = 0; i < RasterLineCount; i++)
            {
                _rasterLines[i] = Line("ScanRaster_" + i, parent, _lineMaterial, tuning.ScanCoreWidth * 0.72f, 2, false);
            }

            _fanMesh = CreateFanMesh(parent, _fanMaterial, out _fanRenderer);
            _points = new FeelSprites("ScanBeamPoints", parent, spriteMaterial, PointSlots, false, 0f);
        }

        /// <summary>True while the beam or its completion outline ping is active or easing out.</summary>
        public bool Shown => _level > VisibleThreshold || _pingRemaining > VisibleThreshold;

        /// <summary>Current eased visibility level (0..1).</summary>
        public float Level => Ease.InOutSine(_level);

        /// <summary>True while the completion outline ping flash is active.</summary>
        public bool IsPinging => _pingRemaining > VisibleThreshold;

        /// <summary>Starts or resumes the scan fan at <paramref name="progress"/> (0..1) over <paramref name="remainingSeconds"/>.</summary>
        public void Begin(Vector3 target, Vector3 span, float progress, float remainingSeconds)
        {
            _target = target;
            _span = span.sqrMagnitude > MinDistance ? span : Vector3.up * _tuning.ScanDefaultHeight;
            _progress = Mathf.Clamp01(progress);
            _rate = remainingSeconds > MinFadeSeconds ? (1f - _progress) / remainingSeconds : 0f;
            _pingRemaining = 0f;
            _active = true;
        }

        /// <summary>Keeps progress in step with quarter beats.</summary>
        public void Sync(float progress)
        {
            _progress = Mathf.Clamp01(progress);
        }

        /// <summary>Eases the beam out on let-go.</summary>
        public void End()
        {
            _active = false;
        }

        /// <summary>Triggers the single clean completion "ping" flash along the completed outline, then fades out.</summary>
        public void Complete()
        {
            _progress = 1f;
            _active = false;
            _pingRemaining = PingDuration;
        }

        public void Step(float deltaTime, Vector3 emitter)
        {
            _elapsed += deltaTime;
            float fadeSeconds = _active ? _tuning.ScanEaseInSeconds : _tuning.ScanEaseOutSeconds;
            _level = Mathf.MoveTowards(_level, _active ? 1f : 0f, deltaTime / Mathf.Max(fadeSeconds, MinFadeSeconds));
            if (_active)
            {
                _progress = Mathf.Clamp01(_progress + _rate * deltaTime);
            }

            if (_pingRemaining > 0f)
            {
                _pingRemaining = Mathf.Max(0f, _pingRemaining - deltaTime);
            }

            _points.Begin();
            bool shown = _level > VisibleThreshold;
            bool outlineShown = shown || _pingRemaining > VisibleThreshold;
            SetRenderersEnabled(shown, outlineShown);
            if (outlineShown)
            {
                StepGeometry(emitter, shown);
            }

            _points.End();
        }

        private void StepGeometry(Vector3 emitter, bool fanShown)
        {
            float eased = Ease.InOutSine(_level);
            float spanHeight = Mathf.Max(_span.magnitude, 0.36f);
            Vector3 up = _span.sqrMagnitude > MinDistance ? _span.normalized : Vector3.up;
            Vector3 lineCentre = _target + _span * (_progress - 0.5f);
            Vector3 toEmitter = emitter - lineCentre;
            float distance = toEmitter.magnitude;
            Vector3 towardLens = distance > MinDistance ? toEmitter / distance : Vector3.back;
            Vector3 right = Vector3.Cross(up, towardLens);
            right = right.sqrMagnitude > MinDistance ? right.normalized : Vector3.right;

            float standOff = Mathf.Min(_tuning.ScanContactOffset, distance * MaxContactShare);
            float apertureScale = Mathf.Lerp(ApertureStartMultiplier, ApertureEndMultiplier, _progress);
            float apertureForward = Mathf.Min(_tuning.ScanApertureOffset * apertureScale, distance * MaxContactShare);
            Vector3 aperture = emitter - towardLens * apertureForward;
            Vector3 contact = lineCentre + towardLens * standOff;
            Vector3 surfaceCenter = _target + towardLens * standOff;

            if (fanShown)
            {
                StepFanAndAperture(emitter, aperture, contact, surfaceCenter, right, up, spanHeight, eased);
            }

            StepRasterAndOutline(surfaceCenter, right, up, spanHeight, eased);
        }

        private void StepFanAndAperture(
            Vector3 emitter,
            Vector3 aperture,
            Vector3 contact,
            Vector3 surfaceCenter,
            Vector3 right,
            Vector3 up,
            float spanHeight,
            float eased)
        {
            float widthScale = Mathf.Lerp(MinWidthShare, 1f, eased);
            _core.SetPosition(0, emitter);
            _core.SetPosition(1, contact);
            _sheath.SetPosition(0, emitter);
            _sheath.SetPosition(1, contact);
            _core.widthMultiplier = _tuning.ScanCoreWidth * widthScale;
            _sheath.widthMultiplier = _tuning.ScanSheathWidth * widthScale;

            Color coreStart = _tuning.ScanCoreColor;
            coreStart.a *= eased;
            Color coreEnd = coreStart;
            coreEnd.a *= ContactEndShare;
            _core.startColor = coreStart;
            _core.endColor = coreEnd;

            Color sheathStart = _tuning.ScanBeamColor;
            sheathStart.a *= eased;
            Color sheathEnd = sheathStart;
            sheathEnd.a *= ContactEndShare;
            _sheath.startColor = sheathStart;
            _sheath.endColor = sheathEnd;

            // Vertical laser fan sweeping laterally across the target
            float halfWidth = Mathf.Clamp(spanHeight * 0.52f, 0.20f, 0.48f);
            float sweepHoriz = Mathf.Sin(_elapsed * 4.4f) * halfWidth * 0.85f;
            float fanHalfHeight = Mathf.Clamp(spanHeight * 0.44f, 0.16f, 0.40f);
            Vector3 sweepCenter = Vector3.Lerp(surfaceCenter, contact, 0.35f) + right * sweepHoriz;
            Vector3 fanTop = sweepCenter + up * fanHalfHeight;
            Vector3 fanBottom = sweepCenter - up * fanHalfHeight;

            UpdateFanLinesAndSheet(emitter, fanTop, fanBottom, widthScale, eased);
            UpdateLensAndMotes(emitter, aperture, contact, sweepCenter, right, up, fanHalfHeight, widthScale, eased);
        }

        private void UpdateFanLinesAndSheet(
            Vector3 emitter,
            Vector3 fanTop,
            Vector3 fanBottom,
            float widthScale,
            float eased)
        {
            Color edgeColor = _tuning.ScanBeamColor;
            edgeColor.a = 0.22f * eased;
            _fanUpper.widthMultiplier = _tuning.ScanCoreWidth * 0.65f * widthScale;
            _fanLower.widthMultiplier = _tuning.ScanCoreWidth * 0.65f * widthScale;
            _fanUpper.SetPosition(0, emitter);
            _fanUpper.SetPosition(1, fanTop);
            _fanLower.SetPosition(0, emitter);
            _fanLower.SetPosition(1, fanBottom);
            _fanUpper.startColor = edgeColor;
            _fanUpper.endColor = edgeColor;
            _fanLower.startColor = edgeColor;
            _fanLower.endColor = edgeColor;

            Color laserColor = Color.Lerp(_tuning.ScanBeamColor, AmberRasterColor, 0.25f);
            laserColor.a = 0.42f * eased;
            _laserContact.widthMultiplier = _tuning.ScanCoreWidth * 1.1f * widthScale;
            _laserContact.SetPosition(0, fanBottom);
            _laserContact.SetPosition(1, fanTop);
            _laserContact.startColor = laserColor;
            _laserContact.endColor = laserColor;

            _fanVertices[0] = emitter;
            _fanVertices[1] = fanTop;
            _fanVertices[2] = fanBottom;
            Color sheetApex = _tuning.ScanBeamColor;
            sheetApex.a = 0.10f * eased;
            Color sheetEdge = _tuning.ScanBeamColor;
            sheetEdge.a = 0.05f * eased;
            _fanColors[0] = sheetApex;
            _fanColors[1] = sheetEdge;
            _fanColors[2] = sheetEdge;
            _fanMesh.vertices = _fanVertices;
            _fanMesh.colors = _fanColors;
            _fanMesh.RecalculateBounds();
        }

        private void UpdateLensAndMotes(
            Vector3 emitter,
            Vector3 aperture,
            Vector3 contact,
            Vector3 sweepCenter,
            Vector3 right,
            Vector3 up,
            float fanHalfHeight,
            float widthScale,
            float eased)
        {
            Color lensCore = _tuning.ScanCoreColor;
            lensCore.a = _tuning.ScanLensCoreAlpha * eased;
            _points.Add(emitter, _tuning.ScanLensCoreSize * widthScale, lensCore, Vector3.zero);
            _points.Add(aperture, _tuning.ScanLensCoreSize * widthScale, lensCore, Vector3.zero);

            Color lensHalo = _tuning.ScanBeamColor;
            lensHalo.a = _tuning.ScanLensHaloAlpha * eased;
            _points.Add(aperture, _tuning.ScanLensHaloSize * widthScale, lensHalo, Vector3.zero);

            Color contactDot = _tuning.ScanBeamColor;
            contactDot.a = _tuning.ScanContactAlpha * eased;
            _points.Add(contact, _tuning.ScanContactSize * widthScale, contactDot, Vector3.zero);

            for (int i = 0; i < MoteCount; i++)
            {
                float phase = _elapsed * 0.85f + i * 0.37f;
                float life01 = phase - Mathf.Floor(phase);
                float env = Mathf.Sin(life01 * Mathf.PI);
                float hOffset = Mathf.Sin(i * 2.1f + _elapsed * 0.9f) * fanHalfHeight * 0.85f;
                float vOffset = (life01 - 0.5f) * fanHalfHeight * 1.35f;
                Vector3 motePos = Vector3.Lerp(sweepCenter, aperture, (i & 1) == 0 ? 0.12f : 0.28f)
                    + right * hOffset + up * vOffset;
                Color moteColor = (i % 3 == 0) ? AmberRasterColor : CyanRasterColor;
                moteColor.a = 0.14f * env * eased;
                float moteSize = Mathf.Lerp(0.016f, 0.026f, (i & 1)) * widthScale;
                _points.Add(motePos, moteSize, moteColor, Vector3.zero);
            }
        }

        private void StepRasterAndOutline(
            Vector3 surfaceCenter,
            Vector3 right,
            Vector3 up,
            float spanHeight,
            float eased)
        {
            float halfW = Mathf.Clamp(spanHeight * 0.52f, 0.20f, 0.48f);
            float halfH = Mathf.Clamp(spanHeight * 0.48f, 0.18f, 0.44f);
            for (int i = 0; i < RasterLineCount; i++)
            {
                float row01 = (i + 0.5f) / RasterLineCount;
                float reveal = Mathf.Clamp01((_progress - row01 * 0.92f) * 6f) * eased;
                LineRenderer raster = _rasterLines[i];
                if (reveal <= VisibleThreshold)
                {
                    raster.enabled = false;
                    continue;
                }

                raster.enabled = true;
                float y = Mathf.Lerp(-halfH, halfH, row01);
                float rowWidth = halfW * (0.82f + 0.18f * Mathf.Sin(row01 * Mathf.PI));
                Vector3 rowCenter = surfaceCenter + up * y;
                raster.SetPosition(0, rowCenter - right * rowWidth);
                raster.SetPosition(1, rowCenter + right * rowWidth);
                Color c = (i % 3 == 1) ? AmberRasterColor : CyanRasterColor;
                c.a *= reveal * Mathf.Lerp(0.55f, 0.95f, _progress);
                raster.startColor = c;
                raster.endColor = c;
            }

            float ping01 = _pingRemaining > 0f
                ? Mathf.Sin((1f - _pingRemaining / PingDuration) * Mathf.PI)
                : 0f;
            float outlineReveal = Mathf.Max(eased * Mathf.Lerp(0.25f, 1f, _progress * _progress), ping01);
            Color outlineColor = Color.Lerp(OutlineBaseColor, OutlinePingColor, ping01);
            outlineColor.a *= outlineReveal;
            _outlineLoop.widthMultiplier = _tuning.ScanCoreWidth * Mathf.Lerp(0.8f, 1.35f, ping01);
            _outlineLoop.startColor = outlineColor;
            _outlineLoop.endColor = outlineColor;
            _outlineLoop.SetPosition(0, surfaceCenter - right * halfW - up * halfH);
            _outlineLoop.SetPosition(1, surfaceCenter - right * halfW + up * halfH);
            _outlineLoop.SetPosition(2, surfaceCenter + right * halfW + up * halfH);
            _outlineLoop.SetPosition(3, surfaceCenter + right * halfW - up * halfH);
            _outlineLoop.SetPosition(4, surfaceCenter - right * halfW - up * halfH);
        }

        private void SetRenderersEnabled(bool fanShown, bool outlineShown)
        {
            _core.enabled = fanShown;
            _sheath.enabled = fanShown;
            _fanUpper.enabled = fanShown;
            _fanLower.enabled = fanShown;
            _laserContact.enabled = fanShown;
            _fanRenderer.enabled = fanShown;
            _outlineLoop.enabled = outlineShown;
            if (!fanShown)
            {
                for (int i = 0; i < RasterLineCount; i++)
                {
                    _rasterLines[i].enabled = false;
                }
            }
        }

        private static Material CreateMaterial(Material source, string name, int queue)
        {
            var mat = new Material(source)
            {
                name = name,
                renderQueue = queue,
            };
            mat.SetColor("_Color", Color.white);
            mat.SetFloat("_Intensity", LineIntensity);
            mat.SetFloat("_SrcBlend", (float)BlendMode.One);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            return mat;
        }

        private static Mesh CreateFanMesh(Transform parent, Material material, out MeshRenderer renderer)
        {
            var host = new GameObject("ScanBeamFanSheet");
            host.transform.SetParent(parent, false);
            MeshFilter filter = host.AddComponent<MeshFilter>();
            renderer = host.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.enabled = false;

            var mesh = new Mesh { name = "ScanBeamFanMesh" };
            mesh.MarkDynamic();
            mesh.vertices = new Vector3[3];
            mesh.colors = new[] { Color.clear, Color.clear, Color.clear };
            mesh.uv = new[] { new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 1 };
            filter.sharedMesh = mesh;
            return mesh;
        }

        private static LineRenderer Line(string name, Transform parent, Material material, float width, int count, bool loop)
        {
            var host = new GameObject(name);
            host.transform.SetParent(parent, false);
            var line = host.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = count;
            line.loop = loop;
            line.widthMultiplier = width;
            line.numCapVertices = 2;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.sharedMaterial = material;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.lightProbeUsage = LightProbeUsage.Off;
            line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            line.enabled = false;
            return line;
        }
    }
}
