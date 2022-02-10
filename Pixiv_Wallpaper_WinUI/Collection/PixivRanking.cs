using Pixiv_Wallpaper_WinUI.Model;
using Pixiv_Wallpaper_WinUI.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Pixiv_Wallpaper_WinUI.Collection
{
    class PixivRanking
    {
        private ConcurrentQueue<ImageInfo> illustQueue;
        private Pixiv pixiv;
        private ResourceLoader loader;
        private string rankingMode;
        private DateTime date;
        private string nextUrl;

        public PixivRanking(ResourceLoader loader, Pixiv pixiv)
        {
            this.pixiv = pixiv;
            this.loader = loader;
            illustQueue = new ConcurrentQueue<ImageInfo>();
            rankingMode = "";
            date = DateTime.UtcNow.Date.AddDays(-1);
            nextUrl = "begin";
        }

        public async Task<bool> ListUpdate(string rankingMode, bool flag = false)
        {
            if (flag || illustQueue == null || illustQueue.Count == 0 || this.rankingMode != rankingMode)
            {
                //日期参数依旧存在问题
                if (rankingMode == "day")
                {
                    ValueTuple<ConcurrentQueue<ImageInfo>, string> vt = new ValueTuple<ConcurrentQueue<ImageInfo>, string>();
                    if (date.Year == 1)
                        vt = await pixiv.getRankingList(mode: rankingMode,date : null, nextUrl: nextUrl);
                    else
                        vt = await pixiv.getRankingList(mode : rankingMode, date : date.ToString("yyyy-MM-dd"), nextUrl : nextUrl);
                    illustQueue = vt.Item1;
                    nextUrl = vt.Item2;
                    if (nextUrl == "begin")
                        date = date.AddDays(-1);
                    /*
                    else
                    {
                        string dt = System.Web.HttpUtility.ParseQueryString(new Uri(nextUrl).Query).Get("date");
                        date = DateTime.ParseExact(System.Web.HttpUtility.ParseQueryString(new Uri(nextUrl).Query).Get("date"),
                            "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    }
                    */
                }    
                else
                {
                    var t = await pixiv.getRankingList(mode: rankingMode, nextUrl: nextUrl);
                    illustQueue = t.Item1;
                    nextUrl = t.Item2;
                }         
                if (illustQueue != null)
                {
                    return true;
                }
                else
                {
                    string title = loader.GetString("FailToGetRankingUpdatingQueue");
                    string content = loader.GetString("FailToGetQueueExplanation");
                    ToastMessage tm = new ToastMessage(title, content, ToastMessage.ToastMode.OtherMessage);
                    tm.ToastPush(60);
                    return false;
                }
            }
            else
                return false;
        }

        public async Task<ImageInfo> SelectArtwork(string rankingMode)
        {
            await ListUpdate(rankingMode);
            ImageInfo img = null;
            while (true)
            {
                if (illustQueue == null)
                {
                    break;
                }
                else
                {
                    if (illustQueue.Count != 0)
                    {
                        illustQueue.TryDequeue(out img);
                        if (img != null && img.WHratio >= 1.33 && !img.isR18)
                        {
                            return img;
                        }
                    }
                    else 
                    {
                        if (rankingMode != "day")
                        {
                            string title = loader.GetString("EmptyQueue");
                            ToastMessage tm = new ToastMessage(title, null, ToastMessage.ToastMode.OtherMessage);
                            tm.ToastPush(60);
                        }
                        break;
                    }
                }
            }
            return null;
        }
    }
}
