using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PixivCS;

namespace Pixiv_Wallpaper_for_Windows_10.Util
{
    class PixivCSBaseModify : PixivBaseAPI
    {
        public PixivCSBaseModify()
        {
            TargetIPs["oauth.secure.pixiv.net"] = "210.140.131.199";
            TargetIPs["www.pixiv.net"] = "210.140.131.199";
            TargetIPs["app-api.pixiv.net"] = "210.140.131.199";

            TargetSubjects.Add("210.140.131.199", "CN=*.pixiv.net, O=pixiv Inc., OU=Development department, L=Shibuya-ku, S=Tokyo, C=JP");
            TargetSNs.Add("210.140.131.199", "346B03F05A00DD2FFAE58853");
            TargetTPs.Add("210.140.131.199", "07954CC4735FA33B629899E1DC2591500B090FB5");
        }
        
    }
}
