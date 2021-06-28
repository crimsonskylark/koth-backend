﻿using System.Collections.Generic;
using System.Linq;
using static CitizenFX.Core.Native.API;

namespace Server.User.Classes
{
    class Infantry : PlayerClass
    {
        public int ClassId { get => 1; }
        public string ClassName { get => "Infantaria"; }
        public int TimeToRevive { get => 20; }
        public List<int> AvailableWeapons
        {
            get => new()
            {
                GetHashKey("weapon_assaultrifle"),
                GetHashKey("weapon_carbinerifle"),
                GetHashKey("weapon_specialcarbine"),
                GetHashKey("weapon_bullpuprifle"),
                GetHashKey("weapon_militaryrifle")
            };
        }
        public int Model { get => GetHashKey("csb_ramp_marine"); }
        public uint DefaultWeapon { get => (uint)GetHashKey("weapon_assaultrifle"); }
        public int ReviveCooldown { get => 60; }

        public bool CanUseWeapon(int hash)
        {
            return AvailableWeapons.FirstOrDefault(wep_hash => wep_hash == hash) != default;
        }
    }
}