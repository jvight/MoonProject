using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Authoritative Editor-side recipe table for the 13 R1 stylized-realism material slots, their source texture paths
    /// under <c>Assets/_Project/Art/R1/Textures/</c>, their generated <c>.mat</c> output paths under
    /// <see cref="ArtPaths.R1MaterialsFolder"/>, and the pure slot-mapping functions used by the postprocessor and
    /// model builder.
    /// </summary>
    public static class R1MaterialRecipes
    {
        public const string ShaderName = "MoonProject/R1/Lit";
        public const string ShaderPath = ArtPaths.R1LitShader;
        public const string CrtFaceShaderName = "MoonProject/R1/CrtFace";
        public const string CrtFaceShaderPath = ArtPaths.R1CrtFaceShader;

        public const string RootFolder = "Assets/_Project/Art/R1";
        public const string TexturesFolder = RootFolder + "/Textures";
        public const string ModelsFolder = RootFolder + "/Models";
        public const string GeneratedMaterialsFolder = ArtPaths.R1MaterialsFolder;
        public const string GeneratedModelsFolder = ArtPaths.R1ModelsFolder;

        public const string TrimSheetAlbedoPath = TexturesFolder + "/T_R1_TrimSheet_Albedo.png";
        public const string TrimSheetNormalPath = TexturesFolder + "/T_R1_TrimSheet_Normal.png";
        public const string TrimSheetMaskPath = TexturesFolder + "/T_R1_TrimSheet_Mask.png";
        public const string TrimSheetEmissionPath = TexturesFolder + "/T_R1_TrimSheet_Emission.png";

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
        public const string SolarCellSlot = "R1_SolarCell";
        public const string ScreenSlot = "R1_Screen";
        public const string BrassSlot = "R1_Brass";
        public const string CrtFaceMaterialName = "R1_CrtFace";
        public const string BasaltSlot = "R1_Basalt";
        public const string SignageSlot = "R1_Signage";
        public const string WhitePaintSlot = "R1_WhitePaint";
        public const string TealMarkSlot = "R1_TealMark";
        public const string SilverFoilSlot = "R1_SilverFoil";
        public const string RackOutlineSlot = "R1_RackOutline";
        public const string RackFillSlot = "R1_RackFill";

        public const float DefaultBloomSafeMax = 1.06f;
        public const float DefaultHotCoreThreshold = 0.85f;
        public const float DefaultHotCorePeak = 1.65f;
        public const float DefaultEdgeWearThreshold = 0.22f;
        public const float DefaultEdgeWearSharpness = 2.0f;
        public static readonly Color DefaultDustColor = new Color(0.46f, 0.48f, 0.54f, 1f);

        private static readonly R1MaterialSpec[] Specs =
        {
            new R1MaterialSpec(
                R1MaterialSlot.CreamEnamel,
                CreamEnamelSlot,
                CreamEnamelSlot,
                "T_R1_CreamEnamel",
                "Primary warm-cream baked enamel hull plating for rover shells, habitat modules, and companion bodies.",
                new Color(0.92f, 0.89f, 0.82f, 1f),
                0.70f,
                1.00f,
                0.15f,
                0.76f,
                new Color(0.68f, 0.71f, 0.76f, 1f),
                0.65f,
                0.72f,
                0.58f,
                0.22f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.OrangeStripe,
                OrangeStripeSlot,
                OrangeStripeSlot,
                "T_R1_OrangeStripe",
                "Warm, slightly dusty orange accent stripes, grab handles, hatch rings, and safety bands.",
                new Color(0.85f, 0.55f, 0.35f, 1f),
                0.75f,
                1.00f,
                0.14f,
                0.64f,
                new Color(0.86f, 0.81f, 0.74f, 1f),
                0.68f,
                0.55f,
                0.52f,
                0.22f,
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
                0.80f,
                1.00f,
                0.16f,
                0.64f,
                new Color(0.76f, 0.79f, 0.84f, 1f),
                0.64f,
                0.82f,
                0.64f,
                0.24f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.BareSteel,
                BareSteelSlot,
                BareSteelSlot,
                "T_R1_BareSteel",
                "Unpainted machined steel for actuator rods, latches, fasteners, wheel hubs, and exposed mechanisms.",
                new Color(0.74f, 0.77f, 0.81f, 1f),
                0.75f,
                1.00f,
                0.28f,
                0.78f,
                new Color(0.90f, 0.92f, 0.96f, 1f),
                0.55f,
                0.94f,
                0.78f,
                0.16f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.Rubber,
                RubberSlot,
                RubberSlot,
                "T_R1_Rubber",
                "Matte dark elastomer for dust bellows, articulated joint boots, pressure seals, tyres, and cable sheathing.",
                new Color(0.17f, 0.18f, 0.21f, 1f),
                0.85f,
                1.00f,
                0.06f,
                0.28f,
                new Color(0.32f, 0.34f, 0.38f, 1f),
                0.42f,
                0.05f,
                0.22f,
                0.30f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.GoldFoil,
                GoldFoilSlot,
                GoldFoilSlot,
                "T_R1_GoldFoil",
                "Broad-crinkled golden Kapton multi-layer thermal insulation blankets tucked into instrument cavities and lander undersides.",
                new Color(0.94f, 0.79f, 0.36f, 1f),
                0.80f,
                1.00f,
                0.35f,
                0.50f,
                new Color(1.00f, 0.91f, 0.62f, 1f),
                0.25f,
                0.98f,
                0.55f,
                0.08f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.DarkGlass,
                DarkGlassSlot,
                DarkGlassSlot,
                "T_R1_DarkGlass",
                "Glossy dark-tinted optical glass for camera domes, sensor windows, and unlit instrument bezels.",
                new Color(0.09f, 0.12f, 0.16f, 1f),
                0.35f,
                1.00f,
                0.68f,
                0.95f,
                new Color(0.34f, 0.38f, 0.44f, 1f),
                0.32f,
                0.15f,
                0.76f,
                0.10f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.Copper,
                CopperSlot,
                CopperSlot,
                "T_R1_Copper",
                "Brownish-pink metallic copper for antenna coils, heat-pipe fittings, grounding straps, and relay terminals.",
                new Color(0.76f, 0.53f, 0.49f, 1f),
                0.75f,
                1.00f,
                0.28f,
                0.80f,
                new Color(0.89f, 0.70f, 0.65f, 1f),
                0.56f,
                0.95f,
                0.80f,
                0.16f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.SagePatch,
                SagePatchSlot,
                SagePatchSlot,
                "T_R1_SagePatch",
                "Muted sage-green field-repair patches, stowage covers, and secondary maintenance access panels.",
                new Color(0.54f, 0.64f, 0.55f, 1f),
                0.75f,
                1.00f,
                0.14f,
                0.56f,
                new Color(0.76f, 0.79f, 0.77f, 1f),
                0.60f,
                0.58f,
                0.50f,
                0.24f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.LampAmber,
                LampAmberSlot,
                LampAmberSlot,
                "T_R1_LampAmber",
                "Smooth glowing warm amber indicator lamps, porch floodlight lenses, and beacon emitters with a small hot core.",
                new Color(0.98f, 0.74f, 0.32f, 1f),
                0.50f,
                1.00f,
                0.48f,
                0.90f,
                new Color(0.86f, 0.78f, 0.68f, 1f),
                0.25f,
                0.25f,
                0.68f,
                0.06f,
                true,
                new Color(1.00f, 0.76f, 0.32f, 1f),
                1.00f),
            new R1MaterialSpec(
                R1MaterialSlot.SolarCell,
                SolarCellSlot,
                SolarCellSlot,
                "T_R1_SolarCell",
                "Deep blue-violet photovoltaic glass cells with fine silver busbars and collector grid for solar arrays and Tilly's solar wing.",
                new Color(0.20f, 0.22f, 0.44f, 1f),
                0.55f,
                1.00f,
                0.55f,
                0.94f,
                new Color(0.78f, 0.81f, 0.86f, 1f),
                0.38f,
                0.85f,
                0.82f,
                0.12f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.Screen,
                ScreenSlot,
                CrtFaceMaterialName,
                "T_R1_DarkGlass",
                "Expressive warm-amber CRT face material driven by FaceDisplay and MoonProject/R1/CrtFace.",
                new Color(0.125f, 0.078f, 0.039f, 1f),
                0.30f,
                1.00f,
                0.72f,
                0.96f,
                new Color(0.28f, 0.32f, 0.38f, 1f),
                0.22f,
                0.12f,
                0.80f,
                0.06f,
                false,
                new Color(1.00f, 0.68f, 0.22f, 1f),
                1.00f),
            new R1MaterialSpec(
                R1MaterialSlot.Brass,
                BrassSlot,
                BrassSlot,
                "T_R1_Brass",
                "Warm golden machined brass for lamp bezels, chest lens ring, cheek lens, and fittings.",
                new Color(0.86f, 0.69f, 0.32f, 1f),
                0.65f,
                1.00f,
                0.34f,
                0.82f,
                new Color(0.96f, 0.84f, 0.50f, 1f),
                0.52f,
                0.96f,
                0.84f,
                0.14f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.Basalt,
                BasaltSlot,
                BasaltSlot,
                "T_R1_Basalt",
                "Weathered lunar basalt and breccia stone for boulders, rock lumps, pebbles, and canyon gate monoliths.",
                new Color(0.36f, 0.37f, 0.40f, 1f),
                1.35f,
                0.04f,
                0.04f,
                0.18f,
                new Color(0.47f, 0.48f, 0.51f, 1f),
                0.22f,
                0.02f,
                0.14f,
                0.82f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.Signage,
                SignageSlot,
                SignageSlot,
                "T_R1_Signage",
                "1980s stencil signage atlas (LUMEN, SEC-04, HABITAT & LAB, WORKSHOP, EVAC 1988, programme logo mark, hazard stripes, 4 pictograms, and emissive amber AIRLOCK_STATUS readout SEALED · 1988 · STANDBY / SUIT 01-04 · OFFLINE).",
                new Color(0.92f, 0.89f, 0.82f, 1f),
                0.65f,
                1.00f,
                0.15f,
                0.68f,
                new Color(0.70f, 0.72f, 0.76f, 1f),
                0.55f,
                0.65f,
                0.52f,
                0.24f,
                true,
                new Color(1.00f, 0.74f, 0.30f, 1f),
                1.00f),
            new R1MaterialSpec(
                R1MaterialSlot.WhitePaint,
                WhitePaintSlot,
                WhitePaintSlot,
                "T_R1_WhitePaint",
                "Chalky cool-white low-gloss thermal control paint for 1960s-70s landers, experiment boxes, flat white Kapton ribbon, utility trusses, and Kestrel-3.",
                new Color(0.91f, 0.92f, 0.90f, 1f),
                0.65f,
                1.00f,
                0.15f,
                0.22f,
                new Color(0.76f, 0.78f, 0.80f, 1f),
                0.58f,
                0.55f,
                0.34f,
                0.24f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.TealMark,
                TealMarkSlot,
                TealMarkSlot,
                "T_R1_TealMark",
                "Muted teal painted markings and trim for Meridian and Kestrel-3 hardware.",
                new Color(0.18f, 0.43f, 0.45f, 1f),
                0.70f,
                1.00f,
                0.22f,
                0.48f,
                new Color(0.66f, 0.74f, 0.76f, 1f),
                0.64f,
                0.58f,
                0.42f,
                0.22f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.SilverFoil,
                SilverFoilSlot,
                SilverFoilSlot,
                "T_R1_SilverFoil",
                "Crinkled aluminised Mylar/Teflon thermal blanket and reflector film for 1960s-70s probes and sortie packages.",
                new Color(0.79f, 0.81f, 0.82f, 1f),
                1.35f,
                1.00f,
                0.38f,
                0.72f,
                new Color(0.92f, 0.94f, 0.96f, 1f),
                0.46f,
                0.92f,
                0.68f,
                0.10f,
                false,
                Color.black,
                0f),
            new R1MaterialSpec(
                R1MaterialSlot.RackOutline,
                RackOutlineSlot,
                RackOutlineSlot,
                "T_R1_RackOutline",
                "The socket rack's lit silhouette line: an even warm glow Gameplay sets per bay through _EmissionColor.",
                new Color(0.23f, 0.18f, 0.13f, 1f),
                0.30f,
                1.00f,
                0.36f,
                0.44f,
                new Color(0.23f, 0.18f, 0.13f, 1f),
                0.00f,
                0.00f,
                0.40f,
                0.00f,
                true,
                new Color(1.00f, 0.78f, 0.48f, 1f),
                1.00f),
            new R1MaterialSpec(
                R1MaterialSlot.RackFill,
                RackFillSlot,
                RackFillSlot,
                "T_R1_RackFill",
                "The socket rack's filled silhouette behind the line: a softer warm glow Gameplay sets per bay through _EmissionColor.",
                new Color(0.18f, 0.15f, 0.12f, 1f),
                0.30f,
                1.00f,
                0.30f,
                0.38f,
                new Color(0.18f, 0.15f, 0.12f, 1f),
                0.00f,
                0.00f,
                0.34f,
                0.00f,
                true,
                new Color(1.00f, 0.70f, 0.40f, 1f),
                1.00f)
        };

        /// <summary>All 20 fixed R1 material slot specifications in slot enum order.</summary>
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

        /// <summary>
        /// Pure helper that trims whitespace and strips Unity's <c>" (Instance)"</c> suffix from a raw material name.
        /// </summary>
        public static string NormalizeSlotName(string rawSlotName)
        {
            if (string.IsNullOrEmpty(rawSlotName))
            {
                return string.Empty;
            }

            const string InstanceSuffix = " (Instance)";
            string trimmed = rawSlotName.Trim();
            if (trimmed.EndsWith(InstanceSuffix, StringComparison.Ordinal))
            {
                trimmed = trimmed.Substring(0, trimmed.Length - InstanceSuffix.Length).Trim();
            }

            return trimmed;
        }

        /// <summary>
        /// Pure slot-mapping function: resolves a raw Blender/Unity material name to its <see cref="R1MaterialSlot"/>.
        /// </summary>
        public static bool TryMapSlotName(string rawSlotName, out R1MaterialSlot slot)
        {
            if (TryGetBySlotName(rawSlotName, out R1MaterialSpec spec))
            {
                slot = spec.Slot;
                return true;
            }

            slot = default;
            return false;
        }

        /// <summary>Looks up an R1 material specification by its Blender material slot name.</summary>
        public static bool TryGetBySlotName(string rawSlotName, out R1MaterialSpec spec)
        {
            string clean = NormalizeSlotName(rawSlotName);
            if (!string.IsNullOrEmpty(clean))
            {
                for (int i = 0; i < Specs.Length; i++)
                {
                    if (string.Equals(Specs[i].SlotName, clean, StringComparison.Ordinal))
                    {
                        spec = Specs[i];
                        return true;
                    }
                }
            }

            spec = default;
            return false;
        }

        /// <summary>Project-relative path to the generated Unity <c>.mat</c> asset for <paramref name="spec"/>.</summary>
        public static string GetMaterialPath(R1MaterialSpec spec)
        {
            return GeneratedMaterialsFolder + "/" + spec.MaterialAssetName + ".mat";
        }

        /// <summary>Project-relative path to the source albedo texture for <paramref name="spec"/>.</summary>
        public static string GetAlbedoTexturePath(R1MaterialSpec spec)
        {
            return TexturesFolder + "/" + spec.TexturePrefix + "_Albedo.png";
        }

        /// <summary>Project-relative path to the source tangent-space normal map for <paramref name="spec"/>.</summary>
        public static string GetNormalTexturePath(R1MaterialSpec spec)
        {
            return TexturesFolder + "/" + spec.TexturePrefix + "_Normal.png";
        }

        /// <summary>Project-relative path to the source packed mask map for <paramref name="spec"/>.</summary>
        public static string GetMaskTexturePath(R1MaterialSpec spec)
        {
            return TexturesFolder + "/" + spec.TexturePrefix + "_Mask.png";
        }

        /// <summary>Project-relative path to the source emission map for <paramref name="spec"/>, or null if non-emissive.</summary>
        public static string GetEmissionTexturePath(R1MaterialSpec spec)
        {
            return spec.HasEmission ? TexturesFolder + "/" + spec.TexturePrefix + "_Emission.png" : null;
        }
    }
}
