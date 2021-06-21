using CitizenFX.Core;
using System;
using System.Collections.Generic;

namespace koth_server
{
    internal class KothTeam : BaseScript, IEquatable<KothTeam>
    {
        public int team_id { get; }
        public string team_name { get; }
        public bool is_full { get; private set; }
        public int flag_points { get; private set; }
        public int team_points { get; private set; }
        public Vector2 spawn_region;
        public List<KothPlayer> players = new List<KothPlayer>();
        public KothTeam(int id, string name, Vector2 session_spawn)
        {
            team_id = id;
            team_name = name;
            flag_points = 0;
            team_points = 0;
            is_full = false;
            spawn_region = session_spawn;
        }

        public bool Join(KothPlayer p)
        {
            if (p.curr_team != null && p.curr_team.team_id != team_id)
            {
                if (p.curr_team.team_id != 0)
                {
                    p.curr_team.Leave(p);
                }
                Debug.WriteLine($"Player p.base_player != null &&{p.base_player.Name} joined {team_name}");
                p.SetTeam(this);
                players.Add(p);
                TriggerClientEvent(p.base_player, "koth:updateTeamCount", team_id, players.Count);
                return true;
            }
            return false;
        }

        public bool Leave(KothPlayer p)
        {
            if (p.curr_team != null && p.curr_team == this)
            {
                Debug.WriteLine($"Player p.base_player != null &&{p.base_player.Name} left team {team_name}.");
                players.Remove(p);
                p.SetTeam(null);
                return true;
            }
            return false;
        }

        public void AddFlagPoint()
        {
            Debug.WriteLine($"Flag point added to {team_name}, total {flag_points}.");
            flag_points += 1;
        }

        public void AddTeamPoint()
        {
            Debug.WriteLine($"Team point added to {team_name}, total {team_points}.");
            team_points += 1;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as KothTeam);
        }

        public bool Equals(KothTeam other)
        {
            return other != null &&
                   team_id == other.team_id;
        }

        public override int GetHashCode()
        {
            return 591577740 + team_id.GetHashCode();
        }

        public static bool operator ==(KothTeam first, KothTeam second) => (object)first != null && (object)second != null && first.team_id == second.team_id;
        public static bool operator !=(KothTeam first, KothTeam second) => (object)first != null && (object)second != null && first.team_id != second.team_id;
    }
}
