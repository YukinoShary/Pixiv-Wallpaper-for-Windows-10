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
        public WebViewLogin()
        {
            this.InitializeComponent();
            lp = this;
            frame = Window.Current.Content as Frame;
            loader = ResourceLoader.GetForCurrentView("Resources");
            baseAPI = new PixivCS.PixivBaseAPI();
            baseAPI.ExperimentalConnection = true;
            baseAPI.ClientLog += ClientLogOutput;
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
                if (refreshToken != null && !refreshToken.Equals("ERROR"))
                {
                    res = await baseAPI.AuthAsync(refreshToken);
                    var currentUser = res.Response.User;
                    pixiv = new Pixiv(baseAPI, currentUser);
                    frame.Navigate(typeof(MainPage), new ValueTuple<Pixiv, Conf>(pixiv, conf));
                }
                else
                {
                    //没有解决WebView持续显示的问题
                    lp.webView.Source = baseAPI.GenerateWebViewUri();
                    lp.webView.Visibility = Visibility.Visible;
                }
            }
            catch (Exception e)
            {
                //refreshToken失效导致的登录失败
                Console.WriteLine(e.Message);
                lp.webView.Source = baseAPI.GenerateWebViewUri();
                lp.webView.Visibility = Visibility.Visible;
            }
        }

        public static async Task GetToken(string uri)
        {
            Console.WriteLine(uri);
            try
            {
                string[] uriSplit = uri.Split('=', '&');
                Console.WriteLine(uriSplit[1]);
                var res = await baseAPI.Code2Token(uriSplit[1]);
                var currentUser = res.Response.User;
                pixiv = new Pixiv(baseAPI, currentUser);
                conf.RefreshToken = baseAPI.RefreshToken;
                frame.Navigate(typeof(MainPage), new ValueTuple<Pixiv, Conf>(pixiv, conf));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                //未完成
                //ToastMessage message = new ToastMessage();
            }

        }

        private async Task ClientLogOutput(byte[] b)
        {
            StorageFile file = (StorageFile)await ApplicationData.Current.LocalFolder.TryGetItemAsync("log.txt")
                ?? await ApplicationData.Current.LocalFolder.CreateFileAsync("log.txt");

            using (Stream stream = await file.OpenStreamForWriteAsync())
            {
                using (StreamWriter sw = new StreamWriter(stream))
                {
                    await sw.WriteLineAsync("-----------------------------------------");
                    await sw.WriteLineAsync(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
                    using (MemoryStream ms = new MemoryStream())
                    {
                        await stream.WriteAsync(b, 0, b.Length);
                    }
                    await sw.WriteLineAsync("-----------------------------------------");
                }

            }
        }
    }
}
