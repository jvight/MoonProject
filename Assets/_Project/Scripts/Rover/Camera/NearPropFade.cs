using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// Props near the lens thin away by dither and come back as the camera moves off (the feel audit's foreground
    /// fix): a cut panel or a crate between the lens and 07 never fills the frame. Each renderer within the fade
    /// distance of the lens, on the prop layer, gets a <c>_NearKeep</c> in its property block (R1Lit dithers on it); one
    /// that has left is eased back to 1. 07 and the tow's load are never touched. Fixed buffers: allocation-free.
    /// </summary>
    public sealed class NearPropFade
    {
        private const int HitCapacity = 48;
        private const int FadedCapacity = 32;
        private const float Whole = 0.999f;

        private static readonly int NearKeepId = Shader.PropertyToID("_NearKeep");

        private readonly Collider[] _hits = new Collider[HitCapacity];
        private readonly Renderer[] _faded = new Renderer[FadedCapacity];
        private readonly float[] _keep = new float[FadedCapacity];
        private readonly float[] _target = new float[FadedCapacity];
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private int _count;

        /// <summary>How many renderers are thinned (or coming back) now.</summary>
        public int Count => _count;

        /// <summary>1 whole beyond <paramref name="full"/>, 0 gone within <paramref name="gone"/>, smooth between.</summary>
        public static float Keep(float distance, float gone, float full)
        {
            return Smoothing.SmoothStep(gone, Mathf.Max(full, gone + 1e-3f), distance);
        }

        /// <summary>The keep a renderer is at now, or 1 when it is not thinned (tests).</summary>
        public float KeepOf(Renderer renderer)
        {
            int index = IndexOf(renderer);
            return index >= 0 ? _keep[index] : 1f;
        }

        /// <param name="lens">The camera's place.</param>
        /// <param name="gone">Metres within which a prop is gone.</param>
        /// <param name="full">Metres beyond which a prop is whole.</param>
        /// <param name="halfLife">Seconds for a prop to ease halfway to its keep.</param>
        /// <param name="spare">Renderers that are never thinned (the tow's load).</param>
        public void Step(Vector3 lens, float gone, float full, float halfLife, float deltaTime, Renderer[] spare)
        {
            for (int i = 0; i < _count; i++)
            {
                _target[i] = 1f;
            }

            int hits = Physics.OverlapSphereNonAlloc(lens, full, _hits, 1 << Layers.Prop, QueryTriggerInteraction.Ignore);
            for (int h = 0; h < hits; h++)
            {
                Renderer renderer = RendererOf(_hits[h]);
                if (renderer == null || !renderer.enabled || IsSpare(renderer, spare))
                {
                    continue;
                }

                float distance = Vector3.Distance(lens, renderer.bounds.ClosestPoint(lens));
                float keep = Keep(distance, gone, full);
                if (keep >= Whole)
                {
                    continue;
                }

                int index = IndexOf(renderer);
                if (index < 0)
                {
                    if (_count == FadedCapacity)
                    {
                        continue;
                    }

                    index = _count++;
                    _faded[index] = renderer;
                    _keep[index] = 1f;
                }

                _target[index] = keep;
            }

            for (int i = _count - 1; i >= 0; i--)
            {
                _keep[i] = deltaTime > 0f ? Smoothing.Damp(_keep[i], _target[i], halfLife, deltaTime) : _target[i];
                bool back = _target[i] >= Whole && _keep[i] >= Whole;
                Apply(i, back ? 1f : _keep[i]);
                if (back)
                {
                    Remove(i);
                }
            }
        }

        /// <summary>Brings every thinned prop back at once (a scene going away, a cut to another shot).</summary>
        public void Reset()
        {
            for (int i = _count - 1; i >= 0; i--)
            {
                Apply(i, 1f);
                Remove(i);
            }
        }

        private void Apply(int index, float keep)
        {
            Renderer renderer = _faded[index];
            if (renderer == null)
            {
                return;
            }

            renderer.GetPropertyBlock(_block);
            _block.SetFloat(NearKeepId, keep);
            renderer.SetPropertyBlock(_block);
        }

        private void Remove(int index)
        {
            _count--;
            _faded[index] = _faded[_count];
            _keep[index] = _keep[_count];
            _target[index] = _target[_count];
            _faded[_count] = null;
        }

        private int IndexOf(Renderer renderer)
        {
            for (int i = 0; i < _count; i++)
            {
                if (ReferenceEquals(_faded[i], renderer))
                {
                    return i;
                }
            }

            return -1;
        }

        private static Renderer RendererOf(Collider collider)
        {
            return collider.TryGetComponent(out Renderer own) ? own : collider.GetComponentInParent<Renderer>();
        }

        private static bool IsSpare(Renderer renderer, Renderer[] spare)
        {
            for (int i = 0; i < spare.Length; i++)
            {
                if (ReferenceEquals(spare[i], renderer))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
