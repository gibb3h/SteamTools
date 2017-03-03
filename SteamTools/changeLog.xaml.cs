using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SteamTools
{
    /// <summary>
    /// Interaction logic for changeLog.xaml
    /// </summary>
    public partial class changeLog : Window
    {
        public changeLog()
        {
            InitializeComponent();
            browser.Source = new Uri("https://github.com/gibb3h/SteamTools/commits/master");
        }
    }
}
