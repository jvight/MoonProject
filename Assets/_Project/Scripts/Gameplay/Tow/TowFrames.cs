using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Structures built on a towed load (docs/design/opening-v3-purpose.md R6: the battery bank on the habitat's power
    /// module): the frame placed on the load takes it in, the load set into the structure where it stands; a frame
    /// taken down again (or a weld given up) gives it back where it stood. A loaded game's placed structures take
    /// their loads in the same way.
    /// </summary>
    public sealed class TowFrames : IDisposable
    {
        private readonly StructureCatalog _catalog;
        private readonly TowLine _tow;
        private readonly IDisposable _placed;
        private readonly IDisposable _removed;

        public TowFrames(EventBus events, StructureCatalog catalog, TowLine tow)
        {
            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            _catalog = catalog != null ? catalog : throw new ArgumentNullException(nameof(catalog));
            _tow = tow ?? throw new ArgumentNullException(nameof(tow));
            _placed = events.Subscribe<StructurePlaced>(OnPlaced);
            _removed = events.Subscribe<StructureRemoved>(OnRemoved);
        }

        public void Dispose()
        {
            _placed.Dispose();
            _removed.Dispose();
        }

        private void OnPlaced(StructurePlaced placed)
        {
            string load = FrameLoadOf(placed.StructureId);
            if (load.Length > 0)
            {
                _tow.TakeIn(load, placed.Position, placed.Yaw);
            }
        }

        private void OnRemoved(StructureRemoved removed)
        {
            string load = FrameLoadOf(removed.StructureId);
            if (load.Length > 0)
            {
                _tow.GiveBack(load);
            }
        }

        private string FrameLoadOf(string structureId)
        {
            return _catalog.TryGet(structureId, out StructureDefinition definition) ? definition.FrameLoad
                : string.Empty;
        }
    }
}
