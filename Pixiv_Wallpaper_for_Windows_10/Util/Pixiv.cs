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
using Windows.ApplicationModel.Resources;
using Pixiv_Wallpaper_for_Windows_10.Model;

namespace Pixiv_Wallpaper_for_Windows_10.Util
{
    public class Pixiv
    {
        private readonly string RECOMM_URL = "https://www.pixiv.net/rpc/recommender.php?type=illust&sample_illusts=auto&num_recommendations=3000&page=discovery&mode=all";
        private readonly string RECOMMSAM_URL = "https://www.pixiv.net/rpc/recommender.php?type=illust&sample_illusts=";
        private readonly string DETA_URL = "https://api.imjad.cn/pixiv/v1/?type=illust&id=";
        private readonly string RALL_URL = "https://www.pixiv.net/ranking.php?mode=daily&content=illust&p=1&format=json";

        public string cookie { get; set; }
        private string nexturl = "begin";
        public static PixivBaseAPI GlobalBaseAPI;
        private DownloadManager download;

        public Pixiv()
        {
            GlobalBaseAPI = new PixivBaseAPI();  
            GlobalBaseAPI.ExperimentalConnection = true;   //直连模式打开
            download = new DownloadManager();
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

            rall = await top50.GetDataAsync();

            if (!rall.Equals("ERROR"))
            {
                dynamic o = JObject.Parse(rall);
                JArray arr = o.contents;

                foreach (JToken j in arr)
                {
                    queue.Enqueue(j["illust_id"].ToString());
                }
            }
            else
            {
                string title = MainPage.loader.GetString("UnknownError");
                string content = " ";
                ToastManagement tm = new ToastManagement(title, content, ToastManagement.OtherMessage);
                tm.ToastPush(60);
            }
            return queue;
        }


        /// <summary>
        /// 获取"猜你喜欢"推荐列表(Web模拟)
        /// </summary>
        /// <returns>插画id队列</returns>

        public async Task<ConcurrentQueue<string>> getRecommlistV1(string imgId)
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
            recomm.cookie = cookie;
            recomm.referrer = "https://www.pixiv.net/discovery";

            like = await recomm.GetDataAsync();

            if (like != "ERROR")
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
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns>插画信息队列</returns>
        public async Task<ConcurrentQueue<ImageInfo>> getRecommenlistV2(string account = null, string password = null)
        {
            ConcurrentQueue<ImageInfo> queue = new ConcurrentQueue<ImageInfo>();
            PixivCS.Objects.IllustRecommended recommendres = null;
            if (GlobalBaseAPI.AccessToken == null)
            {
                try
                {
                    PixivCS.Objects.AuthResult res = null;
                    res = await GlobalBaseAPI.AuthAsync(account, password);
                }
                catch (Exception e)
                {
                    return null;
                }
            }
            //是否使用nexturl更新list
            if ("begin".Equals(nexturl))
            {
                recommendres = await new PixivAppAPI(GlobalBaseAPI).GetIllustRecommendedAsync();
            }
            else
            {
                Uri next = new Uri(nexturl);
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
                nexturl = recommendres.NextUrl?.ToString() ?? "";
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
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
            return queue;
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
            string data = await info.GetDataAsync();
            if (!data.Equals("ERROR"))
            {
                imginfo = new ImageInfo();

                dynamic o = JObject.Parse(data);
                dynamic ill = o.response;
                imginfo.viewCount = (int)ill[0]["stats"]["views_count"];
                imginfo.imgUrl = ill[0]["image_urls"]["large"].ToString();
                switch(ill[0]["age_limit"].ToString())
                {
                    case "all_age":
                        imginfo.isR18 = false;
                        break;
                    case "limit_r18":
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

        /// <summary>
        /// 插画下载
        /// </summary>
        /// <param name="img">要下载的插画信息</param>
        /// <returns>是否成功下载插画</returns>
        public async Task<string> downloadImgV1(ImageInfo img)
        {
            Regex reg = new Regex("/c/[0-9]+x[0-9]+/img-master");
            img.imgUrl = reg.Replace(img.imgUrl, "/img-master", 1);
            HttpUtil download = new HttpUtil(img.imgUrl, HttpUtil.Contype.IMG);
            download.referrer = "https://www.pixiv.net/artworks/" + img.imgId;
            download.cookie = cookie;
            return await download.ImageDownloadAsync(img);   
        }

        /// <summary>
        /// 使用PixivCS API进行插画下载
        /// </summary>
        /// <param name="img"></param>
        /// <returns>是否成功下载插画</returns>
        public async Task<string> downloadImgV2(ImageInfo img)
        {
            try
            {
                if (await ApplicationData.Current.LocalFolder.TryGetItemAsync(img.imgId + '.' + img.format) != null)
                {
                    return "EXIST";
                }
                else
                {
                    using (Stream resStream = await (await new PixivAppAPI(GlobalBaseAPI).RequestCall("GET",
                      img.imgUrl, new Dictionary<string, string>() { { "Referer", "https://app-api.pixiv.net/" } })).
                      Content.ReadAsStreamAsync())
                    {
                        StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(img.imgId + '.' + img.format, CreationCollisionOption.ReplaceExisting);
                        using (Stream writer = await file.OpenStreamForWriteAsync())
                        {
                            await resStream.CopyToAsync(writer);
                            return img.imgId;
                        }
                    }
                }   
            }
            catch(Exception)
            {
                string title = MainPage.loader.GetString("UnknownError");
                string content = " ";
                ToastManagement tm = new ToastManagement(title, content, ToastManagement.OtherMessage);
                tm.ToastPush(60);
                return "ERROR";
            }
        }
    }
}
