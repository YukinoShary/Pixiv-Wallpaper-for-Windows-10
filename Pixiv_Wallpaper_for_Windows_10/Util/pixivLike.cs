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

namespace Pixiv_Wallpaper_for_Windows_10.Util
{
    class PixivLike
    {
        private ConcurrentQueue<string> like = new ConcurrentQueue<string>();
        private ConcurrentQueue<ImageInfo> likeV2 = new ConcurrentQueue<ImageInfo>();
        private Pixiv pixiv = new Pixiv();
        private Conf c = new Conf();

        /// <summary>
        /// 列表更新1
        /// </summary>
        /// <param name="flag">是否强制更新</param>
        /// <returns>返回值用于判断是否成功更新队列</returns>
        public async Task<bool> ListUpdateV1(bool flag = false)
        {
            if (like == null || like.Count == 0 || flag)
            {
                like = await pixiv.getRecommlistV1();
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
            if(like!=null&&like.Count!=0)
            {
                while (true)
                {
                    string id = "";
                    if(like.TryDequeue(out id))
                    {
                        img = await pixiv.getImageInfo(id);
                    }
                    if (img!=null&&img.WHratio>=1.33&&!img.isR18)
                    {
                        if(!await pixiv.downloadImg(img))         //当获取插画失败时返回null
                        {
                            img = null;
                        }
                        break;
                    }
                }
            }
            else
            {
                //使UI线程调用lambda表达式内的方法
                /*await MainPage.mp.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    //UI code here
                    MessageDialog dialog = new MessageDialog("更新推荐列表失败");
                    await dialog.ShowAsync();
                });*/
                string title = "获取pixiv推荐队列失败";
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
                    likeV2.TryDequeue(out img);
                    if (img != null && img.WHratio >= 1.33 && !img.isR18)
                    {
                        if(!await pixiv.downloadImgV2(img))              //当获取插画失败时返回null
                        {
                            img = null;
                        }
                        break;
                    }
                }
            }
            else 
            {
                //使UI线程调用lambda表达式内的方法
                /*await MainPage.mp.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    //UI code here
                    MessageDialog dialog = new MessageDialog("更新推荐列表失败");
                    await dialog.ShowAsync();
                });*/
                string title = "获取pixiv推荐队列失败";
                string content = "请检查你的网络连接以及账号和密码的正确性";
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
