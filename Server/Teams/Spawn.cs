using System.Collections.Generic;

namespace Server
{
    class Spawn
    {
        public float[] PlayerSpawn { get; set; }
        public List<float[]> VehicleSpawnLocations { get; set; }
        public List<float[]> AirSpawnLocations { get; set; }
        public float[] WeaponsDealerCoords { get; set; }
        public float[] VehiclesDealerCoords { get; set; }

        public Spawn ( ) { }
        public Spawn ( float[] _player_spawn, List<float[]> _vehicle_spawn, List<float[]> _air_spawn, float[] _weapons_dealer, float[] _vehicles_dealer )
        {
            PlayerSpawn = _player_spawn;
            VehicleSpawnLocations = _vehicle_spawn;
            AirSpawnLocations = _air_spawn;
            WeaponsDealerCoords = _weapons_dealer;
            VehiclesDealerCoords = _vehicles_dealer;
        }
    }
}
