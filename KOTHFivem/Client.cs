using CitizenFX.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace KOTHFivem
{
    class Client : BaseScript
    {
        bool isFactionSelectionOpen = false;
        bool isMenuOpen = true;
        public Client()
        {
            Debug.WriteLine("Starting up KOTH...");

            RegisterNuiCallbackType("toggleMenuVisibility");
            RegisterNuiCallbackType("toggleTeamSelection");
            RegisterNuiCallbackType("teamSelection");

            Exports["spawnmanager"].setAutoSpawn(false);
        }

        [EventHandler("onClientResourceStart")]
        void onClientResourceStartEvent(string name)
        {
            if (name.Equals(GetCurrentResourceName()))
            {
                Debug.WriteLine("Bem-vindo ao servidor de King of the Hill do Faded!");
                //SetNuiFocus(isMenuOpen, isMenuOpen);
            }
        }

        [EventHandler("playerDropped")]
        void onPlayerDropped(Player player, string reason)
        {
            TriggerServerEvent("KOTH:OnPlayerDrop", player);
        }

        [EventHandler("onPlayerConnected")]
        void onPlayerConnected() { }

        [EventHandler("__cfx_nui:toggleTeamSelection")]
        void onToggleTeamSelection(IDictionary<string, object> data, CallbackDelegate cb)
        {
            /*
             * Handle all the spawn/re-spawn logic here. 
             * - Select the appropriate model for the selected faction
             * - Spawn player at current faction spawn location
             */
            Debug.WriteLine("toggleTeamSelection called!");
            isFactionSelectionOpen = !isFactionSelectionOpen;
            Exports["spawnmanager"].spawnPlayer();
            cb(new
            {
                ok = true
            });
        }

        [EventHandler("__cfx_nui:toggleMenuVisibility")]
        void onToggleMenuVisibility(IDictionary<string, object> data, CallbackDelegate cb)
        {
            Debug.WriteLine("Toggling all the way..");
            isMenuOpen = !isMenuOpen;
            SetNuiFocus(isMenuOpen, isMenuOpen);
            cb(new
            {
                ok = true
            });
        }

        [Tick]
        async Task ToggleMenuVisibility()
        {
            if (IsControlPressed(0, 288))
            {
                isMenuOpen = !isMenuOpen;
                string msg = JsonConvert.SerializeObject(new { type = "menu_toggle" });
                SendNuiMessage(msg);
                SetNuiFocus(isMenuOpen, isMenuOpen);
            }
            await Delay(0);
        }

        [EventHandler("__cfx_nui:teamSelection")]
        void onSelectTeam(IDictionary<string, object> data, CallbackDelegate cb)
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

        [EventHandler("koth:updateTeamCount")]
        void onUpdateTeamCount(int team_id, int new_count)
        {
            Debug.WriteLine($"Updating player count for team {team_id}.");
            SendNuiMessage(JsonConvert.SerializeObject(new { type = "update", team_id = team_id, new_count = new_count }));
        }
    }
}
