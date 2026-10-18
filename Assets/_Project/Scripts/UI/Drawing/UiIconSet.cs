using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>
    /// The catalog of all 30 inventory and equipment vector icons (<c>item_*.svg</c> and <c>kit_*.svg</c>), built from
    /// <c>Assets/_Project/UI/Icons/</c> by <c>UI/Icons</c> and drawn with <see cref="UiIconPainter"/>. Looking up an
    /// unknown id throws immediately so missing icons never fail silently.
    /// </summary>
    public sealed class UiIconSet : ScriptableObject
    {
        public const int ExpectedIconCount = 30;

        [SerializeField] private UiIconShape[] _icons = Array.Empty<UiIconShape>();

        public int Count => _icons != null ? _icons.Length : 0;

        public UiIconShape[] Icons => _icons;

        internal void Set(UiIconShape[] icons)
        {
            _icons = icons ?? throw new ArgumentNullException(nameof(icons));
        }

        /// <summary>Null when all 30 icons are present and valid; otherwise what is wrong.</summary>
        public string Validate()
        {
            return ValidateArray(_icons);
        }

        internal static string ValidateArray(UiIconShape[] icons)
        {
            if (icons == null || icons.Length != ExpectedIconCount)
            {
                return $"expected {ExpectedIconCount} icons, found {(icons == null ? 0 : icons.Length)}";
            }

            for (int i = 0; i < icons.Length; i++)
            {
                string problem = icons[i].Validate();
                if (problem != null)
                {
                    return problem;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the icon for <paramref name="id"/> (accepting either full <c>item_metal</c>/<c>kit_wing</c> or bare
        /// <c>metal</c>/<c>wing</c>). Throws <see cref="InvalidOperationException"/> loudly if no icon matches.
        /// </summary>
        public UiIconShape GetIcon(string id)
        {
            return FindInArray(_icons, id);
        }

        internal static UiIconShape FindInArray(UiIconShape[] icons, string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new InvalidOperationException("Cannot look up a UI icon for an empty or null id.");
            }

            if (icons != null)
            {
                for (int i = 0; i < icons.Length; i++)
                {
                    string candidate = icons[i].Id;
                    if (string.Equals(candidate, id, StringComparison.OrdinalIgnoreCase) ||
                        (candidate.StartsWith("item_", StringComparison.Ordinal) &&
                         string.Equals(candidate.Substring(5), id, StringComparison.OrdinalIgnoreCase)) ||
                        (candidate.StartsWith("kit_", StringComparison.Ordinal) &&
                         string.Equals(candidate.Substring(4), id, StringComparison.OrdinalIgnoreCase)))
                    {
                        return icons[i];
                    }
                }
            }

            throw new InvalidOperationException($"No UI icon exists for id '{id}'.");
        }

        /// <summary>
        /// The icon id for <paramref name="material"/>: the face's own drawing (<c>icon_*</c>, see
        /// <see cref="RoverDiagramData.FaceIconIds"/>) where Art made one, else the SVG icon (<c>item_*</c>).
        /// </summary>
        public static string IconIdOf(SalvageMaterial material)
        {
            switch (material)
            {
                case SalvageMaterial.Metal: return "icon_metal_plates";
                case SalvageMaterial.Wiring: return "icon_wiring_spool";
                case SalvageMaterial.Optics: return "item_optics";
                case SalvageMaterial.CircuitChips: return "item_circuit_chips";
                case SalvageMaterial.OrangeBeads: return "item_orange_beads";
                case SalvageMaterial.Ilmenite: return "item_ilmenite";
                case SalvageMaterial.WaterIce: return "item_water_ice";
                case SalvageMaterial.RareEarths: return "item_rare_earths";
                case SalvageMaterial.Glass: return "item_glass";
                case SalvageMaterial.Alloy: return "item_alloy";
                case SalvageMaterial.Coolant: return "item_coolant";
                case SalvageMaterial.Crystal: return "item_crystal";
                case SalvageMaterial.Superconductor: return "item_superconductor";
                default:
                    throw new InvalidOperationException($"SalvageMaterial '{material}' has no icon id.");
            }
        }

        /// <summary>The face icon id (<see cref="RoverDiagramData.FaceIconIds"/>) for story part <paramref name="partId"/>.</summary>
        public static string IconIdOfPart(string partId)
        {
            if (string.IsNullOrEmpty(partId))
            {
                throw new InvalidOperationException("Story part id cannot be null or empty.");
            }

            switch (partId)
            {
                case "part.igniter":
                    return "icon_spare_igniter";
                case "part.icecore":
                    return "icon_ice_core_tube";
                default:
                    throw new InvalidOperationException($"Story part '{partId}' has no icon id.");
            }
        }
    }
}
