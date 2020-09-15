using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pixiv_Wallpaper_for_Windows_10.Model
{
    /// <summary>
    /// 图片信息实体类
    /// </summary>
    public class ImageInfo
    {
        /// <summary>
        /// 作品名称
        /// </summary>
        public string title { set; get; }
        /// <summary>
        /// 作品ID
        /// </summary>
        public string imgId { get; set; }
        /// <summary>
        /// 作品URL
        /// </summary>
        public string imgUrl { get; set; }
        /// <summary>
        /// 作者ID
        /// </summary>
        public string userId { get; set; }
        /// <summary>
        /// 作者名称
        /// </summary>
        public string userName { get; set; }
        /// <summary>
        /// 是否为R18作品
        /// </summary>
        public bool isR18 { get; set; }
        /// <summary>
        /// 浏览数
        /// </summary>
        public int viewCount { get; set; }

        /// <summary>
        /// 图片宽度
        /// </summary>
        public int width { get; set; }

        /// <summary>
        /// 图片高度
        /// </summary>
        public int height { get; set; }

        /// <summary>
        /// 图片宽高比
        /// </summary>
        public double WHratio
        {
            get
            {
                if (width == 0 || height == 0)
                {
                    return 0.5;
                }
                else
                {
                    return width * 1.00 / height;
                }
            }
        }

        /// <summary>
        /// 图片文件格式
        /// </summary>
        public string format { get; set; }
    }
}
