namespace MoonProject.Gameplay
{
    /// <summary>
    /// Which base prefab carries a friend's home socket. Serialized in content assets: append, never renumber.
    /// </summary>
    public enum FriendHome
    {
        /// <summary>A socket on the lander (Tilly's perch, FriendSocket_&lt;id&gt;).</summary>
        Lander = 0,

        /// <summary>
        /// A socket on the radio tower (Bell's corner, BellCorner). Every tower stage carries it at the same spot;
        /// the scene build pins it under the tower anchor so stage swaps never move a friend's home.
        /// </summary>
        RadioTower = 1,
    }
}
