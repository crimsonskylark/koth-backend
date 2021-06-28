using CitizenFX.Core;
using System.Linq;

namespace Server
{
    public static class Utils
    {

        public static string GetPlayerLicense(IdentifierCollection identifiers)
        {
            return identifiers.Where((id) => id.StartsWith("license:")).FirstOrDefault();
        }
    }
}
