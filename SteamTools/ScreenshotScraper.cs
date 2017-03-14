using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;

namespace SteamTools
{
    internal class ScreenshotScraper
    {

        private readonly HtmlParser _parser = new HtmlParser();
        private readonly HttpClient _http = new HttpClient();

        public async Task<List<ScreenShot>> GetScreenShots(string userProfile, List<ScreenShot> cached)
        {

            var results = new List<ScreenShot>();
            var request = await _http.GetAsync(userProfile + Consts.UrlScreenshot);
            var response = await request.Content.ReadAsStreamAsync();
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
                        .Select(img => GetScreenShot(img, user).ContinueWith(t => results.AddRange(t.Result)));
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
                                               GetScreenShot(img, user).ContinueWith(t => results.AddRange(t.Result)));
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

        private async Task<List<ScreenShot>> GetScreenShot(IElement img, string user)
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

                    var gameName = imgDoc.QuerySelectorAll(Consts.ElemGame).Any()
                                   ? imgDoc.QuerySelectorAll(Consts.ElemGame).First().TextContent
                                   : imgDoc.QuerySelectorAll(Consts.ElemGameNonSteam).First().TextContent;
                    var appid = 0;
                    if (imgDoc.QuerySelectorAll(Consts.ElemGame).Any())
                    {
                        appid =
                            int.Parse(
                                new Uri(imgDoc.QuerySelectorAll(Consts.ElemGame).First().GetAttribute("href")).Segments[2].Replace("/", ""));
                    }
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
    }
}
