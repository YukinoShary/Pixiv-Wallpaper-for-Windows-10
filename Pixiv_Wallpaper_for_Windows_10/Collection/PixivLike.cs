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
    class PixivLike
    {
        private ConcurrentQueue<string> likeV1 = new ConcurrentQueue<string>();
        private ConcurrentQueue<ImageInfo> likeV2 = new ConcurrentQueue<ImageInfo>();
        private Pixiv pixiv;
        private Conf config;
        private ResourceLoader loader;

        public PixivLike(Conf config, ResourceLoader loader)
        {
            this.config = config;
            this.loader = loader;
            pixiv = new Pixiv();
        }

        /// <summary>
        /// 列表更新1
        /// </summary>
        /// <param name="flag">是否强制更新</param>
        /// <returns>返回值用于判断是否成功更新队列</returns>
        public async Task<bool> ListUpdateV1(bool flag = false, string imgId = null)
        {
            pixiv.cookie = config.cookie;
            if (likeV1 == null || likeV1.Count == 0 || flag)
            {
                likeV1 = await pixiv.getRecommenlist(imgId);
                if (likeV1 != null)
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
            if(likeV1 != null&&likeV1.Count != 0)
            {
                while (true)
                {
                    string id = "";
                    if (likeV1.TryDequeue(out id))
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
            if(likeV2!=null&&likeV2.Count!=0)
            {
                while (true)
                {
                    if (likeV2.TryDequeue(out img))
                    {
                        if (img != null && img.WHratio >= 1.33 && !img.isR18
                        && await Windows.Storage.ApplicationData.Current.LocalFolder.
                    TryGetItemAsync(img.imgId + '.' + img.format) == null)
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
        /// 列表更新方式2
        /// </summary>
        /// <param name="flag">是否强制更新</param>
        /// <returns></returns>
        public async Task<bool> ListUpdateV2(bool flag = false, string imgId = null)
        {
            if(flag || likeV2.Count==0 || likeV1 == null)
            {
                likeV2 = await pixiv.getRecommenlist(imgId, config.account, config.password);
                if (likeV2 != null)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }
    }
}
