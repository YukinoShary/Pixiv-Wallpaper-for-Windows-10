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
using Pixiv_Wallpaper_for_Windows_10.Model;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Pixiv_Wallpaper_for_Windows_10.ViewModel;

namespace Pixiv_Wallpaper_for_Windows_10
{
    /// <summary>
    /// 图片预览界面
    /// @ democyann
    /// </summary>
    public sealed partial class ShowPage : Page
    {
        private DownloadManager download = new DownloadManager();
        private Conf c;
        private ImageInfo img;
        private StorageFile file;
        private ImageShowViewModel viewModel;
        //TODO:设计新的ShowPage UI

        public ShowPage()
        {
            this.InitializeComponent();
            downloadProgress.Visibility = Visibility.Collapsed;
            SetImage();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        { 
            if(e.Parameter != null)
            {
                viewModel = e.Parameter as ImageShowViewModel;
            }
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
                title.Text = img.title;
                userName.Text = img.userName;
                viewCount.Text = (img.viewCount + MainPage.loader.GetString("ReviewTimes"));
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
