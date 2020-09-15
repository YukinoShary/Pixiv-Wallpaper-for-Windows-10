using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Windows.Storage;
using System.Diagnostics;
using PixivCS;

namespace Pixiv_Wallpaper_for_Windows_10.Util
{
    class DownloadManager
    { 
        public bool downloading { get; private set; }

        public async Task<bool> DownloadVer1(string url, string id, string format, string cookie, Func<long, long, Task> ProgressCallback)
        {
            //一次只能更新一张插画，在当前下载任务完成前忽略新的下载请求
            if (!downloading)
            {
                downloading = true;
            }
            else
            {
                return false;
            }
            
            Regex reg = new Regex("/c/[0-9]+x[0-9]+/img-master");
            url = reg.Replace(url, "/img-master", 1);
            HttpUtil downloadRequest = new HttpUtil(url, HttpUtil.Contype.IMG);
            downloadRequest.referrer = "https://www.pixiv.net/artworks/" + id;
            downloadRequest.cookie = cookie;
            HttpResponseMessage res = await downloadRequest.NewImageDownloadAsync();
            if(res == null)
            {
                downloading = false;
                return false;
            }

            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(id + '.' + format, CreationCollisionOption.ReplaceExisting);
            var length = res.Content.Headers.ContentLength ?? -1;
            using (var resStream = await res.Content.ReadAsStreamAsync())
            {
                using (Stream fStream = await file.OpenStreamForWriteAsync())
                {
                    var bytesCounter = 0L;
                    int bytesRead;
                    byte[] buffer = new byte[4096];
                    try
                    {
                        while ((bytesRead = await resStream.ReadAsync(buffer, 0, 4096)) != 0)
                        {
                            bytesCounter += bytesRead;
                            await fStream.WriteAsync(buffer, 0, bytesRead);
                            _ = ProgressCallback.Invoke(bytesCounter, length);
                        }
                    }
                    catch(Exception e)
                    {
                        string title = MainPage.loader.GetString("ConnectionLost");
                        string content = MainPage.loader.GetString("ConnectionLostExplanation");
                        ToastManagement tm = new ToastManagement(title, content, ToastManagement.OtherMessage);
                        tm.ToastPush(60);
                        Debug.WriteLine(e.Message);
                        downloading = false;
                        return false;
                    }
                }
            }
            downloading = false;
            return true;
        }

        public async Task<bool> DownloadVer2(string url, string id, string format, Func<long, long, Task> ProgressCallback)
        {
            //一次只能更新一张插画，在当前下载任务完成前忽略新的下载请求
            if (!downloading)
            {
                downloading = true;
            }
            else
            {
                return false;
            }

            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(id + '.' + format, CreationCollisionOption.ReplaceExisting);
            try
            {
                var message = await new PixivAppAPI(Pixiv.GlobalBaseAPI).RequestCall("GET", url, new Dictionary<string, string>() { { "Referer", "https://app-api.pixiv.net/" } });
                long length = message.Content.Headers.ContentLength ?? -1;
                using (var resStream = await message.Content.ReadAsStreamAsync())
                {
                    using (Stream fStream = await file.OpenStreamForWriteAsync())
                    {
                        var bytesCounter = 0L;
                        int bytesRead;
                        byte[] buffer = new byte[4096];
                        while ((bytesRead = await resStream.ReadAsync(buffer, 0, 4096)) != 0)
                        {
                            bytesCounter += bytesRead;
                            await fStream.WriteAsync(buffer, 0, bytesRead);
                            _ = ProgressCallback(bytesCounter, length);
                        }
                    }
                }
            }
            catch (Exception)
            {
                string title = MainPage.loader.GetString("UnknownError");
                string content = " ";
                ToastManagement tm = new ToastManagement(title, content, ToastManagement.OtherMessage);
                tm.ToastPush(60);
                return false;
            }           
            downloading = false;
            return true;
        }
    }
}
