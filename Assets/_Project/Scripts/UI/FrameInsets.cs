namespace MoonProject.UI
{
    /// <summary>How far a frame is pulled in from each edge of the panel, in panel units.</summary>
    internal readonly struct FrameInsets
    {
        public FrameInsets(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public float Left { get; }

        public float Top { get; }

        public float Right { get; }

        public float Bottom { get; }
    }
}
