using NUnit.Framework;
using UnityEngine;
using MoonProject.Art;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// Pillar 6 ("Alone, and at peace") guards for the atmosphere under look A "Moon grey" (the owner's pick for 0.5.5):
    /// near ground and the far rim stay crisp under a thin veil while the distant ranges soften,
    /// the earthlight is low enough to rake long shadows yet leaves most of the floor lit and never exceeds the light
    /// Art caps its surfaces against, the fill softly lifts the side the earthlight leaves dark without ever adding to
    /// a face's brightest light, lit dust never crosses the bloom threshold, and Earth glows softly, far under the
    /// warm lamps of home.
    /// </summary>
    public sealed class AtmosphereTests
    {
        // The rover's view of the base at play distances, the far rim and The Peak, and the world's edge (metres).
        private const float NearDistance = 150f;
        private const float RimDistance = 450f;
        private const float EdgeDistance = 1000f;
        private const float MaxNearFog = 0.1f;
        private const float MinRimFog = 0.05f;
        private const float MaxRimFog = 0.15f;
        private const float MinEdgeFog = 0.25f;

        private const float MinLightElevation = 12f;
        private const float MaxLightElevation = 25f;
        private const float MaxShadowedFloor = 0.25f;
        private const float FloorRadius = 280f;
        private const float FloorStep = 20f;
        private const float ShadowReach = 700f;
        private const float ShadowStep = 4f;
        private const float DustStep = 6f;

        // The basin haze, seen from 07's chase height: a light veil at play distance (like the fog's), a faint layer
        // over the far floor, and the rim's crest (well above the haze's height scale) left nearly clear.
        private const float HazeEyeHeight = 3.5f;
        private const float MaxNearHaze = 0.08f;
        private const float MinFarFloorHaze = 0.05f;
        private const float CrestHeight = 60f;
        private const float MaxCrestHazeShare = 0.2f;

        // Art rounds its reference light to two decimals.
        private const float ReferenceTolerance = 0.01f;

        // The fill lifts the earthlight's shadow side well above the flat ambient, yet stays a few stops under the
        // earthlight (lit faces keep their contrast) and lights top faces only faintly. Normals are swept this finely.
        private const float MinFillOverAmbient = 3f;
        private const float MaxFillShare = 0.2f;
        private const float MaxFillOnTopFaces = 0.05f;
        private const float NormalStep = 2f;

        // Earth may just touch the bloom (a soft glow); its limb and halo in the sky stay under it.
        private const float EarthGlowCap = 1.1f;

        // Earth is a pale, cool jewel: no face or rim as saturated as the palette's own cyan ocean (HSV saturation).
        private const float MaxEarthSaturation = 0.6f;
        private static readonly PaletteSwatch[] EarthSwatches =
            { PaletteSwatch.EarthOcean, PaletteSwatch.EarthLand, PaletteSwatch.Cream };

        private AtmosphereSettings _atmosphere;
        private SkySettings _sky;
        private MoonSurface _surface;

        [OneTimeSetUp]
        public void CreateWorld()
        {
            _atmosphere = new AtmosphereSettings();
            _sky = new SkySettings();
            _surface = new MoonSurface(new SurfaceSettings(), WorldSettings.DefaultSeed);
        }

        [Test]
        public void Fog_KeepsTheBasinCrisp_AndSoftensTheDistantRanges()
        {
            Assert.LessOrEqual(Fog(NearDistance), MaxNearFog, "the base must stay crisp at play distances");
            Assert.GreaterOrEqual(Fog(RimDistance), MinRimFog, "the far rim and The Peak keep a faint veil");
            Assert.LessOrEqual(Fog(RimDistance), MaxRimFog, "the far rim stays crisp under look A");
            Assert.GreaterOrEqual(Fog(EdgeDistance), MinEdgeFog, "the distant ranges at the world's edge soften");
        }

        [Test]
        public void Haze_LiesLowInTheBasin_LeavingNearGroundCrispAndTheCrestClear()
        {
            var eye = new Vector3(0f, HazeEyeHeight, 0f);
            Assert.LessOrEqual(_atmosphere.HazeAmount(eye, new Vector3(0f, 0f, NearDistance)), MaxNearHaze,
                "the base must stay crisp at play distances");
            float farFloor = _atmosphere.HazeAmount(eye, new Vector3(0f, 0f, RimDistance));
            Assert.GreaterOrEqual(farFloor, MinFarFloorHaze, "the far floor keeps a faint haze");
            float crest = _atmosphere.HazeAmount(eye, new Vector3(0f, CrestHeight, RimDistance));
            Assert.Less(crest, farFloor * MaxCrestHazeShare, "the rim's crest stays above the haze");
        }

        [Test]
        public void Earthlight_IsLow_YetLeavesMostOfTheFloorLit()
        {
            Assert.That(_atmosphere.LightElevation, Is.InRange(MinLightElevation, MaxLightElevation));
            Vector3 toLight = WorldAtmosphere.LightSourceDirection(_atmosphere, _sky);
            int floor = 0;
            int shadowed = 0;
            for (float z = -FloorRadius; z <= FloorRadius; z += FloorStep)
            {
                for (float x = -FloorRadius; x <= FloorRadius; x += FloorStep)
                {
                    if (x * x + z * z > FloorRadius * FloorRadius || !_surface.IsDrivable(x, z))
                    {
                        continue;
                    }

                    floor++;
                    var ground = new Vector3(x, _surface.SampleHeight(x, z) + 0.5f, z);
                    for (float d = ShadowStep; d < ShadowReach; d += ShadowStep)
                    {
                        Vector3 ray = ground + toLight * d;
                        if (_surface.SampleHeight(ray.x, ray.z) > ray.y)
                        {
                            shadowed++;
                            break;
                        }
                    }
                }
            }

            Assert.LessOrEqual((float)shadowed / floor, MaxShadowedFloor, "the rim's shadow covers too much floor");
        }

        [Test]
        public void Earthlight_StaysAtOrUnderArtsReferenceLight()
        {
            // Art darkens bright surfaces until they stay under its bloom threshold in this light: a brighter
            // earthlight than the threshold allows would make cream panels glow like lamps.
            Color light = _atmosphere.LightColor.linear * _atmosphere.LightIntensity + _atmosphere.AmbientSky.linear;
            Color reference = ReferenceLight();
            Assert.LessOrEqual(light.r, reference.r + ReferenceTolerance, "red");
            Assert.LessOrEqual(light.g, reference.g + ReferenceTolerance, "green");
            Assert.LessOrEqual(light.b, reference.b + ReferenceTolerance, "blue");
        }

        [Test]
        public void Fill_SoftlyLiftsTheSideTheEarthlightLeavesDark()
        {
            Vector3 toLight = WorldAtmosphere.LightSourceDirection(_atmosphere, _sky);
            Vector3 toFill = WorldAtmosphere.FillSourceDirection(_atmosphere, _sky);
            Assert.Less(Vector3.Dot(toLight, toFill), 0f, "the fill must come from the earthlight's dark side");

            float fill = Fill().grayscale;
            Assert.GreaterOrEqual(fill, MinFillOverAmbient * _atmosphere.AmbientEquator.linear.grayscale,
                "the fill barely lifts the shadow side above the flat ambient");
            Assert.LessOrEqual(fill, MaxFillShare * Earthlight().grayscale,
                "the fill flattens the earthlight's modelling");
            Assert.LessOrEqual(fill * Mathf.Max(0f, toFill.y), MaxFillOnTopFaces,
                "the fill lights top faces, which belong to the earthlight");
        }

        [Test]
        public void Fill_NeverLightsAFacePastArtsReferenceLight()
        {
            // Art caps its surfaces against the earthlight square-on: no face may gather more from both lights.
            Vector3 toLight = WorldAtmosphere.LightSourceDirection(_atmosphere, _sky);
            Vector3 toFill = WorldAtmosphere.FillSourceDirection(_atmosphere, _sky);
            Color reference = ReferenceLight();
            for (float pitch = -90f; pitch <= 90f; pitch += NormalStep)
            {
                for (float yaw = 0f; yaw < 360f; yaw += NormalStep)
                {
                    Vector3 normal = Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
                    Color light = _atmosphere.AmbientSky.linear
                        + Earthlight() * Mathf.Max(0f, Vector3.Dot(normal, toLight))
                        + Fill() * Mathf.Max(0f, Vector3.Dot(normal, toFill));
                    string face = $"face ({pitch}, {yaw})";
                    Assert.LessOrEqual(light.r, reference.r + ReferenceTolerance, face + " red");
                    Assert.LessOrEqual(light.g, reference.g + ReferenceTolerance, face + " green");
                    Assert.LessOrEqual(light.b, reference.b + ReferenceTolerance, face + " blue");
                }
            }
        }

        [Test]
        public void Earth_GlowsSoftly_FarUnderTheWarmLamps()
        {
            var post = new PostProcessSettings();
            Color rim = _sky.EarthAtmosphere.linear * _sky.EarthRim;
            float brightest = 0f;
            foreach (PaletteSwatch swatch in EarthSwatches)
            {
                Color face = EarthMeshBuilder.FaceColor(swatch, _sky) * _sky.EarthGlow + rim;
                brightest = Mathf.Max(brightest, face.maxColorComponent);
            }

            Assert.LessOrEqual(brightest, EarthGlowCap * post.BloomThreshold,
                "Earth's lit side and rim bloom too much");
            Assert.Less(brightest, Palette.WarmLampGlow, "Earth outshines the lamps of home");
            Color sky = _sky.EarthAtmosphere.linear * (_sky.EarthLimb + _sky.EarthHaloStrength);
            Assert.Less(sky.maxColorComponent, post.BloomThreshold, "Earth's limb and halo in the sky bloom");
        }

        [Test]
        public void Earth_IsAPaleCoolJewel_NotASaturatedCyan()
        {
            foreach (PaletteSwatch swatch in EarthSwatches)
            {
                Assert.LessOrEqual(Saturation(EarthMeshBuilder.FaceColor(swatch, _sky).gamma), MaxEarthSaturation,
                    $"Earth's {swatch} faces");
            }

            Assert.LessOrEqual(Saturation(_sky.EarthAtmosphere), MaxEarthSaturation, "Earth's rim and halo");
        }

        [Test]
        public void LitDust_StaysUnderTheBloomThreshold()
        {
            Vector3 toLight = WorldAtmosphere.LightSourceDirection(_atmosphere, _sky);
            var painter = new TerrainPainter(new TerrainPaintSettings(), WorldSettings.DefaultSeed, toLight,
                _surface.PadRadius);
            Color light = _atmosphere.LightColor.linear * _atmosphere.LightIntensity + _atmosphere.AmbientSky.linear;
            float brightest = 0f;
            for (float z = -FloorRadius; z <= FloorRadius; z += DustStep)
            {
                for (float x = -FloorRadius; x <= FloorRadius; x += DustStep)
                {
                    SurfaceSample sample = _surface.Sample(x, z);
                    Color dust = ((Color)painter.Ground(new Vector3(x, sample.Height, z), sample)).linear;
                    brightest = Mathf.Max(brightest, Mathf.Max(dust.r * light.r, Mathf.Max(dust.g * light.g,
                        dust.b * light.b)));
                }
            }

            Assert.Less(brightest, new PostProcessSettings().BloomThreshold,
                "dust facing the earthlight would bloom; only the warm lights may");
        }

        /// <summary>
        /// Art's reference light scaled by how far World's bloom threshold sits above Art's: a surface Art caps just
        /// under its threshold stays just under World's in this much light.
        /// </summary>
        private static Color ReferenceLight()
        {
            return Palette.ReferenceLight * (new PostProcessSettings().BloomThreshold / Palette.BloomThreshold);
        }

        private Color Earthlight()
        {
            return _atmosphere.LightColor.linear * _atmosphere.LightIntensity;
        }

        private Color Fill()
        {
            return _atmosphere.FillColor.linear * _atmosphere.FillIntensity;
        }

        private static float Saturation(Color color)
        {
            Color.RGBToHSV(color, out float _, out float saturation, out float _);
            return saturation;
        }

        private float Fog(float distance)
        {
            float density = _atmosphere.FogDensity * distance;
            return 1f - Mathf.Exp(-density * density);
        }
    }
}
