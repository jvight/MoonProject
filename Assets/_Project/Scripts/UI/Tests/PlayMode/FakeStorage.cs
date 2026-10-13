using System.Collections.Generic;
using MoonProject.Core;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>A grid of slots a test sets by hand (07's hold or home's shelves).</summary>
    public class FakeSlots : IMaterialSlots
    {
        private readonly List<MaterialSlot> _slots = new List<MaterialSlot>();

        public int SlotCount => _slots.Count;

        public int UsedSlots
        {
            get
            {
                int used = 0;
                foreach (MaterialSlot slot in _slots)
                {
                    used += slot.IsEmpty ? 0 : 1;
                }

                return used;
            }
        }

        public bool IsFull => UsedSlots == SlotCount;

        public MaterialSlot GetSlot(int index)
        {
            return _slots[index];
        }

        public int Units(SalvageMaterial material)
        {
            int units = 0;
            foreach (MaterialSlot slot in _slots)
            {
                units += !slot.IsEmpty && slot.Material == material ? slot.Units : 0;
            }

            return units;
        }

        public int FreeUnits(SalvageMaterial material)
        {
            return 0;
        }

        /// <summary>Replaces every slot: <paramref name="empty"/> empty slots after the filled ones.</summary>
        public void Set(int empty, params MaterialSlot[] filled)
        {
            _slots.Clear();
            _slots.AddRange(filled);
            for (int i = 0; i < empty; i++)
            {
                _slots.Add(new MaterialSlot(SalvageMaterial.Metal, 0, 4));
            }
        }
    }

    /// <summary>07's hold, set by hand.</summary>
    public sealed class FakeHold : FakeSlots, ICargoHold
    {
        public int WaitingBundles { get; set; }
    }

    /// <summary>Every material described with the names the tables hold.</summary>
    public sealed class FakeCatalog : IMaterialCatalog
    {
        private const int SalvageStack = 4;
        private const int LunarStack = 6;

        public bool Contains(SalvageMaterial material)
        {
            return true;
        }

        public MaterialFamily FamilyOf(SalvageMaterial material)
        {
            return (int)material < 3 ? MaterialFamily.Salvage
                : (int)material < 8 ? MaterialFamily.Lunar : MaterialFamily.Refined;
        }

        public int StackSize(SalvageMaterial material)
        {
            return FamilyOf(material) == MaterialFamily.Salvage ? SalvageStack : LunarStack;
        }

        public string NameKey(SalvageMaterial material)
        {
            return UiKeys.MaterialName(material);
        }

        public string JobKey(SalvageMaterial material)
        {
            return UiKeys.MaterialName(material) + ".job";
        }

        public string SourceKey(SalvageMaterial material)
        {
            return UiKeys.MaterialName(material) + ".source";
        }
    }
}
