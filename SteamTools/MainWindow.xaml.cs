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
using System.Collections.ObjectModel;
using System.Threading;
using System.Text.RegularExpressions;
using System.Text;
using System.Reflection;
using SteamTools.Properties;
using System.Diagnostics;
using System.Net;
using System.Deployment;

namespace SteamTools
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public ObservableCollection<User> Users { get; set; }
        private List<Game> AllGames { get; set; } 

        private static readonly Regex SWhitespace = new Regex(@"\s+");
        private static readonly Regex UrlMatch = new Regex(@"^((http:\/\/steamcommunity\.com\/groups\/)[a-zA-Z]+(\/)[a-zA-Z]+)$");
        private Uri _groupUri;

        private static readonly string installedDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public MainWindow()
        {
            if (System.Deployment.Application.ApplicationDeployment.CurrentDeployment.IsFirstRun)
            {
                new changeLog().Show();
            }
               

            InitializeComponent();
            GetData();
            GroupUrl.Text = Settings.Default.groupUrl ?? string.Empty;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Settings.Default.groupUrl = GroupUrl.Text;
            Settings.Default.Save();
            WriteData();
            base.OnClosing(e);
        }

        public static string ReplaceWhitespace(string input, string replacement)
        {
            //SWhitespace.Replace(input, replacement);
            return Regex.Replace(input, @"(?<=^\s*)\s|\s(?=\s*$)", "");
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Update();           
        }

        private async void Update()
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

                    buildUserTasks(users);

                    if (document.QuerySelectorAll(".pageLinks").Any())
                    {
                        var lastPage = int.Parse(document.QuerySelector(".pageLinks").Children.Where(c => c.ClassName.Equals("pagelink")).Last().TextContent, System.Globalization.NumberStyles.AllowThousands);
                        for (int i = 2; i <= lastPage; i++)
                        {
                            using (var membersRequest = await http.GetAsync(GroupUrl.Text + "/?p=" + i))
                            using (var membersResponse = await membersRequest.Content.ReadAsStreamAsync())
                            using (var membersDocument = parser.Parse(membersResponse))
                                users = membersDocument.QuerySelectorAll(".member_block").ToList();

                            buildUserTasks(users);
                        }
                    }

                    GetTags();
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


        public async void buildUserTasks(IList<IElement> users)
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
            await Task.WhenAll(downloadTasksQuery);
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

        public void ShowStats()
        {
            PlayerStats.Content = string.Format("{0} Users ({1} of which have private profiles)", Users.Count, Users.Count(u => u.PrivateProfile.Equals(true)));
            GameStats.Content = string.Format("{0} Games ({1} of which are no longer on the Steam Store)", Users.SelectMany(u => u.Games).GroupBy(g => g.AppId).Count(), Users.SelectMany(u => u.Games.Where(g => g.ExistsInStore.Equals(false))).GroupBy(g => g.AppId).Count());
            TagsStats.Content = string.Format("{0} unique tags", Users.SelectMany(u => u.Games).SelectMany(g => g.Tags).Distinct().Count());
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

        private async void GetTags()
        {
            var allGameIds = Users.SelectMany(x => x.Games.Where(d => d.Tags.Count.Equals(0))).Select(f => f.AppId).Distinct().ToList();
//            var allGameIds = Users.SelectMany(x => x.Games.Where(d => d.Tags.Count.Equals(0) && d.ExistsInStore)).Select(f => f.AppId).Distinct().ToList();
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
            //var downloadtasksquery = from curr in allGames
            //                         select getTags("http://store.steampowered.com/app/" + curr).ContinueWith(t =>
            //{
            //    foreach(var user in users)
            //    {
            //        foreach(var game in user.games)
            //        {
            //            if (game.appId.Equals(curr))
            //                game.tags = t.Result;
            //        }
            //    }
            //});
            //await Task.WhenAll(downloadtasksquery);
            Progress.Visibility = System.Windows.Visibility.Hidden;
            Label.Content = "Processing Complete!";

            WriteData();
            
            ShowStats();
            Render.IsEnabled = true;
        }

        private void WriteData()
        {
            var jsonName = _groupUri.Segments[2].Replace("/", "") + ".json";
            File.WriteAllText(jsonName, JsonConvert.SerializeObject(Users));
            var newGames = Users.SelectMany(u => u.Games).GroupBy(g => g.AppId).Select(g => g.First()).ToList();
            var uniqueList = AllGames.Concat(newGames).GroupBy(item => item.AppId).Select(group => group.First()).ToList();
            File.WriteAllText("cachedGames.json", JsonConvert.SerializeObject(uniqueList));
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

        private void processGameComp_Click(object sender, RoutedEventArgs e)
        {

            var pageBuilder = new StringBuilder();

            pageBuilder.AppendLine("<!DOCTYPE html><html><head><title>games list yo!</title>");
            pageBuilder.AppendLine("<script src=\"https://ajax.googleapis.com/ajax/libs/jquery/1.8.2/jquery.min.js\"></script>");
            pageBuilder.AppendLine("<script src=\"https://luis-almeida.github.io/filtrify/js/filtrify.min.js\"></script>");
            pageBuilder.AppendLine("<link rel=\"stylesheet\" href=\"" + Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString().Replace("\\", "/") + "/css/filtrify.css\">");
            pageBuilder.AppendLine("<script> $(document).ready(function() {$.filtrify(\"container\", \"placeHolder\");})</script>");
            pageBuilder.AppendLine("<style>.background {background-color: #2F2727;}#container li {background-color: #9A9C98;margin:5px;padding:10px;-webkit-border-radius: 10px;-moz-border-radius: 10px;border-radius: 10px;-webkit-box-shadow: inset 0px 0px 10px 5px rgba(92,92,92,1);-moz-box-shadow: inset 0px 0px 10px 5px rgba(92,92,92,1);box-shadow: inset 0px 0px 10px 5px rgba(92,92,92,1);}#container li img{margin:5px;-webkit-border-radius: 5px;-moz-border-radius: 5px;border-radius: 5px;-webkit-box-shadow: 2px 2px 2px 1px rgba(92,92,92,1);-moz-box-shadow: 2px 2px 2px 1px rgba(92,92,92,1);box-shadow: 2px 2px 2px 1px rgba(92,92,92,1);}#placeHolder {-webkit-box-shadow: inset 0px 0px 10px 5px rgba(92,92,92,1);-moz-box-shadow: inset 0px 0px 10px 5px rgba(92,92,92,1);box-shadow: inset 0px 0px 10px 5px rgba(92,92,92,1);background-color: #9A9C98; padding:10px; -webkit-border-radius: 10px; -moz-border-radius: 10px; border-radius: 10px;}</style>  </head>  <body class=\"background\">    <div style=\"padding-left:50px; padding-top:50px; margin-right:auto; margin-left:auto; width:50%;\">");
            pageBuilder.AppendLine("<div id=\"placeHolder\"></div><ul id=\"container\">");

            var allGames = Users.SelectMany(u => u.Games).GroupBy(g => g.AppId).Select(g => g.First()).ToList();
            allGames.Sort(delegate (Game g1, Game g2)
            {
                var g1Count = Users.Where(u => u.Games.Any(g => g.AppId.Equals(g1.AppId))).Count();
                var g2Count = Users.Where(u => u.Games.Any(g => g.AppId.Equals(g2.AppId))).Count();

                return g2Count.CompareTo(g1Count);
            });

            foreach (var game in allGames)
            {
                var gameUsers = Users.Where(u => u.Games.Any(g => g.AppId.Equals(game.AppId))).ToList();

                pageBuilder.AppendLine("<li style=\"list-style:none\" data-tags=\"" + string.Join(", ", game.Tags) + "\" data-user=\"" + string.Join(", ", gameUsers.Select(u => u.Name).ToList()) + "\">");
                pageBuilder.AppendLine("<img src=\"" + game.Logo + "\" title=\"" + game.Name + "\"/>");
                foreach (var usr in gameUsers)
                {
                    pageBuilder.AppendLine("<img src=\"" + usr.Logo + "\" style=\"width:32px;height:32px\" title=\"" + usr.Name + "\"/>");
                }
                pageBuilder.AppendLine("</li>");
            } 
            pageBuilder.AppendLine("</ul>");
            pageBuilder.AppendLine("</div>");
            pageBuilder.AppendLine("</body>");
            pageBuilder.AppendLine("</html>");
            var htmlPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "\\gameComp.html";
            File.WriteAllText(htmlPath, pageBuilder.ToString());
            Process.Start(htmlPath);
        }

        private void GetData()
        {
            if (!string.IsNullOrEmpty(Settings.Default.groupUrl))
            {
                _groupUri = new Uri(Settings.Default.groupUrl);
                if (File.Exists(_groupUri.Segments[2].Replace("/", "") + ".json"))
                {
                    Users = JsonConvert.DeserializeObject<ObservableCollection<User>>(File.ReadAllText(_groupUri.Segments[2].Replace("/", "") + ".json"));
                    ShowStats();
                }
                else
                    Users = new ObservableCollection<User>();
            } else
                Users = new ObservableCollection<User>();

            AllGames = File.Exists("cachedGames.json") ? JsonConvert.DeserializeObject<List<Game>>(File.ReadAllText("cachedGames.json")) : new List<Game>();

            Button.IsEnabled = true;
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
    }   
}
