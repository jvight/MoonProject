namespace MoonProject.Gameplay
{
    /// <summary>
    /// When the tether lets go on its own: the relic is caught (it hangs well beyond the tether length for a moment,
    /// e.g. wedged behind a rock while 07 drives on) or it is simply far too far away. Pure state machine.
    /// </summary>
    public sealed class TetherSnapRule
    {
        private readonly float _stretchLimit;
        private readonly float _grace;
        private readonly float _distanceLimit;
        private float _stuckTime;

        public TetherSnapRule(float stretchLimit, float grace, float distanceLimit)
        {
            _stretchLimit = stretchLimit;
            _grace = grace;
            _distanceLimit = distanceLimit;
        }

        /// <summary>0..1: how close the tether is to letting go (beam wobble, UI reticle).</summary>
        public float Strain { get; private set; }

        public void Reset()
        {
            _stuckTime = 0f;
            Strain = 0f;
        }

        /// <summary>Returns true when the tether should let go now.</summary>
        public bool Step(float distance, float length, float deltaTime)
        {
            float stretch = distance - length;
            Strain = _stretchLimit <= 0f ? 0f : UnityEngine.Mathf.Clamp01(stretch / _stretchLimit);
            if (distance > _distanceLimit)
            {
                return true;
            }

            _stuckTime = stretch > _stretchLimit ? _stuckTime + deltaTime : 0f;
            return _stuckTime > _grace;
        }
    }
}
