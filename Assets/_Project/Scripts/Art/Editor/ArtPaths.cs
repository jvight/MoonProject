namespace MoonProject.Art.Editor
{
    /// <summary>Output locations of the Art builders (docs/ARCHITECTURE.md, "Content pipeline").</summary>
    public static class ArtPaths
    {
        public const string Root = MoonProject.Editor.Builders.GeneratedAssets.Root + "/Art";
        public const string PaletteFolder = Root + "/Palette";
        public const string PaletteTexture = PaletteFolder + "/T_Palette.png";
        public const string PaletteEmissionTexture = PaletteFolder + "/T_PaletteEmission.png";
        public const string LowPolyMaterial = PaletteFolder + "/M_LowPoly.mat";
        public const string RoverFolder = Root + "/Rover";
        public const string RockFolder = Root + "/Rocks";
        public const string ScrapFolder = Root + "/Scrap";
        public const string BaseFolder = Root + "/Base";
        public const string RelicFolder = Root + "/Relics";

        /// <summary>
        /// URP Simple Lit: Lambert diffuse (no specular sheen on flat palette faces) plus an emission map, with every
        /// URP light, shadow, fog, Forward+, SRP Batcher and instancing path maintained by Unity.
        /// </summary>
        public const string LowPolyShader = "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLit.shader";
    }
}
