using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MoonProject.Rover
{
    /// <summary>
    /// The dip to dark while the face screen's camera is put back (<see cref="FaceCloseSequence"/>): a global
    /// post-processing volume that takes the frame's exposure down, so 07's CRT and the world fade together. At no
    /// weight it is switched off and costs nothing.
    /// </summary>
    public sealed class FaceCloseDip : IDisposable
    {
        // Above the world's own volume and the menu backdrop's (100), so the dip wins while it runs.
        private const float Priority = 200f;

        private readonly GameObject _host;
        private readonly VolumeProfile _profile;
        private readonly Volume _volume;

        public FaceCloseDip(Transform parent, float darkExposure)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "FaceCloseDip";
            ColorAdjustments colour = _profile.Add<ColorAdjustments>(true);
            colour.postExposure.Override(darkExposure);
            _host = new GameObject("FaceCloseDip");
            _host.transform.SetParent(parent, false);
            _volume = _host.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = Priority;
            _volume.sharedProfile = _profile;
            _volume.weight = 0f;
            _volume.enabled = false;
        }

        /// <summary>How dark the view is (0 clear, 1 as dark as the exposure goes).</summary>
        public float Dark01 => _volume.weight;

        public void Set(float dark01)
        {
            float weight = Mathf.Clamp01(dark01);
            _volume.weight = weight;
            _volume.enabled = weight > 0f;
        }

        public void Dispose()
        {
            UnityEngine.Object.Destroy(_host);
            UnityEngine.Object.Destroy(_profile);
        }
    }
}
