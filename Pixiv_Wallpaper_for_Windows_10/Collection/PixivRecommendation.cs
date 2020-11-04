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
        private ConcurrentQueue<string> RecommV1;
        private ConcurrentQueue<ImageInfo> RecommV2;
        private Pixiv pixiv;
        private Conf config;
        private ResourceLoader loader;
        private string nextUrl;

        public PixivRecommendation(Conf config, ResourceLoader loader)
        {
            this.config = config;
            this.loader = loader;
            pixiv = new Pixiv();
            RecommV1 = new ConcurrentQueue<string>();
            RecommV2 = new ConcurrentQueue<ImageInfo>();
            nextUrl = "begin";
        }

        /// <summary>
        /// 列表更新1
        /// </summary>
        /// <param name="flag">是否强制更新</param>
        /// <returns>返回值用于判断是否成功更新队列</returns>
        public async Task<bool> ListUpdateV1(bool flag = false, string imgId = null)
        {
            pixiv.cookie = config.cookie;
            if (RecommV1 == null || RecommV1.Count == 0 || flag)
            {
                RecommV1 = await pixiv.getRecommenlist(imgId);
                if (RecommV1 != null)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        /// <summary>
        /// 使用Web模拟登录的选择方式
        /// </summary>
        /// <returns></returns>
        public async Task<ImageInfo> SelectArtWorkV1()
        {
            await ListUpdateV1();
            ImageInfo img = null;
            if(RecommV1 != null&&RecommV1.Count != 0)
            {
                while (true)
                {
                    string id = "";
                    if (RecommV1.TryDequeue(out id))
                    {
                        img = await pixiv.getImageInfo(id);
                        if (img != null && img.WHratio >= 1.33 && !img.isR18
                        && await Windows.Storage.ApplicationData.Current.LocalFolder.
                        TryGetItemAsync(img.imgId + '.' + img.format) == null)    //判断高宽比合适，非R18，且非重复插画
                        {
                            return img;
                        }
                    }
                    else
                    {
                        //TODO:DequeueError
                        return null;
                    }
                }
            }
            else
            {
                string title = loader.GetString("FailToGetQueue");
                string content = loader.GetString("FailToGetQueueExplanation");
                ToastMessage tm = new ToastMessage(title, content, ToastMessage.ToastMode.OtherMessage);
                tm.ToastPush(60);
                return null;
            }
        }

        /// <summary>
        /// 使用PixivCS API的选择方法
        /// </summary>
        /// <returns></returns>
        public async Task<ImageInfo> SelectArtWorkV2()
        {
            await ListUpdateV2();
            ImageInfo img = null;
            while (true)
            {
                if (RecommV2 == null)
                {
                    string title = loader.GetString("FailToGetQueue");
                    string content = loader.GetString("FailToGetQueueExplanation");
                    ToastMessage tm = new ToastMessage(title, content, ToastMessage.ToastMode.OtherMessage);
                    tm.ToastPush(60);
                    return null;
                }
                else
                {
                    if (RecommV2.Count != 0)
                    {
                        RecommV2.TryDequeue(out img);
                        if (img != null && img.WHratio >= 1.33 && !img.isR18
                        && await Windows.Storage.ApplicationData.Current.LocalFolder.
                        TryGetItemAsync(img.imgId + '.' + img.format) == null)
                        {
                            return img;
                        }
                    }
                    else if (!await ListUpdateV2())
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
        public async Task<bool> ListUpdateV2(bool flag = false, string imgId = null)
        {
            if(flag || RecommV2.Count == 0 || RecommV2 == null)
            {
                var t = await pixiv.getRecommenlist(imgId, nextUrl, config.account, config.password);
                RecommV2 = t.Item1;
                nextUrl = t.Item2;
                if (RecommV2 != null && RecommV2.Count != 0)
                    return true;                   
                else
                    return false;
            }
            else
                return false;
        }
    }
}
