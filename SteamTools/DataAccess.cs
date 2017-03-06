using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace SteamTools
{
    class DataAccess
    {
        public List<User> GetCachedUsers(string groupUrl)
        {
            var users = new List<User>();
            if (!string.IsNullOrEmpty(groupUrl))
            {
                var groupUri = new Uri(groupUrl);
                if (File.Exists(groupUri.Segments[2].Replace("/", "") + ".json"))
                    users = JsonConvert.DeserializeObject<ObservableCollection<User>>(File.ReadAllText(groupUri.Segments[2].Replace("/", "") + ".json")).ToList();
            }

            return users;
        }

        public List<Game> GetCachedGames()
        {
           return File.Exists("cachedGames.json") ? JsonConvert.DeserializeObject<List<Game>>(File.ReadAllText("cachedGames.json")) : new List<Game>();
        }

        public void WriteCachedUsers(string groupUrl, List<User> users)
        {
            if (!string.IsNullOrEmpty(groupUrl))
            {
                var groupUri = new Uri(groupUrl);
                var jsonName = groupUri.Segments[2].Replace("/", "") + ".json";
                File.WriteAllText(jsonName, JsonConvert.SerializeObject(users));
            }
        }

        public void WriteCachedGames(List<Game> allGames)
        {
            File.WriteAllText("cachedGames.json", JsonConvert.SerializeObject(allGames));
        }
    }
}
