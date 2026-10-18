using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay.Feel.Editor
{
    /// <summary>
    /// Where the scripted capture's cut happens: 07's emitter, the piece and a cut line laid across its face through
    /// its CutPoint (until the site prefabs carry their own cut lines). The hot point moves with the script's progress.
    /// </summary>
    internal sealed class FeelCaptureAnchors : IFeelAnchors
    {
        private readonly Transform _emitter;
        private readonly Vector3[] _line;
        private readonly Vector3 _faceNormal;

        public FeelCaptureAnchors(Transform emitter, Transform piece, Vector3 cutLocal, Vector3 normalLocal,
            float halfLength)
        {
            _emitter = emitter;
            CutPiece = piece;
            _faceNormal = normalLocal.normalized;
            Vector3 across = Vector3.Cross(_faceNormal, Vector3.up);
            across = across.sqrMagnitude > 1e-4f ? across.normalized : Vector3.right;
            Vector3 down = Vector3.Cross(across, _faceNormal).normalized;
            _line = new[]
            {
                cutLocal - across * halfLength + down * halfLength * 0.2f,
                cutLocal - down * halfLength * 0.15f,
                cutLocal + across * halfLength + down * halfLength * 0.1f,
            };
        }

        public float Progress { get; set; }

        public Vector3 Emitter => _emitter.position;

        public Vector3 ScanEmitter => _emitter.position;

        /// <summary>The capture stages no scan target: the beam draws to its beat's point.</summary>
        public bool TryScanContact(Vector3 lens, out Vector3 point, out Vector3 normal)
        {
            point = lens;
            normal = Vector3.up;
            return false;
        }

        public Vector3 CutPoint => CutPiece.TransformPoint(Along(Progress));

        public Vector3 CutNormal => CutPiece.TransformDirection(_faceNormal);

        public Transform CutPiece { get; }

        public IReadOnlyList<Vector3> CutLine => _line;

        public Vector3 CutFaceNormal => _faceNormal;

        private Vector3 Along(float t)
        {
            float scaled = Mathf.Clamp01(t) * (_line.Length - 1);
            int i = Mathf.Min(Mathf.FloorToInt(scaled), _line.Length - 2);
            return Vector3.Lerp(_line[i], _line[i + 1], scaled - i);
        }
    }
}
