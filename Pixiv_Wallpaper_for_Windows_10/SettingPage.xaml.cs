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
using Pixiv_Wallpaper_for_Windows_10.Model;
using Pixiv_Wallpaper_for_Windows_10.Util;
using System.Collections.ObjectModel;
using Windows.System;
using System.Threading.Tasks;
using Windows.Storage.Search;
using Windows.Storage.FileProperties;
using Windows.ApplicationModel.Resources;
using Windows.Security.Credentials;

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
        private ResourceLoader loader = ResourceLoader.GetForCurrentView("Resources");


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
            lock_switch.IsOn = c.lockscr;

            var vp = c.ActPswText;
            if (vp.Item1.Equals("null"))
                textbox1.Text = "";
            else
                textbox1.Text = vp.Item1;
            if (vp.Item2.Equals("null"))
                passwordbox1.Password = "";
            else
                passwordbox1.Password = vp.Item2;

            switch (c.mode)
            {
                case "Bookmark":
                    radiobutton1.IsChecked = true;
                    break;
                case "FollowIllust":
                    radiobutton2.IsChecked = true;
                    break;
                case "Recommendation":
                    radiobutton3.IsChecked = true;
                    break;
                default:
                    radiobutton1.IsChecked = true;
                    break;
            }
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

        private void radiobutton2_Checked(object sender, RoutedEventArgs e)
        {
            c.mode = "FollowIllust";
        }

        private void radiobutton3_Checked(object sender, RoutedEventArgs e)
        {
            textbox1.IsEnabled = true;
            passwordbox1.IsEnabled = true;
            c.mode = "Recommendation";
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
            if(!(textbox1.Text.Equals(c.ActPswText.Item1)&&passwordbox1.Password.Equals(c.ActPswText.Item2)))
            {
                string str1, str2;
                if (textbox1.Text == null)
                    str1 = "";
                else
                    str1 = textbox1.Text;
                if (passwordbox1.Password == null)
                    str2 = "";
                else
                    str2 = passwordbox1.Password;
                c.ActPswText = (str1, str2);
            }
        }

        private void passwordbox1_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!(textbox1.Text.Equals(c.ActPswText.Item1) && passwordbox1.Password.Equals(c.ActPswText.Item2)))
            {
                string str1, str2;
                if (textbox1.Text == null)
                    str1 = "";
                else
                    str1 = textbox1.Text;
                if (passwordbox1.Password == null)
                    str2 = "";
                else
                    str2 = passwordbox1.Password;
                c.ActPswText = (str1, str2);
            }
        }

        private void combox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            c.time = (int)combox1.SelectedValue;
        }

        private void radiobutton1_Checked(object sender, RoutedEventArgs e)
        {
            c.mode = "Bookmark";
        }

        private void combox2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            c.backgroundMode = combox2.SelectedValue.ToString();
        }

        private void lock_switch_Toggled(object sender, RoutedEventArgs e)
        {
            c.lockscr = lock_switch.IsOn;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string str1, str2;
            if (textbox1.Text == null || textbox1.Text.Equals(""))
                str1 = "null";
            else
                str1 = textbox1.Text;
            if (passwordbox1.Password == null || passwordbox1.Password.Equals(""))
                str2 = "null";
            else
                str2 = passwordbox1.Password;
            c.ActPswText = (str1, str2);
        }
    }
}
