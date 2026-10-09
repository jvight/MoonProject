namespace MoonProject.Gameplay
{
    /// <summary>
    /// When 07 rests on the charging dock (docs/features/M3-14): once it has stood still on the dock, with no drive
    /// input, for a moment. Never forced: any drive input, or being moved off the dock, ends the rest at once. Pure
    /// logic: distance, speed and input in, rest out.
    /// </summary>
    public sealed class DockRest
    {
        private readonly float _radius;
        private readonly float _maxSpeed;
        private readonly float _delay;
        private readonly float _deadZone;

        /// <param name="radius">Metres from the dock's anchor 07 counts as on the dock.</param>
        /// <param name="maxSpeed">Speed (m/s) below which 07 counts as stopped.</param>
        /// <param name="delay">Seconds of stillness before it rests.</param>
        /// <param name="deadZone">Drive input below this counts as none.</param>
        public DockRest(float radius, float maxSpeed, float delay, float deadZone)
        {
            _radius = radius;
            _maxSpeed = maxSpeed;
            _delay = delay;
            _deadZone = deadZone;
        }

        public bool Docked { get; private set; }

        /// <summary>Seconds 07 has stood still on the dock so far (0 once it rests or moves).</summary>
        public float StillFor { get; private set; }

        /// <param name="distance">Horizontal metres from 07 to the dock's anchor.</param>
        /// <param name="speed">07's speed (m/s).</param>
        /// <param name="input">Size of 07's drive input (0..1).</param>
        /// <returns>True when <see cref="Docked"/> changed this step.</returns>
        public bool Step(float distance, float speed, float input, float deltaTime)
        {
            bool onDock = distance <= _radius;
            bool driving = input > _deadZone;
            if (Docked)
            {
                if (!driving && onDock)
                {
                    return false;
                }

                Docked = false;
                return true;
            }

            if (driving || !onDock || speed > _maxSpeed)
            {
                StillFor = 0f;
                return false;
            }

            StillFor += deltaTime;
            if (StillFor < _delay)
            {
                return false;
            }

            StillFor = 0f;
            Docked = true;
            return true;
        }
    }
}
