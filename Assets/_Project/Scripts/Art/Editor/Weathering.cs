using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Decades of neglect laid over home and over 07 (VISION ruling 12), as three removable skins drawn with the
    /// vertex-coloured weather material (<see cref="ModelMaterial.PaletteWeather"/>), so restoration can clean a part
    /// by hiding its skin. They stack in this order, each a few millimetres proud of the last, and are cleaned from
    /// the top down: <c>Weather_Dust</c> (caked dust rising from the ground and lying on every face turned to the
    /// sky, drifts and fallen debris; it holds the paint and rust it settled on), <c>Weather_Rust</c> (runs from every
    /// panel's top edge with a bloom at the bolt, bare metal darkening to rust towards the ground, collars and
    /// streaks at joints and feet) and <c>Weather_Paint</c> (the paint broken into tired panels: chalky, grimy,
    /// yellowed, stained, mismatched replacements, stripe sections chipped to the metal). Permanent damage (dents,
    /// hanging panels, bent rails, loose cables) is part of the model itself. Big flat facets, never texture grime.
    /// </summary>
    public static class Weathering
    {
        public const string PaintName = "Weather_Paint";
        public const string RustName = "Weather_Rust";
        public const string DustName = "Weather_Dust";

        /// <summary>How far authored rust and dust stand off the surface they lie on (metres).</summary>
        public const float RustLift = 0.012f;

        public const float DustLift = 0.016f;

        // The rust and dust skins stand off at these multiples of a profile's paint lift: oxidised metal over the
        // paint, rust runs a hair over the oxidised metal they may cross, and the dust over each layer it settled on.
        private const float RustStep = 2f;
        private const float RunStep = 2.5f;
        private const float DustStep = 2.7f;

        // Oxidised rust keeps the bright rust's hue at this fraction of its value.
        private const float DarkRustValue = 0.62f;

        // The share of a painted panel's tone rolls that never reach the mismatched replacements.
        private const float ReplacedShare = 0.84f;

        /// <summary>The bright rust of runs and blooms (the Rust swatch as painted).</summary>
        public static Color32 Rust => Palette.GetSurface(PaletteSwatch.Rust);

        /// <summary>Old, deep rust (bolts, oxidised legs and feet): darker, still warm under the night light.</summary>
        public static Color32 DarkRust
        {
            get
            {
                Color32 rust = Rust;
                return new Color32((byte)(rust.r * DarkRustValue), (byte)(rust.g * DarkRustValue),
                    (byte)(rust.b * DarkRustValue), 0xFF);
            }
        }

        /// <summary>
        /// Weathers <paramref name="node"/>'s <paramref name="surface"/> as <paramref name="profile"/> says (patchwork
        /// paint, rust runs and oxidised metal, dust over all of it) plus the model's own authored rust (collars,
        /// window streaks, blooms) and dust (drifts, debris), and adds the three skins as its children.
        /// </summary>
        public static void Weather(ModelNode node, string meshPrefix, LowPolyMeshBuilder surface,
            WeatherProfile profile, LowPolyMeshBuilder rust, LowPolyMeshBuilder dust)
        {
            Weather(node, meshPrefix, surface, profile, null, rust, dust);
        }

        /// <summary>
        /// As the other overload, with the model's own authored wear laid over its patchwork paint (scuffs, chips).
        /// </summary>
        public static void Weather(ModelNode node, string meshPrefix, LowPolyMeshBuilder surface,
            WeatherProfile profile, LowPolyMeshBuilder paint, LowPolyMeshBuilder rust, LowPolyMeshBuilder dust)
        {
            Wear(node, Skins(meshPrefix, surface, profile, paint, rust, dust));
        }

        /// <summary>
        /// The three skins of <paramref name="surface"/> as <see cref="Weather"/> makes them, as meshes (those with
        /// geometry, in stacking order) to hang with <see cref="Wear"/> on every node showing the same part, such as
        /// the three bay arms' links.
        /// </summary>
        public static IReadOnlyList<ModelMesh> Skins(string meshPrefix, LowPolyMeshBuilder surface,
            WeatherProfile profile, LowPolyMeshBuilder paint, LowPolyMeshBuilder rust, LowPolyMeshBuilder dust)
        {
            List<SkinPlane> planes = WeatherSkins.Planes(surface);
            LowPolyMeshBuilder paintSkin = LowPolyMeshBuilder.WithVertexColours(surface.TriangleCount * 2);
            WeatherSkins.Patchwork(paintSkin, surface, planes, profile, profile.Lift);
            if (paint != null)
            {
                paintSkin.Append(paint, Matrix4x4.identity);
            }

            LowPolyMeshBuilder rustSkin = LowPolyMeshBuilder.WithVertexColours(surface.TriangleCount);
            WeatherSkins.Oxidise(rustSkin, paintSkin, profile, (RustStep - 1f) * profile.Lift);
            WeatherSkins.RustRuns(rustSkin, surface, planes, profile, RunStep * profile.Lift);
            if (rust != null)
            {
                rustSkin.Append(rust, Matrix4x4.identity);
            }

            LowPolyMeshBuilder dustSkin = LowPolyMeshBuilder.WithVertexColours(surface.TriangleCount);
            WeatherSkins.Dust(dustSkin, profile, DustStep * profile.Lift, paintSkin, rustSkin);
            if (dust != null)
            {
                dustSkin.Append(dust, Matrix4x4.identity);
            }

            var skins = new List<ModelMesh>(3);
            AddSkin(skins, meshPrefix, PaintName, paintSkin);
            AddSkin(skins, meshPrefix, RustName, rustSkin);
            AddSkin(skins, meshPrefix, DustName, dustSkin);
            return skins;
        }

        /// <summary>Hangs <paramref name="skins"/> (from <see cref="Skins"/>) on <paramref name="node"/>.</summary>
        public static void Wear(ModelNode node, IReadOnlyList<ModelMesh> skins)
        {
            foreach (ModelMesh skin in skins)
            {
                string layer = skin.Name.Substring(skin.Name.LastIndexOf("Weather_", System.StringComparison.Ordinal));
                node.Add(new ModelNode(layer, Vector3.zero, Quaternion.identity, skin, ModelMaterial.PaletteWeather));
            }
        }

        /// <summary>
        /// Adds the skins that have geometry to <paramref name="node"/>, their meshes named after it (plain builders
        /// become skins in their swatches' colours).
        /// </summary>
        public static void Attach(ModelNode node, string meshPrefix, LowPolyMeshBuilder paint, LowPolyMeshBuilder rust,
            LowPolyMeshBuilder dust)
        {
            Add(node, meshPrefix, PaintName, paint);
            Add(node, meshPrefix, RustName, rust);
            Add(node, meshPrefix, DustName, dust);
        }

        /// <summary>
        /// A whole part caked over (a wheel, a bogie): every face of <paramref name="surface"/> again,
        /// <paramref name="lift"/> proud and shaded <paramref name="amount"/> of the way towards
        /// <paramref name="toward"/>, as one skin mesh to hang (with <see cref="CoatNode"/>) on every node showing the
        /// part.
        /// </summary>
        public static ModelMesh Coat(string meshPrefix, string layer, LowPolyMeshBuilder surface, Color32 toward,
            float amount, float lift)
        {
            LowPolyMeshBuilder coat = LowPolyMeshBuilder.WithVertexColours(surface.TriangleCount);
            coat.Shade(coat.AppendWhere(surface, t => true, lift), toward, amount);
            return new ModelMesh(meshPrefix + "_" + layer, coat);
        }

        /// <summary>A node showing a coat from <see cref="Coat"/> as the <paramref name="layer"/> skin.</summary>
        public static ModelNode CoatNode(string layer, ModelMesh coat)
        {
            return new ModelNode(layer, Vector3.zero, Quaternion.identity, coat, ModelMaterial.PaletteWeather);
        }

        /// <summary>A rust collar round a joint or a foot (a short band round <paramref name="axis"/>'s Y).</summary>
        public static void Collar(LowPolyMeshBuilder b, Matrix4x4 axis, float radius, float height)
        {
            b.Prism(axis, radius, height, 8, PaletteSwatch.Rust, false);
        }

        /// <summary>
        /// The tone a panel painted <paramref name="swatch"/> weathered to, picked by <paramref name="roll"/> (0..1):
        /// shade it <paramref name="amount"/> of the way towards <paramref name="toward"/>'s colour. Unless
        /// <paramref name="mismatched"/>, no panel was ever replaced in another colour.
        /// </summary>
        public static void PanelTone(PaletteSwatch swatch, float roll, bool mismatched, out PaletteSwatch toward,
            out float amount)
        {
            switch (swatch)
            {
                case PaletteSwatch.Enamel:
                case PaletteSwatch.Cream:
                case PaletteSwatch.FadedPaint:
                    Pick(mismatched ? roll : roll * ReplacedShare, out toward, out amount,
                        (0.15f, PaletteSwatch.Cream, 0.6f), (0.38f, PaletteSwatch.FadedPaint, 1f),
                        (0.58f, PaletteSwatch.Charcoal, 0.55f), (0.7f, PaletteSwatch.Honey, 0.45f),
                        (0.84f, PaletteSwatch.Rust, 0.55f), (0.92f, PaletteSwatch.Sage, 0.85f),
                        (1f, PaletteSwatch.Metal, 0.85f));
                    return;
                case PaletteSwatch.WarmAccent:
                case PaletteSwatch.FadedAccent:
                    Pick(roll, out toward, out amount, (0.45f, PaletteSwatch.FadedAccent, 1f),
                        (0.65f, PaletteSwatch.FadedPaint, 0.65f), (0.88f, PaletteSwatch.Metal, 0.9f),
                        (1f, PaletteSwatch.Rust, 0.65f));
                    return;
                case PaletteSwatch.Honey:
                    Pick(roll, out toward, out amount, (0.3f, PaletteSwatch.Rust, 0.5f),
                        (0.55f, PaletteSwatch.Charcoal, 0.45f), (0.8f, PaletteSwatch.FadedPaint, 0.55f),
                        (1f, PaletteSwatch.Honey, 0.1f));
                    return;
                case PaletteSwatch.Metal:
                    Pick(roll, out toward, out amount, (0.35f, PaletteSwatch.Charcoal, 0.4f),
                        (0.65f, PaletteSwatch.Rust, 0.42f), (0.85f, PaletteSwatch.FadedPaint, 0.3f),
                        (1f, PaletteSwatch.Metal, 0f));
                    return;
                case PaletteSwatch.Rust:
                case PaletteSwatch.CakedDust:
                case PaletteSwatch.DustLight:
                case PaletteSwatch.DustMid:
                case PaletteSwatch.DustShadow:
                    toward = swatch;
                    amount = 0f;
                    return;
                default:
                    Pick(roll, out toward, out amount, (0.5f, PaletteSwatch.CakedDust, 0.25f),
                        (1f, PaletteSwatch.Charcoal, 0.15f));
                    return;
            }
        }

        private static void Pick(float roll, out PaletteSwatch toward, out float amount,
            params (float upTo, PaletteSwatch toward, float amount)[] tones)
        {
            foreach ((float upTo, PaletteSwatch target, float share) in tones)
            {
                if (roll < upTo)
                {
                    toward = target;
                    amount = share;
                    return;
                }
            }

            (_, toward, amount) = tones[tones.Length - 1];
        }

        private static void AddSkin(List<ModelMesh> skins, string meshPrefix, string layer, LowPolyMeshBuilder skin)
        {
            if (skin.TriangleCount > 0)
            {
                skins.Add(new ModelMesh(meshPrefix + "_" + layer, skin));
            }
        }

        private static void Add(ModelNode node, string meshPrefix, string layer, LowPolyMeshBuilder geometry)
        {
            if (geometry == null || geometry.TriangleCount == 0)
            {
                return;
            }

            LowPolyMeshBuilder skin = geometry;
            if (!geometry.HasVertexColours)
            {
                skin = LowPolyMeshBuilder.WithVertexColours(geometry.TriangleCount);
                skin.Append(geometry, Matrix4x4.identity);
            }

            node.Add(new ModelNode(layer, Vector3.zero, Quaternion.identity,
                new ModelMesh(meshPrefix + "_" + layer, skin), ModelMaterial.PaletteWeather));
        }
    }
}
