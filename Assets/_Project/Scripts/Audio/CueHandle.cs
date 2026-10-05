namespace MoonProject.Audio
{
    /// <summary>
    /// A cue resolved once (at initialisation) from its id, so playback never hashes strings. The default value is
    /// invalid; playing an invalid handle is a wiring bug and is reported.
    /// </summary>
    public readonly struct CueHandle
    {
        private readonly int _slot;

        internal CueHandle(int index)
        {
            _slot = index + 1;
        }

        public bool IsValid => _slot > 0;

        /// <summary>Index into the <see cref="AudioLibrary"/>; -1 when invalid.</summary>
        public int Index => _slot - 1;
    }
}
