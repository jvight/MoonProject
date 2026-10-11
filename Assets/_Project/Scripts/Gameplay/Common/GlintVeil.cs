using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The pickup glints step aside while the title is up (docs/features/M3-09): the title frames home under a soft
    /// depth of field, which blurs any glint near its camera into a big blue disc, and the title is the player's home,
    /// not a hunt. Hidden from <see cref="Core.Events.TitleShown"/> until the session starts, then they ease back in
    /// over the reveal time. One veil, shared through <see cref="GameplayServices"/> by every
    /// <see cref="PickupGlints"/>.
    /// </summary>
    public sealed class GlintVeil
    {
        private bool _hidden;
        private float _shownAt = float.NegativeInfinity;

        /// <summary>The title came up: every glint goes at once (they were never seen behind the black).</summary>
        public void Hide()
        {
            _hidden = true;
        }

        /// <summary>Play began at <paramref name="time"/>: the glints ease back in from there.</summary>
        public void Show(float time)
        {
            if (!_hidden)
            {
                return;
            }

            _hidden = false;
            _shownAt = time;
        }

        /// <summary>The glints' strength 0..1 at <paramref name="time"/>.</summary>
        public float Level(float time, float revealSeconds)
        {
            if (_hidden)
            {
                return 0f;
            }

            float t = Mathf.Clamp01((time - _shownAt) / Mathf.Max(revealSeconds, Mathf.Epsilon));
            return t * t * (3f - 2f * t);
        }
    }
}
