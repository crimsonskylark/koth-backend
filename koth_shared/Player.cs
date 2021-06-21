using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace koth_shared
{
    /*
     * This class represents a player inside the game mode.
     */
    class BasePlayer
    {
        public int entity_handle { get; private set; }
        public int total_kills { get; private set; }
        public int total_deaths { get; private set; }
        public int total_points { get; private set; }
        public int total_money { get; private set; }
    }
}
