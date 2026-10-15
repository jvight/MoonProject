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

        /// <summary>
        /// The weather skins' material (LofiWeather): each vertex's own colour, lit exactly like M_LowPoly.
        /// </summary>
        public const string LowPolyWeatherMaterial = PaletteFolder + "/M_LowPolyWeather.mat";

        /// <summary>The warm light-decal disc's additive material (LightPoolBuilder, M3-18 V3).</summary>
        public const string LightPoolMaterial = PaletteFolder + "/M_LightPool.mat";

        /// <summary>07's lens glass ring (LofiLensGlass): it shows only Earth's reflection (cutscenes O10).</summary>
        public const string LensGlassMaterial = PaletteFolder + "/M_LensGlass.mat";
        public const string RoverFolder = Root + "/Rover";
        public const string RockFolder = Root + "/Rocks";
        public const string BaseFolder = Root + "/Base";
        public const string RelicFolder = Root + "/Relics";
        public const string FriendFolder = Root + "/Friends";
        public const string PickupFolder = Root + "/Pickups";
        public const string PropFolder = Root + "/Props";
        public const string RelayFolder = Root + "/Relay";
        public const string SitesFolder = Root + "/Sites";
        public const string LandmarksFolder = Root + "/Landmarks";

        /// <summary>
        /// The Lofi lit shader (M3-18 V2): URP's Blinn-Phong with no specular plus the palette emission map, a cool
        /// silhouette rim and the basin's air, SRP Batcher compatible.
        /// </summary>
        public const string LowPolyShader = "Assets/_Project/Shaders/Art/LofiLit.shader";

        /// <summary>The Art domain's vertex-coloured variant of the palette shader for the weather skins.</summary>
        public const string WeatherShader = "Assets/_Project/Shaders/Art/LofiWeather.shader";

        /// <summary>The additive pool of warm light on the ground (M3-18 V3).</summary>
        public const string LightPoolShader = "Assets/_Project/Shaders/Art/LofiLightPool.shader";

        /// <summary>The additive glass of 07's lens: Earth's reflection and a faint edge sheen.</summary>
        public const string LensGlassShader = "Assets/_Project/Shaders/Art/LofiLensGlass.shader";
        public const string ScanSolidShader = "Assets/_Project/Shaders/Art/LofiScanSolid.shader";
        public const string ScanLinesShader = "Assets/_Project/Shaders/Art/LofiScanLines.shader";
        public const string ScanSolidMaterial = PaletteFolder + "/M_ScanSolid.mat";
        public const string ScanLinesMaterial = PaletteFolder + "/M_ScanLines.mat";
        public const string FragmentFolder = Root + "/Fragments";

        public const string R1Root = Root + "/R1";
        public const string R1MaterialsFolder = R1Root + "/Materials";
        public const string R1ModelsFolder = R1Root + "/Models";
        public const string R1LitShader = "Assets/_Project/Shaders/R1/R1Lit.shader";
        public const string R1CrtFaceShader = "Assets/_Project/Shaders/R1/R1CrtFace.shader";
    }
}
