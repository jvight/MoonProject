using MoonProject.Core;

namespace MoonProject.App
{
    /// <summary>
    /// New Game over a save (docs/design/cutscenes.md §1.7): the scene reload that puts the save away waits until the
    /// camera has pushed into 07's closed lens (<see cref="WakeBeat.PushedIn"/>), so it happens inside O1's black and
    /// there is never a loading screen. If the beat never comes, it gives up waiting after a while
    /// (<see cref="TimedOut"/>) so the reload is never lost.
    /// </summary>
    public sealed class PushedInGate
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
            if (!IsWaiting || beat != WakeBeat.PushedIn)
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
