using UnityEngine;

namespace MoonProject.Core.Events
{
    /// <summary>A scrap piece reached the rover. <see cref="ComboStep"/> climbs while pickups chain (melody step).</summary>
    public readonly struct ScrapCollected
    {
        public ScrapCollected(Vector3 position, int value, int comboStep)
        {
            Position = position;
            Value = value;
            ComboStep = comboStep;
        }

        public Vector3 Position { get; }

        public int Value { get; }

        /// <summary>0 for the first pickup of a chain, +1 for each pickup that follows within the combo window.</summary>
        public int ComboStep { get; }
    }

    /// <summary>The scrap balance changed (pickup, purchase, refund).</summary>
    public readonly struct CurrencyChanged
    {
        public CurrencyChanged(int total, int delta)
        {
            Total = total;
            Delta = delta;
        }

        public int Total { get; }

        public int Delta { get; }
    }

    /// <summary>The rover emitted a sonar ping from <see cref="Origin"/>.</summary>
    public readonly struct SonarPinged
    {
        public SonarPinged(Vector3 origin, float range)
        {
            Origin = origin;
            Range = range;
        }

        public Vector3 Origin { get; }

        public float Range { get; }
    }

    /// <summary>A buried relic answered a ping (published at the moment the answer is heard).</summary>
    public readonly struct RelicAnswered
    {
        public RelicAnswered(Vector3 position, float distance, string relicId)
        {
            Position = position;
            Distance = distance;
            RelicId = relicId;
        }

        public Vector3 Position { get; }

        public float Distance { get; }

        /// <summary>Which relic answered (lets each relic answer with its own note).</summary>
        public string RelicId { get; }
    }

    /// <summary>The tractor beam started lifting a buried relic.</summary>
    public readonly struct ExcavationStarted
    {
        public ExcavationStarted(Vector3 position)
        {
            Position = position;
        }

        public Vector3 Position { get; }
    }

    /// <summary>The tractor beam stopped; <see cref="Completed"/> is true when the relic fully surfaced.</summary>
    public readonly struct ExcavationStopped
    {
        public ExcavationStopped(Vector3 position, bool completed)
        {
            Position = position;
            Completed = completed;
        }

        public Vector3 Position { get; }

        public bool Completed { get; }
    }

    /// <summary>A relic finished surfacing and is now a loose physics object.</summary>
    public readonly struct RelicSurfaced
    {
        public RelicSurfaced(Vector3 position, string relicId)
        {
            Position = position;
            RelicId = relicId;
        }

        public Vector3 Position { get; }

        public string RelicId { get; }
    }

    /// <summary>The energy tether latched onto a body.</summary>
    public readonly struct TetherAttached
    {
        public TetherAttached(Vector3 position, float mass)
        {
            Position = position;
            Mass = mass;
        }

        public Vector3 Position { get; }

        public float Mass { get; }
    }

    /// <summary>The tether let go: released by the player, or softly snapped by the anti-frustration rule.</summary>
    public readonly struct TetherReleased
    {
        public TetherReleased(Vector3 position, bool snapped)
        {
            Position = position;
            Snapped = snapped;
        }

        public Vector3 Position { get; }

        public bool Snapped { get; }
    }

    /// <summary>A relic was placed on a museum shelf at the base.</summary>
    public readonly struct RelicDeposited
    {
        public RelicDeposited(string relicId, Vector3 position, int displayedCount)
        {
            RelicId = relicId;
            Position = position;
            DisplayedCount = displayedCount;
        }

        public string RelicId { get; }

        public Vector3 Position { get; }

        /// <summary>Relics on display after this deposit.</summary>
        public int DisplayedCount { get; }
    }

    /// <summary>An upgrade (rover or base) was bought.</summary>
    public readonly struct UpgradePurchased
    {
        public UpgradePurchased(string upgradeId, int level)
        {
            UpgradeId = upgradeId;
            Level = level;
        }

        public string UpgradeId { get; }

        public int Level { get; }
    }

    /// <summary>The radio tower's clear-signal radius changed (tower upgrade, load).</summary>
    public readonly struct SignalRadiusChanged
    {
        public SignalRadiusChanged(float radius)
        {
            Radius = radius;
        }

        public float Radius { get; }
    }

    /// <summary>A relay mast was restored and linked: the station's reach grew (docs/features/M3-06).</summary>
    public readonly struct RelayRestored
    {
        public RelayRestored(string relayId, Vector3 position, int litCount, int total, string linkedNodeId,
            float pulseSeconds)
        {
            RelayId = relayId;
            Position = position;
            LitCount = litCount;
            Total = total;
            LinkedNodeId = linkedNodeId;
            PulseSeconds = pulseSeconds;
        }

        /// <summary>
        /// A restoration known only by its mast (test stand-ins): no linked node (<see cref="LinkedNodeId"/> empty)
        /// and a pulse that arrives at once.
        /// </summary>
        public RelayRestored(string relayId, Vector3 position, int litCount, int total)
            : this(relayId, position, litCount, total, string.Empty, 0f)
        {
        }

        public string RelayId { get; }

        /// <summary>World position of the restored mast's Lamp (where 07 and the camera look up to).</summary>
        public Vector3 Position { get; }

        /// <summary>Lit masts after this one (home not counted).</summary>
        public int LitCount { get; }

        /// <summary>Masts in the whole game.</summary>
        public int Total { get; }

        /// <summary>The lit node its ground pulse runs to ("home" or a mast's id): the one it links to.</summary>
        public string LinkedNodeId { get; }

        /// <summary>Seconds from this event until the ground pulse arrives at <see cref="LinkedNodeId"/>.</summary>
        public float PulseSeconds { get; }
    }

    /// <summary>
    /// The radio-hop node list opened (07 parked on a lit pad choosing where to go) or closed. The camera keeps its
    /// lonely wide shot closed while the list is open.
    /// </summary>
    public readonly struct RadioHopListChanged
    {
        public RadioHopListChanged(bool open)
        {
            Open = open;
        }

        public bool Open { get; }
    }

    /// <summary>A radio-hop began: static rises and the view eases out before 07 is moved.</summary>
    public readonly struct RadioHopStarted
    {
        public RadioHopStarted(string fromId, string toId)
        {
            FromId = fromId;
            ToId = toId;
        }

        /// <summary>The node 07 hops from ("home" or a relay id).</summary>
        public string FromId { get; }

        public string ToId { get; }
    }

    /// <summary>A radio-hop ended: 07 stands on the target pad and the view eases back in.</summary>
    public readonly struct RadioHopFinished
    {
        public RadioHopFinished(string toId)
        {
            ToId = toId;
        }

        public string ToId { get; }
    }

    /// <summary>07 picked up a cassette tape (a new radio track and Ro's liner note).</summary>
    public readonly struct CassetteCollected
    {
        public CassetteCollected(string cassetteId, Vector3 position, int collected, int total)
        {
            CassetteId = cassetteId;
            Position = position;
            Collected = collected;
            Total = total;
        }

        public string CassetteId { get; }

        public Vector3 Position { get; }

        /// <summary>Tapes owned after this one.</summary>
        public int Collected { get; }

        /// <summary>Tapes in the whole game.</summary>
        public int Total { get; }
    }

    /// <summary>
    /// Something <see cref="IRadioProgram"/> reports changed (dial turned, tape chosen or collected, dial unlocked,
    /// save loaded). Listeners re-read the program; the event carries no data so it can never disagree with it.
    /// </summary>
    public readonly struct RadioProgramChanged
    {
    }

    /// <summary>07 opened a crew log (a cache or a friend's repair): the UI shows its card.</summary>
    public readonly struct CrewLogFound
    {
        public CrewLogFound(string logId, Vector3 position)
        {
            LogId = logId;
            Position = position;
        }

        /// <summary>Localization key suffix: the card reads <c>log.&lt;id&gt;</c>.</summary>
        public string LogId { get; }

        public Vector3 Position { get; }
    }

    /// <summary>Bell started pointing at something undiscovered (a warm pillar stands at the target).</summary>
    public readonly struct BellSignalPicked
    {
        public BellSignalPicked(BellSignalTarget target, Vector3 position)
        {
            Target = target;
            Position = position;
        }

        public BellSignalTarget Target { get; }

        public Vector3 Position { get; }
    }

    /// <summary>The thing Bell pointed at was found; its pillar fades.</summary>
    public readonly struct BellSignalFound
    {
        public BellSignalFound(BellSignalTarget target, Vector3 position)
        {
            Target = target;
            Position = position;
        }

        public BellSignalTarget Target { get; }

        public Vector3 Position { get; }
    }

    /// <summary>
    /// A moment of Bell's that Audio voices (other domains may react too): see <see cref="BellCued"/>.
    /// </summary>
    public enum BellCue
    {
        /// <summary>07's beam slid the tape into her slot during her repair (a tape click).</summary>
        TapeSlotted = 0,

        /// <summary>Her needle starts sweeping the band as she wakes from her repair (a station sweep).</summary>
        NeedleSwept = 1,

        /// <summary>She taps a foot to the music at home (a soft leg tap).</summary>
        FootTapped = 2,

        /// <summary>A new relic reached the shelf: her happy station-switch crackle.</summary>
        Crackled = 3,

        /// <summary>07 turned her dial one detent (a detented click; RadioProgramChanged follows).</summary>
        DialTurned = 4,
    }

    /// <summary>
    /// Bell did something worth a sound (docs/features/M3-05). Her jingle needs no cue of its own: its first three
    /// notes play on FriendRepaired("bell") as she stands up, the whole jingle on FriendGreeted("bell").
    /// </summary>
    public readonly struct BellCued
    {
        public BellCued(BellCue cue, Vector3 position)
        {
            Cue = cue;
            Position = position;
        }

        public BellCue Cue { get; }

        public Vector3 Position { get; }
    }

    /// <summary>
    /// A moment of a relay mast's restoration that Audio voices (other domains may react too): see
    /// <see cref="RelayCued"/>. The link tone needs no cue of its own: it plays on <see cref="RelayRestored"/>, when
    /// the mast comes online and its pulse leaves for home.
    /// </summary>
    public enum RelayCue
    {
        /// <summary>07 picked up a mast's relay part (drawn in like a friend's part).</summary>
        PartCollected = 0,

        /// <summary>07's beam starts stitching the mast (the scrap is paid).</summary>
        Stitched = 1,

        /// <summary>The relay part clicks into the mast's junction box.</summary>
        PartSlotted = 2,

        /// <summary>The mast straightens up with a creak.</summary>
        Straightened = 3,

        /// <summary>The mast's lamp starts warming: to full when it links home, else to a low listening glow.</summary>
        LampWarmed = 4,
    }

    /// <summary>A relay mast did something worth a sound (docs/features/M3-06).</summary>
    public readonly struct RelayCued
    {
        public RelayCued(RelayCue cue, string relayId, Vector3 position)
        {
            Cue = cue;
            RelayId = relayId;
            Position = position;
        }

        public RelayCue Cue { get; }

        /// <summary>The mast's anchor id ("relay.0", ...).</summary>
        public string RelayId { get; }

        public Vector3 Position { get; }
    }
}
