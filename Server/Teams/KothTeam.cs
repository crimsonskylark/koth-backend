using CitizenFX.Core;
using koth_server.Map;
using Server.User;
using System;
using System.Collections.Generic;

namespace Server
{
    internal class KothTeam : IEquatable<KothTeam>
    {
        public int Id { get; }
        public string Name { get; }
        public bool Full { get; private set; } = false;
        public int PlayersOnHill { get; private set; } = 0;
        public int Points { get; private set; } = 0;
        public TeamZone Zone;
        public List<KothPlayer> Players = new( );
        public Dictionary<int, IPlayerClass> PlayerClasses = new( );
        public KothTeam ( )
        {
            Id = 0;
            Name = "";
        }

        public KothTeam ( int _team_id, string _team_name, TeamZone zone )
        {
            Id = _team_id;
            Name = _team_name;
            PlayersOnHill = 0;
            Points = 0;
            Full = false;
            Zone = zone;
        }

        public void AddFlagPoint ( )
        {
            Debug.WriteLine( $"Flag point added to {Name}, total {PlayersOnHill}." );
            PlayersOnHill += 1;
        }

        public void AddTeamPoint ( )
        {
            Debug.WriteLine( $"Team point added to {Name}, total {Points}." );
            Points += 1;
        }

        public override bool Equals ( object obj )
        {
            return Equals( obj as KothTeam );
        }

        public bool Equals ( KothTeam other )
        {
            return other != null &&
                   Id == other.Id;
        }

        public override int GetHashCode ( )
        {
            return 591577740 + Id.GetHashCode( );
        }

        public Spawn GetSpawn ( )
        {
            return new Spawn( );
        }

        public float[] GetPlayerSpawnLocation ( )
        {
            return GetSpawn( ).PlayerSpawn;
        }

        public static bool operator == ( KothTeam first, KothTeam second ) => first is object && second is object && first.Id == second.Id;
        public static bool operator != ( KothTeam first, KothTeam second ) => first is object && second is object && first.Id != second.Id;
    }
}
