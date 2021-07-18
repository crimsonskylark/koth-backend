using Server.User.Classes;
using System.Collections.Generic;

using static CitizenFX.Core.Native.API;

namespace Server.Teams
{
    class AEGIS : KothTeam
    {
        public AEGIS ( ) : base( 1, "AEGIS Corp.", new( ) )
        {
            //Base.SpawnPoints.Enqueue( new Spawn( new float[] { -1315.25f, 278.37f, 63.67f, 268.84f },
            //      new List<float[]>( ) { new float[] { -1291.29f, 285.36f, 64.8f, 253.87f },
            //                            new float[] { -1293.44f, 282.07f, 64.79f, 253.87f },
            //                            new float[] {-1288.82f, 291.33f, 64.81f, 234.83f } },
            //      new List<float[]>( ) { new float[] {-1306.41f, 294.13f, 64.81f, 250.6f },
            //                            new float[] {-1307.25f, 280.53f, 64.24f, 148.0f },
            //                            new float[] {-1303.21f, 278.19f, 64.28f, 154.12f } },
            //      new float[] { -1307.25f, 280.53f, 64.24f, 148.0f },
            //      new float[] { -1305.07f, 276.79F, 64.1f, 119.55f }
            //  ) );
            PlayerClasses.Add( 1, new Infantry( (uint)GetHashKey( "s_m_y_marine_03" ) ) );
        }
    }
}
