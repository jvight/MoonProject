using System.Collections.Generic;
using System.Text;

namespace MoonProject.UI.Editor
{
    /// <summary>
    /// The characters the UI font atlases are baked with at build time: printable ASCII (including digits), the whole
    /// Vietnamese alphabet, UI symbols and typography, and every character of every string table and credits file. The
    /// baked atlases are static so nothing writes to font assets at runtime.
    /// </summary>
    internal static class FontCharacters
    {
        private const char FirstPrintable = ' ';
        private const char LastPrintable = '~';

        /// <summary>Precomposed Vietnamese letters (both cases).</summary>
        public const string Vietnamese =
            "ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚÝàáâãèéêìíòóôõùúýĂăĐđĨĩŨũƠơƯư" +
            "ẠạẢảẤấẦầẨẩẪẫẬậẮắẰằẲẳẴẵẶặẸẹẺẻẼẽẾếỀềỂểỄễỆệỈỉỊịỌọỎỏỐốỒồỔổỖỗỘộỚớỜờỞởỠỡỢợỤụỦủỨứỪừỬửỮữỰựỲỳỴỵỶỷỸỹ";

        /// <summary>
        /// Typographic marks and UI symbols the copy and HUD use: curly quotes, dashes, ellipsis, multiplication,
        /// middle dot, bullet, angle quotes, degree, copyright, plus-minus, and minus sign.
        /// </summary>
        public const string Typography = "‘’“”–—…×·•‹›°©±−";

        /// <summary>
        /// Every distinct character of the base set and <paramref name="texts"/>, sorted (deterministic).
        /// </summary>
        public static string Collect(IEnumerable<string> texts)
        {
            var characters = new SortedSet<char>();
            for (char c = FirstPrintable; c <= LastPrintable; c++)
            {
                characters.Add(c);
            }

            AddAll(characters, Vietnamese);
            AddAll(characters, Typography);
            foreach (string text in texts)
            {
                AddAll(characters, text);
            }

            var builder = new StringBuilder(characters.Count);
            foreach (char c in characters)
            {
                builder.Append(c);
            }

            return builder.ToString();
        }

        private static void AddAll(SortedSet<char> characters, string text)
        {
            if (text == null)
            {
                return;
            }

            foreach (char c in text)
            {
                if (!char.IsControl(c))
                {
                    characters.Add(c);
                }
            }
        }
    }
}
