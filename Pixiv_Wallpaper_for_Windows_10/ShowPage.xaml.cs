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
    /// </summary>
    public sealed partial class ShowPage : Page
    {
        private ImageShowViewModel viewModel;

        public ShowPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        { 
            if(e.Parameter != null)
            {
                viewModel = e.Parameter as ImageShowViewModel;
            }
        }
        
    }
}
