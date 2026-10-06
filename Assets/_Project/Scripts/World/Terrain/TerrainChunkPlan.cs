using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// One square chunk of the terrain grid: where it is, its cell size and the cell sizes of its four neighbours
    /// (needed to stitch borders between tiers without cracks). A missing neighbour reports the chunk's own size.
    /// </summary>
    public readonly struct TerrainChunkPlan
    {
        public TerrainChunkPlan(int column, int row, Vector2 origin, float size, float cellSize, float westCellSize,
            float eastCellSize, float southCellSize, float northCellSize)
        {
            Column = column;
            Row = row;
            Origin = origin;
            Size = size;
            CellSize = cellSize;
            WestCellSize = westCellSize;
            EastCellSize = eastCellSize;
            SouthCellSize = southCellSize;
            NorthCellSize = northCellSize;
        }

        public int Column { get; }

        public int Row { get; }

        /// <summary>World XZ of the chunk's south-west corner (minimum X and Z).</summary>
        public Vector2 Origin { get; }

        public float Size { get; }

        public float CellSize { get; }

        public float WestCellSize { get; }

        public float EastCellSize { get; }

        public float SouthCellSize { get; }

        public float NorthCellSize { get; }

        /// <summary>Cells along one edge.</summary>
        public int Cells => Mathf.RoundToInt(Size / CellSize);
    }
}
