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
using Pixiv_Wallpaper_WinUI.Model;
using Windows.Security.Credentials;

namespace Pixiv_Wallpaper_WinUI.Util
{
    public class Pixiv
    {
        private PixivBaseAPI baseAPI;
        private PixivCS.Objects.ResponseUser currentUser;

        public Pixiv(PixivBaseAPI baseAPI, PixivCS.Objects.ResponseUser currentUser)
        {
            this.baseAPI = baseAPI;
            this.currentUser = currentUser;
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
        public async Task<ValueTuple<ConcurrentQueue<ImageInfo>, string>> getRecommenlist(string imgId, string nextUrl)
        {
            ConcurrentQueue<ImageInfo> queue = new ConcurrentQueue<ImageInfo>();
            if (imgId == null)
            {
                PixivCS.Objects.IllustRecommended recommendres = null;
                try
                {
                    //是否使用nexturl更新list
                    if ("begin".Equals(nextUrl))
                    {
                        recommendres = await new PixivAppAPI(baseAPI).GetIllustRecommendedAsync();
                    }
                    else
                    {
                        Uri next = new Uri(nextUrl);
                        string getparam(string param) => HttpUtility.ParseQueryString(next.Query).Get(param);
                        recommendres = await new PixivAppAPI(baseAPI).GetIllustRecommendedAsync
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
                    return new ValueTuple<ConcurrentQueue<ImageInfo>, string>(null, "begin");
                }
            }
            else
            {
                PixivCS.Objects.UserIllusts related = null;
                try
                {
                    related = await new PixivAppAPI(baseAPI).GetIllustRelatedAsync(imgId);
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
                    return new ValueTuple<ConcurrentQueue<ImageInfo>, string>(null, "begin");
                }
            }
            return new ValueTuple<ConcurrentQueue<ImageInfo>, string>(queue, nextUrl);
        }

        /// <summary>
        /// 获取已关注画师最近更新的插画队列(PixivCS)
        /// </summary>
        /// <param name="nextUrl">从首次请求中获取到的带有参数设置的请求url,可用于下一次的带参请求</param>
        /// <param name="actPsw"></param>
        /// <param name="refreshToken">元组数据，item1为插画信息队列，item2为下次请求的url参数</param>
        /// <returns></returns>
        public async Task<ValueTuple<ConcurrentQueue<ImageInfo>, string>> getIllustFollowList(string nextUrl)
        {
            ConcurrentQueue<ImageInfo> queue = new ConcurrentQueue<ImageInfo>();
            PixivCS.Objects.UserIllusts followIllust = null;
            try
            {
                if (nextUrl == "begin")
                {
                    followIllust = await new PixivAppAPI(baseAPI).GetIllustFollowAsync();
                }
                else
                {
                    Uri next = new Uri(nextUrl);
                    string getparam(string param) => HttpUtility.ParseQueryString(next.Query).Get(param);
                    followIllust = await new PixivCS
                        .PixivAppAPI(baseAPI)
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
            catch (Exception)
            {
                return new ValueTuple<ConcurrentQueue<ImageInfo>, string>(null, "begin");
            }
            return new ValueTuple<ConcurrentQueue<ImageInfo>, string>(queue, nextUrl);
        }

        /// <summary>
        /// 获取书签插画队列(PixivCS)
        /// </summary>
        /// <param name="nextUrl">从首次请求中获取到的带有参数设置的请求url,可用于下一次的带参请求</param>
        /// <param name="actPsw"></param>
        /// <param name="refreshToken"></param>
        /// <returns>元组数据，item1为插画信息队列，item2为下次请求的url参数</returns>
        public async Task<ValueTuple<ConcurrentQueue<ImageInfo>, string>> getBookmarkIllustList(string nextUrl)
        {
            ConcurrentQueue<ImageInfo> queue = new ConcurrentQueue<ImageInfo>();
            PixivCS.Objects.UserIllusts bookmarkIllust = null;
            try
            {
                if (nextUrl == "begin")
                {
                    bookmarkIllust = await new PixivAppAPI(baseAPI).GetUserBookmarksIllustAsync(currentUser.Id);
                }
                else
                {
                    Uri next = new Uri(nextUrl);
                    string getparam(string param) => HttpUtility.ParseQueryString(next.Query).Get(param);
                    bookmarkIllust = await new PixivCS
                        .PixivAppAPI(baseAPI)
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
            catch (Exception)
            {
                return new ValueTuple<ConcurrentQueue<ImageInfo>, string>(null, "begin");
            }
            return new ValueTuple<ConcurrentQueue<ImageInfo>, string>(queue, nextUrl);
        }
    }
}

