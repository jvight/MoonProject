namespace MoonProject.Editor.Builders
{
    /// <summary>Outcome of one builder run.</summary>
    public readonly struct BuilderResult
    {
        public BuilderResult(string path, bool succeeded, double seconds, int errorCount, string firstError)
        {
            Path = path;
            Succeeded = succeeded;
            Seconds = seconds;
            ErrorCount = errorCount;
            FirstError = firstError;
        }

        public string Path { get; }

        public bool Succeeded { get; }

        public double Seconds { get; }

        /// <summary>Errors, exceptions and failed asserts logged while the builder ran.</summary>
        public int ErrorCount { get; }

        public string FirstError { get; }
    }
}
