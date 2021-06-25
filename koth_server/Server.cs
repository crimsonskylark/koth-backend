using CitizenFX.Core;
using koth_server.Teams;
using koth_server.User;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace koth_server
{
    enum STATE_UPDATE_TYPE : int
    {
        PLAYER_JOIN,
        PLAYER_LEAVE,
        TEAM_JOIN,
        TEAM_LEAVE,
        PLAYER_DEATH,
        PLAYER_KILL,
        FLAG_POINT,
        TEAM_POINT
    }
    class Server : BaseScript
    {
        Dictionary<Player, KothPlayer> players = new();
        List<KothTeam> teams = new();

        readonly string[] update_endpoints = { "player_join", "player_leave", "team_join", "team_leave", "player_death", "player_kill", "flag_point", "team_point" };
        public Server ( )
        {
            Debug.WriteLine("server main started!");
            Debug.WriteLine("setting up stuff");
            teams.Add(new KothTeam());
            teams.Add(new AEGIS());
        }

        #region GameEvents
        [EventHandler("playerJoining")]
        void onPlayerJoining ( [FromSource] Player player, string old_id )
        {
            Debug.WriteLine($"Player {player.Handle} has joined the server.");
            players.Add(player, new KothPlayer(player));
        }

        [EventHandler("playerDropped")]
        void onPlayerDropped ( [FromSource] Player player, string reason )
        {
            if (players.TryGetValue(player, out KothPlayer p))
            {
                p.LeaveTime = DateTime.UtcNow;
                Debug.WriteLine($"Player {player.Handle} has left the server at {p.LeaveTime}. (Reason: {reason})");
                players.Remove(player);
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
                    players.Add(p, new KothPlayer(p));
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

            var team = teams.Find((t) => t.team_id == int_teamid);

            if (players[player].JoinTeam(team))
            {
                var teammates = (from p in team.players
                                 where p.Base != player
                                 select p.Base.Character.Handle).ToArray();

                var spawn = players[player].CurrentTeam.GetSpawn();

                Debug.WriteLine($"Spawn point: {spawn.player_spawn[0]} {spawn.player_spawn[1]} {spawn.player_spawn[2]}");

                player.TriggerEvent("koth:playerJoinedTeam",
                                    spawn.player_spawn,
                                    players[player].CurrentTeam.team_uniform,
                                    spawn.weapons_dealer,
                                    spawn.vehicles_dealer);

                player.TriggerEvent("chat:addMessage", new { args = new[] { $"You are now part of team {team.team_name}" } });

                return;
            }

            player.TriggerEvent("chat:addMessage", new { args = new[] { $"Failed to join team {team.team_name}" } });
        }

        [EventHandler("koth:playerFinishSetup")]
        void onPlayerFinishSetup ( [FromSource] Player player )
        {
            var _player = players[player];
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
