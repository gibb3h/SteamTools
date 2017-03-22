using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamTools.Classes
{
    public  static class Logger
    {

        public static void log(string message)
        {
            TextWriter tw = new StreamWriter("error.log", true);
            tw.WriteLine(DateTime.Now+" - "+message);
            tw.Close(); 
        }

        public static void log(Exception e)
        {
            TextWriter tw = new StreamWriter("error.log", true);
            tw.Write(DateTime.Now + " - " + e.Message);
            tw.WriteLine(DateTime.Now + " - " + e.StackTrace);
            tw.Close(); 
        }
    }
}
