using UnityEditor;
using UnityEngine;
using MoonProject.Art;
using MoonProject.Editor.Builders;

namespace MoonProject.Rover.Editor
{
    /// <summary>
    /// Physics and effect assets the rover prefab uses: the frictionless sphere material (all traction is done in
    /// code), the multiply-blended track material, and the ballistic regolith grain spray texture on a lit,
    /// alpha-blended particle material (dust catches the Earthlight and 07's headlamp) with a tight ground fade so
    /// low parabolic grain arcs stay visible right down to the regolith surface. The lamp motes share the grain
    /// texture on an unlit, additive particle material: they are only ever as bright as the beam they hang in
    /// (set per mote at runtime), with an HDR tint for a whisper of bloom.
    /// </summary>
    public static class RoverFxAssetBuilder
    {
        /// <summary>How far a fresh, fully opaque track darkens the ground toward the dust swatch.</summary>
        private const float TrackTintStrength = 0.95f;

        /// <summary>Fraction of the track's half width that feathers out to nothing at each edge.</summary>
        private const float TrackEdgeSoftness = 0.5f;

        /// <summary>Tracks start fading at this camera distance (m) and are gone by the far one.</summary>
        private const float TrackFadeNear = 120f;

        private const float TrackFadeFar = 260f;

        private const int DustTextureSize = 64;

        /// <summary>Fraction of each individual regolith grain's radius that stays fully opaque before its antialiased rim.</summary>
        private const float DustGrainCore = 0.55f;

        /// <summary>Distance (m) over which a grain cluster softens right as its billboard touches the ground mesh.</summary>
        private const float DustGroundFade = 0.03f;

        /// <summary>Grains nearer the camera than this vanish, fading in fully by the far distance (m).</summary>
        private const float DustCameraFadeNear = 0.4f;

        private const float DustCameraFadeFar = 1.6f;

        /// <summary>Motes nearer the camera than this vanish, fading in fully by the far distance (m).</summary>
        private const float MoteCameraFadeNear = 0.3f;

        private const float MoteCameraFadeFar = 1.2f;

        /// <summary>Authored iris blade box dimensions (m) on CutterOrigin.</summary>
        private const float NozzleBladeLength = 0.028f;
        private const float NozzleBladeWidth = 0.016f;
        private const float NozzleBladeThickness = 0.004f;
        private const float NozzleBladeEdgeWear = 0.25f;

        /// <summary>
        /// Deterministic cluster of discrete regolith grain specks in normalized [-1, 1] UV space:
        /// (x, y, radius, peakAlpha). Zero foggy haze between grains so each particle renders as crisp regolith specks.
        /// </summary>
        private static readonly Vector4[] DustGrainSpecks =
        {
            new Vector4(0.00f, 0.00f, 0.14f, 1.00f),
            new Vector4(-0.34f, -0.18f, 0.10f, 0.95f),
            new Vector4(0.32f, -0.22f, 0.11f, 0.92f),
            new Vector4(-0.14f, 0.36f, 0.10f, 0.90f),
            new Vector4(0.28f, 0.30f, 0.09f, 0.88f),
            new Vector4(-0.56f, 0.14f, 0.09f, 0.85f),
            new Vector4(0.58f, 0.04f, 0.09f, 0.84f),
            new Vector4(-0.22f, -0.54f, 0.09f, 0.82f),
            new Vector4(0.18f, -0.58f, 0.08f, 0.80f),
            new Vector4(-0.48f, -0.44f, 0.08f, 0.78f),
            new Vector4(0.50f, -0.42f, 0.08f, 0.76f),
            new Vector4(-0.42f, 0.52f, 0.08f, 0.75f),
            new Vector4(0.46f, 0.48f, 0.08f, 0.74f),
        };

        [MoonBuilder("Rover/Materials", 305)]
        public static void Build()
        {
            BuildSphereMaterial();
            BuildTrackMaterial();
            BuildDustTexture();
            BuildDustMaterial();
            BuildMoteMaterial();
            BuildThreadMaterial();
            BuildNozzleMeshes();
            AssetDatabase.SaveAssets();
        }

        public static void BuildNozzleMeshes()
        {
            BuildNozzleBladeMesh();
            BuildNozzleCoreMesh();
        }

        private static void BuildSphereMaterial()
        {
            var material = new PhysicsMaterial
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum,
            };
            GeneratedAssets.CreateOrReplace(material, RoverAssetPaths.SpherePhysicsMaterial);
        }

        private static void BuildTrackMaterial()
        {
            var shader = BuildWiring.Require<Shader>(RoverAssetPaths.TrackShader, "the Rover shaders folder");
            Color tint = Palette.Get(PaletteSwatch.DustMid);
            tint.a = TrackTintStrength;
            var material = new Material(shader) { enableInstancing = false };
            material.SetColor("_Color", tint);
            material.SetFloat("_EdgeSoftness", TrackEdgeSoftness);
            material.SetFloat("_FadeNear", TrackFadeNear);
            material.SetFloat("_FadeFar", TrackFadeFar);
            MaterialValidation.Validate(material);
            GeneratedAssets.CreateOrReplace(material, RoverAssetPaths.TrackMaterial);
        }

        /// <summary>Cluster of crisp regolith grain specks with transparent vacuum between them (no smoky cloud halo).</summary>
        private static void BuildDustTexture()
        {
            var texture = new Texture2D(DustTextureSize, DustTextureSize, TextureFormat.RGBA32, true, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[DustTextureSize * DustTextureSize];
            float half = DustTextureSize * 0.5f;
            for (int y = 0; y < DustTextureSize; y++)
            {
                for (int x = 0; x < DustTextureSize; x++)
                {
                    float dx = (x + 0.5f - half) / half;
                    float dy = (y + 0.5f - half) / half;
                    float alpha01 = SampleGrainCluster(dx, dy);
                    byte alpha = (byte)Mathf.RoundToInt(alpha01 * 255f);
                    pixels[y * DustTextureSize + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(true, false);
            GeneratedAssets.CreateOrReplace(texture, RoverAssetPaths.DustTexture);
        }

        private static float SampleGrainCluster(float dx, float dy)
        {
            float best = 0f;
            for (int i = 0; i < DustGrainSpecks.Length; i++)
            {
                Vector4 speck = DustGrainSpecks[i];
                float gx = dx - speck.x;
                float gy = dy - speck.y;
                float radius = speck.z;
                float distSq = gx * gx + gy * gy;
                if (distSq >= radius * radius)
                {
                    continue;
                }

                float dist = Mathf.Sqrt(distSq);
                float rimT = Mathf.InverseLerp(DustGrainCore * radius, radius, dist);
                float grainAlpha = speck.w * (1f - Mathf.SmoothStep(0f, 1f, rimT));
                if (grainAlpha > best)
                {
                    best = grainAlpha;
                }
            }

            return best;
        }

        private static void BuildDustMaterial()
        {
            var shader = BuildWiring.Require<Shader>(RoverAssetPaths.ParticlesSimpleLitShader, "the URP package");
            var texture = BuildWiring.Require<Texture2D>(RoverAssetPaths.DustTexture, "Rover/Materials");
            var material = new Material(shader) { enableInstancing = true };
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_SoftParticlesEnabled", 1f);
            material.SetFloat("_SoftParticlesNearFadeDistance", 0f);
            material.SetFloat("_SoftParticlesFarFadeDistance", DustGroundFade);
            material.SetFloat("_CameraFadingEnabled", 1f);
            material.SetFloat("_CameraNearFadeDistance", DustCameraFadeNear);
            material.SetFloat("_CameraFarFadeDistance", DustCameraFadeFar);
            material.SetColor("_SpecColor", Color.black);
            material.SetFloat("_Smoothness", 0f);
            material.SetFloat("_Surface", (float)BaseShaderGUI.SurfaceType.Transparent);
            material.SetFloat("_Blend", (float)BaseShaderGUI.BlendMode.Alpha);
            BaseShaderGUI.SetupMaterialBlendMode(material);
            MaterialValidation.Validate(material);
            GeneratedAssets.CreateOrReplace(material, RoverAssetPaths.DustMaterial);
        }

        private static void BuildThreadMaterial()
        {
            var shader = BuildWiring.Require<Shader>(RoverAssetPaths.UnlitShader, "the URP package");
            var material = new Material(shader) { enableInstancing = true };
            MaterialValidation.Validate(material);
            GeneratedAssets.CreateOrReplace(material, RoverAssetPaths.ThreadMaterial);
        }

        private static void BuildMoteMaterial()
        {
            var shader = BuildWiring.Require<Shader>(RoverAssetPaths.ParticlesUnlitShader, "the URP package");
            var texture = BuildWiring.Require<Texture2D>(RoverAssetPaths.DustTexture, "Rover/Materials");
            var material = new Material(shader) { enableInstancing = true };
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_SoftParticlesEnabled", 0f);
            material.SetFloat("_CameraFadingEnabled", 1f);
            material.SetFloat("_CameraNearFadeDistance", MoteCameraFadeNear);
            material.SetFloat("_CameraFarFadeDistance", MoteCameraFadeFar);
            material.SetFloat("_Surface", (float)BaseShaderGUI.SurfaceType.Transparent);
            material.SetFloat("_Blend", (float)BaseShaderGUI.BlendMode.Additive);
            BaseShaderGUI.SetupMaterialBlendMode(material);
            MaterialValidation.Validate(material);
            GeneratedAssets.CreateOrReplace(material, RoverAssetPaths.MoteMaterial);
        }

        private static void BuildNozzleBladeMesh()
        {
            float hx = NozzleBladeLength * 0.5f;
            float hy = NozzleBladeWidth * 0.5f;
            float hz = NozzleBladeThickness * 0.5f;
            var vertices = new Vector3[]
            {
                // +Z front
                new Vector3(-hx, -hy, hz), new Vector3(hx, -hy, hz), new Vector3(hx, hy, hz), new Vector3(-hx, hy, hz),
                // -Z back
                new Vector3(hx, -hy, -hz), new Vector3(-hx, -hy, -hz), new Vector3(-hx, hy, -hz), new Vector3(hx, hy, -hz),
                // +Y top
                new Vector3(-hx, hy, hz), new Vector3(hx, hy, hz), new Vector3(hx, hy, -hz), new Vector3(-hx, hy, -hz),
                // -Y bottom
                new Vector3(-hx, -hy, -hz), new Vector3(hx, -hy, -hz), new Vector3(hx, -hy, hz), new Vector3(-hx, -hy, hz),
                // +X right
                new Vector3(hx, -hy, hz), new Vector3(hx, -hy, -hz), new Vector3(hx, hy, -hz), new Vector3(hx, hy, hz),
                // -X left
                new Vector3(-hx, -hy, -hz), new Vector3(-hx, -hy, hz), new Vector3(-hx, hy, hz), new Vector3(-hx, hy, -hz),
            };
            var uvs = new Vector2[24];
            var colors = new Color[24];
            var triangles = new int[36];
            var vcol = new Color(1f, NozzleBladeEdgeWear, 0f, 1f);
            for (int face = 0; face < 6; face++)
            {
                int v = face * 4;
                uvs[v + 0] = new Vector2(0f, 0f);
                uvs[v + 1] = new Vector2(1f, 0f);
                uvs[v + 2] = new Vector2(1f, 1f);
                uvs[v + 3] = new Vector2(0f, 1f);
                colors[v + 0] = vcol;
                colors[v + 1] = vcol;
                colors[v + 2] = vcol;
                colors[v + 3] = vcol;
                int t = face * 6;
                triangles[t + 0] = v + 0;
                triangles[t + 1] = v + 2;
                triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 0;
                triangles[t + 4] = v + 3;
                triangles[t + 5] = v + 2;
            }

            var mesh = new Mesh
            {
                vertices = vertices,
                uv = uvs,
                colors = colors,
                triangles = triangles,
            };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            GeneratedAssets.CreateOrReplace(mesh, RoverAssetPaths.NozzleBladeMesh);
        }

        private static void BuildNozzleCoreMesh()
        {
            const float h = 0.5f;
            var vertices = new Vector3[]
            {
                new Vector3(-h, -h, 0f), new Vector3(h, -h, 0f), new Vector3(h, h, 0f), new Vector3(-h, h, 0f),
                new Vector3(h, -h, 0f), new Vector3(-h, -h, 0f), new Vector3(-h, h, 0f), new Vector3(h, h, 0f),
            };
            var uvs = new Vector2[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            };
            var vcol = new Color(1f, 0f, 0f, 1f);
            var colors = new Color[] { vcol, vcol, vcol, vcol, vcol, vcol, vcol, vcol };
            var triangles = new int[]
            {
                0, 1, 2, 0, 2, 3,
                4, 5, 6, 4, 6, 7,
            };
            var mesh = new Mesh
            {
                vertices = vertices,
                uv = uvs,
                colors = colors,
                triangles = triangles,
            };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            GeneratedAssets.CreateOrReplace(mesh, RoverAssetPaths.NozzleCoreMesh);
        }
    }
}
