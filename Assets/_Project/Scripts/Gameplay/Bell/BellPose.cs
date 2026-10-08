namespace MoonProject.Gameplay
{
    /// <summary>
    /// Bell's animation on top of her rig's rest pose for one frame (angles in degrees, offsets in metres; see the
    /// Bell rig contract, docs/ARCHITECTURE.md). Legs are indexed FL, FR, RL, RR. A plain value, no allocation.
    /// </summary>
    public struct BellPose
    {
        public const int Legs = 4;
        public const int FrontLeft = 0;
        public const int FrontRight = 1;
        public const int RearLeft = 2;
        public const int RearRight = 3;

        private float _legFrontLeft;
        private float _legFrontRight;
        private float _legRearLeft;
        private float _legRearRight;
        private float _shinFrontLeft;
        private float _shinFrontRight;
        private float _shinRearLeft;
        private float _shinRearRight;

        /// <summary>Body and hips up (m).</summary>
        public float Bob { get; set; }

        /// <summary>Body roll about its forward axis (sway, waddle).</summary>
        public float Roll { get; set; }

        /// <summary>Body pitch about its side axis (positive tips the dial down).</summary>
        public float Pitch { get; set; }

        /// <summary>Whole-body turn about up, on top of where she faces (watching 07 park).</summary>
        public float Turn { get; set; }

        /// <summary>Lid about its hinge: 0 closed, negative lifts it (a brow lift).</summary>
        public float Lid { get; set; }

        /// <summary>Needle about the dial hub: 0 the band's left end, positive sweeps right.</summary>
        public float Needle { get; set; }

        /// <summary>Tuning knob about its local Z, turning with the needle's detents (07's beam taps it).</summary>
        public float Knob { get; set; }

        /// <summary>Dial lamp glow (1 reads warm amber).</summary>
        public float DialLamp { get; set; }

        /// <summary>Speaker cone push along its axis (m).</summary>
        public float Speaker { get; set; }

        /// <summary>Hip swing of leg <paramref name="leg"/> about its side axis (negative swings it forward).</summary>
        public float Leg(int leg)
        {
            switch (leg)
            {
                case FrontLeft:
                    return _legFrontLeft;
                case FrontRight:
                    return _legFrontRight;
                case RearLeft:
                    return _legRearLeft;
                default:
                    return _legRearRight;
            }
        }

        /// <summary>Knee bend of leg <paramref name="leg"/> (positive folds the shin back).</summary>
        public float Shin(int leg)
        {
            switch (leg)
            {
                case FrontLeft:
                    return _shinFrontLeft;
                case FrontRight:
                    return _shinFrontRight;
                case RearLeft:
                    return _shinRearLeft;
                default:
                    return _shinRearRight;
            }
        }

        public void SetLeg(int leg, float swing, float bend)
        {
            switch (leg)
            {
                case FrontLeft:
                    _legFrontLeft = swing;
                    _shinFrontLeft = bend;
                    break;
                case FrontRight:
                    _legFrontRight = swing;
                    _shinFrontRight = bend;
                    break;
                case RearLeft:
                    _legRearLeft = swing;
                    _shinRearLeft = bend;
                    break;
                default:
                    _legRearRight = swing;
                    _shinRearRight = bend;
                    break;
            }
        }

        /// <summary>Adds to leg <paramref name="leg"/>'s swing and bend.</summary>
        public void AddLeg(int leg, float swing, float bend)
        {
            SetLeg(leg, Leg(leg) + swing, Shin(leg) + bend);
        }
    }
}
