using System.Collections.Generic;

namespace Server.User
{
    interface IPlayerClass
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
