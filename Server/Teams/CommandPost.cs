using System.Collections.Generic;

using static CitizenFX.Core.Native.API;

namespace Server
{
    class CommandPost
    {
        public Queue<Spawn> SpawnPoints = new();

        private int _weaponsDealerVehicleProp { get; set; }
        public int WeaponsDealerVehicleProp { get => NetworkGetNetworkIdFromEntity(_weaponsDealerVehicleProp); private set => _weaponsDealerVehicleProp = value; }

        private int _vehiclesDealerProp { get; set; }
        public int VehicleDealerProp { get => _vehiclesDealerProp; private set => _vehiclesDealerProp = value; }

        public CommandPost()
        { }

        public Spawn GetSpawnPoint()
        {
            return SpawnPoints.Peek();
        }
    }
}
