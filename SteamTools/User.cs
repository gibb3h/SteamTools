using System.Collections.Generic;

namespace SteamTools
{
    public class User
    {
        public string Name { get; set; }
        public string Logo { get; set; }
        public string ProfileUrl { get; set; }
        public List<Game> Games { get; set; } = new List<Game>();
        public string GameCount { get { return Games.Count.ToString(); } }
        public bool PrivateProfile { get; set; } = false;
    }
}
