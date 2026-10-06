using System.Collections.Generic;
using System.IO;
using System.Text;
using MoonProject.Editor.Builders;

namespace MoonProject.Editor.Automation
{
    /// <summary>
    /// Batch entry points for content builders.
    /// <code>
    /// python tools/unity_batch.py exec --method MoonProject.Editor.Automation.BuildAutomation.RunBuilders
    ///     [--arg filter=REGEX]   only builders whose path matches (case-insensitive), e.g. filter=^Art/
    /// python tools/unity_batch.py exec --method MoonProject.Editor.Automation.BuildAutomation.ListBuilders
    /// python tools/unity_batch.py exec --method MoonProject.Editor.Automation.BuildAutomation.CheckIdempotency
    ///     [--arg filter=REGEX]   builds twice, requires byte-identical Generated/ and Scenes/ (see IdempotencyCheck)
    /// </code>
    /// </summary>
    public static class BuildAutomation
    {
        private const char LineBreak = '\n';

        public static void RunBuilders()
        {
            BatchRunner.Run(nameof(RunBuilders), args => BuilderRegistry.DiscoverAndRun(args.GetString("filter", "")));
        }

        public static void CheckIdempotency()
        {
            BatchRunner.Run(nameof(CheckIdempotency), args =>
            {
                var report = new StringBuilder();
                bool idempotent = IdempotencyCheck.Run(args.GetString("filter", ""), report);
                string text = report.ToString();
                foreach (string line in text.Split(LineBreak))
                {
                    if (line.Trim().Length > 0)
                    {
                        BatchRunner.Log("idempotency: " + line.TrimEnd());
                    }
                }

                Directory.CreateDirectory(args.OutputDirectory);
                File.WriteAllText(Path.Combine(args.OutputDirectory, "idempotency.txt"), text);
                return idempotent;
            });
        }

        public static void ListBuilders()
        {
            BatchRunner.Run(nameof(ListBuilders), args =>
            {
                IReadOnlyList<BuilderInfo> builders = BuilderRegistry.Discover(out IReadOnlyList<string> problems);
                foreach (BuilderInfo builder in builders)
                {
                    BatchRunner.Log($"builder {builder.Order,5}  {builder.Path}  ({builder.MethodName})");
                }

                foreach (string problem in problems)
                {
                    UnityEngine.Debug.LogError(problem);
                }

                BatchRunner.Log($"{builders.Count} builder(s) registered");
                return problems.Count == 0;
            });
        }
    }
}
