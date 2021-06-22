using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace koth_server
{
    public static class Utils
    {
        static KothTeam defaultTeam = new KothTeam(0, "", new Vector3());

        public static string GetPlayerLicense(IdentifierCollection identifiers)
        {
            return identifiers.Where((id) => id.StartsWith("license:")).FirstOrDefault();
        }
    }
}
