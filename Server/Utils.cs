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

        static public float lerp(int v0, int v1, float t)
        {
            return (1 - t) * v0 + t * v1;
        }
    }
}
