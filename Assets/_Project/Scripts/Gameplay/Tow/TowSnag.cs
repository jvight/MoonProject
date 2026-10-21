namespace MoonProject.Gameplay
{
    /// <summary>
    /// When a towed load is caught (a lip, a rock's edge, a prop's corner): the cable has stayed stretched past the
    /// snag stretch for the snag time while the load hardly moved toward the winch (a load that only lags, still
    /// coming, is never caught). The winch then lifts it a little and pays out slack, and the count starts again; the
    /// hook stays on, so backing up or steering wide frees it. Only a load past the snap distance (07 pulled far past
    /// all its slack) lets go. Pure state machine.
    /// </summary>
    public sealed class TowSnag
    {
        private float _caughtTime;

        /// <summary>Times the winch has lifted the load free on this tow.</summary>
        public int Lifts { get; private set; }

        public void Reset()
        {
            _caughtTime = 0f;
            Lifts = 0;
        }

        /// <summary>
        /// One physics step at <paramref name="distance"/> metres from the winch, the load coming toward it at
        /// <paramref name="coming"/> m/s: what the winch should do.
        /// </summary>
        public TowSnagAction Step(float distance, float length, float coming, float deltaTime, TowSettings settings)
        {
            if (distance > settings.LetGoDistance)
            {
                return TowSnagAction.LetGo;
            }

            bool caught = distance - length > settings.SnagStretch && coming < settings.SnagSpeed;
            _caughtTime = caught ? _caughtTime + deltaTime : 0f;
            if (_caughtTime < settings.SnagSeconds)
            {
                return TowSnagAction.None;
            }

            _caughtTime = 0f;
            Lifts++;
            return TowSnagAction.LiftFree;
        }
    }
}
