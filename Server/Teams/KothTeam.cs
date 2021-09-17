using System.Collections.Generic;
using CitizenFX.Core;
using koth_server.Map;
using Server.User;

namespace Server
{
    internal class KothTeam
    {
        public int Id { get; }
        public string Name { get; }
        public bool Full { get; private set; } = false;
        public int PlayersOnHill { get; private set; } = 0;
        public int Points { get; private set; } = 0;
        public TeamZone Zone;
        public List<KothPlayer> Players = new();

        public KothTeam ( )
        {
            Id = -1;
            Name = "";
        }

        public KothTeam ( int _team_id, string _team_name, TeamZone _zone )
        {
            Id = _team_id;
            Name = _team_name;
            PlayersOnHill = 0;
            Points = 0;
            Full = false;
            Zone = _zone;
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

        public Spawn GetSpawn ( )
        {
            return new Spawn();
        }

        public float[] GetPlayerSpawnLocation ( )
        {
            return GetSpawn().PlayerSpawn;
        }
    }
}