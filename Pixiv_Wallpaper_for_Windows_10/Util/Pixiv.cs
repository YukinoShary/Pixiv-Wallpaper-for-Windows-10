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
using Windows.Security.Credentials;

namespace Pixiv_Wallpaper_for_Windows_10.Util
{
    public class Pixiv
    {
        public static PixivBaseAPI GlobalBaseAPI = new PixivBaseAPI();
        private static bool loginSuccess;
        private static PixivCS.Objects.ResponseUser currentUser;

        public Pixiv()
        {
            GlobalBaseAPI.ExperimentalConnection = true;   //直连模式打开
            loginSuccess = false;
            currentUser = null;
        }

        /// <summary>
        /// 提供内部pixiv方法登录
        /// </summary>
        /// <param name="actPsw"></param>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        private async Task<string> LoginMethod(string account, string password, string refreshToken)
        {
            PixivCS.Objects.AuthResult res = null;
            try
            {
                if (refreshToken != null && !refreshToken.Equals("ERROR"))
                {
                    res = await GlobalBaseAPI.AuthAsync(refreshToken);
                    currentUser = res.Response.User;
                    loginSuccess = true;
                }
                else
                {
                    res = await GlobalBaseAPI.AuthAsync(account, password);
                    currentUser = res.Response.User;
                    refreshToken = GlobalBaseAPI.RefreshToken;
                    loginSuccess = true;
                }
                return refreshToken;
            }
            catch (Exception)
            {
                try
                {
                    res = await GlobalBaseAPI.AuthAsync(account, password);
                    currentUser = res.Response.User;
                    refreshToken = GlobalBaseAPI.RefreshToken;
                    loginSuccess = true;
                    return refreshToken;
                }
                catch
                {
                    loginSuccess = false;
                }
                return "ERROR"; //凭证不能为空
            }
        }
        /// <summary>
        /// 获取"猜你喜欢"(PixivCS Api)
        /// </summary>
        /// <param name="imgId">获取与此插画类似的其他插画</param>
        /// <param name="nextUrl">从首次请求中获取到的带有参数设置的请求url,可用于下一次的带参请求</param>
        /// <param name="account"></param>
        /// /// <param name="password"></param>
        /// <param name="refreshToken"></param>
        /// <returns>元组数据，item1为插画信息队列，item2为下次请求的url参数</returns>
        public async Task<ValueTuple<ConcurrentQueue<ImageInfo>, string, string>> getRecommenlist(string imgId, string nextUrl, string account, string password, string refreshToken)
        {
            ConcurrentQueue<ImageInfo> queue = new ConcurrentQueue<ImageInfo>();
            string newToken = refreshToken;
            if (GlobalBaseAPI.AccessToken == null  || loginSuccess == false)
            {
                newToken = await LoginMethod(account, password, refreshToken);
            }
            if(imgId == null)
            {
                PixivCS.Objects.IllustRecommended recommendres = null;
                try
                {
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
                    nextUrl = recommendres.NextUrl?.ToString() ?? "";
                    foreach (PixivCS.Objects.UserPreviewIllust ill in recommendres.Illusts)
                    {
                        //当插画数大于1时插画URL会改变，以后再解决吧
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
                    return new ValueTuple<ConcurrentQueue<ImageInfo>, string, string>(null, "begin", newToken);
                }
            }
            else
            {
                PixivCS.Objects.UserIllusts related = null;
                try
                {
                    related = await new PixivAppAPI(GlobalBaseAPI).GetIllustRelatedAsync(imgId);
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
                    return new ValueTuple<ConcurrentQueue<ImageInfo>, string, string>(null, "begin", newToken);
                }
            }      
            return new ValueTuple<ConcurrentQueue<ImageInfo>, string, string>(queue, nextUrl, newToken);
        }

        /// <summary>
        /// 获取已关注画师最近更新的插画队列(PixivCS)
        /// </summary>
        /// <param name="nextUrl">从首次请求中获取到的带有参数设置的请求url,可用于下一次的带参请求</param>
        /// <param name="actPsw"></param>
        /// <param name="refreshToken">元组数据，item1为插画信息队列，item2为下次请求的url参数</param>
        /// <returns></returns>
        public async Task<ValueTuple<ConcurrentQueue<ImageInfo>, string, string>> getIllustFollowList(string nextUrl, string account, string password, string refreshToken)
        {
            ConcurrentQueue<ImageInfo> queue = new ConcurrentQueue<ImageInfo>();
            PixivCS.Objects.UserIllusts followIllust = null;
            string newToken = refreshToken;
            if (GlobalBaseAPI.AccessToken == null || loginSuccess == false)
            {
                newToken = await LoginMethod(account, password, refreshToken);
            }
            try
            {
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
                    //当插画数大于1时插画URL会改变，以后再解决吧
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
            catch(Exception)
            {
                return new ValueTuple<ConcurrentQueue<ImageInfo>, string, string>(null, "begin", newToken);
            }
            return new ValueTuple<ConcurrentQueue<ImageInfo>, string, string>(queue, nextUrl, newToken);
        }

        /// <summary>
        /// 获取书签插画队列(PixivCS)
        /// </summary>
        /// <param name="nextUrl">从首次请求中获取到的带有参数设置的请求url,可用于下一次的带参请求</param>
        /// <param name="actPsw"></param>
        /// <param name="refreshToken"></param>
        /// <returns>元组数据，item1为插画信息队列，item2为下次请求的url参数</returns>
        public async Task<ValueTuple<ConcurrentQueue<ImageInfo>, string, string>> getBookmarkIllustList(string nextUrl, string account, string password, string refreshToken)
        {
            ConcurrentQueue<ImageInfo> queue = new ConcurrentQueue<ImageInfo>();
            PixivCS.Objects.UserIllusts bookmarkIllust = null;
            string newToken = refreshToken;
            if (GlobalBaseAPI.AccessToken == null || loginSuccess == false)
            {
                newToken = await LoginMethod(account, password, refreshToken);
            }
            try
            {
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
                        imginfo.title = ill.Title;
                        imginfo.userName = ill.User.Name;
                        imginfo.imgId = ill.Id.ToString();
                        imginfo.height = (int)ill.Height;
                        imginfo.width = (int)ill.Width;
                        imginfo.format = imginfo.imgUrl.Split('.').Last();
                        queue.Enqueue(imginfo);
                    }
                }
            }
            catch(Exception)
            {
                return new ValueTuple<ConcurrentQueue<ImageInfo>, string, string>(null, "begin", newToken);
            }
            return new ValueTuple<ConcurrentQueue<ImageInfo>, string, string>(queue, nextUrl, newToken);
        }
    }
}
