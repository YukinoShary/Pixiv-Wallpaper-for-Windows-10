# Pixiv Wallpaper for Windows 10

***由于pixiv更改了登录页面导致旧版WebView(EdgeHTML)无法进行登录，因此现在改为使用WebView2(Chromium)但未开发完成的WinUI3平台，同时下架了Microsoft Store中已经无法使用的旧版本应用。WinUI3 版本在此仓库的Release中获取***

## 基础功能

抓取pixiv.net的图片并设置为Windows 10桌面壁纸的UWP APP，需要在Windows 10 2104以上的版本系统中运行。

支持模式:  

>- Pixiv 书签
> 
>- 已关注用户的更新
> 
>- Pixiv 推荐
> 
>- Pixiv 排行榜

## UWP使用代理
您需要自己准备代理服务器与代理软件，通过代理软件的全局代理模式或者PAC代理模式并 [解除UWP应用Loopback限制](https://sspai.com/post/41137 "UWP loopback")的方式解决UWP应用通过代理连接pixiv.net的问题。从Microsoft Store获取的应用可以直接以管理员模式开启控制台面板输入：  

`checknetisolation loopbackexempt -a -n=63929Shary.PixivWallpaperforWindows10_hzhgmpxe3vqfr`

由于pixiv部分url并未被完全封锁，大部分的PAC文件中并没有写入这部分url，可能会出现代理开启的情况下图片加载速度还是很慢的情况。因此在PAC代理模式下，推荐手动将pixiv图床的url “i.pximg.net”与“pixivsketch.net”添加进pac列表中以加速图片下载速度。  

## 已知问题

WinUI3处于未开发完成的状态，开发过程中遇到了以下的已知问题

>- 无法使用Acrylic(毛玻璃)效果，应用整体锁定为黑底白字主题
>- UI控件多语言支持不完善，仅有部分控件可以显示其他两种语言
