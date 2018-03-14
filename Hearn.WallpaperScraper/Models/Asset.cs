using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Hearn.WallpaperScraper.Models
{
    class Asset
    {

        public enum AssetTypes
        {
            Unknown,
            PNG,
            JPG_JFIF,
            JPG_EXIF
        }

        public string Path { get; set; }

        public AssetTypes AssetType { get; set; }

        public BitmapImage Image { get; set; }

        public string Dimensions { get; set; }

        public int PreviewWidth { get; set; }

        public DateTime CreatedDate { get; set; }

    }
}
