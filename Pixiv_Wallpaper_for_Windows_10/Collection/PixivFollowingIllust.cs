﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pixiv_Wallpaper_for_Windows_10.Model;
using Pixiv_Wallpaper_for_Windows_10.Util;
using Windows.ApplicationModel.Resources;

namespace Pixiv_Wallpaper_for_Windows_10.Collection
{
    class PixivFollowingIllust
    {
        private ConcurrentQueue<ImageInfo> illustQueue;
        private Pixiv pixiv;
        private Conf config;
        private ResourceLoader loader;
        private string nextUrl;
        public PixivFollowingIllust(Conf config, ResourceLoader loader)
        {
            illustQueue = new ConcurrentQueue<ImageInfo>();
            this.config = config;
            this.loader = loader;
            pixiv = new Pixiv();
            nextUrl = "begin";
        }
        public async Task<bool> ListUpdate(bool flag = false)
        {
            if (flag || illustQueue == null || illustQueue.Count == 0)
            {
                var t = await pixiv.getIllustFollowList(nextUrl, config.account, config.password);
                illustQueue = t.Item1;
                nextUrl = t.Item2;
                if (illustQueue != null && illustQueue.Count != 0)
                    return true;
                else
                    return false;
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
                if(illustQueue == null)
                {
                    string title = loader.GetString("FailToGetFollowingUserUpdatingQueue");
                    string content = loader.GetString("FailToGetQueueExplanation");
                    ToastMessage tm = new ToastMessage(title, content, ToastMessage.ToastMode.OtherMessage);
                    tm.ToastPush(60);
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