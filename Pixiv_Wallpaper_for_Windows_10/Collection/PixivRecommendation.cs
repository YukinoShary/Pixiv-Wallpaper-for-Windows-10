using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.UI.Popups;
using System.Collections.Concurrent;
using Windows.ApplicationModel.Resources;
using Pixiv_Wallpaper_for_Windows_10.Util;
using Pixiv_Wallpaper_for_Windows_10.Model;

namespace Pixiv_Wallpaper_for_Windows_10.Collection
{
    class PixivRecommendation
    {
        private ConcurrentQueue<ImageInfo> Recomm;
        private Pixiv pixiv;
        private ResourceLoader loader;
        private string nextUrl;

        public PixivRecommendation(ResourceLoader loader, Pixiv pixiv)
        {
            this.loader = loader;
            this.pixiv = pixiv;
            Recomm = new ConcurrentQueue<ImageInfo>();
            nextUrl = "begin";
        }

        
        public async Task<ImageInfo> SelectArtwork()
        {
            await ListUpdate();
            ImageInfo img = null;
            while (true)
            {
                if (Recomm == null)
                {
                    return null;
                }
                else
                {
                    if (Recomm.Count != 0)
                    {
                        Recomm.TryDequeue(out img);
                        if (img != null && img.WHratio >= 1.33 && !img.isR18
                        && await Windows.Storage.ApplicationData.Current.LocalFolder.
                        TryGetItemAsync(img.imgId + '.' + img.format) == null)
                        {
                            return img;
                        }
                    }
                    else if (!await ListUpdate())
                    {
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// 列表更新方式2
        /// </summary>
        /// <param name="flag">是否强制更新</param>
        /// <returns></returns>
        public async Task<bool> ListUpdate(bool flag = false, string imgId = null)
        {
            if(flag || Recomm == null || Recomm.Count == 0)
            {
                var t = await pixiv.getRecommenList(imgId, nextUrl);
                Recomm = t.Item1;
                nextUrl = t.Item2;
                if (Recomm != null && Recomm.Count != 0)
                {
                    return true;
                }                      
                else
                {
                    string title = loader.GetString("FailToGetQueue");
                    string content = loader.GetString("FailToGetQueueExplanation");
                    ToastMessage tm = new ToastMessage(title, content, ToastMessage.ToastMode.OtherMessage);
                    tm.ToastPush(60);
                    return false;
                }    
            }
            else
                return false;
        }
    }
}
