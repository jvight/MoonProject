using MoonProject.Core;

namespace MoonProject.App
{
    /// <summary>
    /// New Game over a save (docs/design/cutscenes.md §1.7): the scene reload that puts the save away waits until the
    /// picture has faded to black (<see cref="WakeBeat.FadedOut"/>), so it happens inside the black before O1 and
    /// there is never a loading screen. If the beat never comes, it gives up waiting after a while
    /// (<see cref="TimedOut"/>) so the reload is never lost.
    /// </summary>
    public sealed class FadedOutGate
    {
        private float _waited;

        /// <summary>True from <see cref="Arm"/> until the gate opens.</summary>
        public bool IsWaiting { get; private set; }

        /// <summary>True when the gate opened on the timeout rather than on the beat.</summary>
        public bool TimedOut { get; private set; }

        public void Arm()
        {
            IsWaiting = true;
            TimedOut = false;
            _waited = 0f;
        }

        /// <summary>Returns true once, when <paramref name="beat"/> opens the armed gate.</summary>
        public bool Notify(WakeBeat beat)
        {
            if (!IsWaiting || beat != WakeBeat.FadedOut)
            {
                return false;
            }

            IsWaiting = false;
            return true;
        }

        /// <summary>Returns true once, when the armed gate has waited <paramref name="timeout"/> seconds.</summary>
        public bool Step(float deltaTime, float timeout)
        {
            if (!IsWaiting)
            {
                return false;
            }

            _waited += deltaTime;
            if (_waited < timeout)
            {
                return false;
            }

            IsWaiting = false;
            TimedOut = true;
            return true;
        }
    }
}
