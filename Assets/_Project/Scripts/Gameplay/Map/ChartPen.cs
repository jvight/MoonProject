using System;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Map;

namespace MoonProject.Gameplay.Map
{
    /// <summary>
    /// Kenji's chart pen behind the paper (docs/features/M3-07 steps 1 and 4): once the view rests on the map, it inks
    /// every place that is inked but not drawn yet, in place order (home first), one stroke apart, publishing
    /// <see cref="MapInkCued"/> for each (Audio climbs the pentatonic scale by its order, UI animates the stroke).
    /// Then it rests until the next look.
    /// </summary>
    public sealed class ChartPen
    {
        private readonly EventBus _events;
        private readonly MapKnowledge _knowledge;
        private float _nextAt;
        private int _order;
        private bool _drawing;

        public ChartPen(EventBus events, MapKnowledge knowledge)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _knowledge = knowledge ?? throw new ArgumentNullException(nameof(knowledge));
        }

        /// <summary>True while places are left to ink in this look.</summary>
        public bool IsDrawing => _drawing;

        /// <summary>The look began at <paramref name="now"/>: the first stroke comes <paramref name="lead"/> s later.</summary>
        public void Begin(float now, float lead)
        {
            _nextAt = now + lead;
            _order = 0;
            _drawing = true;
        }

        /// <summary>The look ended: what is not drawn yet waits for the next look.</summary>
        public void Stop()
        {
            _drawing = false;
        }

        /// <summary>Inks the next place when its stroke is due; <paramref name="stroke"/> s until the one after.</summary>
        public void Tick(float now, float stroke)
        {
            if (!_drawing || now < _nextAt)
            {
                return;
            }

            int index = _knowledge.TakeNextInk();
            if (index < 0)
            {
                _drawing = false;
                return;
            }

            MapPlace place = _knowledge.GetPlace(index);
            _events.Publish(new MapInkCued(place.Id, place.Position, _order));
            _order++;
            _nextAt = now + stroke;
        }
    }
}
