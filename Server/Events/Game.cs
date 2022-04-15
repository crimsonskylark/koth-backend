using System;
using System.Dynamic;
using CitizenFX.Core;

namespace Server.Events
{
    internal class Game
    {
        internal static void OnPlayerDropped ( [FromSource] Player player, string reason )
        {
            var p = Server.GetPlayerByPlayerObj(player);

            if (p != null)
            {
                p.LeaveTime = DateTime.UtcNow;

                Debug.WriteLine($"Player {p.Base.Handle} has left the server at {p.LeaveTime}. (Reason: {reason})");

                p.LeaveTeam();

                Koth.OnPlayerOutsideSafeZone(player);
                Koth.OnPlayerOutsideCombatZone(player);

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
    }
}