using CitizenFX.Core;
using koth_server.Teams;
using System.Linq;

namespace koth_server
{
    public static class Utils
    {

        public static string GetPlayerLicense(IdentifierCollection identifiers)
        {
            return identifiers.Where((id) => id.StartsWith("license:")).FirstOrDefault();
        }
    }
}
