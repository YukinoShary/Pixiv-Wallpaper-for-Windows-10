using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace Pixiv_Wallpaper_for_Windows_10.Util
{
    class WebViewLogin
    {
        private string loginUri;
        private string targetUri;
        public delegate void OutputCookie(string str);
        public OutputCookie Method;//需要在调用方实现此委托方法，以实现cookie的赋值

        private HttpBaseProtocolFilter filter = new HttpBaseProtocolFilter();
        private WebView webView = new WebView();
        private AppWindow appWindow;
        private HttpRequestMessage httpRequestMessage = new HttpRequestMessage();

        public WebViewLogin(string loginUri,string targetUri)
        {
            filter.UseProxy = true;
            webView.NavigationStarting += WebView_NavigationStarting;
            this.loginUri = loginUri;
            this.targetUri = targetUri;
        }

        public async Task ShowWebView(int width,int height)
        {
            appWindow = await AppWindow.TryCreateAsync();
            webView.Width = width;
            webView.Height = height;
            appWindow.RequestSize(new Windows.Foundation.Size(width, height));
            ElementCompositionPreview.SetAppWindowContent(appWindow, webView);

            httpRequestMessage.Method = HttpMethod.Get;
            httpRequestMessage.RequestUri = new Uri(loginUri);
            appWindow.Closed += delegate
            {
                appWindow = null;
                httpRequestMessage.Dispose();
                filter.Dispose();
                webView.Stop();
            };
            webView.NavigateWithHttpRequestMessage(httpRequestMessage);
            await appWindow.TryShowAsync();
        }

        public void ClearCookies()
        {
            int cookiesCount = filter.CookieManager.GetCookies(new Uri(targetUri)).Count;
            for (int i = 0; i <= cookiesCount - 1; i++)
            {
                filter.CookieManager.DeleteCookie(filter.CookieManager.GetCookies(new Uri(targetUri)).First());
            }
        }

        private async void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            if (args.Uri.ToString() == targetUri)
            {
                List<HttpCookie> li = filter.CookieManager.GetCookies(new Uri(targetUri)).ToList();
                string cookie = "";
                for (int i = 0; i <= li.Count - 1; i++)
                {
                    cookie += li[i];
                }
                Debug.Write(cookie);
                if (cookie != "")
                {
                    Method(cookie);
                    await appWindow.CloseAsync();
                }
            }
        }
    }
}
