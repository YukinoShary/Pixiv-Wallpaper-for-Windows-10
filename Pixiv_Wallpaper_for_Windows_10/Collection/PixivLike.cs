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

namespace Pixiv_Wallpaper_for_Windows_10.Collection
{
    class PixivLike
    {
        private ConcurrentQueue<string> like = new ConcurrentQueue<string>();
        private ConcurrentQueue<ImageInfo> likeV2 = new ConcurrentQueue<ImageInfo>();
        private Pixiv pixiv = new Pixiv();
        private Conf c = new Conf();
        private static ResourceLoader loader = ResourceLoader.GetForCurrentView("Resources");

        /// <summary>
        /// 列表更新1
        /// </summary>
        /// <param name="flag">是否强制更新</param>
        /// <returns>返回值用于判断是否成功更新队列</returns>
        public async Task<bool> ListUpdateV1(bool flag = false, string imgId = null)
        {
            if (like == null || like.Count == 0 || flag)
            {
                like = await pixiv.getRecommlistV1(imgId);
                if (like != null)
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
            pixiv.cookie = c.cookie;

            await ListUpdateV1();
            ImageInfo img = null;
            if(like != null&&like.Count != 0)
            {
                while (true)
                {
                    string id = "";
                    if (like.TryDequeue(out id))
                    {
                        img = await pixiv.getImageInfo(id);
                    }
                    if (img != null && img.WHratio >= 1.33 && !img.isR18)
                    {
                        string result = await pixiv.downloadImgV1(img);
                        if (img.imgId.Equals(result))//返回结果为imgId则跳出循环
                        {
                            break;
                        }
                        else if ("ERROR".Equals(await pixiv.downloadImgV1(img)))//返回结果为"ERROR"则使img为null并跳出循环
                        {
                            img = null;
                            break;
                        }
                        //若返回结果为"EXIST"则继续循环
                    }
                }
            }
            else
            {
                string title = loader.GetString("FailToGetQueue");
                string content = "请检查你的网络连接，检查登录状态或尝试再次登录以更新过期的cookie";
                ToastManagement tm = new ToastManagement(title, content, ToastManagement.OtherMessage);
                tm.ToastPush(60);
            }
            return img;
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
                    string id = "";
                    if (like.TryDequeue(out id))
                    {
                        img = await pixiv.getImageInfo(id);
                    }
                    if (img != null && img.WHratio >= 1.33 && !img.isR18)
                    {
                        string result = await pixiv.downloadImgV2(img);
                        if (img.imgId.Equals(result))
                        {
                            break;
                        }    
                        else if ("ERROR".Equals(await pixiv.downloadImgV2(img)))
                        {
                            img = null;
                            break;
                        }
                        //若返回结果为"EXIST"则继续循环
                    }
                }
            }
            else 
            {
                string title = loader.GetString("FailToGetQueue");
                string content = loader.GetString("FailToGetQueueExplanation");
                ToastManagement tm = new ToastManagement(title, content, ToastManagement.OtherMessage);
                tm.ToastPush(60);
            }
            return img;
        }

        /// <summary>
        /// 列表更新方式2
        /// </summary>
        /// <param name="flag">是否强制更新</param>
        /// <returns></returns>
        public async Task<bool> ListUpdateV2(bool flag = false)
        {
            if(flag || likeV2.Count==0 || like == null)
            {
                likeV2 = await pixiv.getRecommenlistV2(c.account,c.password);
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
