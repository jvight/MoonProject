using System;
using UnityEngine.InputSystem;

namespace MoonProject.Autoplay
{
    /// <summary>Maps the key cap a prompt chip shows ("E", "TAB", "SPACE") to the key the bot presses.</summary>
    public static class GlyphKeys
    {
        public static bool TryResolve(string glyph, out Key key)
        {
            key = Key.None;
            if (string.IsNullOrWhiteSpace(glyph))
            {
                return false;
            }

            string cap = glyph.Trim();
            if (cap.Length == 1 && cap[0] >= 'a' && cap[0] <= 'z' || cap.Length == 1 && cap[0] >= 'A' && cap[0] <= 'Z')
            {
                return Enum.TryParse(cap.ToUpperInvariant(), out key);
            }

            switch (cap.ToUpperInvariant())
            {
                case "TAB":
                    key = Key.Tab;
                    return true;
                case "SPACE":
                    key = Key.Space;
                    return true;
                case "ENTER":
                case "RETURN":
                    key = Key.Enter;
                    return true;
                case "ESC":
                case "ESCAPE":
                    key = Key.Escape;
                    return true;
                default:
                    return false;
            }
        }
    }
}
