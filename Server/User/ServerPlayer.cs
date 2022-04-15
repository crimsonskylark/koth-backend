using System;
using CitizenFX.Core;

namespace Server.User
{
    internal class ServerPlayer
    {
        public Player Base { get; private set; }
        public string License { get; private set; }
        public int TotalKills { get; private set; }
        public int TotalDeaths { get; private set; }
        public int TotalPoints { get; private set; }
        public int TotalMoney { get; private set; }
        public int SessionKills { get; private set; }
        public int SessionDeaths { get; private set; }
        public int SessionMoney { get; private set; }
        public int SessionPoints { get; private set; }
        public Team Team { get; private set; } = null;
        public DateTime JoinTime { get; private set; }
        public DateTime LeaveTime { get; set; }
        public float Experience { get; private set; }
        public int Level { get; private set; }
        public bool CanBeRevived { get; private set; }
        public bool IsInsideSafeZone { get; set; } = true;
        public bool IsInsideAO { get; set; }
        

        // cached and used for canculations
        public Vector3 SafeZoneVec3 = new Vector3();
        public ServerPlayer ( Player base_player )
        {
            Base = base_player;
            JoinTime = DateTime.UtcNow;

            License = Utils.GetPlayerLicense(Base.Identifiers);
            Debug.WriteLine($"Licença: {License}");
            Debug.WriteLine($"Player joined server at {JoinTime}");
        }

        ~ServerPlayer ( )
        {
            LeaveTime = DateTime.UtcNow;
            TotalMoney += SessionMoney;
            TotalKills += SessionKills;
            TotalDeaths += SessionDeaths;
            // SavePlayerInformation();
            Debug.WriteLine($"Player leaving server at {LeaveTime}.");
        }

        public bool JoinTeam ( Team t )
        {
            if (!t.Equals(Team))
            {
                if (Team != null)
                    LeaveTeam();

                Team = t;
                t.Players.Add(this);

                Debug.WriteLine($"Player {Base.Name} joined team {Team.Name}");

                SafeZoneVec3.X = Team.Zone.SafeZone[0];
                SafeZoneVec3.Y = Team.Zone.SafeZone[1];
                SafeZoneVec3.Z = Team.Zone.SafeZone[2];

                return true;
            }
            return false;
        }

        public void LeaveTeam ( )
        {
            Debug.WriteLine($"Player {Base.Name} left team {Team.Name}");
            if (Team != null)
                Team.Players.Remove(this);
        }

        internal void AddKill()
        {
            SessionKills += 1;
            Debug.WriteLine($"Added one kill to { Base.Name }, total kills { SessionKills }");
        }

        internal void AddDeath()
        {
            SessionDeaths += 1;
            Debug.WriteLine($"Added one death to { Base.Name }, total kills { SessionDeaths }");
        }

        internal void AddMoneyToAccount(int amount)
        {
            TotalMoney += amount;
            Debug.WriteLine($"Added ${amount} to \"{Base.Name}\"'s account, total ${TotalMoney}");
        }

        internal void AddPointsToPlayer(int amount = 10)
        {
            SessionPoints += amount;
            Debug.WriteLine($"Added {amount} points to \"{Base.Name}\"");
        }

        public void Respawn ( )
        {
            var where = Team.GetPlayerSpawnLocation( );
            //TriggerClientEvent( "koth:spawnPlayer",
            //                   where[0],
            //                   where[1],
            //                   where[2],
            //                   where[3],
            //                   CurrentTeam. );
        }

        public bool SavePlayer ( )
        {
            /* Save information to the database */
            return true;
        }
    }
}