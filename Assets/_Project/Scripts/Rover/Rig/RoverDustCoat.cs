using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The long-sleep regolith coat on 07's upper parts: raises each part's lunar-dust amount in the R1 shader through
    /// a property block (merged into any block already on the renderer, so lamps and the screen keep their own values)
    /// and puts it back to the material's own amount at level 0. The wheels and running gear are the visual rig's.
    /// </summary>
    public sealed class RoverDustCoat
    {
        private static readonly int DustAmountId = Shader.PropertyToID("_DustAmount");

        private readonly Renderer[][] _zones = new Renderer[PostDust.ZoneCount][];
        private readonly float[][] _base = new float[PostDust.ZoneCount][];
        private readonly Transform[] _anchors = new Transform[PostDust.ZoneCount];
        private readonly float[] _applied = new float[PostDust.ZoneCount];
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        /// <summary>
        /// Sorts the dustable renderers under <paramref name="root"/> into zones by which part they hang from; those in
        /// <paramref name="excluded"/> (the running gear) are left out, the rest are the body.
        /// </summary>
        public RoverDustCoat(Transform root, Transform head, Transform wing, Transform turret, Transform antenna,
            IReadOnlyCollection<Renderer> excluded)
        {
            var lists = new List<Renderer>[PostDust.ZoneCount];
            for (int i = 0; i < lists.Length; i++)
            {
                lists[i] = new List<Renderer>(8);
                _applied[i] = -1f;
            }

            _anchors[(int)DustZone.Head] = head;
            _anchors[(int)DustZone.Wing] = wing;
            _anchors[(int)DustZone.Antenna] = antenna;
            _anchors[(int)DustZone.Turret] = turret;
            _anchors[(int)DustZone.Body] = root;
            _anchors[(int)DustZone.Wheels] = root;

            var skip = new HashSet<Renderer>(excluded);
            foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (skip.Contains(renderer) || !HasDust(renderer))
                {
                    continue;
                }

                Transform t = renderer.transform;
                DustZone zone = DustZone.Body;
                if (t.IsChildOf(head))
                {
                    zone = DustZone.Head;
                }
                else if (t.IsChildOf(wing))
                {
                    zone = DustZone.Wing;
                }
                else if (t.IsChildOf(antenna))
                {
                    zone = DustZone.Antenna;
                }
                else if (turret != null && t.IsChildOf(turret))
                {
                    zone = DustZone.Turret;
                }

                lists[(int)zone].Add(renderer);
            }

            for (int i = 0; i < lists.Length; i++)
            {
                _zones[i] = lists[i].ToArray();
                _base[i] = new float[_zones[i].Length];
                for (int r = 0; r < _zones[i].Length; r++)
                {
                    _base[i][r] = _zones[i][r].sharedMaterial.GetFloat(DustAmountId);
                }
            }
        }

        /// <summary>Where a part is, for the dust that falls off it.</summary>
        public Vector3 AnchorPosition(DustZone zone)
        {
            return _anchors[(int)zone].position;
        }

        /// <summary>Sets a part's coat: 0 is its own material's amount, 1 is <paramref name="heavyAmount"/>.</summary>
        public void Set(DustZone zone, float level, float heavyAmount)
        {
            int index = (int)zone;
            if (_applied[index] < 0f && level <= 0f)
            {
                return;
            }

            if (Mathf.Abs(level - _applied[index]) < 1e-3f)
            {
                return;
            }

            _applied[index] = level;
            Renderer[] renderers = _zones[index];
            for (int i = 0; i < renderers.Length; i++)
            {
                float own = _base[index][i];
                renderers[i].GetPropertyBlock(_block);
                _block.SetFloat(DustAmountId, Mathf.Lerp(own, Mathf.Max(own, heavyAmount), level));
                renderers[i].SetPropertyBlock(_block);
            }
        }

        private static bool HasDust(Renderer renderer)
        {
            Material material = renderer.sharedMaterial;
            return material != null && material.HasProperty(DustAmountId);
        }
    }
}
