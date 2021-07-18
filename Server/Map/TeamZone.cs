﻿using CitizenFX.Core;
using System.Collections.Generic;

namespace koth_server.Map
{
    public class TeamZone
    {
        public float[] SafeZone { get; set; }
        public uint InfantryModel { get; set; }
        public float[] PlayerSpawnCoords { get; set; }
        public List<float[]> HeliSpawnPoints { get; set; }
        public List<float[]> CarSpawnPoints { get; set; }
        public float[] VehDealerCoords { get; set; }
        public float[] VehDealerPropCoords { get; set; }
    }
}
