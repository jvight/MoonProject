using MoonProject.Core;

namespace MoonProject.App
{
    /// <summary>
    /// The title and play states and the moves between them (docs/features/M3-09). Pure logic: the bootstrap acts on
    /// what each move answers (enable the controls, reload the scene).
    /// </summary>
    public sealed class GameFlow : IGameFlow
    {
        public GameFlowState State { get; private set; } = GameFlowState.Title;

        /// <summary>Known once the save has loaded, before any Start() runs.</summary>
        public bool HasSave { get; private set; }

        /// <summary>
        /// The save is loaded. A boot armed by New Game (<paramref name="startFresh"/>) goes straight into a fresh game;
        /// a boot without the title (<paramref name="showTitle"/> false) goes straight into play on its save.
        /// </summary>
        public void Loaded(bool hasSave, bool startFresh, bool showTitle)
        {
            HasSave = hasSave && !startFresh;
            State = startFresh || !showTitle ? GameFlowState.Playing : GameFlowState.Title;
        }

        /// <summary>Continue on the title: true when play begins on the loaded save.</summary>
        public bool TryContinue()
        {
            if (State != GameFlowState.Title || !HasSave)
            {
                return false;
            }

            State = GameFlowState.Playing;
            return true;
        }

        /// <summary>
        /// New Game: true when play begins right here (the title over a world with no save is already fresh). False
        /// means the save must be put away and the scene reloaded into a fresh boot.
        /// </summary>
        public bool TryStartHere()
        {
            if (State != GameFlowState.Title || HasSave)
            {
                return false;
            }

            State = GameFlowState.Playing;
            return true;
        }

        /// <summary>"Quit to title": true when play may save and reload into the title.</summary>
        public bool CanQuitToTitle => State == GameFlowState.Playing;
    }
}
