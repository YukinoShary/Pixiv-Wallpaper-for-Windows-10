using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using Pixiv_Wallpaper_for_Windows_10.Util;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace Pixiv_Wallpaper_for_Windows_10
{
    /// <summary>
    /// 图片预览界面
    /// @ democyann
    /// </summary>
    public sealed partial class ShowPage : Page
    {

        private Conf c;
        private ImageInfo img;
        private StorageFile file;
        private static ResourceLoader loader = ResourceLoader.GetForCurrentView("Resources");

        public ShowPage()
        {
            this.InitializeComponent();
            SetImage();
        }

        private async Task SetImage()
        {
            c = new Conf();
            if (c.lastImg == null)
            {
                file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/SplashScreen.scale-200.png"));
            }
            else
            {
                img = c.lastImg;
                file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/" + c.lastImg.imgId + '.' + img.format));
                
                textBlock1.Text = img.title;
                textBlock2.Text = img.userName;
                textBlock3.Text = (img.viewCount + loader.GetString("ReviewTimes"));
            }
            using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read))
            {
                BitmapImage bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(fileStream);
                show_img.Source = bitmap;
            }
        }
        
    }
}
