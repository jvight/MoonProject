using System;

namespace MoonProject.Editor.Builders
{
    /// <summary>
    /// Registers a <c>static void Method()</c> in any editor assembly as a content builder. Builders appear under
    /// <c>MoonProject/Build/&lt;path&gt;</c>, run in <see cref="Order"/> (then path) order from
    /// <c>MoonProject/Build/Build All</c>, and from batch via
    /// <c>unity_batch.py exec --method MoonProject.Editor.Automation.BuildAutomation.RunBuilders</c>.
    /// <para>A builder must be deterministic (seeded, no time/random state) and idempotent (re-running it rewrites
    /// the same assets in place; use <see cref="GeneratedAssets"/> so GUIDs survive). Report problems with
    /// <c>Debug.LogError</c> or by throwing: either fails the run.</para>
    /// <para>Order bands: Art 100-199, World 200-299, Rover 300-399, Audio 400-499, Gameplay 500-599, UI 600-699,
    /// scenes 1000+. Path = "&lt;Domain&gt;/&lt;Thing&gt;", e.g. "Art/Rocks".</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class MoonBuilderAttribute : Attribute
    {
        public MoonBuilderAttribute(string path, int order = 0)
        {
            Path = path;
            Order = order;
        }

        /// <summary>Menu path below MoonProject/Build, '/'-separated, unique across the project.</summary>
        public string Path { get; }

        /// <summary>Lower runs first; builders that consume other builders' output use a higher order.</summary>
        public int Order { get; }
    }
}
