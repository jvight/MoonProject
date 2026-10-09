using UnityEngine;

namespace MoonProject.Core.Events
{
    /// <summary>The rover touched down after being airborne. Drives landing audio, dust puffs and camera bump.</summary>
    public readonly struct RoverLanded
    {
        public RoverLanded(Vector3 position, float impactSpeed, float airTime)
        {
            Position = position;
            ImpactSpeed = impactSpeed;
            AirTime = airTime;
        }

        public Vector3 Position { get; }

        /// <summary>Downward speed at touchdown in m/s (always positive).</summary>
        public float ImpactSpeed { get; }

        /// <summary>Seconds spent airborne before this landing.</summary>
        public float AirTime { get; }
    }

    /// <summary>
    /// 07 began waking up at the start of the session (eye opening, design ruling 8): the moment the radio crackles on.
    /// </summary>
    public readonly struct RoverAwoke
    {
        public RoverAwoke(Vector3 position, bool wokenByPlayer)
        {
            Position = position;
            WokenByPlayer = wokenByPlayer;
        }

        public Vector3 Position { get; }

        /// <summary>True when the player drove before 07 woke on its own (the wake-up is quick, not slow).</summary>
        public bool WokenByPlayer { get; }
    }

    /// <summary>
    /// 07 got stuck and is being lifted gently to a nearby open spot (design ruling 7). The lift is a continuous arc
    /// from <see cref="From"/> to <see cref="To"/> lasting <see cref="Duration"/> seconds; audio or UI may soften it.
    /// </summary>
    public readonly struct RoverRecovering
    {
        public RoverRecovering(Vector3 from, Vector3 to, float duration)
        {
            From = from;
            To = to;
            Duration = duration;
        }

        public Vector3 From { get; }

        public Vector3 To { get; }

        public float Duration { get; }
    }

    /// <summary>
    /// The Hover-Jump charge grew: published as charging starts (strength 0) and at each equal step up to a full charge
    /// (1); with the default four steps that is five notes, ready for a pentatonic climb. Drives the charge hum.
    /// </summary>
    public readonly struct RoverJumpCharged
    {
        public RoverJumpCharged(float strength)
        {
            Strength = strength;
        }

        /// <summary>Charge so far, 0..1.</summary>
        public float Strength { get; }
    }

    /// <summary>07 leapt with the Hover-Jump. Its landing is announced by <see cref="RoverLanded"/> as usual.</summary>
    public readonly struct RoverJumped
    {
        public RoverJumped(float strength)
        {
            Strength = strength;
        }

        /// <summary>0 for a tap (small hop) .. 1 for a full charge (the big leap).</summary>
        public float Strength { get; }
    }

    /// <summary>
    /// A Hover-Jump charge ended without a leap: 07 left the ground, the ability went away or gameplay held it still.
    /// </summary>
    public readonly struct RoverJumpCancelled
    {
    }

    /// <summary>A piece of 07's visible kit or a friend's gift on 07 (docs/features/M3-11, VISION ruling 11).</summary>
    public enum RoverKitPiece
    {
        /// <summary>The Hover-Jump's coils under the belly.</summary>
        HoverCoils = 0,

        /// <summary>The Warm Headlamp's caged lamp bar across the front.</summary>
        LampBar = 1,

        /// <summary>The Boost Coils' twin capacitor drums on the flanks.</summary>
        CapacitorDrums = 2,

        /// <summary>The Cargo Cradle's strapped rear rack.</summary>
        CargoRack = 3,

        /// <summary>Tilly's gift: the solar wing's missing cell replaced.</summary>
        SolarCell = 4,

        /// <summary>Bell's gift: the "07" freshly stencilled and a radio pennant on the antenna.</summary>
        FreshPaint = 5,
    }

    /// <summary>
    /// A kit piece is about to be fitted on 07 after a purchase (<see cref="Gift"/> false), or a friend's gift is about
    /// to appear as 07 comes home (true): the install moment begins and the camera eases round to look. A loaded game
    /// shows its kit silently, without this.
    /// </summary>
    public readonly struct RoverKitInstalling
    {
        public RoverKitInstalling(RoverKitPiece piece, bool gift)
        {
            Piece = piece;
            Gift = gift;
        }

        public RoverKitPiece Piece { get; }

        /// <summary>True for a friend's gift: the soft version of the moment.</summary>
        public bool Gift { get; }
    }

    /// <summary>
    /// The piece is on 07: a crafted piece set on its socket by the Rover Bay's arm (the "fitted" clunk; 07 strikes its
    /// proud pose as the turntable shows it), or a friend's gift fully there.
    /// </summary>
    public readonly struct RoverKitFitted
    {
        public RoverKitFitted(RoverKitPiece piece, bool gift, string upgradeId)
        {
            Piece = piece;
            Gift = gift;
            UpgradeId = upgradeId ?? string.Empty;
        }

        public RoverKitPiece Piece { get; }

        /// <summary>True for a friend's gift.</summary>
        public bool Gift { get; }

        /// <summary>
        /// The upgrade that brought a crafted piece (UI names it by this, "upgrade.&lt;id&gt;.name"); empty for a gift.
        /// </summary>
        public string UpgradeId { get; }
    }

    /// <summary>
    /// The Boost Coils engaged (<see cref="Boosting"/> true: holding Drive at cruise on open, flat-ish ground) or let
    /// go. The boost itself eases in and out over about a second; listeners ease their own response (the hum).
    /// </summary>
    public readonly struct RoverBoostChanged
    {
        public RoverBoostChanged(bool boosting)
        {
            Boosting = boosting;
        }

        public bool Boosting { get; }
    }

    /// <summary>
    /// 07 was set down somewhere else in one step (<see cref="IRoverPlacement.PlaceAt"/>, e.g. a radio-hop while the
    /// view is dark): the camera and anything that follows 07 snap with it instead of easing across.
    /// </summary>
    public readonly struct RoverPlaced
    {
        public RoverPlaced(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        /// <summary>The ground point 07 now rests on.</summary>
        public Vector3 Position { get; }

        /// <summary>07's heading (yaw only).</summary>
        public Quaternion Rotation { get; }
    }

    /// <summary>
    /// The camera began its slow drift out to the lonely wide shot after 07 rested a while (<see cref="Wide"/> true;
    /// VISION pillar 6), or handed the view back because the player drove, looked around or something began
    /// (false). 07 sighs as the frame opens; anything else that wants to breathe out with it may listen.
    /// </summary>
    public readonly struct RoverWideShotChanged
    {
        public RoverWideShotChanged(bool wide)
        {
            Wide = wide;
        }

        /// <summary>True as the frame starts to open, false as it starts to come back to the chase view.</summary>
        public bool Wide { get; }
    }

    /// <summary>
    /// The stargazing beat began or ended: 07 rests while the player looks up at the sky. UI fades the HUD and audio
    /// thins the music while it is on; any drive input ends it.
    /// </summary>
    public readonly struct StargazingChanged
    {
        public StargazingChanged(bool isStargazing)
        {
            IsStargazing = isStargazing;
        }

        public bool IsStargazing { get; }
    }
}
