using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KOTHFivem
{
    class Teams
    {
        static public void onTeamSelection32(IDictionary<string, object> data, CallbackDelegate cb)
        {
            Debug.WriteLine("selectTeam event called.");
            cb(new
            {
                ok = true,
            });
        }
    }
}
