using CitizenFX.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using static CitizenFX.Core.Native.API;

namespace Client
{
    class Client : BaseScript
    {
        private bool MenuOpen = true;
        private bool TeamSelectionOpen = false;
        private bool ClassSelectionOpen = false;

        private bool CreatedWeaponsDealerNPC = false;
        private bool CreatedVehiclesDealerNPC = false;

        private readonly uint WeaponsDealerModel = (uint)GetHashKey( "s_m_m_marine_02" );

        private readonly uint VehiclesDealerModel = (uint)GetHashKey( "s_m_y_marine_01" );
        private readonly uint VehiclesDealerPropModel = (uint)GetHashKey( "insurgent2" );

        private List<float> WeaponsDealerCoords;
        private List<float> VehiclesDealerCoords;

        private dynamic SessionSpawnPoint;

        public Client ( )
        {
            Debug.WriteLine( "Starting up KOTH..." );

            RegisterNuiCallbackType( "toggleMenuVisibility" );
            RegisterNuiCallbackType( "toggleTeamSelection" );
            RegisterNuiCallbackType( "toggleClassSelection" );

            RegisterNuiCallbackType( "teamSelection" );
            RegisterNuiCallbackType( "classSelection" );
        }

#if DEBUG

        #region Debug

        private void PrintHealth ( )
        {
            Debug.WriteLine( $"Entity health: {Game.PlayerPed.Health}" );
            Debug.WriteLine( $"Entity max health: {Game.PlayerPed.MaxHealth}" );
        }

        #endregion Debug

#endif

        #region GameEvents

        [EventHandler( "onClientMapStart" )]
        void OnClientMapStart ( string _ )
        {
            Exports["spawnmanager"].setAutoSpawn( false );

            if ( BusyspinnerIsOn( ) )
                BusyspinnerOff( );

            RequestModel( WeaponsDealerModel );
            RequestModel( VehiclesDealerModel );
            RequestModel( VehiclesDealerPropModel );
        }

        [EventHandler( "onClientResourceStart" )]
        void OnClientResourceStart ( string name )
        {
            if ( name.Equals( GetCurrentResourceName( ) ) )
            {
                Debug.WriteLine( "Bem-vindo ao servidor de King of the Hill do Faded!" );
                SetNuiFocus( MenuOpen, MenuOpen );
            }
        }

        [EventHandler( "playerSpawned" )]
        async void OnPlayerSpawned ( object _ )
        {
            if ( !CreatedWeaponsDealerNPC )
            {
                while ( !HasModelLoaded( WeaponsDealerModel ) )
                {
                    await Delay( 250 );
                }

                /* Default to pre-defined value if unable to get ground zero */
                float groundZero = WeaponsDealerCoords[2];

                GetGroundZFor_3dCoord( WeaponsDealerCoords[0], WeaponsDealerCoords[1], WeaponsDealerCoords[2], ref groundZero, false );

                Debug.WriteLine( $"Weapon dealer position: {WeaponsDealerCoords[0]}, {WeaponsDealerCoords[1]}, {WeaponsDealerCoords[2]}, {WeaponsDealerCoords[3]}" );
                var weaponsDealerHandle = CreatePed( 0, WeaponsDealerModel, WeaponsDealerCoords[0], WeaponsDealerCoords[1], groundZero, WeaponsDealerCoords[3], false, true );

                if ( weaponsDealerHandle != 0 )
                {
                    EntitySetAttr( weaponsDealerHandle );
                    CreatedWeaponsDealerNPC = true;
                }
                else
                {
                    Debug.WriteLine( $"Failed to create weapons dealer ped. Please report this error!" );
                }
            }

            if ( !CreatedVehiclesDealerNPC )
            {
                while ( !HasModelLoaded( VehiclesDealerModel ) || !HasModelLoaded( VehiclesDealerPropModel ) )
                {
                    await Delay( 250 );
                }

                float groundZero = VehiclesDealerCoords[2];
                GetGroundZFor_3dCoord( VehiclesDealerCoords[0], VehiclesDealerCoords[1], VehiclesDealerCoords[2], ref groundZero, false );

#if DEBUG
                Debug.WriteLine( $"Vehicle dealer position: {VehiclesDealerCoords[0]}, {VehiclesDealerCoords[1]}, {VehiclesDealerCoords[2]}, {VehiclesDealerCoords[3]}" );
#endif

                var vehicleDealerHandle = CreatePed( 0, VehiclesDealerModel, VehiclesDealerCoords[0], VehiclesDealerCoords[1], groundZero, VehiclesDealerCoords[3], false, true );

                var vehiclesDealerPropHandle = CreateVehicle( VehiclesDealerPropModel,
                                                   VehiclesDealerCoords[0] + 1.7f,
                                                   VehiclesDealerCoords[1] + .5f,
                                                   VehiclesDealerCoords[2],
                                                   VehiclesDealerCoords[3] * 1.8f,
                                                   false,
                                                   true );

                SetVehicleDoorsLockedForAllPlayers( vehiclesDealerPropHandle, true );
                SetVehicleDoorsLocked( vehiclesDealerPropHandle, 2 );
                if ( vehicleDealerHandle != 0 )
                {
                    EntitySetAttr( vehicleDealerHandle );
                    EntitySetAttr( vehiclesDealerPropHandle );
                    CreatedVehiclesDealerNPC = true;
                }
                else
                {
                    Debug.WriteLine( $"Failed to create vehicle dealer ped. Please report this error!" );
                }

            }

            Game.PlayerPed.DropsWeaponsOnDeath = false;
            TriggerServerEvent( "koth:playerFinishSetup" );

            SetModelAsNoLongerNeeded( VehiclesDealerPropModel );
            SetModelAsNoLongerNeeded( VehiclesDealerModel );
            SetModelAsNoLongerNeeded( WeaponsDealerModel );
        }

        #endregion GameEvents


        #region NUICallbacks

        [EventHandler( "__cfx_nui:toggleTeamSelection" )]
        void OnToggleTeamSelection ( IDictionary<string, object> data, CallbackDelegate cb )
        {
            TeamSelectionOpen = !TeamSelectionOpen;
            cb( new
            {
                ok = true
            } );
        }

        [EventHandler( "__cfx_nui:toggleMenu" )]
        void OnToggleMenu ( IDictionary<string, object> data, CallbackDelegate cb )
        {
            MenuOpen = !MenuOpen;
            cb( new
            {
                ok = true
            } );
        }

        [EventHandler( "__cfx_nui:toggleClassSelection" )]
        void OnToggleClassSelection ( IDictionary<string, object> data, CallbackDelegate cb )
        {
            ClassSelectionOpen = !ClassSelectionOpen;
            cb( new
            {
                ok = true
            } );
        }

        [EventHandler( "__cfx_nui:teamSelection" )]
        void OnSelectTeam ( IDictionary<string, object> data, CallbackDelegate cb )
        {
            if ( !data.TryGetValue( "team_id", out var teamIdObj ) )
            {
                cb( new { error = "Invalid team!", ok = false } );
                return;
            }

            var team_id = ( teamIdObj as string ) ?? "";

            TriggerServerEvent( "koth:teamJoin", team_id );

            cb( new
            {
                ok = true,
            } );
        }

        [EventHandler( "__cfx_nui:classSelection" )]
        void OnClassSelection ( IDictionary<string, object> data, CallbackDelegate cb )
        {
            if ( !data.TryGetValue( "class_id", out var classIdObj ) )
            {
                cb( new { error = "Invalid class!", ok = false } );
                return;
            }

            var class_id = ( classIdObj as string ) ?? "";

            Debug.WriteLine( $"Selecting class: {class_id}" );

            TriggerServerEvent( "koth:classSelected", class_id );

            cb( new
            {
                ok = true,
            } );
        }

        #endregion NUICallbacks


        #region TickRoutines

        [Tick]
        async Task RevivePlayer ( )
        {
            Game.DisableControlThisFrame( 0, Control.ReplayStartStopRecordingSecondary );
            if ( Game.PlayerPed.IsDead && IsDisabledControlPressed( 0, (int)Control.ReplayStartStopRecordingSecondary ) )
            {
                NetworkResurrectLocalPlayer( Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, Game.PlayerPed.Position.Z, Game.PlayerPed.Heading, false, false );
                Game.PlayerPed.IsInvincible = false;
                Game.PlayerPed.ClearBloodDamage( );
            }
            await Delay( 100 );
        }

        [Tick]
        async Task GetStateUpdate ( )
        {
            if ( !Game.PlayerPed.IsDead )
            {
                SendNuiMessage( JsonConvert.SerializeObject( new { type = "health_update", health = Game.PlayerPed.Health, max_health = Game.PlayerPed.MaxHealth } ) );
            }

            await Delay( 100 );
        }

        #endregion TickRoutines

        #region GameModeEvents

        [EventHandler( "koth:playerJoinedTeam" )]
        void OnPlayerJoinedTeam ( )
        {
            SendNuiMessage( JsonConvert.SerializeObject( new { type = "team_selection_toggle" } ) );
            SendNuiMessage( JsonConvert.SerializeObject( new { type = "class_selection_toggle" } ) );
        }

        [EventHandler( "koth:spawnPlayer" )]
        void SpawnPlayer ( float x, float y, float z, float heading, int model )
        {
            Exports["spawnmanager"].spawnPlayer( new { x, y, z, heading, model, skipFade = false } );
        }

        [EventHandler( "koth:playerSelectedClass" )]
        void OnPlayerSelectClass ( List<object> spawnCoords, List<object> vehiclesDealerCoords, List<object> weaponsDealerCoords, int spawnModel )
        {
            if ( !CreatedWeaponsDealerNPC && !CreatedVehiclesDealerNPC )
            {
                WeaponsDealerCoords = weaponsDealerCoords.OfType<float>( ).ToList( );
                VehiclesDealerCoords = vehiclesDealerCoords.OfType<float>( ).ToList( );
            }

            ClassSelectionOpen = !ClassSelectionOpen;
            SetNuiFocus( ClassSelectionOpen, ClassSelectionOpen );

            /* used in "playerSpawned" */
            var playerSpawnCoords = spawnCoords.OfType<float>( ).ToList( );

            /* just reuse it in the future */
            SessionSpawnPoint = Exports["spawnmanager"].addSpawnPoint( new { x = playerSpawnCoords[0], y = playerSpawnCoords[1], z = playerSpawnCoords[2], heading = playerSpawnCoords[3], model = spawnModel, skipFade = false } );

            Exports["spawnmanager"].spawnPlayer( SessionSpawnPoint );

            SendNuiMessage( JsonConvert.SerializeObject( new { type = "finish_setup" } ) );
        }

        #endregion GameModeEvents

        #region GameModeFunctions

        void EntitySetAttr ( int entity )
        {
            SetEntityAsMissionEntity( entity, true, true );
            SetEntityInvincible( entity, true );
            FreezeEntityPosition( entity, true );
            SetBlockingOfNonTemporaryEvents( entity, true );
        }

        #endregion GameModeFunctions
    }
}
