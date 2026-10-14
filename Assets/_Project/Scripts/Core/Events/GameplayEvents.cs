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

    /// <summary>
    /// A relic settled into 07's Cargo Cradle (docs/features/M3-11): it rides home in the rear rack instead of on the
    /// tether.
    /// </summary>
    public readonly struct RelicStowed
    {
        public RelicStowed(string relicId, Vector3 position)
        {
            RelicId = relicId;
            Position = position;
        }

        public string RelicId { get; }

        /// <summary>Where it rests in the rack (the cargo seat).</summary>
        public Vector3 Position { get; }
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
    /// A beat of a station's crafting that Audio voices (docs/features/M3-14, VISION ruling 14: 07 has no hands): see
    /// <see cref="StationCued"/>.
    /// </summary>
    public enum StationCue
    {
        /// <summary>
        /// 07's beam reaches the station's hopper and the recipe's bundles start flying in (a feed rattle).
        /// </summary>
        FeedStarted = 0,

        /// <summary>
        /// All the recipe's bundles are in the hopper (the feed's hum settles away): the station takes over from here.
        /// </summary>
        Fed = 1,

        /// <summary>The radio tower's service hatch swings open (a hinge creak).</summary>
        HatchOpened = 2,

        /// <summary>07's beam starts stitching up the tower while its new section rises.</summary>
        StitchStarted = 3,

        /// <summary>The stitching beam lets go and the tower's hatch swings shut (a latch click).</summary>
        HatchClosed = 4,

        /// <summary>
        /// One of the recipe's bundles reaches the hopper's mouth and drops in (a clunk and a short rattle): once per
        /// bundle, between <see cref="FeedStarted"/> and <see cref="Fed"/>, the last one just before
        /// <see cref="Fed"/>.
        /// </summary>
        BundleDropped = 5,
    }

    /// <summary>
    /// A station (Kenji's Rover Bay, the radio tower's service port) did something worth a sound while crafting
    /// <see cref="UpgradeId"/>, which was bought just before (<see cref="UpgradePurchased"/>).
    /// </summary>
    public readonly struct StationCued
    {
        public StationCued(StationCue cue, string upgradeId, Vector3 position)
        {
            Cue = cue;
            UpgradeId = upgradeId;
            Position = position;
        }

        public StationCue Cue { get; }

        public string UpgradeId { get; }

        /// <summary>The hopper's mouth (feed beats), the hatch (hatch beats) or the stitch's start.</summary>
        public Vector3 Position { get; }
    }

    /// <summary>
    /// 07 came to rest on the charging dock at home (<see cref="Docked"/> true), or left it (false): docked once it
    /// has stood still on the dock's pad for a moment with no drive input; any drive input undocks it at once
    /// (docs/features/M3-14). The rover settles 07 gently onto <see cref="Position"/> and <see cref="Rotation"/> (the
    /// lander's DockAnchor: 07's pivot, facing the lander), dims its lamp and lets go when undocked; the dock's glow
    /// warms meanwhile.
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

    /// <summary>The radio tower's clear-signal radius changed (tower upgrade, load).</summary>
    public readonly struct SignalRadiusChanged
    {
        public SignalRadiusChanged(float radius)
        {
            Radius = radius;
        }

        public float Radius { get; }
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
    /// A moment of a relay mast's restoration that Audio voices (other domains may react too): see
    /// <see cref="RelayCued"/>. The link tone needs no cue of its own: it plays on <see cref="RelayRestored"/>, when
    /// the mast comes online and its pulse leaves for home.
    /// </summary>
    public enum RelayCue
    {
        /// <summary>07 picked up a mast's relay part (drawn in like a friend's part).</summary>
        PartCollected = 0,

        /// <summary>07's beam starts stitching the mast (the recipe is paid).</summary>
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
    /// A dormant station answered the sonar (docs/features/M3-16 rule 1): a soft low "not yet" tone for Audio and one
    /// line for UI saying what wakes it (<see cref="ReasonKey"/>: "notyet.bay", "notyet.lift", "notyet.shelf").
    /// Published when the ping's ring reaches it.
    /// </summary>
    public readonly struct StationNotYet
    {
        public StationNotYet(string reasonKey, Vector3 position, float distance)
        {
            ReasonKey = reasonKey;
            Position = position;
            Distance = distance;
        }

        public string ReasonKey { get; }

        public Vector3 Position { get; }

        public float Distance { get; }
    }

    /// <summary>
    /// Kenji's pen plotter on the bay's back wall started drawing the sheet of decoded kit <see cref="KitId"/>
    /// (docs/features/M3-19 §5): the hand-off pulse arrived, or the bay woke with sheets waiting. Audio plays the pen
    /// scratch at <see cref="Position"/> while it draws for <see cref="Seconds"/>.
    /// </summary>
    public readonly struct BlueprintDrawing
    {
        public BlueprintDrawing(string kitId, Vector3 position, float seconds)
        {
            KitId = kitId;
            Position = position;
            Seconds = seconds;
        }

        public string KitId { get; }

        public Vector3 Position { get; }

        public float Seconds { get; }
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
    /// 07 handed component <see cref="PartId"/> to the tower's port hopper at <see cref="Position"/>
    /// (docs/features/M3-16 P1): it leaves 07's back and glides in, and tower level <see cref="TowerLevel"/> can now be
    /// built.
    /// </summary>
    public readonly struct ComponentDelivered
    {
        public ComponentDelivered(string partId, int towerLevel, Vector3 position)
        {
            PartId = partId;
            TowerLevel = towerLevel;
            Position = position;
        }

        public string PartId { get; }

        public int TowerLevel { get; }

        public Vector3 Position { get; }
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

    /// <summary>A beat of the crew's belly lift that Audio voices (docs/features/M3-19 §4): see <see cref="LiftCued"/>.
    /// The lift's feed and stitch use <see cref="StationCued"/>.</summary>
    public enum LiftCue
    {
        /// <summary>07 comes near the jammed winch (once per approach, at the winch): an old creak.</summary>
        WinchCreaked = 0,

        /// <summary>The platform starts moving (at the platform): a ratchet and a soft motor hum while it rides.
        /// </summary>
        RideStarted = 1,

        /// <summary>The platform reaches a stop (at the platform): a clunk, and the ride's sound lets go.</summary>
        RideStopped = 2,

        /// <summary>The belly hatch's leaves slide open above the rising platform (at the hatch): an A.</summary>
        HatchOpened = 3,

        /// <summary>The belly hatch's leaves close behind the lowered platform (at the hatch): a soft seat.</summary>
        HatchClosed = 4,
    }

    /// <summary>The crew's lift did something worth a sound at <see cref="Position"/>.</summary>
    public readonly struct LiftCued
    {
        public LiftCued(LiftCue cue, Vector3 position)
        {
            Cue = cue;
            Position = position;
        }

        public LiftCue Cue { get; }

        public Vector3 Position { get; }
    }

    /// <summary>
    /// The crew's belly lift (docs/features/M3-19 §4) started or finished carrying 07: <see cref="Riding"/> while it
    /// moves, <see cref="Inside"/> once it settled in the lab (the turntable takes over; Back brings it down).
    /// </summary>
    public readonly struct LiftRideChanged
    {
        public LiftRideChanged(bool riding, bool inside, Vector3 position)
        {
            Riding = riding;
            Inside = inside;
            Position = position;
        }

        public bool Riding { get; }

        public bool Inside { get; }

        /// <summary>The platform's position.</summary>
        public Vector3 Position { get; }
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
    /// The Rover Bay finished crafting <see cref="Part"/>: it waits in the parts row at <see cref="RowIndex"/>
    /// (docs/design/0.5.5-owner-feedback.md §3.5), never in a hold slot and never fitted by itself.
    /// </summary>
    public readonly struct PartCrafted
    {
        public PartCrafted(RoverKitPiece part, int rowIndex)
        {
            Part = part;
            RowIndex = rowIndex;
        }

        public RoverKitPiece Part { get; }

        public int RowIndex { get; }
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
    /// <see cref="Part"/> came off <see cref="Socket"/> into the parts row at its station (§3.5): Rover plays the
    /// short reverse moment, and the ability it gave pauses until it is refitted.
    /// </summary>
    public readonly struct PartRemoved
    {
        public PartRemoved(RoverSocket socket, RoverKitPiece part)
        {
            Socket = socket;
            Part = part;
        }

        public RoverSocket Socket { get; }

        public RoverKitPiece Part { get; }
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
}
