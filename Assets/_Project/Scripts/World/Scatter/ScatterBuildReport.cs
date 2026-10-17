namespace MoonProject.World
{
    /// <summary>What one scatter build produced and how long it took on the main thread (milliseconds).</summary>
    public readonly struct ScatterBuildReport
    {
        public ScatterBuildReport(int pebbles, int boulders, int triangles, int drawGroups, double buildMs,
            int solidCobbles = 0)
        {
            Pebbles = pebbles;
            SolidCobbles = solidCobbles;
            Boulders = boulders;
            Triangles = triangles;
            DrawGroups = drawGroups;
            BuildMs = buildMs;
        }

        public int Pebbles { get; }

        /// <summary>Medium cobbles (ScatterKind.Pebble at or above CobbleColliderMinSize) given a SphereCollider.</summary>
        public int SolidCobbles { get; }

        public int Boulders { get; }

        public int Triangles { get; }

        /// <summary>Combined chunk meshes (one draw each before culling).</summary>
        public int DrawGroups { get; }

        public double BuildMs { get; }

        public override string ToString()
        {
            return $"{Pebbles} pebbles ({SolidCobbles} solid cobbles), {Boulders} boulders, {Triangles} triangles " +
                $"in {DrawGroups} chunk meshes, built in {BuildMs:0} ms";
        }
    }
}
