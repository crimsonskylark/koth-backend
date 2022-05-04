using CitizenFX.Core;
using Serilog;
using System;
using System.Reflection;

namespace Server.Events
{
    internal class Game
    {
        internal static void OnPlayerDropped([FromSource] Player player, string reason)
        {
            var p = GameSession.GetKothPlayerByPlayerObj(player);
            try
            {
                if (p != null)
                {
                    p.LeaveTime = DateTime.UtcNow;

                    Log.Logger.Information($"Player {p.Citizen.Name} has left the server at {p.LeaveTime}. (Reason: {reason})");

                    GameSession.Match.LeaveTeam(p);

                    GameSession.Match.QueueMatchUpdate(new
                    {
                        type = "game_state_update",
                        update_type = GameState.PlayerJoinLeave,
                        name = player.Name,
                        leaving = true
                    });

                    Koth.OnPlayerOutsideSafeZone(player);
                    Koth.OnPlayerOutsideCombatZone(player);

                    GameSession.SavePlayer(p);

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
            catch (Exception ex)
            {
                Log.Logger.Error($"[{MethodBase.GetCurrentMethod().Name}] Exception: { ex.Message }");
            }
        }
    }
}