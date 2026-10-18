namespace MoonProject.Gameplay
{
    /// <summary>
    /// Says once per hold of E that the verb is blocked: the first blocked frame of a hold reports, later ones do not,
    /// and letting go arms it again. Pure logic.
    /// </summary>
    public sealed class BlockNotice
    {
        private bool _reported;

        /// <summary>True on the first frame of this hold that <paramref name="blocked"/> is true.</summary>
        public bool Report(bool held, bool blocked)
        {
            if (!held)
            {
                _reported = false;
                return false;
            }

            if (!blocked || _reported)
            {
                return false;
            }

            _reported = true;
            return true;
        }
    }
}
