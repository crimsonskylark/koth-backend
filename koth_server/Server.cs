using CitizenFX.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
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
        Dictionary<Player, KothPlayer> players = new Dictionary<Player, KothPlayer>();
        List<KothTeam> teams = new List<KothTeam>();
        readonly string[] update_endpoints = { "player_join", "player_leave", "team_join", "team_leave", "player_death", "player_kill", "flag_point", "team_point" };
        public Server()
        {
            Debug.WriteLine("server main started!");
            Debug.WriteLine("setting up stuff");
            teams.Add(new KothTeam(0, "", new Vector3()));
            teams.Add(new KothTeam(1, "AEGIS Corp.", new Vector3(1523.32f, 2250.2f, 189.0f)));
            teams.Add(new KothTeam(2, "FARC", new Vector3()));
            teams.Add(new KothTeam(3, "Delta Force", new Vector3()));
        }

        [EventHandler("playerJoining")]
        void onPlayerJoining([FromSource] Player player, string old_id)
        {
            Debug.WriteLine($"Player {player.Handle} has joined the server.");
            players.Add(player, new KothPlayer(player));
        }

        [EventHandler("playerDropped")]
        void onPlayerDropped([FromSource] Player player, string reason)
        {
            if (players.TryGetValue(player, out KothPlayer p))
            {
                p.leave_time = DateTime.UtcNow;
                Debug.WriteLine($"Player {player.Handle} has left the server at {p.leave_time}. (Reason: {reason})");
                players.Remove(player);
            }
            else
            {
                Debug.WriteLine($"[!!!] Player not found.");
            }
        }

        [EventHandler("baseevents:onPlayerKilled")]
        private void onPlayerKilled([FromSource] Player player, int killerType, ExpandoObject obj)
        {
            Debug.WriteLine("Player killed");
            foreach (var v in obj)
            {
                Debug.WriteLine($"Key: {v.Key} value: {v.Value}");
            }
        }

        [EventHandler("koth:teamJoin")]
        private void onTeamJoin([FromSource] Player player, string team_id)
        {
            var valid_team = int.TryParse(team_id, out int int_teamid);
            if (string.IsNullOrEmpty(team_id) || !valid_team || int_teamid < 0 || int_teamid > 3)
            {
                Debug.WriteLine($"Invalid team: {int_teamid}");
                return;
            }

            var team = teams.Find((t) => t.team_id == int_teamid);

            players[player].JoinTeam(team);

            Debug.WriteLine($"teamJoin called by {player.Identifiers} with team id {team_id}");
            /* x */                 /* y */              /* heading */
            player.TriggerEvent("koth:playerJoinedTeam", team.spawn_region.X, team.spawn_region.Y, team.spawn_region.Z, GetHashKey("a_f_m_fatbla_01"));
            TriggerClientEvent("chat:addMessage", new { args = "You have spawned! Congrats!" });
        }

        [EventHandler("onResourceStart")]
        void onResourceStart(string name)
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

        void UpdateGameState(string new_state, STATE_UPDATE_TYPE type)
        {
            
        }
    }
}
