using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shell;
using AngleSharp.Parser.Html;
using FuzzyString;
using Newtonsoft.Json;
using SteamTools.Classes;

namespace SteamTools
{
    /// <summary>
    /// Interaction logic for gameUpdate.xaml
    /// </summary>
    public partial class GameUpdate : Window
    {
        private List<Game> _currentCache;
        private readonly DataAccess _dataAccess = new DataAccess();
        private List<apiGame> _allApps = new List<apiGame>();

        public GameUpdate(List<Game> allGames)
        {
            _currentCache = allGames;
            InitializeComponent();

            GetLatestGames();
        }

        private void GetLatestGames()
        {
            try
            {
                var http = new HttpClient();
                var request = http.GetAsync(Consts.ApiUrl).Result;
                var response = request.Content.ReadAsStringAsync().Result;

                var allAppsContainer = JsonConvert.DeserializeObject<apiMain>(response);
                _allApps = allAppsContainer.applist.apps;
            }
            catch (Exception e)
            {
                
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _dataAccess.WriteCachedGames(_currentCache);
            base.OnClosing(e);
        }

        public async void Update()
        {
            var uiContext = TaskScheduler.FromCurrentSynchronizationContext();
            var stopTasks = false;
            taskBarItemInfo.ProgressValue = (double)_currentCache.Count / (double)_allApps.Count;
            GameCacheProgress.Value = _currentCache.Count;
            GameCacheProgress.Maximum = _allApps.Count;
            var i = GameCacheProgress.Value;
            foreach (
                var curr in
                    _allApps.Where(curr => !stopTasks).Where(curr => !_currentCache.Any(g => g.AppId.Equals(curr.appid)))
                )
            {
                i++;
                await GetDetails(curr).ContinueWith(task =>
                    {
                        if (!task.IsFaulted)
                        {
                            _currentCache.Add(task.Result);
                        }
                        else
                        {
                            Exception ex = task.Exception;
                            while (ex is AggregateException && ex.InnerException != null)
                                ex = ex.InnerException;
                            if (ex != null)
                            {
                                var continueRes = MessageBox.Show(ex.Message + Environment.NewLine+ "Continue with processing?", "Error!", MessageBoxButton.OKCancel);
                                if (continueRes.Equals(MessageBoxResult.Cancel))                            
                                    stopTasks = true;                              
                            }                      
                        }
                        ProgressText.Text = i + " of " + _allApps.Count;
                    }, uiContext);
            }


            _dataAccess.WriteCachedGames(_currentCache);

            taskBarItemInfo.ProgressState = TaskbarItemProgressState.None;
            var result =
                MessageBox.Show(
                    _currentCache.Count >= _allApps.Count
                        ? "processing complete!"
                        : "processing did not complete, please try again", "Results", MessageBoxButton.OK);
            if (result == MessageBoxResult.OK)
            {
                Close();
            }
        }

        public static string ReplaceWhitespace(string input, string replacement)
        {
            return Regex.Replace(input, @"(?<=^\s*)\s|\s(?=\s*$)", "");
        }

        private async Task<Game> GetDetails(apiGame curr)
        {
            var newGame = new Game {AppId = curr.appid, Name = curr.name, ExistsInStore = false};
            var handler = new HttpClientHandler { UseCookies = false };
            var tags = new List<string>();
            try
            {
                using (var http = new HttpClient(handler))
                {
                    var headMsg = new HttpRequestMessage(HttpMethod.Head,
                                                         "http://store.steampowered.com/app/" + newGame.AppId);
                    headMsg.Headers.Add("Cookie", Consts.MatureCookies);
                    var headResult = await http.SendAsync(headMsg);
                    if (
                        !headResult.RequestMessage.RequestUri.OriginalString.Equals("http://store.steampowered.com/") &&
                        !headResult.RequestMessage.RequestUri.Host.Equals("steamcommunity.com"))
                    {
                        if (!headResult.RequestMessage.RequestUri.Segments[1].Equals("app/"))
                        {
                            NameLabel.Content = newGame.Name + " is not a game";
                            NameLabel.Foreground = new SolidColorBrush(Colors.Red);
                        }
                        else
                        {
                            var message = new HttpRequestMessage(HttpMethod.Get,
                                                                 "http://store.steampowered.com/app/" + newGame.AppId);
                            message.Headers.Add("Cookie",
                                                Consts.MatureCookies);
                            var result = await http.SendAsync(message);
                            result.EnsureSuccessStatusCode();
                            var parser = new HtmlParser();
                            var response = await result.Content.ReadAsStreamAsync();
                            var document = parser.Parse(response);
                            if (!document.QuerySelectorAll("#error_box").Any())
                            {
                                newGame.Logo = document.QuerySelector(".game_header_image_full").GetAttribute("src");
                                if (string.IsNullOrEmpty(newGame.Logo))
                                    newGame.Logo = await GetLogo(newGame.Name);
                                newGame.ExistsInStore = true;
                                tags.AddRange(
                                    document.QuerySelectorAll(".app_tag")
                                            .Where(t => t.TextContent.Replace(" ", "") != "+")
                                            .Select(ele => ReplaceWhitespace(ele.TextContent, "")));
                                if (tags.Count == 0)
                                    tags.Add("No tags");

                                NameLabel.Content = newGame.Name + " Found in store";
                                NameLabel.Foreground = new SolidColorBrush(Colors.Green);
                            }
                            else
                            {
                                NameLabel.Content = newGame.Name + " Not in store";
                                NameLabel.Foreground = new SolidColorBrush(Colors.Red);
                            }
                        }
                    }
                    else
                    {
                        NameLabel.Content = newGame.Name + " Not in store";
                        NameLabel.Foreground = new SolidColorBrush(Colors.Red);
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            newGame.Tags = tags;
           
            return newGame;
        }

        private async Task<string> GetLogo(string name)
        {

            var nme = _dataAccess.SteamGrids.Where(a => a.ApproximatelyEquals(name,
                new List<FuzzyStringComparisonOptions>()
                {
                    FuzzyStringComparisonOptions.UseOverlapCoefficient,
                    FuzzyStringComparisonOptions.UseLongestCommonSubsequence,
                    FuzzyStringComparisonOptions.UseLongestCommonSubstring
                }, FuzzyStringComparisonTolerance.Strong));
            try
            {
                using (var http = new HttpClient())
                {
                    var request = await http.GetAsync($"http://www.steamgriddb.com/api/grids?game={nme.First()}");
                    var response = await request.Content.ReadAsStreamAsync();
                    request.EnsureSuccessStatusCode();
                    using (var sr = new StreamReader(response))
                        return JsonConvert.DeserializeObject<dynamic>(sr.ReadToEnd()).data[0].grid_url;
                }
            }
            catch (Exception ex)
            {
                Logger.log(ex);
                //   MessageBox.Show(ex.Message);
            }

            return "";


        }

        public bool UpdatesAvailable()
        {
            return _allApps.Count > _currentCache.Count;
        }

        internal class apiGame
        {
            public int appid;
            public string name;

        }

        internal class apiGameList
        {
            public List<apiGame> apps;
        }

        internal class apiMain
        {
            public apiGameList applist;
        }

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            taskBarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            Update();
        }
    }
}
