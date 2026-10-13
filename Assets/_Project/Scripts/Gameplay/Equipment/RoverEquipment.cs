using System;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// 07's equipment (docs/design/0.5.5-owner-feedback.md §3.5): one part per socket and a short row of crafted parts
    /// waiting to be fitted. A bay part is crafted into the row and fitted, swapped or taken off by hand while 07 rests
    /// on the bay's turntable; Kenji's port parts and friends' gifts fit themselves in their own moments. Abilities
    /// follow fitted parts: fitting grants, taking off pauses. A swap's old part is recycled into Metal (never lost).
    /// A load lays the saved state in and publishes nothing. Allocation-free after construction.
    /// </summary>
    public sealed class RoverEquipment : IRoverEquipment
    {
        private static readonly int SocketCount = Enum.GetValues(typeof(RoverSocket)).Length;

        private readonly EventBus _events;
        private readonly IRoverAbilities _abilities;
        private readonly EquipmentTable _table;
        private readonly Func<PartStation, bool> _atStation;
        private readonly Func<RoverKitPiece, int> _recycledMetal;
        private readonly Action<int> _deliverMetal;
        private readonly bool[] _occupied;
        private readonly RoverKitPiece[] _fitted;
        private readonly bool[] _known;
        private readonly RoverKitPiece[] _row;

        /// <param name="atStation">True while 07 rests at the station (the bay's turntable, the port's pad).</param>
        /// <param name="recycledMetal">Metal the bay makes of a swapped-out part (half its recipe's).</param>
        /// <param name="deliverMetal">Where recycled Metal goes: home's shelves, else the hold, else a bundle.</param>
        public RoverEquipment(EventBus events, IRoverAbilities abilities, EquipmentTable table, int rowCapacity,
            Func<PartStation, bool> atStation, Func<RoverKitPiece, int> recycledMetal, Action<int> deliverMetal)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
            _table = table ?? throw new ArgumentNullException(nameof(table));
            _atStation = atStation ?? throw new ArgumentNullException(nameof(atStation));
            _recycledMetal = recycledMetal ?? throw new ArgumentNullException(nameof(recycledMetal));
            _deliverMetal = deliverMetal ?? throw new ArgumentNullException(nameof(deliverMetal));
            if (rowCapacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(rowCapacity), rowCapacity,
                    "The row holds a part at least.");
            }

            _occupied = new bool[SocketCount];
            _fitted = new RoverKitPiece[SocketCount];
            _known = new bool[SocketCount];
            _row = new RoverKitPiece[rowCapacity];
        }

        public int RowCapacity => _row.Length;

        public int RowCount { get; private set; }

        /// <summary>True when the row is full: the bay crafts nothing more until a part is fitted.</summary>
        public bool RowFull => RowCount >= _row.Length;

        public RoverSocket SocketOf(RoverKitPiece part)
        {
            return _table.Find(part).Socket;
        }

        public bool TryGetFitted(RoverSocket socket, out RoverKitPiece part)
        {
            part = _fitted[(int)socket];
            return _occupied[(int)socket];
        }

        public bool IsKnown(RoverSocket socket)
        {
            return _known[(int)socket];
        }

        public RoverKitPiece RowPart(int index)
        {
            if (index < 0 || index >= RowCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"The row holds {RowCount} parts.");
            }

            return _row[index];
        }

        /// <summary>True when <paramref name="part"/> is fitted in its socket now.</summary>
        public bool IsFitted(RoverKitPiece part)
        {
            int socket = (int)_table.Find(part).Socket;
            return _occupied[socket] && _fitted[socket] == part;
        }

        public bool CanFit(RoverKitPiece part)
        {
            PartStation station = _table.Find(part).Station;
            return station != PartStation.Gift && _atStation(station);
        }

        /// <summary>
        /// The socket for <paramref name="part"/> is known from now on (its blueprint completed): the screen names it.
        /// </summary>
        public void MarkKnown(RoverKitPiece part)
        {
            _known[(int)_table.Find(part).Socket] = true;
        }

        /// <summary>
        /// The bay finished crafting <paramref name="part"/>: it waits in the row (<see cref="PartCrafted"/>). False,
        /// and nothing changes, when the row is full.
        /// </summary>
        public bool Craft(RoverKitPiece part)
        {
            EquipmentPart entry = _table.Find(part);
            if (entry.Station != PartStation.Bay)
            {
                throw new ArgumentException($"{part} is fitted at {entry.Station}, never crafted.", nameof(part));
            }

            if (RowFull)
            {
                return false;
            }

            int index = RowCount;
            _row[index] = part;
            RowCount++;
            _known[(int)entry.Socket] = true;
            _events.Publish(new PartCrafted(part, index));
            return true;
        }

        /// <summary>
        /// Kenji's port fits one of his parts, or a friend's gift settles on 07: the part goes straight into its socket
        /// (<see cref="PartFitted"/>, with <see cref="PartFitted.Gift"/> for a gift).
        /// </summary>
        public void FitItself(RoverKitPiece part)
        {
            EquipmentPart entry = _table.Find(part);
            if (entry.Station == PartStation.Bay)
            {
                throw new ArgumentException($"{part} is crafted and fitted at the bay.", nameof(part));
            }

            Fit(entry, entry.Station == PartStation.Gift);
        }

        public bool TryFit(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= RowCount || !CanFit(_row[rowIndex]))
            {
                return false;
            }

            EquipmentPart entry = _table.Find(_row[rowIndex]);
            RemoveFromRow(rowIndex);
            Fit(entry, false);
            return true;
        }

        public bool TryUnequip(RoverSocket socket)
        {
            int s = (int)socket;
            if (!_occupied[s] || RowFull || !CanFit(_fitted[s]))
            {
                return false;
            }

            EquipmentPart entry = _table.Find(_fitted[s]);
            _occupied[s] = false;
            Revoke(entry);
            _row[RowCount] = entry.Piece;
            RowCount++;
            _events.Publish(new PartRemoved(socket, entry.Piece));
            return true;
        }

        public EquipmentSaveData Capture()
        {
            int fitted = 0;
            for (int s = 0; s < SocketCount; s++)
            {
                fitted += _occupied[s] ? 1 : 0;
            }

            var data = new EquipmentSaveData { fitted = new int[fitted], row = new int[RowCount] };
            int next = 0;
            for (int s = 0; s < SocketCount; s++)
            {
                if (_occupied[s])
                {
                    data.fitted[next++] = (int)_fitted[s];
                }
            }

            for (int i = 0; i < RowCount; i++)
            {
                data.row[i] = (int)_row[i];
            }

            return data;
        }

        /// <summary>
        /// True once a save's own equipment section was laid in: older saves' owned kit is then ignored.
        /// </summary>
        public bool Restored { get; private set; }

        /// <summary>
        /// Lays a save in: each fitted part in its socket (its ability granted), the row in order, replacing anything
        /// an older section's kit laid first. Publishes nothing; Rover reads the state at boot. Pieces this build does
        /// not know (a newer save's) are skipped.
        /// </summary>
        public void Restore(EquipmentSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Restored = true;
            for (int s = 0; s < SocketCount; s++)
            {
                if (_occupied[s])
                {
                    _occupied[s] = false;
                    Revoke(_table.Find(_fitted[s]));
                }
            }

            RowCount = 0;
            foreach (int piece in data.fitted ?? Array.Empty<int>())
            {
                if (_table.TryFind((RoverKitPiece)piece, out EquipmentPart entry))
                {
                    Lay(entry);
                }
            }

            foreach (int piece in data.row ?? Array.Empty<int>())
            {
                if (!RowFull && _table.TryFind((RoverKitPiece)piece, out EquipmentPart entry))
                {
                    _row[RowCount] = entry.Piece;
                    RowCount++;
                    _known[(int)entry.Socket] = true;
                }
            }
        }

        /// <summary>
        /// A save from before equipment (0.5.4): a part 07 owned loads fitted in its socket, the row empty (§3.5 "Save
        /// and migration"). Ignored once the save's own equipment section is in (<see cref="Restored"/>), so the order
        /// sections load in never matters. Publishes nothing.
        /// </summary>
        public void RestoreOwned(RoverKitPiece part)
        {
            if (!Restored)
            {
                Lay(_table.Find(part));
            }
        }

        /// <summary>
        /// A bought part that a load found neither fitted nor waiting (its socket empty) goes back into the row,
        /// silently: a game closed between a purchase and its craft never loses the paid part.
        /// </summary>
        public void ReturnUnplaced(RoverKitPiece part)
        {
            EquipmentPart entry = _table.Find(part);
            if (_occupied[(int)entry.Socket] || InRow(part) || RowFull)
            {
                return;
            }

            _row[RowCount] = part;
            RowCount++;
            _known[(int)entry.Socket] = true;
        }

        private bool InRow(RoverKitPiece part)
        {
            for (int i = 0; i < RowCount; i++)
            {
                if (_row[i] == part)
                {
                    return true;
                }
            }

            return false;
        }

        private void Lay(EquipmentPart entry)
        {
            int s = (int)entry.Socket;
            _occupied[s] = true;
            _fitted[s] = entry.Piece;
            _known[s] = true;
            Grant(entry);
        }

        private void Fit(EquipmentPart entry, bool gift)
        {
            int s = (int)entry.Socket;
            bool replaced = _occupied[s];
            RoverKitPiece old = _fitted[s];
            int metal = 0;
            if (replaced)
            {
                Revoke(_table.Find(old));
                metal = _recycledMetal(old);
            }

            _occupied[s] = true;
            _fitted[s] = entry.Piece;
            _known[s] = true;
            Grant(entry);
            _events.Publish(new PartFitted(entry.Socket, entry.Piece, gift, replaced, old, metal));
            if (metal > 0)
            {
                _deliverMetal(metal);
            }
        }

        private void RemoveFromRow(int index)
        {
            for (int i = index; i < RowCount - 1; i++)
            {
                _row[i] = _row[i + 1];
            }

            RowCount--;
        }

        private void Grant(EquipmentPart entry)
        {
            if (entry.GivesAbility)
            {
                _abilities.Grant(entry.Ability);
            }
        }

        private void Revoke(EquipmentPart entry)
        {
            if (entry.GivesAbility)
            {
                _abilities.Revoke(entry.Ability);
            }
        }
    }
}
