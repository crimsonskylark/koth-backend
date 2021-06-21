using CitizenFX.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;

using static CitizenFX.Core.Native.API;

namespace koth_server
{
    class Server : BaseScript
    {
        Dictionary<Player, KothPlayer> players = new Dictionary<Player, KothPlayer>();
        List<KothTeam> teams = new List<KothTeam>();
        public Server()
        {
            Debug.WriteLine("server main started!");
            Debug.WriteLine("setting up stuff");
            teams.Add(new KothTeam(1, "AEGIS Corp.", new Vector2(0.0f, 0.0f)));
            teams.Add(new KothTeam(2, "FARC", new Vector2(0.0f, 0.0f)));
            teams.Add(new KothTeam(3, "Delta Force", new Vector2(0.0f, 0.0f)));
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

            team.Join(players[player]);

            Debug.WriteLine($"teamJoin called by {player.Identifiers} with team id {team_id}");

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
    }
}
