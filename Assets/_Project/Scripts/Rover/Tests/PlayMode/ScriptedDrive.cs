using UnityEngine;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// Holds 07's wheel in feel sessions, exactly like a stick held steady: no virtual devices, no input-system state,
    /// so the sessions behave the same alone, in the full suite and in the interactive editor. Clamped to the unit
    /// circle like <c>InputReader.Drive</c>, so (1, 1) arrives as a normalised keyboard diagonal does.
    /// </summary>
    public sealed class ScriptedDrive : IRoverDriveSource
    {
        private Vector2 _drive;

        public Vector2 Drive
        {
            get => _drive;
            set => _drive = Vector2.ClampMagnitude(value, 1f);
        }
    }
}
