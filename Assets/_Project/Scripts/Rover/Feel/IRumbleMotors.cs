namespace MoonProject.Rover
{
    /// <summary>The gamepad's two rumble motors (a fake in tests).</summary>
    public interface IRumbleMotors
    {
        /// <summary>Whether a pad that can rumble is connected now.</summary>
        bool IsConnected { get; }

        /// <summary>Sets both motors (0..1); a no-op without a pad.</summary>
        void Set(float low, float high);
    }
}
