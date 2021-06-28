using CitizenFX.Core;
using Server.Teams;
using Server.User;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;
using Shared;


namespace Server
{
    enum StateUpdate : int
    {
        PlayerJoin,
        PlayerLeave,
        PlayerDeath,
        PlayerKill,
        PlayerOnHill,
        TeamJoin,
        TeamLeave,
        TeamPoint,
        HillCaptured,
        HillLost,
        HillContested
    }
    class Server : BaseScript
    {
        Dictionary<Player, KothPlayer> ServerPlayers = new();
        List<KothTeam> Teams = new();


        public Server ( )
        {
            Debug.WriteLine("server main started!");
            Debug.WriteLine("setting up stuff");
            Teams.Add(new KothTeam());
            Teams.Add(new AEGIS());
        }

        #region GameEvents
        [EventHandler("playerJoining")]
        void onPlayerJoining ( [FromSource] Player player, string old_id )
        {
            Debug.WriteLine($"Player {player.Handle} has joined the server.");
            ServerPlayers.Add(player, new KothPlayer(player));
        }

        [EventHandler("playerDropped")]
        void onPlayerDropped ( [FromSource] Player player, string reason )
        {
            if (ServerPlayers.TryGetValue(player, out KothPlayer p))
            {
                p.LeaveTime = DateTime.UtcNow;
                Debug.WriteLine($"Player {player.Handle} has left the server at {p.LeaveTime}. (Reason: {reason})");
                ServerPlayers.Remove(player);
            }
            else
            {
                Debug.WriteLine($"[!!!] Player not found.");
            }
        }

        [EventHandler("onResourceStart")]
        void onResourceStart ( string name )
        {
            if (GetCurrentResourceName().Equals(name))
            {
                foreach (var p in Players)
                {
                    Debug.WriteLine($"Adding players to player list after restart.");
                    ServerPlayers.Add(p, new KothPlayer(p));
                }
            }
        }
        #endregion GameEvents

        #region BaseEvents

        [EventHandler("baseevents:onPlayerKilled")]
        private void onPlayerKilled ( [FromSource] Player player, int killerType, ExpandoObject obj )
        {
            Debug.WriteLine("Player killed");
            foreach (var v in obj)
            {
                Debug.WriteLine($"Key: {v.Key} value: {v.Value}");
            }
        }

        #endregion BaseEvents

        #region KOTHEvents

        [EventHandler("koth:teamJoin")]
        private void onTeamJoin ( [FromSource] Player player, string team_id )
        {
            var valid_team = int.TryParse(team_id, out int int_teamid);

            if (string.IsNullOrEmpty(team_id) || !valid_team || int_teamid < 0 || int_teamid > 3)
            {
                Debug.WriteLine($"Invalid team: {int_teamid}");
                return;
            }

            var team = Teams.Find((t) => t.Id == int_teamid);

            if (ServerPlayers[player].JoinTeam(team))
            {
                var teammates = (from p in team.Players
                                 where p.Base != player
                                 select p.Base.Character.Handle).ToArray();

                var spawn = ServerPlayers[player].CurrentTeam.GetSpawn();

                Debug.WriteLine($"Spawn point: {spawn.PlayerSpawn[0]} {spawn.PlayerSpawn[1]} {spawn.PlayerSpawn[2]}");          

                player.TriggerEvent("koth:playerJoinedTeam", spawn.PlayerSpawn, ServerPlayers[player].CurrentTeam.Uniform, spawn.VehiclesDealerCoords, spawn.WeaponsDealerCoords);

                player.TriggerEvent("chat:addMessage", new { args = new[] { $"You are now part of team {team.Name}" } });

                return;
            }

            player.TriggerEvent("chat:addMessage", new { args = new[] { $"Failed to join team {team.Name}" } });
        }

        [EventHandler("koth:playerFinishSetup")]
        void onPlayerFinishSetup ( [FromSource] Player player )
        {
            var _player = ServerPlayers[player];
            var _handle = _player.Base.Character.Handle;

            if (DoesEntityExist(_handle))
            {
                GiveWeaponToPed(_handle,
                                _player.Class.DefaultWeapon,
                                200,
                                false,
                                true);
                SetPedArmour(_handle, 100);
            }
        }

        #endregion KOTHEvents
    }
}
