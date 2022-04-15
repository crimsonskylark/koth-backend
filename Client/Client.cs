using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Koth.Shared;
using Newtonsoft.Json;
using static CitizenFX.Core.Native.API;

namespace Client
{
    internal class Client : BaseScript
    {
        private bool MenuOpen = true;
        private bool TeamSelectionOpen = false;

        private bool CreatedVehiclesDealerNPC = false;

        private readonly uint VehiclesDealerModel = (uint)GetHashKey("s_m_y_marine_03");
        private readonly uint VehiclesDealerPropModel = (uint)GetHashKey("insurgent2");

        private readonly string SerializedFinishMsg = JsonConvert.SerializeObject(new { type = "finish_setup" });

        private float[] VehiclesDealerCoords;
        private float[] VehiclesDealerPropCoords;
        private float[] AO;

        private List<int> EnemyTeamHashes = new List<int>();
        private int PlayerTeamHash;

        private dynamic SessionSpawnPoint;

        public Client()
        {
            RegisterNuiCallbackType("toggleMenuVisibility");
            RegisterNuiCallbackType("toggleTeamSelection");
            RegisterNuiCallbackType("toggleClassSelection");

            RegisterNuiCallbackType("teamSelection");
            RegisterNuiCallbackType("classSelection");
        }

        #region GameEvents

        [EventHandler("onClientMapStart")]
        private void OnClientMapStart(string _)
        {
            Exports["spawnmanager"].setAutoSpawn(false);
            if (BusyspinnerIsOn())
                BusyspinnerOff();
        }

        [EventHandler("onClientResourceStart")]
        private void OnClientResourceStart(string name)
        {
            if (name.Equals(GetCurrentResourceName()))
            {
                Debug.WriteLine("Bem-vindo ao servidor de King of the Hill do Faded!");

                SetNuiFocus(MenuOpen, MenuOpen);
            }
            NetworkSetFriendlyFireOption(false);
        }

        [EventHandler("playerSpawned")]
        private async void OnPlayerSpawned(object _)
        {
            if (!CreatedVehiclesDealerNPC)
            {
                RequestModel(VehiclesDealerModel);
                RequestModel(VehiclesDealerPropModel);

                while (!HasModelLoaded(VehiclesDealerModel) || !HasModelLoaded(VehiclesDealerPropModel))
                {
                    await Delay(100);
                }

                float groundZero = VehiclesDealerCoords[2];
                GetGroundZFor_3dCoord(VehiclesDealerCoords[0], VehiclesDealerCoords[1], VehiclesDealerCoords[2], ref groundZero, false);

                var vehicleDealerHandle = CreatePed(0, VehiclesDealerModel, VehiclesDealerCoords[0], VehiclesDealerCoords[1], groundZero, VehiclesDealerCoords[3], false, true);

                Function.Call((Hash)0x283978A15512B2FE, vehicleDealerHandle, false);

                SetPedComponentVariation(vehicleDealerHandle, 1, 0, 1, 0);
                SetPedComponentVariation(vehicleDealerHandle, 0, 0, 0, 0);

                var vehiclesDealerPropHandle = CreateVehicle(VehiclesDealerPropModel,
                                                   VehiclesDealerPropCoords[0],
                                                   VehiclesDealerPropCoords[1],
                                                   VehiclesDealerPropCoords[2],
                                                   VehiclesDealerPropCoords[3],
                                                   false,
                                                   true);

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
                    Debug.WriteLine($"Falha em criar o vendedor de veículos.");
                }
            }

            Game.PlayerPed.DropsWeaponsOnDeath = false;
            TriggerServerEvent("koth:playerFinishSetup");
        }

        #endregion GameEvents

        #region NUICallbacks

        [EventHandler("__cfx_nui:toggleTeamSelection")]
        private void OnToggleTeamSelection(IDictionary<string, object> data, CallbackDelegate cb)
        {
            TeamSelectionOpen = !TeamSelectionOpen;
            cb(new
            {
                ok = true
            });
        }

        [EventHandler("__cfx_nui:toggleMenu")]
        private void OnToggleMenu(IDictionary<string, object> data, CallbackDelegate cb)
        {
            MenuOpen = !MenuOpen;
            cb(new
            {
                ok = true
            });
        }

        [EventHandler("__cfx_nui:teamSelection")]
        private void OnSelectTeam(IDictionary<string, object> data, CallbackDelegate cb)
        {
            if (!data.TryGetValue("team_id", out var teamIdObj))
            {
                cb(new { error = "Invalid team!", ok = false });
                return;
            }

            var team_id = (teamIdObj as string) ?? "";

            Debug.WriteLine($"Trying to join team {team_id}");

            TriggerServerEvent("koth:playerTeamJoin", team_id);

            cb(new
            {
                ok = true,
            });
        }

        #endregion NUICallbacks

        #region TickRoutines

        [Tick]
        private async Task RevivePlayer()
        {
            Game.DisableControlThisFrame(0, Control.ReplayStartStopRecording);
            if (Game.PlayerPed.IsDead && IsDisabledControlPressed(0, (int)Control.ReplayStartStopRecording))
            {
                NetworkResurrectLocalPlayer(Game.PlayerPed.Position.X, Game.PlayerPed.Position.Y, Game.PlayerPed.Position.Z, Game.PlayerPed.Heading, false, false);
                Game.PlayerPed.IsInvincible = false;
                Game.PlayerPed.ClearBloodDamage();
            }
            await Delay(0);
        }

        [Tick]
        private async Task GetStateUpdate()
        {
            if (!Game.PlayerPed.IsDead)
            {
                SendNuiMessage(JsonConvert.SerializeObject(new { type = "health_update", health = Game.PlayerPed.Health, max_health = Game.PlayerPed.MaxHealth }));
            }

            await Delay(100);
        }

        [Tick]
        private async Task TestRoutine()
        {
            var pos = Game.PlayerPed.Position;
            var next = pos + (Game.PlayerPed.ForwardVector * 25.0f);
            DrawLine(pos.X, pos.Y, pos.Z + 0.4f, next.X, next.Y, next.Z + 0.4f, 255, 255, 255, 255);

            var entities = GetGamePool("CPed");

            foreach (int ent in entities)
            {
                if (!IsPedHuman(ent))
                    continue;

                Vector3 cd = GetEntityCoords(ent, false);

                if (cd.Z < 0)
                    continue;

                switch (cd.DistanceToSquared(pos))
                {
                    case var dis when dis <= 800.0f:
                        {
                            DrawLine(pos.X, pos.Y, pos.Z, cd.X, cd.Y, cd.Z, 61, 224, 71, 255);
                            break;
                        }
                    case var dis when dis >= 2000.0f:
                        {
                            DrawLine(pos.X, pos.Y, pos.Z, cd.X, cd.Y, cd.Z, 224, 83, 137, 255);
                            break;
                        }
                    default:
                        continue;
                }

            }

            await Task.FromResult(0);
        }

        [Tick]
        private async Task SetRelationships()
        {
            try
            {
                var curr = Game.PlayerPed;
                var entity = curr.Handle;

                SetEntityCanBeDamagedByRelationshipGroup(entity, false, PlayerTeamHash);

                foreach (var enemy in EnemyTeamHashes)
                {
                    SetRelationshipBetweenGroups((int)Relationship.Hate, (uint)PlayerTeamHash, (uint)enemy);
                    SetRelationshipBetweenGroups((int)Relationship.Hate, (uint)enemy, (uint)PlayerTeamHash);
                }

                SetCanAttackFriendly(entity, false, false);
                NetworkSetFriendlyFireOption(false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        #endregion TickRoutines

        #region GameModeEvents

        [EventHandler("koth:playerJoinedTeam")]
        private void OnPlayerJoinedTeam(string spawnInfo)
        {

            SendNuiMessage(JsonConvert.SerializeObject(new { type = "team_selection_toggle" }));

            var spawn = JsonConvert.DeserializeObject<SpawnContext>(spawnInfo);

            VehiclesDealerCoords = spawn.vehiclesDealerCoords;
            VehiclesDealerPropCoords = spawn.vehiclesDealerPropCoords;

            /* used in "playerSpawned" */
            var playerSpawnCoords = spawn.playerSpawnCoords;

            uint temp = 0;

            AddRelationshipGroup(spawn.playerTeamName, ref temp);

            SetPedRelationshipGroupHash(Game.PlayerPed.Handle, temp);

            PlayerTeamHash = (int)temp;

            foreach (var enemyTeam in spawn.enemyTeamNames)
            {
                AddRelationshipGroup(enemyTeam, ref temp);
                EnemyTeamHashes.Add((int)temp);
            }

            AO = spawn.combatZone;

            foreach (var teammate in spawn.playerTeammates)
            {
                if (teammate != 0)
                {
                    var b = AddBlipForEntity(Players[teammate].Character.Handle);
                    SetBlipAsFriendly(b, true);
                }
            }

            /* just reuse it in the future */
            SessionSpawnPoint = Exports["spawnmanager"].addSpawnPoint(new { x = playerSpawnCoords[0], y = playerSpawnCoords[1], z = playerSpawnCoords[2], heading = playerSpawnCoords[3], model = spawn.playerModel, skipFade = false });

            Exports["spawnmanager"].spawnPlayer(SessionSpawnPoint);


            Exports["polyzone"].setupGameZones(new { x = playerSpawnCoords[0], y = playerSpawnCoords[1], z = playerSpawnCoords[2], h = playerSpawnCoords[3] }, new { x = AO[0], y = AO[1], z = AO[2] });

            SendNuiMessage(SerializedFinishMsg);
            SetNuiFocus(false, false);
        }

        [EventHandler("koth:spawnPlayer")]
        private void SpawnPlayer(float x, float y, float z, float heading, int model)
        {
            Exports["spawnmanager"].spawnPlayer(new { x, y, z, heading, model, skipFade = false });
        }

        [EventHandler("koth:safeHeal")]
        private void OnSafeHeal(int amount)
        {
            if (GetEntityHealth(Game.PlayerPed.Handle) < 200)
            {
                SetEntityHealth(Game.PlayerPed.Handle, amount);
            }
        }

        [EventHandler("koth:newTeammate")]
        private void OnNewTeammate(int netid)
        {
            if (netid != 0)
            {
                var p = Players[netid];
                var b = AddBlipForEntity(p.Character.Handle);
                SetBlipAsFriendly(b, true);
            }
        }

        [EventHandler("koth:StateUpdate")]
        private void OnStateUpdate(string newState)
        {
            Debug.WriteLine(newState);
            SendNuiMessage(newState);
        }

        #endregion GameModeEvents

        #region GameModeFunctions

        private void EntitySetAttr(int entity)
        {
            SetEntityAsMissionEntity(entity, true, true);
            SetEntityInvincible(entity, true);
            FreezeEntityPosition(entity, true);
            SetBlockingOfNonTemporaryEvents(entity, true);
        }

        #endregion GameModeFunctions
    }
}