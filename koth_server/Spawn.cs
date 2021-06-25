using CitizenFX.Core;
using System;
using System.Collections.Generic;

using static CitizenFX.Core.Native.API;

namespace koth_server
{
    class Spawn : BaseScript
    {
        public float[] player_spawn { get; set; }
        public List<float[]> vehicle_spawn { get; set; }
        public List<float[]> air_spawn { get; set; }
        public float[] weapons_dealer { get; set; }
        public float[] vehicles_dealer { get; set; }
        public bool inUse { get; set; }

        public Spawn ( float[] _player_spawn, List<float[]> _vehicle_spawn, List<float[]> _air_spawn, float[] _weapons_dealer, float[] _vehicles_dealer )
        {
            player_spawn = _player_spawn;
            vehicle_spawn = _vehicle_spawn;
            air_spawn = _air_spawn;
            weapons_dealer = _weapons_dealer;
            vehicles_dealer = _vehicles_dealer;
        }
    }
}
