namespace MoonProject.Audio
{
    /// <summary>
    /// The radio during a radio-hop (M3-06): from <see cref="Begin"/> the set eases into static over the out time
    /// (music ducked and filtered away as the screen fades) and holds there while 07 is moved; from
    /// <see cref="Finish"/> it eases back over the in time, resolving into whatever clarity the target node has.
    /// A finish before the out ramp completes turns back from where it is, never jumping. Allocation-free.
    /// </summary>
    public sealed class RadioHop
    {
        private readonly LoopFader _fade = new LoopFader();

        /// <summary>0 normal radio .. 1 all static (smoothed at both ends).</summary>
        public float Amount => _fade.Gain;

        /// <summary>True from <see cref="Begin"/> until the radio has resolved after <see cref="Finish"/>.</summary>
        public bool Active => _fade.IsOn || _fade.IsAudible;

        public void Begin()
        {
            _fade.FadeIn();
        }

        public void Finish()
        {
            _fade.FadeOut();
        }

        /// <summary>Advances the hop; returns <see cref="Amount"/>.</summary>
        public float Step(float deltaTime, float outTime, float inTime)
        {
            _fade.Step(deltaTime, outTime, inTime);
            return Amount;
        }
    }
}
