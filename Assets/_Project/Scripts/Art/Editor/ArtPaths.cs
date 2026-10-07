namespace MoonProject.Art.Editor
{
    /// <summary>Output locations of the Art builders (docs/ARCHITECTURE.md, "Content pipeline").</summary>
    public static class ArtPaths
    {
        public const string Root = MoonProject.Editor.Builders.GeneratedAssets.Root + "/Art";
        public const string PaletteFolder = Root + "/Palette";
        public const string PaletteTexture = PaletteFolder + "/T_Palette.png";
        /// <summary>
        /// The glow of every swatch in linear HDR (half-float EXR, so the living lights can glow above the bloom
        /// threshold at intensity 1); the _Linear suffix keeps the import rules from treating it as sRGB.
        /// </summary>
        public const string PaletteEmissionTexture = PaletteFolder + "/T_PaletteEmission_Linear.exr";
        public const string LowPolyMaterial = PaletteFolder + "/M_LowPoly.mat";

        /// <summary>
        /// M_LowPoly with its glow authored off (_EmissionColor black, emission still enabled): for glow renderers
        /// that start dark and are lit at runtime through a MaterialPropertyBlock (part lamps, a dormant eye).
        /// </summary>
        public const string LowPolyGlowOffMaterial = PaletteFolder + "/M_LowPolyGlowOff.mat";
        public const string RoverFolder = Root + "/Rover";
        public const string RockFolder = Root + "/Rocks";
        public const string ScrapFolder = Root + "/Scrap";
        public const string BaseFolder = Root + "/Base";
        public const string RelicFolder = Root + "/Relics";
        public const string FriendFolder = Root + "/Friends";
        public const string PickupFolder = Root + "/Pickups";
        public const string PropFolder = Root + "/Props";

        /// <summary>
        /// URP Simple Lit: Lambert diffuse (no specular sheen on flat palette faces) plus an emission map, with every
        /// URP light, shadow, fog, Forward+, SRP Batcher and instancing path maintained by Unity.
        /// </summary>
        public const string LowPolyShader = "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLit.shader";
    }
}
