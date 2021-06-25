using CitizenFX.Core;
using koth_server.User;
using System;
using System.Collections.Generic;

namespace koth_server
{
    internal class KothTeam : IEquatable<KothTeam>
    {
        public int team_id { get; }
        public string team_name { get; }
        public bool is_full { get; private set; }
        public int members_on_hill { get; private set; }
        public int team_points { get; private set; }
        public TeamBase team_base;
        public List<KothPlayer> players = new();
        public uint team_uniform = 0;

        public KothTeam()
        {
            team_id = 0;
            team_name = "";
        }

        public KothTeam ( int _team_id, string _team_name, TeamBase _team_base, uint _team_uniform )
        {
            team_id = _team_id;
            team_name = _team_name;
            members_on_hill = 0;
            team_points = 0;
            is_full = false;
            team_base = _team_base;
            team_uniform = _team_uniform;
        }

        public void AddFlagPoint ( )
        {
            Debug.WriteLine($"Flag point added to {team_name}, total {members_on_hill}.");
            members_on_hill += 1;
        }

        public void AddTeamPoint ( )
        {
            Debug.WriteLine($"Team point added to {team_name}, total {team_points}.");
            team_points += 1;
        }

        public override bool Equals ( object obj )
        {
            return Equals(obj as KothTeam);
        }

        public bool Equals ( KothTeam other )
        {
            return other != null &&
                   team_id == other.team_id;
        }

        public override int GetHashCode ( )
        {
            return 591577740 + team_id.GetHashCode();
        }

        public Spawn GetSpawn ( )
        {
            return team_base.GetSpawnPoint();
        }

        public float[] GetPlayerSpawnLocation()
        {
            return GetSpawn().player_spawn;
        }

        public static bool operator == ( KothTeam first, KothTeam second ) => first is object && second is object && first.team_id == second.team_id;
        public static bool operator != ( KothTeam first, KothTeam second ) => first is object && second is object && first.team_id != second.team_id;
    }
}
