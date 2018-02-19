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
using SteamTools.Classes;
using SteamTools.Properties;
using System.Diagnostics;
using System.Net;
using System.Windows.Shell;
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
                if (System.Deployment.Application.ApplicationDeployment.CurrentDeployment != null &&
                    System.Deployment.Application.ApplicationDeployment.CurrentDeployment.IsFirstRun)
                    new ChangeLog().Show();
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
            PlayerStats.Content = string.Format("{0} Users ({1} of which have private profiles)", Users.Count,
                Users.Count(u => u.PrivateProfile.Equals(true)));
            GameStats.Content = string.Format("{0} Games ({1} of which are no longer on the Steam Store)",
                allGames.Count(), AllGames.Count(g => g.ExistsInStore.Equals(false) && allGames.Contains(g.AppId)));
            TagsStats.Content = string.Format("{0} unique tags",
                AllGames.Where(g => allGames.Contains(g.AppId)).SelectMany(g => g.Tags).Distinct().Count());
            ScreenStats.Content = string.Format("{0} Screenshots",
                Users.Sum(u => _dataAccess.GetScreenShots(u.SteamId).Count));
        }

        private List<int> GetUserGameIds()
        {
            var allGameIds = AllGames.Select(f => f.AppId).Distinct().ToList();
            var userGameIds = Users.SelectMany(u => u.Games).Distinct().ToList();
            allGameIds = allGameIds.Where(userGameIds.Contains).ToList();
            allGameIds.Sort();
            return allGameIds;
        }

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
            var tmp2 = str.Split(new[] {"[{"}, StringSplitOptions.None);

            if (tmp2.Length >= 2)
            {
                var tmp3 = tmp2[1].Split(new[] {"}];"}, StringSplitOptions.None);
                try
                {
                    var jsonStr = string.Format("{0}{1}{2}", "[{", tmp3[0], "}]");
                    var game = JsonConvert.DeserializeObject<List<Game>>(jsonStr);
                    if (_allOk)
                    {
                        games.AddRange(game.Select(g => g.AppId).ToList());
                    }
                }
                catch (Exception e)
                {
                    Logger.log(e);
                    MessageBox.Show(e.Message);
                }
            }

            return games;
        }

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
                    taskBarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
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
                        _dataAccess.WriteScreenShots(u.SteamId,
                            await
                                screenshotScraper.GetScreenShots(u.ProfileUrl,
                                    _dataAccess.GetScreenShots(u.SteamId)));
                        Progress.Value++;
                        taskBarItemInfo.ProgressValue = Progress.Value / Progress.Maximum;
                        ShowStats();
                    }

                    Label.Content = "Processing Complete!";
                    Render.IsEnabled = true;
                }
                catch (Exception ex)
                {
                    Logger.log(ex);
                    Label.Content = "An error occured";
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    Progress.Visibility = Visibility.Hidden;
                    taskBarItemInfo.ProgressState = TaskbarItemProgressState.None;
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
                    if (!Users.Any(u => u.SteamId.Equals(t.Result.SteamId)))
                    {
                        Users.Add(t.Result);
                        Dispatcher.Invoke(ShowStats);
                    }
                    else
                    {
                        foreach (var user in Users.Where(u => u.SteamId.Equals(t.Result.SteamId)))
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
            var user = Users.Where(u => u.Name.Equals(usr.QuerySelector(".linkFriend").TextContent))
                .DefaultIfEmpty(new User()).First();
            user.Name = usr.QuerySelector(".linkFriend").TextContent;
            user.Logo = usr.QuerySelector(".playerAvatar a img").GetAttribute("src").Replace(".jpg", "_full.jpg");
            user.ProfileUrl = usr.QuerySelector(".playerAvatar a").GetAttribute("href");

            var http = new HttpClient();
            var request = await http.GetAsync(user.ProfileUrl + "/games?tab=all");
            if (request.StatusCode.Equals((HttpStatusCode) 429))
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
                    request = await http.PostAsync("https://steamid.io/lookup",
                        new FormUrlEncodedContent(new Dictionary<string, string> {{"input", user.ProfileUrl}}));
                    response = await request.Content.ReadAsStreamAsync();
                    var parser = new HtmlParser();
                    var document = parser.Parse(response);
                    var tmp = document.QuerySelectorAll("script")[0].InnerHtml.Trim().TrimEnd(';');
                    var script = JsonConvert.DeserializeObject<dynamic>(tmp);
                    user.SteamId = script.url.ToString().Split('/')[4];
                }
                catch (Exception ex)
                {
                    if (ex.Message.Equals("Profile is Private"))
                        user.PrivateProfile = true;
                }
            }
            return user;
        }

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
                Button.IsEnabled = true;
            }
        }

        private void showFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(InstalledDir);
        }

        private void processGameComp_Click(object sender, RoutedEventArgs e)
        {
            var comp = new Comparison(AllGames, Users) {WindowStartupLocation = WindowStartupLocation.CenterScreen};
            comp.SourceInitialized += (s, a) => comp.WindowState = WindowState.Maximized;
            comp.Show();
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
