using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The base's wake-up (docs/features/M3-16): when 07 counts as awake and away from its dock, how near each place
    /// greets 07 the first time, how far apart those lines come, and which content the step flow waits on. Created by
    /// the Gameplay/Tuning builder; runtime code only reads it.
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

        [Tooltip("The memory shelf.")]
        [Range(1f, 30f)] [SerializeField] private float _shelfRadius = 4f;

        [Tooltip("The radio tower.")]
        [Range(1f, 30f)] [SerializeField] private float _towerRadius = 6f;

        [Tooltip("Kenji's Rover Bay.")]
        [Range(1f, 30f)] [SerializeField] private float _bayRadius = 7f;

        [Tooltip("Kenji's blueprint board.")]
        [Range(1f, 30f)] [SerializeField] private float _boardRadius = 4f;

        [Tooltip("The crew's ladder.")]
        [Range(1f, 30f)] [SerializeField] private float _ladderRadius = 3f;

        [Tooltip("The crew's lift.")]
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

        [Header("Blueprints")]
        [Tooltip("Where the other kits' blueprint tubes lie: each in a salvage site's heart, found with its relic.")]
        [SerializeField] private BlueprintTube[] _tubes =
        {
            new BlueprintTube("rover.cargo_cradle", "site.garage"),
            new BlueprintTube("rover.warm_headlamp", "site.kestrel"),
            new BlueprintTube("rover.boost_coils", "site.drill"),
        };

        [Header("The tower's story parts (P1)")]
        [Tooltip("One part per tower level, needed besides materials: the coil in the depot's heart (L1), the " +
                 "transformer in the garage's heart (L2), the relay spare at the first restored mast (L3).")]
        [SerializeField] private TowerPart[] _towerParts =
        {
            new TowerPart("Part_TowerCoil", 1, TowerPartSource.SiteHeart, "site.depot", Vector2.zero),
            new TowerPart("Part_Transformer", 2, TowerPartSource.SiteHeart, "site.garage", new Vector2(0f, 1.3f),
                true),
            new TowerPart("Part_RelaySpare", 3, TowerPartSource.FirstRestoredMast, string.Empty, Vector2.zero),
        };

        [Tooltip("Seconds a story part takes to ease into view once it is there to be found.")]
        [Range(0.1f, 4f)] [SerializeField] private float _partAppearSeconds = 1.2f;

        [Tooltip("Seconds a story part glides from 07's back into the tower's port hopper.")]
        [Range(0.3f, 5f)] [SerializeField] private float _partFeedSeconds = 1.6f;

        [Tooltip("Height (m) of the arc a story part glides along into the hopper.")]
        [Range(0f, 3f)] [SerializeField] private float _partFeedArc = 0.8f;

        [Header("Kenji's pen plotter (P7)")]
        [Tooltip("07 hands a blueprint tube to the plotter within this many metres (horizontal) of it.")]
        [Range(1f, 15f)] [SerializeField] private float _plotterReach = 6f;

        [Tooltip("Seconds the plotter takes to draw a delivered sheet.")]
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

        public float PlotterReach => _plotterReach;

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
                case ProgressionKeys.PlaceShelf: return _shelfRadius;
                case ProgressionKeys.PlaceTower: return _towerRadius;
                case ProgressionKeys.PlaceBay: return _bayRadius;
                case ProgressionKeys.PlaceBoard: return _boardRadius;
                case ProgressionKeys.PlaceLadder: return _ladderRadius;
                case ProgressionKeys.PlaceLift: return _liftRadius;
                case ProgressionKeys.PlaceRack: return _rackRadius;
                default: throw new ArgumentException($"'{placeId}' is not a place of the base.", nameof(placeId));
            }
        }
    }
}
