using System;

namespace MoonProject.World
{
    /// <summary>
    /// Serialised state of the lunar sky cycle (<see cref="WorldSaveKeys.Sky"/>, version 1): the lunar clock time in
    /// seconds and whether the opening eclipse hold is still active.
    /// </summary>
    [Serializable]
    public sealed class SkySaveData
    {
        public float timeSeconds;
        public bool holdingEclipse;
    }
}
