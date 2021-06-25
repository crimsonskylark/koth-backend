using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace KOTHFivem
{
    class Client : BaseScript
    {
        bool isFactionSelectionOpen = false;
        bool isMenuOpen = true;
        bool hasCreatedWeaponsDealer = false;
        bool hasCreatedVehDealer = false;

        uint weaDealer = (uint)GetHashKey("s_m_m_marine_02");
        uint vehDealer = (uint)GetHashKey("s_m_y_marine_01");

        List<float> weaDealerCoords;
        List<float> vehDealerCoords;

        dynamic spawn_point;

        public Client ( )
        {
            Debug.WriteLine("Starting up KOTH...");

            RegisterNuiCallbackType("toggleMenuVisibility");
            RegisterNuiCallbackType("toggleTeamSelection");
            RegisterNuiCallbackType("teamSelection");
        }

        #region GameEvents

        [EventHandler("onClientMapStart")]
        void onClientMapStart ( string _ )
        {
            Exports["spawnmanager"].setAutoSpawn(false);
            if (BusyspinnerIsOn())
                BusyspinnerOff();
        }

        [EventHandler("onClientResourceStart")]
        void onClientResourceStartEvent ( string name )
        {
            if (name.Equals(GetCurrentResourceName()))
            {
                Debug.WriteLine("Bem-vindo ao servidor de King of the Hill do Faded!");
                SetNuiFocus(isMenuOpen, isMenuOpen);
            }
        }

        [EventHandler("playerSpawned")]
        async void onPlayerSpawned ( object _ )
        {
            var vehDealerVehicleModel = (uint)GetHashKey("insurgent2");

            if (!hasCreatedWeaponsDealer)
            {
                RequestModel(weaDealer);
                while (!HasModelLoaded(weaDealer))
                {
                    await Delay(250);
                }

                float gz = weaDealerCoords[2];

                GetGroundZFor_3dCoord(weaDealerCoords[0], weaDealerCoords[1], weaDealerCoords[2], ref gz, false);

                Debug.WriteLine($"Weapon dealer position: {weaDealerCoords[0]}, {weaDealerCoords[1]}, {weaDealerCoords[2]}, {weaDealerCoords[3]}");
                var wep_dealer_ped = CreatePed(0, weaDealer, weaDealerCoords[0], weaDealerCoords[1], gz, weaDealerCoords[3], false, true);

                if (wep_dealer_ped != 0)
                {
                    PedSetAttr(wep_dealer_ped);
                    hasCreatedWeaponsDealer = true;
                }
                else
                {
                    Debug.WriteLine($"Failed to create weapons dealer ped. Please report this error!");
                }
            }

            if (!hasCreatedVehDealer)
            {
                while (!HasModelLoaded(vehDealer) || !HasModelLoaded(vehDealerVehicleModel))
                {
                    await Delay(250);
                }

                float gz = vehDealerCoords[2];
                GetGroundZFor_3dCoord(vehDealerCoords[0], vehDealerCoords[1], vehDealerCoords[2], ref gz, false);

                Debug.WriteLine($"Vehicle dealer position: {vehDealerCoords[0]}, {vehDealerCoords[1]}, {vehDealerCoords[2]}, {vehDealerCoords[3]}");

                var veh_dealer_handle = CreatePed(0, vehDealer, vehDealerCoords[0], vehDealerCoords[1], gz, vehDealerCoords[3], false, true);

                var veh_dealer_apc = CreateVehicle(vehDealerVehicleModel,
                                                   vehDealerCoords[0] + 1.7f,
                                                   vehDealerCoords[1] + .5f,
                                                   gz,
                                                   vehDealerCoords[3] * 1.8f,
                                                   false,
                                                   true);

                SetVehicleDoorsLocked(veh_dealer_apc, 2);
                SetVehicleDoorsLockedForAllPlayers(veh_dealer_apc, true);
                if (veh_dealer_handle != 0)
                {
                    PedSetAttr(veh_dealer_handle);
                    PedSetAttr(veh_dealer_apc);
                    hasCreatedVehDealer = true;
                }
                else
                {
                    Debug.WriteLine($"Failed to create vehicle dealer ped. Please report this error!");
                }

            }

            SetPedDropsWeaponsWhenDead(Game.PlayerPed.Handle, false);
            TriggerServerEvent("koth:playerFinishSetup");

            SetModelAsNoLongerNeeded(vehDealerVehicleModel);
            SetModelAsNoLongerNeeded(vehDealer);
            SetModelAsNoLongerNeeded(weaDealer);
        }

        #endregion GameEvents


        #region NUICallbacks

        [EventHandler("__cfx_nui:toggleTeamSelection")]
        void onToggleTeamSelection ( IDictionary<string, object> data, CallbackDelegate cb )
        {
            isFactionSelectionOpen = !isFactionSelectionOpen;
            cb(new
            {
                ok = true
            });
        }

        [EventHandler("__cfx_nui:toggleMenu")]
        void onToggleMenu ( IDictionary<string, object> data, CallbackDelegate cb )
        {
            isMenuOpen = !isMenuOpen;
            cb(new
            {
                ok = true
            });
        }

        [EventHandler("__cfx_nui:teamSelection")]
        void onSelectTeam ( IDictionary<string, object> data, CallbackDelegate cb )
        {
            if (!data.TryGetValue("team_id", out var teamIdObj))
            {
                cb(new { error = "invalid team", ok = false });
                return;
            }

            var team_id = (teamIdObj as string) ?? "";

            Debug.WriteLine($"Team id: {team_id}");
            Debug.WriteLine("selectTeam event called.");

            TriggerServerEvent("koth:teamJoin", team_id);

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
            if (IsPedDeadOrDying(Game.PlayerPed.Handle, true) && IsDisabledControlPressed(0, (int)Control.ReplayStartStopRecordingSecondary))
            {
                Debug.WriteLine("Should revive");
                var pos = GetEntityCoords(Game.PlayerPed.Handle, false);
                NetworkResurrectLocalPlayer(pos.X, pos.Y, pos.Z, GetEntityHeading(Game.PlayerPed.Handle), false, false);
                SetPlayerInvincible(Game.PlayerPed.Handle, false);
                ClearPedBloodDamage(Game.PlayerPed.Handle);
            }
            await Delay(0);
        }

        #endregion TickRoutines

        #region KOTHEvents

        [EventHandler("koth:playerJoinedTeam")]
        void onPlayerJoinedTeam ( List<dynamic> player, int model, List<dynamic> weapons_dealer, List<dynamic> vehicle_dealer )
        {
            var player_info = player.OfType<float>().ToList();


            SendNuiMessage(JsonConvert.SerializeObject(new { type = "menu_toggle" }));

            /* used in "playerSpawned" */
            weaDealerCoords = weapons_dealer.OfType<float>().ToList();
            vehDealerCoords = vehicle_dealer.OfType<float>().ToList();

            isMenuOpen = !isMenuOpen;
            SetNuiFocus(isMenuOpen, isMenuOpen);

            /* just reuse it in the future */
            spawn_point = Exports["spawnmanager"].addSpawnPoint(new { x = player_info[0], y = player_info[1], z = player_info[2], heading = player_info[3], model = model, skipFade = false });

            Debug.WriteLine($"Spawning at {player_info[0]}, {player_info[1]}, {player_info[2]} with heading { player_info[3] } and model {model}.");

            Exports["spawnmanager"].spawnPlayer(spawn_point);
        }

        [EventHandler("koth:spawnPlayer")]
        void SpawnPlayer ( float x, float y, float z, float heading, int model )
        {
            Debug.WriteLine($"Received spawn command: {x}, {y}, {z}, {heading}, {model}");
            Exports["spawnmanager"].spawnPlayer(new { x, y, z, heading, model, skipFade = false });
        }

        #endregion KOTHEvents

        #region KOTHFunctions

        void PedSetAttr ( int entity )
        {
            SetEntityAsMissionEntity(entity, true, true);
            SetEntityInvincible(entity, true);
            FreezeEntityPosition(entity, true);
            SetBlockingOfNonTemporaryEvents(entity, true);
        }

        #endregion KOTHFunctions
    }
}
