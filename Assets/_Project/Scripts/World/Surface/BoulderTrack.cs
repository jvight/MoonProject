using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The shallow furrow a boulder left as it rolled down a slope, from where it broke loose to where it rests (the
    /// boulder itself is World's scene prop at <see cref="End"/>).
    /// </summary>
    [Serializable]
    public struct BoulderTrack
    {
        [Tooltip("Where the boulder broke loose, up the slope (world XZ, metres).")]
        [SerializeField] private Vector2 _start;

        [Tooltip("Where the boulder came to rest, down the slope (world XZ, metres).")]
        [SerializeField] private Vector2 _end;

        [Tooltip("Width of the furrow, metres: the boulder's own width and the soft shoulders it pushed up, broad " +
            "enough that the floor stays smooth for 07.")]
        [Range(1f, 8f)]
        [SerializeField] private float _width;

        [Tooltip("Depth of the furrow, metres: shallow, read only in raking light.")]
        [Range(0f, 1f)]
        [SerializeField] private float _depth;

        [Tooltip("R1 rock prefab name of the boulder that rests at the furrow's end (Generated/Art/R1/Models/Rocks).")]
        [SerializeField] private string _rock;

        public BoulderTrack(Vector2 start, Vector2 end, float width, float depth, string rock)
        {
            _start = start;
            _end = end;
            _width = width;
            _depth = depth;
            _rock = rock;
        }

        public Vector2 Start => _start;
        public Vector2 End => _end;
        public float Width => _width;
        public float Depth => _depth;
        public string Rock => _rock ?? string.Empty;
    }
}
