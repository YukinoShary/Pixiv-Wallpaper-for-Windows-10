using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Pixiv_Wallpaper_for_Windows_10.Model;
using Pixiv_Wallpaper_for_Windows_10.Util;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;

namespace Pixiv_Wallpaper_for_Windows_10.ViewModel
{
    class ImageShowViewModel : INotifyPropertyChanged
    {
        private string title;
        public string Title
        {
            get => title;
            private set
            {
                title = value;
                OnPropertyChanged();
            }
        }

        private int viewCount;
        public int ViewCount 
        {
            get => viewCount;
            private set
            {
                viewCount = value;
                OnPropertyChanged();
            }
        }

        private string userName;
        public string UserName
        {
            get => userName;
            private set
            {
                userName = value;
                OnPropertyChanged();
            }
        }

        private int height;
        public int Height
        {
            get => height;
            private set
            {
                height = value;
                OnPropertyChanged();
            }
        }

        private int width;
        public int Width
        {
            get => width;
            private set
            {
                width = value;
                OnPropertyChanged();
            }
        }

        private BitmapImage imageSource;
        public BitmapImage ImageSource
        {
            get => imageSource;
            private set
            {
                imageSource = value;
                OnPropertyChanged();
            }
        }

        private int progress;
        public int Progress 
        { 
            get => progress;
            private set
            {
                progress = value;
                OnPropertyChanged();
            }
        }

        private Visibility loading;
        public Visibility Loading
        {
            get => loading;
            private set
            {
                loading = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 设定ViewModel的各项属性值，将于ShowPage中呈现
        /// </summary>
        /// <param name="image">设置给ViewModel的插画信息</param>
        /// <returns></returns>
        public async Task SetItems(ImageInfo image = null)
        {
            BitmapImage bitmap = new BitmapImage();
            if(image != null)
            {
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/" + image.imgId + '.' + image.format));
                using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read))
                {
                    await bitmap.SetSourceAsync(fileStream);
                    Title = image.title;
                    ViewCount = image.viewCount;
                    UserName = image.userName;
                    Height = image.height;
                    Width = image.width;
                    ImageSource = bitmap;
                    Progress = 100;
                    Loading = Visibility.Collapsed;
                }
            }
            else
            {
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/SplashScreen.scale-200.png"));
                using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read))
                {
                    await bitmap.SetSourceAsync(fileStream);
                    Loading = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// 获取图像
        /// </summary>
        /// <param name="image">需要下载图像数据的插画信息</param>
        /// <param name="c">应用存储的设置页配置数据</param>
        /// <returns></returns>
        public async Task LoadImageAsync(ImageInfo image, Conf c)
        {
            bool result;
            Loading = Visibility.Visible;
            DownloadManager download = new DownloadManager();
            if(c.mode.Equals("You_Like_V2"))
            {
                //lamda表达式写匿名回调函数作为参数
                result = await download.DownloadVer2(image.imgUrl, image.imgId, image.format, async (loaded, length) => 
                {
                    await Task.Run(() =>
                    {
                        Progress = (int)(loaded * 100 / length);
                    });
                });
            }
            else
            {
                result = await download.DownloadVer1(image.imgUrl, image.imgId, image.format, c.cookie, async(loaded, length) => 
                {
                    await Task.Run(() =>
                    {
                        Progress = (int)(loaded * 100 / length);
                    });
                });
            }

            if (result)
            {
                await SetItems(image);
                c.lastImg = image;
            }
            //图片获取失败则显示lastImage
            else
            {
                await SetItems(c.lastImg);
            }
        }
    }
}
