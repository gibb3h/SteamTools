using System.Windows;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace SteamTools.Classes
{
    internal class DataAccess
    {
        public List<User> GetCachedUsers(string groupUrl)
        {
            var users = new List<User>();
            if (!string.IsNullOrEmpty(groupUrl))
            {
                var groupUri = new Uri(groupUrl);
                if (File.Exists(groupUri.Segments[2].Replace("/", "") + ".json"))
                    using (var sr = new StreamReader(groupUri.Segments[2].Replace("/", "") + ".json"))
                        users = JsonConvert.DeserializeObject<ObservableCollection<User>>(sr.ReadToEnd()).ToList();
            }
            return users;
        }

        public List<Game> GetCachedGames()
        {
            if (!File.Exists("cachedGames.json"))
                return new List<Game>();

            using (var sr = new StreamReader("cachedGames.json"))
                return JsonConvert.DeserializeObject<List<Game>>(sr.ReadToEnd());
        }

        public List<ScreenShot> GetScreenShots(string user)
        {
            var shots = new List<ScreenShot>();
            var file = new FileInfo(Consts.ScreenShotDirectory + "/" + user + ".json");
            if (File.Exists(file.FullName))
                try
                {
                    using (var sr = new StreamReader(file.FullName))
                        shots = JsonConvert.DeserializeObject<ObservableCollection<ScreenShot>>(sr.ReadToEnd()).ToList();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            return shots;
        }

        public void WriteCachedUsers(string groupUrl, List<User> users)
        {
            if (!string.IsNullOrEmpty(groupUrl))
            {
                var groupUri = new Uri(groupUrl);
                var jsonName = groupUri.Segments[2].Replace("/", "") + ".json";
                try
                {
                    File.WriteAllText(jsonName, JsonConvert.SerializeObject(users));
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }
        }

        public void WriteCachedGames(List<Game> allGames)
        {
            try
            {
                File.WriteAllText("cachedGames.json", JsonConvert.SerializeObject(allGames));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        public void WriteScreenShots(string user, List<ScreenShot> shots)
        {
            if (!Directory.Exists(Consts.ScreenShotDirectory))
            {
                Directory.CreateDirectory(Consts.ScreenShotDirectory);
            }
            var file = new FileInfo(Consts.ScreenShotDirectory + "/" + user + ".json");
            try
            {

                File.WriteAllText(file.FullName, JsonConvert.SerializeObject(shots));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }
    }
}
