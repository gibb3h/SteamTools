using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SteamTools.Classes;

namespace SteamTools
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            base.OnStartup(e);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            Logger.log(ex);
            Current.Shutdown();
        }

        void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.log(e.Exception);
            e.Handled = false;
            Current.Shutdown();
        }
    }
}
