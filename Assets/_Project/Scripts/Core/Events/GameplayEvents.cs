using UnityEngine;

namespace MoonProject.Core.Events
{
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

    /// <summary>
    /// A salvage site answered a ping (docs/features/M3-13): it still has pieces to cut or a relic in its heart.
    /// Published at the moment the answer is heard; each site answers with its own tone.
    /// </summary>
    public readonly struct SiteAnswered
    {
        public SiteAnswered(string siteId, Vector3 position, float distance, bool holdsRelic)
        {
            SiteId = siteId;
            Position = position;
            Distance = distance;
            HoldsRelic = holdsRelic;
        }

        /// <summary>The site's anchor id ("site.depot", "site.kestrel", ...).</summary>
        public string SiteId { get; }

        public Vector3 Position { get; }

        public float Distance { get; }

        /// <summary>A relic still waits in the site's heart (the answer can carry a warmer overtone).</summary>
        public bool HoldsRelic { get; }
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
        public RelicDeposited(string relicId, Vector3 position, int displayedCount, string crewSet = null,
            int setCount = 0, int setSize = 0)
        {
            RelicId = relicId;
            Position = position;
            DisplayedCount = displayedCount;
            CrewSet = crewSet;
            SetCount = setCount;
            SetSize = setSize;
        }

        public string RelicId { get; }

        public Vector3 Position { get; }

        /// <summary>Relics on display after this deposit.</summary>
        public int DisplayedCount { get; }

        /// <summary>Whose belongings it is ("ro", "kenji", "ines", "theo"), or null.</summary>
        public string CrewSet { get; }

        /// <summary>That crew member's belongings on display after this deposit.</summary>
        public int SetCount { get; }

        /// <summary>How many belongings that crew member has in this version.</summary>
        public int SetSize { get; }
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

    /// <summary>
    /// 07 is held on a charging pad (<see cref="Docked"/> true), or let go of it (false): docked while it charges on a
    /// pad and the wheel is untouched; any touch of the wheel undocks it at once. The rover eases 07 gently onto
    /// <see cref="Position"/> (07's pivot on the pad's deck) and <see cref="Rotation"/> (the pad's heading), dims its
    /// lamp and lets go when undocked.
    /// </summary>
    public readonly struct RoverDockChanged
    {
        public RoverDockChanged(bool docked, Vector3 position, Quaternion rotation)
        {
            Docked = docked;
            Position = position;
            Rotation = rotation;
        }

        public bool Docked { get; }

        public Vector3 Position { get; }

        public Quaternion Rotation { get; }
    }

    /// <summary>07's beam started cutting a salvage piece loose (docs/features/M3-13).</summary>
    public readonly struct SalvageCutStarted
    {
        public SalvageCutStarted(Vector3 position, SalvageMaterial material)
        {
            Position = position;
            Material = material;
        }

        /// <summary>The piece's cut point, where the sparks fly.</summary>
        public Vector3 Position { get; }

        public SalvageMaterial Material { get; }
    }

    /// <summary>The cut stopped: the piece came loose (<see cref="Completed"/>) or the player let go (progress kept).</summary>
    public readonly struct SalvageCutStopped
    {
        public SalvageCutStopped(bool completed)
        {
            Completed = completed;
        }

        public bool Completed { get; }
    }

    /// <summary>
    /// Salvage reached 07 (a cut piece folded into its cargo). <see cref="ComboStep"/> climbs while pieces chain at
    /// one site, for the salvage melody.
    /// </summary>
    public readonly struct MaterialSalvaged
    {
        public MaterialSalvaged(SalvageMaterial material, int amount, Vector3 position, string siteId, int comboStep,
            MaterialSource source = MaterialSource.Cut)
        {
            Source = source;
            Material = material;
            Amount = amount;
            Position = position;
            SiteId = siteId;
            ComboStep = comboStep;
        }

        public SalvageMaterial Material { get; }

        public int Amount { get; }

        public Vector3 Position { get; }

        /// <summary>The site it came from ("site.kestrel", ...).</summary>
        public string SiteId { get; }

        /// <summary>0 for the first piece in a chain, then 1, 2, ... (resets after a pause).</summary>
        public int ComboStep { get; }

        /// <summary>How it was gained (M3-27): a cut, a dig, a drill, refining or a crew crate.</summary>
        public MaterialSource Source { get; }
    }

    /// <summary>07's material stock changed (salvage, a craft, load). Carries the new totals.</summary>
    public readonly struct MaterialsChanged
    {
        public MaterialsChanged(int metal, int wiring, int optics)
        {
            Metal = metal;
            Wiring = wiring;
            Optics = optics;
        }

        public int Metal { get; }

        public int Wiring { get; }

        public int Optics { get; }
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

    /// <summary>A radio-hop ended: 07 stands on the target pad and the view eases back in.</summary>
    public readonly struct RadioHopFinished
    {
        public RadioHopFinished(string toId)
        {
            ToId = toId;
        }

        public string ToId { get; }
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

        /// <summary>07's beam taps her dial's knob (a tiny tick) just before she turns it.</summary>
        DialTapped = 5,
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
    /// The player chose to start over (the title's New Game, confirmed when a save exists). The App puts the save away
    /// as &lt;slot&gt;.old-&lt;stamp&gt;.json (never deleted) and reloads the scene, which boots straight into a fresh
    /// game (no title) once.
    /// </summary>
    public readonly struct NewGameRequested
    {
    }

    /// <summary>
    /// Power reached a new stage of the base (docs/features/M3-16), or was restored on load
    /// (<see cref="Restored"/> = true: apply the state at once, with no wake moment).
    /// </summary>
    public readonly struct BasePowerChanged
    {
        public BasePowerChanged(BasePowerStage stage, bool restored)
        {
            Stage = stage;
            Restored = restored;
        }

        public BasePowerStage Stage { get; }

        public bool Restored { get; }
    }

    /// <summary>
    /// The gentle "what next" line changed (docs/features/M3-16): a localization key for the current step's invitation,
    /// or empty when there is nothing to suggest. UI shows it under the compass while 07 is still.
    /// </summary>
    public readonly struct NextStepChanged
    {
        public NextStepChanged(string lineKey)
        {
            LineKey = lineKey;
        }

        public string LineKey { get; }
    }

    /// <summary>
    /// 07 came near a place for the first time this save (docs/features/M3-16 "Each place and its one job"). UI shows the
    /// place's name and its one-line job once; <see cref="PlaceId"/> is a <see cref="WorldAnchorIds"/>-style id.
    /// </summary>
    public readonly struct PlaceFirstApproached
    {
        public PlaceFirstApproached(string placeId)
        {
            PlaceId = placeId;
        }

        public string PlaceId { get; }
    }

    /// <summary>
    /// 07 picked up one of the tower's components (docs/features/M3-16 P1, M3-21 §6): the component tower level
    /// <see cref="TowerLevel"/> needs besides materials. <see cref="PartId"/> is Art's model name (Part_TowerCoil,
    /// Part_Transformer, Part_RelaySpare). It rides on 07's back until <see cref="ComponentDelivered"/>. Restored is
    /// true when a loaded game puts it back on 07 (once, after the save is applied): shown at once, no ride-in.
    /// </summary>
    public readonly struct ComponentFound
    {
        public ComponentFound(string partId, int towerLevel, Vector3 position, bool restored)
        {
            PartId = partId;
            TowerLevel = towerLevel;
            Position = position;
            Restored = restored;
        }

        public string PartId { get; }

        public int TowerLevel { get; }

        /// <summary>Where 07 picked it up.</summary>
        public Vector3 Position { get; }

        public bool Restored { get; }
    }

    /// <summary>
    /// 07 parked on its charging dock after a trip on which something new woke or was found (docs/features/M3-16 P6,
    /// "the dock's evening"). Rover eases out to a wide shot of home's newly lit lamps and Audio swells the evening
    /// tone; once the shot has settled, Gameplay's tower sweep raises the next signal pillar. Published by Gameplay at
    /// most once per return home.
    /// </summary>
    public readonly struct DockEveningOpened
    {
        public DockEveningOpened(Vector3 dockPosition, float towerHold = 0f)
        {
            DockPosition = dockPosition;
            TowerHold = towerHold;
        }

        public Vector3 DockPosition { get; }

        /// <summary>
        /// Seconds the wide shot rests on the tower beyond its usual hold: above zero on the first evening after a
        /// recovery row completed (docs/features/M3-21 §2); the tower's sweep waits as long.
        /// </summary>
        public float TowerHold { get; }
    }

    /// <summary>
    /// 07's look up the crew's ladder (docs/features/M3-16 "Each place"): the first time near it, 07 looks up the
    /// ladder to the deck, then at the lift. Published as each look begins (<see cref="Looking"/>, toward
    /// <see cref="Subject"/>) and once more when it ends; Rover frames each look.
    /// </summary>
    public readonly struct LadderLookChanged
    {
        public LadderLookChanged(bool looking, Vector3 subject)
        {
            Looking = looking;
            Subject = subject;
        }

        public bool Looking { get; }

        public Vector3 Subject { get; }
    }

    /// <summary>
    /// The cut's charge-up reached a stage (docs/design/0.5.5-owner-feedback.md §5.1). Published right after the
    /// charge's <see cref="FeelBeat"/> Anticipate with <see cref="Stage"/> 0 (the charge starts: the 0.5 s brace
    /// first unless <see cref="Warm"/>), then at each stage lock, 1..<see cref="StagesNeeded"/>; the last is the full
    /// lock. A release before it drains (the Anticipate's Cancel beat).
    /// </summary>
    public readonly struct CutChargeStaged
    {
        public CutChargeStaged(int stage, int stagesNeeded, bool warm, float stageSeconds)
        {
            StagesNeeded = Mathf.Max(1, stagesNeeded);
            Stage = Mathf.Clamp(stage, 0, StagesNeeded);
            Warm = warm;
            StageSeconds = Mathf.Max(0f, stageSeconds);
        }

        /// <summary>0 as the charge starts, then the stage that just locked.</summary>
        public int Stage { get; }

        /// <summary>The stages the piece needs before the beam fires: small 1, medium 2, large and hull 3.</summary>
        public int StagesNeeded { get; }

        /// <summary>A warm emitter (a re-press soon after the last firing at the same site): no brace, quicker
        /// stages.</summary>
        public bool Warm { get; }

        /// <summary>Seconds each stage takes to build at this tier (warm included).</summary>
        public float StageSeconds { get; }

        /// <summary>The needed stage locked: the beam fires after the held breath.</summary>
        public bool IsFullLock => Stage >= StagesNeeded;
    }

    /// <summary>
    /// <see cref="Part"/> is going onto 07 in <see cref="Socket"/> (§3.5): a crafted part fitted at its station, or a
    /// friend's gift that fits itself (<see cref="Gift"/>). Rover plays the install moment (a swap when
    /// <see cref="Replaced"/>: the old part comes off and the new one goes on in one moment) and the Proud chirp. The
    /// bay recycles a swapped-out part into <see cref="RecycledMetal"/> Metal for home (never lost). A loaded game
    /// publishes nothing: Rover reads <see cref="IRoverEquipment"/> at boot.
    /// </summary>
    public readonly struct PartFitted
    {
        public PartFitted(RoverSocket socket, RoverKitPiece part, bool gift, bool replaced, RoverKitPiece replacedPart,
            int recycledMetal)
        {
            Socket = socket;
            Part = part;
            Gift = gift;
            Replaced = replaced;
            ReplacedPart = replacedPart;
            RecycledMetal = recycledMetal;
        }

        public RoverSocket Socket { get; }

        public RoverKitPiece Part { get; }

        /// <summary>True for a friend's gift: the soft version of the moment, no bay arms.</summary>
        public bool Gift { get; }

        /// <summary>True when a fitted lower tier came off for it.</summary>
        public bool Replaced { get; }

        /// <summary>The part that came off (meaningful only when <see cref="Replaced"/>).</summary>
        public RoverKitPiece ReplacedPart { get; }

        /// <summary>Metal the bay made of the part that came off (0 without a swap).</summary>
        public int RecycledMetal { get; }
    }

    /// <summary>
    /// 07 started scanning fragment <see cref="FragmentId"/> (docs/design/0.5.5-owner-feedback.md §1.2): it eases to a
    /// stop, its lid lifts fully and the fan of light sweeps from the lens toward <see cref="Point"/>.
    /// </summary>
    public readonly struct ScanStarted
    {
        public ScanStarted(string fragmentId, Vector3 point)
        {
            FragmentId = fragmentId ?? string.Empty;
            Point = point;
        }

        public string FragmentId { get; }

        public Vector3 Point { get; }
    }

    /// <summary>
    /// The scan of <see cref="FragmentId"/> stopped short (E let go): <see cref="Progress"/> is kept.
    /// </summary>
    public readonly struct ScanStopped
    {
        public ScanStopped(string fragmentId, float progress)
        {
            FragmentId = fragmentId ?? string.Empty;
            Progress = progress;
        }

        public string FragmentId { get; }

        public float Progress { get; }
    }

    /// <summary>
    /// Fragment <see cref="FragmentId"/> is fully scanned and its dissolve begins (§1.2, D2): fragment
    /// <see cref="Index"/> + 1 of <see cref="Count"/> for <see cref="BlueprintId"/>. <see cref="Known"/> when the
    /// blueprint was already complete: <see cref="Metal"/> follows the dissolve instead of new knowledge.
    /// </summary>
    public readonly struct FragmentScanned
    {
        public FragmentScanned(string fragmentId, string blueprintId, int index, int count, bool known, int metal)
        {
            FragmentId = fragmentId ?? string.Empty;
            BlueprintId = blueprintId ?? string.Empty;
            Index = index;
            Count = count;
            Known = known;
            Metal = metal;
        }

        public string FragmentId { get; }

        public string BlueprintId { get; }

        /// <summary>How many of the blueprint's fragments were scanned before this one (0-based).</summary>
        public int Index { get; }

        public int Count { get; }

        public bool Known { get; }

        /// <summary>
        /// Metal a known blueprint's fragment pays after its dissolve (Gameplay's tuning; 0 for new knowledge): the
        /// pickup note shows this amount.
        /// </summary>
        public int Metal { get; }
    }

    /// <summary>
    /// A line of 07's boot readout changed (docs/design/opening-v2.md §10): <see cref="System"/> is now
    /// <see cref="State"/>. Restored at a session start, once per system, so the readout starts from the save; live
    /// when a fault is fixed (the cutter turning OK at B6: Rover lights the cutter's pilot light, Audio plays the
    /// "done" tone, UI turns the line OK).
    /// </summary>
    public readonly struct RoverSystemChanged
    {
        public RoverSystemChanged(RoverSystem system, RoverSystemState state, bool restored)
        {
            System = system;
            State = state;
            Restored = restored;
        }

        public RoverSystem System { get; }

        public RoverSystemState State { get; }

        public bool Restored { get; }
    }

    /// <summary>
    /// 07 took story part <see cref="PartId"/> (the spare igniter, opening-v2 §10 B4) out of a crate at
    /// <see cref="Point"/> into its hold: UI's pickup note.
    /// </summary>
    public readonly struct StoryPartCollected
    {
        public StoryPartCollected(string partId, Vector3 point)
        {
            PartId = partId ?? string.Empty;
            Point = point;
        }

        public string PartId { get; }

        public Vector3 Point { get; }
    }

    /// <summary>
    /// E asked the faulted cutter to cut at <see cref="Point"/> and it refused (opening-v2 §10, before B6): Audio's
    /// low fault tone, UI flashes the CUTTER · FAULT line. Once per press.
    /// </summary>
    public readonly struct CutterRefused
    {
        public CutterRefused(Vector3 point)
        {
            Point = point;
        }

        public Vector3 Point { get; }
    }

    /// <summary>
    /// The first time the faulted cutter is asked to cut (opening v3 slice 1.6): it tries to light without its igniter
    /// and sputters at <see cref="Point"/>. Audio plays the dry sputter, Rover a startled beat on 07's face; UI's
    /// cutter card (CUTTER · FAULT · NO IGNITER) follows the <see cref="CutterRefused"/> it comes with. Once per game.
    /// </summary>
    public readonly struct CutterSputtered
    {
        public CutterSputtered(Vector3 point)
        {
            Point = point;
        }

        public Vector3 Point { get; }
    }

    /// <summary>
    /// A story scan is fully scanned (docs/design/opening-v2.md B7, the habitat's airlock panel): 07's CRT shows the
    /// readout <see cref="TextKey"/>, read off <see cref="Point"/>. It teaches and pays nothing; once per save, never
    /// on a load.
    /// </summary>
    public readonly struct StoryScanned
    {
        public StoryScanned(string storyId, string textKey, Vector3 point)
        {
            StoryId = storyId ?? string.Empty;
            TextKey = textKey ?? string.Empty;
            Point = point;
        }

        public string StoryId { get; }

        public string TextKey { get; }

        public Vector3 Point { get; }
    }

    /// <summary>
    /// The last fragment of <see cref="BlueprintId"/>'s set is scanned (§1.2 step 4): the sheet completes on the
    /// lab's board, the ticker says so once and 07 chirps Proud. The bay can build it from now on.
    /// </summary>
    public readonly struct BlueprintCompleted
    {
        public BlueprintCompleted(string blueprintId)
        {
            BlueprintId = blueprintId ?? string.Empty;
        }

        public string BlueprintId { get; }
    }

    /// <summary>
    /// The seal of container <see cref="ContainerId"/> is cut through (docs/design/0.5.5-owner-feedback.md §2): the
    /// cut's own Reward, then the container opens (<see cref="ContainerOpened"/>).
    /// </summary>
    public readonly struct ContainerSealCut
    {
        public ContainerSealCut(string containerId, ContainerKind kind, Vector3 position)
        {
            ContainerId = containerId ?? string.Empty;
            Kind = kind;
            Position = position;
        }

        public string ContainerId { get; }

        public ContainerKind Kind { get; }

        public Vector3 Position { get; }
    }

    /// <summary>
    /// Container <see cref="ContainerId"/> stands open and its find slides out (§2). A loaded game's opened containers
    /// are shown open with no event.
    /// </summary>
    public readonly struct ContainerOpened
    {
        public ContainerOpened(string containerId, ContainerKind kind, Vector3 position)
        {
            ContainerId = containerId ?? string.Empty;
            Kind = kind;
            Position = position;
        }

        public string ContainerId { get; }

        public ContainerKind Kind { get; }

        public Vector3 Position { get; }
    }

    /// <summary>
    /// One of the base's lamps began to come on at dusk (docs/design: the first nightfall's payoff): the lamp of
    /// structure <see cref="InstanceId"/>, <see cref="Index"/> places along the base's lighting order (0 = the first).
    /// Raised once per lamp as it starts easing up; audio can follow it.
    /// </summary>
    public readonly struct BaseLampLit
    {
        public BaseLampLit(int instanceId, int index)
        {
            InstanceId = instanceId;
            Index = index;
        }

        public int InstanceId { get; }

        public int Index { get; }
    }

    /// <summary>
    /// The charging pad's lock closed on 07: its wheel chocks stand and its side clamp arms have swung in against its
    /// flanks (the soft mechanical thunk), at <see cref="Position"/>. Raised once per lock, not for a lock a loaded game
    /// shows silently.
    /// </summary>
    public readonly struct PadLockClosed
    {
        public PadLockClosed(Vector3 position)
        {
            Position = position;
        }

        public Vector3 Position { get; }
    }

    /// <summary>
    /// The charging pad's lock began to let 07 go (the battery is full, or the player pushed the throttle): the clamp
    /// arms open first, the chocks last.
    /// </summary>
    public readonly struct PadLockOpened
    {
        public PadLockOpened(Vector3 position)
        {
            Position = position;
        }

        public Vector3 Position { get; }
    }

    /// <summary>Why 07's tow cable let its load go (<see cref="TowReleased"/>).</summary>
    public enum TowRelease
    {
        /// <summary>The player let go (the tether key).</summary>
        Player = 0,

        /// <summary>The load came home: it reached the apron, and the winch unhooks it there.</summary>
        Arrived = 1,

        /// <summary>The load fell too far behind (caught fast while 07 drove on): the cable unhooked softly.</summary>
        Snapped = 2,
    }

    /// <summary>
    /// 07's winch hooked its cable onto a towable load (docs/design/opening-v3-purpose.md R6, ruling 4: a physical
    /// cable, no beam): <see cref="TowId"/> names the load, <see cref="Position"/> is its hitch.
    /// </summary>
    public readonly struct TowAttached
    {
        public TowAttached(string towId, Vector3 position, float mass)
        {
            TowId = towId;
            Position = position;
            Mass = mass;
        }

        public string TowId { get; }

        public Vector3 Position { get; }

        /// <summary>The load's mass (kg): heavier loads strain the cable more.</summary>
        public float Mass { get; }
    }

    /// <summary>07's tow cable let its load <see cref="TowId"/> go at <see cref="Position"/> (its hitch).</summary>
    public readonly struct TowReleased
    {
        public TowReleased(string towId, Vector3 position, TowRelease reason)
        {
            TowId = towId;
            Position = position;
            Reason = reason;
        }

        public string TowId { get; }

        public Vector3 Position { get; }

        public TowRelease Reason { get; }
    }

    /// <summary>
    /// A bay of the socket rack at the pad changed what it shows (<see cref="IRackStatus"/>): its outline appeared
    /// (the rack came out at a dock, or 07 saw the part) or it filled (the part was fitted). Restored is true when a
    /// session start replays a saved rack, which plays no reveal.
    /// </summary>
    public readonly struct RackChanged
    {
        public RackChanged(int bay, RackBayState state, bool restored)
        {
            Bay = bay;
            State = state;
            Restored = restored;
        }

        /// <summary>The bay's index on the rack, left to right.</summary>
        public int Bay { get; }

        public RackBayState State { get; }

        public bool Restored { get; }
    }

    /// <summary>
    /// The first dusk begins (docs/design/opening-v3-purpose.md R6): after power-on the sky held in the afternoon
    /// until 07 docked after visiting a near site, or learned the battery bank; now the Sun sinks to last light and
    /// holds there until the night is ready. Published once by Gameplay's opening, and again at each session start of
    /// a game where it has begun, with <see cref="Restored"/> true, so the sky snaps to it with no transition.
    /// </summary>
    public readonly struct OpeningDuskBegan
    {
        public OpeningDuskBegan(bool restored)
        {
            Restored = restored;
        }

        /// <summary>Replayed from a loaded game, not begun now.</summary>
        public bool Restored { get; }
    }
}
