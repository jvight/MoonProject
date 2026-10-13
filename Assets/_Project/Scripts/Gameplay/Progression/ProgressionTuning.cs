using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The base's wake-up (docs/features/M3-16, M3-19): when 07 counts as awake and away from its dock, how near each
    /// place greets 07 the first time, how far apart those lines come, and which content the step flow waits on.
    /// Created by the Gameplay/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class ProgressionTuning : ScriptableObject
    {
        [Header("Waking (step 0)")]
        [Tooltip("07 has woken up and gone exploring once it is this many metres (horizontal) from its dock.")]
        [Range(3f, 60f)] [SerializeField] private float _wakeDistance = 15f;

        [Header("First approach: a place greets 07 within this many metres (horizontal)")]
        [Tooltip("07's charging dock.")]
        [Range(1f, 30f)] [SerializeField] private float _dockRadius = 4f;

        [Tooltip("The lander, home.")]
        [Range(1f, 40f)] [SerializeField] private float _landerRadius = 9f;

        [Tooltip("The radio tower.")]
        [Range(1f, 30f)] [SerializeField] private float _towerRadius = 6f;

        [Tooltip("Kenji's Rover Bay.")]
        [Range(1f, 30f)] [SerializeField] private float _bayRadius = 7f;

        [Tooltip("The crew's lab, greeted the first time the lift settles 07 inside (horizontal metres from the " +
                 "platform's centre).")]
        [Range(1f, 30f)] [SerializeField] private float _labRadius = 3f;

        [Tooltip("The crew's ladder.")]
        [Range(1f, 30f)] [SerializeField] private float _ladderRadius = 3f;

        [Tooltip("The crew's belly lift.")]
        [Range(1f, 30f)] [SerializeField] private float _liftRadius = 4f;

        [Tooltip("Bell's tape rack.")]
        [Range(1f, 30f)] [SerializeField] private float _rackRadius = 4f;

        [Tooltip("Seconds between two places' first lines, so they come one at a time.")]
        [Range(0f, 20f)] [SerializeField] private float _placeLineGap = 6f;

        [Tooltip("The dock's place card waits until 07's battery is below this share (the first return from a trip).")]
        [Range(0.5f, 1f)] [SerializeField] private float _dockCardBelow = 0.9f;

        [Tooltip("The first-listen line waits for a listen at least this many metres from the dock (away from home).")]
        [Range(10f, 200f)] [SerializeField] private float _listenAwayFromHome = 40f;

        [Header("The steps wait on this content")]
        [Tooltip("The friend whose faint signal the tower hears first (step 3).")]
        [SerializeField] private string _signalFriend = "tilly";

        [Tooltip("The friend brought home over the chasm (step 6).")]
        [SerializeField] private string _leapFriend = "bell";

        [Tooltip("The kit that leaps the chasm (Hover-Jump): 07 looks across from the lip only while it has none (P2).")]
        [SerializeField] private string _leapKit = "rover.hover_jump";

        [Header("The chasm before the bay (P2)")]
        [Tooltip("07 has looked across the chasm once it comes this many metres (horizontal) from the lip without " +
                 "Hover-Jump.")]
        [Range(3f, 60f)] [SerializeField] private float _chasmLookRadius = 14f;

        [Tooltip("Seconds 07 holds still and looks across at Bell's light.")]
        [Range(1f, 10f)] [SerializeField] private float _chasmLookSeconds = 3.5f;

        [Tooltip("Brightness of Bell's warm pillar when Tilly spots it and when it pulses as 07 looks across.")]
        [Range(0.1f, 2f)] [SerializeField] private float _bellLightBrightness = 0.8f;

        [Header("The dock's evening (P6)")]
        [Tooltip("Seconds docked after the evening opens (Rover's wide shot settling) before the tower's listening " +
                 "sweep raises the next signal pillar.")]
        [Range(0.5f, 15f)] [SerializeField] private float _eveningSweepDelay = 4.5f;

        [Tooltip("Seconds the first evening after a recovery row completes holds longer on the tower before its " +
                 "sweep (docs/features/M3-21 §2).")]
        [Range(0f, 6f)] [SerializeField] private float _eveningCompletionHold = 2f;

        [Tooltip("Brightness of the pillar the evening's sweep raises on the horizon.")]
        [Range(0.1f, 2f)] [SerializeField] private float _eveningPillarBrightness = 0.7f;

        [Tooltip("Seconds after tower level 1 wakes home before its first sweep raises the faint signal's pillar " +
                 "(first-hour v2 §4).")]
        [Range(0f, 10f)] [SerializeField] private float _towerSweepDelay = 3f;

        [Tooltip("Brightness of the faint signal's pillar the tower's first sweep raises.")]
        [Range(0.1f, 2f)] [SerializeField] private float _towerSweepBrightness = 0.8f;

        [Header("The masts' letters (radio-letters text)")]
        [Tooltip("The first this many lit masts play one letter each (the relay's official messages)...")]
        [Range(0, 12)] [SerializeField] private int _singleLetterMasts = 4;

        [Tooltip("...and every later mast plays this many (Ro's own letters).")]
        [Range(1, 4)] [SerializeField] private int _lettersAfter = 2;

        [Tooltip("Letters there are to play, letter.mast_1 to letter.mast_n.")]
        [Range(0, 40)] [SerializeField] private int _letterCount = 10;

        [Header("Kenji's garage port (first-hour v2 §3)")]
        [Tooltip("The salvage site the port stands in: its last cut opens the port.")]
        [SerializeField] private string _portSite = "site.garage";

        [Tooltip("Where Art's GaragePort stands, in the site's own space (behind the garage's painted 07 pad).")]
        [SerializeField] private Vector3 _portLocal = new Vector3(0f, 0.12f, 3.3f);

        [Tooltip("The port's turn (deg) in the site's space: 180 faces the pad and the door.")]
        [Range(-180f, 180f)] [SerializeField] private float _portYaw = 180f;

        [Tooltip("The middle of the port's pad, in the site's own space (Art's painted 07 pad).")]
        [SerializeField] private Vector3 _portPadLocal = new Vector3(0f, 0.12f, 1.5f);

        [Tooltip("Metres from the pad's middle within which 07 counts as on the pad.")]
        [Range(0.5f, 4f)] [SerializeField] private float _portPadRadius = 1.4f;

        [Tooltip("Speed (m/s) under which 07 counts as parked on the pad.")]
        [Range(0.05f, 1f)] [SerializeField] private float _portParkSpeed = 0.3f;

        [Tooltip("Seconds 07 stays parked on the pad before the port's arm fits the tether ring.")]
        [Range(0f, 4f)] [SerializeField] private float _portParkSeconds = 1f;

        [Tooltip("Metres from the pad within which the open port's place card shows, once.")]
        [Range(2f, 30f)] [SerializeField] private float _portPlaceRadius = 9f;

        [Tooltip("Seconds the tape deck glides from Kenji's spare crate onto the tray, where the arm fits it.")]
        [Range(0.2f, 4f)] [SerializeField] private float _portTrayGlideSeconds = 1.2f;


        [Header("Recipe cards (M3-24 B)")]
        [Tooltip("Metres from the camera within which a hopper or a board sheet shows its recipe card when looked at.")]
        [Range(2f, 40f)] [SerializeField] private float _recipeLookDistance = 18f;

        [Tooltip("Degrees off the view's centre within which a hopper or a sheet counts as looked at.")]
        [Range(1f, 30f)] [SerializeField] private float _recipeLookAngle = 9f;

        [Tooltip("The card already shown holds until the view is this many times the angle away (no edge flicker).")]
        [Range(1f, 3f)] [SerializeField] private float _recipeLookHold = 1.6f;

        [Header("Tech tree and recovery panel (M3-21)")]
        [Tooltip("The tech board's kit pins and the bay's upgrade that builds each; a kit node left out stays unknown.")]
        [SerializeField] private TechKit[] _techKits =
        {
            new TechKit(TechNodeId.HoverJump, "rover.hover_jump"),
            new TechKit(TechNodeId.CargoCradle, "rover.cargo_cradle"),
            new TechKit(TechNodeId.WarmHeadlamp, "rover.warm_headlamp"),
            new TechKit(TechNodeId.CapacitorDrums, "rover.boost_coils"),
            new TechKit(TechNodeId.CellPackII, "rover.cell_pack"),
        };

        [Tooltip("The recovery panel's lamps no built thing lights yet: the Peak's foot, the Dish, the Answer.")]
        [SerializeField] private RecoveryLamps _recoveryLamps = new RecoveryLamps(1, 4, 1);

        [Header("The tower's components (P1)")]
        [Tooltip("One part per tower level, needed besides materials: the coil in the depot's heart (L1), the " +
                 "transformer in the garage's heart (L2), the relay spare at the first restored mast (L3).")]
        [SerializeField] private TowerPart[] _towerParts =
        {
            new TowerPart("Part_TowerCoil", 1, TowerPartSource.SiteHeart, "site.depot", Vector2.zero,
                container: "CoilCrate"),
            new TowerPart("Part_Transformer", 2, TowerPartSource.SiteHeart, "site.garage", new Vector2(0f, 1.3f),
                true, "JunctionBox"),
            new TowerPart("Part_RelaySpare", 3, TowerPartSource.FirstRestoredMast, string.Empty, Vector2.zero),
        };

        [Tooltip("Seconds a component takes to ease into view once it is there to be found.")]
        [Range(0.1f, 4f)] [SerializeField] private float _partAppearSeconds = 1.2f;


        [Tooltip("Seconds a component glides from 07's back into the tower's port hopper.")]
        [Range(0.3f, 5f)] [SerializeField] private float _partFeedSeconds = 1.6f;

        [Tooltip("Height (m) of the arc a component glides along into the hopper.")]
        [Range(0f, 3f)] [SerializeField] private float _partFeedArc = 0.8f;

        [Header("Kenji's pen plotter (P7)")]
        [Tooltip("Seconds the plotter takes to draw a learned sheet.")]
        [Range(1f, 20f)] [SerializeField] private float _plotterDrawSeconds = 6f;

        [Tooltip("Rows the pen sweeps across the bed while drawing.")]
        [Range(1, 24)] [SerializeField] private int _plotterRows = 8;

        [Tooltip("Glow of the plotter's ready lamp while it draws.")]
        [Range(0f, 4f)] [SerializeField] private float _plotterLampGlow = 1.5f;

        [Tooltip("Ease time (seconds) of the ready lamp coming on and going off.")]
        [Range(0.05f, 3f)] [SerializeField] private float _plotterLampEase = 0.4f;

        public float WakeDistance => _wakeDistance;

        public float ChasmLookRadius => _chasmLookRadius;

        public float ChasmLookSeconds => _chasmLookSeconds;

        public float BellLightBrightness => _bellLightBrightness;

        public float EveningSweepDelay => _eveningSweepDelay;

        public int SingleLetterMasts => _singleLetterMasts;

        public int LettersAfter => _lettersAfter;

        public int LetterCount => _letterCount;

        public float EveningPillarBrightness => _eveningPillarBrightness;

        public float TowerSweepDelay => _towerSweepDelay;

        public float TowerSweepBrightness => _towerSweepBrightness;

        public float EveningCompletionHold => _eveningCompletionHold;

        public float PlotterDrawSeconds => _plotterDrawSeconds;

        public int PlotterRows => _plotterRows;

        public float PlotterLampGlow => _plotterLampGlow;

        public float PlotterLampEase => _plotterLampEase;

        public float PlaceLineGap => _placeLineGap;
        public float ListenAwayFromHome => _listenAwayFromHome;
        public float DockCardBelow => _dockCardBelow;

        public string SignalFriend => _signalFriend;

        public string LeapFriend => _leapFriend;

        public string LeapKit => _leapKit;


        public string PortSite => _portSite;

        public Vector3 PortLocal => _portLocal;

        public float PortYaw => _portYaw;

        public Vector3 PortPadLocal => _portPadLocal;

        public float PortPadRadius => _portPadRadius;

        public float PortParkSpeed => _portParkSpeed;

        public float PortParkSeconds => _portParkSeconds;

        public float PortPlaceRadius => _portPlaceRadius;

        public float PortTrayGlideSeconds => _portTrayGlideSeconds;


        public float RecipeLookDistance => _recipeLookDistance;

        public float RecipeLookAngle => _recipeLookAngle;

        public float RecipeLookHold => _recipeLookHold;

        public IReadOnlyList<TechKit> TechKits => _techKits;

        public RecoveryLamps RecoveryLamps => _recoveryLamps;

        public IReadOnlyList<TowerPart> TowerParts => _towerParts;

        public float PartAppearSeconds => _partAppearSeconds;

        public float PartFeedSeconds => _partFeedSeconds;

        public float PartFeedArc => _partFeedArc;

        /// <summary>The first-approach radius of the place <paramref name="placeId"/>.</summary>
        public float RadiusOf(string placeId)
        {
            switch (placeId)
            {
                case ProgressionKeys.PlaceDock: return _dockRadius;
                case ProgressionKeys.PlaceLander: return _landerRadius;
                case ProgressionKeys.PlaceTower: return _towerRadius;
                case ProgressionKeys.PlaceBay: return _bayRadius;
                case ProgressionKeys.PlaceLab: return _labRadius;
                case ProgressionKeys.PlaceLadder: return _ladderRadius;
                case ProgressionKeys.PlaceLift: return _liftRadius;
                case ProgressionKeys.PlaceRack: return _rackRadius;
                default: throw new ArgumentException($"'{placeId}' is not a place of the base.", nameof(placeId));
            }
        }
    }
}
