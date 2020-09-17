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

        private static readonly String USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.102 Safari/537.36 Edg/85.0.564.51";

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
    }
}
