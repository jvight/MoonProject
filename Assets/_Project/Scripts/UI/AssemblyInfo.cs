using System.Runtime.CompilerServices;

// The scene contributor wires UISystem through internal members and the tests drive the pure UI logic and the
// explicit-services entry point; nothing outside the UI domain sees them.
[assembly: InternalsVisibleTo("MoonProject.UI.Editor")]
[assembly: InternalsVisibleTo("MoonProject.UI.Tests")]
[assembly: InternalsVisibleTo("MoonProject.UI.PlayModeTests")]
