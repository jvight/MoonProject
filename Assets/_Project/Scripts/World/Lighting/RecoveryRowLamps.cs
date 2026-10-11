namespace MoonProject.World
{
    /// <summary>
    /// How a recovery target's lamps (docs/features/M3-21) sit in its row of sockets on the lab's recovery panel: lamp
    /// n in socket n, except that on a row whose last socket is the larger end lamp (the Link's, the Peak's foot) the
    /// last lamp always takes that socket and the sockets between stay hidden. Plain logic.
    /// </summary>
    public static class RecoveryRowLamps
    {
        /// <summary>The socket lamp <paramref name="lamp"/> of <paramref name="lamps"/> shows in.</summary>
        public static int Socket(int lamp, int lamps, int sockets, bool endLamp)
        {
            return endLamp && lamp == lamps - 1 ? sockets - 1 : lamp;
        }

        /// <summary>Whether socket <paramref name="socket"/> holds one of the row's <paramref name="lamps"/> lamps.</summary>
        public static bool Shows(int socket, int lamps, int sockets, bool endLamp)
        {
            if (!endLamp || lamps <= 0)
            {
                return socket < lamps;
            }

            return socket < lamps - 1 || socket == sockets - 1;
        }

        /// <summary>Whether socket <paramref name="socket"/>'s lamp is lit with <paramref name="lit"/> lamps lit.</summary>
        public static bool Lit(int socket, int lit, int lamps, int sockets, bool endLamp)
        {
            for (int lamp = 0; lamp < lit && lamp < lamps; lamp++)
            {
                if (Socket(lamp, lamps, sockets, endLamp) == socket)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
