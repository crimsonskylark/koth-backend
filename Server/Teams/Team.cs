using System.Collections.Generic;
using CitizenFX.Core;
using Server.Map;
using Server.User;

namespace Server
{
    internal class Team
    {
        internal int Id { get; }
        internal string Name { get; }
        internal bool Full { get; private set; } = false;
        internal int PlayersOnHill { get; private set; } = 0;
        internal int Points { get; private set; } = 0;
        internal TeamZone Zone;
        internal List<KothPlayer> Members;

        private readonly int MAX_MEMBER_COUNT;

        public Team ( )
        {
            Id = -1;
            Name = "";
        }

        public Team ( int _teamId, string _teamName, TeamZone _teamZone, int _maxMemberCount )
        {
            Id = _teamId;
            Name = _teamName;
            PlayersOnHill = 0;
            Points = 0;
            Full = false;
            Zone = _teamZone;
            Members = new(_maxMemberCount);

            MAX_MEMBER_COUNT = _maxMemberCount;
        }

        internal int AddFlagPoint ( )
        {
            PlayersOnHill++;
            Debug.WriteLine($"Flag point added to {Name}, total {PlayersOnHill}.");
            return PlayersOnHill;
        }

        internal int SetFlagPoints(int newval)
        {
            PlayersOnHill = newval;
            Debug.WriteLine($"Set {Name} points to {newval}.");
            return PlayersOnHill;
        }

        internal int AddTeamPoint ( )
        {
            Points++;
            Debug.WriteLine($"Team point added to {Name}, total {Points}.");
            return Points;
        }

        internal int RemoveFlagPoint()
        {
            PlayersOnHill--;
            Debug.WriteLine($"Flag point added to {Name}, total {PlayersOnHill}.");
            return PlayersOnHill;
        }

        internal void SetTeamFullStatus(bool isFull)
        {
            Full = isFull;
        }

        internal Spawn GetSpawn ( )
        {
            return new Spawn();
        }

        internal float[] GetPlayerSpawnLocation ( )
        {
            return GetSpawn().PlayerSpawn;
        }

        internal bool Join(KothPlayer player)
        {
            if (Full)
            {
                Debug.WriteLine($"Player {player.CfxPlayer.Name} failed to join team {Name}. Team is full.");
                return false;
            }

            var currPlayerCount = Members.Count;

            if (currPlayerCount + 1 >= MAX_MEMBER_COUNT)
            {
                Debug.WriteLine($"Team {Name} is now full.");
                Full = true;
            }

            Debug.WriteLine($"Player {player.CfxPlayer.Name} joined team {Name}");

            Members.Add(player);

            player.Team = this;

            player.SafeZoneVec3.X = Zone.SafeZone[0];
            player.SafeZoneVec3.Y = Zone.SafeZone[1];
            player.SafeZoneVec3.Z = Zone.SafeZone[2];

            return true;
        }

        internal bool Leave(KothPlayer player)
        {
            Debug.WriteLine($"Player {player.CfxPlayer.Name} left team {Name}");
            return Members.Remove(player);
        }
    }
}