using System.Collections.Generic;
using MoonProject.Editor.Builders;

namespace MoonProject.Editor.Automation
{
    /// <summary>
    /// Batch entry points for content builders.
    /// <code>
    /// python tools/unity_batch.py exec --method MoonProject.Editor.Automation.BuildAutomation.RunBuilders
    ///     [--arg filter=REGEX]   only builders whose path matches (case-insensitive), e.g. filter=^Art/
    /// python tools/unity_batch.py exec --method MoonProject.Editor.Automation.BuildAutomation.ListBuilders
    /// </code>
    /// </summary>
    public static class BuildAutomation
    {
        public static void RunBuilders()
        {
            BatchRunner.Run(nameof(RunBuilders), args => BuilderRegistry.DiscoverAndRun(args.GetString("filter", "")));
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
