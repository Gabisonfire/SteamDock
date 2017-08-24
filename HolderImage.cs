using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Steam_Dock
{
    // Extending Image class to be able to hold a "Game" object
    class HolderImage : Image
    {
        public Game game { get; set; }
    }
}
