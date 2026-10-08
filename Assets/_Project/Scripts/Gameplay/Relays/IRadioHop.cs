namespace MoonProject.Gameplay
{
    /// <summary>
    /// The radio-hop (docs/features/M3-06), registered in the GameContext for the UI's tiny node list and screen fade.
    /// Parked on the pad of home or of a lit mast, not towing a relic, 07 can open a list of the other lit nodes, pick
    /// one and hop there in about 2 s: <see cref="MoonProject.Core.Events.RadioHopStarted"/>, an eased fade
    /// (<see cref="Fade"/>), 07 placed on the target pad facing out, then
    /// <see cref="MoonProject.Core.Events.RadioHopFinished"/> as the view eases back in. Gameplay already drives it
    /// from Interact (press to open, tap for the next node, hold to hop, drive off to close); the UI may call the same
    /// methods from its own controls. Read-only properties are allocation-free: poll them every frame.
    /// </summary>
    public interface IRadioHop
    {
        RadioHopPhase Phase { get; }

        /// <summary>07 is parked on a lit node's pad with somewhere to hop to: the list can open.</summary>
        bool CanOpen { get; }

        /// <summary>The node (an <see cref="MoonProject.Core.IStationReach"/> index) 07 is parked on, or -1.</summary>
        int Here { get; }

        /// <summary>Lit nodes in the open list (home first, then masts in order), 07's own excluded.</summary>
        int ChoiceCount { get; }

        /// <summary>
        /// The node (an <see cref="MoonProject.Core.IStationReach"/> index) of list entry <paramref name="choice"/>.
        /// </summary>
        int ChoiceNode(int choice);

        /// <summary>Localization key of list entry <paramref name="choice"/>'s name ("hop.node.home", ...).</summary>
        string ChoiceLabelKey(int choice);

        /// <summary>The highlighted list entry (0 when the list opens).</summary>
        int Selected { get; }

        /// <summary>0..1 how far Interact has been held toward hopping to the highlighted node.</summary>
        float ConfirmHold { get; }

        /// <summary>0 clear .. 1 dark: the screen fade of a hop in progress.</summary>
        float Fade { get; }

        /// <summary>0..1 through the hop in progress (0 while none).</summary>
        float Progress { get; }

        /// <summary>Opens the list (when <see cref="CanOpen"/>); false when it cannot.</summary>
        bool Open();

        /// <summary>Highlights the next entry (wrapping).</summary>
        void Next();

        /// <summary>Highlights the previous entry (wrapping).</summary>
        void Previous();

        /// <summary>Hops to the highlighted node; false when the list is not open.</summary>
        bool Confirm();

        /// <summary>Closes the list without hopping.</summary>
        void Cancel();
    }
}
