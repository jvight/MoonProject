namespace MoonProject.Audio
{
    /// <summary>
    /// The radio's level around the title over a save (docs/features/M3-09 "The frames"): the save's station plays
    /// softly under the menu, rises to full as Continue eases into the chase frame, and falls silent as New Game puts
    /// the save away under the push into the lens. Outside that title it stays at 1 and changes nothing.
    /// </summary>
    public sealed class TitleRadio
    {
        private readonly EasedValue _level = new EasedValue(1f);
        private float _target = 1f;

        /// <summary>True while the title over a save holds the station at its soft level.</summary>
        public bool HoldsSave { get; private set; }

        /// <summary>The radio's level multiplier 0..1.</summary>
        public float Level => _level.Value;

        /// <summary>The title over a save came up: the station rises from silence to <paramref name="soft"/>.</summary>
        public void ShowOverSave(float soft)
        {
            HoldsSave = true;
            _target = soft;
            _level.Snap(0f);
        }

        /// <summary>A session started (Continue, or play without a title): the station rises to full.</summary>
        public void StartSession()
        {
            HoldsSave = false;
            _target = 1f;
        }

        /// <summary>New Game over the title's save: the station falls silent. Ignored anywhere else.</summary>
        public void PutAway()
        {
            if (!HoldsSave)
            {
                return;
            }

            HoldsSave = false;
            _target = 0f;
        }

        public float Step(float deltaTime, float riseTime, float fallTime)
        {
            return _level.Step(_target, deltaTime, riseTime, fallTime);
        }
    }
}
