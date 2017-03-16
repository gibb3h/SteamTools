using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SteamTools.Classes
{
    class Renderer
    {

        public static void Render(List<User> users, List<Game> allGames)
        {
            var pageBuilder = new StringBuilder();

            pageBuilder.AppendLine("<!DOCTYPE html><html><head><title>games list yo!</title>");
            pageBuilder.AppendLine("<script src=\"https://ajax.googleapis.com/ajax/libs/jquery/1.8.2/jquery.min.js\"></script>");
            pageBuilder.AppendLine("<script src=\"https://luis-almeida.github.io/filtrify/js/filtrify.min.js\"></script>");
            pageBuilder.AppendLine("<link rel=\"stylesheet\" href=\"" + Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString().Replace("\\", "/") + "/css/filtrify.css\">");
            pageBuilder.AppendLine("<script> $(document).ready(function() {$.filtrify(\"container\", \"placeHolder\");})</script>");
            pageBuilder.AppendLine("<style>.background {background-color: #2F2727;}#container li {background-color: #9A9C98;margin:5px;padding:10px;-webkit-border-radius: 10px;-moz-border-radius: 10px;border-radius: 10px;-webkit-box-shadow: inset 0px 0px 10px 5px rgba(92,92,92,1);-moz-box-shadow: inset 0px 0px 10px 5px rgba(92,92,92,1);box-shadow: inset 0px 0px 10px 5px rgba(92,92,92,1);}#container li img{margin:5px;-webkit-border-radius: 5px;-moz-border-radius: 5px;border-radius: 5px;-webkit-box-shadow: 2px 2px 2px 1px rgba(92,92,92,1);-moz-box-shadow: 2px 2px 2px 1px rgba(92,92,92,1);box-shadow: 2px 2px 2px 1px rgba(92,92,92,1);}#placeHolder {-webkit-box-shadow: inset 0px 0px 10px 5px rgba(92,92,92,1);-moz-box-shadow: inset 0px 0px 10px 5px rgba(92,92,92,1);box-shadow: inset 0px 0px 10px 5px rgba(92,92,92,1);background-color: #9A9C98; padding:10px; -webkit-border-radius: 10px; -moz-border-radius: 10px; border-radius: 10px;}</style>  </head>  <body class=\"background\">    <div style=\"padding-left:50px; padding-top:50px; margin-right:auto; margin-left:auto; width:50%;\">");
            pageBuilder.AppendLine("<div id=\"placeHolder\"></div><ul id=\"container\">");

            var allIds = users.SelectMany(u => u.Games).GroupBy(g => g).Select(g => g.First()).ToList();
            var userGames = allGames.Where(g => allIds.Contains(g.AppId)).ToList();

            userGames.Sort(delegate(Game g1, Game g2)
            {
                var g1Count = users.Count(u => u.Games.Contains(g1.AppId));
                var g2Count = users.Count(u => u.Games.Contains(g2.AppId));

                return g2Count.CompareTo(g1Count);
            });

            foreach (var id in userGames)
            {
                var gameUsers = users.Where(u => u.Games.Any(g => g.Equals(id.AppId))).ToList();
                if (allGames.Any(g => g.AppId.Equals(id.AppId)))
                {
                    var gameObj = allGames.First(g => g.AppId.Equals(id.AppId));
                    pageBuilder.AppendLine("<li style=\"list-style:none\" data-tags=\"" +
                                           string.Join(", ", gameObj.Tags) + "\" data-user=\"" +
                                           string.Join(", ", gameUsers.Select(u => u.Name).ToList()) + "\">");
                    pageBuilder.AppendLine("<img src=\"" + gameObj.Logo + "\" title=\"" + gameObj.Name + "\"/>");
                    foreach (var usr in gameUsers)
                    {
                        pageBuilder.AppendLine("<img src=\"" + usr.Logo + "\" style=\"width:32px;height:32px\" title=\"" +
                                               usr.Name + "\"/>");
                    }
                    pageBuilder.AppendLine("</li>");
                }
            }
            pageBuilder.AppendLine("</ul>");
            pageBuilder.AppendLine("</div>");
            pageBuilder.AppendLine("</body>");
            pageBuilder.AppendLine("</html>");
            var htmlPath = Directory.GetParent(Assembly.GetExecutingAssembly().Location) + "\\gameComp.html";
            File.WriteAllText(htmlPath, pageBuilder.ToString());
            Process.Start(htmlPath);
        }
    }
}
