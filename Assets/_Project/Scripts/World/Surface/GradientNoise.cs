namespace MoonProject.World
{
    /// <summary>
    /// Seeded 2D gradient (Perlin) noise with a quintic fade, so values are C2-continuous. Output is roughly in
    /// [-1, 1]. Instances are immutable after construction and safe to sample from several threads.
    /// </summary>
    public sealed class GradientNoise
    {
        private const int TableSize = 256;
        private const int TableMask = TableSize - 1;
        private const float Diagonal = 0.70710678f;

        // Perlin 2D peaks at about +-0.707 with unit gradients; rescale so callers can reason in [-1, 1].
        private const float OutputScale = 1.41421356f;

        // Each fBm octave is rotated by ~36.9 degrees (3-4-5 triangle) so lattice axes never line up across octaves.
        private const float OctaveCos = 0.8f;
        private const float OctaveSin = 0.6f;
        private const float OctaveShiftX = 37.17f;
        private const float OctaveShiftY = 91.43f;

        private readonly int[] _permutation = new int[TableSize * 2];
        private readonly float[] _gradientX = { 1f, -1f, 0f, 0f, Diagonal, -Diagonal, Diagonal, -Diagonal };
        private readonly float[] _gradientY = { 0f, 0f, 1f, -1f, Diagonal, Diagonal, -Diagonal, -Diagonal };

        public GradientNoise(uint seed)
        {
            var random = new SeededRandom(seed);
            for (int i = 0; i < TableSize; i++)
            {
                _permutation[i] = i;
            }

            for (int i = TableSize - 1; i > 0; i--)
            {
                int j = random.Range(0, i + 1);
                int swap = _permutation[i];
                _permutation[i] = _permutation[j];
                _permutation[j] = swap;
            }

            for (int i = 0; i < TableSize; i++)
            {
                _permutation[i + TableSize] = _permutation[i];
            }
        }

        /// <summary>Single octave of noise at (x, y), roughly in [-1, 1].</summary>
        public float Sample(float x, float y)
        {
            int xi = SmoothMath.FastFloor(x);
            int yi = SmoothMath.FastFloor(y);
            float xf = x - xi;
            float yf = y - yi;
            int xa = xi & TableMask;
            int ya = yi & TableMask;

            int[] p = _permutation;
            int a = p[xa] + ya;
            int b = p[xa + 1] + ya;

            float u = Fade(xf);
            float v = Fade(yf);
            float n00 = Dot(p[a], xf, yf);
            float n10 = Dot(p[b], xf - 1f, yf);
            float n01 = Dot(p[a + 1], xf, yf - 1f);
            float n11 = Dot(p[b + 1], xf - 1f, yf - 1f);

            float nx0 = n00 + (n10 - n00) * u;
            float nx1 = n01 + (n11 - n01) * u;
            return (nx0 + (nx1 - nx0) * v) * OutputScale;
        }

        /// <summary>Fractal sum of <paramref name="octaves"/> octaves, normalised back to roughly [-1, 1].</summary>
        public float Fractal(float x, float y, int octaves, float lacunarity, float gain)
        {
            float sum = 0f;
            float amplitude = 1f;
            float total = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += amplitude * Sample(x, y);
                total += amplitude;
                amplitude *= gain;
                float rotatedX = (OctaveCos * x - OctaveSin * y) * lacunarity + OctaveShiftX;
                y = (OctaveSin * x + OctaveCos * y) * lacunarity + OctaveShiftY;
                x = rotatedX;
            }

            return sum / total;
        }

        /// <summary>
        /// Ridged fractal in [0, 1]: sharp-looking crests built from a smooth absolute value, so ridges stay C1 while
        /// reading as jagged once sampled by a low-poly grid. <paramref name="softness"/> rounds each crest.
        /// </summary>
        public float Ridged(float x, float y, int octaves, float lacunarity, float gain, float softness)
        {
            float sum = 0f;
            float amplitude = 1f;
            float total = 0f;
            float crestScale = 1f / (1f + softness);
            for (int i = 0; i < octaves; i++)
            {
                float ridge = 1f - SmoothMath.SmoothAbs(Sample(x, y), softness) * crestScale;
                sum += amplitude * ridge * ridge;
                total += amplitude;
                amplitude *= gain;
                float rotatedX = (OctaveCos * x - OctaveSin * y) * lacunarity + OctaveShiftX;
                y = (OctaveSin * x + OctaveCos * y) * lacunarity + OctaveShiftY;
                x = rotatedX;
            }

            return sum / total;
        }

        private static float Fade(float t)
        {
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        private float Dot(int hash, float x, float y)
        {
            int index = hash & 7;
            return _gradientX[index] * x + _gradientY[index] * y;
        }
    }
}
