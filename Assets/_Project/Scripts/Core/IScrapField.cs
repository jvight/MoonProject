using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// Kestrel-3's trail as a scrap field (0.5.5 §2): the cuttable hull fragments' slots down the clear lane of the
    /// furrow, planned by the World domain and registered in the GameContext in its Initialize (read it in your own
    /// Initialize, after World's), the 12-16 buried scenery fragments staying World's own. Its scene system also
    /// implements it in edit mode for editor previews. Gameplay stands Art's <c>HullFragment_&lt;Variant&gt;</c> on
    /// each slot as a piece to cut.
    /// </summary>
    public interface IScrapField
    {
        /// <summary>Cuttable slots in the lane (valid once the world is generated).</summary>
        int Count { get; }

        /// <summary>
        /// The field's dust tint over Art's fragments (a linear base-colour multiplier), one source for every fragment.
        /// </summary>
        Color Dust { get; }

        /// <summary>Slot <paramref name="index"/> (0 .. <see cref="Count"/> - 1).</summary>
        ScrapPlateInfo Slot(int index);
    }
}
