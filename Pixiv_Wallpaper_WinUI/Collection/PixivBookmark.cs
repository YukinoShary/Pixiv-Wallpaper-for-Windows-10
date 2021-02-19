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
    class PixivBookmark
    {
        private ConcurrentQueue<ImageInfo> illustQueue;
        private Pixiv pixiv;
        private Conf config;
        private ResourceLoader loader;
        private string nextUrl;
        public PixivBookmark(Conf config, ResourceLoader loader, Pixiv pixiv)
        {
            illustQueue = new ConcurrentQueue<ImageInfo>();
            this.pixiv = pixiv;
            this.config = config;
            this.loader = loader;
            nextUrl = "begin";
        }

        public async Task<bool> ListUpdate(bool flag = false)
        {
            if(flag || illustQueue == null || illustQueue.Count == 0)
            { 
                var t = await pixiv.getBookmarkIllustList(nextUrl, config.ActPswText.Item1, config.ActPswText.Item2, config.RefreshToken);
                illustQueue = t.Item1;
                nextUrl = t.Item2;
                config.RefreshToken = t.Item3;
                if (illustQueue != null)
                {
                    return true;
                }      
                else
                {
                    string title = loader.GetString("FailToGetBookmarkQueue");
                    string content = loader.GetString("FailToGetQueueExplanation");
                    ToastMessage tm = new ToastMessage(title, content, ToastMessage.ToastMode.OtherMessage);
                    tm.ToastPush(60);
                    return false;
                }                      
            }
            else
                return false;
        }

        public async Task<ImageInfo> SelectArtwork()
        {
            await ListUpdate();
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
                    else if (!await ListUpdate())
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
