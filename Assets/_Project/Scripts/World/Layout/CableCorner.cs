using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Where the old cable's one bend lies (opening v2 §4): the cable runs from 07's post along the crew tracks,
    /// turns through Art's bend and runs a set length straight into the plug at the lander's port. Given the post,
    /// the socket, the bend's turn and chord and the plug's reach, finds the bend's start and the heading the long run
    /// arrives on. World XZ; angles clockwise seen from above, as Unity's yaw. Plain logic.
    /// </summary>
    public static class CableCorner
    {
        private const int Iterations = 8;

        /// <param name="post">Where the cable leaves 07's post.</param>
        /// <param name="socket">The port's socket the plug goes into.</param>
        /// <param name="turn">The bend's turn, degrees (positive: to the right).</param>
        /// <param name="chord">The bend's chord, start to end, metres.</param>
        /// <param name="reach">How far the plug reaches flat from its cable end into the socket, metres.</param>
        /// <param name="approach">How long the straight run from the bend to the plug is, metres.</param>
        /// <param name="bendStart">Where the bend starts: the long run from the post ends here.</param>
        /// <param name="heading">The long run's heading into the bend (unit).</param>
        public static void Solve(Vector2 post, Vector2 socket, float turn, float chord, float reach, float approach,
            out Vector2 bendStart, out Vector2 heading)
        {
            heading = (socket - post).normalized;
            bendStart = post;
            for (int i = 0; i < Iterations; i++)
            {
                Vector2 into = Rotate(heading, turn);
                bendStart = socket - into * (reach + approach) - Rotate(heading, turn * 0.5f) * chord;
                heading = (bendStart - post).normalized;
            }
        }

        /// <summary><paramref name="direction"/> turned <paramref name="degrees"/> clockwise seen from above.</summary>
        public static Vector2 Rotate(Vector2 direction, float degrees)
        {
            Vector3 turned = Quaternion.Euler(0f, degrees, 0f) * new Vector3(direction.x, 0f, direction.y);
            return new Vector2(turned.x, turned.z);
        }
    }
}
