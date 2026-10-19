namespace MoonProject.UI
{
    /// <summary>
    /// One thing the face's boot reports (<see cref="BootFace.TryTakeCue"/>), published as Core's
    /// <c>LogoStrokeDrawn</c>, <c>LoadStep</c>, <c>BootTextTyped</c> or <c>BootLineCompleted</c>.
    /// </summary>
    internal readonly struct BootFaceCue
    {
        public BootFaceCue(BootFaceCueKind kind, int a, int b)
        {
            Kind = kind;
            A = a;
            B = b;
        }

        public BootFaceCueKind Kind { get; }

        /// <summary>The stroke, the load step, or the line.</summary>
        public int A { get; }

        /// <summary>For <see cref="BootFaceCueKind.Typed"/>: the last character on screen; else 0.</summary>
        public int B { get; }
    }
}
