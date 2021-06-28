using CitizenFX.Core;
using Server.User;
using System;
using System.Collections.Generic;

namespace Server
{
    internal class KothTeam : IEquatable<KothTeam>
    {
        public int Id { get; }
        public string Name { get; }
        public bool Full { get; private set; }
        public int PlayersOnHill { get; private set; }
        public int Points { get; private set; }
        public CommandPost Base;
        public List<KothPlayer> Players = new();
        public uint Uniform = 0;
        public KothTeam()
        {
            Id = 0;
            Name = "";
        }

        public KothTeam ( int _team_id, string _team_name, CommandPost _team_base, uint _team_uniform )
        {
            Id = _team_id;
            Name = _team_name;
            PlayersOnHill = 0;
            Points = 0;
            Full = false;
            Base = _team_base;
            Uniform = _team_uniform;
        }

        public void AddFlagPoint ( )
        {
            Debug.WriteLine($"Flag point added to {Name}, total {PlayersOnHill}.");
            PlayersOnHill += 1;
        }

        public void AddTeamPoint ( )
        {
            Debug.WriteLine($"Team point added to {Name}, total {Points}.");
            Points += 1;
        }

        public override bool Equals ( object obj )
        {
            return Equals(obj as KothTeam);
        }

        public bool Equals ( KothTeam other )
        {
            return other != null &&
                   Id == other.Id;
        }

        public override int GetHashCode ( )
        {
            return 591577740 + Id.GetHashCode();
        }

        public Spawn GetSpawn ( )
        {
            return Base.GetSpawnPoint();
        }

        public float[] GetPlayerSpawnLocation()
        {
            return GetSpawn().PlayerSpawn;
        }

        public static bool operator == ( KothTeam first, KothTeam second ) => first is object && second is object && first.Id == second.Id;
        public static bool operator != ( KothTeam first, KothTeam second ) => first is object && second is object && first.Id != second.Id;
    }
}
