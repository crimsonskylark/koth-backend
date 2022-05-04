using CitizenFX.Core;

namespace Server
{
    public static class Utils
    {

        public static string GetPlayerLicense(IdentifierCollection identifiers, string type = "license")
        {
            return identifiers[type];
        }

        public static float lerp(int v0, int v1, float t)
        {
            return (1 - t) * v0 + t * v1;
        }
    }
}
