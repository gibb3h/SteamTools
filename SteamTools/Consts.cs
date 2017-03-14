using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SteamTools
{
    public static class Consts
    {
        public static string UrlScreenshot
        {
            get { return "/screenshots/?appid=0&sort=newestfirst&browsefilter=myfiles&view=grid"; }
        }

        public static string ElemPage
        {
            get { return ".pagingPageLink"; }
        }

        public static string ElemUser
        {
            get { return "#HeaderUserInfoName a"; }
        }

        public static string ElemImgFloat
        {
            get { return ".floatHelp"; }
        }

        public static string ElemImg
        {
            get { return ".imgWallItem"; }
        }

        public static string ElemImgId
        {
            get { return ".imgWallHover"; }
        }

        public static string ElemDesc
        {
            get { return ".ellipsis"; }
        }

        public static string ElemGame
        {
            get { return ".screenshotAppName a"; }
        }

        public static string ElemGameNonSteam
        {
            get { return ".breadcrumbs a"; }
        }

        public static string ApiUrl
        {
            get { return "http://api.steampowered.com/ISteamApps/GetAppList/v0002/"; }
        }

        public static string MatureCookies
        {
            get { return "birthtime=504921601; lastagecheckage=1-January-1986; mature_content=1"; }
        }

        public static Regex UrlMatch
        {
            get { return new Regex(@"^((http:\/\/steamcommunity\.com\/groups\/)[a-zA-Z]+(\/)[a-zA-Z]+)$"); }
        }

        public static string ScreenShotDirectory
        {
            get
            {
                return Path.Combine(Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location), "ScreenShots");
            }
        }
    }
}
