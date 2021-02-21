using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.WindowManagement;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace Pixiv_Wallpaper_for_Windows_10
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

        public WebViewLogin(string loginUri, string targetUri)
        {
            filter.UseProxy = true;
            this.loginUri = loginUri;
            this.targetUri = targetUri;
            webView.NavigationStarting += WebView_NavigationStarting;
            webView.ScriptNotify += WebView_ScriptNotify;
        }

        public async Task ShowWebView(int width, int height)
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
                Console.Write(cookie);
                if (cookie != "")
                {
                    Method(cookie);
                    await appWindow.CloseAsync();
                }
            }
        }

        /// <summary>
        /// 抓取Script
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void WebView_ScriptNotify(object sender, NotifyEventArgs e)
        {
            Windows.UI.Popups.MessageDialog dialog;
            string[] messageArray = e.Value.Split(':');
            string message;
            string type;
            if (messageArray.Length > 1)
            {
                message = messageArray[1];
                type = messageArray[0];
            }
            else
            {
                message = e.Value;
                type = "typeAlert";
            }
            dialog = new Windows.UI.Popups.MessageDialog(message);
            Console.WriteLine("type=" + type + " ,message=" + message);

            if (type.Equals("typeConfirm"))
            {
                dialog.Commands.Add(new UICommand("Yes"));
                dialog.Commands.Add(new UICommand("Cancel"));
                dialog.DefaultCommandIndex = 0;
                dialog.CancelCommandIndex = 1;
            }
            var result = await dialog.ShowAsync();
            if (result.Label.Equals("Yes"))
            {
                // do something you want, maybe invoke a script
                //await webView1.InvokeScriptAsync("eval", new string[] { functionString });
            }
            else
            {
                // do something you want, maybe invoke a script
                //await webView1.InvokeScriptAsync("eval", new string[] { functionString });
            }
        }
    }
}
