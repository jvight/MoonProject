using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Lays the square chunk grid over [-extent, extent]^2 and gives each chunk a cell tier by the distance of its
    /// nearest point to the base (fine on the drivable floor, mid on the rim wall, coarse on the crest, far beyond it),
    /// and fine cells wherever the surface asks for them (Whispering Canyon).
    /// </summary>
    public static class TerrainChunkPlanner
    {
        /// <param name="needsFine">True for chunk rectangles that must use fine cells whatever their distance.</param>
        public static TerrainChunkPlan[] Plan(TerrainMeshSettings settings, System.Func<Rect, bool> needsFine)
        {
            float size = settings.ChunkSize;
            int count = Mathf.RoundToInt(settings.GridHalfExtent * 2f / size);
            float start = -settings.GridHalfExtent;
            var cells = new float[count, count];
            for (int row = 0; row < count; row++)
            {
                for (int column = 0; column < count; column++)
                {
                    float minX = start + column * size;
                    float minZ = start + row * size;
                    float cell = CellSizeFor(minX, minZ, size, settings);
                    cells[column, row] = needsFine(new Rect(minX, minZ, size, size))
                        ? Mathf.Min(cell, settings.FineCellSize)
                        : cell;
                }
            }

            var plans = new TerrainChunkPlan[count * count];
            for (int row = 0; row < count; row++)
            {
                for (int column = 0; column < count; column++)
                {
                    float own = cells[column, row];
                    plans[row * count + column] = new TerrainChunkPlan(column, row,
                        new Vector2(start + column * size, start + row * size), size, own,
                        column > 0 ? cells[column - 1, row] : own,
                        column < count - 1 ? cells[column + 1, row] : own,
                        row > 0 ? cells[column, row - 1] : own,
                        row < count - 1 ? cells[column, row + 1] : own);
                }
            }

            return plans;
        }

        private static float CellSizeFor(float minX, float minZ, float size, TerrainMeshSettings settings)
        {
            float distance = DistanceToRect(0f, 0f, minX, minZ, size);
            if (settings.NearFineRadius > 0f && settings.NearFineCellSize < settings.FineCellSize)
            {
                float siteRadius = settings.NearFineRadius * 0.5f;
                Vector2 extra = settings.SecondaryFineFocus;
                if (distance < settings.NearFineRadius
                    || DistanceToRect(-12.9f, 184.5f, minX, minZ, size) < siteRadius
                    || DistanceToRect(285f, 92f, minX, minZ, size) < siteRadius
                    || (extra.sqrMagnitude > 1f && DistanceToRect(extra.x, extra.y, minX, minZ, size) < siteRadius))
                {
                    return settings.NearFineCellSize;
                }
            }

            if (distance < settings.FineRadius)
            {
                return settings.FineCellSize;
            }

            if (distance < settings.MidRadius)
            {
                return settings.MidCellSize;
            }

            return distance < settings.CoarseRadius ? settings.CoarseCellSize : settings.FarCellSize;
        }

        private static float DistanceToRect(float x, float z, float minX, float minZ, float size)
        {
            float dx = Mathf.Clamp(x, minX, minX + size) - x;
            float dz = Mathf.Clamp(z, minZ, minZ + size) - z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
