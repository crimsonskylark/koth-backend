using CitizenFX.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace KOTHFivem
{
    class Main : BaseScript
    {
        bool isFactionSelectionOpen = false;
        bool isMenuOpen = true;
        public Main()
        {
            Debug.WriteLine("Starting up KOTH...");

            RegisterNuiCallbackType("toggleMenuVisibility");
            RegisterNuiCallbackType("toggleTeamSelection");
        }

        [EventHandler("onClientResourceStart")]
        void onClientResourceStartEvent(string name)
        {
            if (name.Equals(GetCurrentResourceName()))
            {
                Debug.WriteLine("Bem-vindo ao servidor de King of the Hill do Faded!");
                SetNuiFocus(isMenuOpen, isMenuOpen);
            }
        }
        
        [EventHandler("playerDropped")]
        void onPlayerDropped(Player player, string reason)
        {
            TriggerServerEvent("KOTH:OnPlayerDrop", true);
        }

        [EventHandler("onPlayerConnected")]
        void onPlayerConnected() { }

        [EventHandler("__cfx_nui:toggleTeamSelection")]
        void onToggleTeamSelection(IDictionary<string, object> data, CallbackDelegate cb)
        {
            Debug.WriteLine("toggleTeamSelection called!");
            isFactionSelectionOpen = !isFactionSelectionOpen;
            SetNuiFocus(isFactionSelectionOpen, isFactionSelectionOpen);
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
            await Delay(0);
        }
        
    }
}
