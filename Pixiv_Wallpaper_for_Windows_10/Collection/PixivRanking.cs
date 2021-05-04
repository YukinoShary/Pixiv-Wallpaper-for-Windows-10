using Pixiv_Wallpaper_for_Windows_10.Model;
using Pixiv_Wallpaper_for_Windows_10.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Pixiv_Wallpaper_for_Windows_10.Collection
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
            date = DateTime.Today;
            nextUrl = "begin";
        }

        public async Task<bool> ListUpdate(string rankingMode, bool flag = false)
        {
            if (flag || illustQueue == null || illustQueue.Count == 0 || this.rankingMode != rankingMode)
            {
                if (rankingMode == "day")
                {
                    //切换日期与nextUrl逻辑冲突未解决
                    var t = await pixiv.getRankingList(mode : rankingMode, date : date.ToString("yyyy-MM-dd"), nextUrl : nextUrl);
                    illustQueue = t.Item1;
                    nextUrl = t.Item2;
                    date = date.AddDays(-1);
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
                    return null;
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
                    else if (rankingMode != "day")
                    {
                        string title = loader.GetString("EmptyQueue");
                        ToastMessage tm = new ToastMessage(title, null, ToastMessage.ToastMode.OtherMessage);
                        tm.ToastPush(60);
                        return null;
                    }
                }
            }
        }
    }
}
