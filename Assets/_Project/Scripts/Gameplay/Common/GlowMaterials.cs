using System;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Art;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The recipe of each SoftGlow material (colour from the palette, which fades and bands it uses, how it blends).
    /// The Gameplay/Materials builder writes these as assets; tests build the same materials in memory.
    /// </summary>
    public static class GlowMaterials
    {
        public const string ShaderName = "MoonProject/Gameplay/SoftGlow";

        private const int TransparentQueue = 3000;

        /// <summary>Radial falloff of round sprites: (1 - r^2)^2, a soft ball (dust, sparks).</summary>
        private const float SoftRadialPower = 2f;

        /// <summary>Radial falloff of the home halo: a bright core with a long faint tail and no visible rim.</summary>
        private const float HaloRadialPower = 5f;

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        private static readonly int AcrossInId = Shader.PropertyToID("_AcrossIn");
        private static readonly int AcrossOutId = Shader.PropertyToID("_AcrossOut");
        private static readonly int LengthFadeId = Shader.PropertyToID("_LengthFade");
        private static readonly int BandCountId = Shader.PropertyToID("_BandCount");
        private static readonly int BandSpeedId = Shader.PropertyToID("_BandSpeed");
        private static readonly int BandStrengthId = Shader.PropertyToID("_BandStrength");
        private static readonly int FresnelMixId = Shader.PropertyToID("_FresnelMix");
        private static readonly int FresnelPowerId = Shader.PropertyToID("_FresnelPower");
        private static readonly int CoreMixId = Shader.PropertyToID("_CoreMix");
        private static readonly int CorePowerId = Shader.PropertyToID("_CorePower");
        private static readonly int RadialMaskId = Shader.PropertyToID("_RadialMask");
        private static readonly int RadialPowerId = Shader.PropertyToID("_RadialPower");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int CullId = Shader.PropertyToID("_Cull");

        /// <summary>A new material for <paramref name="role"/> on the SoftGlow <paramref name="shader"/>.</summary>
        public static Material Create(Shader shader, GlowRole role)
        {
            if (shader == null)
            {
                throw new ArgumentNullException(nameof(shader));
            }

            var material = new Material(shader) { name = "M_Glow" + role };
            Configure(material, role);
            return material;
        }

        public static void Configure(Material material, GlowRole role)
        {
            if (material == null)
            {
                throw new ArgumentNullException(nameof(material));
            }

            Color tech = Palette.Get(PaletteSwatch.TechGlow);
            Color warm = Palette.Get(PaletteSwatch.WarmLamp);
            Color dust = Palette.Get(PaletteSwatch.DustLight);
            Set(material, tech, edge: 0f, length: 0f, bands: 0f, speed: 0f, strength: 0f, fresnel: 0f,
                fresnelPower: 2f, core: 0f, corePower: 1.5f, radial: 0f, additive: true, cull: CullMode.Off);
            switch (role)
            {
                case GlowRole.SonarRing:
                    // A calm wave: a soft leading edge (outside) and a long, fading tail behind it (inside).
                    material.SetFloat(AcrossInId, 0.8f);
                    material.SetFloat(AcrossOutId, 0.2f);
                    break;
                case GlowRole.SiteRing:
                    material.SetFloat(AcrossInId, 0.5f);
                    material.SetFloat(AcrossOutId, 0.5f);
                    break;
                case GlowRole.SitePillar:
                    Set(material, tech, edge: 0f, length: 0.4f, bands: 3f, speed: 0.25f, strength: 0.35f,
                        fresnel: 0f, fresnelPower: 2f, core: 1f, corePower: 1.2f, radial: 0f, additive: true,
                        cull: CullMode.Off);
                    break;
                case GlowRole.FriendPillar:
                    Set(material, warm, edge: 0f, length: 0.4f, bands: 3f, speed: 0.18f, strength: 0.35f,
                        fresnel: 0f, fresnelPower: 2f, core: 1f, corePower: 1.2f, radial: 0f, additive: true,
                        cull: CullMode.Off);
                    break;
                case GlowRole.TractorBeam:
                    Set(material, tech, edge: 0f, length: 0.18f, bands: 5f, speed: 0.8f, strength: 0.45f,
                        fresnel: 0f, fresnelPower: 2f, core: 0.8f, corePower: 1f, radial: 0f, additive: true,
                        cull: CullMode.Off);
                    break;
                case GlowRole.TetherBeam:
                    Set(material, tech, edge: 0.5f, length: 0.04f, bands: 6f, speed: 1.6f, strength: 0.4f,
                        fresnel: 0f, fresnelPower: 2f, core: 0f, corePower: 1.5f, radial: 0f, additive: true,
                        cull: CullMode.Off);
                    break;
                case GlowRole.Flash:
                    material.SetFloat(FresnelMixId, 0.5f);
                    material.SetFloat(FresnelPowerId, 1.5f);
                    break;
                case GlowRole.RelicHalo:
                    Set(material, tech, edge: 0f, length: 0f, bands: 0f, speed: 0f, strength: 0f, fresnel: 1f,
                        fresnelPower: 1.5f, core: 0f, corePower: 1.5f, radial: 0f, additive: true,
                        cull: CullMode.Front);
                    break;
                case GlowRole.Dust:
                    dust.a = 0.4f;
                    Set(material, dust, edge: 0f, length: 0f, bands: 0f, speed: 0f, strength: 0f, fresnel: 0f,
                        fresnelPower: 2f, core: 0f, corePower: 1.5f, radial: 1f, additive: false,
                        cull: CullMode.Off);
                    break;
                case GlowRole.WarmRing:
                    material.SetColor(ColorId, warm);
                    material.SetFloat(AcrossInId, 0.5f);
                    material.SetFloat(AcrossOutId, 0.5f);
                    break;
                case GlowRole.Spark:
                    Set(material, warm, edge: 0f, length: 0f, bands: 0f, speed: 0f, strength: 0f, fresnel: 0f,
                        fresnelPower: 2f, core: 0f, corePower: 1.5f, radial: 1f, additive: true,
                        cull: CullMode.Off);
                    break;
                case GlowRole.HomeHalo:
                    // The lamps' own amber (the palette's warm glow, which keeps less green and blue so it stays
                    // amber once graded), passed as gamma so the shader receives it linear; a bright core with a
                    // long faint tail, so from afar it reads as glow around the windows, not as a disc.
                    Set(material, (Palette.GetGlow(PaletteSwatch.WarmLamp) / Palette.WarmLampGlow).gamma, edge: 0f,
                        length: 0f, bands: 0f, speed: 0f, strength: 0f, fresnel: 0f, fresnelPower: 2f, core: 0f,
                        corePower: 1.5f, radial: 1f, additive: true, cull: CullMode.Off);
                    material.SetFloat(RadialPowerId, HaloRadialPower);
                    break;
                case GlowRole.WarmGlow:
                    material.SetColor(ColorId, warm);
                    material.SetFloat(FresnelMixId, 0.4f);
                    material.SetFloat(FresnelPowerId, 1.2f);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown glow role.");
            }
        }

        private static void Set(Material material, Color color, float edge, float length, float bands, float speed,
            float strength, float fresnel, float fresnelPower, float core, float corePower, float radial,
            bool additive, CullMode cull)
        {
            material.SetColor(ColorId, color);
            material.SetFloat(IntensityId, 1f);
            material.SetFloat(AcrossInId, edge);
            material.SetFloat(AcrossOutId, edge);
            material.SetFloat(LengthFadeId, length);
            material.SetFloat(BandCountId, bands);
            material.SetFloat(BandSpeedId, speed);
            material.SetFloat(BandStrengthId, strength);
            material.SetFloat(FresnelMixId, fresnel);
            material.SetFloat(FresnelPowerId, fresnelPower);
            material.SetFloat(CoreMixId, core);
            material.SetFloat(CorePowerId, corePower);
            material.SetFloat(RadialMaskId, radial);
            material.SetFloat(RadialPowerId, SoftRadialPower);
            material.SetFloat(SrcBlendId, (float)BlendMode.One);
            material.SetFloat(DstBlendId, (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            material.SetFloat(CullId, (float)cull);
            material.renderQueue = TransparentQueue;
        }
    }
}
