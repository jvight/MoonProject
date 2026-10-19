#ifndef MOONPROJECT_R1_CRT_FACE_CORE_INCLUDED
#define MOONPROJECT_R1_CRT_FACE_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "R1CrtFaceInput.hlsl"

static const float CRT_ASPECT = 4.0 / 3.0;
static const float CRT_PI = 3.14159265;
static const float HAPPY_ARC_SIN = 0.92977649;  // sin(0.38 * PI)
static const float HAPPY_ARC_COS = 0.36812455;  // cos(0.38 * PI)
static const float CLOSED_ARC_SIN = 0.89100652; // sin(0.35 * PI)
static const float CLOSED_ARC_COS = 0.45399050; // cos(0.35 * PI)
static const float QUESTION_SIN = 0.13954311;   // sin(0.14)
static const float QUESTION_COS = 0.99021600;   // cos(0.14)
static const float QUESTION_ARC_SIN = 0.92387953; // sin(0.375 * PI)
static const float QUESTION_ARC_COS = 0.38268343; // cos(0.375 * PI)
static const float MIN_STROKE_PX = 0.011;
static const float CLOSED_MIN_STROKE_PX = 0.0085;
static const float BOOT_LINE_HALF_HEIGHT = 0.0055;
static const float BOOT_LINE_HALF_WIDTH_SCALE = 0.45;
static const float LOW_POWER_SCANLINE_MIN = 95.0;
static const float ROLLING_BAND_WIDTH = 0.12;
static const float ROLLING_BAND_STRENGTH = 0.08;
static const float HOT_CORE_INNER_DIST = -0.012;
static const float GLASS_SPECULAR_STRENGTH = 0.04;
static const float2 CONTENT_FALLBACK_TEXEL_SIZE = float2(1.0 / 1152.0, 1.0 / 768.0);
static const float CONTENT_TENT_RADIUS_SCALE = 18.0;
static const half CONTENT_SHOULDER_ATTEN = 0.72h;
static const half CONTENT_DIRECT_GAIN = 1.12h;
// Content luminance below which a pixel is a dark fill (shown as the tube's black), and above which it is lit.
static const half2 CONTENT_FILL_LUMA = half2(0.012h, 0.040h);
static const half3 CONTENT_BG_LINEAR = half3(0.0178h, 0.0170h, 0.0026h);
static const half3 CONTENT_EDGE_LINEAR = half3(0.0162h, 0.0155h, 0.0024h);
static const half3 CONTENT_VIGNETTE_FLOOR = half3(0.0023h, 0.0027h, 0.0040h);
static const float GLASS_ASPECT = 1.5;
static const float GLASS_CORNER_RADIUS_H = 0.113924;

struct Attributes
{
    float4 positionOS : POSITION;
    float3 normalOS   : NORMAL;
    float2 uv         : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    float3 normalWS   : TEXCOORD1;
    float2 uv         : TEXCOORD2;
    half   fogFactor  : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings CrtFacePassVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = vertexInput.positionCS;
    output.positionWS = vertexInput.positionWS;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    output.uv = input.uv;
    output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    return output;
}

float SdfRoundedBox(float2 p, float2 halfSize, float radius)
{
    float r = min(radius, min(halfSize.x, halfSize.y));
    float2 q = abs(p) - (halfSize - r);
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}

float SdfSegment(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / max(dot(ba, ba), 1e-6));
    return length(pa - ba * h);
}

float SdfSymmetricArc(float2 q, float radius, float sinHalfSpan, float cosHalfSpan, float halfStroke)
{
    float dist = (cosHalfSpan * q.x < sinHalfSpan * q.y)
        ? abs(length(q) - radius)
        : length(q - float2(sinHalfSpan, cosHalfSpan) * radius);
    return dist - halfStroke;
}

void EvaluateSingleEye(
    float2 canvasPos,
    float aaWidth,
    float4 eyeRect,
    float4 eyeShape,
    float4 eyeMode,
    float mirrorSign,
    out float coverage,
    out float minSdf)
{
    float2 center = float2(eyeRect.x * CRT_ASPECT, eyeRect.y);
    float w = max(eyeRect.z, 0.008);
    float h = max(eyeRect.w, 0.006);

    float tilt = eyeShape.x * mirrorSign;
    float cut = eyeShape.y;
    float squint = eyeShape.z;

    float arcWeight = saturate(eyeMode.x);
    float effectiveArc = smoothstep(0.45, 0.85, arcWeight);
    float ringWeight = saturate(eyeMode.y);
    float closedWeight = saturate(eyeMode.z);
    float solidWeight = saturate((1.0 - effectiveArc) * (1.0 - ringWeight) * (1.0 - closedWeight));

    float2 d = canvasPos - center;

    // 1. Solid pill with top-lid plane cut and bottom-lid squint cut
    float2 halfSize = float2(w, h) * 0.5;
    float sdfSolid = SdfRoundedBox(d, halfSize, min(halfSize.x, halfSize.y));
    if (cut > 0.005 || abs(tilt) > 0.005)
    {
        float sTilt, cTilt;
        sincos(tilt, sTilt, cTilt);
        float yCut = -halfSize.y + cut * h;
        float sdfTop = (yCut * cTilt + d.x * sTilt) - d.y * cTilt;
        sdfSolid = max(sdfSolid, sdfTop);
    }

    if (squint > 0.005)
    {
        float normX = clamp(d.x / max(halfSize.x, 1e-4), -1.0, 1.0);
        float archLift = arcWeight * h * 0.12 * (1.0 - normX * normX);
        float sdfBot = d.y - (halfSize.y - squint * h * 0.9 - archLift);
        sdfSolid = max(sdfSolid, sdfBot);
    }

    float covSolid = smoothstep(aaWidth, -aaWidth, sdfSolid) * solidWeight;

    // 2. Happy upward crescent arc (^)
    float2 pArc = d - float2(0.0, h * 0.18);
    float2 qArc = float2(abs(pArc.x), -pArc.y);
    float halfStrokeArc = max(MIN_STROKE_PX, w * 0.30) * 0.5;
    float sdfArc = SdfSymmetricArc(qArc, w * 0.5, HAPPY_ARC_SIN, HAPPY_ARC_COS, halfStrokeArc);
    float covArc = smoothstep(aaWidth, -aaWidth, sdfArc) * effectiveArc;

    // 3. Startled hollow ellipse ring (O)
    float2 ab = max(float2(w * 0.5, h * 0.42), 0.005);
    float2 dEll = d / ab;
    float rEll = length(dEll);
    float2 gradEll = d / max(ab * ab * max(rEll, 1e-4), 1e-6);
    float halfStrokeRing = max(MIN_STROKE_PX, w * 0.20) * 0.5;
    float sdfRing = (abs(rEll - 1.0) / max(length(gradEll), 1e-4)) - halfStrokeRing;
    float covRing = smoothstep(aaWidth, -aaWidth, sdfRing) * ringWeight;

    // 4. Asleep downward closed crescent arc (v)
    float2 pClosed = d - float2(0.0, -h * 0.05);
    float2 qClosed = float2(abs(pClosed.x), pClosed.y);
    float halfStrokeClosed = max(CLOSED_MIN_STROKE_PX, w * 0.20) * 0.5;
    float sdfClosed = SdfSymmetricArc(qClosed, w * 0.45, CLOSED_ARC_SIN, CLOSED_ARC_COS, halfStrokeClosed);
    float covClosed = smoothstep(aaWidth, -aaWidth, sdfClosed) * closedWeight;

    coverage = saturate(covSolid + covArc + covRing + covClosed);

    float blendedSdf = 1.0;
    if (solidWeight > 0.01)
    {
        blendedSdf = min(blendedSdf, sdfSolid);
    }
    if (effectiveArc > 0.01)
    {
        blendedSdf = min(blendedSdf, sdfArc);
    }
    if (ringWeight > 0.01)
    {
        blendedSdf = min(blendedSdf, sdfRing);
    }
    if (closedWeight > 0.01)
    {
        blendedSdf = min(blendedSdf, sdfClosed);
    }

    minSdf = blendedSdf;
}

float EvaluateQuestionSymbol(float2 canvasPos, float aaWidth, out float sdfOut)
{
    float2 center = float2(0.80 * CRT_ASPECT, 0.24);
    float2 d = canvasPos - center;
    // Rotate by -0.14 rad into symbol local space
    float2 p = float2(
        d.x * QUESTION_COS + d.y * QUESTION_SIN,
        -d.x * QUESTION_SIN + d.y * QUESTION_COS);

    // Upper hook arc centered at (0, -0.030) opening down-left
    float2 pHook = p - float2(0.0, -0.030);
    // Rotate hook so its symmetry axis points toward (-0.35, +0.936) (opening down-left)
    float2 rotHook = float2(
        pHook.x * 0.936 - pHook.y * 0.35,
        pHook.x * 0.35 + pHook.y * 0.936);
    float2 qHook = float2(abs(rotHook.x), -rotHook.y);
    float sdfHook = SdfSymmetricArc(qHook, 0.024, QUESTION_ARC_SIN, QUESTION_ARC_COS, 0.0085);

    // Lower stem from right side of hook (0.016, -0.014) to (0.0, 0.002) then down to (0.0, 0.019)
    float sdfStemA = SdfSegment(p, float2(0.016, -0.014), float2(0.0, 0.002)) - 0.0085;
    float sdfStemB = SdfSegment(p, float2(0.0, 0.002), float2(0.0, 0.019)) - 0.0085;
    float sdfDot = length(p - float2(0.0, 0.043)) - 0.010;

    sdfOut = min(min(sdfHook, min(sdfStemA, sdfStemB)), sdfDot);
    return smoothstep(aaWidth, -aaWidth, sdfOut);
}

float EvaluateBatterySymbol(float2 canvasPos, float aaWidth, float animTime, out float sdfOut)
{
    float2 center = float2(0.50 * CRT_ASPECT, 0.82);
    float2 p = canvasPos - center;

    float sdfOuterBox = SdfRoundedBox(p - float2(-0.0055, 0.0), float2(0.0555, 0.0278), 0.004);
    float sdfShell = abs(sdfOuterBox) - 0.0042;
    float sdfNub = SdfRoundedBox(p - float2(0.0555, 0.0), float2(0.0055, 0.0139), 0.0015);
    float sdfBar = SdfRoundedBox(p - float2(-0.0389, 0.0), float2(0.0111, 0.0167), 0.0015);

    sdfOut = min(min(sdfShell, sdfNub), sdfBar);
    float blink = (sin(animTime * 6.0) > -0.2) ? 0.95 : 0.25;
    return smoothstep(aaWidth, -aaWidth, sdfOut) * blink;
}

float EvaluateZzzSymbol(float2 canvasPos, float aaWidth, float animTime, out float sdfOut)
{
    sdfOut = 1.0;
    float totalCov = 0.0;

    [unroll]
    for (int i = 0; i < 3; i++)
    {
        float fi = (float)i;
        float drift = frac(animTime * 0.4 + fi * 0.33);
        float zAlpha = sin(drift * CRT_PI) * 0.85;
        float2 zCenter = float2(
            (0.76 + fi * 0.0375) * CRT_ASPECT,
            0.32 - drift * 0.0778 - fi * 0.0444);
        float sz = 0.017 + fi * 0.0045;
        float stroke = sz * 0.28;

        float2 p = canvasPos - zCenter;
        float dTop = SdfSegment(p, float2(-sz, -sz), float2(sz, -sz)) - stroke;
        float dDiag = SdfSegment(p, float2(sz, -sz), float2(-sz, sz)) - stroke;
        float dBot = SdfSegment(p, float2(-sz, sz), float2(sz, sz)) - stroke;
        float dZ = min(dTop, min(dDiag, dBot));

        sdfOut = min(sdfOut, dZ);
        totalCov = max(totalCov, smoothstep(aaWidth, -aaWidth, dZ) * zAlpha);
    }

    return totalCov;
}

float EvaluateScanProgressRing(float2 canvasPos, float aaWidth, float progress01, out float sdfOut)
{
    float2 center = float2(0.80 * CRT_ASPECT, 0.24);
    float2 p = canvasPos - center;
    float r = length(p);
    float ringRadius = 0.078;
    float ringHalfWidth = 0.0048;

    // Clockwise angle from top (12 o'clock = 0..1)
    float angle = atan2(p.x, -p.y);
    float cw01 = frac((angle + 2.0 * CRT_PI) / (2.0 * CRT_PI));

    float sdfRing = abs(r - ringRadius) - ringHalfWidth;
    float arcGate = smoothstep(progress01 + 0.02, progress01 - 0.01, cw01);

    // Four cardinal quarter-ticks (25%, 50%, 75%, 100%) outside the ring
    float2 q = abs(p);
    float tickX = SdfSegment(q, float2(0.082, 0.0), float2(0.096, 0.0)) - 0.0035;
    float tickY = SdfSegment(q, float2(0.0, 0.082), float2(0.0, 0.096)) - 0.0035;
    float sdfTicks = min(tickX, tickY);

    sdfOut = min(sdfRing + (1.0 - arcGate) * 0.04, sdfTicks);
    float ringCov = smoothstep(aaWidth, -aaWidth, sdfRing) * arcGate;
    float tickCov = smoothstep(aaWidth, -aaWidth, sdfTicks) * (0.45 + 0.55 * arcGate);
    return max(ringCov, tickCov);
}

half4 CrtFacePassFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float bootProgress = saturate(_FaceCrtState.x);
    float brightness = max(_FaceCrtState.y, 0.0);
    float lowPower = saturate(_FaceCrtState.z);
    float verticalJitter = _FaceCrtState.w;

    float scanlinePhase = _FacePhases.x;
    float rollingBandPhase = _FacePhases.y;
    float bootLineEnd = max(_FacePhases.z, 0.01);

    float contentMix = saturate(_FaceContentState.x);
    float verticalPinch = saturate(_FaceContentState.y);
    float lineGlow = saturate(_FaceContentState.z);

    // On RoverModel's Screen mesh, UV.x=0 is on the viewer's right (+X in Unity head space) and UV.x=1 is on the viewer's left;
    // mirror X into viewer left-to-right [0..1] space so eye asymmetry and glyphs (?, zzz, battery) read upright and un-mirrored.
    float2 rawUv = float2(1.0 - input.uv.x, input.uv.y);
    float2 jitteredUv = float2(rawUv.x, rawUv.y + verticalJitter);
    float2 cc = jitteredUv - 0.5;
    float rSq = dot(cc, cc);

    float openSpan = max(1.0 - verticalPinch, 0.012);
    float deflectedY = 0.5 + (jitteredUv.y - 0.5) / openSpan;
    float2 deflectedUv = float2(jitteredUv.x, deflectedY);
    float deflectionBandMask = (verticalPinch > 0.001)
        ? smoothstep(0.495 * openSpan + 0.010, 0.480 * openSpan, abs(jitteredUv.y - 0.5))
        : 1.0;

    float2 canvasPos = float2(deflectedUv.x * CRT_ASPECT, 1.0 - deflectedUv.y);
    float aaWidth = max(length(fwidth(float2(jitteredUv.x * CRT_ASPECT, 1.0 - jitteredUv.y))), 0.0025);

    float primaryCov = 0.0;
    float ghostCov = 0.0;
    float minPrimarySdf = 1.0;

    if (bootProgress < bootLineEnd)
    {
        // Power-on horizontal line opening from screen center
        float lineT = saturate(bootProgress / bootLineEnd);
        float halfW = max(0.005, CRT_ASPECT * BOOT_LINE_HALF_WIDTH_SCALE * lineT);
        float2 lineCenter = float2(0.5 * CRT_ASPECT, 0.5);
        float sdfLine = SdfRoundedBox(canvasPos - lineCenter, float2(halfW, BOOT_LINE_HALF_HEIGHT), BOOT_LINE_HALF_HEIGHT);
        primaryCov = smoothstep(aaWidth, -aaWidth, sdfLine);
        minPrimarySdf = sdfLine;
    }
    else
    {
        float covL, sdfL;
        EvaluateSingleEye(canvasPos, aaWidth, _EyeLeftRect, _EyeLeftShape, _EyeLeftMode, 1.0, covL, sdfL);

        float covR, sdfR;
        EvaluateSingleEye(canvasPos, aaWidth, _EyeRightRect, _EyeRightShape, _EyeRightMode, -1.0, covR, sdfR);

        primaryCov = saturate(covL + covR);
        minPrimarySdf = min(sdfL, sdfR);

        // Phosphor trail ghost from previous/lagged eye parameters
        if (_TrailAlpha > 0.001)
        {
            float gCovL, gSdfL;
            EvaluateSingleEye(canvasPos, aaWidth, _GhostLeftRect, _GhostLeftShape, _GhostLeftMode, 1.0, gCovL, gSdfL);

            float gCovR, gSdfR;
            EvaluateSingleEye(canvasPos, aaWidth, _GhostRightRect, _GhostRightShape, _GhostRightMode, -1.0, gCovR, gSdfR);

            ghostCov = saturate(gCovL + gCovR) * _TrailAlpha;
        }

        // Corner/status symbol (?, battery, zzz)
        int symbolId = (int)round(_FaceSymbol.x);
        float symbolAlpha = saturate(_FaceSymbol.y);
        float animTime = _FaceSymbol.z;
        float scanRing = saturate(_FaceSymbol.w);
        if (symbolId > 0 && symbolAlpha > 0.005)
        {
            float symSdf = 1.0;
            float symCov = 0.0;
            if (symbolId == 1)
            {
                symCov = EvaluateQuestionSymbol(canvasPos, aaWidth, symSdf);
            }
            else if (symbolId == 2)
            {
                symCov = EvaluateBatterySymbol(canvasPos, aaWidth, animTime, symSdf);
            }
            else if (symbolId == 3)
            {
                symCov = EvaluateZzzSymbol(canvasPos, aaWidth, animTime, symSdf);
            }

            symCov *= symbolAlpha;
            primaryCov = max(primaryCov, symCov);
            minPrimarySdf = min(minPrimarySdf, symSdf);
        }

        if (scanRing > 0.005)
        {
            float ringSdf = 1.0;
            float ringCov = EvaluateScanProgressRing(canvasPos, aaWidth, scanRing, ringSdf);
            primaryCov = max(primaryCov, ringCov);
            minPrimarySdf = min(minPrimarySdf, ringSdf);
        }

        primaryCov *= deflectionBandMask;
        ghostCov *= deflectionBandMask;
    }

    // Dual-radius phosphor halo matching face_prototype.html's blur(10px) #c86a14 + blur(3px) #ffae38 passes
    float glowRadius = max((float)_GlowRadius, 0.004);
    float distOutside = max(minPrimarySdf, 0.0);
    float wideHalo = exp(-distOutside / (glowRadius * 2.1)) * saturate(1.0 - distOutside / (glowRadius * 5.0));
    float tightHalo = exp(-distOutside / (glowRadius * 0.48)) * saturate(1.0 - distOutside / (glowRadius * 1.8));
    float poweredOn = (bootProgress < 0.01) ? 0.0 : 1.0;
    wideHalo *= poweredOn * deflectionBandMask;
    tightHalo *= poweredOn * deflectionBandMask;

    // Hot phosphor inner core factor (only deep inside the stroke/pill reaches _HotCorePeak > BloomThreshold)
    float innerCore = smoothstep(0.0, HOT_CORE_INNER_DIST, minPrimarySdf) * primaryCov;

    // Rounded screen bezel SDF in physical 3:2 glass aspect (corner radius 0.0420 m / 0.3687 m = 0.113924)
    float2 glassPos = (jitteredUv - 0.5) * float2(GLASS_ASPECT, 1.0);
    float bezelSdf = SdfRoundedBox(glassPos, float2(0.5 * GLASS_ASPECT, 0.5), GLASS_CORNER_RADIUS_H);
    float rimVig = smoothstep(0.004, -0.022, bezelSdf);
    float edgePhosphorGlow = smoothstep(-0.028, -0.006, bezelSdf) * smoothstep(0.005, -0.002, bezelSdf) * poweredOn;

    // Warm dark CRT screen background for procedural eyes
    float vig = smoothstep(0.52, 0.04, rSq) * rimVig;
    half3 bgPhosphor = lerp(_ScreenEdgeColor.rgb, _ScreenBaseColor.rgb, (half)vig)
        + _PhosphorGlowColor.rgb * (half)(0.05 * vig * poweredOn + 0.14 * edgePhosphorGlow);

    // Compose phosphor layers:
    // 1. Dark warm phosphor base + wide halo + tight halo + ghost trail + primary amber shape
    half shapeWeight = (half)max(primaryCov, ghostCov);
    half3 bodyEmission = bgPhosphor
        + _PhosphorGlowColor.rgb * (half)(wideHalo * 0.55)
        + _PhosphorColor.rgb * (half)(tightHalo * 0.38);
    bodyEmission = lerp(bodyEmission, _PhosphorColor.rgb, shapeWeight);

    // Clamp non-core emission to _BloomSafeMax so background and outer glow never blow out bloom
    half bodyMax = max(bodyEmission.r, max(bodyEmission.g, bodyEmission.b));
    if (bodyMax > _BloomSafeMax)
    {
        bodyEmission *= (_BloomSafeMax / max(bodyMax, (half)1e-4));
    }

    // 2. Small hot core inside eyes/symbols rises to _HotCorePeak (> Palette.BloomThreshold = 1.25)
    half3 hotCoreTarget = _HotCoreColor.rgb * _HotCorePeak;
    half3 phosphorHdr = lerp(bodyEmission, hotCoreTarget, (half)(innerCore * 0.65));

    // 3. Screen content mode (_ContentTex): UI rendered flat onto the convex CRT glass UVs with a near-neutral
    //    warm-black background (sat <= 0.25, val ~0.075), stroke-localized phosphor glow, and thin edge vignette.
    if (contentMix > 0.001)
    {
        float inDeflectedBounds = step(0.0, deflectedUv.x) * step(deflectedUv.x, 1.0)
            * step(0.0, deflectedUv.y) * step(deflectedUv.y, 1.0) * deflectionBandMask;
        float2 contentUv = saturate(deflectedUv);
        // Read the UI with its own screen-space footprint, so the texture's mips and anisotropy filter it the way the
        // screen needs at any resolution (no aliasing when the face covers fewer pixels than the texture holds).
        float2 contentDx = ddx(deflectedUv);
        float2 contentDy = ddy(deflectedUv);
        half4 contentSample = SAMPLE_TEXTURE2D_GRAD(_ContentTex, sampler_ContentTex, contentUv, contentDx, contentDy)
            * (half)inDeflectedBounds;

        // The halation was tuned against the 1152 x 768 layout. The content texture may be sharper than that (the UI
        // renders it at twice the layout so text and line art stay crisp on a big screen), so the halo keeps its
        // radius in UV and its blur by sampling that many mip levels deeper; the stroke itself is read at mip 0.
        float2 tentOffset = CONTENT_FALLBACK_TEXEL_SIZE * (glowRadius * CONTENT_TENT_RADIUS_SCALE);
        float sharpnessMips = _ContentTex_TexelSize.z > 0.0
            ? log2(max(_ContentTex_TexelSize.z * CONTENT_FALLBACK_TEXEL_SIZE.x, 1.0)) : 0.0;
        float tightMip = max((float)_ContentGlowMip, 0.5) + sharpnessMips;
        half3 tightGlow = (
            SAMPLE_TEXTURE2D_LOD(_ContentTex, sampler_ContentTex, saturate(contentUv + float2( tentOffset.x,  tentOffset.y)), tightMip).rgb +
            SAMPLE_TEXTURE2D_LOD(_ContentTex, sampler_ContentTex, saturate(contentUv + float2(-tentOffset.x,  tentOffset.y)), tightMip).rgb +
            SAMPLE_TEXTURE2D_LOD(_ContentTex, sampler_ContentTex, saturate(contentUv + float2( tentOffset.x, -tentOffset.y)), tightMip).rgb +
            SAMPLE_TEXTURE2D_LOD(_ContentTex, sampler_ContentTex, saturate(contentUv + float2(-tentOffset.x, -tentOffset.y)), tightMip).rgb
        ) * 0.25h;

        half contentLum = max(contentSample.r, max(contentSample.g, contentSample.b));
        half glowLum = max(tightGlow.r, max(tightGlow.g, tightGlow.b));
        // Localize phosphor glow strictly around strokes and text (zero wash over dark background)
        half glowGate = smoothstep(0.035h, 0.140h, glowLum);
        half shoulderWeight = saturate(1.0h - contentLum * CONTENT_SHOULDER_ATTEN);
        half glowMask = (half)inDeflectedBounds * (_GlowRadius > 0.001h ? 1.0h : 0.0h);
        half3 phosphorHalation = (tightGlow * _PhosphorColor.rgb)
            * (_ContentGlowIntensity * glowGate * shoulderWeight * glowMask);

        // The UI is premultiplied over a clear texture: lay it over the tube's dark by its own coverage, so its
        // anti-aliased edges, thin strokes and authored colours come through as drawn (no stroke boost, no gold tint),
        // lifted only a touch for the phosphor's emission.
        float contentRimVig = smoothstep(0.003, -0.016, bezelSdf);
        float2 vigD = abs(cc) * float2(0.57, 0.675);
        half vigFactor = (half)pow(saturate(1.0 - dot(vigD, vigD)), 2.5);
        half3 contentBgCenter = max(CONTENT_BG_LINEAR - CONTENT_VIGNETTE_FLOOR * (1.0h - vigFactor), 0.0h) / max(vigFactor, 0.58h);
        half3 contentBgEdge = max(CONTENT_EDGE_LINEAR - CONTENT_VIGNETTE_FLOOR * (1.0h - vigFactor), 0.0h) / max(vigFactor, 0.58h);
        half3 contentBg = lerp(contentBgEdge, contentBgCenter, (half)contentRimVig);
        half coverage = saturate(contentSample.a);
        // A stroke's own colour (un-premultiplied) keeps its hue but never falls below the look's floor
        // (_ContentMinStrokeLuma; 0 when the palette carries its own contrast); edges keep their partial coverage.
        half3 strokeColour = contentSample.rgb / max(coverage, (half)1e-3) * CONTENT_DIRECT_GAIN;
        half strokeLuma = max(dot(strokeColour, half3(0.2126h, 0.7152h, 0.0722h)), (half)1e-4);
        strokeColour *= max(1.0h, _ContentMinStrokeLuma / strokeLuma);
        // A saturated colour (coral, green) runs out of headroom before it reaches the floor: keep its hue at full
        // strength and lighten it toward white only as far as the floor needs.
        half strokePeak = max(strokeColour.r, max(strokeColour.g, strokeColour.b));
        strokeColour /= max(strokePeak, 1.0h);
        half cappedLuma = dot(strokeColour, half3(0.2126h, 0.7152h, 0.0722h));
        strokeColour = lerp(strokeColour, (half3)1.0h,
            saturate((_ContentMinStrokeLuma - cappedLuma) / max(1.0h - cappedLuma, (half)1e-3)));
        // Scaled down, a one-texel line spreads to part coverage: a soft curve (half reads as seven eighths) keeps
        // thin strokes solid while their edges stay graded.
        half uncovered = 1.0h - coverage;
        half shown = 1.0h - uncovered * uncovered * uncovered;
        half3 composite = contentBg * (1.0h - shown) + strokeColour * shown;
        // A dark panel fill is the tube's own black, not a coloured plate: only luminous content shows its colour.
        half lit = smoothstep(CONTENT_FILL_LUMA.x, CONTENT_FILL_LUMA.y, contentLum);
        half3 contentBody = lerp(contentBg, composite, lit) + phosphorHalation * (half)contentRimVig;
        half contentSafeMax = min(_BloomSafeMax, 1.02h);
        half contentBodyMax = max(contentBody.r, max(contentBody.g, contentBody.b));
        if (contentBodyMax > contentSafeMax)
        {
            contentBody *= (contentSafeMax / max(contentBodyMax, (half)1e-4));
        }

        phosphorHdr = lerp(phosphorHdr, contentBody, (half)contentMix);
    }

    // 4. Channel-switch horizontal phosphor line (collapse / expand pinch between eyes and content)
    if (lineGlow > 0.001)
    {
        float2 rawCanvas = float2(jitteredUv.x * CRT_ASPECT, 1.0 - jitteredUv.y);
        float lineHalfW = lerp(0.46, 0.43, verticalPinch) * CRT_ASPECT;
        float sdfSwitchLine = SdfRoundedBox(
            rawCanvas - float2(0.5 * CRT_ASPECT, 0.5),
            float2(lineHalfW, BOOT_LINE_HALF_HEIGHT * 1.15),
            BOOT_LINE_HALF_HEIGHT);
        float switchLineCov = smoothstep(aaWidth * 1.5, -aaWidth, sdfSwitchLine);
        float switchLineHalo = exp(-max(sdfSwitchLine, 0.0) / max(glowRadius * 1.1, 0.005));
        half3 switchLineCol = _PhosphorGlowColor.rgb * (half)(switchLineHalo * 0.65)
            + _PhosphorColor.rgb * (half)(switchLineCov * 0.85)
            + hotCoreTarget * (half)(switchLineCov * 0.55);
        phosphorHdr = lerp(phosphorHdr, max(phosphorHdr, switchLineCol), (half)lineGlow);
    }

    // CRT horizontal scanlines (coarsening slightly in low power, halved again in content mode so glyphs stay whole)
    float scanCount = lerp((float)_ScanlineCount, LOW_POWER_SCANLINE_MIN, lowPower);
    float scanWave = 0.5 + 0.5 * sin((jitteredUv.y * scanCount + scanlinePhase) * CRT_PI * 2.0);
    float scanDepth = lerp((float)_ScanlineDepth, (float)_ScanlineDepth * 1.45, lowPower);
    scanDepth *= lerp(1.0, 1.0 - saturate((float)_ContentScanlineAtten), contentMix);
    float scanMod = lerp(1.0 - scanDepth, 1.0 + scanDepth * 0.25, scanWave);

    float bandY = frac(rollingBandPhase);
    float bandDist = abs((1.0 - jitteredUv.y) - bandY);
    float bandBoost = 1.0 + ROLLING_BAND_STRENGTH * (1.0 - contentMix) * exp(-(bandDist * bandDist) / (ROLLING_BAND_WIDTH * ROLLING_BAND_WIDTH));

    // Thin rounded screen bezel edge vignette right at the rubber gasket
    float bezelMask = smoothstep(0.004, -0.012, bezelSdf);

    half3 finalEmission = phosphorHdr * (half)(brightness * scanMod * bandBoost * bezelMask);

    // Soft, small glass specular reflection at the bezel edge only, never over the content
    float3 normalWS = NormalizeNormalPerVertex(input.normalWS);
    float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    Light mainLight = GetMainLight();
    float3 halfDir = SafeNormalize(mainLight.direction + viewDirWS);
    float ndoth = saturate(dot(normalWS, halfDir));
    float specPow = exp2(10.0 * _Smoothness + 1.0);
    float topSpecY = smoothstep(0.955, 0.978, jitteredUv.y) * smoothstep(0.996, 0.982, jitteredUv.y);
    float topSpecX = smoothstep(0.28, 0.06, abs(jitteredUv.x - 0.5));
    float topEdgeSpec = topSpecY * topSpecX;
    float bezelRimBand = smoothstep(-0.030, -0.012, bezelSdf) * smoothstep(0.002, -0.005, bezelSdf);
    float topBezelBias = saturate(0.35 + 0.65 * saturate((jitteredUv.y - 0.5) * 1.6));
    float contentBezelSpec = bezelRimBand * topBezelBias * (0.45 + 0.55 * ndoth);
    half mainLightLum = dot(mainLight.color, half3(0.2126h, 0.7152h, 0.0722h));
    half3 neutralSheenColor = lerp(mainLight.color, half3(1.02h, 0.98h, 0.92h) * mainLightLum, (half)(contentMix * 0.75));
    float specFactor = lerp(pow(ndoth, specPow) * (0.25 + 0.75 * topEdgeSpec), contentBezelSpec, contentMix);
    half3 glassSpec = neutralSheenColor * (half)(specFactor * GLASS_SPECULAR_STRENGTH * lerp(1.0, 0.22, contentMix));

    half3 rimBase = lerp(bgPhosphor * 0.35h, CONTENT_EDGE_LINEAR, (half)contentMix);
    half3 color = finalEmission + glassSpec + rimBase * (half)(1.0 - bezelMask);
    color = lerp(MixFog(color, input.fogFactor), color, (half)contentMix);
    return half4(color, 1.0h);
}

#endif
