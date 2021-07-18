using System.Collections.Generic;

namespace koth_server.Map
{
    class MapContainer
    {
        public int SafeZoneRadius { get; set; }
        public int AoRadius { get; set; }
        public int VehDealerPropHash { get; set; }
        public List<Map> Maps { get; set; }
    }
}
