using System;

namespace SteamTools
{
    /// <summary>
    /// Interaction logic for changeLog.xaml
    /// </summary>
    public partial class ChangeLog
    {
        public ChangeLog()
        {
            InitializeComponent();
            browser.Source = new Uri("https://github.com/gibb3h/SteamTools/commits/master");
        }
    }
}
