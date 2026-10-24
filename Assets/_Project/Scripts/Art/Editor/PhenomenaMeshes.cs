using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Procedural mesh generators for the 0.6 lunar phenomena VFX prefabs:
    /// 1. Continuous terminator horizon haze band and irregular, sparse drifting electrostatic mote wisps.
    /// 2. Irregular soft-rimmed micrometeorite crater with feathered terrain-decal skirt, asymmetric impact glass
    ///    shards/blebs, and compact low-profile bloom flash burst.
    /// 3. Braided non-uniform down-slope sheeting regolith avalanche tongues, continuous vacuum toe settle apron,
    ///    and faceted mineral grain shards.
    /// 4. Rising Earth disc preview mesh for the Earthrise stillness beat.
    /// </summary>
    public static class PhenomenaMeshes
    {
        /// <summary>
        /// Builds a gently curved, multi-layered continuous terminator horizon haze band mesh spanning
        /// <paramref name="width"/> metres across and <paramref name="height"/> metres vertically.
        /// </summary>
        public static Mesh CreateHorizonHazeBandMesh(float width, float height, int horizontalSegments = 36)
        {
            int segsX = Mathf.Max(8, horizontalSegments);
            const int segsY = 6;
            const int layers = 3;
            int vertsPerLayer = (segsX + 1) * (segsY + 1);
            int trisPerLayer = segsX * segsY * 6;

            var vertices = new Vector3[vertsPerLayer * layers];
            var normals = new Vector3[vertsPerLayer * layers];
            var uvs = new Vector2[vertsPerLayer * layers];
            var colors = new Color32[vertsPerLayer * layers];
            var triangles = new int[trisPerLayer * layers];

            int v = 0;
            int t = 0;
            int stride = segsX + 1;

            float[] widthScales = { 1.00f, 0.92f, 0.82f };
            float[] heightScales = { 1.00f, 0.68f, 0.42f };
            float[] zOffsets = { 0.55f, 0.0f, -0.35f };
            float[] alphaScales = { 145f, 175f, 135f };

            for (int layer = 0; layer < layers; layer++)
            {
                int layerBaseVert = v;
                float layerWidth = width * widthScales[layer];
                float layerHeight = height * heightScales[layer];
                float halfWidth = layerWidth * 0.5f;
                float arcDepth = layerWidth * 0.085f;
                float zOffset = zOffsets[layer];

                for (int y = 0; y <= segsY; y++)
                {
                    float vCoord = y / (float)segsY;
                    float verticalAlpha = Mathf.Clamp01(Mathf.Sin(vCoord * Mathf.PI) * 1.12f);

                    for (int x = 0; x <= segsX; x++)
                    {
                        float uCoord = x / (float)segsX;
                        float centeredX = (uCoord - 0.5f) * 2f;
                        float posX = centeredX * halfWidth;

                        // Gentle organic height undulation along the horizon ridge
                        float ridgeWave = 1.0f
                            + 0.12f * Mathf.Sin(uCoord * 7.5f + layer * 1.3f)
                            + 0.08f * Mathf.Cos(uCoord * 13.0f - layer * 0.9f);
                        float posY = vCoord * layerHeight * ridgeWave;
                        float posZ = zOffset + (1f - centeredX * centeredX) * arcDepth;

                        float lateralMod = 0.82f
                            + 0.18f * Mathf.Sin(uCoord * 9.2f + 0.6f) * Mathf.Cos(uCoord * 4.7f - 0.4f);
                        byte alphaByte = (byte)Mathf.Clamp(
                            Mathf.RoundToInt(verticalAlpha * lateralMod * alphaScales[layer]),
                            0,
                            255);

                        vertices[v] = new Vector3(posX, posY, posZ);
                        normals[v] = new Vector3(-centeredX * 0.18f, 0.15f, -1f).normalized;
                        uvs[v] = new Vector2(uCoord, vCoord);
                        colors[v] = new Color32(255, 250, 240, alphaByte);
                        v++;
                    }
                }

                for (int y = 0; y < segsY; y++)
                {
                    for (int x = 0; x < segsX; x++)
                    {
                        int i0 = layerBaseVert + y * stride + x;
                        int i1 = i0 + 1;
                        int i2 = i0 + stride;
                        int i3 = i2 + 1;

                        triangles[t++] = i0;
                        triangles[t++] = i2;
                        triangles[t++] = i1;

                        triangles[t++] = i1;
                        triangles[t++] = i2;
                        triangles[t++] = i3;
                    }
                }
            }

            var mesh = new Mesh { name = "Mesh_DustGlowHorizonBand" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.colors32 = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Builds sparse, irregularly spaced, curved/tilted mote wisp veils of varying height, width, and brightness
        /// emerging organically along the terminator glow band (zero evenly spaced vertical bars).
        /// </summary>
        public static Mesh CreateMoteColumnsMesh(int columnCount, float spanWidth, float columnRadius, float columnHeight)
        {
            int cols = Mathf.Max(2, columnCount);
            const int segsY = 6;

            var vertices = new List<Vector3>(cols * (segsY + 1) * 4);
            var normals = new List<Vector3>(cols * (segsY + 1) * 4);
            var uvs = new List<Vector2>(cols * (segsY + 1) * 4);
            var colors = new List<Color32>(cols * (segsY + 1) * 4);
            var triangles = new List<int>(cols * segsY * 12);

            // Asymmetric, non-periodic lateral anchor offsets within the visible horizon span, with natural clusters & wide gaps
            float[] irregularAnchors =
            {
                -0.23f, -0.17f, -0.07f, 0.04f, 0.09f, 0.19f, 0.25f,
                -0.12f, 0.14f, -0.27f, -0.02f, 0.28f, -0.20f, 0.11f, -0.15f, 0.22f,
            };
            float[] heightVariations =
            {
                0.52f, 1.08f, 0.68f, 1.18f, 0.44f, 0.92f, 0.58f,
                0.76f, 0.46f, 0.94f, 0.60f, 0.70f, 1.10f, 0.50f, 0.82f, 0.64f,
            };
            float[] brightnessVariations =
            {
                0.68f, 1.00f, 0.80f, 1.00f, 0.58f, 0.94f, 0.64f,
                0.76f, 0.60f, 0.86f, 0.72f, 0.82f, 0.96f, 0.62f, 0.84f, 0.70f,
            };

            for (int c = 0; c < cols; c++)
            {
                float hash = Frac(Mathf.Sin((c + 1) * 19.73f) * 43758.5453f);
                float anchorX = irregularAnchors[c % irregularAnchors.Length];
                float centerX = anchorX * (spanWidth * 0.72f) + (hash - 0.5f) * (spanWidth * 0.022f);
                float centerZ = (Frac(hash * 7.13f) - 0.5f) * 1.15f + (1f - anchorX * anchorX * 4f) * (spanWidth * 0.02f);

                float heightRatio = heightVariations[c % heightVariations.Length];
                float colH = columnHeight * heightRatio;
                float colR = columnRadius * Mathf.Lerp(0.85f, 1.45f, Frac(hash * 5.47f));
                float brightScale = brightnessVariations[c % brightnessVariations.Length];

                // Gentle lateral tilt and S-curve drift so no wisp stands as a rigid vertical bar
                float leanSign = (c % 2 == 0) ? 1f : -1f;
                float lateralLean = leanSign * Mathf.Lerp(0.45f, 1.15f, Frac(hash * 9.1f));
                float curveAmp = Mathf.Lerp(0.24f, 0.58f, Frac(hash * 11.3f));

                byte seedByte = (byte)Mathf.RoundToInt(hash * 255f);
                byte heightByte = (byte)Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(heightRatio / 1.15f) * 255f), 40, 255);

                for (int plane = 0; plane < 2; plane++)
                {
                    float yawRad = ((plane * 82f) + 22f + hash * 35f) * Mathf.Deg2Rad;
                    Vector3 rightDir = new Vector3(Mathf.Cos(yawRad), 0f, Mathf.Sin(yawRad));
                    Vector3 normal = new Vector3(-Mathf.Sin(yawRad), 0.12f, Mathf.Cos(yawRad)).normalized;

                    int baseIdx = vertices.Count;
                    for (int y = 0; y <= segsY; y++)
                    {
                        float vCoord = y / (float)segsY;
                        float py = vCoord * colH;
                        float driftX = vCoord * lateralLean + Mathf.Sin(vCoord * Mathf.PI * 1.35f + hash * 6.28f) * curveAmp;
                        float driftZ = Mathf.Cos(vCoord * Mathf.PI + hash * 4.1f) * (curveAmp * 0.45f);

                        // Wider, softer base fanning gently and tapering at the top of the wisp
                        float widthProfile = Mathf.Lerp(1.25f, 0.55f, vCoord) * (0.85f + 0.25f * Mathf.Sin(vCoord * Mathf.PI));
                        Vector3 right = rightDir * (colR * widthProfile);

                        float vertAlpha = Mathf.SmoothStep(0f, 0.18f, vCoord) * Mathf.Pow(1f - vCoord, 0.78f);
                        byte alphaByte = (byte)Mathf.Clamp(Mathf.RoundToInt(vertAlpha * brightScale * 255f), 0, 255);

                        vertices.Add(new Vector3(centerX + driftX - right.x, py, centerZ + driftZ - right.z));
                        vertices.Add(new Vector3(centerX + driftX + right.x, py, centerZ + driftZ + right.z));
                        normals.Add(normal);
                        normals.Add(normal);
                        uvs.Add(new Vector2(0f, vCoord));
                        uvs.Add(new Vector2(1f, vCoord));
                        colors.Add(new Color32(64, heightByte, seedByte, alphaByte));
                        colors.Add(new Color32(64, heightByte, seedByte, alphaByte));
                    }

                    for (int y = 0; y < segsY; y++)
                    {
                        int i0 = baseIdx + y * 2;
                        int i1 = i0 + 1;
                        int i2 = i0 + 2;
                        int i3 = i0 + 3;

                        triangles.Add(i0);
                        triangles.Add(i2);
                        triangles.Add(i1);

                        triangles.Add(i1);
                        triangles.Add(i2);
                        triangles.Add(i3);
                    }
                }
            }

            var mesh = new Mesh { name = "Mesh_DustGlowMoteColumns" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Builds a low-poly faceted octahedral/diamond mineral shard mesh used by ParticleSystemRenderers so particles
        /// render as crisp angular crystals/grains and never as round billboard dots.
        /// </summary>
        public static Mesh CreateFacetedMoteShardMesh()
        {
            Vector3 top = new Vector3(0f, 1.0f, 0f);
            Vector3 bottom = new Vector3(0f, -1.0f, 0f);
            Vector3 n0 = new Vector3(0.55f, 0.1f, 0f);
            Vector3 n1 = new Vector3(0f, -0.08f, 0.48f);
            Vector3 n2 = new Vector3(-0.52f, 0.06f, 0f);
            Vector3 n3 = new Vector3(0f, -0.05f, -0.46f);

            var vertices = new List<Vector3>(24);
            var normals = new List<Vector3>(24);
            var uvs = new List<Vector2>(24);
            var colors = new List<Color32>(24);
            var triangles = new List<int>(24);

            AddFacetTriangle(vertices, normals, uvs, colors, triangles, top, n1, n0, new Color32(255, 255, 255, 255));
            AddFacetTriangle(vertices, normals, uvs, colors, triangles, top, n2, n1, new Color32(240, 245, 255, 255));
            AddFacetTriangle(vertices, normals, uvs, colors, triangles, top, n3, n2, new Color32(230, 238, 250, 255));
            AddFacetTriangle(vertices, normals, uvs, colors, triangles, top, n0, n3, new Color32(250, 248, 240, 255));

            AddFacetTriangle(vertices, normals, uvs, colors, triangles, bottom, n0, n1, new Color32(225, 230, 240, 255));
            AddFacetTriangle(vertices, normals, uvs, colors, triangles, bottom, n1, n2, new Color32(215, 222, 235, 255));
            AddFacetTriangle(vertices, normals, uvs, colors, triangles, bottom, n2, n3, new Color32(220, 226, 238, 255));
            AddFacetTriangle(vertices, normals, uvs, colors, triangles, bottom, n3, n0, new Color32(235, 238, 245, 255));

            var mesh = new Mesh { name = "Mesh_FacetedMoteShard" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Builds an irregular, soft-rimmed micrometeorite crater mesh with an alpha-feathered terrain-decal ejecta
        /// skirt (blending seamlessly into the ground with zero hard octagon/polygon edge), small rim clod meshes, and
        /// asymmetrically clustered, irregular glowing impact glass shards and melt blebs.
        /// Vertex colors: R=AO, G=EdgeWear, B=Cavity, A=ShardHeatScale.
        /// UVs: uv.x = GlassMask (0=regolith/decal, 1=impact glass), uv.y = DecalFeatherAlpha (1=core, 0=outer edge).
        /// </summary>
        public static Mesh CreateImpactCraterAndGlassMesh(float craterRadius)
        {
            const int radialSectors = 36;
            const int rings = 8;
            float radius = Mathf.Max(0.25f, craterRadius);

            var vertices = new List<Vector3>(1400);
            var normals = new List<Vector3>(1400);
            var uvs = new List<Vector2>(1400);
            var colors = new List<Color32>(1400);
            var triangles = new List<int>(2400);

            // Ring profile from center (r=0) out to feathered terrain-decal skirt (r=8)
            float[] ringRadii = { 0.0f, 0.19f, 0.36f, 0.58f, 0.84f, 1.12f, 1.46f, 1.88f, 2.35f };
            float[] ringHeights = { 0.010f, 0.006f, -0.022f, -0.048f, 0.062f, 0.024f, 0.006f, 0.002f, 0.0005f };
            float[] ringGlassMask = { 1.0f, 0.96f, 0.42f, 0.05f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
            float[] ringDecalAlpha = { 1.0f, 1.0f, 1.0f, 1.0f, 0.94f, 0.72f, 0.42f, 0.15f, 0.0f };

            Color32[] ringColors =
            {
                new Color32(235, 245, 240, 255), // r0: Central molten glass puddle core
                new Color32(210, 232, 220, 240), // r1: Irregular melt puddle lobe
                new Color32(135, 195, 165, 210), // r2: Melt margin / dark pit floor
                new Color32(85, 130, 110, 180),  // r3: Deep shadowed crater pit wall
                new Color32(215, 215, 210, 180), // r4: Soft overturned crater rim crest
                new Color32(190, 182, 190, 170), // r5: Outer rim flank
                new Color32(175, 162, 175, 160), // r6: Inner terrain-decal ejecta apron
                new Color32(168, 152, 168, 150), // r7: Mid terrain-decal feather skirt
                new Color32(162, 148, 162, 140), // r8: Outer zero-alpha tie-in edge
            };

            var ringPoints = new Vector3[rings + 1, radialSectors];
            var ringUvs = new Vector2[rings + 1, radialSectors];
            var ringVertColors = new Color32[rings + 1, radialSectors];

            for (int r = 0; r <= rings; r++)
            {
                for (int s = 0; s < radialSectors; s++)
                {
                    float baseAngle = (s / (float)radialSectors) * Mathf.PI * 2f;
                    float hash = Frac(Mathf.Sin((r * 23 + s * 41 + 7) * 37.89f) * 43758.5453f);

                    // Multi-harmonic organic asymmetry so the crater and melt pool are never circular or octagonal
                    float harmonicShape = 1.0f
                        + 0.15f * Mathf.Cos(baseAngle * 2f + 0.45f)
                        + 0.11f * Mathf.Sin(baseAngle * 3f - 0.85f)
                        + 0.08f * Mathf.Cos(baseAngle * 5f + 1.65f)
                        + 0.05f * Mathf.Sin(baseAngle * 7f + 0.30f);

                    float radJitter = r == 0 ? 0f : Mathf.Lerp(0.90f, 1.10f, Frac(hash * 7.7f));
                    float angleJitter = r == 0 ? 0f : (hash - 0.5f) * (Mathf.PI * 2f / radialSectors) * 0.32f;
                    float angle = baseAngle + angleJitter;

                    // Slight off-center shift for the inner melt puddle (oblique impact signature)
                    Vector2 centerShift = r <= 2
                        ? new Vector2(0.045f * radius * (1f - r * 0.3f), -0.03f * radius * (1f - r * 0.3f))
                        : Vector2.zero;

                    // Asymmetric rim height (higher on downrange northeast arc, lower/scoured on southwest arc)
                    float rimAsym = 0.78f + 0.36f * Mathf.Cos(baseAngle - 0.55f) + 0.18f * Mathf.Sin(baseAngle * 3f + 1.1f);
                    float heightScale = (r == 4 || r == 5) ? Mathf.Max(0.35f, rimAsym) : 1.0f;
                    float heightJitter = (r == 0 || r >= 7) ? 0f : (Frac(hash * 13.1f) - 0.5f) * 0.012f * radius;

                    float rr = ringRadii[r] * radius * harmonicShape * radJitter;
                    ringPoints[r, s] = new Vector3(
                        centerShift.x + Mathf.Cos(angle) * rr,
                        ringHeights[r] * radius * heightScale + heightJitter,
                        centerShift.y + Mathf.Sin(angle) * rr);

                    // Break up the outer melt margin (r=1..2) into irregular lobes
                    float meltLobe = 0.65f + 0.35f * Mathf.Cos(baseAngle * 3f + 0.8f) * Mathf.Sin(baseAngle * 2f - 0.5f);
                    float glassVal = ringGlassMask[r];
                    if (r == 2)
                    {
                        glassVal *= Mathf.SmoothStep(0.35f, 0.85f, meltLobe);
                    }

                    // Vary outer decal alpha with angular ejecta rays
                    float rayFactor = r >= 5
                        ? Mathf.Lerp(0.68f, 1.15f, 0.5f + 0.5f * Mathf.Cos(baseAngle * 4f + 0.4f) * Mathf.Sin(baseAngle * 3f - 1.1f))
                        : 1.0f;
                    float decalVal = r == rings ? 0.0f : Mathf.Clamp01(ringDecalAlpha[r] * rayFactor);

                    ringUvs[r, s] = new Vector2(glassVal, decalVal);
                    ringVertColors[r, s] = ringColors[r];
                }
            }

            // Smoothly shaded crater & terrain-decal surface so the soft rim and decal skirt blend naturally into ground
            int[,] gridVertIndices = new int[rings + 1, radialSectors];
            for (int r = 0; r <= rings; r++)
            {
                for (int s = 0; s < radialSectors; s++)
                {
                    int prevS = (s - 1 + radialSectors) % radialSectors;
                    int nextS = (s + 1) % radialSectors;
                    int innerR = Mathf.Max(0, r - 1);
                    int outerR = Mathf.Min(rings, r + 1);

                    Vector3 tangentTheta = ringPoints[r, nextS] - ringPoints[r, prevS];
                    Vector3 tangentRad = ringPoints[outerR, s] - ringPoints[innerR, s];
                    Vector3 smoothNormal = Vector3.Cross(tangentTheta, tangentRad).normalized;
                    if (smoothNormal.y < 0f)
                    {
                        smoothNormal = -smoothNormal;
                    }

                    if (r == 0 || r >= 6)
                    {
                        smoothNormal = Vector3.Lerp(smoothNormal, Vector3.up, 0.75f).normalized;
                    }

                    gridVertIndices[r, s] = vertices.Count;
                    vertices.Add(ringPoints[r, s]);
                    normals.Add(smoothNormal);
                    uvs.Add(ringUvs[r, s]);
                    colors.Add(ringVertColors[r, s]);
                }
            }

            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < radialSectors; s++)
                {
                    int nextS = (s + 1) % radialSectors;
                    int i00 = gridVertIndices[r, s];
                    int i01 = gridVertIndices[r, nextS];
                    int i10 = gridVertIndices[r + 1, s];
                    int i11 = gridVertIndices[r + 1, nextS];

                    if (r == 0)
                    {
                        triangles.Add(i00);
                        triangles.Add(i11);
                        triangles.Add(i10);
                    }
                    else
                    {
                        triangles.Add(i00);
                        triangles.Add(i11);
                        triangles.Add(i10);

                        triangles.Add(i00);
                        triangles.Add(i01);
                        triangles.Add(i11);
                    }
                }
            }

            // Add 6 small irregular overturned regolith clods tucked along the crater rim for small-mesh silhouette breakup
            float[] clodAngles = { 0.25f, 0.95f, 1.85f, 3.10f, 4.35f, 5.40f };
            for (int c = 0; c < clodAngles.Length; c++)
            {
                float seed = Frac(Mathf.Sin((c + 1) * 47.19f) * 43758.5453f);
                float angle = clodAngles[c] + (seed - 0.5f) * 0.22f;
                int sector = Mathf.Clamp(Mathf.RoundToInt((angle / (Mathf.PI * 2f)) * radialSectors) % radialSectors, 0, radialSectors - 1);
                Vector3 rimPos = Vector3.Lerp(ringPoints[4, sector], ringPoints[5, sector], Frac(seed * 3.7f));
                float clodW = Mathf.Lerp(0.055f, 0.11f, Frac(seed * 5.3f)) * radius;
                float clodH = Mathf.Lerp(0.025f, 0.055f, Frac(seed * 8.9f)) * radius;

                AddIrregularShardOrClod(
                    vertices,
                    normals,
                    uvs,
                    colors,
                    triangles,
                    rimPos + new Vector3(0f, -0.008f * radius, 0f),
                    clodW,
                    clodH,
                    new Vector3(Mathf.Cos(angle) * 0.25f, 1f, Mathf.Sin(angle) * 0.25f).normalized,
                    seed,
                    new Vector2(0f, 0.96f),
                    new Color32(195, 210, 195, 160),
                    new Color32(230, 235, 225, 180));
            }

            // Add 14 irregular, asymmetrically clustered vitreous impact glass shards, crags, and melt blebs
            // spread unevenly across the pit floor and inner wall (never a circular ring of identical cones)
            (Vector2 offset, float widthScale, float heightScale, float tiltX, float tiltZ, byte heatByte)[] glassSpecs =
            {
                // Primary off-center fused melt crag & splinter cluster
                (new Vector2(0.08f, -0.04f), 0.17f, 0.20f, 0.32f, -0.20f, 255),
                (new Vector2(0.21f, 0.06f), 0.14f, 0.14f, 0.48f, 0.25f, 245),
                (new Vector2(-0.05f, -0.15f), 0.15f, 0.11f, -0.24f, -0.38f, 240),
                (new Vector2(0.16f, -0.19f), 0.10f, 0.16f, 0.42f, -0.45f, 230),
                (new Vector2(0.31f, -0.07f), 0.12f, 0.09f, 0.56f, -0.14f, 210),

                // Secondary smaller cluster on northwest inner floor/wall
                (new Vector2(-0.25f, 0.14f), 0.13f, 0.13f, -0.48f, 0.32f, 220),
                (new Vector2(-0.16f, 0.24f), 0.09f, 0.15f, -0.30f, 0.52f, 200),
                (new Vector2(-0.34f, 0.05f), 0.11f, 0.07f, -0.58f, 0.18f, 185),

                // Irregular scattered melt splatters & droplets further out toward the inner rim
                (new Vector2(0.03f, 0.32f), 0.08f, 0.06f, 0.15f, 0.56f, 170),
                (new Vector2(0.38f, 0.18f), 0.09f, 0.09f, 0.62f, 0.30f, 180),
                (new Vector2(-0.24f, -0.29f), 0.10f, 0.07f, -0.42f, -0.48f, 165),
                (new Vector2(0.32f, -0.30f), 0.07f, 0.06f, 0.48f, -0.52f, 155),
                (new Vector2(-0.41f, -0.12f), 0.08f, 0.08f, -0.64f, -0.24f, 160),
                (new Vector2(-0.02f, 0.06f), 0.16f, 0.07f, 0.12f, 0.18f, 250),
            };

            for (int i = 0; i < glassSpecs.Length; i++)
            {
                (Vector2 offset, float widthScale, float heightScale, float tiltX, float tiltZ, byte heatByte) = glassSpecs[i];
                float seed = Frac(Mathf.Sin((i + 1) * 63.71f) * 43758.5453f);
                Vector3 center = new Vector3(offset.x * radius, -0.006f * radius, offset.y * radius);
                Vector3 tiltDir = new Vector3(tiltX, 1f, tiltZ).normalized;

                Color32 baseGlass = new Color32(195, 228, 215, heatByte);
                Color32 tipGlass = new Color32(255, 255, 250, heatByte);

                AddIrregularShardOrClod(
                    vertices,
                    normals,
                    uvs,
                    colors,
                    triangles,
                    center,
                    widthScale * radius,
                    heightScale * radius,
                    tiltDir,
                    seed,
                    new Vector2(1.0f, 1.0f),
                    baseGlass,
                    tipGlass);
            }

            var mesh = new Mesh { name = "Mesh_ImpactCraterAndGlass" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Builds a compact, low-profile 3D starlight burst mesh for the brief micrometeorite impact flash
        /// (flattened vertically and tilted so planes never sum along a vertical axis into a light pillar).
        /// </summary>
        public static Mesh CreateImpactFlashStarMesh()
        {
            var vertices = new List<Vector3>(36);
            var normals = new List<Vector3>(36);
            var uvs = new List<Vector2>(36);
            var colors = new List<Color32>(36);
            var triangles = new List<int>(54);

            // Three low-pitched, tilted burst planes + one horizontal ground-skimming flash plane (no vertical pillar)
            Quaternion[] planeRots =
            {
                Quaternion.Euler(58f, 15f, 0f),
                Quaternion.Euler(52f, 135f, 12f),
                Quaternion.Euler(62f, 255f, -10f),
                Quaternion.Euler(86f, 45f, 0f),
            };
            float[] planeScales = { 1.00f, 0.88f, 0.82f, 0.95f };

            for (int p = 0; p < planeRots.Length; p++)
            {
                Quaternion rot = planeRots[p];
                float s = planeScales[p];
                Vector3 right = (rot * Vector3.right) * s;
                Vector3 up = (rot * Vector3.up) * (s * 0.68f);
                Vector3 normal = (rot * Vector3.forward).normalized;

                int baseIdx = vertices.Count;
                vertices.Add(-right - up);
                vertices.Add(right - up);
                vertices.Add(-right + up);
                vertices.Add(right + up);

                for (int k = 0; k < 4; k++)
                {
                    normals.Add(normal);
                    colors.Add(new Color32(255, 250, 240, 235));
                }

                uvs.Add(new Vector2(0f, 0f));
                uvs.Add(new Vector2(1f, 0f));
                uvs.Add(new Vector2(0f, 1f));
                uvs.Add(new Vector2(1f, 1f));

                triangles.Add(baseIdx);
                triangles.Add(baseIdx + 2);
                triangles.Add(baseIdx + 1);

                triangles.Add(baseIdx + 1);
                triangles.Add(baseIdx + 2);
                triangles.Add(baseIdx + 3);
            }

            var mesh = new Mesh { name = "Mesh_ImpactFlashStar" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Builds an organic, non-uniform down-slope sheeting regolith stream mesh for the gentle moonquake dust slide:
        /// a continuous multi-lobed base avalanche sheet with asymmetric meandering chutes plus irregular overlapping
        /// low parabolic vacuum saltation veils (zero evenly spaced parallel stripes).
        /// </summary>
        public static Mesh CreateSlopeSheetMesh(float width, float length, float slopeAngleDegrees)
        {
            const int baseSegsX = 28;
            const int baseSegsY = 18;
            const int arcVeilCount = 5;
            const int arcSegsY = 14;

            float slopeRad = slopeAngleDegrees * Mathf.Deg2Rad;
            float sinSlope = Mathf.Sin(slopeRad);
            float cosSlope = Mathf.Cos(slopeRad);
            float topY = length * sinSlope;
            Vector3 slopeNormal = new Vector3(0f, cosSlope, sinSlope).normalized;

            var vertices = new List<Vector3>(1200);
            var normals = new List<Vector3>(1200);
            var uvs = new List<Vector2>(1200);
            var colors = new List<Color32>(1200);
            var triangles = new List<int>(3200);

            // 1. Continuous, asymmetric multi-tongue base regolith sheet across the slope
            int baseStartVert = vertices.Count;
            int stride = baseSegsX + 1;

            for (int y = 0; y <= baseSegsY; y++)
            {
                float vCoord = y / (float)baseSegsY;
                float distAlong = vCoord * length;
                float longitudinalFade = Mathf.SmoothStep(0f, 0.14f, vCoord) * Mathf.SmoothStep(1f, 0.82f, vCoord);

                // Meandering chute centers down the slope (asymmetric primary, secondary, and minor forks)
                float chute1Center = -0.22f + 0.08f * Mathf.Sin(vCoord * 4.2f + 0.4f) - 0.05f * vCoord;
                float chute1Width = Mathf.Lerp(0.24f, 0.36f, vCoord);

                float chute2Center = 0.24f - 0.07f * Mathf.Cos(vCoord * 3.6f - 0.5f) + 0.06f * vCoord;
                float chute2Width = Mathf.Lerp(0.15f, 0.25f, vCoord);
                float chute2Onset = Mathf.SmoothStep(0.08f, 0.32f, vCoord);

                float chute3Center = 0.03f + 0.09f * Mathf.Sin(vCoord * 5.5f + 1.7f);
                float chute3Width = 0.14f;
                float chute3Fade = Mathf.SmoothStep(0.0f, 0.20f, vCoord) * Mathf.SmoothStep(0.88f, 0.48f, vCoord);

                for (int x = 0; x <= baseSegsX; x++)
                {
                    float uCoord = x / (float)baseSegsX;
                    float normX = uCoord - 0.5f;

                    float d1 = (normX - chute1Center) / chute1Width;
                    float w1 = Mathf.Exp(-d1 * d1 * 2.1f);

                    float d2 = (normX - chute2Center) / chute2Width;
                    float w2 = Mathf.Exp(-d2 * d2 * 2.4f) * chute2Onset * 0.85f;

                    float d3 = (normX - chute3Center) / chute3Width;
                    float w3 = Mathf.Exp(-d3 * d3 * 2.6f) * chute3Fade * 0.58f;

                    float combinedChute = Mathf.Clamp01(w1 + w2 + w3);
                    float skimLift = 0.045f + 0.055f * combinedChute;

                    float px = normX * width;
                    float py = topY - distAlong * sinSlope + skimLift * cosSlope;
                    float pz = distAlong * cosSlope + skimLift * sinSlope;

                    byte alphaByte = (byte)Mathf.Clamp(
                        Mathf.RoundToInt(longitudinalFade * combinedChute * 245f),
                        0,
                        255);

                    vertices.Add(new Vector3(px, py, pz));
                    normals.Add(slopeNormal);
                    uvs.Add(new Vector2(uCoord, vCoord));
                    colors.Add(new Color32(255, 252, 245, alphaByte));
                }
            }

            for (int y = 0; y < baseSegsY; y++)
            {
                for (int x = 0; x < baseSegsX; x++)
                {
                    int i0 = baseStartVert + y * stride + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + stride;
                    int i3 = i2 + 1;

                    triangles.Add(i0);
                    triangles.Add(i2);
                    triangles.Add(i1);

                    triangles.Add(i1);
                    triangles.Add(i2);
                    triangles.Add(i3);
                }
            }

            // 2. Five irregular, asymmetric low parabolic vacuum saltation veils of varied widths, lengths, and angles
            (float xStart, float xEnd, float vStart, float vSpan, float halfWidthScale, float peakLift, byte seedByte)[] veilSpecs =
            {
                (-0.26f, -0.32f, 0.04f, 0.78f, 0.22f, 0.24f, 37),
                (-0.14f, -0.05f, 0.12f, 0.66f, 0.16f, 0.18f, 91),
                (0.22f, 0.31f, 0.18f, 0.72f, 0.19f, 0.21f, 149),
                (0.02f, -0.09f, 0.08f, 0.54f, 0.14f, 0.15f, 203),
                (0.16f, 0.12f, 0.28f, 0.58f, 0.13f, 0.16f, 241),
            };

            for (int a = 0; a < arcVeilCount; a++)
            {
                (float xStart, float xEnd, float vStart, float vSpan, float halfWidthScale, float peakLift, byte seedByte) = veilSpecs[a];
                int baseVert = vertices.Count;

                for (int s = 0; s <= arcSegsY; s++)
                {
                    float uArc = s / (float)arcSegsY;
                    float vCoord = Mathf.Clamp01(vStart + uArc * vSpan);
                    float distAlong = vCoord * length;

                    // Clean vacuum parabola: 4 * h_max * u * (1 - u)
                    float parabolaLift = 0.05f + 4f * peakLift * uArc * (1f - uArc);
                    float centerNormX = Mathf.Lerp(xStart, xEnd, uArc)
                        + 0.035f * Mathf.Sin(uArc * Mathf.PI * 1.5f + a * 1.7f);
                    float centerX = centerNormX * width;
                    float halfW = width * halfWidthScale * Mathf.Lerp(0.72f, 1.28f, uArc);

                    float py = topY - distAlong * sinSlope + parabolaLift * cosSlope;
                    float pz = distAlong * cosSlope + parabolaLift * sinSlope;

                    byte edgeAlpha = (byte)Mathf.RoundToInt(Mathf.Sin(uArc * Mathf.PI) * 210f);
                    Color32 col = new Color32(255, 252, 245, edgeAlpha);

                    vertices.Add(new Vector3(centerX - halfW, py, pz));
                    vertices.Add(new Vector3(centerX + halfW, py, pz));
                    normals.Add(slopeNormal);
                    normals.Add(slopeNormal);
                    uvs.Add(new Vector2(0f, uArc));
                    uvs.Add(new Vector2(1f, uArc));
                    colors.Add(col);
                    colors.Add(col);
                }

                for (int s = 0; s < arcSegsY; s++)
                {
                    int i0 = baseVert + s * 2;
                    int i1 = i0 + 1;
                    int i2 = i0 + 2;
                    int i3 = i0 + 3;

                    triangles.Add(i0);
                    triangles.Add(i2);
                    triangles.Add(i1);

                    triangles.Add(i1);
                    triangles.Add(i2);
                    triangles.Add(i3);
                }
            }

            var mesh = new Mesh { name = "Mesh_DustSlideSheet" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Builds a continuous, organic fan-shaped vacuum settle apron mesh at the toe of the moonquake dust slide
        /// (Z = L*cos(theta), Y = 0..maxHeight) with zero concentric ring stripes.
        /// </summary>
        public static Mesh CreateToeSettleApronMesh(float width, float length, float slopeAngleDegrees, float maxHeight)
        {
            float slopeRad = slopeAngleDegrees * Mathf.Deg2Rad;
            float toeZ = length * Mathf.Cos(slopeRad);
            float halfWidth = width * 0.66f;
            float h = Mathf.Max(0.12f, maxHeight);

            const int segsX = 24;
            const int segsZ = 10;
            const int lobes = 2;

            var vertices = new List<Vector3>((segsX + 1) * (segsZ + 1) * lobes);
            var normals = new List<Vector3>((segsX + 1) * (segsZ + 1) * lobes);
            var uvs = new List<Vector2>((segsX + 1) * (segsZ + 1) * lobes);
            var colors = new List<Color32>((segsX + 1) * (segsZ + 1) * lobes);
            var triangles = new List<int>(segsX * segsZ * 6 * lobes);

            int stride = segsX + 1;

            for (int lobe = 0; lobe < lobes; lobe++)
            {
                int baseVert = vertices.Count;
                float lobeScaleX = lobe == 0 ? 1.0f : 0.82f;
                float lobeShiftX = lobe == 0 ? -0.18f : 0.24f;
                float lobeHeight = h * (lobe == 0 ? 1.0f : 0.72f);
                byte seedByte = (byte)(lobe == 0 ? 64 : 178);

                for (int z = 0; z <= segsZ; z++)
                {
                    float vCoord = z / (float)segsZ;
                    // Radial profile from just inside the slope toe out onto the flat floor
                    float radialDist = Mathf.Lerp(-0.22f, 1.45f, vCoord);
                    // Low ballistic settle arch peaking near the toe (vCoord ~ 0.28) and dropping to ground at both ends
                    float radialArch = Mathf.SmoothStep(0f, 0.28f, vCoord) * Mathf.Pow(1f - vCoord, 1.35f) * 1.45f;

                    for (int x = 0; x <= segsX; x++)
                    {
                        float uCoord = x / (float)segsX;
                        float centered = (uCoord - 0.5f) * 2f;

                        // Asymmetric lateral thickness matching the two main slope avalanche chutes
                        float lobeShape = 0.65f
                            + 0.35f * Mathf.Cos((centered - 0.15f) * 2.4f)
                            + 0.22f * Mathf.Sin(centered * 4.1f + lobe * 1.5f);
                        float lateralArch = Mathf.Clamp01(1f - centered * centered) * Mathf.Clamp01(lobeShape);

                        float fanSpread = 1f + vCoord * 0.32f;
                        float px = lobeShiftX + centered * halfWidth * lobeScaleX * fanSpread;
                        float pz = toeZ + radialDist + (1f - centered * centered) * 0.48f * vCoord;
                        float py = 0.008f + lobeHeight * Mathf.Clamp01(radialArch * lateralArch);

                        Vector3 n = new Vector3(-centered * 0.22f, 0.82f, 0.52f).normalized;
                        byte alphaByte = (byte)Mathf.Clamp(
                            Mathf.RoundToInt(Mathf.Clamp01(radialArch * lateralArch * 1.25f) * (lobe == 0 ? 225f : 185f)),
                            0,
                            255);

                        vertices.Add(new Vector3(px, py, pz));
                        normals.Add(n);
                        uvs.Add(new Vector2(uCoord, vCoord));
                        colors.Add(new Color32(255, 252, 245, alphaByte));
                    }
                }

                for (int z = 0; z < segsZ; z++)
                {
                    for (int x = 0; x < segsX; x++)
                    {
                        int i0 = baseVert + z * stride + x;
                        int i1 = i0 + 1;
                        int i2 = i0 + stride;
                        int i3 = i2 + 1;

                        triangles.Add(i0);
                        triangles.Add(i2);
                        triangles.Add(i1);

                        triangles.Add(i1);
                        triangles.Add(i2);
                        triangles.Add(i3);
                    }
                }
            }

            var mesh = new Mesh { name = "Mesh_DustSlideToeSettle" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Builds a faceted low-poly disc mesh representing the warm-blue Earth climbing over the horizon ridge
        /// in the Earthrise stillness preview prefab.
        /// </summary>
        public static Mesh CreateEarthriseDiscMesh(float radius = 3.2f)
        {
            const int segments = 24;
            var vertices = new Vector3[segments + 2];
            var normals = new Vector3[segments + 2];
            var uvs = new Vector2[segments + 2];
            var colors = new Color32[segments + 2];
            var triangles = new int[segments * 3];

            vertices[0] = Vector3.zero;
            normals[0] = Vector3.back;
            uvs[0] = new Vector2(0.5f, 0.5f);
            colors[0] = new Color32(235, 245, 255, 255);

            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float cx = Mathf.Cos(angle);
                float cy = Mathf.Sin(angle);
                vertices[i + 1] = new Vector3(cx * radius, cy * radius, 0f);
                normals[i + 1] = Vector3.back;
                uvs[i + 1] = new Vector2(0.5f + cx * 0.5f, 0.5f + cy * 0.5f);
                byte r = (byte)Mathf.RoundToInt(Mathf.Lerp(155f, 250f, cy * 0.5f + 0.5f));
                byte g = (byte)Mathf.RoundToInt(Mathf.Lerp(195f, 240f, cy * 0.5f + 0.5f));
                colors[i + 1] = new Color32(r, g, 255, 245);
            }

            for (int i = 0; i < segments; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 2;
                triangles[i * 3 + 2] = i + 1;
            }

            var mesh = new Mesh { name = "Mesh_EarthriseDisc" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.colors32 = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddIrregularShardOrClod(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<Color32> colors,
            List<int> triangles,
            Vector3 center,
            float baseRadius,
            float height,
            Vector3 tiltDir,
            float seed,
            Vector2 uvValue,
            Color32 baseColor,
            Color32 apexColor)
        {
            // 4-sided asymmetric base polygon so shards read as irregular splinters/slabs, never uniform cones
            float baseYaw = seed * Mathf.PI * 2f;
            float[] cornerAngles =
            {
                baseYaw,
                baseYaw + 1.35f + Frac(seed * 3.1f) * 0.35f,
                baseYaw + 3.05f + Frac(seed * 5.7f) * 0.35f,
                baseYaw + 4.75f + Frac(seed * 7.9f) * 0.35f,
            };
            float[] cornerScales =
            {
                Mathf.Lerp(0.65f, 1.35f, Frac(seed * 2.3f)),
                Mathf.Lerp(0.55f, 1.15f, Frac(seed * 4.9f)),
                Mathf.Lerp(0.70f, 1.40f, Frac(seed * 8.1f)),
                Mathf.Lerp(0.50f, 1.10f, Frac(seed * 11.7f)),
            };

            Vector3 apex = center + tiltDir * height;
            Vector3 b0 = center + new Vector3(Mathf.Cos(cornerAngles[0]) * baseRadius * cornerScales[0], -0.012f, Mathf.Sin(cornerAngles[0]) * baseRadius * cornerScales[0]);
            Vector3 b1 = center + new Vector3(Mathf.Cos(cornerAngles[1]) * baseRadius * cornerScales[1], -0.012f, Mathf.Sin(cornerAngles[1]) * baseRadius * cornerScales[1]);
            Vector3 b2 = center + new Vector3(Mathf.Cos(cornerAngles[2]) * baseRadius * cornerScales[2], -0.012f, Mathf.Sin(cornerAngles[2]) * baseRadius * cornerScales[2]);
            Vector3 b3 = center + new Vector3(Mathf.Cos(cornerAngles[3]) * baseRadius * cornerScales[3], -0.012f, Mathf.Sin(cornerAngles[3]) * baseRadius * cornerScales[3]);

            AddFacetTriangleWithUvAndColors(vertices, normals, uvs, colors, triangles, apex, b1, b0, uvValue, apexColor, baseColor, baseColor);
            AddFacetTriangleWithUvAndColors(vertices, normals, uvs, colors, triangles, apex, b2, b1, uvValue, apexColor, baseColor, baseColor);
            AddFacetTriangleWithUvAndColors(vertices, normals, uvs, colors, triangles, apex, b3, b2, uvValue, apexColor, baseColor, baseColor);
            AddFacetTriangleWithUvAndColors(vertices, normals, uvs, colors, triangles, apex, b0, b3, uvValue, apexColor, baseColor, baseColor);
        }

        private static void AddFacetTriangle(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<Color32> colors,
            List<int> triangles,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Color32 color)
        {
            AddFacetTriangleWithUvAndColors(
                vertices,
                normals,
                uvs,
                colors,
                triangles,
                a,
                b,
                c,
                new Vector2(0.5f, 0.5f),
                color,
                color,
                color);
        }

        private static void AddFacetTriangleWithUvAndColors(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<Color32> colors,
            List<int> triangles,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector2 uvValue,
            Color32 ca,
            Color32 cb,
            Color32 cc)
        {
            int idx = vertices.Count;
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            uvs.Add(uvValue);
            uvs.Add(uvValue);
            uvs.Add(uvValue);
            colors.Add(ca);
            colors.Add(cb);
            colors.Add(cc);
            triangles.Add(idx);
            triangles.Add(idx + 1);
            triangles.Add(idx + 2);
        }

        private static float Frac(float value)
        {
            return value - Mathf.Floor(value);
        }
    }
}
