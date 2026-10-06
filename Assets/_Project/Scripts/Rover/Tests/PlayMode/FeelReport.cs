using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>Collects measured feel metrics against their targets and renders them as a Markdown table.</summary>
    public sealed class FeelReport
    {
        private readonly StringBuilder _table = new StringBuilder();
        private readonly List<string> _failures = new List<string>();

        public FeelReport()
        {
            _table.AppendLine("| Metric | Measured | Target | Result |");
            _table.AppendLine("|---|---|---|---|");
        }

        public IReadOnlyList<string> Failures => _failures;

        public string Table => _table.ToString();

        /// <summary>Records a metric that passes within [<paramref name="min"/>, <paramref name="max"/>].</summary>
        public void Add(string metric, float value, string unit, float min, float max, string target)
        {
            bool pass = value >= min && value <= max && !float.IsNaN(value);
            string measured = value.ToString("0.00", CultureInfo.InvariantCulture) + " " + unit;
            _table.AppendLine($"| {metric} | {measured} | {target} | {(pass ? "PASS" : "FAIL")} |");
            if (!pass)
            {
                _failures.Add($"{metric}: {measured} (target {target})");
            }
        }

        /// <summary>Records an informational metric with no target.</summary>
        public void Note(string metric, float value, string unit)
        {
            string measured = value.ToString("0.00", CultureInfo.InvariantCulture) + " " + unit;
            _table.AppendLine($"| {metric} | {measured} | (info) | - |");
        }

        public void Write(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, Table);
            Debug.Log("[rover-metrics]\n" + Table);
        }
    }
}
