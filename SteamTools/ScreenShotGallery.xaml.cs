
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Net.Http;
using System.Windows;
using SteamTools.Classes;

namespace SteamTools
{
    /// <summary>
    /// 	Interaction logic for Window1.xaml
    /// </summary>
    public partial class ScreenShotGallery
    {
        private ObservableCollection<ScreenShotGal> _screenShots;

        public ObservableCollection<ScreenShotGal> ScreenShots
        {
            get { return _screenShots ?? (_screenShots = new ObservableCollection<ScreenShotGal>()); }
        }

        public ScreenShotGallery(int appId, List<User> users)
        {
            DataContext = this;
            _screenShots = new ObservableCollection<ScreenShotGal>();
            var da = new DataAccess();

            foreach (var usr in users)
            {
                var tmp = da.GetGameScreenShots(appId, usr.Name);
                tmp.Reverse();
                foreach (var screenShot in tmp)
                {
                    _screenShots.Add(new ScreenShotGal()
                        {
                            AppId = screenShot.AppId,
                            Description = screenShot.Description,
                            Filename = screenShot.Filename,
                            GameName = screenShot.GameName,
                            Link = screenShot.Link,
                            Url = screenShot.Url,
                            User = usr
                        });
                }
            }

            InitializeComponent();
        }

        private async void DownloadScreenShots_OnClick(object sender, RoutedEventArgs e)
        {
            progressBar.Visibility = Visibility.Visible;
            progressBar.Maximum = _screenShots.Count;
             var da = new DataAccess();
            using (var http = new HttpClient())
            {
                foreach (var s in _screenShots)
                {
                    var request = await http.GetAsync(s.Url);
                    var response = await request.Content.ReadAsStreamAsync();
                    request.EnsureSuccessStatusCode();
                    await da.DownloadScreenShot(response, s.GameName, s.Filename);
                    progressBar.Value++;
                }
            }

            progressBar.Visibility = Visibility.Hidden;
            progressBar.Value = 0;
        }
    }

    public class ScreenShotGal
    {
        public string Url { get; set; }
        public string Filename { get; set; }
        public string Link { get; set; }
        public User User { get; set; }
        public string Description { get; set; }
        public int AppId { get; set; }
        public string GameName { get; set; }
    }
}