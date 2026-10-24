using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Procedural mesh generators for the 0.6 lunar phenomena VFX prefabs:
    /// 1. Terminator horizon haze band and thin rising electrostatic mote columns.
    /// 2. Fresh micrometeorite crater bowl, fused impact glass shards, and faceted flash star.
    /// 3. Down-slope sheeting regolith ribbons, low vacuum toe settle veil, and faceted mineral grain shards.
    /// 4. Rising Earth disc preview mesh for the Earthrise stillness beat.
    /// </summary>
    public static class PhenomenaMeshes
    {
        /// <summary>
        /// Builds a gently curved two-layer terminator horizon haze band mesh spanning <paramref name="width"/> metres
        /// across and <paramref name="height"/> metres vertically.
        /// </summary>
        public static Mesh CreateHorizonHazeBandMesh(float width, float height, int horizontalSegments = 24)
        {
            int segsX = Mathf.Max(4, horizontalSegments);
            const int segsY = 4;
            const int layers = 2;
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

            for (int layer = 0; layer < layers; layer++)
            {
                int layerBaseVert = v;
                float layerWidth = width * (layer == 0 ? 1.0f : 0.86f);
                float layerHeight = height * (layer == 0 ? 1.0f : 0.58f);
                float halfWidth = layerWidth * 0.5f;
                float arcDepth = layerWidth * 0.08f;
                float zOffset = layer == 0 ? 0.45f : -0.15f;

                for (int y = 0; y <= segsY; y++)
                {
                    float vCoord = y / (float)segsY;
                    float posY = vCoord * layerHeight;
                    float verticalAlpha = Mathf.Clamp01(Mathf.Sin(vCoord * Mathf.PI) * 1.15f);
                    byte alphaByte = (byte)Mathf.RoundToInt(verticalAlpha * (layer == 0 ? 235f : 255f));

                    for (int x = 0; x <= segsX; x++)
                    {
                        float uCoord = x / (float)segsX;
                        float centeredX = (uCoord - 0.5f) * 2f;
                        float posX = centeredX * halfWidth;
                        float posZ = zOffset + (1f - centeredX * centeredX) * arcDepth;

                        vertices[v] = new Vector3(posX, posY, posZ);
                        normals[v] = new Vector3(-centeredX * 0.2f, 0.15f, -1f).normalized;
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
        /// Builds thin vertical crossed-ribbon columns distributed non-uniformly along the terminator band to depict
        /// electrostatically levitated mote filaments rising from the ridge.
        /// </summary>
        public static Mesh CreateMoteColumnsMesh(int columnCount, float spanWidth, float columnRadius, float columnHeight)
        {
            int cols = Mathf.Max(2, columnCount);
            var vertices = new List<Vector3>(cols * 16);
            var normals = new List<Vector3>(cols * 16);
            var uvs = new List<Vector2>(cols * 16);
            var colors = new List<Color32>(cols * 16);
            var triangles = new List<int>(cols * 24);

            for (int c = 0; c < cols; c++)
            {
                float tCol = cols == 1 ? 0f : (c / (float)(cols - 1)) - 0.5f;
                float hash = Frac(Mathf.Sin((c + 1) * 19.73f) * 43758.5453f);
                float centerX = tCol * (spanWidth * 0.72f) + (hash - 0.5f) * (spanWidth * 0.05f);
                float centerZ = (Frac(hash * 7.13f) - 0.5f) * 1.15f;
                float colH = columnHeight * Mathf.Lerp(0.82f, 1.18f, Frac(hash * 3.91f));
                float colR = columnRadius * Mathf.Lerp(0.85f, 1.25f, Frac(hash * 5.47f));
                byte seedByte = (byte)Mathf.RoundToInt(hash * 255f);

                // Two crossed vertical ribbons per column (angled at 25 deg and 115 deg) with per-column 0..1 UVs
                for (int plane = 0; plane < 2; plane++)
                {
                    float yawRad = ((plane * 90f) + 25f + hash * 20f) * Mathf.Deg2Rad;
                    Vector3 right = new Vector3(Mathf.Cos(yawRad), 0f, Mathf.Sin(yawRad)) * colR;
                    Vector3 normal = new Vector3(-Mathf.Sin(yawRad), 0f, Mathf.Cos(yawRad));

                    int baseIdx = vertices.Count;

                    vertices.Add(new Vector3(centerX - right.x, 0f, centerZ - right.z));
                    vertices.Add(new Vector3(centerX + right.x, 0f, centerZ + right.z));
                    vertices.Add(new Vector3(centerX - right.x * 0.72f, colH, centerZ - right.z * 0.72f));
                    vertices.Add(new Vector3(centerX + right.x * 0.72f, colH, centerZ + right.z * 0.72f));

                    for (int k = 0; k < 4; k++)
                    {
                        normals.Add(normal);
                    }

                    uvs.Add(new Vector2(0f, 0f));
                    uvs.Add(new Vector2(1f, 0f));
                    uvs.Add(new Vector2(0f, 1f));
                    uvs.Add(new Vector2(1f, 1f));

                    colors.Add(new Color32(255, 245, 225, 245));
                    colors.Add(new Color32(255, 245, 225, 245));
                    colors.Add(new Color32(235, 245, 255, 165));
                    colors.Add(new Color32(235, 245, 255, 165));

                    // Encode column seed in blue channel while preserving warm tint
                    colors[baseIdx] = new Color32(255, 245, seedByte, 245);
                    colors[baseIdx + 1] = new Color32(255, 245, seedByte, 245);
                    colors[baseIdx + 2] = new Color32(235, 245, seedByte, 165);
                    colors[baseIdx + 3] = new Color32(235, 245, seedByte, 165);

                    triangles.Add(baseIdx);
                    triangles.Add(baseIdx + 2);
                    triangles.Add(baseIdx + 1);

                    triangles.Add(baseIdx + 1);
                    triangles.Add(baseIdx + 2);
                    triangles.Add(baseIdx + 3);
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
        /// Builds the small fresh micrometeorite crater mesh and its central fused impact glass melt pool + jagged
        /// vitreous glass shards. Vertex colors follow R1 convention: R=AO, G=EdgeWear, B=Cavity, A=GlassMeltMask.
        /// </summary>
        public static Mesh CreateImpactCraterAndGlassMesh(float craterRadius)
        {
            const int radialSectors = 14;
            const int rings = 5;
            float radius = Mathf.Max(0.25f, craterRadius);

            var vertices = new List<Vector3>(320);
            var normals = new List<Vector3>(320);
            var uvs = new List<Vector2>(320);
            var colors = new List<Color32>(320);
            var triangles = new List<int>(640);

            // Ring profile (normalized radius rFrac, height, AO, EdgeWear, Cavity, GlassMeltMask)
            float[] ringRadii = { 0.0f, 0.28f, 0.56f, 0.88f, 1.15f, 1.48f };
            float[] ringHeights = { 0.015f, 0.008f, -0.045f, 0.125f, 0.055f, 0.004f };
            Color32[] ringColors =
            {
                new Color32(220, 245, 235, 255), // Center molten glass dome (A=255)
                new Color32(205, 230, 215, 245), // Inner glass melt pool edge (A=245)
                new Color32(175, 165, 175, 120), // Crater bowl floor/wall transition (A=120)
                new Color32(248, 245, 250, 15),  // Raised fresh crater rim crest (A=15, high G edge wear)
                new Color32(225, 190, 225, 0),   // Outer ejecta slope (A=0)
                new Color32(210, 165, 215, 0),   // Outer regolith tie-in skirt (A=0)
            };

            var ringPoints = new Vector3[rings + 1, radialSectors];
            for (int r = 0; r <= rings; r++)
            {
                for (int s = 0; s < radialSectors; s++)
                {
                    float baseAngle = (s / (float)radialSectors) * Mathf.PI * 2f;
                    float hash = Frac(Mathf.Sin((r * 19 + s * 37 + 5) * 41.23f) * 43758.5453f);
                    float angleJitter = r == 0 ? 0f : (hash - 0.5f) * (Mathf.PI * 2f / radialSectors) * 0.28f;
                    float radJitter = r == 0 ? 0f : Mathf.Lerp(0.86f, 1.14f, Frac(hash * 7.7f));
                    float heightJitter = r == 0 ? 0f : (Frac(hash * 13.1f) - 0.5f) * 0.020f;

                    float rr = ringRadii[r] * radius * radJitter;
                    float angle = baseAngle + angleJitter;
                    ringPoints[r, s] = new Vector3(
                        Mathf.Cos(angle) * rr,
                        ringHeights[r] * radius + heightJitter,
                        Mathf.Sin(angle) * rr);
                }
            }

            // Flat-shaded faceted triangles for crisp R1 low-poly crater + melt pool facets
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < radialSectors; s++)
                {
                    int nextS = (s + 1) % radialSectors;
                    Vector3 p00 = ringPoints[r, s];
                    Vector3 p01 = ringPoints[r, nextS];
                    Vector3 p10 = ringPoints[r + 1, s];
                    Vector3 p11 = ringPoints[r + 1, nextS];

                    Color32 cInner = ringColors[r];
                    Color32 cOuter = ringColors[r + 1];

                    if (r == 0)
                    {
                        AddFacetTriangleWithColors(vertices, normals, uvs, colors, triangles,
                            p00, p11, p10, cInner, cOuter, cOuter);
                    }
                    else
                    {
                        AddFacetTriangleWithColors(vertices, normals, uvs, colors, triangles,
                            p00, p11, p10, cInner, cOuter, cOuter);
                        AddFacetTriangleWithColors(vertices, normals, uvs, colors, triangles,
                            p00, p01, p11, cInner, cInner, cOuter);
                    }
                }
            }

            // Add 9 irregular angular vitreous impact glass shards protruding from the central melt pool
            // (high G = EdgeWear and A = 255 so they glow white-amber when hot and keep a permanent glint when cooled)
            const int glassShardCount = 9;
            for (int i = 0; i < glassShardCount; i++)
            {
                float seed = Frac(Mathf.Sin((i + 1) * 63.71f) * 43758.5453f);
                float angle = (i / (float)glassShardCount) * Mathf.PI * 2f + (seed - 0.5f) * 0.35f;
                float dist = (i == 0 ? 0.03f : Mathf.Lerp(0.10f, 0.42f, Frac(seed * 5.3f))) * radius;
                Vector3 center = new Vector3(Mathf.Cos(angle) * dist, 0.01f * radius, Mathf.Sin(angle) * dist);

                float shardHeight = Mathf.Lerp(0.09f, 0.22f, Frac(seed * 9.1f)) * radius;
                float shardWidth = Mathf.Lerp(0.06f, 0.14f, Frac(seed * 3.7f)) * radius;
                Vector3 tiltDir = new Vector3(Mathf.Cos(angle) * 0.32f, 1f, Mathf.Sin(angle) * 0.32f).normalized;
                Vector3 apex = center + tiltDir * shardHeight;

                float baseYaw = seed * Mathf.PI * 2f;
                Vector3 b0 = center + new Vector3(Mathf.Cos(baseYaw) * shardWidth, -0.02f, Mathf.Sin(baseYaw) * shardWidth);
                Vector3 b1 = center + new Vector3(Mathf.Cos(baseYaw + 2.094f) * shardWidth, -0.02f, Mathf.Sin(baseYaw + 2.094f) * shardWidth);
                Vector3 b2 = center + new Vector3(Mathf.Cos(baseYaw + 4.188f) * shardWidth, -0.02f, Mathf.Sin(baseYaw + 4.188f) * shardWidth);

                Color32 baseGlass = new Color32(200, 230, 215, 255);
                Color32 tipGlass = new Color32(255, 255, 250, 255);

                AddFacetTriangleWithColors(vertices, normals, uvs, colors, triangles, apex, b1, b0, tipGlass, baseGlass, baseGlass);
                AddFacetTriangleWithColors(vertices, normals, uvs, colors, triangles, apex, b2, b1, tipGlass, baseGlass, baseGlass);
                AddFacetTriangleWithColors(vertices, normals, uvs, colors, triangles, apex, b0, b2, tipGlass, baseGlass, baseGlass);
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
        /// Builds a 3D crossed starlight burst mesh for the brief distant white micrometeorite impact flash.
        /// </summary>
        public static Mesh CreateImpactFlashStarMesh()
        {
            var vertices = new List<Vector3>(36);
            var normals = new List<Vector3>(36);
            var uvs = new List<Vector2>(36);
            var colors = new List<Color32>(36);
            var triangles = new List<int>(54);

            // Three intersecting star planes (XY, ZY, and diagonal) so the flash reads from any camera angle
            float[] planeYaws = { 0f, 60f, 120f };
            for (int p = 0; p < planeYaws.Length; p++)
            {
                Quaternion rot = Quaternion.Euler(0f, planeYaws[p], 0f);
                Vector3 right = rot * Vector3.right;
                Vector3 up = Vector3.up;
                Vector3 normal = rot * Vector3.forward;

                int baseIdx = vertices.Count;
                vertices.Add(-right - up);
                vertices.Add(right - up);
                vertices.Add(-right + up);
                vertices.Add(right + up);

                for (int k = 0; k < 4; k++)
                {
                    normals.Add(normal);
                    colors.Add(new Color32(255, 250, 240, 255));
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
        /// Builds the down-slope sheeting regolith stream mesh for the gentle moonquake dust slide, including both
        /// surface-skimming mineral streams and low parabolic vacuum saltation arc ribbons.
        /// Local coordinates: crest at (Y = L*sin(theta), Z = 0) down to toe at (Y = 0, Z = L*cos(theta)).
        /// </summary>
        public static Mesh CreateSlopeSheetMesh(float width, float length, float slopeAngleDegrees)
        {
            const int ribbonCount = 6;
            const int arcRibbonCount = 7;
            const int lengthSegments = 14;

            float slopeRad = slopeAngleDegrees * Mathf.Deg2Rad;
            float sinSlope = Mathf.Sin(slopeRad);
            float cosSlope = Mathf.Cos(slopeRad);
            float topY = length * sinSlope;
            Vector3 slopeNormal = new Vector3(0f, cosSlope, sinSlope).normalized;

            int totalRibbons = ribbonCount + arcRibbonCount;
            var vertices = new List<Vector3>(totalRibbons * (lengthSegments + 1) * 2);
            var normals = new List<Vector3>(totalRibbons * (lengthSegments + 1) * 2);
            var uvs = new List<Vector2>(totalRibbons * (lengthSegments + 1) * 2);
            var colors = new List<Color32>(totalRibbons * (lengthSegments + 1) * 2);
            var triangles = new List<int>(totalRibbons * lengthSegments * 6);

            // 1. Surface-skimming regolith sheet streams
            for (int r = 0; r < ribbonCount; r++)
            {
                float tRibbon = (r / (float)(ribbonCount - 1)) - 0.5f;
                float ribbonCenter = tRibbon * (width * 0.78f);
                float ribbonHalfWidth = (width / ribbonCount) * 0.74f;
                float skimLift = 0.06f + (r % 2) * 0.035f;

                int baseVert = vertices.Count;
                for (int s = 0; s <= lengthSegments; s++)
                {
                    float vCoord = s / (float)lengthSegments;
                    float distAlong = vCoord * length;
                    float y = topY - distAlong * sinSlope + skimLift * cosSlope;
                    float z = distAlong * cosSlope + skimLift * sinSlope;

                    float widthScale = Mathf.Lerp(0.80f, 1.18f, vCoord);
                    float halfW = ribbonHalfWidth * widthScale;

                    byte edgeAlpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(Mathf.Sin(vCoord * Mathf.PI) * 1.2f) * 255f);
                    Color32 col = new Color32(255, 252, 245, edgeAlpha);

                    vertices.Add(new Vector3(ribbonCenter - halfW, y, z));
                    vertices.Add(new Vector3(ribbonCenter + halfW, y, z));
                    normals.Add(slopeNormal);
                    normals.Add(slopeNormal);
                    uvs.Add(new Vector2(0f, vCoord));
                    uvs.Add(new Vector2(1f, vCoord));
                    colors.Add(col);
                    colors.Add(col);
                }

                for (int s = 0; s < lengthSegments; s++)
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

            // 2. Low parabolic ballistic saltation arc ribbons skimming 8..28 cm above the slope in vacuum
            for (int a = 0; a < arcRibbonCount; a++)
            {
                float seed = Frac(Mathf.Sin((a + 1) * 37.91f) * 43758.5453f);
                float laneX = ((a / (float)(arcRibbonCount - 1)) - 0.5f) * (width * 0.72f) + (seed - 0.5f) * 0.25f;
                float startFrac = Mathf.Lerp(0.06f, 0.38f, Frac(seed * 3.1f));
                float arcSpanFrac = Mathf.Lerp(0.45f, 0.58f, Frac(seed * 7.3f));
                float peakArcLift = Mathf.Lerp(0.14f, 0.28f, Frac(seed * 5.7f));
                float halfW = 0.22f;

                int baseVert = vertices.Count;
                for (int s = 0; s <= lengthSegments; s++)
                {
                    float uArc = s / (float)lengthSegments;
                    float vCoord = Mathf.Clamp01(startFrac + uArc * arcSpanFrac);
                    float distAlong = vCoord * length;
                    // Clean vacuum parabola: 4 * h_max * u * (1 - u)
                    float parabolaLift = 0.05f + 4f * peakArcLift * uArc * (1f - uArc);

                    float y = topY - distAlong * sinSlope + parabolaLift * cosSlope;
                    float z = distAlong * cosSlope + parabolaLift * sinSlope;

                    byte edgeAlpha = (byte)Mathf.RoundToInt(Mathf.Sin(uArc * Mathf.PI) * 235f);
                    Color32 col = new Color32(255, 250, 240, edgeAlpha);

                    vertices.Add(new Vector3(laneX - halfW, y, z));
                    vertices.Add(new Vector3(laneX + halfW, y, z));
                    normals.Add(slopeNormal);
                    normals.Add(slopeNormal);
                    uvs.Add(new Vector2(0f, uArc));
                    uvs.Add(new Vector2(1f, uArc));
                    colors.Add(col);
                    colors.Add(col);
                }

                for (int s = 0; s < lengthSegments; s++)
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
        /// Builds the low ground-skimming vacuum settle apron mesh at the toe of the moonquake dust slide
        /// (Z = L*cos(theta), Y = 0..maxHeight).
        /// </summary>
        public static Mesh CreateToeSettleApronMesh(float width, float length, float slopeAngleDegrees, float maxHeight)
        {
            float slopeRad = slopeAngleDegrees * Mathf.Deg2Rad;
            float toeZ = length * Mathf.Cos(slopeRad);
            float halfWidth = width * 0.68f;
            float h = Mathf.Max(0.12f, maxHeight);

            const int segsX = 14;
            const int layers = 4;
            var vertices = new List<Vector3>((segsX + 1) * layers * 2);
            var normals = new List<Vector3>((segsX + 1) * layers * 2);
            var uvs = new List<Vector2>((segsX + 1) * layers * 2);
            var colors = new List<Color32>((segsX + 1) * layers * 2);
            var triangles = new List<int>(segsX * layers * 6);

            // Four low ground-skimming ballistic settle arcs fanning out at the toe of the slope (never rising above maxHeight)
            float[] zOffsets = { -0.10f, 0.25f, 0.65f, 1.05f };
            float[] heightScales = { 0.85f, 1.00f, 0.82f, 0.58f };

            for (int layer = 0; layer < layers; layer++)
            {
                float zOffset = toeZ + zOffsets[layer];
                float layerH = h * heightScales[layer];
                int baseIdx = vertices.Count;

                for (int x = 0; x <= segsX; x++)
                {
                    float u = x / (float)segsX;
                    float centered = (u - 0.5f) * 2f;
                    float px = centered * halfWidth * (1f + layer * 0.08f);
                    float pz = zOffset + (1f - centered * centered) * 0.52f;
                    float arch = Mathf.Sqrt(Mathf.Max(0f, 1f - centered * centered));

                    vertices.Add(new Vector3(px, 0.01f, pz));
                    vertices.Add(new Vector3(px, layerH * arch, pz + 0.28f));

                    normals.Add(new Vector3(0f, 0.72f, 0.69f).normalized);
                    normals.Add(new Vector3(0f, 0.72f, 0.69f).normalized);

                    uvs.Add(new Vector2(u, 0f));
                    uvs.Add(new Vector2(u, 1f));

                    colors.Add(new Color32(255, 252, 245, 245));
                    colors.Add(new Color32(255, 252, 245, 75));
                }

                for (int x = 0; x < segsX; x++)
                {
                    int i0 = baseIdx + x * 2;
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
            AddFacetTriangleWithColors(vertices, normals, uvs, colors, triangles, a, b, c, color, color, color);
        }

        private static void AddFacetTriangleWithColors(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<Vector2> uvs,
            List<Color32> colors,
            List<int> triangles,
            Vector3 a,
            Vector3 b,
            Vector3 c,
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
            uvs.Add(new Vector2(0.5f, 1f));
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
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
