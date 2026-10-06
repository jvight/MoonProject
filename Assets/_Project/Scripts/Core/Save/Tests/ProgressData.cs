using System;

namespace MoonProject.Core.Save.Tests
{
    /// <summary>Test save data (current shape, version 3).</summary>
    [Serializable]
    public sealed class ProgressData
    {
        public int scrap;
        public string name = string.Empty;
        public int relics;
    }
}
