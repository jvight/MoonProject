using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using MoonProject.Editor.Automation;

namespace MoonProject.Art.Editor.Concept
{
    /// <summary>
    /// Look variants of the same frames for the owner to pick (preview only): the look today, then A "Moon grey",
    /// B "Cold night" and C "Low sun". Each opens Main.unity (never saved) and overrides, in memory only, the
    /// palette's ground and rock swatches (props and the baked terrain colours alike), the sky's globals, the fog and
    /// haze, the ambient, the earthlight and fill, and a grade Volume over the world's own. Frames: 07 at the dock
    /// with the mast and Earth over the rim, a wide basin and a close 07.
    /// <code>
    /// python tools/unity_batch.py exec --method MoonProject.Art.Editor.Concept.LookVariants.Capture
    /// </code>
    /// PNGs land in the run's output folder under look/.
    /// </summary>
    public static class LookVariants
    {
        private static readonly int SkyTop = Shader.PropertyToID("_MoonSkyTop");
        private static readonly int HorizonGlow = Shader.PropertyToID("_MoonHorizonGlow");
        private static readonly int HorizonBand = Shader.PropertyToID("_MoonHorizonBand");
        private static readonly int StarParams = Shader.PropertyToID("_MoonStarParams");
        private static readonly int MilkyWay = Shader.PropertyToID("_MoonMilkyWayAxis");
        private static readonly int HazeColor = Shader.PropertyToID("_MoonHazeColor");
        private static readonly int Haze = Shader.PropertyToID("_MoonHaze");
        private static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        private static readonly int EarthDirection = Shader.PropertyToID("_MoonEarthDirection");

        private static readonly PaletteSwatch[] Ground =
        {
            PaletteSwatch.DustLight, PaletteSwatch.DustMid, PaletteSwatch.DustShadow, PaletteSwatch.RockLight,
            PaletteSwatch.RockDark, PaletteSwatch.CakedDust,
        };

        public static void Capture()
        {
            BatchRunner.Run(nameof(LookVariants), args =>
            {
                string output = Path.Combine(args.OutputDirectory, "look");
                bool captured = true;
                foreach (Variant variant in Variants())
                {
                    captured &= CaptureVariant(variant, output);
                }

                return captured;
            });
        }

        /// <summary>One look: what replaces the world's values (null or empty keeps the world's own).</summary>
        private sealed class Variant
        {
            public string Name { get; set; }

            /// <summary>New sRGB colours for the ground and rock swatches, in <see cref="Ground"/>'s order.</summary>
            public Color32[] Ground { get; set; }

            public Color? SkyTop { get; set; }

            /// <summary>
            /// Scales on the horizon glow's colour (x) and height (y), the band's colour (z) and the Milky Way (w).
            /// </summary>
            public Vector4 SkyScale { get; set; } = Vector4.one;

            public float StarScale { get; set; } = 1f;

            public Color? FogColor { get; set; }

            public float FogScale { get; set; } = 1f;

            public Color[] Ambient { get; set; }

            public Color? LightColor { get; set; }

            public float? LightIntensity { get; set; }

            /// <summary>
            /// The earthlight's source (elevation, bearing from the heading toward Earth), degrees; null keeps it.
            /// </summary>
            public Vector2? LightSource { get; set; }

            public float LightShadow { get; set; } = -1f;

            public Color? FillColor { get; set; }

            public float? FillIntensity { get; set; }

            public Vector2? FillSource { get; set; }

            public Action<VolumeProfile> Grade { get; set; }
        }

        private static IEnumerable<Variant> Variants()
        {
            yield return new Variant { Name = "0_today" };
            yield return new Variant
            {
                Name = "A_moon_grey",
                Ground = new[]
                {
                    Srgb(0xA6, 0xA7, 0xAB), Srgb(0x78, 0x7A, 0x80), Srgb(0x3E, 0x40, 0x48), Srgb(0x6C, 0x6D, 0x72),
                    Srgb(0x52, 0x53, 0x59), Srgb(0x93, 0x94, 0x99),
                },
                SkyTop = new Color(0.004f, 0.006f, 0.02f),
                SkyScale = new Vector4(0.7f, 0.35f, 0.6f, 1f),
                FogColor = new Color(0.035f, 0.04f, 0.06f),
                FogScale = 0.3f,
                Ambient = new[]
                {
                    new Color(0.06f, 0.065f, 0.085f), new Color(0.042f, 0.045f, 0.058f),
                    new Color(0.012f, 0.013f, 0.018f),
                },
                LightColor = new Color(0.92f, 0.94f, 1f),
                Grade = profile => Grade(profile, 0f, 22f, -18f, new Color(0.36f, 0.39f, 0.46f),
                    new Color(1f, 0.92f, 0.8f), -15f),
            };
            yield return new Variant
            {
                Name = "B_cold_night",
                Ground = new[]
                {
                    Srgb(0x8E, 0x95, 0xA6), Srgb(0x62, 0x69, 0x7C), Srgb(0x2A, 0x30, 0x40), Srgb(0x5A, 0x60, 0x70),
                    Srgb(0x44, 0x4A, 0x5A), Srgb(0x80, 0x86, 0x96),
                },
                SkyTop = new Color(0.002f, 0.003f, 0.01f),
                SkyScale = new Vector4(0.35f, 0.25f, 0.35f, 1.8f),
                StarScale = 1.6f,
                FogColor = new Color(0.02f, 0.025f, 0.04f),
                FogScale = 0.1f,
                Ambient = new[]
                {
                    new Color(0.03f, 0.038f, 0.07f), new Color(0.022f, 0.027f, 0.048f),
                    new Color(0.003f, 0.003f, 0.008f),
                },
                LightColor = new Color(0.72f, 0.84f, 1f),
                LightIntensity = 1.8f,
                LightShadow = 1f,
                FillIntensity = 0.1f,
                Grade = profile => Grade(profile, -0.15f, 30f, -15f, new Color(0.22f, 0.3f, 0.55f),
                    new Color(0.95f, 0.97f, 1f), -25f),
            };
            yield return new Variant
            {
                Name = "C_low_sun",
                Ground = new[]
                {
                    Srgb(0xB0, 0xAD, 0xA8), Srgb(0x88, 0x86, 0x82), Srgb(0x48, 0x48, 0x50), Srgb(0x78, 0x75, 0x72),
                    Srgb(0x5C, 0x5A, 0x5C), Srgb(0x9E, 0x9B, 0x96),
                },
                SkyTop = new Color(0.003f, 0.004f, 0.014f),
                SkyScale = new Vector4(0.45f, 0.3f, 0.4f, 0.8f),
                FogColor = new Color(0.03f, 0.03f, 0.045f),
                FogScale = 0.12f,
                Ambient = new[]
                {
                    new Color(0.03f, 0.04f, 0.07f), new Color(0.02f, 0.025f, 0.045f), new Color(0.005f, 0.005f, 0.01f),
                },
                LightColor = new Color(1f, 0.88f, 0.7f),
                LightIntensity = 3f,
                LightSource = new Vector2(20f, -90f),
                LightShadow = 1f,
                FillColor = new Color(0.55f, 0.66f, 1f),
                FillIntensity = 0.35f,
                FillSource = new Vector2(20f, 215f),
                Grade = profile => Grade(profile, -0.15f, 28f, -8f, new Color(0.25f, 0.33f, 0.55f),
                    new Color(1f, 0.82f, 0.6f), 10f),
            };
        }

        private static bool CaptureVariant(Variant variant, string output)
        {
            EditorSceneManager.OpenScene(CaptureAutomation.MainScenePath, OpenSceneMode.Single);
            var restore = new List<(Material Material, Texture Texture)>();
            var temporary = new ArtLightingPreview.TemporaryObjects();
            var sky = new Dictionary<int, Vector4>();
            foreach (int id in new[] { SkyTop, HorizonGlow, HorizonBand, StarParams, MilkyWay, HazeColor, Haze })
            {
                sky[id] = Shader.GetGlobalVector(id);
            }

            try
            {
                Bounds rover = Bounds(Root("[Rover]"));
                Vector4 earth = Shader.GetGlobalVector(EarthDirection);
                Vector3 heading = new Vector3(earth.x, 0f, earth.z).normalized;
                Apply(variant, heading, temporary, restore);
                CameraPoseSet poses = Frames(rover.center, heading);
                return CaptureAutomation.CapturePoses(poses, output, "look_" + variant.Name) > 0;
            }
            finally
            {
                foreach ((Material material, Texture texture) in restore)
                {
                    material.SetTexture(BaseMap, texture);
                }

                foreach (KeyValuePair<int, Vector4> global in sky)
                {
                    Shader.SetGlobalVector(global.Key, global.Value);
                }

                temporary.Dispose();
            }
        }

        // The owner's frame (07 at the dock, the mast, Earth over the rim) from behind and above 07 looking toward
        // Earth, a wide basin from above the pad, and a close three-quarter of 07.
        private static CameraPoseSet Frames(Vector3 rover, Vector3 heading)
        {
            Vector3 at = rover - Vector3.up * 0.6f;
            Vector3 side = Vector3.Cross(Vector3.up, heading);
            var poses = new[]
            {
                ArtLightingPreview.Pose("dock",
                    ArtLightingPreview.Point(at - heading * 7f + side * 1.5f + Vector3.up * 2.6f),
                    ArtLightingPreview.Point(at + heading * 20f + Vector3.up * 5f), 58f),
                new CameraPose { name = "wide_basin", position = new[] { 0.78f, 8f, -16f },
                    euler = new[] { 8f, -5f, 0f }, fov = 60f },
                ArtLightingPreview.Pose("close_07", ArtLightingPreview.Point(at + heading * 2.6f + side * 1.8f +
                    Vector3.up * 1.3f), ArtLightingPreview.Point(at + Vector3.up * 0.8f), 40f),
            };
            return new CameraPoseSet { width = 1920, height = 1080, postProcessing = true, poses = poses };
        }

        private static void Apply(Variant variant, Vector3 heading, ArtLightingPreview.TemporaryObjects temporary,
            List<(Material, Texture)> restore)
        {
            if (variant.Ground != null)
            {
                Repaint(variant.Ground, temporary, restore);
            }

            if (variant.SkyTop.HasValue)
            {
                Vector4 top = Shader.GetGlobalVector(SkyTop);
                Color c = variant.SkyTop.Value;
                Shader.SetGlobalVector(SkyTop, new Vector4(c.r, c.g, c.b, top.w));
            }

            Vector4 glow = Shader.GetGlobalVector(HorizonGlow);
            Shader.SetGlobalVector(HorizonGlow, new Vector4(glow.x * variant.SkyScale.x, glow.y * variant.SkyScale.x,
                glow.z * variant.SkyScale.x, glow.w * variant.SkyScale.y));
            Vector4 band = Shader.GetGlobalVector(HorizonBand);
            Shader.SetGlobalVector(HorizonBand, new Vector4(band.x * variant.SkyScale.z, band.y * variant.SkyScale.z,
                band.z * variant.SkyScale.z, band.w));
            Vector4 stars = Shader.GetGlobalVector(StarParams);
            Shader.SetGlobalVector(StarParams, new Vector4(stars.x, stars.y * variant.StarScale, stars.z, stars.w));
            Vector4 milky = Shader.GetGlobalVector(MilkyWay);
            Shader.SetGlobalVector(MilkyWay, new Vector4(milky.x, milky.y, milky.z, milky.w * variant.SkyScale.w));

            if (variant.FogColor.HasValue)
            {
                RenderSettings.fogColor = variant.FogColor.Value;
                Vector4 haze = Shader.GetGlobalVector(HazeColor);
                Color c = variant.FogColor.Value.linear;
                Shader.SetGlobalVector(HazeColor, new Vector4(c.r, c.g, c.b, haze.w * variant.FogScale));
            }

            RenderSettings.fogDensity *= variant.FogScale;
            Vector4 hazeDensity = Shader.GetGlobalVector(Haze);
            Shader.SetGlobalVector(Haze, new Vector4(hazeDensity.x * variant.FogScale, hazeDensity.y, hazeDensity.z,
                hazeDensity.w));

            if (variant.Ambient != null)
            {
                RenderSettings.ambientSkyColor = variant.Ambient[0];
                RenderSettings.ambientEquatorColor = variant.Ambient[1];
                RenderSettings.ambientGroundColor = variant.Ambient[2];
                DynamicGI.UpdateEnvironment();
            }

            Light earthlight = RenderSettings.sun;
            earthlight.color = variant.LightColor ?? earthlight.color;
            earthlight.intensity = variant.LightIntensity ?? earthlight.intensity;
            earthlight.shadowStrength = variant.LightShadow >= 0f ? variant.LightShadow : earthlight.shadowStrength;
            if (variant.LightSource.HasValue)
            {
                earthlight.transform.rotation = Quaternion.LookRotation(-Source(variant.LightSource.Value, heading));
            }

            Light fill = Search("Fill Light").GetComponent<Light>();
            fill.color = variant.FillColor ?? fill.color;
            fill.intensity = variant.FillIntensity ?? fill.intensity;
            if (variant.FillSource.HasValue)
            {
                fill.transform.rotation = Quaternion.LookRotation(-Source(variant.FillSource.Value, heading));
            }

            if (variant.Grade != null)
            {
                var profile = ScriptableObject.CreateInstance<VolumeProfile>();
                temporary.Add(profile);
                variant.Grade(profile);
                var grade = new GameObject("LookVariantGrade").AddComponent<Volume>();
                grade.isGlobal = true;
                grade.priority = 100f;
                grade.sharedProfile = profile;
                temporary.Add(grade.gameObject);
            }
        }

        // The ground and rock swatches change everywhere at once: in the palette texture the props sample, and in
        // the terrain's baked vertex colours (each shifted by its nearest old swatch's change, keeping its shading).
        private static void Repaint(Color32[] colours, ArtLightingPreview.TemporaryObjects temporary,
            List<(Material, Texture)> restore)
        {
            Color32[] pixels = PaletteAssetBuilder.CreateBasePixels();
            var fromSurface = new Color32[Ground.Length];
            for (int i = 0; i < Ground.Length; i++)
            {
                fromSurface[i] = Palette.GetSurface(Ground[i]);
            }

            for (int p = 0; p < pixels.Length; p++)
            {
                for (int i = 0; i < Ground.Length; i++)
                {
                    if (pixels[p].Equals(fromSurface[i]))
                    {
                        pixels[p] = colours[i];
                    }
                }
            }

            Material palette = PaletteAssetBuilder.LoadMaterial();
            Texture original = palette.GetTexture(BaseMap);
            var texture = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false, false)
            {
                filterMode = original.filterMode,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false);
            temporary.Add(texture);

            var seen = new HashSet<Material>();
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material != null && material.HasProperty(BaseMap) &&
                            material.GetTexture(BaseMap) == original && seen.Add(material))
                        {
                            restore.Add((material, original));
                            material.SetTexture(BaseMap, texture);
                        }
                    }

                    if (renderer.sharedMaterial != null && renderer.sharedMaterial.name.StartsWith("M_Ground",
                        StringComparison.Ordinal))
                    {
                        Recolour(renderer.GetComponent<MeshFilter>(), colours);
                    }
                }
            }
        }

        private static void Recolour(MeshFilter filter, Color32[] colours)
        {
            if (filter == null || filter.sharedMesh == null)
            {
                return;
            }

            Mesh mesh = filter.sharedMesh;
            Color32[] vertex = mesh.colors32;
            for (int v = 0; v < vertex.Length; v++)
            {
                int nearest = 0;
                float best = float.MaxValue;
                for (int i = 0; i < Ground.Length; i++)
                {
                    Color32 old = Palette.Get(Ground[i]);
                    float d = Sq(vertex[v].r - old.r) + Sq(vertex[v].g - old.g) + Sq(vertex[v].b - old.b);
                    if (d < best)
                    {
                        best = d;
                        nearest = i;
                    }
                }

                Color32 from = Palette.Get(Ground[nearest]);
                Color32 to = colours[nearest];
                vertex[v] = new Color32(Shift(vertex[v].r, from.r, to.r), Shift(vertex[v].g, from.g, to.g),
                    Shift(vertex[v].b, from.b, to.b), vertex[v].a);
            }

            mesh.colors32 = vertex;
        }

        private static void Grade(VolumeProfile profile, float exposure, float contrast, float saturation,
            Color shadows, Color highlights, float balance)
        {
            var colour = profile.Add<ColorAdjustments>(true);
            colour.postExposure.Override(exposure);
            colour.contrast.Override(contrast);
            colour.saturation.Override(saturation);
            var split = profile.Add<SplitToning>(true);
            split.shadows.Override(shadows);
            split.highlights.Override(highlights);
            split.balance.Override(balance);
        }

        private static Vector3 Source(Vector2 elevationBearing, Vector3 heading)
        {
            Vector3 flat = Quaternion.AngleAxis(elevationBearing.y, Vector3.up) * heading;
            float elevation = elevationBearing.x * Mathf.Deg2Rad;
            return flat * Mathf.Cos(elevation) + Vector3.up * Mathf.Sin(elevation);
        }

        private static Bounds Bounds(Transform root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        private static Transform Root(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name == name)
                {
                    return root.transform;
                }
            }

            throw new InvalidOperationException($"LookVariants: Main.unity has no root '{name}'.");
        }

        private static Transform Search(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                {
                    if (node.name == name)
                    {
                        return node;
                    }
                }
            }

            throw new InvalidOperationException($"LookVariants: Main.unity has no '{name}'.");
        }

        private static Color32 Srgb(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 0xFF);
        }

        private static float Sq(int v)
        {
            return v * v;
        }

        private static byte Shift(byte value, byte from, byte to)
        {
            return (byte)Mathf.Clamp(value + to - from, 0, 255);
        }
    }
}
