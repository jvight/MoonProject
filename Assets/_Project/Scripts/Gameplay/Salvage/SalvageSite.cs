using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One wreck in the world (docs/features/M3-13): Art's site prefab on its World anchor and its pieces. Picked
    /// clean, the skeleton stays.
    /// </summary>
    public sealed class SalvageSite
    {
        private readonly List<SalvagePiece> _pieces = new List<SalvagePiece>();
        private Vector3 _shudderRest;
        private float _shudderLeft;
        private float _shudderDuration;
        private float _shudderTime;

        internal SalvageSite(int index, string id, Transform root, float radius, ComboCounter combo)
        {
            Index = index;
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Root = root != null ? root : throw new ArgumentNullException(nameof(root));
            Radius = radius;
            Combo = combo ?? throw new ArgumentNullException(nameof(combo));
        }

        /// <summary>Position in the catalog (stable).</summary>
        public int Index { get; }

        /// <summary>Its anchor id ("site.kestrel"): saves and events use it.</summary>
        public string Id { get; }

        /// <summary>The site prefab's instance, standing on the anchor with +Z along its forward.</summary>
        public Transform Root { get; }

        public Vector3 Position => Root.position;

        /// <summary>Radius (m) of the site's flat footprint (its World anchor's).</summary>
        public float Radius { get; }

        public IReadOnlyList<SalvagePiece> Pieces => _pieces;

        /// <summary>Pieces not yet taken (on the wreck, held, or on their way into 07).</summary>
        public int Remaining
        {
            get
            {
                int remaining = 0;
                for (int i = 0; i < _pieces.Count; i++)
                {
                    if (_pieces[i].State != SalvagePieceState.Taken)
                    {
                        remaining++;
                    }
                }

                return remaining;
            }
        }

        /// <summary>
        /// Every piece is cut: folded into 07, on its way, or held on the wreck for want of room (it waits to be taken,
        /// but the wreck is done).
        /// </summary>
        public bool IsPickedClean
        {
            get
            {
                for (int i = 0; i < _pieces.Count; i++)
                {
                    if (_pieces[i].State == SalvagePieceState.Attached)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>The salvage melody of this site: consecutive pieces climb a step.</summary>
        internal ComboCounter Combo { get; }

        internal void Add(SalvagePiece piece)
        {
            _pieces.Add(piece ?? throw new ArgumentNullException(nameof(piece)));
        }

        /// <summary>The piece numbered <paramref name="number"/>, or null.</summary>
        public SalvagePiece Find(int number)
        {
            for (int i = 0; i < _pieces.Count; i++)
            {
                if (_pieces[i].Number == number)
                {
                    return _pieces[i];
                }
            }

            return null;
        }

        /// <summary>A piece cracks free: the wreck shudders for <paramref name="seconds"/> (feel-pass §3.1).</summary>
        internal void Shudder(float seconds)
        {
            if (_shudderLeft <= 0f)
            {
                _shudderRest = Root.position;
            }

            _shudderDuration = seconds;
            _shudderLeft = seconds;
            _shudderTime = 0f;
        }

        /// <summary>The shudder's side-to-side shake, fading out, then the wreck back where it stands.</summary>
        internal void StepShudder(float deltaTime, float amplitude, float frequency)
        {
            if (_shudderLeft <= 0f)
            {
                return;
            }

            _shudderLeft -= deltaTime;
            _shudderTime += deltaTime;
            if (_shudderLeft <= 0f)
            {
                Root.position = _shudderRest;
                return;
            }

            float fade = _shudderLeft / _shudderDuration;
            float shake = Mathf.Sin(_shudderTime * frequency * 2f * Mathf.PI);
            Root.position = _shudderRest + Root.right * (amplitude * fade * shake);
        }
    }
}
