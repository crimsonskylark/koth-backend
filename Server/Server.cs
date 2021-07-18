using CitizenFX.Core;
using koth_server.Map;
using Newtonsoft.Json;
using Server.Teams;
using Server.User;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using static CitizenFX.Core.Native.API;

namespace Server
{
    enum StateUpdate : int
    {
        PlayerJoin,
        PlayerLeave,
        PlayerDeath,
        PlayerKill,
        PlayerOnHill,
        TeamJoin,
        TeamLeave,
        TeamPoint,
        HillCaptured,
        HillLost,
        HillContested
    }
    class Server : BaseScript
    {
        readonly Dictionary<Player, KothPlayer> ServerPlayers = new( );
        readonly List<KothTeam> Teams = new( );
        readonly MapContainer MapConfig = new( );

        public Server ( )
        {
            Debug.WriteLine( "server main started!" );
            Debug.WriteLine( "setting up stuff" );

            // All teams
            var config = LoadResourceFile( GetCurrentResourceName( ), "config/maps.json" );
            
            MapConfig = JsonConvert.DeserializeObject<MapContainer>( config );
            
            var r = new Random( ).Next(0, MapConfig.Maps.Count()-1);
            var session = MapConfig.Maps[r];

            foreach (var t in session.Teams)
            {
                Debug.WriteLine( t.SafeZone[0].ToString( ) );
            }

        }

        #region GameEvents
        [EventHandler( "playerJoining" )]
        void OnPlayerJoining ( [FromSource] Player player, string old_id )
        {
            Debug.WriteLine( $"Player {player.Handle} has joined the server." );
            ServerPlayers.Add( player, new KothPlayer( player ) );
        }

        [EventHandler( "playerDropped" )]
        void OnPlayerDropped ( [FromSource] Player player, string reason )
        {
            if ( ServerPlayers.TryGetValue( player, out KothPlayer p ) )
            {
                p.LeaveTime = DateTime.UtcNow;
                Debug.WriteLine( $"Player {player.Handle} has left the server at {p.LeaveTime}. (Reason: {reason})" );
                ServerPlayers.Remove( player );
                /* TODO: Save player information in database */
            }
            else
            {
                /* Should never be reached in production. */
                Debug.WriteLine( $"[!!!] Player not found." );
            }
        }

        [EventHandler( "onResourceStart" )]
        void OnResourceStart ( string name )
        {
            if ( GetCurrentResourceName( ).Equals( name ) )
            {
                foreach ( var p in Players )
                {
                    Debug.WriteLine( $"Adding players to player list after restart." );
                    ServerPlayers.Add( p, new KothPlayer( p ) );
                }
            }
        }
        #endregion GameEvents

        #region BaseEvents

        [EventHandler( "baseevents:onPlayerKilled" )]
        private void OnPlayerKilled ( [FromSource] Player player, int killerType, ExpandoObject obj )
        {
            Debug.WriteLine( "Player killed" );
            foreach ( var v in obj )
            {
                Debug.WriteLine( $"Key: {v.Key} value: {v.Value}" );
            }
        }

        #endregion BaseEvents

        #region KOTHEvents

        [EventHandler( "koth:teamJoin" )]
        private void OnTeamJoin ( [FromSource] Player player, string team_id )
        {
            Debug.WriteLine( "onTeamJoinServerEvent" );
            var valid_team = int.TryParse( team_id, out int IntTeamId );
            Debug.WriteLine( $"Valid team: {valid_team}" );
            if ( string.IsNullOrEmpty( team_id ) || !valid_team || IntTeamId < 0 || IntTeamId > 3 )
            {
                Debug.WriteLine( $"Invalid team: {IntTeamId}" );
                return;
            }

            var team = Teams.Find( ( t ) => t.Id == IntTeamId );

            Debug.WriteLine( $"Player team: {team}" );

            if ( ServerPlayers[player].JoinTeam( team ) )
            {
                var teammates = ( from p in team.Players
                                  where p.Base != player
                                  select NetworkGetEntityOwner( p.Base.Character.Handle ) ).ToArray( );

                Debug.WriteLine( $"Joining team with teammates {teammates}" );

                player.TriggerEvent( "koth:playerJoinedTeam", teammates );

                player.TriggerEvent( "chat:addMessage", new { args = new[] { $"You are now part of team {team.Name}" } } );

                return;
            }

            player.TriggerEvent( "chat:addMessage", new { args = new[] { $"Failed to join team {team.Name}" } } );
        }

        [EventHandler( "koth:classSelected" )]
        private void OnClassSelected ( [FromSource] Player player, string class_id )
        {
            Debug.WriteLine( "OnClassSelected" );
            var valid_class = int.TryParse( class_id, out int IntClassId );
            if ( string.IsNullOrEmpty( class_id ) || !valid_class || IntClassId < 0 || IntClassId > 3 )
            {
                Debug.WriteLine( $"Invalid class: {IntClassId}" );
                return;
            }

            var p = ServerPlayers[player];
            p.Class = p.CurrentTeam.PlayerClasses[IntClassId];

            var spawn = ServerPlayers[player].CurrentTeam.GetSpawn( );

            player.TriggerEvent( "koth:playerSelectedClass", spawn.PlayerSpawn, spawn.VehiclesDealerCoords, spawn.WeaponsDealerCoords, p.Class.Model );
        }

        [EventHandler( "koth:playerFinishSetup" )]
        void OnPlayerFinishSetup ( [FromSource] Player player )
        {
            var _player = ServerPlayers[player];
            var _handle = _player.Base.Character.Handle;

            if ( DoesEntityExist( _handle ) && IsPedAPlayer( _handle ) )
            {
                GiveWeaponToPed( _handle,
                                _player.Class.DefaultWeapon,
                                200,
                                false,
                                true );
                SetPedArmour( _handle, 100 );
            }
        }

        #endregion KOTHEvents
    }
}
