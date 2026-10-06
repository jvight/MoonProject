using System.Runtime.CompilerServices;

// Builders and the scene contributor wire components and populate content through internal members; the tests
// exercise the same paths. Nothing outside the Gameplay domain sees them.
[assembly: InternalsVisibleTo("MoonProject.Gameplay.Editor")]
[assembly: InternalsVisibleTo("MoonProject.Gameplay.Tests")]
[assembly: InternalsVisibleTo("MoonProject.Gameplay.PlayModeTests")]
