using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using PixivCS;
using System.Collections.Concurrent;
using System.Web;
using System.IO;
using Pixiv_Wallpaper_for_Windows_10.Model;

namespace Pixiv_Wallpaper_for_Windows_10.Util
{
    public class Pixiv
    {
        private readonly string RECOMM_URL = "https://www.pixiv.net/rpc/recommender.php?type=illust&sample_illusts=auto&num_recommendations=1000&page=discovery&mode=all";
        private readonly string RECOMMSAM_URL = "https://www.pixiv.net/rpc/recommender.php?type=illust&sample_illusts=";
        private readonly string DETA_URL = "https://api.imjad.cn/pixiv/v1/?type=illust&id=";
        private readonly string RALL_URL = "https://www.pixiv.net/ranking.php?mode=daily&content=illust&p=1&format=json";

        //private string nexturl = "begin";
        public static PixivBaseAPI GlobalBaseAPI;
        //public string cookie { get; set; }

        public Pixiv()
        {
            GlobalBaseAPI = new PixivBaseAPI();  
            GlobalBaseAPI.ExperimentalConnection = true;   //直连模式打开
        }


        /// <summary>
        /// 获取TOP 50推荐列表
        /// </summary>
        /// <returns></returns>
        public async Task<ConcurrentQueue<string>> getRallist()
        {
            string rall;
            ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
            HttpUtil top50 = new HttpUtil(RALL_URL, HttpUtil.Contype.JSON);

            rall = await top50.NewGetDataAsync();

            if (!rall.Equals("ERROR"))
            {
                dynamic o = JObject.Parse(rall);
                JArray arr = o.contents;

                foreach (JToken j in arr)
                {
                    queue.Enqueue(j["illust_id"].ToString());
                }
            }
            return queue;
        }


        /// <summary>
        /// 获取"猜你喜欢"推荐列表(Web模拟)
        /// </summary>
        /// <returns>插画id队列</returns>

        public async Task<ConcurrentQueue<string>> getRecommenlist(string imgId)
        {
            string like, finalUrl;
            HttpUtil recomm;
            ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
            if(imgId == null)
            {
                finalUrl = RECOMM_URL;
            }
            else
            {
                finalUrl = RECOMMSAM_URL + imgId + "&num_recommendations=52";//根据sample插画进行关联推荐
            }
            recomm = new HttpUtil(finalUrl, HttpUtil.Contype.JSON);
            //recomm.cookie = cookie;
            recomm.referrer = "https://www.pixiv.net/discovery";

            like = await recomm.NewGetDataAsync();

            if (!like.Equals("ERROR"))
            {
                dynamic o = JObject.Parse(like);
                JArray arr = o.recommendations;
                foreach (JToken j in arr)
                {
                    queue.Enqueue(j.ToString());
                }
            }
    
            return queue;
        }

        /// <summary>
        /// 获取"猜你喜欢"(PixivCS Api)
        /// </summary>
        /// <param name="imgId">获取与此插画类似的其他插画</param>
        /// <param name="nextUrl">从首次请求中获取到的带有参数设置的请求url,可用于下一次的带参请求</param>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns>元组数据，item1为插画信息队列，item2为下次请求的url参数</returns>
        public async Task<Tuple<ConcurrentQueue<ImageInfo>, string>> getRecommenlist(string imgId, string nextUrl, string account = null, string password = null)
        {
            ConcurrentQueue<ImageInfo> queue = new ConcurrentQueue<ImageInfo>();
            if (GlobalBaseAPI.AccessToken == null)
            {
                try
                {
                    PixivCS.Objects.AuthResult res = null;
                    res = await GlobalBaseAPI.AuthAsync(account, password);
                }
                catch
                {
                    return null;
                }
            }
            if(imgId == null)
            {
                PixivCS.Objects.IllustRecommended recommendres = null;
                //是否使用nexturl更新list
                if ("begin".Equals(nextUrl))
                {
                    recommendres = await new PixivAppAPI(GlobalBaseAPI).GetIllustRecommendedAsync();
                }
                else
                {
                    Uri next = new Uri(nextUrl);
                    string getparam(string param) => HttpUtility.ParseQueryString(next.Query).Get(param);
                    recommendres = await new PixivAppAPI(GlobalBaseAPI).GetIllustRecommendedAsync
                        (ContentType: getparam("content_type"),
                         IncludeRankingLabel: bool.Parse(getparam("include_ranking_label")),
                         Filter: getparam("filter"),
                         MinBookmarkIDForRecentIllust: getparam("min_bookmark_id_for_recent_illust"),
                         MaxBookmarkIDForRecommended: getparam("max_bookmark_id_for_recommend"),
                         Offset: getparam("offset"),
                         IncludeRankingIllusts: bool.Parse(getparam("include_ranking_illusts")),
                         IncludePrivacyPolicy: getparam("include_privacy_policy"));
                }
                try
                {
                    nextUrl = recommendres.NextUrl?.ToString() ?? "";
                    foreach (PixivCS.Objects.UserPreviewIllust ill in recommendres.Illusts)
                    {
                        if (ill.PageCount == 1)
                        {
                            ImageInfo imginfo = new ImageInfo();
                            imginfo.imgUrl = ill.MetaSinglePage.OriginalImageUrl.ToString();
                            imginfo.viewCount = (int)ill.TotalView;
                            imginfo.isR18 = false;
                            imginfo.userId = ill.User.Id.ToString();
                            imginfo.userName = ill.User.Name;
                            imginfo.imgId = ill.Id.ToString();
                            imginfo.title = ill.Title;
                            imginfo.height = (int)ill.Height;
                            imginfo.width = (int)ill.Width;
                            imginfo.format = imginfo.imgUrl.Split('.').Last();
                            queue.Enqueue(imginfo);
                        }
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }
            else
            {
                PixivCS.Objects.UserIllusts related = null;
                related = await new PixivAppAPI(GlobalBaseAPI).GetIllustRelatedAsync(imgId);
                try
                {
                    foreach (PixivCS.Objects.UserPreviewIllust ill in related.Illusts)
                    {
                        if (ill.PageCount == 1)
                        {
                            ImageInfo imginfo = new ImageInfo();
                            imginfo.imgUrl = ill.MetaSinglePage.OriginalImageUrl.ToString();
                            imginfo.viewCount = (int)ill.TotalView;
                            imginfo.isR18 = false;
                            imginfo.userId = ill.User.Id.ToString();
                            imginfo.userName = ill.User.Name;
                            imginfo.imgId = ill.Id.ToString();
                            imginfo.title = ill.Title;
                            imginfo.height = (int)ill.Height;
                            imginfo.width = (int)ill.Width;
                            imginfo.format = imginfo.imgUrl.Split('.').Last();
                            queue.Enqueue(imginfo);
                        }
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }      
            return new Tuple<ConcurrentQueue<ImageInfo>, string>(queue, nextUrl);
        }

        /// <summary>
        /// 获取已关注画师最近更新的插画队列(PixivCS)
        /// </summary>
        /// <param name="nextUrl">从首次请求中获取到的带有参数设置的请求url,可用于下一次的带参请求</param>
        /// <param name="account"></param>
        /// <param name="password">元组数据，item1为插画信息队列，item2为下次请求的url参数</param>
        /// <returns></returns>
        public async Task<Tuple<ConcurrentQueue<ImageInfo>, string>> getIllustFollowList(string nextUrl, string account = null, string password = null)
        {
            ConcurrentQueue<ImageInfo> queue = new ConcurrentQueue<ImageInfo>();
            PixivCS.Objects.UserIllusts followIllust = null;
            if (GlobalBaseAPI.AccessToken == null)
            {
                try
                {
                    PixivCS.Objects.AuthResult res = null;
                    res = await GlobalBaseAPI.AuthAsync(account, password);
                }
                catch(Exception e)
                {
                    //处理错误
                    Console.WriteLine(e.Message);
                    return null;
                }
            }
            if (nextUrl == "begin")
            {
                followIllust = await new PixivAppAPI(GlobalBaseAPI).GetIllustFollowAsync();
            }
            else
            {
                Uri next = new Uri(nextUrl);
                string getparam(string param) => HttpUtility.ParseQueryString(next.Query).Get(param);
                followIllust = await new PixivCS
                    .PixivAppAPI(GlobalBaseAPI)
                    .GetIllustFollowAsync(getparam("restrict"), getparam("offset"));
            }
            nextUrl = followIllust.NextUrl?.ToString() ?? "";
            foreach (PixivCS.Objects.UserPreviewIllust ill in followIllust.Illusts)
            {
                if (ill.PageCount == 1)
                {
                    ImageInfo imginfo = new ImageInfo();
                    imginfo.imgUrl = ill.MetaSinglePage.OriginalImageUrl.ToString();
                    imginfo.viewCount = (int)ill.TotalView;
                    imginfo.isR18 = false;
                    imginfo.userId = ill.User.Id.ToString();
                    imginfo.userName = ill.User.Name;
                    imginfo.imgId = ill.Id.ToString();
                    imginfo.title = ill.Title;
                    imginfo.height = (int)ill.Height;
                    imginfo.width = (int)ill.Width;
                    imginfo.format = imginfo.imgUrl.Split('.').Last();
                    queue.Enqueue(imginfo);
                }
            }
            return new Tuple<ConcurrentQueue<ImageInfo>, string>(queue, nextUrl);
        }

        /// <summary>
        /// 获取书签插画队列(PixivCS)
        /// </summary>
        /// <param name="nextUrl">从首次请求中获取到的带有参数设置的请求url,可用于下一次的带参请求</param>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns>元组数据，item1为插画信息队列，item2为下次请求的url参数</returns>
        public async Task<Tuple<ConcurrentQueue<ImageInfo>, string>> getBookmarkIllustList(string nextUrl, string account = null, string password = null)
        {
            ConcurrentQueue<ImageInfo> queue = new ConcurrentQueue<ImageInfo>();
            PixivCS.Objects.UserIllusts bookmarkIllust = null;
            PixivCS.Objects.ResponseUser currentUser = null;
            if (GlobalBaseAPI.AccessToken == null)
            {
                try
                {
                    PixivCS.Objects.AuthResult res = null;
                    res = await GlobalBaseAPI.AuthAsync(account, password);
                    currentUser = res.Response.User;
                }
                catch
                {
                    return null;
                }
            }
            if (nextUrl == "begin")
            {
                bookmarkIllust = await new PixivAppAPI(GlobalBaseAPI).GetUserBookmarksIllustAsync(currentUser.Id);
            }
            else
            {
                Uri next = new Uri(nextUrl);
                string getparam(string param) => HttpUtility.ParseQueryString(next.Query).Get(param);
                bookmarkIllust = await new PixivCS
                    .PixivAppAPI(GlobalBaseAPI)
                    .GetUserBookmarksIllustAsync(currentUser.Id, getparam("restrict"),
                    getparam("filter"), getparam("max_bookmark_id"));
            }
            nextUrl = bookmarkIllust.NextUrl?.ToString() ?? "";
            foreach (PixivCS.Objects.UserPreviewIllust ill in bookmarkIllust.Illusts)
            {
                if (ill.PageCount == 1)
                {
                    ImageInfo imginfo = new ImageInfo();
                    imginfo.imgUrl = ill.MetaSinglePage.OriginalImageUrl.ToString();
                    imginfo.viewCount = (int)ill.TotalView;
                    imginfo.isR18 = false;
                    imginfo.userId = ill.User.Id.ToString();
                    imginfo.userName = ill.User.Name;
                    imginfo.imgId = ill.Id.ToString();
                    imginfo.height = (int)ill.Height;
                    imginfo.width = (int)ill.Width;

                    queue.Enqueue(imginfo);
                }
            }
            return new Tuple<ConcurrentQueue<ImageInfo>, string>(queue, nextUrl);
        }

        /// <summary>
        /// 查询插画信息
        /// </summary>
        /// <param name="imgId">要查找的作品ID</param>
        /// <returns></returns>
        public async Task<ImageInfo> getImageInfo(string imgId)
        {
            ImageInfo imginfo = null;
            HttpUtil info = new HttpUtil(DETA_URL + imgId, HttpUtil.Contype.JSON);
            string data = await info.NewGetDataAsync();
            if (!data.Equals("ERROR"))
            {
                imginfo = new ImageInfo();

                dynamic o = JObject.Parse(data);
                dynamic ill = o.response;
                imginfo.viewCount = (int)ill[0]["stats"]["views_count"];
                imginfo.imgUrl = ill[0]["image_urls"]["large"].ToString();
                switch (ill[0]["age_limit"].ToString())
                {
                    case "all-age":
                        imginfo.isR18 = false;
                        break;
                    case "r18":
                        imginfo.isR18 = true;
                        break;
                    case "r18-g":
                        imginfo.isR18 = true;
                        break;
                }
                imginfo.userId = ill[0]["user"]["id"].ToString();
                imginfo.userName = ill[0]["user"]["name"].ToString();
                imginfo.imgId = ill[0]["id"].ToString() ;
                imginfo.title = ill[0]["title"].ToString();
                imginfo.height = (int)ill[0]["height"];
                imginfo.width = (int)ill[0]["width"];
                imginfo.format = imginfo.imgUrl.Split('.').Last();
            }
            return imginfo;
        }
    }
}
