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

namespace Pixiv_Wallpaper_for_Windows_10.ViewModel
{
    class ImageShowViewModel : INotifyPropertyChanged
    {
        public string Title { get; set; }
        public int ViewCount { get; set; }
        public string UserName { get; set; }
        public int Height { get; set; }
        public int Width { get; set; }
        public BitmapImage ImageSource { get; set; }
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
        public async Task SetItems(ImageInfo image)
        {
            BitmapImage bitmap = new BitmapImage();
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
