namespace MoonProject.Autoplay
{
    /// <summary>
    /// Turns the game's state flags into a frame's <see cref="FrameTag"/>s and names the face screen's open and close
    /// windows ("Tab open #1"): each flip of the screen starts a transition window. Allocates only on a flip.
    /// </summary>
    public sealed class FrameTagger
    {
        /// <summary>Seconds after the face screen flips that count as its opening or closing transition.</summary>
        public const float TransitionSeconds = 1.5f;

        private bool _face;
        private float _transitionEnd;
        private int _opens;
        private int _closes;
        private bool _databank;
        private int _flipsToDatabank;
        private int _flipsBack;
        private string _flipName = string.Empty;

        /// <summary>The open or close window the last step fell in ("Tab open #2"), or empty outside one.</summary>
        public string Window { get; private set; } = string.Empty;

        /// <param name="databank">The face screen is on its databank page (a flip of it starts a window too).</param>
        public FrameTag Step(float now, bool faceOpen, bool building, bool scanning, bool docked,
            bool databank = false)
        {
            if (databank != _databank)
            {
                _databank = databank;
                _transitionEnd = now + TransitionSeconds;
                _flipName = databank ? "Databank flip #" + ++_flipsToDatabank : "Hold flip #" + ++_flipsBack;
            }

            if (faceOpen != _face)
            {
                _face = faceOpen;
                _transitionEnd = now + TransitionSeconds;
                _flipName = faceOpen ? "Tab open #" + ++_opens : "Tab close #" + ++_closes;
            }

            FrameTag tags = FrameTag.None;
            bool inTransition = now < _transitionEnd;
            Window = inTransition ? _flipName : string.Empty;
            if (faceOpen)
            {
                tags |= FrameTag.Face;
            }

            if (inTransition)
            {
                tags |= FrameTag.Transition;
            }

            if (building)
            {
                tags |= FrameTag.Build;
            }

            if (scanning)
            {
                tags |= FrameTag.Scan;
            }

            if (docked)
            {
                tags |= FrameTag.Dock;
            }

            return tags;
        }
    }
}
