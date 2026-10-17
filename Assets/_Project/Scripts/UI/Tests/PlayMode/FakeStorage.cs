using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>A grid of slots a test sets by hand (07's hold or home's shelves).</summary>
    public class FakeSlots : IMaterialSlots
    {
        private readonly List<MaterialSlot> _slots = new List<MaterialSlot>();

        public int SlotCount => _slots.Count;

        public virtual int UsedSlots
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

        /// <summary>Empties the first slot holding <paramref name="material"/> and returns what it held.</summary>
        public bool TryTake(SalvageMaterial material, out MaterialSlot taken)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (!_slots[i].IsEmpty && _slots[i].Material == material)
                {
                    taken = _slots[i];
                    _slots[i] = new MaterialSlot(material, 0, taken.Capacity);
                    return true;
                }
            }

            taken = default;
            return false;
        }

        /// <summary>Puts <paramref name="slot"/> into the first empty slot; false when none is.</summary>
        public bool TryPut(MaterialSlot slot)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsEmpty)
                {
                    _slots[i] = slot;
                    return true;
                }
            }

            return false;
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
        private readonly List<string> _parts = new List<string>();

        public int WaitingBundles { get; set; }

        public override int UsedSlots => base.UsedSlots + _parts.Count;

        public string GetPart(int index)
        {
            int part = index - base.UsedSlots;
            return part >= 0 && part < _parts.Count ? _parts[part] : string.Empty;
        }

        public bool HasPart(string partId)
        {
            return !string.IsNullOrEmpty(partId) && _parts.Contains(partId);
        }

        public void AddPart(string partId)
        {
            _parts.Add(partId);
        }

        public void RemovePart(string partId)
        {
            _parts.Remove(partId);
        }

        public void ClearParts()
        {
            _parts.Clear();
        }
    }

    /// <summary>07's systems, set by hand: the boot readout's lines and the igniter fit on the socket screen.</summary>
    public sealed class FakeSystems : IRoverSystems
    {
        private readonly RoverSystemState[] _states =
        {
            RoverSystemState.Ok, RoverSystemState.Ok, RoverSystemState.Ok, RoverSystemState.Ok,
            RoverSystemState.Fault, RoverSystemState.Jammed, RoverSystemState.ReceiveOnly, RoverSystemState.Empty,
        };

        public bool CanFitIgniter { get; set; }

        /// <summary>How many times the player asked to fit the igniter.</summary>
        public int Fits { get; private set; }

        public RoverSystemState StateOf(RoverSystem system)
        {
            return _states[(int)system];
        }

        public void Set(RoverSystem system, RoverSystemState state)
        {
            _states[(int)system] = state;
        }

        public bool TryFitIgniter()
        {
            if (!CanFitIgniter)
            {
                return false;
            }

            Fits++;
            CanFitIgniter = false;
            _states[(int)RoverSystem.Cutter] = RoverSystemState.Ok;
            return true;
        }
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

    /// <summary>The sky, set by hand: the Sun's elevation is all the Sun's arc reads.</summary>
    public sealed class FakeSunlight : ISunlight
    {
        public float ElevationDegrees { get; set; } = 10f;

        public float TimeSeconds => 0f;

        public bool IsHoldingEclipse => false;

        public Vector3 SunDirection
        {
            get
            {
                float radians = ElevationDegrees * Mathf.Deg2Rad;
                return new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
            }
        }

        public float EclipseFactor => 0f;

        public float EarthPhase => 0f;

        public SkyPhase Phase => SkyPhase.Sunlit;

        public float SunlightAt(Vector3 position)
        {
            return 1f;
        }

        public void ReleaseOpeningEclipse()
        {
        }
    }
}
