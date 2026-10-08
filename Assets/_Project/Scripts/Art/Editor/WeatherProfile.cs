namespace MoonProject.Art.Editor
{
    /// <summary>
    /// How hard the decades have worked one model over (see <see cref="Weathering.Weather"/>): the size of the tired
    /// panels its paint breaks into and whether some were replaced in a mismatched colour, how many rust runs hang
    /// under each panel's top edge, how high rust creeps up from the ground and how deep it bites at the bottom, and
    /// how high the dust climbs and how thick it lies. Heights are in the model's own space (its root on the ground).
    /// </summary>
    public sealed class WeatherProfile
    {
        public WeatherProfile(int seed, float lift, float panelWidth, float panelHeight, float paintWear,
            bool mismatched, float runsPerMetre, float rustHeight, float metalRust, float paintRust, float tide,
            float groundDust, float topDust)
        {
            Seed = seed;
            Lift = lift;
            PanelWidth = panelWidth;
            PanelHeight = panelHeight;
            PaintWear = paintWear;
            Mismatched = mismatched;
            RunsPerMetre = runsPerMetre;
            RustHeight = rustHeight;
            MetalRust = metalRust;
            PaintRust = paintRust;
            Tide = tide;
            GroundDust = groundDust;
            TopDust = topDust;
        }

        /// <summary>Picks every panel's tone and every rust run, so two models never weather alike.</summary>
        public int Seed { get; }

        /// <summary>
        /// How far the paint skin stands proud of the model (metres); the rust and dust skins stack at multiples of
        /// it. A few millimetres on buildings, a hair on 07, whose fresh "07" from Bell must sit over all three.
        /// </summary>
        public float Lift { get; }

        /// <summary>Width of the panels a big flat face's paint breaks into (metres).</summary>
        public float PanelWidth { get; }

        /// <summary>Height of those panels (metres); rust runs hang from their top edges.</summary>
        public float PanelHeight { get; }

        /// <summary>How far the panels' tones have gone (0..1): full on the buildings, gentler on 07.</summary>
        public float PaintWear { get; }

        /// <summary>Whether some panels were replaced in a mismatched colour (never on 07: it stays itself).</summary>
        public bool Mismatched { get; }

        /// <summary>Rust runs per metre of a panel's top edge, on average.</summary>
        public float RunsPerMetre { get; }

        /// <summary>How high the rust creeps up from the ground (metres; zero for none).</summary>
        public float RustHeight { get; }

        /// <summary>How far bare metal at the ground has turned to dark rust (0..1).</summary>
        public float MetalRust { get; }

        /// <summary>How far paint at the ground has rusted through (0..1).</summary>
        public float PaintRust { get; }

        /// <summary>How high the dust climbs from the ground (metres; zero for a part off the ground).</summary>
        public float Tide { get; }

        /// <summary>How far everything at the ground has turned to caked dust (0..1).</summary>
        public float GroundDust { get; }

        /// <summary>How far the faces turned to the sky have turned to caked dust (0..1).</summary>
        public float TopDust { get; }
    }
}
