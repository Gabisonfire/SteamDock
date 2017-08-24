using System;
using Steam.Local;
using System.IO;

namespace Steam_Dock
{
    class Game : LocalApp, IComparable<Game>
    {
        public bool Visible { get; set; }
        public string Exec { get; set; }
        public string SafeName { get; set; }

        public Game(string manifest, bool Visible = true): base(manifest)
        {
            this.Visible = Visible;
            Exec = "steam://rungameid/" + ID.ToString();
            SafeName = new DirectoryInfo(InstallDir).Name;
        }

        public int CompareTo(Game other)
        {
            return Name.CompareTo(other.Name);
        }             
    }
}
