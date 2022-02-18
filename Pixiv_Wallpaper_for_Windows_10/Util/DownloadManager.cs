using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using Windows.Web.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Windows.Storage;
using System.Diagnostics;
using PixivCS;
using Windows.ApplicationModel.Resources;

namespace Pixiv_Wallpaper_for_Windows_10.Util
{
    class DownloadManager
    { 
        public static bool downloading { get; private set; }

        public static async Task<bool> DownloadAsync(string url, string id, string format, Pixiv p, Func<long, long, Task> ProgressCallback)
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
            if(await ApplicationData.Current.LocalFolder.TryGetItemAsync(id + '.' + format) == null)
            {
                try
                {
                    StorageFolder folder = await ApplicationData.Current.LocalFolder.GetFolderAsync("illusts");
                    StorageFile file = await folder.CreateFileAsync(id + '.' + format, CreationCollisionOption.ReplaceExisting);
                    var message = await new PixivAppAPI(p.baseAPI).RequestCall("GET", url, new Dictionary<string, string>() { { "Referer", "https://app-api.pixiv.net/" } });
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
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    downloading = false;
                    return false;
                }
            }                      
            downloading = false;
            return true;
        }
    }
}
