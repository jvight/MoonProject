using System;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Reads the Art site contract's node names (docs/features/M3-13): a site piece is
    /// "Salvage_&lt;n&gt;_&lt;Material&gt;" or "Salvage_&lt;n&gt;_&lt;Material&gt;_Drag", a loose trail bit is
    /// "Debris_&lt;Material&gt;_&lt;n&gt;". Pure string work, run at initialisation and by the economy test.
    /// </summary>
    public static class SalvagePieceName
    {
        public const string PiecePrefix = "Salvage_";
        public const string DebrisPrefix = "Debris_";
        public const string DragSuffix = "Drag";

        private const char Separator = '_';

        /// <summary>
        /// True for a site piece's name, with its number, material and whether it must be dragged clear first.
        /// </summary>
        public static bool TryParsePiece(string name, out int number, out SalvageMaterial material, out bool drag)
        {
            number = -1;
            material = SalvageMaterial.Metal;
            drag = false;
            if (name == null || !name.StartsWith(PiecePrefix, StringComparison.Ordinal))
            {
                return false;
            }

            string[] parts = name.Substring(PiecePrefix.Length).Split(Separator);
            if (parts.Length < 2 || parts.Length > 3 || !int.TryParse(parts[0], out number) || number < 0 ||
                !TryParseMaterial(parts[1], out material))
            {
                number = -1;
                return false;
            }

            if (parts.Length == 3)
            {
                if (!string.Equals(parts[2], DragSuffix, StringComparison.Ordinal))
                {
                    number = -1;
                    return false;
                }

                drag = true;
            }

            return true;
        }

        /// <summary>True for a loose trail bit's name ("Debris_Optics_1"), with its material.</summary>
        public static bool TryParseDebris(string name, out SalvageMaterial material)
        {
            material = SalvageMaterial.Metal;
            if (name == null || !name.StartsWith(DebrisPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            string[] parts = name.Substring(DebrisPrefix.Length).Split(Separator);
            return parts.Length == 2 && int.TryParse(parts[1], out int number) && number >= 0 &&
                   TryParseMaterial(parts[0], out material);
        }

        private static bool TryParseMaterial(string text, out SalvageMaterial material)
        {
            switch (text)
            {
                case nameof(SalvageMaterial.Metal):
                    material = SalvageMaterial.Metal;
                    return true;
                case nameof(SalvageMaterial.Wiring):
                    material = SalvageMaterial.Wiring;
                    return true;
                case nameof(SalvageMaterial.Optics):
                    material = SalvageMaterial.Optics;
                    return true;
                default:
                    material = SalvageMaterial.Metal;
                    return false;
            }
        }
    }
}
