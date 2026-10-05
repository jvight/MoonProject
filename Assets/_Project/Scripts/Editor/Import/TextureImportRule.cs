namespace MoonProject.Editor.Import
{
    /// <summary>
    /// Import settings <see cref="ImportRules"/> forces onto a generated texture: point filtering, no mipmaps, no
    /// compression, no power-of-two rescaling (palette cells must stay exact); only the colour space varies.
    /// </summary>
    public readonly struct TextureImportRule
    {
        public TextureImportRule(bool srgb)
        {
            Srgb = srgb;
        }

        /// <summary>False for data textures (file name ending in <see cref="ImportRuleSet.LinearSuffix"/>).</summary>
        public bool Srgb { get; }
    }
}
