namespace MoonProject.World
{
    /// <summary>Height plus the region weights the terrain painter and scatter use to tell places apart.</summary>
    public readonly struct SurfaceSample
    {
        public SurfaceSample(float height, float craterBowl, float craterRim, float rimZone, float canyonFloor = 0f,
            float chasm = 0f, float scorch = 0f)
        {
            Height = height;
            CraterBowl = craterBowl;
            CraterRim = craterRim;
            RimZone = rimZone;
            CanyonFloor = canyonFloor;
            Chasm = chasm;
            Scorch = scorch;
        }

        public float Height { get; }

        /// <summary>0 outside craters and bowls, rising to 1 at a crater's centre.</summary>
        public float CraterBowl { get; }

        /// <summary>0 away from crater rims, 1 on a raised rim crest.</summary>
        public float CraterRim { get; }

        /// <summary>0 on the basin floor, 1 on the rim crest and beyond.</summary>
        public float RimZone { get; }

        /// <summary>1 on Whispering Canyon's floor (its corridors), 0 elsewhere.</summary>
        public float CanyonFloor { get; }

        /// <summary>1 in the canyon's chasm trough, 0 elsewhere.</summary>
        public float Chasm { get; }

        /// <summary>1 on the scorched dust of Kestrel-3's crater and furrow, 0 elsewhere.</summary>
        public float Scorch { get; }
    }
}
