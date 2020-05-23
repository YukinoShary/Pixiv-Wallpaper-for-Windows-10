# Pixiv Wallpaper for Windows 10
[<img src='https://assets.windowsphone.com/85864462-9c82-451e-9355-a3d5f874397a/English_get-it-from-MS_InvariantCulture_Default.png' alt='English badge' width=284 height=104/>](https://www.microsoft.com/zh-cn/p/pixiv-wallpaper-for-windows-10/9n71rkg8kcvc?activetab=pivot:overviewtab)

抓取pixiv.net的图片并设置为Windows 10桌面壁纸的UWP APP，需要在Windows 10 1903以上的版本系统中运行。

依赖部分NuGet包，没有多语种支持，目前只有简体中文一种语言。

有top50与"猜你喜欢"两种模式，"猜你喜欢"对应网页pixiv.net/discovery 内容，需要登录pixiv账号。

有两种登录模式供选择，一种采用WebView登录，支持外部账号关联登录(如weibo、Twitter账号登录)，该模式下均使用pixiv web接口进行http通信;
PixivCS登录模式采用[PixivCS](https://github.com/tobiichiamane/pixivcs/blob/master/PixivAppAPI.cs/ "PixivCS") API，内部大部分使用ios客户端api进行通信。PixivCS模式的代码参考了[pixivUWP](https://github.com/tobiichiamane/pixivfs-uwp/ "pixiv-uwp")项目。十分感谢[鱼姐姐](https://github.com/tobiichiamane)在技术上的指导与帮助~

## UWP使用代理
您需要自己准备代理服务器与代理软件，通过全局代理或者PAC代理+[Loopback](https://sspai.com/post/41137 "UWP loopback")的方式解决访问pixiv.net的问题。

## 关于原始项目
初代版本的是由[democyann](https://github.com/democyann)与我共同开发的。(~~实际上我只是一个只能写UI和部分简单逻辑还要被琉璃姐姐大改逻辑的菜鸡~~)。democyann在2017年之后因为个人原因不再管理此项目，后续开发与维护都由我个人在分支relife完成。在与democyann进行邮件沟通后，她建议我建立自己的代码仓库继续维护此项目。因此今后的Pixiv Wallpaper for Windows 10更新与维护将在此仓库完成，Microsoft Store上面发布的版本也会与此仓库的版本保持一致。

此处为原项目[Pixiv_Wallpaper_for_Win10](https://github.com/democyann/Pixiv_Wallpaper_for_Win10)，已将分支relife与master合并，代码不会再更新。
