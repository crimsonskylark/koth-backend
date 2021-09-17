using System;
using System.Dynamic;
using CitizenFX.Core;

namespace Server.Events
{
    internal static class Game
    {
        internal static void OnPlayerDropped ( [FromSource] Player player, string reason )
        {
            var p = Server.GetPlayerByPlayerObj(player);

            if (p != null)
            {
                p.LeaveTime = DateTime.UtcNow;

                Debug.WriteLine($"Player {p.Base.Handle} has left the server at {p.LeaveTime}. (Reason: {reason})");

                p.LeaveTeam();

                if (Server.RemovePlayerFromPlayerList(player))
                {
                    Debug.WriteLine($"Removed player {p.Base.Handle} from player list.");
                }
                else
                {
                    Debug.WriteLine($"Failed to remove {p.Base.Handle} from player list.");
                }
                /* TODO: Save player information in database */
            }
            else
            {
                /* Should never be reached in production. */
                Debug.WriteLine($"[!!!] Player not found.");
            }
        }

        internal static void OnPlayerKilled ( [FromSource] Player player, int killerType, ExpandoObject obj )
        {
            Debug.WriteLine("Player killed");
            foreach (var v in obj)
            {
                Debug.WriteLine($"Key: {v.Key} value: {v.Value}");
            }
        }
    }
}