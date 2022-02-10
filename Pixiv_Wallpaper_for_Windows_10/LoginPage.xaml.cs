using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Web.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http.Filters;
using System.Threading.Tasks;
using Pixiv_Wallpaper_for_Windows_10.Model;
using Pixiv_Wallpaper_for_Windows_10.Util;
using Windows.ApplicationModel.Resources;
using Windows.Storage;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace Pixiv_Wallpaper_for_Windows_10
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        private static PixivCS.PixivBaseAPI baseAPI;
        private static LoginPage lp;
        private static Conf conf;
        private static Pixiv pixiv;
        private static Frame frame;
        private ResourceLoader loader;
        private readonly string User_Agent = "Mozilla/5.0 (iPad; CPU OS 13_3 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/87.0.4280.77 Mobile/15E148 Safari/604.1";

        public LoginPage()
        {
            this.InitializeComponent();
            lp = this;
            frame = Window.Current.Content as Frame;
            loader = ResourceLoader.GetForCurrentView("Resources");
            baseAPI = new PixivCS.PixivBaseAPI();
            baseAPI.ExperimentalConnection = false;
            conf = new Conf();
            lp.webView.NavigationStarting += Web_NavigationStarting;
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            LoginMethod();
        }

        public static async Task LoginMethod()
        {
            PixivCS.Objects.AuthResult res = null;
            string refreshToken = conf.RefreshToken;
            try
            {
                if (refreshToken != null && !refreshToken.Equals("Invalidation"))
                {
                    res = await baseAPI.AuthAsync(refreshToken);
                    conf.RefreshToken = res.Response.RefreshToken;
                    var currentUser = res.Response.User;
                    pixiv = new Pixiv(baseAPI, currentUser);
                    frame.Navigate(typeof(MainPage), new ValueTuple<Pixiv,Conf>(pixiv, conf));
                    lp.webView.Visibility = Visibility.Collapsed;
                }
                else
                {
                    lp.webView.Navigate(baseAPI.GenerateWebViewUri());
                    lp.webView.Visibility = Visibility.Visible;
                }
            }
            catch (Exception)
            {
                conf.RefreshToken = "Invalidation";
                lp.webView.Navigate(baseAPI.GenerateWebViewUri());
                lp.webView.Visibility = Visibility.Visible;
            }
        }

        public static async Task GetToken(string uri)
        {
            Console.WriteLine(uri);
            try
            {
                string[] uriSplit = uri.Split('=', '&');
                string monitor = uriSplit[1];
                var res = await baseAPI.Code2Token(uriSplit[1]);
                var currentUser = res.Response.User;
                pixiv = new Pixiv(baseAPI, currentUser);
                conf.RefreshToken = res.Response.RefreshToken;
                frame.Navigate(typeof(MainPage), new ValueTuple<Pixiv, Conf>(pixiv, conf));
                lp.webView.Stop();

            }
            catch (Exception)
            {
                string title = lp.loader.GetString("FailToLogin");
                ToastMessage message = new ToastMessage(title, "", ToastMessage.ToastMode.OtherMessage);
                message.ToastPush(30);
            }
        }

        private void Web_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args)
        {
            lp.webView.NavigationStarting -= Web_NavigationStarting;
            args.Cancel = true;
            NavigateWithHeader(args.Uri);
        }

        private void NavigateWithHeader(Uri uri)
        {
            var requestMsg = new Windows.Web.Http.HttpRequestMessage(HttpMethod.Get, uri);
            requestMsg.Headers.Add("User-Agent", User_Agent);
            lp.webView.NavigateWithHttpRequestMessage(requestMsg);
            lp.webView.NavigationStarting += Web_NavigationStarting;
        }
    }
}
