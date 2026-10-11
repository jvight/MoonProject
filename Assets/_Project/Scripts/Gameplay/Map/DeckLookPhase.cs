namespace MoonProject.Gameplay.Map
{
    /// <summary>Where 07's time on the lander deck is (docs/features/M3-07, cutscenes §6), in order.</summary>
    public enum DeckLookPhase
    {
        /// <summary>Not at the deck, or released to ride back down.</summary>
        Off = 0,

        /// <summary>The lookout's "seen" sweep: unvisited places in view blink in turn.</summary>
        Sweeping = 1,

        /// <summary>First ride only: the hatch has swung open; the lookout breathes until 07 looks at it or rests.</summary>
        Breathing = 2,

        /// <summary>The view rests on the crew's map; the chart pen inks what is new.</summary>
        Looking = 3,
    }
}
