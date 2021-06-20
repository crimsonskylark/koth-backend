using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace koth_server
{
    class Main : BaseScript
    {
        public Main()
        {
            Debug.WriteLine("server main started!");
            Debug.WriteLine("setting up stuff");
        }

        [EventHandler("KOTH:OnPlayerDrop")]
        private void onPlayerDrop([FromSource] Player player)
        {
            Debug.WriteLine("Player has dropped from the server");
        }

        [EventHandler("baseevents:onPlayerKilled")]
        void onPlayerKilled([FromSource] Player player, int killerType, ExpandoObject obj)
        {
            Debug.WriteLine("Player killed");
            foreach (var v in obj)
            {
                Debug.WriteLine($"Key: {v.Key} value: {v.Value}");
            }
        }
    }
}
