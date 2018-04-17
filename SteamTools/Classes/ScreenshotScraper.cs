using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using FuzzyString;
using Newtonsoft.Json;
using SteamTools.Properties;

namespace SteamTools.Classes
{
    internal class ScreenshotScraper
    {

        private readonly HtmlParser _parser = new HtmlParser();
        private readonly HttpClient _http = new HttpClient();
        private readonly DataAccess da = new DataAccess();
        public async Task<List<ScreenShot>> GetScreenShots(string userProfile, List<ScreenShot> cached, string steamId)
        {

            var results = new List<ScreenShot>();
            var request = await _http.GetAsync(userProfile + Consts.UrlScreenshot);
            var response = await request.Content.ReadAsStreamAsync();
            Console.WriteLine(userProfile + Consts.UrlScreenshot);
            request.EnsureSuccessStatusCode();
            var document = _parser.Parse(response);
            var count = document.QuerySelectorAll("#image_wall > div:nth-child(3) > div:nth-child(1)").Any()
                            ? int.Parse(
                                new Regex(@"(\d+)$").Match(
                                    document.QuerySelector("#image_wall > div:nth-child(3) > div:nth-child(1)")
                                            .TextContent).Value)
                            : 0;

            if (count <= cached.Count)
                return cached;

            var user = document.QuerySelector(Consts.ElemUser).TextContent;
            var downloadTasksQuery =
                document.QuerySelectorAll(Consts.ElemImgFloat)
                        .ToList().Where(g => !cached.Any(c => c.Filename.Equals(GetFileName(g))))
                        .Select(img => GetScreenShot(img, user, steamId).ContinueWith(t => results.AddRange(t.Result)));
            await Task.WhenAll(downloadTasksQuery);
            if (document.QuerySelectorAll(Consts.ElemPage).Any())
            {
                var lastPage = int.Parse(document.QuerySelectorAll(Consts.ElemPage).Last().TextContent);
                for (var i = 2; i <= lastPage; i++)
                {
                    var pageUrl = string.Format("{0}{1}&p={2}", userProfile, Consts.UrlScreenshot, i);
                    using (var membersRequest = await _http.GetAsync(pageUrl))
                    using (var membersResponse = await membersRequest.Content.ReadAsStreamAsync())
                    using (var membersDocument = _parser.Parse(membersResponse))
                    {
                        membersRequest.EnsureSuccessStatusCode();
                        var query2 =
                            membersDocument.QuerySelectorAll(Consts.ElemImgFloat)
                                           .ToList().Where(g => !cached.Any(c => c.Filename.Equals(GetFileName(g))))
                                           .Select(
                                               img =>
                                               GetScreenShot(img, user, steamId).ContinueWith(t => results.AddRange(t.Result)));
                        await Task.WhenAll(query2);
                    }
                }
            }
            cached.AddRange(results);

            return cached;
        }

        private string GetFileName(IElement img)
        {
            return img.QuerySelector(Consts.ElemImg).QuerySelector(Consts.ElemImgId).GetAttribute("id").Replace("imgWallHover", "") +
                   ".jpg";
        }

        private async Task<List<ScreenShot>> GetScreenShot(IElement img, string user,string steamId)
        {
            var shots = new List<ScreenShot>();
            try
            {
                var imgPage = img.QuerySelector("a").GetAttribute("href");

                using (var imgRequest = await _http.GetAsync(imgPage))
                using (var imgResponse = await imgRequest.Content.ReadAsStreamAsync())
                using (var imgDoc = _parser.Parse(imgResponse))
                {
                    var imgUrl = imgDoc.QuerySelector(".actualmediactn a").GetAttribute("href");

                    var desc = img.QuerySelector(Consts.ElemImg).QuerySelectorAll(Consts.ElemDesc).Any()
                        ? img.QuerySelector(Consts.ElemImg).QuerySelectorAll(Consts.ElemDesc).First().TextContent
                        : "";

                    var nonSteam = !imgDoc.QuerySelectorAll(Consts.ElemGame).Any();        
                    var gameName = !nonSteam
                        ? imgDoc.QuerySelectorAll(Consts.ElemGame).First().TextContent
                        : imgDoc.QuerySelectorAll(Consts.ElemGameNonSteam).Last().TextContent;

                    var appid = 0;

                    if (imgDoc.QuerySelectorAll(Consts.ElemGame).Any())
                    {
                        appid =
                            int.Parse(
                                new Uri(imgDoc.QuerySelectorAll(Consts.ElemGame).First().GetAttribute("href"))
                                    .Segments[2].Replace("/", ""));
                    }

                    if (nonSteam)
                        appid = GetNonSteamGame(gameName, steamId);
                    var ss = new ScreenShot
                    {
                        AppId = appid,
                        Description = desc,
                        Filename = GetFileName(img),
                        Link = imgPage,
                        Url = imgUrl,
                        User = user,
                        GameName = gameName
                    };
                    shots.Add(ss);
                }
            }
            catch (Exception e)
            {
                var dbg = "";
            }
            return shots;
        }

        private int GetNonSteamGame(string gameName, string steamId)
        {
            int fakeAppId;
            var allGames = da.GetCachedGames();
            var usrs = da.GetCachedUsers(Settings.Default.groupUrl);
            var usr = usrs.FirstOrDefault(u => u.SteamId.Equals(steamId));

            if (allGames.Any(g => g.Name.Equals(gameName)))
            {
                var existingId = allGames.FirstOrDefault(g => g.Name.Equals(gameName))?.AppId ?? 0;
                if (usr?.Games?.Contains(existingId) != true)
                {
                    usr?.Games?.Add(existingId);
                    da.WriteCachedUsers(Settings.Default.groupUrl, usrs);
                }

                return existingId;
            }

            if (allGames.Select(g => g.AppId).Min() > -1)
                fakeAppId = - 1;
            else           
                fakeAppId = allGames.Select(g => g.AppId).Min() - 1;

            allGames.Add(new Game
            {
                AppId = fakeAppId,
                ExistsInStore = false,
                Logo = FindGameLogoAsync(gameName).Result,
                Name = gameName
            });
            da.WriteCachedGames(allGames);
            if (usr?.Games?.Contains(fakeAppId) != true)
            {
                usr?.Games?.Add(fakeAppId);
                da.WriteCachedUsers(Settings.Default.groupUrl, usrs);
            }
            return fakeAppId;
        }

        private async Task<string> FindGameLogoAsync(string name)
        {

            var nme = da.SteamGrids.FirstOrDefault(a => a.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(nme))
                nme = da.SteamGrids.FirstOrDefault(a => a.ApproximatelyEquals(name,
                    new List<FuzzyStringComparisonOptions>
                    {
                        FuzzyStringComparisonOptions.UseOverlapCoefficient,
                        FuzzyStringComparisonOptions.UseLongestCommonSubsequence,
                        FuzzyStringComparisonOptions.UseLongestCommonSubstring
                    }, FuzzyStringComparisonTolerance.Strong));
            if (string.IsNullOrEmpty(nme))
                return "";
            try
            {
                var steamGridApiUrl = $"http://www.steamgriddb.com/api/grids?game={nme}";
                using (var request =
                    await _http.GetAsync(steamGridApiUrl).ConfigureAwait(continueOnCapturedContext: false))
                using (var response = await request.Content.ReadAsStreamAsync())
                {
                    request.EnsureSuccessStatusCode();
                    using (var sr = new StreamReader(response))
                        return JsonConvert.DeserializeObject<dynamic>(sr.ReadToEnd()).data[0].grid_url;
                }
            }
            catch (Exception ex)
            {
                Logger.log(ex);
                //   MessageBox.Show(ex.Message);
            }

            return "";

        }
    }
}
