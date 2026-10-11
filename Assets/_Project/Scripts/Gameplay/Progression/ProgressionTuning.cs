using System;
using System.Collections.Generic;
using UnityEngine;

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

        [Header("The steps wait on this content")]
        [Tooltip("The friend whose faint signal the tower hears first (step 3).")]
        [SerializeField] private string _signalFriend = "tilly";

        [Tooltip("The friend brought home over the chasm (step 6).")]
        [SerializeField] private string _leapFriend = "bell";

        [Tooltip("The kit fitted in the bay first (step 5); its blueprint is pinned on Kenji's board from the start.")]
        [SerializeField] private string _firstKit = "rover.hover_jump";

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

        [Tooltip("Brightness of the pillar the evening's sweep raises on the horizon.")]
        [Range(0.1f, 2f)] [SerializeField] private float _eveningPillarBrightness = 0.7f;

        [Header("The masts' letters (radio-letters text)")]
        [Tooltip("The first this many lit masts play one letter each (the relay's official messages)...")]
        [Range(0, 12)] [SerializeField] private int _singleLetterMasts = 4;

        [Tooltip("...and every later mast plays this many (Ro's own letters).")]
        [Range(1, 4)] [SerializeField] private int _lettersAfter = 2;

        [Tooltip("Letters there are to play, letter.mast_1 to letter.mast_n.")]
        [Range(0, 40)] [SerializeField] private int _letterCount = 10;

        [Header("Blueprints")]
        [Tooltip("Where the other kits' blueprint tubes lie: each in a salvage site's heart, found with its relic.")]
        [SerializeField] private BlueprintTube[] _tubes =
        {
            new BlueprintTube("rover.cargo_cradle", "site.garage"),
            new BlueprintTube("rover.warm_headlamp", "site.kestrel"),
            new BlueprintTube("rover.boost_coils", "site.drill"),
        };

        [Header("The tower's components (P1)")]
        [Tooltip("One part per tower level, needed besides materials: the coil in the depot's heart (L1), the " +
                 "transformer in the garage's heart (L2), the relay spare at the first restored mast (L3).")]
        [SerializeField] private TowerPart[] _towerParts =
        {
            new TowerPart("Part_TowerCoil", 1, TowerPartSource.SiteHeart, "site.depot", Vector2.zero),
            new TowerPart("Part_Transformer", 2, TowerPartSource.SiteHeart, "site.garage", new Vector2(0f, 1.3f),
                true),
            new TowerPart("Part_RelaySpare", 3, TowerPartSource.FirstRestoredMast, string.Empty, Vector2.zero),
        };

        [Tooltip("Seconds a component takes to ease into view once it is there to be found.")]
        [Range(0.1f, 4f)] [SerializeField] private float _partAppearSeconds = 1.2f;

        [Tooltip("Seconds a component glides from 07's back into the tower's port hopper.")]
        [Range(0.3f, 5f)] [SerializeField] private float _partFeedSeconds = 1.6f;

        [Tooltip("Height (m) of the arc a component glides along into the hopper.")]
        [Range(0f, 3f)] [SerializeField] private float _partFeedArc = 0.8f;

        [Header("The lab's reader (M3-19)")]
        [Tooltip("Seconds 07's beam takes to feed a blueprint tube into the reader's slot.")]
        [Range(0.3f, 4f)] [SerializeField] private float _readerFeedSeconds = 1.2f;

        [Tooltip("Seconds the reader decodes a tube: a moment, not a timer.")]
        [Range(1f, 10f)] [SerializeField] private float _readerDecodeSeconds = 4f;

        [Tooltip("Turns per second of the reader's reel while it decodes.")]
        [Range(0.1f, 4f)] [SerializeField] private float _reelTurnsPerSecond = 0.8f;

        [Tooltip("Seconds the hand-off's lamp pulse takes from the reader down the leg and along the cable to the " +
                 "bay.")]
        [Range(0.5f, 10f)] [SerializeField] private float _handOffSeconds = 3f;

        [Header("Kenji's pen plotter (P7)")]
        [Tooltip("Seconds the plotter takes to draw a decoded sheet.")]
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

        public float ReaderFeedSeconds => _readerFeedSeconds;

        public float ReaderDecodeSeconds => _readerDecodeSeconds;

        public float ReelTurnsPerSecond => _reelTurnsPerSecond;

        public float HandOffSeconds => _handOffSeconds;

        public float PlotterDrawSeconds => _plotterDrawSeconds;

        public int PlotterRows => _plotterRows;

        public float PlotterLampGlow => _plotterLampGlow;

        public float PlotterLampEase => _plotterLampEase;

        public float PlaceLineGap => _placeLineGap;

        public string SignalFriend => _signalFriend;

        public string LeapFriend => _leapFriend;

        public string FirstKit => _firstKit;

        public IReadOnlyList<BlueprintTube> Tubes => _tubes;

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
