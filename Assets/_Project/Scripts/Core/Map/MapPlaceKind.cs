namespace MoonProject.Core.Map
{
    /// <summary>What kind of place a <see cref="MapPlace"/> is: how the paper draws its outline.</summary>
    public enum MapPlaceKind
    {
        /// <summary>Home: the lander and its stations ("place.lander").</summary>
        Home = 0,

        /// <summary>A salvage site ("site.*").</summary>
        Site = 1,

        /// <summary>A relay mast ("relay.*"): a pencil cross while dark.</summary>
        Relay = 2,

        /// <summary>A place along Whispering Canyon ("canyon.*").</summary>
        Canyon = 3,

        /// <summary>A friend's site ("friend.&lt;id&gt;").</summary>
        Friend = 4,

        /// <summary>A trace on the ground, such as Kestrel-3's skid ("trail.*").</summary>
        Trail = 5,

        /// <summary>A crew landmark on the basin floor, such as Ro's antennas.</summary>
        Landmark = 6,

        /// <summary>Beyond the rim ("region.*"): pencil notes at the paper's edge only, never inked in 0.5.</summary>
        Region = 7,
    }
}
