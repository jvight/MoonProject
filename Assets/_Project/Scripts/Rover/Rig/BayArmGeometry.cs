namespace MoonProject.Rover
{
    /// <summary>
    /// One Rover Bay gantry arm's link lengths (docs/ARCHITECTURE.md, "Contract: Kenji's Rover Bay"): shoulder to
    /// elbow, elbow to wrist, wrist to the tip's end, each hanging along the joint's local -Y at zero pitch.
    /// </summary>
    public readonly struct BayArmGeometry
    {
        public BayArmGeometry(float upper, float lower, float tip)
        {
            Upper = upper;
            Lower = lower;
            Tip = tip;
        }

        /// <summary>Shoulder (Upper) to elbow (Lower), metres.</summary>
        public float Upper { get; }

        /// <summary>Elbow (Lower) to wrist (Tip), metres.</summary>
        public float Lower { get; }

        /// <summary>Wrist (Tip) to the end that holds the piece, metres.</summary>
        public float Tip { get; }

        /// <summary>Farthest the wrist can be from the shoulder.</summary>
        public float Reach => Upper + Lower;
    }
}
