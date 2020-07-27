using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Pixiv_Wallpaper_for_Windows_10.Util;
using System.Collections.ObjectModel;
using Windows.System;
using System.Threading.Tasks;
using Windows.Storage.Search;
using Windows.Storage.FileProperties;
using Windows.ApplicationModel.Resources;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace Pixiv_Wallpaper_for_Windows_10
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SettingPage : Page
    {
        private StorageFolder folder = ApplicationData.Current.LocalFolder;
        private Conf c = new Conf();
        private static ResourceLoader loader = ResourceLoader.GetForCurrentView("Resources");


        public SettingPage()
        {
            this.InitializeComponent();

            //下拉框初始化   多语言适配
            combox1.Items.Add(new KeyValuePair<string, int>(loader.GetString("15Mins"), 15));
            combox1.Items.Add(new KeyValuePair<string, int>(loader.GetString("30Mins"), 30));
            combox1.Items.Add(new KeyValuePair<string, int>(loader.GetString("60Mins"), 60));
            combox1.Items.Add(new KeyValuePair<string, int>(loader.GetString("120Mins"), 120));
            combox1.Items.Add(new KeyValuePair<string, int>(loader.GetString("180Mins"), 180));

            combox2.Items.Add(new KeyValuePair<string, string>(loader.GetString("ExtendedSession"), "ExtendedSession"));
            combox2.Items.Add(new KeyValuePair<string, string>(loader.GetString("BackgroundTask"), "BackgroundTask"));

            CalcutateCacheSize();
            combox1.SelectedValue = c.time;
            combox2.SelectedValue = c.backgroundMode;

            if(c.cookie!=null&&!"".Equals(c.cookie))
            {
                loginV1.Content = loader.GetString("PixivLoginV1LoggedIn");
            }
            else
            {
                loginV1.Content = loader.GetString("PixivLoginV1Not");
            }

            lock_switch.IsOn = c.lockscr;
            textbox1.Text = c.account;
            passwordbox1.Password = c.password;

            switch (c.mode)
            {
                case "Top_50":
                    radiobutton1.IsChecked = true;
                    break;
                case "You_Like_V1":
                    radiobutton2.IsChecked = true;
                    break;
                case "You_Like_V2":
                    radiobutton3.IsChecked = true;
                    break;
                default:
                    radiobutton1.IsChecked = true;
                    break;
            }
        }

        private void SetCookie(string str)
        {
            c.cookie = str;
            loginV1.Content = loader.GetString("PixivLoginV1LoggedIn");
        }

        private async void openFilePath_Click(object sender, RoutedEventArgs e)
        {
            var t = new FolderLauncherOptions();
            await Launcher.LaunchFolderAsync(folder, t);
        }

        private async void clearPicture_Click(object sender, RoutedEventArgs e)
        {
            foreach (StorageFile f in await folder.GetItemsAsync())
            {
                if(!f.Name.Equals(c.lastImg.imgId + '.' + c.lastImg.format))
                {
                    await f.DeleteAsync();
                }     
            }
            CalcutateCacheSize();
        }

        private void cleanToken_Click(object sender, RoutedEventArgs e)
        {
            c.token = null;
            c.cookie = null;
            loginV1.Content = loader.GetString("PixivLoginV1Not"); 
        }

        private async void loginV1_Click(object sender, RoutedEventArgs e)
        {
            WebViewLogin wvl = new WebViewLogin("https://accounts.pixiv.net/login", "https://www.pixiv.net/");
            wvl.ClearCookies();
            wvl.Method += SetCookie;
            await wvl.ShowWebView(1000, 800);
        }

        private void radiobutton2_Checked(object sender, RoutedEventArgs e)
        {
            loginV1.IsEnabled = true;
            c.mode = "You_Like_V1";
        }

        private void radiobutton3_Checked(object sender, RoutedEventArgs e)
        {
            textbox1.IsEnabled = true;
            passwordbox1.IsEnabled = true;
            c.mode = "You_Like_V2";
        }

        private void radiobutton2_Unchecked(object sender, RoutedEventArgs e)
        {
            loginV1.IsEnabled = false;
        }

        private void radiobutton3_Unchecked(object sender, RoutedEventArgs e)
        {
            textbox1.IsEnabled = false;
            passwordbox1.IsEnabled = false;
        }

        private async Task<long> GetFolderSizeAsync()
        {
            var getFileSizeTasks = from file
                                   in await folder.CreateFileQuery().GetFilesAsync()
                                   select file.GetBasicPropertiesAsync().AsTask();
            var fileSizes = await Task.WhenAll(getFileSizeTasks);
            return fileSizes.Sum(i => (long)i.Size);
        }

        private async Task CalcutateCacheSize()
        {
            long current = await GetFolderSizeAsync();
            decimal sizeInMB = new decimal(current) / new decimal(1048576);
            cacheSize.Text = decimal.Round(sizeInMB, 2).ToString() + "MB";
        }

        private void textbox1_LostFocus(object sender, RoutedEventArgs e)
        {
            c.account = textbox1.Text;
        }

        private void passwordbox1_LostFocus(object sender, RoutedEventArgs e)
        {
            c.password = passwordbox1.Password;
        }

        private void combox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            c.time = (int)combox1.SelectedValue;
        }

        private void radiobutton1_Checked(object sender, RoutedEventArgs e)
        {
            c.mode = "Top_50";
        }

        private void combox2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            c.backgroundMode = combox2.SelectedValue.ToString();
        }

        private void lock_switch_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            c.lockscr = lock_switch.IsOn;
        }
    }
}
