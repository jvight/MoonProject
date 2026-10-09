namespace MoonProject.Rover
{
    /// <summary>
    /// Joint angles of one Rover Bay gantry arm, degrees: the shoulder's turn about its local Y, then the local X
    /// pitches of Upper, Lower and Tip, each relative to its parent (as written to the joints' local rotations).
    /// </summary>
    public readonly struct BayArmPose
    {
        public BayArmPose(float yaw, float upper, float lower, float tip)
        {
            Yaw = yaw;
            Upper = upper;
            Lower = lower;
            Tip = tip;
        }

        public float Yaw { get; }

        public float Upper { get; }

        public float Lower { get; }

        public float Tip { get; }
    }
}
