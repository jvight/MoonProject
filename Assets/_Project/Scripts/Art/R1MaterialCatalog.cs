using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Art
{
    /// <summary>
    /// Authoritative catalog of the 11 R1 stylized-realism material slots, their asset paths under
    /// <c>Assets/_Project/Art/R1/</c>, and their one-line intended uses.
    /// </summary>
    public static class R1MaterialCatalog
    {
        public const string ShaderName = "MoonProject/R1/Lit";
        public const string ShaderPath = "Assets/_Project/Shaders/R1/R1Lit.shader";

        public const string RootFolder = "Assets/_Project/Art/R1";
        public const string MaterialsFolder = "Assets/_Project/Art/R1/Materials";
        public const string TexturesFolder = "Assets/_Project/Art/R1/Textures";
        public const string ModelsFolder = "Assets/_Project/Art/R1/Models";

        public const string TrimSheetAlbedoPath = "Assets/_Project/Art/R1/Textures/T_R1_TrimSheet_Albedo.png";
        public const string TrimSheetNormalPath = "Assets/_Project/Art/R1/Textures/T_R1_TrimSheet_Normal.png";
        public const string TrimSheetMaskPath = "Assets/_Project/Art/R1/Textures/T_R1_TrimSheet_Mask.png";
        public const string TrimSheetEmissionPath = "Assets/_Project/Art/R1/Textures/T_R1_TrimSheet_Emission.png";

        public const string CreamEnamelSlot = "R1_CreamEnamel";
        public const string OrangeStripeSlot = "R1_OrangeStripe";
        public const string GreyMetalSlot = "R1_GreyMetal";
        public const string BareSteelSlot = "R1_BareSteel";
        public const string RubberSlot = "R1_Rubber";
        public const string GoldFoilSlot = "R1_GoldFoil";
        public const string DarkGlassSlot = "R1_DarkGlass";
        public const string CopperSlot = "R1_Copper";
        public const string SagePatchSlot = "R1_SagePatch";
        public const string LampAmberSlot = "R1_LampAmber";
        public const string ScreenSlot = "R1_Screen";
        public const string ScreenPlaceholderMaterialName = "R1_ScreenPlaceholder";

        public const float DefaultBloomSafeMax = 1.06f;
        public const float DefaultHotCoreThreshold = 0.85f;
        public const float DefaultHotCorePeak = 1.65f;
        public static readonly Color DefaultDustColor = new Color(0.42f, 0.44f, 0.50f, 1f);

        private static readonly R1MaterialSpec[] Specs =
        {
            new R1MaterialSpec(
                R1MaterialSlot.CreamEnamel,
                CreamEnamelSlot,
                CreamEnamelSlot,
                "T_R1_CreamEnamel",
                "Primary warm-cream baked enamel hull plating for rover shells, habitat modules, and companion bodies.",
                new Color(0.95f, 0.93f, 0.88f, 1f),
                0.95f,
                1.00f,
                0.15f,
                0.78f,
                new Color(0.76f, 0.78f, 0.82f, 1f),
                0.38f,
                0.68f,
                0.56f,
                0.24f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.OrangeStripe,
                OrangeStripeSlot,
                OrangeStripeSlot,
                "T_R1_OrangeStripe",
                "High-visibility worn international-orange accent stripes, grab handles, hatch rings, and safety bands.",
                new Color(0.96f, 0.46f, 0.16f, 1f),
                1.00f,
                1.00f,
                0.12f,
                0.66f,
                new Color(0.84f, 0.78f, 0.70f, 1f),
                0.48f,
                0.55f,
                0.50f,
                0.24f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.GreyMetal,
                GreyMetalSlot,
                GreyMetalSlot,
                "T_R1_GreyMetal",
                "Secondary cool-grey painted structural frames, chassis ribs, mounting brackets, and recessed bays.",
                new Color(0.48f, 0.51f, 0.56f, 1f),
                1.00f,
                1.00f,
                0.15f,
                0.64f,
                new Color(0.74f, 0.77f, 0.82f, 1f),
                0.42f,
                0.78f,
                0.62f,
                0.26f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.BareSteel,
                BareSteelSlot,
                BareSteelSlot,
                "T_R1_BareSteel",
                "Unpainted machined steel for actuator rods, latches, fasteners, wheel hubs, and exposed mechanisms.",
                new Color(0.76f, 0.79f, 0.83f, 1f),
                0.90f,
                1.00f,
                0.25f,
                0.78f,
                new Color(0.88f, 0.90f, 0.94f, 1f),
                0.32f,
                0.92f,
                0.76f,
                0.18f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.Rubber,
                RubberSlot,
                RubberSlot,
                "T_R1_Rubber",
                "Matte dark elastomer for dust bellows, articulated joint boots, pressure seals, tyres, and cable sheathing.",
                new Color(0.16f, 0.17f, 0.20f, 1f),
                1.10f,
                1.00f,
                0.06f,
                0.30f,
                new Color(0.28f, 0.30f, 0.34f, 1f),
                0.20f,
                0.05f,
                0.22f,
                0.34f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.GoldFoil,
                GoldFoilSlot,
                GoldFoilSlot,
                "T_R1_GoldFoil",
                "Crinkled Kapton multi-layer thermal insulation blankets tucked into instrument cavities and lander undersides.",
                new Color(0.95f, 0.72f, 0.24f, 1f),
                1.25f,
                1.00f,
                0.35f,
                0.86f,
                new Color(0.98f, 0.86f, 0.55f, 1f),
                0.28f,
                0.95f,
                0.82f,
                0.16f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.DarkGlass,
                DarkGlassSlot,
                DarkGlassSlot,
                "T_R1_DarkGlass",
                "Glossy dark-tinted optical glass for camera domes, sensor windows, and unlit instrument bezels.",
                new Color(0.08f, 0.11f, 0.15f, 1f),
                0.45f,
                1.00f,
                0.65f,
                0.95f,
                new Color(0.32f, 0.36f, 0.42f, 1f),
                0.14f,
                0.15f,
                0.75f,
                0.10f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.Copper,
                CopperSlot,
                CopperSlot,
                "T_R1_Copper",
                "Warm metallic copper for antenna coils, heat-pipe fittings, grounding straps, and relay terminals.",
                new Color(0.88f, 0.50f, 0.30f, 1f),
                0.90f,
                1.00f,
                0.25f,
                0.80f,
                new Color(0.96f, 0.68f, 0.48f, 1f),
                0.34f,
                0.94f,
                0.78f,
                0.18f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.SagePatch,
                SagePatchSlot,
                SagePatchSlot,
                "T_R1_SagePatch",
                "Muted sage-green field-repair patches, stowage covers, and secondary maintenance access panels.",
                new Color(0.54f, 0.65f, 0.56f, 1f),
                0.95f,
                1.00f,
                0.12f,
                0.56f,
                new Color(0.75f, 0.78f, 0.76f, 1f),
                0.36f,
                0.55f,
                0.48f,
                0.26f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.LampAmber,
                LampAmberSlot,
                LampAmberSlot,
                "T_R1_LampAmber",
                "Bloom-safe warm amber indicator lamps, porch floodlight lenses, and beacon emitters with a small HDR core.",
                new Color(0.98f, 0.74f, 0.32f, 1f),
                0.85f,
                1.00f,
                0.40f,
                0.88f,
                new Color(0.82f, 0.76f, 0.68f, 1f),
                0.15f,
                0.30f,
                0.65f,
                0.08f,
                true,
                new Color(1.00f, 0.76f, 0.32f, 1f),
                1.00f),
            new R1MaterialSpec(
                R1MaterialSlot.Screen,
                ScreenSlot,
                ScreenPlaceholderMaterialName,
                "T_R1_DarkGlass",
                "Plain dark-glass placeholder assigned to the R1_Screen slot and reserved for the CRT face shader slice.",
                new Color(0.06f, 0.09f, 0.12f, 1f),
                0.35f,
                1.00f,
                0.72f,
                0.96f,
                new Color(0.26f, 0.30f, 0.36f, 1f),
                0.10f,
                0.12f,
                0.80f,
                0.06f,
                false,
                Color.black,
                0f)
        };

        /// <summary>All 11 fixed R1 material slot specifications in slot enum order.</summary>
        public static IReadOnlyList<R1MaterialSpec> All => Specs;

        /// <summary>Returns the specification for a given <see cref="R1MaterialSlot"/>.</summary>
        public static R1MaterialSpec Get(R1MaterialSlot slot)
        {
            int index = (int)slot;
            if (index < 0 || index >= Specs.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), slot, "Unknown R1MaterialSlot.");
            }

            return Specs[index];
        }

        /// <summary>Looks up an R1 material specification by its exact Blender material slot name.</summary>
        public static bool TryGetBySlotName(string slotName, out R1MaterialSpec spec)
        {
            if (!string.IsNullOrEmpty(slotName))
            {
                for (int i = 0; i < Specs.Length; i++)
                {
                    if (string.Equals(Specs[i].SlotName, slotName, StringComparison.Ordinal))
                    {
                        spec = Specs[i];
                        return true;
                    }
                }
            }

            spec = default;
            return false;
        }

        /// <summary>Project-relative path to the Unity <c>.mat</c> asset for <paramref name="spec"/>.</summary>
        public static string GetMaterialPath(R1MaterialSpec spec)
        {
            return MaterialsFolder + "/" + spec.MaterialAssetName + ".mat";
        }

        /// <summary>Project-relative path to the albedo texture for <paramref name="spec"/>.</summary>
        public static string GetAlbedoTexturePath(R1MaterialSpec spec)
        {
            return TexturesFolder + "/" + spec.TexturePrefix + "_Albedo.png";
        }

        /// <summary>Project-relative path to the tangent-space normal map for <paramref name="spec"/>.</summary>
        public static string GetNormalTexturePath(R1MaterialSpec spec)
        {
            return TexturesFolder + "/" + spec.TexturePrefix + "_Normal.png";
        }

        /// <summary>Project-relative path to the packed mask map (R=Metal, G=AO, B=Curv, A=Smooth) for <paramref name="spec"/>.</summary>
        public static string GetMaskTexturePath(R1MaterialSpec spec)
        {
            return TexturesFolder + "/" + spec.TexturePrefix + "_Mask.png";
        }

        /// <summary>Project-relative path to the emission map for <paramref name="spec"/>, or null if non-emissive.</summary>
        public static string GetEmissionTexturePath(R1MaterialSpec spec)
        {
            return spec.HasEmission ? TexturesFolder + "/" + spec.TexturePrefix + "_Emission.png" : null;
        }
    }
}
