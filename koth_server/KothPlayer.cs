using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace koth_server
{
    /*
     * Represents a player inside the game.
     * Not to be mistaken with `Player` provided by CFX
     */
    class KothPlayer
    {
        public Player base_player { get; private set; }
        public string player_license { get; private set; }
        public int lifetime_total_kills { get; private set; }
        public int lifetime_total_deaths { get; private set; }
        public int lifetime_total_points { get; private set; }
        public int lifetime_total_money { get; private set; }
        public int session_kills { get; private set; }
        public int session_deaths { get; private set; }
        public int session_money { get; private set; }
#nullable enable
        public KothTeam? curr_team { get; private set; }
#nullable disable
        //public Squad curr_squad { get; private set; }
        public DateTime join_time { get; private set; }
        public DateTime leave_time { get; set; }

        public KothPlayer(Player player)
        {
            base_player = player;
            join_time = DateTime.UtcNow;
            player_license = Utils.GetPlayerLicense(base_player.Identifiers);
            curr_team = null;
            Debug.WriteLine($"Player joined server at {join_time}");
        }

        ~KothPlayer()
        {
            leave_time = DateTime.UtcNow;
            lifetime_total_money += session_money;
            lifetime_total_kills += session_kills;
            lifetime_total_deaths += session_deaths;
            Debug.WriteLine($"Player leaving server at {leave_time}.");
        }

        public void SetTeam(KothTeam t)
        {
            curr_team = t;
            Debug.WriteLine($"Player {base_player.Name} joined {t.team_name} ({t.team_id})");
        }
    }
}
