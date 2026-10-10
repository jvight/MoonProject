using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>One friend's own name height, for a friend whose face sits higher than the shared lift.</summary>
    [Serializable]
    public sealed class FriendNameLift
    {
        [Tooltip("The friend's id, as in its definition (\"bell\").")]
        [SerializeField] private string _friendId;

        [Tooltip("Metres above this friend where its name floats: clear of its face.")]
        [Range(0f, 5f)]
        [SerializeField] private float _liftMetres;

        public FriendNameLift(string friendId, float liftMetres)
        {
            _friendId = friendId;
            _liftMetres = liftMetres;
        }

        public string FriendId => _friendId;

        public float LiftMetres => _liftMetres;
    }
}
