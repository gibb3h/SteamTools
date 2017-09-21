
using System.Collections.ObjectModel;
using System.Collections.Generic;
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
                var tmp = da.GetGameScreenShots(appId, usr.SteamId);
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