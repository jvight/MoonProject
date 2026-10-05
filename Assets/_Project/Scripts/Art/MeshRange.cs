namespace MoonProject.Art
{
    /// <summary>A contiguous run of triangles inside a <see cref="LowPolyMeshBuilder"/> (e.g. one primitive).</summary>
    public readonly struct MeshRange
    {
        public MeshRange(int firstTriangle, int triangleCount)
        {
            FirstTriangle = firstTriangle;
            TriangleCount = triangleCount;
        }

        public int FirstTriangle { get; }

        public int TriangleCount { get; }

        public int EndTriangle => FirstTriangle + TriangleCount;
    }
}
