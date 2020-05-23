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
using System.Diagnostics;
using Windows.Storage;
using Pixiv_Wallpaper_for_Windows_10.Util;
using System.Collections;
using System.Threading;
using Windows.System.UserProfile;
using Windows.UI.Popups;
using Windows.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;
using Windows.ApplicationModel.ExtendedExecution;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.ApplicationModel.Background;

namespace Pixiv_Wallpaper_for_Windows_10
{
    /// <summary>
    /// 主界面
    /// @ democyann
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private DispatcherTimer timer;  //图片推送定时器
        private Conf c;
        private ImageInfo img;
        public static MainPage mp;
        private ExtendedExecutionSession session;
        private PixivTop50 top50;
        private PixivLike like;
        private string backgroundMode;

        public MainPage()
        {
            this.InitializeComponent();
            mp = this;
            c = new Conf();
            img = c.lastImg;
            session = null;
            backgroundMode = c.backgroundMode;

            //后台模式选择
            if(backgroundMode.Equals("BackgroundTask"))
            {
                RegistTask(); //注册后台任务以及时间触发器
            }
            else
            {
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMinutes(c.time);
                timer.Tick += Timer_Tick;
                timer.Start();
                BeginExtendedExecution(); //申请延迟挂起

                foreach (var i in BackgroundTaskRegistration.AllTasks.Values)
                {
                    if (i.Name.Equals("TimeBackgroundTrigger"))
                    {
                        i.Unregister(true);//将之前的时间触发器任务注销
                    }
                }
            }

            main.Navigate(typeof(ShowPage));
        }


        private async void Timer_Tick(object sender, object e)
        {
            SetWallpaper(await update());
        }
        /// <summary>
        /// 作品更新并显示
        /// </summary>
        public async Task<bool> update()
        {
            //timer.Stop();

            switch (c.mode)
            {
                case "Top_50":
                    if(top50 == null)
                    {
                        top50 = new PixivTop50();
                    }
                    img = await top50.SelectArtWork();         
                    break;
                case "You_Like_V1":
                    if(like == null)
                    {
                        like = new PixivLike();
                    }
                    img = await like.SelectArtWorkV1();
                    break;
                case "You_Like_V2":
                    if (like == null)
                    {
                        like = new PixivLike();
                    }
                    img = await like.SelectArtWorkV2(); //该API在UI线程被建立，不支持从子线程调用
                    break;
                default:
                    if (top50 == null)
                    {
                        top50 = new PixivTop50();
                    }
                    await Task.Run(async () => { img = await top50.SelectArtWork(); });
                    break;
            }

            if (img != null)
            {
                c.lastImg = img;
                main.Navigate(typeof(ShowPage));//图片展示页面更新

                if(backgroundMode.Equals("BackgroundTask"))
                {
                    RegistTask(); //重新申请后台计时触发器
                }
                else
                {
                    timer.Interval = TimeSpan.FromMinutes(c.time);
                    timer.Start();
                }
                return true;
            }
            else
                return false;
        }

        public async void SetWallpaper(bool done)
        {
            if(done)
            {
                var dialog = new MessageDialog("");
                if (!UserProfilePersonalizationSettings.IsSupported())
                {
                    string title = "您的设备不支持自动更换壁纸";
                    string content = " ";
                    ToastManagement tm = new ToastManagement(title, content, ToastManagement.OtherMessage);
                    tm.ToastPush(60);
                    return;
                }
                UserProfilePersonalizationSettings settings = UserProfilePersonalizationSettings.Current;
                StorageFile file = null;
                try
                {
                    file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appdata:///local/" + img.imgId + '.' + img.format));
                }
                catch (Exception)
                {
                    timer.Interval = TimeSpan.FromSeconds(2);
                    timer.Start();
                }

                if (c.lockscr)
                {
                    //更换锁屏
                    bool lockscr = await settings.TrySetLockScreenImageAsync(file);
                    if (!lockscr)
                    {
                        string title = "更换锁屏操作失败";
                        string content = " ";
                        ToastManagement tm = new ToastManagement(title, content, ToastManagement.OtherMessage);
                        tm.ToastPush(60);
                    }
                }
                //更换壁纸
                bool deskscr = await settings.TrySetWallpaperImageAsync(file);

                if (!deskscr)
                {
                    string title = "更换壁纸操作失败";
                    string content = " ";
                    ToastManagement tm = new ToastManagement(title, content, ToastManagement.OtherMessage);
                    tm.ToastPush(60);
                }
                else
                {
                    //推送Toast通知
                    string title = "成功更换壁纸";
                    string content = img.title + "\r\n"
                        + "id: " + img.imgId + "\r\n" 
                        + "作者: " + img.userName;
                    string image = file.Path;
                    ToastManagement tm = new ToastManagement(title, content, ToastManagement.WallpaperUpdate, image);
                    tm.ToastPush(10);
                }
            }
        }
        private void ClearExtendedExecution()
        {
            if (session != null)
            {
                session.Revoked -= SessionRevoked;
                session.Dispose();
                session = null;
            }
        }

        private async void BeginExtendedExecution()
        {
            // The previous Extended Execution must be closed before a new one can be requested.
            ClearExtendedExecution();

            var newSession = new ExtendedExecutionSession();
            newSession.Reason = ExtendedExecutionReason.Unspecified;
            newSession.Description = "Raising periodic toasts";
            newSession.Revoked += SessionRevoked;
            ExtendedExecutionResult result = await newSession.RequestExtensionAsync();

            switch(result)
            {
                case ExtendedExecutionResult.Allowed:
                    session = newSession;
                    break;
                default:
                case ExtendedExecutionResult.Denied:
                    newSession.Dispose();
                    //建立Toast通知
                    string title = "Pixiv Wallpaper for Windows 10被禁止扩展会话执行";
                    string content = "由于系统限制，应用程序无法在后台继续活动。请尝试更改电源设置允许应用后台";
                    ToastManagement tm = new ToastManagement(title, content, ToastManagement.BatterySetting);
                    tm.ToastPush(120);
                    break;
            }
        }

        private async void SessionRevoked(object sender, ExtendedExecutionRevokedEventArgs args)
        {
            //session被系统回收时记录原因，session被回收则无法保持后台运行
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                switch (args.Reason)
                {
                    case ExtendedExecutionRevokedReason.Resumed:
                        Debug.WriteLine("Extended execution revoked due to returning to foreground.");
                        break;
                    case ExtendedExecutionRevokedReason.SystemPolicy:
                        Debug.WriteLine("Extended execution revoked due to system policy.");
                        break;
                }
            });
        }

        private async void RegistTask()
        {
            // Otherwise request access
            var status = await BackgroundExecutionManager.RequestAccessAsync();
            if(status == BackgroundAccessStatus.DeniedBySystemPolicy||status == BackgroundAccessStatus.Unspecified)
            {
                string title = "应用后台活动被禁止";
                string content = "由于系统限制，应用程序无法在后台活动。" +
                        "若希望使此应用在后台活动，请尝试更改电源设置以及插入外部电源";
                ToastManagement tm = new ToastManagement(title, content, ToastManagement.BatterySetting);
                tm.ToastPush(120);
            }
            else
            {
                foreach (var i in BackgroundTaskRegistration.AllTasks.Values)
                {
                    if (i.Name.Equals("TimeBackgroundTrigger"))
                    {
                        i.Unregister(true);//将之前的时间触发器任务注销
                    }
                }
                //注册新的时间触发器
                BackgroundTaskBuilder timeBuilder = new BackgroundTaskBuilder();
                timeBuilder.Name = "TimeBackgroundTrigger";
                timeBuilder.SetTrigger(new TimeTrigger(Convert.ToUInt32(c.time), true));
                timeBuilder.IsNetworkRequested = true;
                timeBuilder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                BackgroundTaskRegistration task = timeBuilder.Register();
            } 
        }

        private void Button_Click(object sender, RoutedEventArgs e)     //导航视图开关
        {
            lis.IsPaneOpen = !lis.IsPaneOpen;
        }

        private void show_btn_Click(object sender, RoutedEventArgs e)   //展示页面按钮
        {
            main.Navigate(typeof(ShowPage));
        }

        private void setting_btn_Click(object sender, RoutedEventArgs e) //设置界面按钮
        {
            main.Navigate(typeof(SettingPage));
        }

        private void next_btn_Click(object sender, RoutedEventArgs e)    //下一张图
        {
            update();
        }

        private void visiturl_btn_Click(object sender, RoutedEventArgs e)       //访问p站
        {
            if(img!=null)
            {
                var uriPixiv = new Uri(@"https://www.pixiv.net/artworks/" + img.imgId);
                var visit = Windows.System.Launcher.LaunchUriAsync(uriPixiv);
            }
        }

        private void setWallpaper_btn_Click(object sender, RoutedEventArgs e)
        {
            SetWallpaper(true);
        }

        private async void refresh_btn_Click(object sender, RoutedEventArgs e)
        {
            switch(c.mode)
            {
                case "Top_50":
                    if(top50 == null)
                    {
                        top50 = new PixivTop50();
                    }
                    if(await top50.listUpdate(true))
                    {
                        string title = "Top50插画队列已更新";
                        string content = "插画队列的更新只是更新应用内存中的插画ID队列，你需要点击“下一张”按钮在预览页中查看更新的插画";
                        ToastManagement tm = new ToastManagement(title, content, ToastManagement.OtherMessage);
                        tm.ToastPush(120);
                    }
                    break;
                case "You_Like_V1":
                    if(like == null)
                    {
                        like = new PixivLike();
                    }
                    if(await like.ListUpdateV1(true))
                    {
                        string title = "“猜你喜欢”插画队列已更新";
                        string content = "插画队列的更新只是更新应用内存中的插画ID队列，你需要点击“下一张”按钮在预览页中查看更新的插画";
                        ToastManagement tm = new ToastManagement(title, content, ToastManagement.OtherMessage);
                        tm.ToastPush(120);
                    }
                    break;
                case "You_Like_V2":
                    if(like == null)
                    {
                        like = new PixivLike();
                    }
                    if(await like.ListUpdateV2(true))
                    {
                        string title = "“猜你喜欢”插画队列已更新";
                        string content = "插画队列的更新只是更新应用内存中的插画ID队列，你需要点击“下一张”按钮在预览页中查看更新的插画";
                        ToastManagement tm = new ToastManagement(title, content, ToastManagement.OtherMessage);
                        tm.ToastPush(120);
                    }
                    await like.ListUpdateV2(true);
                    break;
                default:
                    if(top50 == null)
                    {
                        top50 = new PixivTop50();
                    }
                    if(await top50.listUpdate(true))
                    {
                        string title = "Top50插画队列已更新";
                        string content = "插画队列的更新只是更新应用内存中的插画ID队列，你需要点击“下一张”按钮在预览页中查看更新的插画";
                        ToastManagement tm = new ToastManagement(title, content, ToastManagement.OtherMessage);
                        tm.ToastPush(120);
                    }
                    break;
            }
        }

    }
}
