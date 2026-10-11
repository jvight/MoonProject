using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core.Map;

namespace MoonProject.UI.Map
{
    /// <summary>
    /// One place on the crew's paper: where it sits, its ink (outline and icons, rebuilt each time the map opens and
    /// revealed stroke by stroke while the chart pen draws it), and its labels (its name, a pencil "?").
    /// </summary>
    internal sealed class MapMark
    {
        public MapMark(MapPlace place, Vector2 at, float radius, Label name, Label question)
        {
            Place = place;
            At = at;
            Radius = radius;
            Name = name;
            Question = question;
        }

        public MapPlace Place { get; }

        /// <summary>Paper position.</summary>
        public Vector2 At { get; }

        /// <summary>Paper radius of its ring.</summary>
        public float Radius { get; }

        public Label Name { get; }

        public Label Question { get; }

        public StrokeBuffer Ink { get; } = new StrokeBuffer();

        /// <summary>Its ink is on the paper (drawn by an earlier look, or by this look's pen).</summary>
        public bool Drawn { get; set; }

        /// <summary>The pen is drawing it: 0 at the first stroke, 1 once the outline is whole.</summary>
        public float Reveal { get; set; }

        /// <summary>Seconds since the pen started on it (its name fades in after the outline).</summary>
        public float Since { get; set; }

        public bool Animating { get; set; }

        /// <summary>Its name is shown in pencil (visited, not inked yet).</summary>
        public bool PencilName { get; set; }
    }
}
