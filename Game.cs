using System;

namespace Steam_Game_Launcher
{
    class Game : IComparable<Game>
    {
        public string name { get; set; }
        public string ID { get; set; }
        public string url { get; set; }
        public string install_dir { get; set; }
        public bool Visible { get; set; }


        public Game(string name, string ID, string install_dir, bool Visible = true)
        {
            this.name = name;
            this.ID = ID;
            this.install_dir = install_dir;
            this.url = "steam://rungameid/" + ID;
            this.Visible = Visible;
        }

        public int CompareTo(Game other)
        {
            return name.CompareTo(other.name);
        }

             
    }
}
