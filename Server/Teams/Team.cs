using System.Collections.Generic;
using CitizenFX.Core;
using Server.Map;
using Server.User;

namespace Server
{
    internal class Team
    {
        public int Id { get; }
        public string Name { get; }
        public bool Full { get; private set; } = false;
        public int PlayersOnHill { get; private set; } = 0;
        public int Points { get; private set; } = 0;
        public TeamZone Zone;
        public List<ServerPlayer> Players = new();

        public Team ( )
        {
            Id = -1;
            Name = "";
        }

        public Team ( int _team_id, string _team_name, TeamZone _zone )
        {
            Id = _team_id;
            Name = _team_name;
            PlayersOnHill = 0;
            Points = 0;
            Full = false;
            Zone = _zone;
        }

        internal void AddFlagPoint ( )
        {
            PlayersOnHill += 1;
            Debug.WriteLine($"Flag point added to {Name}, total {PlayersOnHill}.");
        }

        internal void SetFlagPoints(int newval)
        {
            PlayersOnHill = newval;
        }

        internal void AddTeamPoint ( )
        {
            Points += 1;
            Debug.WriteLine($"Team point added to {Name}, total {Points}.");
        }

        internal void RemoveFlagPoint()
        {
            PlayersOnHill -= 1;
            Debug.WriteLine($"Flag point added to {Name}, total {PlayersOnHill}.");
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