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
        public List<User> getCachedUsers(string groupUrl)
        {
            var Users = new List<User>();
            if (!string.IsNullOrEmpty(groupUrl))
            {
                var _groupUri = new Uri(groupUrl);
                if (File.Exists(_groupUri.Segments[2].Replace("/", "") + ".json"))
                    Users = JsonConvert.DeserializeObject<ObservableCollection<User>>(File.ReadAllText(_groupUri.Segments[2].Replace("/", "") + ".json")).ToList();
            }

            return Users;
        }

        public List<Game> getCachedGames()
        {
           return File.Exists("cachedGames.json") ? JsonConvert.DeserializeObject<List<Game>>(File.ReadAllText("cachedGames.json")) : new List<Game>();
        }

        public void writeCachedUsers(string groupUrl, List<User> Users)
        {
            if (!string.IsNullOrEmpty(groupUrl))
            {
                var _groupUri = new Uri(groupUrl);
                var jsonName = _groupUri.Segments[2].Replace("/", "") + ".json";
                File.WriteAllText(jsonName, JsonConvert.SerializeObject(Users));
            }
        }

        public void writeCachedGames(List<User> users, List<Game> allGames)
        {
            if (users.Count > 0)
            {
                var newGames = users.SelectMany(u => u.Games).GroupBy(g => g.AppId).Select(g => g.First()).ToList();
                var uniqueList = allGames.Concat(newGames).GroupBy(item => item.AppId).Select(group => group.First()).ToList();
                if (uniqueList.Count > allGames.Count)
                    File.WriteAllText("cachedGames.json", JsonConvert.SerializeObject(uniqueList));
            }
        }
    }
}
