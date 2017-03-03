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
using System.Threading;
using System.Text.RegularExpressions;
using System.Reflection;
using SteamTools.Properties;
using System.Diagnostics;
using System.Net;

namespace SteamTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public List<User> Users { get; set; }
        private List<Game> AllGames { get; set; } 

        private static readonly Regex SWhitespace = new Regex(@"\s+");
        private static readonly Regex UrlMatch = new Regex(@"^((http:\/\/steamcommunity\.com\/groups\/)[a-zA-Z]+(\/)[a-zA-Z]+)$");
        private DataAccess dataAccess = new DataAccess();
        private static readonly string installedDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

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
        }

        public static string ReplaceWhitespace(string input, string replacement)
        {
            return Regex.Replace(input, @"(?<=^\s*)\s|\s(?=\s*$)", "");
        }

        public void ShowStats()
        {
            PlayerStats.Content = string.Format("{0} Users ({1} of which have private profiles)", Users.Count, Users.Count(u => u.PrivateProfile.Equals(true)));
            GameStats.Content = string.Format("{0} Games ({1} of which are no longer on the Steam Store)", Users.SelectMany(u => u.Games).GroupBy(g => g.AppId).Count(), Users.SelectMany(u => u.Games.Where(g => g.ExistsInStore.Equals(false))).GroupBy(g => g.AppId).Count());
            TagsStats.Content = string.Format("{0} unique tags", Users.SelectMany(u => u.Games).SelectMany(g => g.Tags).Distinct().Count());
        }

        private void UpdateUserGame(int appId, List<string> tags)
        {
            var gameName = "";
            foreach (var game in Users.SelectMany(user => user.Games.Where(game => game.AppId.Equals(appId))))
            {
                game.ExistsInStore = !tags.Count.Equals(0);
                game.Tags = tags;
                gameName = game.Name;
            }

            if (!tags.Count.Equals(0))
            {
                Label.Content = "Updating " + gameName;
                ShowStats();
            }

            Progress.Value++;
        }

        private void GetData()
        {
            Users = dataAccess.getCachedUsers(Settings.Default.groupUrl);
            AllGames = dataAccess.getCachedGames();
            ShowStats();
            Button.IsEnabled = true;
        }

        private void WriteData()
        {
            dataAccess.writeCachedUsers(GroupUrl.Text, Users);
            dataAccess.writeCachedGames(Users, AllGames);
        }

        public List<Game> GetGames(Stream response)
        {
            var games = new List<Game>();

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
                    games.AddRange(game);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            }

            games = games.OrderBy(x => x.Name).ToList();
            return games;
        }

        #region Tasks

        private async Task Update()
        {
            if (UrlMatch.IsMatch(GroupUrl.Text))
            {
                Render.IsEnabled = false;
                try
                {
                    var parser = new HtmlParser();
                    var http = new HttpClient();
                    var request = await http.GetAsync(GroupUrl.Text);
                    var response = await request.Content.ReadAsStreamAsync();
                    var document = parser.Parse(response);
 
                    IList<IElement> users = document.QuerySelectorAll(".member_block").ToList(); ;

                    await Task.WhenAll(buildUserTasks(users));

                    if (document.QuerySelectorAll(".pagelink").Any())
                    {
                        var lastPage = int.Parse(document.QuerySelector(".pageLinks").Children.Where(c => c.ClassName.Equals("pagelink")).Last().TextContent, System.Globalization.NumberStyles.AllowThousands);
                        for (int i = 2; i <= lastPage; i++)
                        {
                            using (var membersRequest = await http.GetAsync(GroupUrl.Text + "/?p=" + i))
                            using (var membersResponse = await membersRequest.Content.ReadAsStreamAsync())
                            using (var membersDocument = parser.Parse(membersResponse))
                                users = membersDocument.QuerySelectorAll(".member_block").ToList();

                            await Task.WhenAll(buildUserTasks(users));
                        }
                    }

                   await GetTags();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Please enter a valid Steam group members list URL");
            }
        }

        public IEnumerable<Task> buildUserTasks(IList<IElement> users)
        {
            var uiContext = TaskScheduler.FromCurrentSynchronizationContext();
            var downloadTasksQuery =
                          from usr in users
                          select GetUsers(usr).ContinueWith(t =>
                          {
                              if (!Users.Any(u => u.Name.Equals(t.Result.Name)))
                              {
                                  Users.Add(t.Result);
                                  ShowStats();
                              }
                              else
                              {
                                  foreach (var user in Users.Where(u => u.Name.Equals(t.Result.Name)))
                                  {
                                      user.Games = t.Result.Games;
                                      user.PrivateProfile = t.Result.PrivateProfile;
                                  }
                              }
                          }, uiContext);
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
                    user.Games.AddRange(games.Where(x => user.Games.All(y => y.AppId != x.AppId)));
                    user.Games = user.Games.OrderBy(x => x.Name).ToList();
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

        public async Task<List<string>> GetTags(string url)
        {
            var tags = new List<string>();

            try
            { 
                var handler = new HttpClientHandler { UseCookies = false };
                using (var http = new HttpClient(handler))
                {
                    var headMsg = new HttpRequestMessage(HttpMethod.Head, url);
                    headMsg.Headers.Add("Cookie", "birthtime=504921601; lastagecheckage=1-January-1986");
                    var headResult = await http.SendAsync(headMsg);
                    if (!headResult.RequestMessage.RequestUri.Equals("http://store.steampowered.com/") && !headResult.RequestMessage.RequestUri.Host.Equals("steamcommunity.com"))
                    {
                        var message = new HttpRequestMessage(HttpMethod.Get, url);
                        message.Headers.Add("Cookie", "birthtime=504921601; lastagecheckage=1-January-1986");
                        var result = await http.SendAsync(message);
                        result.EnsureSuccessStatusCode();
                        var parser = new HtmlParser();
                        var response = await result.Content.ReadAsStreamAsync();
                        var document = parser.Parse(response);

                        tags.AddRange(document.QuerySelectorAll(".app_tag").Where(t => t.TextContent.Replace(" ", "") != "+").Select(ele => ReplaceWhitespace(ele.TextContent, "")));
                        if (tags.Count == 0)
                            tags.Add("No tags");
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            return tags;
        }

        private async Task GetTags()
        {
            var allGameIds = Users.SelectMany(x => x.Games.Where(d => d.Tags.Count.Equals(0))).Select(f => f.AppId).Distinct().ToList();
            allGameIds.Sort();
            Progress.Visibility = System.Windows.Visibility.Visible;
            Progress.Maximum = allGameIds.Count;
            try
            {
                var maxThread = new SemaphoreSlim(10);
                var uiContext = TaskScheduler.FromCurrentSynchronizationContext();
                foreach (var appId in allGameIds)
                {
                    maxThread.Wait();
                    if (AllGames.Any(g => g.AppId.Equals(appId)) && AllGames.First(g => g.AppId.Equals(appId)).Tags.Count > 0)
                    {
                        UpdateUserGame(appId, AllGames.First(g => g.AppId.Equals(appId)).Tags);
                        maxThread.Release();
                    }
                    else
                    {
                        await GetTags("http://store.steampowered.com/app/" + appId).ContinueWith(task =>
                        {
                            if (task.IsFaulted)
                            {
                                Exception ex = task.Exception;
                                while (ex is AggregateException && ex.InnerException != null)
                                    ex = ex.InnerException;
                                MessageBox.Show(ex.Message);
                            } else {                             
                                UpdateUserGame(appId, task.Result);
                            }
                            maxThread.Release();
                        }, uiContext);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            Progress.Visibility = System.Windows.Visibility.Hidden;
            Label.Content = "Processing Complete!";

            WriteData();
            
            ShowStats();
            Render.IsEnabled = true;
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
            if (!UrlMatch.IsMatch(GroupUrl.Text))
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
            Process.Start(@installedDir);
        }

        private void processGameComp_Click(object sender, RoutedEventArgs e)
        {
            Renderer.Render(Users.ToList());
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Settings.Default.groupUrl = GroupUrl.Text;
            Settings.Default.Save();
            WriteData();
            base.OnClosing(e);
        }
        #endregion

    }
}
