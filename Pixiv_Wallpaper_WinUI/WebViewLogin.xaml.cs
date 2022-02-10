using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using Pixiv_Wallpaper_WinUI.Model;
using Pixiv_Wallpaper_WinUI.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Web.Http.Filters;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Pixiv_Wallpaper_WinUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class WebViewLogin : Page
    {
        private static PixivCS.PixivBaseAPI baseAPI;
        private static WebViewLogin lp;
        private static Conf conf;
        private static Pixiv pixiv;
        private static Frame frame;
        private ResourceLoader loader;
        private readonly string authUri = "pixiv://account/login";

        public WebViewLogin()
        {
            this.InitializeComponent();
            lp = this;
            frame = Window.Current.Content as Frame;
            loader = ResourceLoader.GetForCurrentView("Resources");
            baseAPI = new PixivCS.PixivBaseAPI();
            webView.NavigationStarting += NavigationStarting;
            conf = new Conf();
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
                    frame.Navigate(typeof(MainPage), new ValueTuple<Pixiv, Conf>(pixiv, conf));
                }
                else
                {
                    lp.webView.Source = baseAPI.GenerateWebViewUri();
                    lp.webView.Visibility = Visibility.Visible;
                }
            }
            catch (Exception)
            {
                //refreshToken失效或是代理+去除SNI导致的认证失败
                try
                {
                    baseAPI.ExperimentalConnection = false;
                    res = await baseAPI.AuthAsync(refreshToken);
                    conf.RefreshToken = res.Response.RefreshToken;
                    var currentUser = res.Response.User;
                    pixiv = new Pixiv(baseAPI, currentUser);
                    frame.Navigate(typeof(MainPage), new ValueTuple<Pixiv, Conf>(pixiv, conf));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    conf.RefreshToken = "Invalidation";
                    lp.webView.Source = baseAPI.GenerateWebViewUri();
                    lp.webView.Visibility = Visibility.Visible;
                }
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

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                string title = lp.loader.GetString("FailToLogin");
                ToastMessage message = new ToastMessage(title, "", ToastMessage.ToastMode.OtherMessage);
                message.ToastPush(30);
            }
        }

        private async void NavigationStarting(WebView2 sender, CoreWebView2NavigationStartingEventArgs args)
        {
            Console.WriteLine(args.Uri);
            string[] uriSplit = args.Uri.Split('?');
            if (uriSplit[0].Equals(authUri))
                await GetToken(args.Uri);
        }
            
    }
}
