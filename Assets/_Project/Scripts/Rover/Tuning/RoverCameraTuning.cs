using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The floaty drone camera behind 07: orbit input, idle recentering, downhill lift, speed FOV, landing bump, camera
    /// moments, the lonely wide shot when 07 rests, and the Cinemachine damping it hands to the orbital follow and
    /// composer.
    /// </summary>
    [CreateAssetMenu(fileName = "RoverCameraTuning", menuName = "MoonProject/Rover/Rover Camera Tuning")]
    public sealed class RoverCameraTuning : ScriptableObject
    {
        private const float MinRecenterSpeedSpan = 0.1f;

        [Header("Look input")]
        [Tooltip("Orbit degrees per pixel of mouse movement.")]
        [Range(0.005f, 1f)]
        [SerializeField] private float _mouseSensitivity = 0.08f;

        [Tooltip("Orbit speed (deg/s) at full gamepad stick.")]
        [Range(10f, 400f)]
        [SerializeField] private float _stickRate = 110f;

        [Tooltip("Invert vertical look by default (the player's setting replaces it at runtime).")]
        [SerializeField] private bool _invertY;

        [Tooltip("Lowest look sensitivity multiplier the settings menu can choose.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _minSensitivity = 0.25f;

        [Tooltip("Highest look sensitivity multiplier the settings menu can choose.")]
        [Range(1f, 10f)]
        [SerializeField] private float _maxSensitivity = 3f;

        [Tooltip("Furthest (deg) the player's look swings the view round from straight behind 07. 180 is no limit: "
            + "the view goes all the way round 07, face included. Less keeps it behind or three-quarter; 07 turning "
            + "under a held view may then leave it further, and the recenter brings it back.")]
        [Range(60f, 180f)]
        [SerializeField] private float _maxLookYaw = 180f;

        [Tooltip("Degrees before the limit over which the look slows to a stop, so the view settles against it.")]
        [Range(5f, 90f)]
        [SerializeField] private float _lookYawSoftBand = 30f;

        [Tooltip("Half-life (s) smoothing the look speed, so the orbit glides instead of stepping.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _lookHalfLife = 0.05f;

        [Header("Orbit")]
        [Tooltip("Distance (m) from the follow point to the camera.")]
        [Range(2f, 30f)]
        [SerializeField] private float _distance = 7.5f;

        [Tooltip("Height (m) of the follow point above the rover's ground contact.")]
        [Range(0f, 4f)]
        [SerializeField] private float _targetHeight = 1.1f;

        [Tooltip("Resting orbit elevation (deg above the horizon).")]
        [Range(-10f, 80f)]
        [SerializeField] private float _defaultPitch = 16f;

        [Tooltip("Lowest orbit elevation (deg): below zero the camera sinks under 07 and looks up at the sky over it. "
            + "Near the dust it stops sinking (Lowest Camera Height) and the rest of the way only tilts up.")]
        [Range(-40f, 60f)]
        [SerializeField] private float _minPitch = -25f;

        [Tooltip("Orbit elevation (deg) of the opening shot: low, so the camera looks almost level across the basin "
            + "at Earth and The Peak (design ruling 8: at most ~6 deg down). It eases to the resting elevation once 07 "
            + "starts driving.")]
        [Range(-10f, 80f)]
        [SerializeField] private float _openingPitch = 6f;

        [Tooltip("Highest orbit elevation (deg).")]
        [Range(10f, 89f)]
        [SerializeField] private float _maxPitch = 60f;

        [Tooltip("Lowest the orbit lets the camera sink (m above 07's ground contact); asking for a lower elevation "
            + "tilts the view up instead, so the camera never digs into the dust.")]
        [Range(0.2f, 2f)]
        [SerializeField] private float _lowestCameraHeight = 0.6f;

        [Tooltip("Most the view tilts up (deg) once the camera stopped sinking, so 07 stays in the frame, a small "
            + "silhouette under the sky.")]
        [Range(0f, 30f)]
        [SerializeField] private float _maxLookUpTilt = 10f;

        [Header("Wake frame (fresh title), when 07's face cannot be framed")]
        [Tooltip("The held frame: orbit angle (deg from straight behind 07), a low front three-quarter view.")]
        [Range(90f, 180f)]
        [SerializeField] private float _wakeYaw = 145f;

        [Tooltip("The held frame: orbit elevation (deg), low, looking up past the lens at the stars.")]
        [Range(-40f, 30f)]
        [SerializeField] private float _wakeElevation = -15f;

        [Tooltip("The held frame: camera distance (m) from 07, close on the lens.")]
        [Range(1.5f, 10f)]
        [SerializeField] private float _wakeDistance = 3.2f;

        [Tooltip("The held frame: where 07 sits on screen (+ = lower), leaving the sky above it.")]
        [Range(-0.4f, 0.4f)]
        [SerializeField] private float _wakeScreenY = 0.22f;

        [Header("Stargazing")]
        [Tooltip("Orbit elevation (deg) where looking up begins to count: 07 starts lifting its head toward the sky.")]
        [Range(-10f, 20f)]
        [SerializeField] private float _stargazeStartElevation = 0f;

        [Tooltip("Orbit elevation (deg) at which the view counts as looking fully up.")]
        [Range(-40f, 0f)]
        [SerializeField] private float _stargazeFullElevation = -20f;

        [Tooltip("Half-life (s) of 07 lifting its gaze to the sky: slow, like noticing the stars.")]
        [Range(0.05f, 5f)]
        [SerializeField] private float _stargazeRiseHalfLife = 1.2f;

        [Tooltip("Half-life (s) of 07 lowering its gaze again when the view drops or the player drives.")]
        [Range(0.05f, 3f)]
        [SerializeField] private float _stargazeFallHalfLife = 0.4f;

        [Tooltip("Seconds of rest while looking up before the stargazing beat begins (HUD fades, music thins).")]
        [Range(0.5f, 15f)]
        [SerializeField] private float _stargazeDelay = 3f;

        [Tooltip("How far up the view must look (0..1 between start and full elevation) for the beat to begin.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _stargazeBeginShare = 0.6f;

        [Tooltip("The beat holds while the view still looks at least this far up (below Begin Share, so a small "
            + "nudge of the view never ends it).")]
        [Range(0f, 1f)]
        [SerializeField] private float _stargazeHoldShare = 0.3f;

        [Tooltip("Above this speed (m/s) 07 is not still: no gaze to the sky and no beat.")]
        [Range(0.01f, 2f)]
        [SerializeField] private float _stargazeMaxSpeed = 0.3f;

        [Header("Following and auto-recenter")]
        [Tooltip("Seconds after the mouse stops before the camera may ease back behind 07.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _recenterDelay = 1.5f;

        [Tooltip("Seconds after the stick returns to centre before the camera may ease back behind 07.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _stickRecenterDelay = 1.5f;

        [Tooltip("Recenter only while 07 drives forward faster than this (m/s): parked, crawling or reversing, the "
            + "view stays where the player left it.")]
        [Range(0f, 8f)]
        [SerializeField] private float _recenterMinSpeed = 2f;

        [Tooltip("Forward speed (m/s) where auto-recenter reaches its full rate and normal delay; slower sightseeing "
            + "waits longer and turns back more gently (CTRL-09).")]
        [Range(1f, 15f)]
        [SerializeField] private float _recenterFullSpeed = 5f;

        [Tooltip("Multiplier on the idle delay at the minimum recenter speed, so slow sightseeing is left alone longer "
            + "(CTRL-09).")]
        [Range(1f, 3f)]
        [SerializeField] private float _recenterSlowDelayScale = 1.6f;

        [Tooltip("Share of the recenter turn and pitch rates kept at the minimum recenter speed, so slow recentering is "
            + "gentle (CTRL-09).")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _recenterSlowRateShare = 0.45f;

        [Tooltip("Seconds 07 must have been driving forward above the minimum speed before recentering may start.")]
        [Range(0f, 5f)]
        [SerializeField] private float _recenterDriveSeconds = 1f;

        [Tooltip("Views nearer than this (deg) to straight behind 07 are left alone: no micro-swings.")]
        [Range(0f, 90f)]
        [SerializeField] private float _recenterDeadzone = 25f;

        [Tooltip("Seconds over which recentering fades in once it starts (no sudden pull).")]
        [Range(0.05f, 5f)]
        [SerializeField] private float _recenterRampTime = 2f;

        [Tooltip("Seconds over which recentering fades out when 07 stops driving forward (look input stops it at "
            + "once).")]
        [Range(0.05f, 3f)]
        [SerializeField] private float _recenterFadeOut = 0.5f;

        [Tooltip("Fastest the view turns (deg/s) while recentering, once fully faded in.")]
        [Range(5f, 120f)]
        [SerializeField] private float _recenterMaxRate = 20f;

        [Tooltip("Degrees before straight behind 07 over which recentering eases out to a stop.")]
        [Range(1f, 90f)]
        [SerializeField] private float _recenterEaseAngle = 20f;

        [Tooltip("Fastest the elevation returns to its default (deg/s) while recentering.")]
        [Range(1f, 60f)]
        [SerializeField] private float _recenterPitchRate = 7f;

        [Tooltip("Seconds the recenter button takes to ease the view behind 07 (eased in and out).")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _recenterButtonSeconds = 1.4f;

        [Tooltip("Half-life (s) of the camera following 07's turns while it sits behind 07 (no free look).")]
        [Range(0f, 3f)]
        [SerializeField] private float _followHalfLife = 0.27f;

        [Tooltip("Fastest the camera follows 07's turns (deg/s): a pivot slides into view and never whips round.")]
        [Range(5f, 180f)]
        [SerializeField] private float _followMaxRate = 45f;

        [Header("Target framing")]
        [Tooltip("07 stopped or slower than this (m/s) near a scan or interaction target lets the camera move to show it.")]
        [Range(0f, 3f)]
        [SerializeField] private float _framingMaxSpeed = 1f;

        [Tooltip("Metres from 07 within which a scan or interaction target is framed.")]
        [Range(1f, 15f)]
        [SerializeField] private float _framingRange = 6f;

        [Tooltip("Half-angle (deg) of the view within which a target counts as in front of the camera; targets "
            + "behind it are left alone.")]
        [Range(10f, 120f)]
        [SerializeField] private float _framingViewCone = 70f;

        [Tooltip("Seconds 07 must be slow and the view still before the camera starts to move.")]
        [Range(0f, 5f)]
        [SerializeField] private float _framingEngageDelay = 0.4f;

        [Tooltip("Most extra camera elevation (deg) the framing may add, so the target shows over 07's body.")]
        [Range(0f, 60f)]
        [SerializeField] private float _framingLift = 25f;

        [Tooltip("Most the camera may swing (deg) to either side of 07 to see past its body.")]
        [Range(0f, 90f)]
        [SerializeField] private float _framingYaw = 80f;

        [Tooltip("Closest the camera may pull in, as a share of its normal distance, to see over 07's shoulder.")]
        [Range(0.2f, 1f)]
        [SerializeField] private float _framingMinRadiusScale = 0.6f;

        [Tooltip("Steps each of swing, lift and pull-in is searched in, from none to its most; more is finer.")]
        [Range(2, 24)]
        [SerializeField] private int _framingSearchSteps = 16;

        [Tooltip("How far the camera's look-at point moves from 07 toward the target (0 = stays on 07): the view then rests on the target, so it stays the thing E scans or uses.")]
        [Range(0f, 1f)]
        [SerializeField] private float _framingLookShare = 0.6f;

        [Tooltip("07's body as the framing sees it: width (m).")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _framingBodyWidth = 1.47f;

        [Tooltip("07's body as the framing sees it: length (m).")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _framingBodyLength = 2f;

        [Tooltip("07's body as the framing sees it: height to the top of its head (m).")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _framingBodyHeight = 1.53f;

        [Tooltip("Metres the sight line must clear 07's body by.")]
        [Range(0f, 1f)]
        [SerializeField] private float _framingBodyClearance = 0.2f;

        [Tooltip("The target must sit inside this central share of the screen (0.7 = the middle 70%).")]
        [Range(0.2f, 1f)]
        [SerializeField] private float _framingCentralShare = 0.7f;

        [Tooltip("Metres short of a target the sight line stops when it is tested against props and ground: a target sits on or in the thing that holds it.")]
        [Range(0f, 2f)]
        [SerializeField] private float _framingSightMargin = 0.6f;

        [Tooltip("What each target a view hides (a prop or the ground in the way, or 07's body or the screen's edge for any "
            + "but the main target) costs it, in units where the full swing costs 1: a clearer view is preferred, but not at "
            + "any cost in camera motion.")]
        [Range(0f, 4f)]
        [SerializeField] private float _framingHiddenCost = 0.8f;

        [Tooltip("Fastest the framing swings or lifts the camera (deg/s): slow and soft, so the owner never sees busy camera motion.")]
        [Range(5f, 90f)]
        [SerializeField] private float _framingMaxRate = 30f;

        [Tooltip("Half-life (s) of the camera easing toward the framing it found.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _framingSolveHalfLife = 0.3f;

        [Tooltip("Seconds the framing takes to ease in: slow and soft.")]
        [Range(0.5f, 8f)]
        [SerializeField] private float _framingRampSeconds = 1.5f;

        [Tooltip("Seconds the framing takes to ease out when 07 drives off, the target goes or the player looks.")]
        [Range(0.3f, 6f)]
        [SerializeField] private float _framingReleaseSeconds = 1.5f;

        [Header("Downhill lift")]
        [Tooltip("Extra elevation (deg) when driving downhill, to see the slope ahead.")]
        [Range(0f, 40f)]
        [SerializeField] private float _downhillLift = 10f;

        [Tooltip("Descent angle (deg) that earns the full lift.")]
        [Range(1f, 60f)]
        [SerializeField] private float _downhillFullAngle = 20f;

        [Tooltip("Half-life (s) of the lift easing in and out.")]
        [Range(0.05f, 5f)]
        [SerializeField] private float _liftHalfLife = 0.7f;

        [Tooltip("Smallest downhill terrain slope (deg along 07's heading) that keeps downhill framing when 07 slows or "
            + "stops on a hill (CTRL-09).")]
        [Range(0f, 15f)]
        [SerializeField] private float _downhillSlopeMinAngle = 1.5f;

        [Tooltip("Share of the downhill terrain slope kept as lift when 07 stops on a hill, so stopping does not tilt "
            + "the view up and lose the slope ahead (CTRL-09).")]
        [Range(0f, 1f)]
        [SerializeField] private float _stoppedDownhillShare = 1f;

        [Tooltip("Lowest forward speed (m/s, negative = slight rollback) at which a stopped rover on a downhill grade "
            + "still holds its downhill pitch lift instead of tilting back up (CTRL-09).")]
        [Range(-3f, 0f)]
        [SerializeField] private float _downhillHoldMinSpeed = -0.5f;

        [Tooltip("Half-life (s) of the pitch lift blending with vertical velocity while airborne over a ridge crest "
            + "(CTRL-09).")]
        [Range(0.05f, 3f)]
        [SerializeField] private float _airborneLiftHalfLife = 0.45f;

        [Tooltip("Reference horizontal speed (m/s) used when converting airborne vertical velocity into pitch lift over "
            + "a crest (CTRL-09).")]
        [Range(0.5f, 15f)]
        [SerializeField] private float _airbornePitchRefSpeed = 4f;

        [Tooltip("Lowest normalized downhill-lift share allowed while airborne (negative pitches the camera slightly "
            + "upward on the rising half of a jump or crest, CTRL-09).")]
        [Range(-1f, 0f)]
        [SerializeField] private float _airborneMinLiftShare = -0.5f;

        [Header("Field of view")]
        [Tooltip("Vertical field of view (deg) at rest.")]
        [Range(20f, 100f)]
        [SerializeField] private float _baseFov = 52f;

        [Tooltip("Extra field of view (deg) at top speed.")]
        [Range(0f, 30f)]
        [SerializeField] private float _speedFovBoost = 3f;

        [Tooltip("Half-life (s) of the FOV change.")]
        [Range(0.05f, 5f)]
        [SerializeField] private float _fovHalfLife = 0.6f;

        [Tooltip("Near clip plane (m). As large as comfortable for depth precision across the huge far range.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _nearClip = 0.3f;

        [Tooltip("Far clip plane (m). The rim backdrop and fog reach ~2.8 km and Earth sits at the far plane, "
            + "so keep it >= 3000.")]
        [Range(3000f, 10000f)]
        [SerializeField] private float _farClip = 3200f;

        [Header("Landing bump")]
        [Tooltip("Camera dip speed (m/s) per m/s of landing impact.")]
        [Range(0f, 1f)]
        [SerializeField] private float _bumpPerImpact = 0.18f;

        [Tooltip("Largest dip speed (m/s), however hard the landing.")]
        [Range(0f, 0.6f)]
        [SerializeField] private float _maxBumpKick = 0.6f;

        [Tooltip("Spring frequency (Hz) of the camera bump.")]
        [Range(0.2f, 6f)]
        [SerializeField] private float _bumpFrequency = 1.4f;

        [Tooltip("Damping ratio of the camera bump (just under 1: one barely visible rebound).")]
        [Range(0.2f, 2f)]
        [SerializeField] private float _bumpDamping = 0.8f;

        [Header("The crawl home (M3-20)")]
        [Tooltip("Extra distance (share of the chase distance) while 07 crawls on its reserve: the frame eases wider.")]
        [Range(0f, 1f)]
        [SerializeField] private float _crawlPullBack = 0.22f;

        [Tooltip("Degrees the chase camera eases lower while 07 crawls, so the horizon and the charger's pillar sit "
            + "in frame.")]
        [Range(0f, 15f)]
        [SerializeField] private float _crawlLower = 4f;

        [Tooltip("Half-life (s) of the crawl framing easing in and out; about three of them (~3 s) settle it.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _crawlHalfLife = 0.9f;

        [Header("Towing a load (opening v3 3.2b)")]
        [Tooltip("Seconds the chase camera takes to rise and draw back when 07 hooks a load, and to settle again when " +
                 "it lets go (eased in and out).")]
        [Range(0.2f, 2f)]
        [SerializeField] private float _towEaseSeconds = 0.6f;

        [Tooltip("Metres of clearance the camera keeps round the load it tows: the load is an obstacle, so the camera " +
                 "rises over it to see 07 and never sits inside it.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _towMargin = 0.5f;

        [Tooltip("Highest (deg) the camera may rise to see 07 over its load.")]
        [Range(20f, 80f)]
        [SerializeField] private float _towMaxElevation = 52f;

        [Tooltip("Furthest the camera may draw back while towing, as a share of the chase distance.")]
        [Range(1f, 3f)]
        [SerializeField] private float _towMaxRadiusScale = 1.8f;

        [Tooltip("Half-life (s) of the solved framing following a swing of the player's view, so it never jumps.")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _towSolveHalfLife = 0.15f;

        [Tooltip("Widest (deg) the camera may swing to a side of the line behind 07 while towing, for a 3/4 rear view that " +
                 "keeps the load and the rope in frame beside 07.")]
        [Range(0f, 80f)]
        [SerializeField] private float _towMaxYawSwing = 45f;

        [Tooltip("Seconds the camera takes to swing round to the side of a load it tows, and to swing back: slower than " +
                 "the rise, so the turn is a gentle drift, never a whip.")]
        [Range(0.6f, 4f)]
        [SerializeField] private float _towYawEaseSeconds = 1.8f;

        [Tooltip("How far the camera's look point moves from 07 toward the load while towing (a share of the way), so the " +
                 "load and the rope stay in frame with 07: a tow reads as 07, the rope and the load together.")]
        [Range(0f, 0.8f)]
        [SerializeField] private float _towLookShare = 0.55f;

        [Header("Camera moments")]
        [Tooltip("Digging: while the excavation beam is on, ease to the side and up so the beam and the rising "
            + "relic are seen beside 07, not hidden behind it. Flows into the relic moment when it surfaces.")]
        [SerializeField] private CameraMomentSettings _digMoment =
            new CameraMomentSettings(1.2f, 0f, 1f, 0.5f, 3f, 0.6f, 35f, 70f, 14f, 0.25f, 15f);

        [Tooltip("A relic finishing surfacing: ease round to frame 07 and the floating relic, then back (~3 s).")]
        [SerializeField] private CameraMomentSettings _relicMoment =
            new CameraMomentSettings(0.9f, 1f, 1.1f, 0.35f, 2.5f, 0.85f, 25f, 60f, 8f, 0.35f, 25f);

        [Tooltip("The radio tower comes online (feel-pass §8.2): ease (~1.2 s) to frame the tower with 07 in the "
            + "foreground, held until it settles; any drive input skips it.")]
        [SerializeField] private CameraMomentSettings _towerMoment =
            new CameraMomentSettings(1.2f, 0.6f, 1.4f, 0.45f, 8f, 0.8f, 10f, 70f, 10f, 0.45f, 120f);

        [Tooltip("The same with the Gentle camera: a smaller, slower turn toward the tower.")]
        [SerializeField] private CameraMomentSettings _gentleTowerMoment =
            new CameraMomentSettings(2f, 0.6f, 2f, 0.3f, 5f, 0.4f, 0f, 35f, 6f, 0.3f, 120f);

        [Tooltip("A relay mast 07 restored lights up: the camera eases low, back and a little to the side, looking up "
            + "past 07 at the mast and its warming lamp against the sky, then returns (~8 s).")]
        [SerializeField] private CameraMomentSettings _relayMoment =
            new CameraMomentSettings(3f, 2.6f, 2.6f, 0.22f, 3f, 0.5f, 26f, 40f, -13f, 0.65f, 40f);

        [Tooltip("The release's view (opening v3 4.4): one slow turn toward relay.1's lamp, the next goal, with 07 kept in " +
                 "the foreground, a hold, then the ease back. The pan below starts where the camera already looks and " +
                 "moves the subject itself, so the share and swing here only say how far the camera follows it; the " +
                 "ease out is the way back (about 6 s in all with the travel and the hold).")]
        [SerializeField] private CameraMomentSettings _releaseMoment =
            new CameraMomentSettings(1.4f, 0f, 2.4f, 1f, 1.2f, 1f, 0f, 120f, -3f, 0f, 100000f);

        [Tooltip("Fastest the release pan turns the camera round (deg/s): a gentle drift, never a whip.")]
        [Range(5f, 40f)]
        [SerializeField] private float _gazePanSweep = 22f;

        [Tooltip("Seconds the pan gives the turn toward relay.1's lamp.")]
        [Range(1f, 4f)]
        [SerializeField] private float _gazePanTravelSeconds = 1.8f;

        [Tooltip("Seconds the pan then holds the lamp while 07's head looks at it.")]
        [Range(1f, 4f)]
        [SerializeField] private float _gazePanHoldSeconds = 2.2f;

        [Tooltip("How far (deg) short of the lamp's bearing the pan settles: the lamp sits on a third of the frame, " +
                 "not its centre.")]
        [Range(0f, 35f)]
        [SerializeField] private float _gazePanSlack = 15f;

        [Tooltip("Furthest (deg) the lamp may lie from where the camera looks for the camera to turn at all; behind that, " +
                 "07's head turn and its chirp carry the look and the camera stays put.")]
        [Range(30f, 180f)]
        [SerializeField] private float _gazePanMaxBearing = 120f;

        [Tooltip("Fastest the release pan tilts toward the lamp's height (deg/s).")]
        [Range(2f, 30f)]
        [SerializeField] private float _gazePanRise = 8f;

        [Tooltip("Half-life (s) of the pan's speed easing: soft starts and stops.")]
        [Range(0.1f, 1.5f)]
        [SerializeField] private float _gazePanHalfLife = 0.5f;

        [Tooltip("The nudge toward the next action's object when it is off screen (G5): one soft eased turn of the camera " +
                 "toward it, with 07 in the foreground; any look or drive input ends it.")]
        [SerializeField] private CameraMomentSettings _nudgeMoment =
            new CameraMomentSettings(1f, 0f, 1.6f, 1f, 1.2f, 1f, 0f, 120f, 0f, 0f, 100000f);

        [Tooltip("Fastest the nudge turns the camera (deg/s).")]
        [Range(5f, 40f)]
        [SerializeField] private float _nudgeSweep = 20f;

        [Tooltip("Seconds the nudge gives the turn toward the object.")]
        [Range(0.8f, 3f)]
        [SerializeField] private float _nudgeTravelSeconds = 1.5f;

        [Tooltip("Seconds the nudge holds the object before easing back.")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _nudgeHoldSeconds = 0.5f;

        [Tooltip("How far (deg) short of the object's bearing the nudge settles: the object comes into the frame's edge, " +
                 "not its centre.")]
        [Range(0f, 50f)]
        [SerializeField] private float _nudgeSlack = 30f;

        [Tooltip("Furthest (deg) the object may lie from where the camera looks for the camera to turn at all; behind that, " +
                 "07's head turn carries the look.")]
        [Range(30f, 180f)]
        [SerializeField] private float _nudgeMaxBearing = 150f;

        [Tooltip("Share of the view's width and height, from each edge, a target must lie inside to count as on screen " +
                 "(no nudge, no head turn).")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _nudgeScreenMargin = 0.08f;

        [Tooltip("07 charging on its pad: the camera eases (calm, about 20 deg/s) to a closer three-quarter rear frame " +
                 "where the lamp bar and the clamps read, instead of pulling out to the wide shot, and eases back when " +
                 "the charge ends. Any look or drive input gives control back at once.")]
        [SerializeField] private CameraMomentSettings _dockMoment =
            new CameraMomentSettings(2.4f, 0f, 2.4f, 0.1f, 0.6f, 1f, 32f, 32f, 5f, -0.35f, 100000f);

        [Tooltip("Metres to 07's side the dock frame looks toward: it sets which side the three-quarter frame sits on.")]
        [Range(1f, 20f)]
        [SerializeField] private float _dockSideMetres = 6f;

        [Tooltip("07's first look up the crew's ladder, then at the lift: the camera eases low and a little back, "
            + "looking up past 07 at the ladder to the deck, follows the gaze across to the lift, and returns once "
            + "the look ends (held while 07 looks).")]
        [SerializeField] private CameraMomentSettings _ladderMoment =
            new CameraMomentSettings(1.6f, 0.8f, 2f, 0.35f, 2.5f, 0.6f, 20f, 50f, -12f, 0.35f, 30f);

        [Tooltip("Seconds a camera moment takes to ease away when the player looks around (always skippable).")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _momentCancelEaseOut = 0.6f;

        [Tooltip("Half-life (s) smoothing a moment's framing, so one moment flowing into the next never jumps.")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _momentBlendHalfLife = 0.3f;

        [Tooltip("Shortest blend (s) into or out of any framed shot: moments, the wide shot and "
            + "the dock's evening.")]
        [Range(1.2f, 4f)]
        [SerializeField] private float _minShotBlend = 1.2f;

        [Header("Gentle camera (the player's setting)")]
        [Tooltip("Shortest blend (s) into or out of any framed shot while the Gentle camera is on.")]
        [Range(2f, 6f)]
        [SerializeField] private float _gentleMinShotBlend = 2f;

        [Tooltip("Speed of the wide shot's slow breathing while the Gentle camera is on (1 = as tuned).")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _gentleBreathRate = 0.5f;

        [Tooltip("Lowest look sensitivity multiplier the settings menu can choose while the Gentle camera is on.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _gentleMinSensitivity = 0.15f;

        [Header("Opening through 07's eye and the title")]
        [Tooltip("The fresh game's wake through 07's own eye (cutscenes §1), the short wake and the title's frames "
            + "(M3-09 Part A).")]
        [SerializeField] private OpeningSettings _opening = new OpeningSettings();

        [Header("Wide shot")]
        [Tooltip("Resting a while: the camera drifts out to a wide, lonely frame with 07 small against the land and "
            + "the sky (VISION pillar 6), and hands back on any drive or look input.")]
        [SerializeField] private WideShotSettings _wideShot = new WideShotSettings();

        [Header("The dock's evening")]
        [Tooltip("07 parked at its dock after a trip: the camera eases out to a wide shot from the dock looking at "
            + "home's newly lit lamps, holds, then eases back (only the timing, frame, lens, ground fit and breathing "
            + "fields apply).")]
        [SerializeField] private WideShotSettings _eveningShot = new WideShotSettings(4f, 3f, 17f, 8f, 0.26f, 6f);

        [Tooltip("Gap (m) the evening camera keeps from solid scenery (the lander, the bay) between it and 07: "
            + "blocked, the frame comes in short of it.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _heldShotClearance = 0.5f;

        [Tooltip("Step (deg) the evening frame turns either way, when scenery blocks the view, to find a clear one.")]
        [Range(5f, 60f)]
        [SerializeField] private float _heldShotYawStep = 25f;

        [Header("Feel: the cut (docs/design/feel-pass.md)")]
        [Tooltip("The charge-up's push-in (0.5.5 §5.6): the fewest stages a piece needs for a fresh charge to earn it "
            + "(3: large and hull plates). Small and medium pieces, warm charges and the Gentle camera never move it.")]
        [Range(1, 3)]
        [SerializeField] private int _cutFrameMinStages = 3;

        [Tooltip("The push-in: how much closer the camera eases (share of its distance: 0.06 = 6%).")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _cutFramePushIn = 0.06f;

        [Tooltip("The push-in: how far (m) lower the camera eases.")]
        [Range(0f, 1f)]
        [SerializeField] private float _cutFrameLower = 0.3f;

        [Tooltip("The push-in turns toward 07's shoulder: how far (m) out to the side the beam runs to the eye looks "
            + "from, so 07's head never hides the beam.")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _cutFrameSide = 1.2f;

        [Tooltip("How far (m) behind 07's emitter, along the beam, the shoulder's eye looks from.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _cutFrameBack = 2.4f;

        [Tooltip("Seconds the push-in takes to ease in over the brace and build (at most 1 s), eased at both ends.")]
        [Range(0.2f, 1f)]
        [SerializeField] private float _cutFrameInSeconds = 1f;

        [Tooltip("Seconds it takes to ease back after the fold or a release.")]
        [Range(0.2f, 4f)]
        [SerializeField] private float _cutFrameOutSeconds = 1.2f;

        [Tooltip("The dig's work framing (feel-pass §3.1): how far (m) above 07's emitter its shoulder camera sits.")]
        [Range(0f, 2f)]
        [SerializeField] private float _digFrameRise = 0.5f;

        [Tooltip("Seconds the dig's framing takes to ease in from its charge-up (07 stands still, so slow is calm).")]
        [Range(0.2f, 4f)]
        [SerializeField] private float _digFrameInSeconds = 1.2f;

        [Tooltip("Seconds the dig's framing takes to ease back after the relic comes up or a release.")]
        [Range(0.2f, 4f)]
        [SerializeField] private float _digFrameOutSeconds = 1f;

        [Tooltip("Gentle camera: the share of the dig framing's side offset and rise it uses.")]
        [Range(0f, 1f)]
        [SerializeField] private float _digFrameGentleShare = 0.5f;

        [Tooltip("Gentle camera: seconds the dig's framing takes to ease in.")]
        [Range(0.2f, 6f)]
        [SerializeField] private float _digFrameGentleSeconds = 2f;

        [Tooltip("Seconds it takes to hand back when the player looks or drives (quick: their choice wins).")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _cutFrameHandBackSeconds = 0.3f;

        [Tooltip("Seconds from the evening shot opening until it eases back (any drive or look input ends it early).")]
        [Range(3f, 30f)]
        [SerializeField] private float _eveningHoldSeconds = 11f;

        [Tooltip("Longest (s) the evening shot stays open while 07 charges on the dock: it holds at least as long " +
                 "as the fill, up to this; a longer fill goes on, visible on the lamps, back in the chase frame.")]
        [Range(3f, 60f)]
        [SerializeField] private float _eveningFillHoldMax = 16f;

        [Header("Cinemachine damping")]
        [Tooltip("Orbital follow position damping (x, y, z): high = floaty drone.")]
        [SerializeField] private Vector3 _positionDamping = new Vector3(1.1f, 0.9f, 1.3f);

        [Tooltip("Share of the position and aim damping kept while the player holds a view of their own (not following "
            + "07): the camera then keeps up with 07, so 07 stays put on screen and the view's heading holds while "
            + "07 drives and turns under it.")]
        [Range(0f, 1f)]
        [SerializeField] private float _freeLookDampingShare = 0.65f;

        [Tooltip("Cap on the share of Cinemachine body and aim damping kept during free-look so the orbital tracker "
            + "and composer stay in step while the remaining damping is applied on the shared follow target (CTRL-08).")]
        [Range(0f, 1f)]
        [SerializeField] private float _freeLookBodyDampingShare = 0.15f;

        [Tooltip("Scale converting the extra free-look damping share into a half-life multiplier on the shared follow "
            + "target position (CTRL-08).")]
        [Range(0f, 1f)]
        [SerializeField] private float _freeLookTargetHalfLifeScale = 0.22f;

        [Tooltip("Half-life (s) of the damping easing between following 07 and a view of the player's own.")]
        [Range(0.05f, 3f)]
        [SerializeField] private float _freeLookDampingHalfLife = 0.4f;

        [Tooltip("Half-life (s) over which Cinemachine position and aim damping fade back in after the wake opening or "
            + "face-inspect mode ends.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _openingDampingFadeHalfLife = 0.25f;

        [Tooltip("Rotation composer damping (horizontal, vertical).")]
        [SerializeField] private Vector2 _aimDamping = new Vector2(0.6f, 0.5f);

        [Tooltip("Where 07 sits on screen (0 = centre, +y = lower), leaving room for the sky and Earth.")]
        [SerializeField] private Vector2 _screenPosition = new Vector2(0f, 0.1f);

        [Header("Terrain avoidance")]
        [Tooltip("Clearance (m) the camera keeps from terrain and rocks.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _cameraRadius = 0.45f;

        [Tooltip("Seconds the camera takes to ease back out after terrain pushed it in.")]
        [Range(0f, 5f)]
        [SerializeField] private float _avoidanceDamping = 0.8f;

        [Tooltip("Seconds the camera takes to move in when terrain blocks the view (small: never inside a hill).")]
        [Range(0f, 2f)]
        [SerializeField] private float _occlusionDamping = 0.25f;

        [Tooltip("Seconds the closest safe camera distance is held, so passing rocks do not make it pump.")]
        [Range(0f, 2f)]
        [SerializeField] private float _occlusionSmoothing = 0.3f;

        [Tooltip("Length (m) of the ray that finds the terrain surface under the camera.")]
        [Range(1f, 50f)]
        [SerializeField] private float _terrainProbe = 12f;

        [Tooltip("Camera distance (m) where wall compression begins lifting the look target over 07's head (CTRL-08).")]
        [Range(1f, 5f)]
        [SerializeField] private float _wallLiftStartDistance = 2.8f;

        [Tooltip("Camera distance (m) where wall compression reaches full target lift over 07's head (CTRL-08).")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _wallLiftFullDistance = 1.4f;

        [Tooltip("Highest vertical lift (m) added to the follow target when a wall pushes the camera into 07 (CTRL-08).")]
        [Range(0f, 1.2f)]
        [SerializeField] private float _wallMaxTargetLift = 0.45f;

        [Tooltip("Extra downward orbit pitch (deg) added when a wall compresses the camera close to 07 (CTRL-08).")]
        [Range(0f, 25f)]
        [SerializeField] private float _wallMaxPitchLift = 10f;

        [Tooltip("Half-life (s) smoothing the wall-compression target lift (CTRL-08).")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _wallLiftHalfLife = 0.16f;

        [Tooltip("Camera distance (m) below which 07's head fades to ShadowsOnly so the camera never clips into the "
            + "head mesh against a wall (CTRL-08).")]
        [Range(0.5f, 3.5f)]
        [SerializeField] private float _headFadeStartDistance = 1.95f;

        [Tooltip("Hysteresis (m) above Head Fade Start Distance before 07's head reappears after wall compression "
            + "(CTRL-08).")]
        [Range(0.02f, 0.5f)]
        [SerializeField] private float _headFadeHysteresis = 0.15f;

        [Header("Face inspect (FACE-INV)")]
        [Tooltip("Opening the face view while 07 still moves waits until its speed (m/s) is below this: 07 brakes " +
                 "smoothly under the hold first, then the camera eases in on a head that no longer sways.")]
        [Range(0.01f, 0.5f)]
        [SerializeField] private float _faceInspectSettleSpeed = 0.05f;

        [Tooltip("Seconds the camera takes to ease into the face-inspect shot when opening the screen UI.")]
        [Range(0.15f, 2f)]
        [SerializeField] private float _faceInspectOpenSeconds = 1.2f;

        [Tooltip("Seconds the camera takes to ease back to the chase camera when closing the screen UI.")]
        [Range(0.15f, 2f)]
        [SerializeField] private float _faceInspectCloseSeconds = 1.1f;

        [Tooltip("World-space height (m) of 07's CRT Screen and inner bezel framed in face-inspect mode.")]
        [Range(0.10f, 0.60f)]
        [SerializeField] private float _faceInspectScreenHeight = 0.3687f;

        [Tooltip("Target share of the viewport height (0..1) filled by 07's CRT Screen in face-inspect mode.")]
        [Range(0.40f, 0.90f)]
        [SerializeField] private float _faceInspectHeightFill = 0.75f;

        [Tooltip("Default camera distance (m) from 07's CRT Screen in face-inspect mode.")]
        [Range(0.35f, 1.50f)]
        [SerializeField] private float _faceInspectDistance = 0.67f;

        [Tooltip("Share of the face shot's distance on the databank page, so the tube nearly fills the frame " +
                 "(docs/design/databank.md §4: 1.08 m to 0.90 m). Under gentle camera the shot crops instead.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float _faceInspectDatabankShare = 0.833f;

        [Tooltip("Seconds the face shot eases between the pages' distances (smooth step). Slower than the page's own " +
                 "flip (DatabankSettings: fade plus draw): the content changes at once, the camera drifts calmly.")]
        [Range(0.05f, 1.5f)]
        [SerializeField] private float _facePageEaseSeconds = 0.9f;

        [Tooltip("Closest pulled-in camera distance (m) allowed when rocks or terrain occlude the default and side views.")]
        [Range(0.25f, 1.00f)]
        [SerializeField] private float _faceInspectMinDistance = 0.42f;

        [Tooltip("Safety floor (m) on the solved face-inspect shot radius so inverse-trig elevation never divides by zero.")]
        [Range(0.01f, 0.25f)]
        [SerializeField] private float _faceInspectMinRadius = 0.05f;

        [Tooltip("Upward head pitch (deg) 07 lifts to meet the face-inspect camera.")]
        [Range(0f, 25f)]
        [SerializeField] private float _faceInspectHeadPitch = 10f;

        [Tooltip("Vertical look-target offset (m) along Screen.up so the camera looks straight at the glass centre.")]
        [Range(-0.10f, 0.10f)]
        [SerializeField] private float _faceInspectLookOffsetY = 0f;

        [Tooltip("Neck yaw offset (deg) tried to the left (-yaw) and right (+yaw) when the default front view is occluded.")]
        [Range(10f, 35f)]
        [SerializeField] private float _faceInspectSideYaw = 28f;

        [Tooltip("Sphere-cast clearance radius (m) used when probing for rocks or terrain in face-inspect mode.")]
        [Range(0.05f, 0.50f)]
        [SerializeField] private float _faceInspectProbeRadius = 0.18f;

        [Tooltip("Camera near clip plane (m) while in face-inspect mode so the head bezel never clips.")]
        [Range(0.02f, 0.25f)]
        [SerializeField] private float _faceInspectNearClip = 0.05f;

        [Tooltip("Share (0..1) of the chassis roll that 07 counter-rolls its head toward level when lifting its head for face inspect.")]
        [Range(0f, 1f)]
        [SerializeField] private float _faceInspectHeadCounterRollShare = 0.5f;

        [Tooltip("Largest head counter-roll (deg) 07 applies toward level when lifting its head for face inspect.")]
        [Range(0f, 20f)]
        [SerializeField] private float _faceInspectHeadCounterRollMax = 8f;

        public float MouseSensitivity => _mouseSensitivity;

        public float StickRate => _stickRate;

        public bool InvertY => _invertY;

        public float MinSensitivity => _minSensitivity;

        public float MaxSensitivity => Mathf.Max(_minSensitivity, _maxSensitivity);

        public float GentleMinSensitivity => Mathf.Min(_gentleMinSensitivity, MaxSensitivity);

        public float MaxLookYaw => _maxLookYaw;

        public float LookYawSoftBand => Mathf.Min(_lookYawSoftBand, _maxLookYaw);

        public float LookHalfLife => _lookHalfLife;

        public float Distance => _distance;

        public float TargetHeight => _targetHeight;

        public float DefaultPitch => Mathf.Clamp(_defaultPitch, MinPitch, MaxPitch);

        public float OpeningPitch => Mathf.Clamp(_openingPitch, MinPitch, MaxPitch);

        public float MinPitch => Mathf.Min(_minPitch, _maxPitch);

        public float MaxPitch => _maxPitch;

        public float LowestCameraHeight => _lowestCameraHeight;

        public float MaxLookUpTilt => _maxLookUpTilt;

        public float WakeYaw => _wakeYaw;

        public float WakeElevation => _wakeElevation;

        public float WakeDistance => _wakeDistance;

        public float WakeScreenY => _wakeScreenY;

        public float StargazeStartElevation => _stargazeStartElevation;

        public float StargazeFullElevation => Mathf.Min(_stargazeFullElevation, _stargazeStartElevation - 1f);

        public float StargazeRiseHalfLife => _stargazeRiseHalfLife;

        public float StargazeFallHalfLife => _stargazeFallHalfLife;

        public float StargazeDelay => _stargazeDelay;

        public float StargazeBeginShare => _stargazeBeginShare;

        public float StargazeHoldShare => Mathf.Min(_stargazeHoldShare, _stargazeBeginShare);

        public float StargazeMaxSpeed => _stargazeMaxSpeed;

        public float RecenterDelay => _recenterDelay;

        public float StickRecenterDelay => _stickRecenterDelay;

        public float RecenterMinSpeed => _recenterMinSpeed;

        public float RecenterFullSpeed => Mathf.Max(_recenterMinSpeed + MinRecenterSpeedSpan, _recenterFullSpeed);

        public float RecenterSlowDelayScale => _recenterSlowDelayScale;

        public float RecenterSlowRateShare => _recenterSlowRateShare;

        public float RecenterDriveSeconds => _recenterDriveSeconds;

        public float RecenterDeadzone => _recenterDeadzone;

        public float RecenterRampTime => _recenterRampTime;

        public float RecenterFadeOut => _recenterFadeOut;

        public float RecenterMaxRate => _recenterMaxRate;

        public float RecenterEaseAngle => _recenterEaseAngle;

        public float RecenterPitchRate => _recenterPitchRate;

        public float RecenterButtonSeconds => _recenterButtonSeconds;

        public float FramingMaxSpeed => _framingMaxSpeed;

        public float FramingRange => _framingRange;

        public float FramingViewCone => _framingViewCone;

        public float FramingEngageDelay => _framingEngageDelay;

        public float FramingLift => _framingLift;

        public float FramingYaw => _framingYaw;

        public float FramingMinRadiusScale => _framingMinRadiusScale;

        public int FramingSearchSteps => _framingSearchSteps;

        public float FramingLookShare => _framingLookShare;

        public float FramingBodyWidth => _framingBodyWidth;

        public float FramingBodyLength => _framingBodyLength;

        public float FramingBodyHeight => _framingBodyHeight;

        public float FramingBodyClearance => _framingBodyClearance;

        public float FramingCentralShare => _framingCentralShare;

        public float FramingSightMargin => _framingSightMargin;

        public float FramingHiddenCost => _framingHiddenCost;

        public float FramingMaxRate => _framingMaxRate;

        public float FramingSolveHalfLife => _framingSolveHalfLife;

        public float FramingRampSeconds => _framingRampSeconds;

        public float FramingReleaseSeconds => _framingReleaseSeconds;

        public float FollowHalfLife => _followHalfLife;

        public float FollowMaxRate => _followMaxRate;

        public float DownhillLift => _downhillLift;

        public float DownhillFullAngle => _downhillFullAngle;

        public float LiftHalfLife => _liftHalfLife;

        public float DownhillSlopeMinAngle => _downhillSlopeMinAngle;

        public float StoppedDownhillShare => _stoppedDownhillShare;

        public float DownhillHoldMinSpeed => _downhillHoldMinSpeed;

        public float AirborneLiftHalfLife => _airborneLiftHalfLife;

        public float AirbornePitchRefSpeed => _airbornePitchRefSpeed;

        public float AirborneMinLiftShare => _airborneMinLiftShare;

        public float BaseFov => _baseFov;

        public float SpeedFovBoost => _speedFovBoost;

        public float FovHalfLife => _fovHalfLife;

        public float NearClip => _nearClip;

        public float FarClip => _farClip;

        public float BumpPerImpact => _bumpPerImpact;

        public float MaxBumpKick => _maxBumpKick;

        public float BumpFrequency => _bumpFrequency;

        public float BumpDamping => _bumpDamping;

        public float CrawlPullBack => _crawlPullBack;

        public float CrawlLower => _crawlLower;

        public float CrawlHalfLife => _crawlHalfLife;

        public float TowEaseSeconds => _towEaseSeconds;

        public float TowMargin => _towMargin;

        public float TowMaxElevation => _towMaxElevation;

        public float TowMaxRadiusScale => _towMaxRadiusScale;

        public float TowSolveHalfLife => _towSolveHalfLife;

        public float TowLookShare => _towLookShare;

        public float TowMaxYawSwing => _towMaxYawSwing;

        public float TowYawEaseSeconds => _towYawEaseSeconds;

        public CameraMomentSettings DigMoment => _digMoment;

        public CameraMomentSettings RelicMoment => _relicMoment;

        public CameraMomentSettings TowerMoment => _towerMoment;

        public CameraMomentSettings GentleTowerMoment => _gentleTowerMoment;

        public CameraMomentSettings RelayMoment => _relayMoment;

        public CameraMomentSettings ReleaseMoment => _releaseMoment;

        public float ReleasePanSweep => _gazePanSweep;

        public float ReleasePanTravelSeconds => _gazePanTravelSeconds;

        public float ReleasePanHoldSeconds => _gazePanHoldSeconds;

        public float ReleasePanSlack => _gazePanSlack;

        public float ReleasePanMaxBearing => _gazePanMaxBearing;

        public float ReleasePanRise => _gazePanRise;

        public float ReleasePanHalfLife => _gazePanHalfLife;

        public CameraMomentSettings NudgeMoment => _nudgeMoment;

        public float NudgeSweep => _nudgeSweep;

        public float NudgeTravelSeconds => _nudgeTravelSeconds;

        public float NudgeHoldSeconds => _nudgeHoldSeconds;

        public float NudgeSlack => _nudgeSlack;

        public float NudgeMaxBearing => _nudgeMaxBearing;

        public float NudgeScreenMargin => _nudgeScreenMargin;

        public CameraMomentSettings DockMoment => _dockMoment;

        public float DockSideMetres => _dockSideMetres;

        public CameraMomentSettings LadderMoment => _ladderMoment;

        public float MomentCancelEaseOut => _momentCancelEaseOut;

        public float MomentBlendHalfLife => _momentBlendHalfLife;

        public float MinShotBlend => _minShotBlend;

        public float GentleMinShotBlend => Mathf.Max(_gentleMinShotBlend, _minShotBlend);

        public float GentleBreathRate => _gentleBreathRate;

        public WideShotSettings WideShot => _wideShot;

        public WideShotSettings EveningShot => _eveningShot;

        public float EveningHoldSeconds => _eveningHoldSeconds;

        public float EveningFillHoldMax => _eveningFillHoldMax;

        public float HeldShotClearance => _heldShotClearance;

        public float HeldShotYawStep => _heldShotYawStep;

        public float CutFrameSide => _cutFrameSide;

        public float CutFrameBack => _cutFrameBack;

        public int CutFrameMinStages => _cutFrameMinStages;

        public float CutFramePushIn => _cutFramePushIn;

        public float CutFrameLower => _cutFrameLower;

        public float CutFrameInSeconds => _cutFrameInSeconds;

        public float CutFrameOutSeconds => _cutFrameOutSeconds;

        public float CutFrameHandBackSeconds => _cutFrameHandBackSeconds;

        public float DigFrameRise => _digFrameRise;

        public float DigFrameInSeconds => _digFrameInSeconds;

        public float DigFrameOutSeconds => _digFrameOutSeconds;

        public float DigFrameGentleShare => _digFrameGentleShare;

        public float DigFrameGentleSeconds => _digFrameGentleSeconds;

        public Vector3 PositionDamping => _positionDamping;

        public float FreeLookDampingShare => _freeLookDampingShare;

        public float FreeLookBodyDampingShare => _freeLookBodyDampingShare;

        public float FreeLookTargetHalfLifeScale => _freeLookTargetHalfLifeScale;

        public float FreeLookDampingHalfLife => _freeLookDampingHalfLife;

        public float OpeningDampingFadeHalfLife => _openingDampingFadeHalfLife;

        public Vector2 AimDamping => _aimDamping;

        public Vector2 ScreenPosition => _screenPosition;

        public float CameraRadius => _cameraRadius;

        public float AvoidanceDamping => _avoidanceDamping;

        public float OcclusionDamping => _occlusionDamping;

        public float OcclusionSmoothing => _occlusionSmoothing;

        public float TerrainProbe => _terrainProbe;

        public float WallLiftStartDistance => _wallLiftStartDistance;

        public float WallLiftFullDistance => _wallLiftFullDistance;

        public float WallMaxTargetLift => _wallMaxTargetLift;

        public float WallMaxPitchLift => _wallMaxPitchLift;

        public float WallLiftHalfLife => _wallLiftHalfLife;

        public float HeadFadeStartDistance => _headFadeStartDistance;

        public float HeadFadeHysteresis => _headFadeHysteresis;

        public OpeningSettings Opening => _opening;

        public float FaceInspectOpenSeconds => _faceInspectOpenSeconds;

        public float FaceInspectSettleSpeed => _faceInspectSettleSpeed;

        public float FaceInspectCloseSeconds => _faceInspectCloseSeconds;

        public float FaceInspectScreenHeight => _faceInspectScreenHeight;

        public float FaceInspectHeightFill => _faceInspectHeightFill;

        public float FaceInspectDistance => _faceInspectDistance;

        public float FaceInspectMinDistance => _faceInspectMinDistance;

        public float FaceInspectDatabankShare => _faceInspectDatabankShare;

        public float FacePageEaseSeconds => _facePageEaseSeconds;

        public float FaceInspectMinRadius => _faceInspectMinRadius;

        public float FaceInspectHeadPitch => _faceInspectHeadPitch;

        public float FaceInspectLookOffsetY => _faceInspectLookOffsetY;

        public float FaceInspectSideYaw => _faceInspectSideYaw;

        public float FaceInspectProbeRadius => _faceInspectProbeRadius;

        public float FaceInspectNearClip => _faceInspectNearClip;

        public float FaceInspectHeadCounterRollShare => _faceInspectHeadCounterRollShare;

        public float FaceInspectHeadCounterRollMax => _faceInspectHeadCounterRollMax;
    }
}
