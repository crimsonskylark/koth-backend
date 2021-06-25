using System.Collections.Generic;

namespace koth_server.User
{
    interface PlayerClass
    {
        public int ClassId { get; }
        public string ClassName { get; }
        public int TimeToRevive { get; }
        public List<int> AvailableWeapons { get; }
        public int Model { get; }
        public uint DefaultWeapon { get; }
        public int ReviveCooldown { get; }

        public bool CanUseWeapon ( int hash );
    }
}
