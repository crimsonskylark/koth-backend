using CitizenFX.Core;
using CitizenFX.Core.Native;
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

        private bool CreatedVehiclesDealerNPC = false;

        private readonly uint VehiclesDealerModel = (uint)GetHashKey( "s_m_y_marine_03" );
        private readonly uint VehiclesDealerPropModel = (uint)GetHashKey( "insurgent2" );

        private List<float> VehiclesDealerCoords;
        private List<float> VehiclesDealerPropCoords;
        private List<float> AO;

        private dynamic SessionSpawnPoint;

        public Client ( )
        {
            Debug.WriteLine("Starting up KOTH...");

            RegisterNuiCallbackType("toggleMenuVisibility");
            RegisterNuiCallbackType("toggleTeamSelection");
            RegisterNuiCallbackType("toggleClassSelection");

            RegisterNuiCallbackType("teamSelection");
            RegisterNuiCallbackType("classSelection");
        }

        #region GameEvents

        [EventHandler("onClientMapStart")]
        void OnClientMapStart ( string _ )
        {
            Exports["spawnmanager"].setAutoSpawn(false);

            if (BusyspinnerIsOn())
                BusyspinnerOff();

        }

        [EventHandler("onClientResourceStart")]
        void OnClientResourceStart ( string name )
        {
            if (name.Equals(GetCurrentResourceName()))
            {
                Debug.WriteLine("Bem-vindo ao servidor de King of the Hill do Faded!");
                SetNuiFocus(MenuOpen, MenuOpen);
            }
        }

        [EventHandler("playerSpawned")]
        async void OnPlayerSpawned ( object _ )
        {
            if (!CreatedVehiclesDealerNPC)
            {

                RequestModel(VehiclesDealerModel);
                RequestModel(VehiclesDealerPropModel);

                while (!HasModelLoaded(VehiclesDealerModel) || !HasModelLoaded(VehiclesDealerPropModel))
                {
                    await Delay(250);
                }

                float groundZero = VehiclesDealerCoords[2];
                GetGroundZFor_3dCoord(VehiclesDealerCoords[0], VehiclesDealerCoords[1], VehiclesDealerCoords[2], ref groundZero, false);

                var vehicleDealerHandle = CreatePed( 0, VehiclesDealerModel, VehiclesDealerCoords[0], VehiclesDealerCoords[1], groundZero, VehiclesDealerCoords[3], false, true );

                Function.Call((Hash)0x283978A15512B2FE, vehicleDealerHandle, false);

                SetPedComponentVariation(vehicleDealerHandle, 1, 0, 1, 0);
                SetPedComponentVariation(vehicleDealerHandle, 0, 0, 0, 0);

                var vehiclesDealerPropHandle = CreateVehicle( VehiclesDealerPropModel,
                                                   VehiclesDealerPropCoords[0],
                                                   VehiclesDealerPropCoords[1],
                                                   VehiclesDealerPropCoords[2],
                                                   VehiclesDealerPropCoords[3],
                                                   false,
                                                   true );

                SetVehicleDoorsLockedForAllPlayers(vehiclesDealerPropHandle, true);
                SetVehicleDoorsLocked(vehiclesDealerPropHandle, 2);

                if (vehicleDealerHandle != 0)
                {
                    EntitySetAttr(vehicleDealerHandle);
                    EntitySetAttr(vehiclesDealerPropHandle);
                    CreatedVehiclesDealerNPC = true;
                }
                else
                {
                    Debug.WriteLine($"Failed to create vehicle dealer ped. Please report this error!");
                }

            }

            Game.PlayerPed.DropsWeaponsOnDeath = false;
            TriggerServerEvent("koth:playerFinishSetup");
        }

        #endregion GameEvents


        #region NUICallbacks

        [EventHandler("__cfx_nui:toggleTeamSelection")]
        void OnToggleTeamSelection ( IDictionary<string, object> data, CallbackDelegate cb )
        {
            TeamSelectionOpen = !TeamSelectionOpen;
            cb(new
            {
                ok = true
            });
        }

        [EventHandler("__cfx_nui:toggleMenu")]
        void OnToggleMenu ( IDictionary<string, object> data, CallbackDelegate cb )
        {
            MenuOpen = !MenuOpen;
            cb(new
            {
                ok = true
            });
        }

        [EventHandler("__cfx_nui:teamSelection")]
        void OnSelectTeam ( IDictionary<string, object> data, CallbackDelegate cb )
        {
            if (!data.TryGetValue("team_id", out var teamIdObj))
            {
                cb(new { error = "Invalid team!", ok = false });
                return;
            }

            var team_id = ( teamIdObj as string ) ?? "";

            TriggerServerEvent("koth:playerTeamJoin", team_id);

            cb(new
            {
                ok = true,
            });
        }

        #endregion NUICallbacks


        #region TickRoutines

        [Tick]
        async Task RevivePlayer ( )
        {
            Game.DisableControlThisFrame(0, Control.ReplayStartStopRecordingSecondary);
            if (Game.PlayerPed.IsDead && IsDisabledControlPressed(0, (int)Control.ReplayStartStopRecordingSecondary))
            {
                NetworkResurrectLocalPlayer(Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, Game.PlayerPed.Position.Z, Game.PlayerPed.Heading, false, false);
                Game.PlayerPed.IsInvincible = false;
                Game.PlayerPed.ClearBloodDamage();
            }
            await Delay(100);
        }

        [Tick]
        async Task GetStateUpdate ( )
        {
            if (!Game.PlayerPed.IsDead)
            {
                SendNuiMessage(JsonConvert.SerializeObject(new { type = "health_update", health = Game.PlayerPed.Health, max_health = Game.PlayerPed.MaxHealth }));
            }

            await Delay(100);
        }

        #endregion TickRoutines

        #region GameModeEvents

        [EventHandler("koth:playerJoinedTeam")]
        void OnPlayerJoinedTeam ( List<object> playerTeammates, List<object> spawnCoords, List<object> vehiclesDealerCoords, List<object> vehDealerPropCoords, List<object> combatZone, uint playerModel )
        {
            SendNuiMessage(JsonConvert.SerializeObject(new { type = "team_selection_toggle" }));

            VehiclesDealerCoords = vehiclesDealerCoords.OfType<float>().ToList();
            VehiclesDealerPropCoords = vehDealerPropCoords.OfType<float>().ToList();

            /* used in "playerSpawned" */
            var playerSpawnCoords = spawnCoords.OfType<float>( ).ToList( );

            uint teamHash = 0;

            AddRelationshipGroup("PlayerTeam", ref teamHash);

            var teammates = playerTeammates.OfType<int>().ToList();

            foreach (var teammate in teammates)
            {
                if (teammate != 0)
                {
                    var b = AddBlipForEntity(teammate);
                    SetBlipAsFriendly(b, true);
                }
            }

            /* just reuse it in the future */
            SessionSpawnPoint = Exports["spawnmanager"].addSpawnPoint(new { x = playerSpawnCoords[0], y = playerSpawnCoords[1], z = playerSpawnCoords[2], heading = playerSpawnCoords[3], model = playerModel, skipFade = false });

            Exports["spawnmanager"].spawnPlayer(SessionSpawnPoint);

            AO = combatZone.OfType<float>().ToList();

            Exports["polyzone"].setupGameZones(new { x = playerSpawnCoords[0], y = playerSpawnCoords[1], z = playerSpawnCoords[2], h = playerSpawnCoords[3] }, new { x = AO[0], y = AO[1], z = AO[2] });

            SendNuiMessage(JsonConvert.SerializeObject(new { type = "finish_setup" }));

            SetNuiFocus(false, false);
        }

        [EventHandler("koth:spawnPlayer")]
        void SpawnPlayer ( float x, float y, float z, float heading, int model )
        {
            Exports["spawnmanager"].spawnPlayer(new { x, y, z, heading, model, skipFade = false });
        }

        #endregion GameModeEvents

        #region GameModeFunctions

        void EntitySetAttr ( int entity )
        {
            SetEntityAsMissionEntity(entity, true, true);
            SetEntityInvincible(entity, true);
            FreezeEntityPosition(entity, true);
            SetBlockingOfNonTemporaryEvents(entity, true);
        }

        #endregion GameModeFunctions
    }
}
