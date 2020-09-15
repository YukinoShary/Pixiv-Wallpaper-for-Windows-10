using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.Diagnostics;
using System.IO.Compression;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.ApplicationModel.Resources;
using Pixiv_Wallpaper_for_Windows_10.Model;

namespace Pixiv_Wallpaper_for_Windows_10.Util
{

    //TODO:测试System.Net.Http重构的新方法
    public class HttpUtil
    {
        public HttpUtil()
        {  
        }
        public enum Contype
        {
            /// <summary>
            /// JSON 数据
            /// </summary>
            JSON,
            /// <summary>
            /// HTML 页面
            /// </summary>
            HTML,
            /// <summary>
            /// 图片类型
            /// </summary>
            IMG
        };
        private string[] contype = {
            "application/json, text/javascript, */*; q=0.01",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8",
            "image/webp,image/apng,image/*,*/*;q=0.8"
        };

        private static readonly String USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.120 Safari/537.36";

        private string url;
        private Contype dataType;

        /// <summary>
        /// 设置或获取Cookie
        /// </summary>
        public string cookie { get; set; }

        /// <summary>
        /// 获取或设置原引用地址
        /// </summary>
        public string referrer { get; set; }

        /// <summary>
        /// 获取或设置认证网址
        /// </summary>
        public string authority { get; set; }

        /// <summary>
        ///   
        /// </summary>
        /// <param name="url">要请求的 URL</param>
        /// <param name="dataType">要请求的 MIME 类型</param>
        public HttpUtil(string url, Contype dataType)
        {
            this.url = url;
            this.dataType = dataType;

            ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        }

        /// <summary>
        /// HTTP GET请求
        /// </summary>
        /// <returns>从URL获取的数据</returns>
        public async Task<string> NewGetDataAsync()
        {
            HttpClient httpClient = null;

            //设置文件解压
            HttpClientHandler handler = new HttpClientHandler()
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            httpClient = new HttpClient(handler);

            //构造请求头
            httpClient.DefaultRequestHeaders.Add("Cookie", cookie);
            httpClient.DefaultRequestHeaders.Add("Accept", contype);
            httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip,deflate,sdch");
            httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
            httpClient.DefaultRequestHeaders.Add("Scheme", "https");
            if (authority != null)
            {
                httpClient.DefaultRequestHeaders.Add("Authority", authority);
            }
            else
            {
                httpClient.DefaultRequestHeaders.Add("Authority", "www.pixiv.net");
            }
            if (referrer != null)
            {
                httpClient.DefaultRequestHeaders.Add("Referer", referrer);
            }

            try
            {
                HttpResponseMessage message = await httpClient.GetAsync(url);
                string res = "Error";
                if(message.StatusCode == HttpStatusCode.OK)
                {
                    using (StreamReader reader = new StreamReader(await message.Content.ReadAsStreamAsync(), Encoding.GetEncoding("utf-8")))
                    {
                        res = await reader.ReadToEndAsync();
                    }
                }
                return res;
            }
            catch (Exception e)
            {
                string title = MainPage.loader.GetString("DataRequestFail");
                string content = "";
                if ("The remote server returned an error: (403) .".Equals(e.Message))
                {
                    content = MainPage.loader.GetString("DataRequestFailExplanation");
                }
                else
                {
                    content = e.Message.ToString();
                }
                ToastManagement tm = new ToastManagement(title, content, ToastManagement.OtherMessage);
                tm.ToastPush(60);
                return "ERROR";
            }
        }

        /// <summary>
        /// HTTP GET请求
        /// </summary>
        /// <returns>返回的数据</returns>
        public async Task<string> GetDataAsync()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Accept = contype[(int)dataType];
            request.Headers["Cookie"] = cookie;
            request.Headers["Accept-Encoding"] = "gzip,deflate,sdch";
            request.UserAgent = USER_AGENT;
            request.Headers["Scheme"] = "https";
            request.Headers["Authority"] = "www.pixiv.net";
            if (authority != null)
            {
                request.Headers["Authority"] = authority;
            }           
            if (referrer != null)
            {
                request.Headers["Referer"] = referrer;
            }

            try
            {
                HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                string res = "Error";
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    Stream s = response.GetResponseStream();
                    if (response.Headers["Content-Encoding"] != null && response.Headers["Content-Encoding"].ToLower().Contains("gzip"))
                    {
                        s = new GZipStream(s, CompressionMode.Decompress);
                    }
                    StreamReader sr = new StreamReader(s, Encoding.GetEncoding("utf-8"));
                    res = await sr.ReadToEndAsync();
                    cookie = response.Headers["Set-Cookie"];

                    sr.Dispose();
                    s.Dispose();  
                }
                response.Dispose();
                return res;
            }
            catch(Exception e)
            {
                string title = MainPage.loader.GetString("DataRequestFail");
                string content = "";
                if ("The remote server returned an error: (403) .".Equals(e.Message))
                {
                    content = MainPage.loader.GetString("DataRequestFailExplanation");
                }
                else
                {
                    content = e.Message.ToString();
                }
                ToastManagement tm = new ToastManagement(title, content, ToastManagement.OtherMessage);
                tm.ToastPush(60);
                return "ERROR";
            }
        }

        /// <summary>
        /// 获取插画原图
        /// </summary>
        /// <returns>插画服务器返回的http回应消息</returns>
        public async Task<HttpResponseMessage> NewImageDownloadAsync()
        {
            HttpClient httpClient = new HttpClient();

            //构造请求头,并访问一次插画页使pixiv计入浏览数
            httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
            httpClient.DefaultRequestHeaders.Add("Accept", contype);
            httpClient.DefaultRequestHeaders.Add("Cookie", cookie);
            httpClient.DefaultRequestHeaders.Add("Referer", "https://www.pixiv.net/discovery");
            await httpClient.GetAsync(referrer);

            //正式获取插画
            httpClient.DefaultRequestHeaders.Add("Referer", referrer);
            try
            {
                HttpResponseMessage message = await httpClient.GetAsync(url);
                if (message.StatusCode == HttpStatusCode.OK)
                {
                    return message;
                }
                else
                {
                    string title = MainPage.loader.GetString("ConnectionFail");
                    string content = MainPage.loader.GetString("ConnectionFailExplanation");
                    ToastManagement tm = new ToastManagement(title, content, ToastManagement.OtherMessage);
                    tm.ToastPush(60);
                    return null;
                }
            }
            catch (Exception)
            {
                string title = MainPage.loader.GetString("ConnectionLost");
                string content = MainPage.loader.GetString("ConnectionLostExplanation");
                ToastManagement tm = new ToastManagement(title, content, ToastManagement.OtherMessage);
                tm.ToastPush(60);
                return null;
            }
        }

        /// <summary>
        /// 插画下载方法
        /// </summary>
        /// <param name="img">插画信息</param>
        /// <returns></returns>
        public async Task<string> ImageDownloadAsync(ImageInfo img)
        {
            //应访问一次插画展示页使pixiv记录一次浏览数以尊重他人成果
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(referrer);
            request.Method = "GET";
            request.Accept = contype[(int)dataType];
            request.Headers["Cookie"] = cookie;
            request.Headers["Referer"] = "https://www.pixiv.net/discovery";
            await request.GetResponseAsync();

            //正式开始获取插画原图
            request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Accept = contype[(int)dataType];
            request.Headers["Cookie"] = cookie;
            request.Headers["Referer"] = referrer;

            try
            {
                //判断文件夹中是否已存在该插画，若存在则直接返回该插画
                if(await ApplicationData.Current.LocalFolder.TryGetItemAsync(img.imgId + '.' + img.format) != null)
                {
                    return "EXIST";
                }
                else
                {
                    HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        using (Stream res = response.GetResponseStream())
                        {

                            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync(img.imgId + '.' + img.format, CreationCollisionOption.ReplaceExisting);
                            using (Stream writer = await file.OpenStreamForWriteAsync())
                            {
                                await res.CopyToAsync(writer);
                                return img.imgId;
                            }
                        }
                    }
                    else
                    {
                        string title = MainPage.loader.GetString("ConnectionFail") ;
                        string content = MainPage.loader.GetString("ConnectionFailExplanation");
                        ToastManagement tm = new ToastManagement(title, content, ToastManagement.OtherMessage);
                        tm.ToastPush(60);
                        return "ERROR";
                    }
                }
            }    
            catch(Exception)
            {
                string title = MainPage.loader.GetString("ConnectionLost");
                string content = MainPage.loader.GetString("ConnectionLostExplanation");
                ToastManagement tm = new ToastManagement(title, content, ToastManagement.OtherMessage);
                tm.ToastPush(60);
                return "ERROR";
            }

        }
    }
}
