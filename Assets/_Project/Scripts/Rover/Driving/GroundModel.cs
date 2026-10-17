using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>
    /// Driveable surface kind beneath 07's wheels (VFX-02): regolith throws ballistic dust and records tracks;
    /// bare rock and metal throw a few crisp chips/sparks and select a distinct surface sound hook.
    /// </summary>
    public enum GroundSurface
    {
        Regolith = 0,
        Rock = 1,
        Metal = 2,
    }

    /// <summary>
    /// Ground-contact forces for the physics sphere, expressed as accelerations so they are independent of mass,
    /// plus driveable surface classification for surface-aware wheel FX and sound hooks.
    /// </summary>
    public static class GroundModel
    {
        private static readonly string[] MetalKeywords =
        {
            "metal", "pad", "dock", "lander", "wreck", "salvage", "site_", "derelict", "scrap",
            "hull", "container", "crate", "tower", "workshop", "bay", "relay", "plate", "strut",
            "deck", "bridge", "solar", "beacon", "depot", "garage", "conduit", "antenna", "dish",
        };

        private static readonly string[] RockKeywords =
        {
            "rock", "boulder", "stone", "cliff", "monolith", "slab", "arch", "pillar", "ruin",
            "canyongate", "outcrop", "crag", "ledge", "wall",
        };

        private static readonly Dictionary<int, GroundSurface> SurfaceCache = new Dictionary<int, GroundSurface>(64);

        /// <summary>1 on gentle slopes, easing to 0 between the full and zero assist angles.</summary>
        public static float SlopeAssist(GroundSettings settings, float slopeAngle)
        {
            return 1f - Smoothing.SmoothStep(settings.SlopeAssistFullAngle, settings.SlopeAssistZeroAngle, slopeAngle);
        }

        /// <summary>
        /// Acceleration that cancels the along-slope pull of gravity (scaled by slope assist) and then gives back a
        /// fraction of it along the heading while rolling, so hills keep some character but a parked rover stays put.
        /// </summary>
        public static Vector3 SlopeAcceleration(GroundSettings settings, Vector3 gravity, Vector3 normal,
            Vector3 forward, float forwardSpeed)
        {
            Vector3 tangential = gravity - normal * Vector3.Dot(gravity, normal);
            float assist = SlopeAssist(settings, Vector3.Angle(normal, Vector3.up));
            float rolling = Smoothing.SmoothStep(0f, settings.SlopeRollSpeed, Mathf.Abs(forwardSpeed));
            Vector3 felt = forward * (Vector3.Dot(tangential, forward) * settings.SlopeInfluence * rolling);
            return (felt - tangential) * assist;
        }

        /// <summary>
        /// Velocity change that swings the in-plane velocity toward the heading (forward or backward, whichever is
        /// closer) without changing its magnitude: turns keep their speed, and a sideways landing straightens out.
        /// </summary>
        public static Vector3 GripVelocityChange(Vector3 velocity, Vector3 normal, Vector3 forward, float gripRate,
            float deltaTime)
        {
            Vector3 planar = velocity - normal * Vector3.Dot(velocity, normal);
            float speed = planar.magnitude;
            if (speed < 1e-4f)
            {
                return Vector3.zero;
            }

            Vector3 heading = Vector3.Dot(planar, forward) >= 0f ? forward : -forward;
            float angle = Vector3.Angle(planar, heading) * Mathf.Deg2Rad;
            float turn = angle * (1f - Mathf.Exp(-gripRate * deltaTime));
            Vector3 aligned = Vector3.RotateTowards(planar, heading * speed, turn, 0f);
            return aligned - planar;
        }

        /// <summary>Extra downward acceleration (m/s^2) on top of world gravity while airborne.</summary>
        public static float ExtraAirGravity(GroundSettings settings, float verticalVelocity, float worldGravity)
        {
            float total = verticalVelocity > 0f ? settings.AirRiseGravity : settings.AirFallGravity;
            return total - Mathf.Abs(worldGravity);
        }

        /// <summary>
        /// Classifies a driveable collider into <see cref="GroundSurface.Regolith"/>, <see cref="GroundSurface.Rock"/>,
        /// or <see cref="GroundSurface.Metal"/>. Cached by instance ID in PlayMode so per-frame queries allocate 0 B.
        /// </summary>
        public static GroundSurface ClassifySurface(Collider collider)
        {
            if (collider == null)
            {
                return GroundSurface.Regolith;
            }

            int id = collider.GetInstanceID();
            if (Application.isPlaying && SurfaceCache.TryGetValue(id, out GroundSurface cached))
            {
                return cached;
            }

            GameObject go = collider.gameObject;
            int layer = go.layer;
            GroundSurface surface = ClassifySurface(go.name, layer);
            if (surface != GroundSurface.Metal)
            {
                Transform parent = collider.transform.parent;
                if (parent != null)
                {
                    GroundSurface parentSurface = ClassifySurface(parent.name, layer);
                    if (parentSurface == GroundSurface.Metal
                        || (surface == GroundSurface.Regolith && parentSurface == GroundSurface.Rock))
                    {
                        surface = parentSurface;
                    }
                }
            }

            if (Application.isPlaying)
            {
                SurfaceCache[id] = surface;
            }

            return surface;
        }

        /// <summary>
        /// Pure surface classification from a collider or node <paramref name="name"/> and Unity <paramref name="layer"/>.
        /// </summary>
        public static GroundSurface ClassifySurface(string name, int layer)
        {
            if (!string.IsNullOrEmpty(name))
            {
                for (int i = 0; i < MetalKeywords.Length; i++)
                {
                    if (name.IndexOf(MetalKeywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return GroundSurface.Metal;
                    }
                }

                for (int i = 0; i < RockKeywords.Length; i++)
                {
                    if (name.IndexOf(RockKeywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return GroundSurface.Rock;
                    }
                }
            }

            return layer == Layers.Prop ? GroundSurface.Rock : GroundSurface.Regolith;
        }

        /// <summary>Distinct sound hook identifier for <paramref name="surface"/> ("regolith", "rock", "metal").</summary>
        public static string SurfaceSoundHook(GroundSurface surface)
        {
            switch (surface)
            {
                case GroundSurface.Metal:
                    return "metal";
                case GroundSurface.Rock:
                    return "rock";
                default:
                    return "regolith";
            }
        }
    }
}
