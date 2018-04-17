using System.Windows;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FuzzyString;

namespace SteamTools.Classes
{
    internal class DataAccess
    {
        private readonly char[] _invalidChars = Path.GetInvalidFileNameChars();

        private List<string> _steamGrids;
        public List<string> SteamGrids
        {
            get
            {
                if ((_steamGrids?.Count ?? 0) == 0)
                    _steamGrids = GetSteamGridDb();
                return _steamGrids;
            }
        }

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

        public List<string> GetSteamGridDb()
        {
            var grids = new List<string>();
            if (!File.Exists("SteamGridDb.json"))
                return grids;

            using (var sr = new StreamReader("SteamGridDb.json"))
            {
                var a = sr.ReadToEnd();
                var b = JsonConvert.DeserializeObject<dynamic>(a);
                var c = b.games;
                foreach (var g in c)
                {
                    grids.Add((string)g);
                }
                return grids;
            }
            
        }

        public List<ScreenShot> GetScreenShots(string user)
        {
            var shots = new List<ScreenShot>();
            var file = new FileInfo(Consts.ScreenShotDirectory + "/" + user + ".json");
            if (File.Exists(file.FullName))
                try
                {
                    using (var sr = new StreamReader(file.FullName))
                        shots = JsonConvert.DeserializeObject<ObservableCollection<ScreenShot>>(sr.ReadToEnd())
                            .ToList();
                }
                catch (Exception e)
                {
                    Logger.log(e);
                    MessageBox.Show(e.Message);
                }

            return shots;
        }

        public List<ScreenShot> GetGameScreenShots(int appId, string user)
        {
            var shots = new List<ScreenShot>();
            var file = new FileInfo(Consts.ScreenShotDirectory + "/" + user + ".json");
            if (File.Exists(file.FullName))
                try
                {
                    using (var sr = new StreamReader(file.FullName))
                        shots = JsonConvert.DeserializeObject<ObservableCollection<ScreenShot>>(sr.ReadToEnd()).ToList()
                            .Where(s => s.AppId.Equals(appId)).ToList();
                }
                catch (Exception e)
                {
                    Logger.log(e);
                    MessageBox.Show(e.Message);
                }

            return shots;

        }

        public async Task<bool> DownloadScreenShot(Stream s, string gamename, string filename)
        {
            var path = Consts.ScreenShotDirectory + Path.DirectorySeparatorChar + new string(gamename
                           .Where(x => !_invalidChars.Contains(x))
                           .ToArray()) + Path.DirectorySeparatorChar + filename;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var fs = File.Create(path))
            {
                await s.CopyToAsync(fs);
            }

            return true;
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
                    Logger.log(e);
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
                Logger.log(e);
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
                Logger.log(e);
                MessageBox.Show(e.Message);
            }
        }

        
    }
}
