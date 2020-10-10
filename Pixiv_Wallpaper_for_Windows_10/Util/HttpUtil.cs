using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Windows.Web.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.Diagnostics;
using System.IO.Compression;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.ApplicationModel.Resources;
using Pixiv_Wallpaper_for_Windows_10.Model;
using HttpStatusCode = Windows.Web.Http.HttpStatusCode;

namespace Pixiv_Wallpaper_for_Windows_10.Util
{

    //TODO:测试System.Net.Http重构的新方法
    public class HttpUtil
    {
        private ResourceLoader loader;
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
        /// 供UI线程调用  
        /// </summary>
        /// <param name="url">要请求的 URL</param>
        /// <param name="dataType">要请求的 MIME 类型</param>
        public HttpUtil(string url, Contype dataType)
        {
            this.url = url;
            this.dataType = dataType;
            loader = ResourceLoader.GetForCurrentView("Resources");
            ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        }

        /// <summary>
        /// 供子线程调用
        /// </summary>
        /// <param name="url"></param>
        /// <param name="dataType"></param>
        /// <param name="loader"></param>
        public HttpUtil(string url, Contype dataType, ResourceLoader loader)
        {
            this.url = url;
            this.dataType = dataType;
            this.loader = loader;
            ServicePointManager.ServerCertificateValidationCallback += (s, cert, chain, sslPolicyErrors) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
        }

        /// <summary>
        /// HTTP GET请求
        /// </summary>
        /// <returns>从URL获取的数据</returns>
        public async Task<string> NewGetDataAsync()
        {
            using (HttpClient httpClient = new HttpClient())
            {
                //构造请求头
                var headers = httpClient.DefaultRequestHeaders;
                headers.UserAgent.TryParseAdd(USER_AGENT);
                headers.Accept.TryParseAdd(contype[(int)dataType]);
                if(cookie != null)
                {
                    headers.Cookie.TryParseAdd(cookie);
                }
                if (authority != null)
                {
                    headers.Add("Authority", authority);
                }
                else
                {
                    headers.Add("Authority", "www.pixiv.net");
                }
                if (referrer != null)
                {
                    headers.Add("Referer", referrer);
                }

                try
                {
                    HttpResponseMessage message = await httpClient.GetAsync(new Uri(url));
                    string res = "Error";
                    if (message.IsSuccessStatusCode)
                    {
                        res = await message.Content.ReadAsStringAsync();
                        return res;
                    }
                    else
                    {
                        string title = loader.GetString("DataRequestFail");
                        string content =  message.ReasonPhrase;
                        ToastMessage tm = new ToastMessage(title, content, ToastMessage.ToastMode.OtherMessage);
                        tm.ToastPush(60);
                        return "ERROR";
                    }
                        
                }
                catch (Exception e)
                {
                    string title = loader.GetString("DataRequestFail");
                    string content = "";
                    if ("The remote server returned an error: (403) .".Equals(e.Message))
                    {
                        content = loader.GetString("DataRequestFailExplanation");
                    }
                    else
                    {
                        content = e.Message.ToString();
                    }
                    ToastMessage tm = new ToastMessage(title, content, ToastMessage.ToastMode.OtherMessage);
                    tm.ToastPush(60);
                    return "ERROR";
                }
            }           
        }

        /// <summary>
        /// 获取插画原图
        /// </summary>
        /// <returns>插画服务器返回的http回应消息</returns>
        public async Task<HttpResponseMessage> NewImageDownloadAsync()
        {
            HttpClient httpClient = new HttpClient();

            //构造请求头
            var headers = httpClient.DefaultRequestHeaders;
            headers.UserAgent.TryParseAdd(USER_AGENT);
            headers.Accept.TryParseAdd(contype[(int)dataType]);
            headers.Cookie.TryParseAdd(cookie);
            headers.Add("Referer", "https://www.pixiv.net/discovery");
            try
            {
                //访问一次插画页使pixiv计入浏览数
                await httpClient.GetAsync(new Uri(referrer));

                //正式获取插画
                httpClient = new HttpClient();
                headers = httpClient.DefaultRequestHeaders;
                headers.UserAgent.TryParseAdd(USER_AGENT);
                headers.Accept.TryParseAdd(contype[(int)dataType]);
                headers.Cookie.TryParseAdd(cookie);
                headers.TryAdd("Referer", referrer);
                
                HttpResponseMessage message = await httpClient.GetAsync(new Uri(url), HttpCompletionOption.ResponseHeadersRead);
                if (message.IsSuccessStatusCode)
                {
                    return message;
                }
                else
                {
                    string title = loader.GetString("ConnectionFail");
                    string content = loader.GetString("ConnectionFailExplanation");
                    ToastMessage tm = new ToastMessage(title, content, ToastMessage.ToastMode.OtherMessage);
                    tm.ToastPush(60);
                    return null;
                }
            }
            catch (Exception)
            {
                string title = loader.GetString("ConnectionLost");
                string content = loader.GetString("ConnectionLostExplanation");
                ToastMessage tm = new ToastMessage(title, content, ToastMessage.ToastMode.OtherMessage);
                tm.ToastPush(60);
                return null;
            }
        }
    }
}
