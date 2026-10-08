using UnityEngine;

namespace MoonProject.Gameplay.Editor
{
    /// <summary>
    /// The six lost memories of the vertical slice (ids from the M2 content contract), each resting in the heart of a
    /// salvage site (docs/features/M3-13). Order is the catalog order. The walkman waits in the supply depot near
    /// home so the first site to salvage also holds the first memory; Kenji's garage holds his bath duck and, beside
    /// it, the garden gnome he kept for luck; the golden record lies in Kestrel-3's crater with a view of The Peak,
    /// where the last broadcast will one day be sent from; the boot is under the tipped drill rig and the teapot in
    /// the canyon lander, past the Hover-Jump gate. Answer notes climb the D major pentatonic ladder from D5 (0 = D5,
    /// 1 = E5, 2 = F#5, 3 = A5, 4 = B5, 5 = D6). Names and memory texts live in the localization tables
    /// (relic.&lt;id&gt;.name, relic.&lt;id&gt;.memory).
    /// </summary>
    internal static class RelicRecipes
    {
        public static readonly RelicRecipe[] All =
        {
            new RelicRecipe("cassette_player", 6f, 3, "depot", Vector2.zero),
            new RelicRecipe("rubber_duck", 3f, 5, "garage", Vector2.zero),
            new RelicRecipe("golden_record", 5f, 0, "kestrel", Vector2.zero),
            new RelicRecipe("astronaut_boot", 9f, 1, "drill", Vector2.zero),
            new RelicRecipe("teapot", 7f, 4, "lander", Vector2.zero),
            new RelicRecipe("garden_gnome", 14f, 2, "garage", new Vector2(1.2f, 0f)),
        };
    }
}
