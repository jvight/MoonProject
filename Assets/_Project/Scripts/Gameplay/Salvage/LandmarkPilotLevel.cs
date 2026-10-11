using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The glow of a site's tall landmark pilot (M3-18 L1): dark until the site's heart is found, then it eases up to
    /// a dim warm light and stays on. The landmark marks a known place, so picking the site clean never dims it.
    /// </summary>
    public sealed class LandmarkPilotLevel
    {
        private readonly float _lit;
        private readonly float _ease;
        private bool _found;

        /// <param name="lit">Glow once lit (a multiple of the authored emission).</param>
        /// <param name="ease">Time constant (s) of the rise.</param>
        public LandmarkPilotLevel(float lit, float ease)
        {
            if (lit < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(lit), lit, "The lit glow cannot be negative.");
            }

            _lit = lit;
            _ease = ease;
        }

        public float Level { get; private set; }

        /// <summary>True once the heart has been found; it never goes back.</summary>
        public bool Found => _found;

        /// <summary>Eases toward the lit glow once <paramref name="heartFound"/>; returns the new level.</summary>
        public float Step(bool heartFound, float deltaTime)
        {
            _found |= heartFound;
            Level = _found ? Damp.Toward(Level, _lit, _ease, deltaTime) : 0f;
            return Level;
        }

        /// <summary>Jumps to the settled state (a loaded save shows a known place at once); returns the level.</summary>
        public float Snap(bool heartFound)
        {
            _found |= heartFound;
            Level = _found ? _lit : 0f;
            return Level;
        }
    }
}
