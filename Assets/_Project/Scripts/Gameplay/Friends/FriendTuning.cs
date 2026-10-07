using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// How every friend behaves: site and part placement rules, the part pickups, the repair and boot-up, flying,
    /// following, walking home unseen, home life, greetings and the spotter ability. Created by the Gameplay/Tuning
    /// builder; runtime code only reads it.
    /// </summary>
    public sealed class FriendTuning : ScriptableObject
    {
        [Header("Site")]
        [Tooltip("Candidate spots tried for a friend's site.")]
        [Range(16, 2048)] [SerializeField] private int _siteAttempts = 600;

        [Tooltip("Metres kept between a site and the edge of the drivable floor.")]
        [Range(0f, 60f)] [SerializeField] private float _siteEdgeMargin = 12f;

        [Tooltip("Steepest ground (degrees) at a site and around it (07 parks there to repair).")]
        [Range(2f, 30f)] [SerializeField] private float _siteMaxSlope = 12f;

        [Tooltip("Radius (m) around a site that must also be that gentle.")]
        [Range(0.5f, 10f)] [SerializeField] private float _siteFlatProbe = 2.5f;

        [Tooltip("Minimum metres between a friend's site and any relic site.")]
        [Range(0f, 100f)] [SerializeField] private float _siteClearOfRelics = 30f;

        [Tooltip("The view check stands at the base pad's edge, this far (m) from its centre toward the site.")]
        [Range(5f, 60f)] [SerializeField] private float _baseEdge = 25f;

        [Tooltip("Height (m) of the viewer's eye at the base edge.")]
        [Range(0.5f, 5f)] [SerializeField] private float _viewerEyeHeight = 1.6f;

        [Tooltip("Height (m) above the site floor that must be in view (the broken friend's silhouette).")]
        [Range(0.1f, 3f)] [SerializeField] private float _siteViewHeight = 0.5f;

        [Tooltip("Terrain samples along that line of sight.")]
        [Range(8, 256)] [SerializeField] private int _sightSamples = 64;

        [Header("Parts")]
        [Tooltip("Candidate spots tried per part.")]
        [Range(8, 512)] [SerializeField] private int _partAttempts = 96;

        [Tooltip("Steepest ground (degrees) a part may lie on.")]
        [Range(2f, 30f)] [SerializeField] private float _partMaxSlope = 14f;

        [Tooltip("Metres kept between a part and the edge of the drivable floor.")]
        [Range(0f, 60f)] [SerializeField] private float _partEdgeMargin = 8f;

        [Tooltip("Minimum metres between two parts of a friend.")]
        [Range(1f, 60f)] [SerializeField] private float _partSpacing = 15f;

        [Tooltip("No part closer than this (m) to the base centre or to a relic site.")]
        [Range(0f, 60f)] [SerializeField] private float _partClearance = 12f;

        [Tooltip("Height (m) a part floats above the ground.")]
        [Range(0.1f, 2f)] [SerializeField] private float _partHover = 0.55f;

        [Tooltip("Bob amplitude (m) of a waiting part.")]
        [Range(0f, 0.5f)] [SerializeField] private float _partBob = 0.1f;

        [Tooltip("Bob frequency (Hz) of a waiting part.")]
        [Range(0.05f, 2f)] [SerializeField] private float _partBobFrequency = 0.3f;

        [Tooltip("Idle turn (degrees per second) of a waiting part.")]
        [Range(0f, 180f)] [SerializeField] private float _partSpin = 30f;

        [Tooltip("A part within this many metres of 07 drifts in (a little closer than scrap: softer).")]
        [Range(1f, 15f)] [SerializeField] private float _partMagnetRadius = 4.5f;

        [Tooltip("Flight time (s) of a part right next to 07.")]
        [Range(0.2f, 4f)] [SerializeField] private float _partFlightDuration = 1f;

        [Tooltip("Extra flight time (s) per metre.")]
        [Range(0f, 0.5f)] [SerializeField] private float _partFlightPerMetre = 0.08f;

        [Tooltip("Height (m) a part lifts in its flight.")]
        [Range(0f, 4f)] [SerializeField] private float _partFlightLift = 1.3f;

        [Tooltip("Radius (m) of a part's spiral.")]
        [Range(0f, 3f)] [SerializeField] private float _partSpiralRadius = 0.4f;

        [Tooltip("Turns of a part's spiral.")]
        [Range(0f, 4f)] [SerializeField] private float _partSpiralTurns = 0.9f;

        [Tooltip("Scale of a part as it reaches 07.")]
        [Range(0.05f, 1f)] [SerializeField] private float _partArrivalScale = 0.5f;

        [Tooltip("Brightness of a lit part lamp on the friend's body (emission × this).")]
        [Range(0f, 4f)] [SerializeField] private float _partLampGlow = 1.6f;

        [Tooltip("Seconds (time constant) for a part lamp to light.")]
        [Range(0f, 3f)] [SerializeField] private float _partLampEase = 0.4f;

        [Header("Repair")]
        [Tooltip("07 can repair a friend from within this many metres.")]
        [Range(1f, 15f)] [SerializeField] private float _repairRadius = 5f;

        [Tooltip("Seconds Interact must be held to begin (then the repair plays out on its own).")]
        [Range(0f, 3f)] [SerializeField] private float _repairHold = 0.5f;

        [Tooltip("Stitches per second of 07's beam across the friend.")]
        [Range(0.2f, 10f)] [SerializeField] private float _stitchRate = 2.5f;

        [Tooltip("Half-width (m) of the stitching across the friend's body.")]
        [Range(0.02f, 2f)] [SerializeField] private float _stitchSpread = 0.25f;

        [Tooltip("Shiver (degrees) of the friend while it is stitched, growing toward the end.")]
        [Range(0f, 20f)] [SerializeField] private float _shiver = 4f;

        [Tooltip("Seconds the eye flickers before it stays on.")]
        [Range(0.1f, 4f)] [SerializeField] private float _eyeFlicker = 1f;

        [Tooltip("Seconds the rotors take to spin up.")]
        [Range(0.1f, 4f)] [SerializeField] private float _rotorSpinUp = 1.2f;

        [Tooltip("Seconds it takes to wobble up into the air.")]
        [Range(0.1f, 6f)] [SerializeField] private float _liftOff = 1.8f;

        [Tooltip("Seconds it looks at 07 before chirping happily.")]
        [Range(0.1f, 4f)] [SerializeField] private float _lookAt07 = 0.9f;

        [Tooltip("Wobble (degrees) while lifting off, settling as it steadies.")]
        [Range(0f, 30f)] [SerializeField] private float _liftWobble = 12f;

        [Header("Flying")]
        [Tooltip("Hover height (m) above the ground while awake.")]
        [Range(0.5f, 6f)] [SerializeField] private float _hoverHeight = 1.6f;

        [Tooltip("Lowest it flies above the ground (m).")]
        [Range(0.2f, 3f)] [SerializeField] private float _minClearance = 0.8f;

        [Tooltip("Cruise speed (m/s).")]
        [Range(1f, 20f)] [SerializeField] private float _cruiseSpeed = 9f;

        [Tooltip("Speed (m/s) when it has fallen behind and catches up.")]
        [Range(1f, 40f)] [SerializeField] private float _catchUpSpeed = 18f;

        [Tooltip("Seconds of easing toward where it wants to be (higher = floatier).")]
        [Range(0.05f, 3f)] [SerializeField] private float _smoothTime = 0.55f;

        [Tooltip("Seconds (time constant) to turn toward where it looks.")]
        [Range(0f, 2f)] [SerializeField] private float _turnEase = 0.3f;

        [Tooltip("Lean (degrees) per m/s² of acceleration, capped below.")]
        [Range(0f, 10f)] [SerializeField] private float _leanPerAcceleration = 2.5f;

        [Tooltip("Largest lean (degrees).")]
        [Range(0f, 45f)] [SerializeField] private float _maxLean = 16f;

        [Tooltip("Hover bob (m).")]
        [Range(0f, 0.5f)] [SerializeField] private float _bob = 0.08f;

        [Tooltip("Hover bob frequency (Hz).")]
        [Range(0.05f, 3f)] [SerializeField] private float _bobFrequency = 0.5f;

        [Tooltip("Rotor spin (degrees per second) in flight.")]
        [Range(0f, 5000f)] [SerializeField] private float _rotorSpeed = 1500f;

        [Tooltip("Seconds (time constant) for the rotors to change speed.")]
        [Range(0f, 3f)] [SerializeField] private float _rotorEase = 0.4f;

        [Header("Following 07")]
        [Tooltip("Metres behind 07.")]
        [Range(0f, 15f)] [SerializeField] private float _followBehind = 4f;

        [Tooltip("Metres above 07.")]
        [Range(0.5f, 10f)] [SerializeField] private float _followHeight = 3f;

        [Tooltip("Metres to one side of 07, so it is never between the camera and 07.")]
        [Range(0f, 10f)] [SerializeField] private float _followSide = 2.5f;

        [Tooltip("It keeps at least this angle (degrees) off the camera's line to 07.")]
        [Range(0f, 45f)] [SerializeField] private float _cameraClearAngle = 12f;

        [Tooltip("Farther than this (m) from 07 it flies at catch-up speed.")]
        [Range(5f, 100f)] [SerializeField] private float _catchUpDistance = 25f;

        [Tooltip("Farther than this (m) it softly fades out and back in behind 07 (never lost).")]
        [Range(20f, 500f)] [SerializeField] private float _reappearDistance = 150f;

        [Tooltip("Seconds of that fade out (and again in).")]
        [Range(0.1f, 3f)] [SerializeField] private float _reappearFade = 0.6f;

        [Header("Home")]
        [Tooltip("07 is home within this distance (m) of the lander: friends live at the base.")]
        [Range(5f, 100f)] [SerializeField] private float _homeRadius = 30f;

        [Tooltip("07 has left home beyond this distance (m): friends come along.")]
        [Range(5f, 150f)] [SerializeField] private float _leaveRadius = 40f;

        [Tooltip("A return counts as coming home (and earns a greeting) after 07 was this far (m) away.")]
        [Range(5f, 200f)] [SerializeField] private float _awayRadius = 45f;

        [Tooltip("Radius (m) of the greeting circle around 07.")]
        [Range(0.5f, 10f)] [SerializeField] private float _greetRadius = 2.6f;

        [Tooltip("Height (m) of the greeting circle above 07.")]
        [Range(0.5f, 10f)] [SerializeField] private float _greetHeight = 2.2f;

        [Tooltip("Seconds of the greeting circle.")]
        [Range(0.5f, 10f)] [SerializeField] private float _greetDuration = 2.8f;

        [Tooltip("Seconds of a nap on the perch (min, max).")]
        [SerializeField] private Vector2 _napDuration = new Vector2(8f, 14f);

        [Tooltip("Seconds of flitting around the lander (min, max).")]
        [SerializeField] private Vector2 _flitDuration = new Vector2(6f, 10f);

        [Tooltip("Distance (m) of flitting waypoints from the lander (min, max).")]
        [SerializeField] private Vector2 _flitRadius = new Vector2(3f, 8f);

        [Tooltip("Height (m) of flitting waypoints above the ground (min, max).")]
        [SerializeField] private Vector2 _flitHeight = new Vector2(1.5f, 3.5f);

        [Tooltip("Seconds per flitting waypoint.")]
        [Range(0.5f, 10f)] [SerializeField] private float _flitHop = 2.5f;

        [Tooltip("Seconds spent inspecting the museum shelf.")]
        [Range(0.5f, 20f)] [SerializeField] private float _inspectDuration = 4f;

        [Tooltip("Metres in front of the shelf it hovers to inspect it.")]
        [Range(0.3f, 5f)] [SerializeField] private float _inspectDistance = 1.6f;

        [Header("Walking home on its own")]
        [Tooltip("Walking speed (m/s) of a friend making its own way home (Bell's waddle).")]
        [Range(0.1f, 5f)] [SerializeField] private float _walkSpeed = 1.1f;

        [Tooltip("Within this many metres of a waypoint on its way it heads for the next one.")]
        [Range(0.1f, 10f)] [SerializeField] private float _walkWaypointReach = 1.5f;

        [Tooltip("It is set down at home only while at least this far (m) from 07, out of view, its home too.")]
        [Range(10f, 300f)] [SerializeField] private float _unseenDistance = 60f;

        [Tooltip("Radius (m) of the sphere standing on the ground (its whole body) that must be off screen.")]
        [Range(0.1f, 5f)] [SerializeField] private float _unseenMargin = 1.2f;

        [Header("Finding its way")]
        [Tooltip("Cell size (m) of the grid a walking friend's way is searched on.")]
        [Range(0.5f, 10f)] [SerializeField] private float _pathCell = 2.5f;

        [Tooltip("Metres the search box reaches beyond both ends of the way.")]
        [Range(0f, 200f)] [SerializeField] private float _pathMargin = 30f;

        [Tooltip("Largest height change (m) between neighbouring cells it walks (a gentle slope, never a wall).")]
        [Range(0.05f, 5f)] [SerializeField] private float _pathMaxClimb = 1f;

        [Tooltip("Most cells the search expands before it gives up (a broken world).")]
        [Range(100, 200000)] [SerializeField] private int _pathMaxNodes = 40000;

        [Header("Spotter")]
        [Tooltip("Undiscovered relics, parts and scrap within this many metres of 07 catch its eye.")]
        [Range(5f, 100f)] [SerializeField] private float _spotRadius = 40f;

        [Tooltip("Height (m) it hovers above what it spotted.")]
        [Range(0.5f, 10f)] [SerializeField] private float _spotHover = 2.5f;

        [Tooltip("Seconds it hovers there, pinging softly.")]
        [Range(0.5f, 15f)] [SerializeField] private float _spotDuration = 3.5f;

        [Tooltip("Seconds between two spots.")]
        [Range(0f, 60f)] [SerializeField] private float _spotCooldown = 6f;

        [Tooltip("It gives up a spot and comes back if 07 gets this far (m) away.")]
        [Range(5f, 150f)] [SerializeField] private float _spotLeash = 50f;

        [Tooltip("Radius (m) of its little light cone on the ground.")]
        [Range(0.1f, 5f)] [SerializeField] private float _spotConeRadius = 1.1f;

        [Tooltip("Brightness of its light cone.")]
        [Range(0f, 4f)] [SerializeField] private float _spotConeGlow = 0.8f;

        [Tooltip("Brightness of the light pillar a spotted relic raises (softer than a ping's answer).")]
        [Range(0f, 4f)] [SerializeField] private float _spotPillar = 0.7f;

        public int SiteAttempts => _siteAttempts;
        public float SiteEdgeMargin => _siteEdgeMargin;
        public float SiteMaxSlope => _siteMaxSlope;
        public float SiteFlatProbe => _siteFlatProbe;
        public float SiteClearOfRelics => _siteClearOfRelics;
        public float BaseEdge => _baseEdge;
        public float ViewerEyeHeight => _viewerEyeHeight;
        public float SiteViewHeight => _siteViewHeight;
        public int SightSamples => _sightSamples;
        public int PartAttempts => _partAttempts;
        public float PartMaxSlope => _partMaxSlope;
        public float PartEdgeMargin => _partEdgeMargin;
        public float PartSpacing => _partSpacing;
        public float PartClearance => _partClearance;
        public float PartHover => _partHover;
        public float PartBob => _partBob;
        public float PartBobFrequency => _partBobFrequency;
        public float PartSpin => _partSpin;
        public float PartMagnetRadius => _partMagnetRadius;
        public float PartFlightDuration => _partFlightDuration;
        public float PartFlightPerMetre => _partFlightPerMetre;
        public float PartFlightLift => _partFlightLift;
        public float PartSpiralRadius => _partSpiralRadius;
        public float PartSpiralTurns => _partSpiralTurns;
        public float PartArrivalScale => _partArrivalScale;
        public float PartLampGlow => _partLampGlow;
        public float PartLampEase => _partLampEase;
        public float RepairRadius => _repairRadius;
        public float RepairHold => _repairHold;
        public float StitchRate => _stitchRate;
        public float StitchSpread => _stitchSpread;
        public float Shiver => _shiver;
        public float EyeFlicker => _eyeFlicker;
        public float RotorSpinUp => _rotorSpinUp;
        public float LiftOff => _liftOff;
        public float LookAt07 => _lookAt07;
        public float LiftWobble => _liftWobble;
        public float HoverHeight => _hoverHeight;
        public float MinClearance => _minClearance;
        public float CruiseSpeed => _cruiseSpeed;
        public float CatchUpSpeed => _catchUpSpeed;
        public float SmoothTime => _smoothTime;
        public float TurnEase => _turnEase;
        public float LeanPerAcceleration => _leanPerAcceleration;
        public float MaxLean => _maxLean;
        public float Bob => _bob;
        public float BobFrequency => _bobFrequency;
        public float RotorSpeed => _rotorSpeed;
        public float RotorEase => _rotorEase;
        public float FollowBehind => _followBehind;
        public float FollowHeight => _followHeight;
        public float FollowSide => _followSide;
        public float CameraClearAngle => _cameraClearAngle;
        public float CatchUpDistance => _catchUpDistance;
        public float ReappearDistance => _reappearDistance;
        public float ReappearFade => _reappearFade;
        public float HomeRadius => _homeRadius;
        public float LeaveRadius => Mathf.Max(_leaveRadius, _homeRadius + 1f);
        public float AwayRadius => Mathf.Max(_awayRadius, LeaveRadius);
        public float GreetRadius => _greetRadius;
        public float GreetHeight => _greetHeight;
        public float GreetDuration => _greetDuration;
        public Vector2 NapDuration => Ordered(_napDuration);
        public Vector2 FlitDuration => Ordered(_flitDuration);
        public Vector2 FlitRadius => Ordered(_flitRadius);
        public Vector2 FlitHeight => Ordered(_flitHeight);
        public float FlitHop => _flitHop;
        public float InspectDuration => _inspectDuration;
        public float InspectDistance => _inspectDistance;
        public float WalkSpeed => _walkSpeed;
        public float WalkWaypointReach => _walkWaypointReach;
        public float UnseenDistance => _unseenDistance;
        public float UnseenMargin => _unseenMargin;
        public float PathCell => _pathCell;
        public float PathMargin => _pathMargin;
        public float PathMaxClimb => _pathMaxClimb;
        public int PathMaxNodes => _pathMaxNodes;
        public float SpotRadius => _spotRadius;
        public float SpotHover => _spotHover;
        public float SpotDuration => _spotDuration;
        public float SpotCooldown => _spotCooldown;
        public float SpotLeash => _spotLeash;
        public float SpotConeRadius => _spotConeRadius;
        public float SpotConeGlow => _spotConeGlow;
        public float SpotPillar => _spotPillar;

        /// <summary>Seconds from the end of the stitching until the friend is up, looking at 07.</summary>
        public float BootDuration => Mathf.Max(_eyeFlicker, _rotorSpinUp) + _liftOff + _lookAt07;

        private static Vector2 Ordered(Vector2 range)
        {
            return range.x <= range.y ? range : new Vector2(range.y, range.x);
        }
    }
}
