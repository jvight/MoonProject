using System;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The docked transfer view's move (0.5.5 §3): one stack at a time between 07's hold and home's shelves, only while
    /// 07 rests on the dock and once the dock's own unload has finished (it runs first, D3), so a stack taken aboard
    /// never rides the belt straight back. The hold and the shelves each announce their change; the totals stay.
    /// </summary>
    public sealed class StackTransfer : IStackTransfer
    {
        private readonly MaterialStock _stock;
        private readonly IMaterialCatalog _catalog;
        private readonly IDockState _dock;
        private readonly DockUnload _unload;

        public StackTransfer(MaterialStock stock, IMaterialCatalog catalog, IDockState dock, DockUnload unload)
        {
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _dock = dock ?? throw new ArgumentNullException(nameof(dock));
            _unload = unload ?? throw new ArgumentNullException(nameof(unload));
        }

        public bool TryMove(SalvageMaterial material, bool toHome, out int moved)
        {
            moved = 0;
            if (!_dock.IsDocked || _unload.Running)
            {
                return false;
            }

            int stack = _catalog.StackSize(material);
            moved = toHome ? _stock.MoveHome(material, stack) : _stock.MoveAboard(material, stack);
            return moved > 0;
        }
    }
}
