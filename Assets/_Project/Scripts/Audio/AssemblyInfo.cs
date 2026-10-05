using System.Runtime.CompilerServices;

// The editor builders populate library/playlist assets and wire scene components through internal setters;
// the tests exercise the same paths. Nothing outside the Audio domain sees them.
[assembly: InternalsVisibleTo("MoonProject.Audio.Editor")]
[assembly: InternalsVisibleTo("MoonProject.Audio.Tests")]
[assembly: InternalsVisibleTo("MoonProject.Audio.PlayModeTests")]
