using UnityEngine;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// The six lost memories of the vertical slice (ids from the M2 content contract), each resting in the heart of a
    /// salvage site (docs/features/M3-13). Order is the catalog order. The depot's heart holds the tower's coil, not a
    /// memory (M3-16 P1), so the first memory is the bath duck in Kenji's garage, towed home at step 4; beside it
    /// lie the garden gnome he kept for luck and, toward the door, the walkman (clear of the duck's side, so 07 can
    /// dig the duck alone); the golden record lies in Kestrel-3's crater with a view
    /// of The Peak, where the last broadcast will one day be sent from; the boot is under the tipped drill rig and the
    /// teapot in the canyon lander, past the Hover-Jump gate. Answer notes climb the D major pentatonic ladder from
    /// D5 (0 = D5, 1 = E5, 2 = F#5, 3 = A5, 4 = B5, 5 = D6). Names and memory texts live in the localization tables
    /// (relic.&lt;id&gt;.name, relic.&lt;id&gt;.memory).
    /// </summary>
    internal static class RelicRecipes
    {
        public static readonly RelicRecipe[] All =
        {
            new RelicRecipe("rubber_duck", "kenji", 3f, 5, "garage"),
            new RelicRecipe("golden_record", "ro", 5f, 0, "kestrel"),
            new RelicRecipe("astronaut_boot", "ines", 9f, 1, "drill"),
            new RelicRecipe("teapot", "theo", 7f, 4, "lander"),
            new RelicRecipe("garden_gnome", "theo", 14f, 2, "garage"),
        };
    }
}
