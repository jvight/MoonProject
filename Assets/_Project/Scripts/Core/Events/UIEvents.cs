namespace MoonProject.Core.Events
{
    /// <summary>
    /// The pause menu opened (<see cref="Paused"/> true: game time eases to a stop and the rover controls are off) or
    /// closed again. The radio keeps playing on unscaled time; listeners may soften anything that would otherwise
    /// hang frozen mid-sound.
    /// </summary>
    public readonly struct PauseChanged
    {
        public PauseChanged(bool paused)
        {
            Paused = paused;
        }

        public bool Paused { get; }
    }

    /// <summary>
    /// The player chose another language: every string read through <see cref="ILocalization"/> should be read
    /// again.
    /// </summary>
    public readonly struct LanguageChanged
    {
        public LanguageChanged(string language)
        {
            Language = language;
        }

        /// <summary>Code of the new language, e.g. "vi".</summary>
        public string Language { get; }
    }

    /// <summary>What a <see cref="UiCue"/> marks, so Audio can give every UI touch its own soft sound.</summary>
    public enum UiCueKind
    {
        /// <summary>The pause menu opened.</summary>
        MenuOpen = 0,

        /// <summary>The pause menu closed (resumed).</summary>
        MenuClose = 1,

        /// <summary>Keyboard or gamepad focus moved to another menu item.</summary>
        FocusMove = 2,

        /// <summary>A menu item was pressed (a button, a toggle, the language selector).</summary>
        Confirm = 3,

        /// <summary>Stepped back one level (a submenu or the quit question closed, a memory card dismissed).</summary>
        Back = 4,

        /// <summary>A memory card began to appear.</summary>
        CardShown = 5,

        /// <summary>The tower's hold-to-confirm ring started filling.</summary>
        HoldFill = 6,

        /// <summary>The ring filled and the upgrade was bought.</summary>
        HoldComplete = 7,

        /// <summary>A context prompt began to appear.</summary>
        PromptShown = 8,

        /// <summary>A settings slider moved one step.</summary>
        SliderStep = 9,

        /// <summary>
        /// The hold ended before the ring filled: the button was let go, or the offer disappeared (once per press).
        /// </summary>
        HoldRelease = 10,

        /// <summary>A part was picked up on the 07 tab (a drag began): Audio plays a soft pick-up tick.</summary>
        PartPick = 11,

        /// <summary>A part was set down where it fits (fitted, or taken off to the row): a soft drop tick.</summary>
        PartDrop = 12,

        /// <summary>A part was dropped where it does not fit: a quiet refusal.</summary>
        PartRefuse = 13,

        /// <summary>A databank page started to flip on 07's face: Audio plays a small relay click.</summary>
        DatabankPageFlip = 14,

        /// <summary>The databank's picture settled on its new page (the CRT channel settling): a soft tube thunk.</summary>
        DatabankChannelSettle = 15,

        /// <summary>The databank's list cursor moved one line: a tape-head tick (rate-limited like focus steps).</summary>
        DatabankListStep = 16,
    }

    /// <summary>
    /// A line for the radio ticker along the bottom of the HUD (docs/features/M3-05 "The ticker"). Any domain may
    /// publish it; the UI localizes <see cref="Key"/>, formats <see cref="Argument"/> into its {0} and queues it, so
    /// publishers never need <see cref="ILocalization"/> and queued lines follow a language change.
    /// </summary>
    public readonly struct TickerLine
    {
        public TickerLine(string key, string argument, bool argumentIsKey)
        {
            Key = key;
            Argument = argument;
            ArgumentIsKey = argumentIsKey;
        }

        public TickerLine(string key, string argument)
            : this(key, argument, false)
        {
        }

        public TickerLine(string key)
            : this(key, string.Empty, false)
        {
        }

        public string Key { get; }

        /// <summary>Text for the line's {0} placeholder (e.g. a bearing); empty when the line has none.</summary>
        public string Argument { get; }

        /// <summary>
        /// True when <see cref="Argument"/> is itself a localization key (e.g. "track.&lt;id&gt;.title" for
        /// "Now playing — {0}"), localized before formatting; false for literal text such as a bearing.
        /// </summary>
        public bool ArgumentIsKey { get; }
    }

    /// <summary>
    /// A UI moment worth a sound. Published by the UI at the moment it happens; carries no position (2D).
    /// </summary>
    public readonly struct UiCue
    {
        public UiCue(UiCueKind kind)
        {
            Kind = kind;
        }

        public UiCueKind Kind { get; }
    }

    /// <summary>
    /// One unit of the materials count rolled up on the HUD chip (docs/design/feel-pass.md §4, CountTick): published by
    /// the UI once per unit as the stock counts up, so Audio can play a rising pentatonic tick and Rover a short
    /// rumble. Gains only: a spend counts down quietly.
    /// </summary>
    public readonly struct StockCountTicked
    {
        public StockCountTicked(SalvageMaterial material, int index, int remaining)
        {
            Material = material;
            Index = index;
            Remaining = remaining;
        }

        /// <summary>The material whose count ticked.</summary>
        public SalvageMaterial Material { get; }

        /// <summary>
        /// Which unit of this roll it is, from 0: it keeps climbing while a second collect extends the roll, and starts
        /// again after the count has rested. A rising pitch follows it.
        /// </summary>
        public int Index { get; }

        /// <summary>How many units are still to come after this one (0 on the roll's last tick).</summary>
        public int Remaining { get; }
    }

    /// <summary>
    /// The first Tab press began its boot moment (docs/design/0.5.5-owner-feedback.md §3): the screen opens dark and
    /// 07's hold reports itself. Published once per save. Rover opens 07's rear hatch for it
    /// (<see cref="RoverHatchCycleRequested"/>); Audio may play the boot's first note.
    /// </summary>
    public readonly struct HoldBootStarted
    {
    }

    /// <summary>One of the hold's slot outlines lit during the boot: Audio plays a rising tick for it.</summary>
    public readonly struct HoldSlotLit
    {
        public HoldSlotLit(int index, int count)
        {
            Index = index;
            Count = count;
        }

        /// <summary>Which slot lit, from 0, left to right.</summary>
        public int Index { get; }

        /// <summary>How many slots the hold has.</summary>
        public int Count { get; }
    }

    /// <summary>A held item finished sliding into its slot during the boot.</summary>
    public readonly struct HoldItemArrived
    {
        public HoldItemArrived(int slot)
        {
            Slot = slot;
        }

        public int Slot { get; }
    }

    /// <summary>
    /// 07's face screen is on (or flipping to) <see cref="Page"/>: the camera's face shot eases to that page's distance
    /// (docs/design/databank.md §4; the databank sits closer so the tube nearly fills the frame).
    /// </summary>
    public readonly struct FacePageChanged
    {
        public FacePageChanged(FacePage page)
        {
            Page = page;
        }

        public FacePage Page { get; }
    }
}
