using CitizenFX.Core;
using Serilog;
using System;

namespace Server.User
{
    internal class KothPlayer
    {
        public Player Citizen { get; private set; }
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
        public int Experience { get; private set; } = 0;
        public int Level { get; private set; } = 0;
        public bool CanBeRevived { get; private set; } = true;
        public bool IsInsideSafeZone { get; set; } = true;
        public bool IsInsideAO { get; set; } = false;

        // cached and used for canculations
        public Vector3 SafeZoneVec3 = new Vector3();

        public KothPlayer(Player _citizen)
        {
            Citizen = _citizen;
            JoinTime = DateTime.UtcNow;

            License = Utils.GetPlayerLicense(Citizen.Identifiers);

            Log.Logger.Information($"\"{Citizen.Name}\" joined the server.\n");
        }

        ~KothPlayer()
        {
            LeaveTime = DateTime.UtcNow;
            TotalMoney += SessionMoney;
            TotalKills += SessionKills;
            TotalDeaths += SessionDeaths;
            // SavePlayerInformation();
            Log.Logger.Information($"\"{Citizen.Name}\" left the server.\n");
        }

        internal void AddLevel()
        {
            Level++;
            Log.Logger.Debug($"\"{ Citizen.Name }\" gained one level { Level }.");
        }

        internal void AddExperience(int _amount)
        {
            Experience += _amount;
            Log.Logger.Debug($"\"{ Citizen.Name }\" gained { _amount } experience.");
        }

        internal void AddKill()
        {
            SessionKills++;
            Log.Logger.Debug($"\"{ Citizen.Name }\" gained one kill ({SessionKills})");
        }

        internal void AddDeath()
        {
            SessionDeaths++;
            Log.Logger.Debug($"\"{ Citizen.Name }\" died ({SessionDeaths})");
        }

        internal void AddMoney(int _amount)
        {
            SessionMoney += _amount;
            Log.Logger.Debug($"\"{Citizen.Name}\"'s was credited ${_amount} for a total of ${TotalMoney} (${SessionMoney})");
        }

        internal void AddPoints(int _amount = 10)
        {
            SessionPoints += _amount;
            Log.Logger.Debug($"\"{Citizen.Name}\" gained {_amount} points.");
        }

        public void Respawn()
        {
            var where = Team.GetPlayerSpawnLocation();
            Log.Logger.Debug($"\"{Citizen.Name}\" has respawned at { where }");
            //TriggerClientEvent( "koth:spawnPlayer",
            //                   where[0],
            //                   where[1],
            //                   where[2],
            //                   where[3],
            //                   CurrentTeam. );
        }
    }
}