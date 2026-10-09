namespace MoonProject.Rover
{
    /// <summary>What <see cref="FriendGift.Step"/> asks for this frame.</summary>
    public enum GiftCue
    {
        None,

        /// <summary>The gift was given before this session: show it at once, no moment.</summary>
        ShowSilently,

        /// <summary>07 just came home after the friend was repaired: present the gift with the soft moment.</summary>
        Present,
    }
}
