using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared
{
    /*
     * This class represents a player inside the game mode.
     */
    class BasePlayer
    {
        public int Entity { get; private set; }
        public int Kills { get; private set; }
        public int Deaths { get; private set; }
        public int Points { get; private set; }
        public int Money { get; private set; }
    }
}
