using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using AngleSharp.Dom;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Reflection;
using SteamTools.Properties;
using System.Diagnostics;
using System.Net;
using Visibility = System.Windows.Visibility;

namespace SteamTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {

        public List<User> Users { get; set; }
        private List<Game> AllGames { get; set; }

        private readonly DataAccess _dataAccess = new DataAccess();
        private static readonly string InstalledDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private bool _allOk = true;

        public MainWindow()
        {
            try
            {
                if (System.Deployment.Application.ApplicationDeployment.CurrentDeployment != null && System.Deployment.Application.ApplicationDeployment.CurrentDeployment.IsFirstRun)
                    new changeLog().Show();
            }
            catch 
            {

            }

            InitializeComponent();
            GetData();
            GroupUrl.Text = Settings.Default.groupUrl ?? string.Empty;
            CheckForUpdates();
        }

        public static string ReplaceWhitespace(string input, string replacement)
        {
            return Regex.Replace(input, @"(?<=^\s*)\s|\s(?=\s*$)", "");
        }

        public void ShowStats()
        {
            var allGames = GetUserGameIds();
            PlayerStats.Content = string.Format("{0} Users ({1} of which have private profiles)", Users.Count, Users.Count(u => u.PrivateProfile.Equals(true)));
            GameStats.Content = string.Format("{0} Games ({1} of which are no longer on the Steam Store)", allGames.Count(), AllGames.Count(g => g.ExistsInStore.Equals(false) && allGames.Contains(g.AppId)));
            TagsStats.Content = string.Format("{0} unique tags", AllGames.Where(g => allGames.Contains(g.AppId)).SelectMany(g => g.Tags).Distinct().Count());
            ScreenStats.Content = string.Format("{0} Screenshots",
                                                Users.Sum(u => _dataAccess.GetScreenShots(u.Name).Count));
        }

        private List<int> GetUserGameIds()
        {
            var allGameIds = AllGames.Select(f => f.AppId).Distinct().ToList();
            var userGameIds = Users.SelectMany(u => u.Games).Distinct().ToList();
            allGameIds = allGameIds.Where(userGameIds.Contains).ToList();
            allGameIds.Sort();
            return allGameIds;
        }

        //private void UpdateUserGame(int appId, List<string> tags)
        //{
        //    var gameName = "";
        //    foreach (var game in AllGames.Where(game => game.AppId.Equals(appId)))
        //    {
        //        game.ExistsInStore = !tags.Count.Equals(0);
        //        game.Tags = tags;
        //        gameName = game.Name;
        //    }

        //    if (!tags.Count.Equals(0))
        //    {
        //        Label.Content = "Updating " + gameName;
        //        ShowStats();
        //    }
        //}

        private void GetData()
        {
            Users = _dataAccess.GetCachedUsers(Settings.Default.groupUrl);
            var dbg = _dataAccess.GetCachedGames();
            AllGames = dbg.GroupBy(x => x.AppId).Select(y => y.First()).ToList(); 
            ShowStats();
            Button.IsEnabled = true;
        }

        private void WriteData()
        {
            _dataAccess.WriteCachedUsers(GroupUrl.Text, Users);
//            _dataAccess.WriteCachedGames(AllGames);
        }

        public List<int> GetGames(Stream response)
        {
            var games = new List<int>();

            var parser = new HtmlParser();
            var document = parser.Parse(response);
            if (document.QuerySelectorAll(".profile_private_info").Any())
            {
                throw new Exception("Profile is Private");
            }
            var tmp = document.QuerySelectorAll("script");
            var gameList = tmp[tmp.Length - 1];
            var str = gameList.InnerHtml;
            var tmp2 = str.Split(new[] { "[{" }, StringSplitOptions.None);

            if (tmp2.Length >= 2)
            {
                var tmp3 = tmp2[1].Split(new[] { "}];" }, StringSplitOptions.None);
                try
                {
                    var jsonStr = string.Format("{0}{1}{2}", "[{", tmp3[0], "}]");
                    var game = JsonConvert.DeserializeObject<List<Game>>(jsonStr);
                    if (_allOk)
                    {
 //                       var res = await AddAllGames(game);
                        games.AddRange(game.Select(g => g.AppId).ToList());
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }

            return games;
        }

        //private async Task<List<int>> AddAllGames(List<Game> game)
        //{
        //    var maxThread = new SemaphoreSlim(10);
        //    var uiContext = TaskScheduler.FromCurrentSynchronizationContext();
        //    foreach (
        //        var currGame in
        //            game.Where(
        //                g =>
        //                !AllGames.Any(g2 => g2.AppId.Equals(g.AppId)) ||
        //                AllGames.Any(g3 => g3.AppId.Equals(g.AppId) && g3.Tags.Count.Equals(0))))
        //    {
        //        if (_allOk)
        //        {
        //            maxThread.Wait();
        //            //if (AllGames.Any(g => g.AppId.Equals(currGame.AppId)) &&
        //            //    AllGames.First(g => g.AppId.Equals(currGame.AppId)).Tags.Count > 0)
        //            if (AllGames.Any(g => g.AppId.Equals(currGame.AppId)))
        //            {
        //                UpdateUserGame(currGame.AppId, AllGames.First(g => g.AppId.Equals(currGame.AppId)).Tags);
        //                maxThread.Release();
        //            }
        //            else
        //            {
        //                AllGames.Add(currGame);
        //                var game1 = currGame;
        //                await GetTags("http://store.steampowered.com/app/" + currGame.AppId).ContinueWith(task =>
        //                    {
        //                        if (task.IsFaulted)
        //                        {
        //                            _allOk = false;
        //                            Exception ex = task.Exception;
        //                            while (ex is AggregateException && ex.InnerException != null)
        //                                ex = ex.InnerException;
        //                            if (ex != null) MessageBox.Show(ex.Message);
        //                        }
        //                        else
        //                        {
        //                            UpdateUserGame(game1.AppId, task.Result);
        //                        }
        //                        maxThread.Release();

        //                    }, uiContext);
        //            }
        //            Progress.Value++;
        //        }
        //    }

        //    return game.Select(g => g.AppId).ToList();
        //    //           AllGames.AddRange(game.Where(g => !AllGames.Any(g2 => g2.AppId.Equals(g.AppId))));
        //}

        #region Tasks

        private async Task Update()
        {
            if (Consts.UrlMatch.IsMatch(GroupUrl.Text))
            {
                Render.IsEnabled = false;
                try
                {
                    Progress.Maximum = GetUserGameIds().Count;
                    Progress.Visibility = Visibility.Visible;
                    var parser = new HtmlParser();
                    var http = new HttpClient();
                    var request = await http.GetAsync(GroupUrl.Text);
                    var response = await request.Content.ReadAsStreamAsync();
                    var document = parser.Parse(response);

                    IList<IElement> users = document.QuerySelectorAll(".member_block").ToList();

                    await Task.WhenAll(BuildUserTasks(users));

                    if (document.QuerySelectorAll(".pagelink").Any())
                    {
                        var lastPage =
                            int.Parse(
                                document.QuerySelector(".pageLinks")
                                        .Children.Last(c => c.ClassName.Equals("pagelink"))
                                        .TextContent, System.Globalization.NumberStyles.AllowThousands);
                        for (var i = 2; i <= lastPage; i++)
                        {
                            using (var membersRequest = await http.GetAsync(GroupUrl.Text + "/?p=" + i))
                            using (var membersResponse = await membersRequest.Content.ReadAsStreamAsync())
                            using (var membersDocument = parser.Parse(membersResponse))
                                users = membersDocument.QuerySelectorAll(".member_block").ToList();

                            await Task.WhenAll(BuildUserTasks(users));
                        }
                    }
                    WriteData();
                    var screenshotScraper = new ScreenshotScraper();
                    Progress.Maximum = users.Count;
                    Progress.Value = 0;
                    foreach (var u in Users)
                    {
                        Label.Content = "Getting Screenshots for " + u.Name;
                        _dataAccess.WriteScreenShots(u.Name,
                                                     await
                                                     screenshotScraper.GetScreenShots(u.ProfileUrl,
                                                                                      _dataAccess.GetScreenShots(u.Name)));
                        Progress.Value++;
                        ShowStats();
                    }

                    Label.Content = "Processing Complete!";                   
                    Render.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    Label.Content = "An error occured";
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    Progress.Visibility = Visibility.Hidden;
                    ShowStats();
                }

            }
            else
            {
                MessageBox.Show("Please enter a valid Steam group members list URL");
            }
        }

        public IEnumerable<Task> BuildUserTasks(IList<IElement> users)
        {
            var downloadTasksQuery =
                          from usr in users
                          select GetUsers(usr).ContinueWith(t =>
                          {
                              if (!Users.Any(u => u.Name.Equals(t.Result.Name)))
                              {
                                  Users.Add(t.Result);
                                  Dispatcher.Invoke(ShowStats);                              
                              }
                              else
                              {
                                  foreach (var user in Users.Where(u => u.Name.Equals(t.Result.Name)))
                                  {
                                      user.Games = t.Result.Games;
                                      user.PrivateProfile = t.Result.PrivateProfile;
                                  }
                              }
                          });
            return downloadTasksQuery;       
        }

        public async Task<User> GetUsers(IElement usr)
        {
            var user = Users.Where(u => u.Name.Equals(usr.QuerySelector(".linkFriend").TextContent)).DefaultIfEmpty(new User()).First();
            user.Name = usr.QuerySelector(".linkFriend").TextContent;
            user.Logo = usr.QuerySelector(".playerAvatar a img").GetAttribute("src").Replace(".jpg", "_full.jpg");
            user.ProfileUrl = usr.QuerySelector(".playerAvatar a").GetAttribute("href");
            var http = new HttpClient();
            var request = await http.GetAsync(user.ProfileUrl + "/games?tab=all");
            if (request.StatusCode.Equals((HttpStatusCode)429))
            {
                MessageBox.Show("Steam has stop accepting requests, you will have to wait a while!");
                Application.Current.Shutdown();
            }
            if (request.IsSuccessStatusCode)
            {
                var response = await request.Content.ReadAsStreamAsync();
                try
                {
                    var games = GetGames(response);
                    user.Games.AddRange(games.Where(x => user.Games.All(y => y != x)));
                    user.PrivateProfile = false;
                }
                catch (Exception ex)
                {
                    if (ex.Message.Equals("Profile is Private"))
                        user.PrivateProfile = true;
                }
            }
            return user;
        }

        //public async Task<List<string>> GetTags(string url)
        //{
        //    var tags = new List<string>();
        //    if (_allOk)
        //    {
        //        try
        //        {
        //            var handler = new HttpClientHandler {UseCookies = false};
        //            using (var http = new HttpClient(handler))
        //            {
        //                var headMsg = new HttpRequestMessage(HttpMethod.Head, url);
        //                headMsg.Headers.Add("Cookie", "birthtime=504921601; lastagecheckage=1-January-1986");
        //                var headResult = await http.SendAsync(headMsg);
        //                if (
        //                    !headResult.RequestMessage.RequestUri.OriginalString.Equals("http://store.steampowered.com/") &&
        //                    !headResult.RequestMessage.RequestUri.Host.Equals("steamcommunity.com"))
        //                {
        //                    var message = new HttpRequestMessage(HttpMethod.Get, url);
        //                    message.Headers.Add("Cookie", "birthtime=504921601; lastagecheckage=1-January-1986");
        //                    var result = await http.SendAsync(message);
        //                    result.EnsureSuccessStatusCode();
        //                    var parser = new HtmlParser();
        //                    var response = await result.Content.ReadAsStreamAsync();
        //                    var document = parser.Parse(response);

        //                    tags.AddRange(
        //                        document.QuerySelectorAll(".app_tag")
        //                                .Where(t => t.TextContent.Replace(" ", "") != "+")
        //                                .Select(ele => ReplaceWhitespace(ele.TextContent, "")));
        //                    if (tags.Count == 0)
        //                        tags.Add("No tags");
        //                }
        //            }
        //        }
        //        catch (Exception e)
        //        {
        //            throw e;
        //        }
        //    }
        //    return tags;
        //}

        #endregion

        #region Events


        private async void button_Click(object sender, RoutedEventArgs e)
        {
            await Update();
        }

        private void GroupUrl_LostFocus(object sender, RoutedEventArgs e)
        {
            GroupUrl.Text = GroupUrl.Text.Replace("#", "/");
            if (!Consts.UrlMatch.IsMatch(GroupUrl.Text))
            {
                MessageBox.Show("Please enter a valid Steam group members list URL");
                GroupUrl.Text = Settings.Default.groupUrl;
                Button.IsEnabled = false;
            }
            else
            {
                Settings.Default.groupUrl = GroupUrl.Text;
                GetData();
            }
        }

        private void showFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(InstalledDir);
        }

        private void processGameComp_Click(object sender, RoutedEventArgs e)
        {
            Renderer.Render(Users.ToList(),AllGames);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Settings.Default.groupUrl = GroupUrl.Text;
            Settings.Default.Save();
            WriteData();
            base.OnClosing(e);
            Application.Current.Shutdown();
        }
        #endregion

        private void RefreshGameCache_Click(object sender, RoutedEventArgs e)
        {
            new GameUpdate(AllGames).ShowDialog(); 
        }

        private void CheckForUpdates()
        {
            var gameUpdate = new GameUpdate(AllGames);
            if (gameUpdate.UpdatesAvailable())
            {
                MessageBox.Show("There are new games on Steam, please update your cache");
                UpdateCache.Visibility = Visibility.Visible;
            }
        }
    }
}
