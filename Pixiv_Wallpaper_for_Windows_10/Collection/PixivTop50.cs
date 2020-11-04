using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Concurrent;
using Windows.ApplicationModel.Resources;
using Pixiv_Wallpaper_for_Windows_10.Util;
using Pixiv_Wallpaper_for_Windows_10.Model;

namespace Pixiv_Wallpaper_for_Windows_10.Collection
{
    class PixivTop50
    {
        private ConcurrentQueue<string> results;
        private Pixiv pixiv;
        private ResourceLoader loader;

        public PixivTop50(ResourceLoader loader)
        {
            pixiv = new Pixiv();
            results = new ConcurrentQueue<string>();
            this.loader = loader;
        }


        /// <summary>
        /// 作品类别更新
        /// </summary>
        /// <param name="flag">是否无视列表情况强制更新 默认为否</param>
        /// <returns>返回值用于判断是否成功更新队列</returns>
        public async Task<bool> listUpdate(bool flag = false)
        {
            if (results == null || results.Count <= 0 || flag)
            {
                results = await pixiv.getRallist();
                if (results != null)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        /// <summary>
        /// 作品推送
        /// </summary>
        /// <returns></returns>
        public async Task<ImageInfo> SelectArtWork()
        {
            await listUpdate();

            ImageInfo img = null;
            if (results != null && results.Count > 0)
            {
                string id = "";
                if (results.TryDequeue(out id))
                {
                    img = await pixiv.getImageInfo(id);
                }
                return img;
            }
            else
            {
                string title = loader.GetString("FailToGetTop50Queue");
                string content = loader.GetString("PleaseCheckNetwork");
                ToastMessage tm = new ToastMessage(title, content, ToastMessage.ToastMode.OtherMessage);
                tm.ToastPush(60);
                return null;
            }           
        }

    }
}
