using System.Collections.Generic;
using System.Linq;
using static CitizenFX.Core.Native.API;

namespace Server.User.Classes
{
    class Infantry : IPlayerClass
    {
        public int ClassId { get => 1; }
        public string ClassName { get => "Infantaria"; }
        public int TimeToRevive { get => 15; }

        public Infantry(uint Uniform)
        {
            Model = (int)Uniform;
        }

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
        public int Model { get; private set; }
        public uint DefaultWeapon { get => (uint)GetHashKey("weapon_assaultrifle"); }
        public int ReviveCooldown { get => 30; }

        public bool CanUseWeapon(int hash)
        {
            return AvailableWeapons.FirstOrDefault(wep_hash => wep_hash == hash) != default;
        }
    }
}
