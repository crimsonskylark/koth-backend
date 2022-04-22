using System;
using CitizenFX.Core;
using Serilog;

namespace Server.Events
{
    internal class Game
    {
        internal static void OnPlayerDropped([FromSource] Player player, string reason)
        {
            var p = GameSession.GetKothPlayerByPlayerObj(player);

            if (p != null)
            {
                p.LeaveTime = DateTime.UtcNow;

                Log.Logger.Information($"Player {p.Citizen.Handle} has left the server at {p.LeaveTime}. (Reason: {reason})");

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
                    Log.Logger.Debug($"Removed player {p.Citizen.Handle} from player list.");
                }
                else
                {
                    Log.Logger.Debug($"Failed to remove {p.Citizen.Handle} from player list.");
                }
                /* TODO: Save player information in database */
            }
            else
            {
                /* Should never be reached in production. */
                Log.Logger.Error($"[!!!] Player not found.");
            }
        }
    }
}