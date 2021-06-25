using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace koth_server
{
    class TeamBase
    {
        public Queue<Spawn> spawn_points = new();

        public TeamBase ( )
        {
        }

        public Spawn GetSpawnPoint()
        {
            return spawn_points.Peek();
        }
    }
}
