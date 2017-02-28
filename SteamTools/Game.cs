using System.Collections.Generic;

namespace SteamTools
{
    public class Game
    {
        public int AppId { get; set; }
        public string Logo { get; set; }
        public string Name { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
        public bool ExistsInStore { get; set; } = true;
    }
}
