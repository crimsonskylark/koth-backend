using System;
using CitizenFX.Core;

namespace Server.Events
{
    internal class Game
    {
        internal static void OnPlayerDropped([FromSource] Player player, string reason)
        {
            var p = GameSession.GetPlayerByPlayerObj(player);

            if (p != null)
            {
                p.LeaveTime = DateTime.UtcNow;

                Debug.WriteLine($"Player {p.CfxPlayer.Handle} has left the server at {p.LeaveTime}. (Reason: {reason})");

                GameSession.Match.LeaveTeam(p);

                GameSession.Match.QueueMatchUpdate(new 
                { 
                    type = "game_state_update", 
                    update_type = GameState.PlayerLeave, 
                    name = player.Name 
                });

                Koth.OnPlayerOutsideSafeZone(player);
                Koth.OnPlayerOutsideCombatZone(player);

                if (GameSession.RemovePlayerFromPlayerList(player))
                {
                    Debug.WriteLine($"Removed player {p.CfxPlayer.Handle} from player list.");
                }
                else
                {
                    Debug.WriteLine($"Failed to remove {p.CfxPlayer.Handle} from player list.");
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