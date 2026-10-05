using System;
using System.Reflection;

namespace MoonProject.Editor.Builders
{
    /// <summary>A discovered, validated <see cref="MoonBuilderAttribute"/> method.</summary>
    public sealed class BuilderInfo
    {
        private readonly Action _invoke;

        public BuilderInfo(string path, int order, bool replacesOpenScene, MethodInfo method)
        {
            Path = path;
            Order = order;
            ReplacesOpenScene = replacesOpenScene;
            Method = method ?? throw new ArgumentNullException(nameof(method));
            _invoke = (Action)Delegate.CreateDelegate(typeof(Action), method);
        }

        public string Path { get; }

        public int Order { get; }

        /// <summary>See <see cref="MoonBuilderAttribute.ReplacesOpenScene"/>.</summary>
        public bool ReplacesOpenScene { get; }

        public MethodInfo Method { get; }

        /// <summary>"Namespace.Type.Method" for logs.</summary>
        public string MethodName => $"{Method.DeclaringType?.FullName}.{Method.Name}";

        public void Invoke()
        {
            _invoke();
        }
    }
}
