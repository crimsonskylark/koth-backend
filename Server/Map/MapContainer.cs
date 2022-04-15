using System.Collections.Generic;

namespace Server.Map
{
    class MapContainer
    {
        public int SafeZoneRadius { get; set; }
        public int AoRadius { get; set; }
        public int VehDealerPropHash { get; set; }
        public List<SessionMap> Maps { get; set; }
    }
}
