using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.QueryStringDotNET;
using Windows.UI.Notifications;
using Windows.ApplicationModel.Resources;

namespace Pixiv_Wallpaper_for_Windows_10.Util
{
    class ToastManagement
    {
        private string title { get; set; }
        private string content { get; set; }
        private string image { get; set; }
        private ToastMode toastMode { get; set; }
        private readonly string logo = "ms-appdata:///Square44x44Logo.scale-200.png";
        private ToastVisual visual;
        private ToastActionsCustom actions;
        private ToastContent toastContent;
        public enum ToastMode
        {
            BatterySetting,
            WallpaperUpdate,
            OtherMessage
        };
        private static ResourceLoader loader = ResourceLoader.GetForCurrentView("Resources");

        public ToastManagement(string title,string content,ToastMode toastMode,string image = null)
        {
            this.title = title;
            this.content = content;
            this.image = image;
            this.toastMode = toastMode;
        }
        /// <summary>
        /// 推送本地Toast通知
        /// </summary>
        /// <param name="minutes">toast消息过期时间(分钟)</param>
        public void ToastPush(int minutes)
        {
            // Construct the visuals of the toast
            if(image != null)
            {
                visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = title,
                                HintMaxLines = 1
                            },
                            new AdaptiveText()
                            {
                                Text = content,
                                HintMaxLines = 2
                            },
                            new AdaptiveImage()
                            {
                                Source = image
                            }
                        },
                        AppLogoOverride = new ToastGenericAppLogo()
                        {
                            Source = logo,
                            HintCrop = ToastGenericAppLogoCrop.Circle
                        }
                    }
                };
            }
            else
            {
                visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = title
                            },
                            new AdaptiveText()
                            {
                                Text = content
                            }
                        },
                        AppLogoOverride = new ToastGenericAppLogo()
                        {
                            Source = logo,
                            HintCrop = ToastGenericAppLogoCrop.Circle
                        }
                    }
                };
            }
            actions = new ToastActionsCustom();
            switch (toastMode)
            {
                case ToastMode.BatterySetting:
                    actions.Buttons.Add(new ToastButton(loader.GetString("BatterySetting"), new QueryString() { "action", "BatterySetting" }.ToString())
                    {
                        ActivationType = ToastActivationType.Protocol
                    });
                    break;
                case ToastMode.WallpaperUpdate:
                    actions.Buttons.Add(new ToastButton(loader.GetString("Next/Text"), new QueryString() { "action", "NextIllust" }.ToString())
                    {
                        ActivationType = ToastActivationType.Foreground
                    });
                    break;
                case ToastMode.OtherMessage:
                    actions = null;
                    break;
            }
            toastContent = new ToastContent();
            toastContent.Visual = visual;
            if(actions != null)
            {
                toastContent.Actions = actions;
            }
            var toast = new ToastNotification(toastContent.GetXml());
            toast.ExpirationTime = DateTime.Now.AddMinutes(minutes);
            ToastNotificationManager.CreateToastNotifier().Show(toast);
        }

    }
}
