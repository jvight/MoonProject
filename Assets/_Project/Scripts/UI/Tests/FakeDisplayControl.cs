using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.UI.Tests
{
    /// <summary>A screen for tests: a few monitor sizes, the last state it was asked to apply, and a count.</summary>
    internal sealed class FakeDisplayControl : IDisplayControl
    {
        private readonly List<Vector2Int> _resolutions = DisplayChoices.Distinct(new[]
        {
            new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(1920, 1080),
            new Vector2Int(2560, 1440), new Vector2Int(800, 600), new Vector2Int(1600, 900),
        }, 1024);

        public IReadOnlyList<Vector2Int> Resolutions => _resolutions;

        /// <summary>The desktop: the second largest size, so "native" is not just the first in the list.</summary>
        public Vector2Int Native { get; set; } = new Vector2Int(1920, 1080);

        public DisplayState Applied { get; private set; }

        public int Applications { get; private set; }

        public void Apply(DisplayState state)
        {
            Applied = state;
            Applications++;
        }
    }
}
