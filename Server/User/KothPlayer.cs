using System;
using CitizenFX.Core;

namespace Server.User
{
    internal class KothPlayer
    {
        public Player CfxPlayer { get; private set; }
        public string License { get; private set; } = "";
        public int TotalKills { get; private set; } = 0;
        public int TotalDeaths { get; private set; } = 0;
        public int TotalPoints { get; private set; } = 0;
        public int TotalMoney { get; private set; } = 0;
        public int SessionKills { get; private set; } = 0;
        public int SessionDeaths { get; private set; } = 0;
        public int SessionMoney { get; private set; } = 0;
        public int SessionPoints { get; private set; } = 0;
        public Team Team { get; set; } = null;
        public DateTime JoinTime { get; private set; }
        public DateTime LeaveTime { get; set; }
        public float Experience { get; private set; } = 0.0f;
        public int Level { get; private set; } = 0;
        public bool CanBeRevived { get; private set; } = true;
        public bool IsInsideSafeZone { get; set; } = true;
        public bool IsInsideAO { get; set; } = false;
        

        // cached and used for canculations
        public Vector3 SafeZoneVec3 = new Vector3();
        public KothPlayer ( Player cfxplayer )
        {
            CfxPlayer = cfxplayer;
            JoinTime = DateTime.UtcNow;

            License = Utils.GetPlayerLicense(CfxPlayer.Identifiers);
            Debug.WriteLine($"Player joined server at {JoinTime}");
        }

        ~KothPlayer ( )
        {
            LeaveTime = DateTime.UtcNow;
            TotalMoney += SessionMoney;
            TotalKills += SessionKills;
            TotalDeaths += SessionDeaths;
            // SavePlayerInformation();
            Debug.WriteLine($"Player leaving server at {LeaveTime}.");
        }

        internal void AddLevel()
        {
            Level++;
            Debug.WriteLine($"Player gained a new level { Level }");
        }

        internal void AddExperience(float amount)
        {
            Experience += amount;
            Debug.WriteLine($"Player gained { amount } experience.");
        }

        internal void AddKill()
        {
            SessionKills++;
            Debug.WriteLine($"Added one kill to { CfxPlayer.Name }, total kills { SessionKills }");
        }

        internal void AddDeath()
        {
            SessionDeaths++;
            Debug.WriteLine($"Added one death to { CfxPlayer.Name }, total kills { SessionDeaths }");
        }

        internal void AddMoney(int amount)
        {
            SessionMoney += amount;
            Debug.WriteLine($"Added ${amount} to \"{CfxPlayer.Name}\"'s account, total ${TotalMoney}");
        }

        internal void AddPoints(int amount = 10)
        {
            SessionPoints += amount;
            Debug.WriteLine($"Added {amount} points to \"{CfxPlayer.Name}\"");
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

        public bool Save ( )
        {
            /* Save information to the database */
            return true;
        }
    }
}