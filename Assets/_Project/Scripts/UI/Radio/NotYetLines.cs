using System;
using System.Collections.Generic;

namespace MoonProject.UI
{
    /// <summary>
    /// A dormant station's "not yet" line reaches the ticker once per reason (docs/design/polish-pass.md §1.1): the
    /// sonar can ring a sleeper again and again, but its line is said the first time only.
    /// </summary>
    internal sealed class NotYetLines
    {
        private readonly HashSet<string> _said = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>True the first time <paramref name="reasonKey"/> is asked about.</summary>
        public bool ShouldSay(string reasonKey)
        {
            return !string.IsNullOrEmpty(reasonKey) && _said.Add(reasonKey);
        }
    }
}
