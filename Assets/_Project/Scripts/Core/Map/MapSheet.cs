using UnityEngine;

namespace MoonProject.Core.Map
{
    /// <summary>
    /// The crew's map's geometry (docs/features/M3-07), written by the World domain's builder from the generated
    /// surface: polylines in basin coordinates (world XZ in metres) for the contours, the rim crest, the canyon walls,
    /// the chasm and the crater rims, plus the world anchors' positions. UI draws it in pencil and ink; no texture is
    /// baked. Runtime code only reads it.
    /// </summary>
    public sealed class MapSheet : ScriptableObject
    {
        [Tooltip("World seed the sheet was traced from; also seeds the hand wobble.")]
        [SerializeField] private int _seed;

        [Tooltip("Centre of the paper in world XZ (the basin's centre).")]
        [SerializeField] private Vector2 _centre;

        [Tooltip("Metres from the centre to the paper's edge.")]
        [Min(1f)]
        [SerializeField] private float _radius = 1f;

        [Tooltip("Metres of height between two contour lines.")]
        [Min(0.1f)]
        [SerializeField] private float _contourInterval = 5f;

        [SerializeField] private Vector2[] _points = new Vector2[0];
        [SerializeField] private MapLine[] _lines = new MapLine[0];
        [SerializeField] private string[] _anchorIds = new string[0];
        [SerializeField] private Vector2[] _anchorPoints = new Vector2[0];

        public int Seed => _seed;

        public Vector2 Centre => _centre;

        public float Radius => _radius;

        public float ContourInterval => _contourInterval;

        public int PointCount => _points.Length;

        public int LineCount => _lines.Length;

        public int AnchorCount => _anchorIds.Length;

        public Vector2 GetPoint(int index)
        {
            return _points[index];
        }

        public MapLine GetLine(int index)
        {
            return _lines[index];
        }

        public string GetAnchorId(int index)
        {
            return _anchorIds[index];
        }

        public Vector2 GetAnchorPoint(int index)
        {
            return _anchorPoints[index];
        }

        /// <summary>Replaces the sheet's content (the World builder's only write).</summary>
        public void Write(int seed, Vector2 centre, float radius, float contourInterval, Vector2[] points,
            MapLine[] lines, string[] anchorIds, Vector2[] anchorPoints)
        {
            _seed = seed;
            _centre = centre;
            _radius = radius;
            _contourInterval = contourInterval;
            _points = points;
            _lines = lines;
            _anchorIds = anchorIds;
            _anchorPoints = anchorPoints;
        }

        /// <summary>A description of the first inconsistency, or null when the sheet can be drawn.</summary>
        public string Validate()
        {
            if (_points == null || _lines == null || _anchorIds == null || _anchorPoints == null)
            {
                return "the sheet has missing arrays (rebuild it with the World builder).";
            }

            if (_lines.Length == 0)
            {
                return "the sheet has no lines (rebuild it with the World builder).";
            }

            if (_anchorIds.Length != _anchorPoints.Length)
            {
                return "the anchor ids and positions differ in length.";
            }

            for (int i = 0; i < _lines.Length; i++)
            {
                MapLine line = _lines[i];
                if (line.Count < 2 || line.Start < 0 || line.Start + line.Count > _points.Length)
                {
                    return $"line {i} runs outside the points.";
                }
            }

            return null;
        }
    }
}
