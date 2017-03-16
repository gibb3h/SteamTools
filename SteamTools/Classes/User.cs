using System.Collections.Generic;

namespace SteamTools.Classes
{
    public class User
    {
        public string Name { get; set; }
        public string Logo { get; set; }
        public string ProfileUrl { get; set; }
        public List<int> Games { get; set; }
        public string GameCount { get { return Games.Count.ToString(); } }
        public bool PrivateProfile { get; set; }

        public User()
        {
            Games = new List<int>();
            PrivateProfile = false;
        }
    }
}
