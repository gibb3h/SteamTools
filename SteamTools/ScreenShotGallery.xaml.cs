
using System.Collections.ObjectModel;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FluidKit.Controls;
using SteamTools.Classes;

namespace SteamTools
{
	/// <summary>
	/// 	Interaction logic for Window1.xaml
	/// </summary>
	public partial class ScreenShotGallery
	{
        private ObservableCollection<ScreenShot> _screenShots;

	    public ObservableCollection<ScreenShot> ScreenShots
	    {
	        get { return _screenShots ?? (_screenShots = new ObservableCollection<ScreenShot>()); }
	    }

		public ScreenShotGallery(int appId, List<string> users)
		{
            DataContext = this;
            _screenShots = new ObservableCollection<ScreenShot>(new DataAccess().GetGameScreenShots(appId, users));

			InitializeComponent();
		}	
	}
}