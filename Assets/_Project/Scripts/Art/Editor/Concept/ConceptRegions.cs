using System;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoonProject.Editor.Automation;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor.Concept
{
    /// <summary>
    /// Region mood frames for the owner's 0.6+ review (docs/design/0.6-world-regions.md §3), preview only: one wide
    /// shot per region with a blockout terrain, its light, colour and sky, and one landmark. Each opens Main.unity
    /// (never saved) for the game's sky, Earth and grade, hides the basin's own geometry and stages the region in its
    /// place; the basin itself is shot as it stands, as the reference.
    /// </summary>
    internal static class ConceptRegions
    {
        public static readonly string[] Names = { "basin", "long_mare", "rim_terraces", "shadowed_crater", "lava_tube",
            "peak_road" };

        private static readonly int EarthDirection = Shader.PropertyToID("_MoonEarthDirection");
        private static readonly int EarthAxisX = Shader.PropertyToID("_MoonEarthAxisX");

        /// <summary>Stages <paramref name="region"/> and captures its frame; false if unknown or empty.</summary>
        public static bool Capture(string region, Material material, string output)
        {
            EditorSceneManager.OpenScene(CaptureAutomation.MainScenePath, OpenSceneMode.Single);
            var temporary = new ArtLightingPreview.TemporaryObjects();
            Vector4 earth = Shader.GetGlobalVector(EarthDirection);
            Vector4 earthAxis = Shader.GetGlobalVector(EarthAxisX);
            try
            {
                CameraPose pose;
                switch (region)
                {
                    case "basin":
                        pose = new CameraPose { name = region, position = new[] { 0.78f, 8f, -16f },
                            euler = new[] { 8f, -5f, 0f }, fov = 60f };
                        break;
                    case "long_mare":
                        pose = LongMare(material, temporary, earth);
                        break;
                    case "rim_terraces":
                        pose = RimTerraces(material, temporary);
                        break;
                    case "shadowed_crater":
                        pose = ShadowedCrater(material, temporary);
                        break;
                    case "lava_tube":
                        pose = LavaTube(material, temporary);
                        break;
                    case "peak_road":
                        pose = PeakRoad(material, temporary);
                        break;
                    default:
                        Debug.LogError($"ConceptRegions: unknown region '{region}' ({string.Join("|", Names)}).");
                        return false;
                }

                var poses = new CameraPoseSet
                {
                    width = 1920, height = 1080, postProcessing = true, poses = new[] { pose },
                };
                return CaptureAutomation.CapturePoses(poses, output, "concept_region") > 0;
            }
            finally
            {
                Shader.SetGlobalVector(EarthDirection, earth);
                Shader.SetGlobalVector(EarthAxisX, earthAxis);
                temporary.Dispose();
            }
        }

        // The Long Mare: near-black basalt to the horizon with a pale swirl, Earth huge and low, a low sun throwing
        // long shadows, and the old comms dish's amber blink far across the plain.
        private static CameraPose LongMare(Material material, ArtLightingPreview.TemporaryObjects temporary,
            Vector4 earth)
        {
            HideBasin();
            var toEarth = new Vector3(earth.x, 0f, earth.z);
            Vector3 ahead = toEarth.normalized;
            Vector3 lowEarth = (ahead + Vector3.up * 0.2f).normalized;
            float radius = earth.w * 3.4f;
            Shader.SetGlobalVector(EarthDirection, new Vector4(lowEarth.x, lowEarth.y, lowEarth.z, radius));
            Shader.SetGlobalVector(EarthAxisX, WithW(Shader.GetGlobalVector(EarthAxisX), Mathf.Tan(radius)));
            Sun(Quaternion.LookRotation(Quaternion.Euler(0f, 75f, 0f) * -ahead + Vector3.down * 0.14f),
                new Color(1f, 0.93f, 0.82f), 1.5f);
            Fog(new Color(0.06f, 0.07f, 0.12f));
            Vector3 origin = ahead * 400f;
            Terrain(material, temporary, origin, 1000f, 64, (x, z) => Swell(x, z, 0.9f, 70f), (x, z, h) =>
            {
                float swirl = Mathf.Sin(x * 0.012f + Mathf.Sin(z * 0.009f) * 2.6f) + Mathf.Sin(z * 0.02f + x * 0.004f);
                return swirl > 1.2f ? PaletteSwatch.DustLight
                    : h > 0.4f ? PaletteSwatch.RockDark : PaletteSwatch.Charcoal;
            });
            Scatter(material, temporary, Vector3.zero, ahead, 60, 120f, 0.3f, 1.2f);
            Vector3 dish = ahead * 520f + Vector3.Cross(Vector3.up, ahead) * 90f;
            Put(ConceptRegionMeshes.CommsDish(), dish, Quaternion.LookRotation(-ahead), material, temporary);
            Glow(temporary, dish + Vector3.up * 26f, Palette.Get(PaletteSwatch.WarmLamp), 30f, 120f);
            temporary.Add(ArtLightingPreview.Rover(temporary, ahead * 14f + Vector3.Cross(Vector3.up, ahead) * 3f,
                Yaw(ahead) + 20f).gameObject);
            return Look("long_mare", Vector3.up * 7f, ahead * 70f + Vector3.up * 9f, 62f);
        }

        // The Rim Terraces: pale gold highland steps climbing away, sharp bright light, and Atlas's work lamp high on a
        // ledge.
        private static CameraPose RimTerraces(Material material, ArtLightingPreview.TemporaryObjects temporary)
        {
            HideBasin();
            Sun(Quaternion.Euler(38f, 200f, 0f), new Color(1f, 0.9f, 0.72f), 1.9f);
            Fog(new Color(0.1f, 0.1f, 0.15f));
            Terrain(material, temporary, new Vector3(0f, 0f, 160f), 420f, 64, Terraces, (x, z, h) =>
                Mathf.Repeat(h, 9f) < 0.9f ? PaletteSwatch.RockLight : h > 20f ? PaletteSwatch.Cream :
                PaletteSwatch.DustLight);
            Vector3 ledge = new Vector3(-18f, Terraces(-18f, 118f), 118f);
            Put(ConceptRobotMeshes.Atlas(), ledge, Quaternion.Euler(0f, 160f, 0f), material, temporary);
            Glow(temporary, ledge + new Vector3(0f, 4.1f, -1.5f), Palette.Get(PaletteSwatch.WarmLamp), 20f, 50f);
            temporary.Add(ArtLightingPreview.Rover(temporary, new Vector3(3f, Terraces(3f, 14f), 14f), 200f)
                .gameObject);
            return Look("rim_terraces", new Vector3(6f, 4f, -6f), new Vector3(-6f, 20f, 100f), 55f);
        }

        // The Shadowed Crater: a black bowl in eternal night, frost glittering only in 07's twin floods, and Moss's
        // faint green glow far across the floor.
        private static CameraPose ShadowedCrater(Material material, ArtLightingPreview.TemporaryObjects temporary)
        {
            HideBasin();
            Sun(Quaternion.Euler(30f, 0f, 0f), new Color(0.5f, 0.6f, 1f), 0.03f);
            RenderSettings.ambientSkyColor = new Color(0.02f, 0.025f, 0.05f);
            RenderSettings.ambientEquatorColor = new Color(0.015f, 0.02f, 0.04f);
            RenderSettings.ambientGroundColor = Color.black;
            Fog(new Color(0.01f, 0.012f, 0.025f));
            Terrain(material, temporary, new Vector3(0f, 0f, 60f), 360f, 64, Bowl, (x, z, h) =>
                h > 6f ? PaletteSwatch.RockDark : PaletteSwatch.DustShadow);
            Put(ConceptRegionMeshes.Frost(5000, 40f, 31), new Vector3(0f, 0.16f, 22f), Quaternion.identity, material,
                temporary);
            Transform rover = ArtLightingPreview.Rover(temporary, new Vector3(0f, 0f, 2f), 0f);
            temporary.Add(rover.gameObject);
            for (int side = -1; side <= 1; side += 2)
            {
                Light flood = ArtLightingPreview.NewLight("Flood", LightType.Spot, new Color(0.92f, 0.96f, 1f), 60f);
                flood.transform.SetPositionAndRotation(new Vector3(side * 0.35f, 1.1f, 2.8f),
                    Quaternion.Euler(9f, side * 12f, 0f));
                flood.range = 60f;
                flood.spotAngle = 66f;
                flood.innerSpotAngle = 30f;
                flood.shadows = LightShadows.Soft;
                temporary.Add(flood.gameObject);
            }

            Put(ConceptRobotMeshes.Moss(), new Vector3(-14f, 0f, 70f), Quaternion.Euler(0f, 150f, 0f), material,
                temporary);
            Glow(temporary, new Vector3(-14f, 1.2f, 70f), Palette.Get(PaletteSwatch.BiolumTeal), 14f, 28f);
            return Look("shadowed_crater", new Vector3(1.8f, 2.4f, -3.5f), new Vector3(-2f, 0.2f, 40f), 58f);
        }

        // The lava tube under the mare: a smooth vaulted basalt cave, a column of sunlight falling through a skylight
        // onto the floor, and crystals glinting in the walls. Awe, not fear.
        private static CameraPose LavaTube(Material material, ArtLightingPreview.TemporaryObjects temporary)
        {
            HideBasin();
            Sun(Quaternion.Euler(78f, 30f, 0f), new Color(1f, 0.95f, 0.85f), 0f);
            RenderSettings.ambientSkyColor = new Color(0.05f, 0.045f, 0.05f);
            RenderSettings.ambientEquatorColor = new Color(0.035f, 0.03f, 0.035f);
            RenderSettings.ambientGroundColor = Color.black;
            Fog(new Color(0.02f, 0.02f, 0.03f));
            const float skylight = 70f;
            Put(ConceptRegionMeshes.Tube(14f, 150f, skylight), Vector3.zero, Quaternion.identity, material, temporary);
            Light shaft = ArtLightingPreview.NewLight("Skylight", LightType.Spot, new Color(1f, 0.94f, 0.82f), 400f);
            shaft.transform.SetPositionAndRotation(new Vector3(0f, 13f, skylight), Quaternion.Euler(90f, 0f, 0f));
            shaft.range = 30f;
            shaft.spotAngle = 40f;
            shaft.innerSpotAngle = 26f;
            shaft.shadows = LightShadows.Soft;
            temporary.Add(shaft.gameObject);
            Put(ConceptRegionMeshes.Motes(700, 4.5f, 12f, 41), new Vector3(0f, 0f, skylight), Quaternion.identity,
                material, temporary);
            Glow(temporary, new Vector3(0f, 1.5f, skylight), new Color(1f, 0.85f, 0.65f), 14f, 30f);
            temporary.Add(ArtLightingPreview.Rover(temporary, new Vector3(1.2f, 0f, skylight - 12f), 0f).gameObject);
            return Look("lava_tube", new Vector3(-3f, 2.6f, 32f), new Vector3(0f, 3f, skylight), 60f);
        }

        // The Peak road: switchbacks climbing a steep grey peak to the great dish on its shoulder, its red beacon
        // the one light you have seen since the first frame.
        private static CameraPose PeakRoad(Material material, ArtLightingPreview.TemporaryObjects temporary)
        {
            HideBasin();
            Terrain(material, temporary, new Vector3(0f, 0f, 260f), 640f, 90, Peak, (x, z, h) =>
                h > 70f ? PaletteSwatch.RockLight : PaletteSwatch.DustMid);
            Put(ConceptRegionMeshes.Switchbacks(Peak, -80f, 30f, 120f, 270f, 6), Vector3.zero, Quaternion.identity,
                material, temporary);
            Vector3 top = new Vector3(-30f, Peak(-30f, 300f), 300f);
            Put(ConceptRegionMeshes.GreatDish(), top, Quaternion.Euler(0f, 160f, 0f), material, temporary, 2f);
            Glow(temporary, top + new Vector3(0f, 84f, 0f), Palette.Get(PaletteSwatch.SignalGlass), 60f, 300f);
            temporary.Add(ArtLightingPreview.Rover(temporary, new Vector3(4f, Peak(4f, 20f), 20f), 0f).gameObject);
            return Look("peak_road", new Vector3(10f, 8f, -10f), new Vector3(-22f, 80f, 270f), 46f);
        }

        // Every renderer and lamp the basin brings (terrain, scatter, the base, the sites) goes dark, so only the
        // sky, Earth, the sun, the grade and the staged region remain, and the region's own lamps get every light slot.
        private static void HideBasin()
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.enabled = renderer.gameObject.name == "Earth";
                }

                foreach (Light light in root.GetComponentsInChildren<Light>(true))
                {
                    light.enabled = light == RenderSettings.sun;
                }
            }
        }

        private static void Sun(Quaternion rotation, Color color, float intensity)
        {
            Light sun = RenderSettings.sun;
            if (sun == null)
            {
                throw new InvalidOperationException("ConceptRegions: Main.unity has no sun light.");
            }

            sun.transform.rotation = rotation;
            sun.color = color;
            sun.intensity = intensity;
        }

        private static void Fog(Color color)
        {
            RenderSettings.fogColor = color;
        }

        private static void Glow(ArtLightingPreview.TemporaryObjects temporary, Vector3 at, Color color,
            float intensity, float range)
        {
            Light glow = ArtLightingPreview.NewLight("Glow", LightType.Point, color, intensity);
            glow.transform.position = at;
            glow.range = range;
            temporary.Add(glow.gameObject);
        }

        private static void Terrain(Material material, ArtLightingPreview.TemporaryObjects temporary, Vector3 centre,
            float size, int cells, Func<float, float, float> height, Func<float, float, float, PaletteSwatch> paint)
        {
            LowPolyMeshBuilder ground = ConceptRegionMeshes.Heightfield(centre, size, cells, height, paint);
            GameObject placed = ArtLightingPreview.MeshObject("ConceptTerrain", ground.ToMesh("ConceptTerrain"),
                material, temporary);
            temporary.Add(placed);
        }

        private static void Scatter(Material material, ArtLightingPreview.TemporaryObjects temporary, Vector3 from,
            Vector3 ahead, int count, float reach, float smallest, float largest)
        {
            var rocks = new LowPolyMeshBuilder(count * 60);
            Vector3 side = Vector3.Cross(Vector3.up, ahead);
            for (int i = 0; i < count; i++)
            {
                float along = 6f + (i * 37 % 100) / 100f * reach;
                float across = ((i * 61 % 100) / 100f - 0.5f) * along * 1.2f;
                Vector3 at = from + ahead * along + side * across;
                RockGenerator.Build(rocks, 900 + i, Mathf.Lerp(smallest, largest, (i * 13 % 10) / 10f),
                    (RockStyle)(i % 4), At(at));
            }

            temporary.Add(ArtLightingPreview.MeshObject("ConceptRocks", rocks.ToMesh("ConceptRocks"), material,
                temporary));
        }

        private static void Put(LowPolyMeshBuilder mesh, Vector3 at, Quaternion turn, Material material,
            ArtLightingPreview.TemporaryObjects temporary, float scale = 1f)
        {
            GameObject placed = ArtLightingPreview.MeshObject("Concept", mesh.ToMesh("Concept"), material, temporary);
            placed.transform.SetPositionAndRotation(at, turn);
            placed.transform.localScale = Vector3.one * scale;
            temporary.Add(placed);
        }

        private static CameraPose Look(string name, Vector3 eye, Vector3 target, float fov)
        {
            return ArtLightingPreview.Pose(name, ArtLightingPreview.Point(eye), ArtLightingPreview.Point(target), fov);
        }

        private static float Yaw(Vector3 direction)
        {
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }

        private static Vector4 WithW(Vector4 v, float w)
        {
            return new Vector4(v.x, v.y, v.z, w);
        }

        private static float Swell(float x, float z, float amplitude, float wavelength)
        {
            return amplitude * (Mathf.Sin(x / wavelength * 6.28f) * 0.5f + Mathf.Sin(z / wavelength * 4.1f + x * 0.01f)
                * 0.5f + 0.5f);
        }

        // Steps climbing north: a flat tread, then a short steep riser, every 30 m, each about 9 m higher.
        private static float Terraces(float x, float z)
        {
            float run = Mathf.Max(0f, z - 30f + Mathf.Sin(x * 0.02f) * 8f);
            float step = Mathf.Floor(run / 30f);
            float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((run - step * 30f - 24f) / 6f));
            return (step + rise) * 9f + Swell(x, z, 0.6f, 25f);
        }

        // A bowl 60 m deep and about 300 m across, its rim rising behind the floor the camera stands on.
        private static float Bowl(float x, float z)
        {
            float r = new Vector2(x, z - 60f).magnitude;
            float rim = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((r - 90f) / 60f)) * 60f;
            return rim + Swell(x, z, 0.12f, 18f);
        }

        // A steep peak whose summit stands 300 m north, about 150 m high.
        private static float Peak(float x, float z)
        {
            float r = new Vector2(x + 30f, z - 300f).magnitude;
            return Mathf.Max(0f, 150f - r * 0.75f) + Swell(x, z, 1.5f, 40f);
        }

    }
}
