namespace MoonProject.World
{
    /// <summary>Height plus the region weights the terrain painter and scatter use to tell places apart.</summary>
    public readonly struct SurfaceSample
    {
        public SurfaceSample(float height, float craterBowl, float craterRim, float rimZone)
        {
            Height = height;
            CraterBowl = craterBowl;
            CraterRim = craterRim;
            RimZone = rimZone;
        }

        public float Height { get; }

        /// <summary>0 outside craters and bowls, rising to 1 at a crater's centre.</summary>
        public float CraterBowl { get; }

        /// <summary>0 away from crater rims, 1 on a raised rim crest.</summary>
        public float CraterRim { get; }

        /// <summary>0 on the basin floor, 1 on the rim crest and beyond.</summary>
        public float RimZone { get; }
    }
}
