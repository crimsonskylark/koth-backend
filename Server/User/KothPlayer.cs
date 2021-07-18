using CitizenFX.Core;
using Server.User.Classes;
using System;

using static CitizenFX.Core.Native.API;

namespace Server.User
{
    /*
     * Represents a player inside the game.
     * Not to be mistaken with `Player` provided by CFX
     */
    class KothPlayer : BaseScript
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
        public KothTeam CurrentTeam { get; private set; }
        //public Squad curr_squad { get; private set; }
        public DateTime JoinTime { get; private set; }
        public DateTime LeaveTime { get; set; }
        public float Experience { get; private set; }
        public int Level { get; private set; }
        public IPlayerClass Class;
        public bool CanBeRevived { get; private set; }

        public KothPlayer ( Player player )
        {
            Base = player;
            JoinTime = DateTime.UtcNow;
            License = Utils.GetPlayerLicense( Base.Identifiers );
            Debug.WriteLine( $"Player joined server at {JoinTime}" );
        }

        ~KothPlayer ( )
        {
            LeaveTime = DateTime.UtcNow;
            TotalMoney += SessionMoney;
            TotalKills += SessionKills;
            TotalDeaths += SessionDeaths;
            Debug.WriteLine( $"Player leaving server at {LeaveTime}." );
        }

        public bool JoinTeam ( KothTeam t )
        {
            Debug.WriteLine( $"t: {t} curr_team: {CurrentTeam}" );
            if ( CurrentTeam != default )
                LeaveTeam( );

            CurrentTeam = t;
            t.Players.Add( this );
            Debug.WriteLine( $"Player {Base.Name} joined team {CurrentTeam.Name}" );
            TriggerClientEvent( "koth:updateTeamCount", t.Id, t.Players.Count );
            return true;
        }

        public void LeaveTeam ( )
        {
            Debug.WriteLine( $"Player {Base.Name} left team {CurrentTeam.Name}" );
            CurrentTeam.Players.Remove( this );
        }

        public void Respawn ( )
        {
            var where = CurrentTeam.GetPlayerSpawnLocation( );
            TriggerClientEvent( "koth:spawnPlayer",
                               where[0],
                               where[1],
                               where[2],
                               where[3],
                               Class.Model );
        }

        public bool SavePlayer ( )
        {
            /* Save information to the database */
            return true;
        }
    }
}
