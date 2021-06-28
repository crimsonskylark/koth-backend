namespace Shared
{
    public class TeamContext
    {
        public float[] PlayerSpawnInformation { get; set; }
        public int PlayerModel;

        public float[] WeaponDealerCoords { get; set; }
        public int WeaponsDealerPedHandle;

        public float[] VehiclesDealerCoords { get; set; }
        public int VehiclesDealerPedHandle;

        public override string ToString ( )
        {
            return $"SpawnInformation({PlayerSpawnInformation[0]}, {PlayerSpawnInformation[1]}, {PlayerSpawnInformation[2]}";
        }
    }
}
