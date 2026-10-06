namespace MoonProject.World
{
    /// <summary>What one terrain build produced and how long each phase took (milliseconds).</summary>
    public readonly struct TerrainBuildReport
    {
        public TerrainBuildReport(int chunks, int triangles, int colliderTriangles, double meshingMs, double uploadMs,
            double colliderBakeMs, double totalMs)
        {
            Chunks = chunks;
            Triangles = triangles;
            ColliderTriangles = colliderTriangles;
            MeshingMs = meshingMs;
            UploadMs = uploadMs;
            ColliderBakeMs = colliderBakeMs;
            TotalMs = totalMs;
        }

        public int Chunks { get; }

        /// <summary>Rendered triangles, chunks and backdrop together.</summary>
        public int Triangles { get; }

        public int ColliderTriangles { get; }

        /// <summary>Height sampling, painting and vertex generation (parallel).</summary>
        public double MeshingMs { get; }

        /// <summary>Creating GameObjects and uploading meshes (main thread).</summary>
        public double UploadMs { get; }

        /// <summary>PhysX cooking of every chunk collider (parallel jobs).</summary>
        public double ColliderBakeMs { get; }

        public double TotalMs { get; }

        public override string ToString()
        {
            return $"{Chunks} chunks, {Triangles} triangles ({ColliderTriangles} in colliders) in {TotalMs:0} ms " +
                $"(meshing {MeshingMs:0} ms, upload {UploadMs:0} ms, collider bake {ColliderBakeMs:0} ms)";
        }
    }
}
