using System;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The look of one cassette id (docs/STORY.md "Cassettes"): its shell, the label colour that tells it apart on
    /// Bell's shelf and the ink of Ro's handwriting on the label's cream strip. Adding a tape is one more style.
    /// </summary>
    public sealed class CassetteStyle
    {
        public CassetteStyle(string id, PaletteSwatch shell, PaletteSwatch label, PaletteSwatch ink)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("A cassette needs its content id.", nameof(id));
            }

            if (Palette.IsEmissive(shell) || Palette.IsEmissive(label) || Palette.IsEmissive(ink))
            {
                throw new ArgumentException($"Cassette '{id}' must be painted with non-glowing swatches.", nameof(id));
            }

            Id = id;
            Shell = shell;
            Label = label;
            Ink = ink;
        }

        /// <summary>Content id (cassette.&lt;id&gt;.* text keys, saves, the prefab name).</summary>
        public string Id { get; }

        public PaletteSwatch Shell { get; }

        /// <summary>The tape's own colour: the label band on both faces and the stripe along its top edge.</summary>
        public PaletteSwatch Label { get; }

        public PaletteSwatch Ink { get; }
    }
}
