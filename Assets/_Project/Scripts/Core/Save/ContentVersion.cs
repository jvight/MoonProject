using System;

namespace MoonProject.Core.Save
{
    /// <summary>
    /// Compares content versions ("0.4.1"): dotted non-negative numbers, compared part by part, a missing part
    /// counting as 0 ("0.4" equals "0.4.0"). Anything else (null, "0.4-beta", "") is not a version and is older than
    /// every version, so a save without a readable content version is never taken as current.
    /// </summary>
    public static class ContentVersion
    {
        /// <summary>True when <paramref name="version"/> is dotted non-negative numbers.</summary>
        public static bool IsValid(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return false;
            }

            foreach (string part in version.Split('.'))
            {
                if (!IsNumber(part))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// True when <paramref name="version"/> is a version no older than <paramref name="floor"/> (a valid version).
        /// </summary>
        public static bool IsAtLeast(string version, string floor)
        {
            if (!IsValid(floor))
            {
                throw new ArgumentException($"'{floor}' is not a content version.", nameof(floor));
            }

            if (!IsValid(version))
            {
                return false;
            }

            string[] a = version.Split('.');
            string[] b = floor.Split('.');
            for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
            {
                int left = i < a.Length ? int.Parse(a[i]) : 0;
                int right = i < b.Length ? int.Parse(b[i]) : 0;
                if (left != right)
                {
                    return left > right;
                }
            }

            return true;
        }

        private static bool IsNumber(string part)
        {
            if (part.Length == 0 || part.Length > 9)
            {
                return false;
            }

            foreach (char c in part)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
