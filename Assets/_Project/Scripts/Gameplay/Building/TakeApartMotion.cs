using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The timeline of taking a structure apart, the reverse of the print: it eases down, then its pieces float to 07 one
    /// after another and shrink as they arrive, the beam reaching out first and thinning at the end. Plain maths, no
    /// pop: every value eases in and out and each piece is gone only once it has shrunk to nothing.
    /// </summary>
    public readonly struct TakeApartMotion
    {
        /// <summary>Share of the flight window each piece's start is staggered over.</summary>
        private const float StaggerShare = 0.2f;

        /// <summary>Share of the ease-down after which the first piece may leave.</summary>
        private const float FirstLeavesAt = 0.7f;

        /// <summary>The scale a piece has when it arrives (it is hidden at this size).</summary>
        public const float ArrivalScale = 0.02f;

        /// <summary>Share of the run after which the beam retracts.</summary>
        private const float BeamRetractsAt = 0.85f;

        private readonly float _easeShare;
        private readonly int _pieces;

        public TakeApartMotion(float easeShare, int pieces)
        {
            _easeShare = Mathf.Clamp(easeShare, 0.05f, 0.6f);
            _pieces = Mathf.Max(1, pieces);
        }

        public int Pieces => _pieces;

        /// <summary>0..1 how far the whole piece has eased down at run time <paramref name="t01"/>.</summary>
        public float Sink01(float t01)
        {
            return Smooth(Mathf.Clamp01(t01) / _easeShare);
        }

        /// <summary>0..1 how far piece <paramref name="index"/> has flown to 07.</summary>
        public float Flight01(int index, float t01)
        {
            float start = _easeShare * FirstLeavesAt;
            float stagger = (1f - start) * StaggerShare;
            float legStart = start + stagger * index / Mathf.Max(1, _pieces - 1);
            float legEnd = 1f - stagger * (_pieces - 1 - index) / Mathf.Max(1, _pieces - 1);
            return Smooth((Mathf.Clamp01(t01) - legStart) / Mathf.Max(legEnd - legStart, 1e-3f));
        }

        /// <summary>Scale of piece <paramref name="index"/> (1 until it leaves, tiny on arrival).</summary>
        public float Scale(int index, float t01)
        {
            float flight = Flight01(index, t01);
            return Mathf.Lerp(1f, ArrivalScale, flight * flight);
        }

        /// <summary>True from the moment the beam should thin out.</summary>
        public bool BeamRetracting(float t01)
        {
            return t01 >= BeamRetractsAt;
        }

        /// <summary>Metres above the straight path piece <paramref name="index"/> floats at flight <paramref name="flight01"/>.</summary>
        public static float Arc(float flight01, float arcMetres)
        {
            return Mathf.Sin(Mathf.PI * Mathf.Clamp01(flight01)) * arcMetres;
        }

        private static float Smooth(float x)
        {
            float c = Mathf.Clamp01(x);
            return c * c * (3f - 2f * c);
        }
    }
}
