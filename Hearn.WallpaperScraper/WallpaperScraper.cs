using Hearn.WallpaperScraper.Models;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Hearn.WallpaperScraper
{
    class WallpaperScraper : INotifyPropertyChanged
    {

        const uint JPG_JFIF = 0xffd8ffe0; //SOI = FF D8, APP0 = DD E0
        const uint JPG_EXIF = 0xffd8ffe1; //SOI = FF D8, APP1 = DD E1
        const uint PNG = 0x89504e47; //0x89 + PNG

        public ObservableCollection<Asset> Assets { get; set; }

        public Command SaveImageCommand { get; set; }

        private string _currentFile;

        public event PropertyChangedEventHandler PropertyChanged;

        public string CurrentFile
        {
            get
            {
                return _currentFile;
            }
            set
            {
                _currentFile = value;
                PropertyChanged(this, new PropertyChangedEventArgs("CurrentFile"));
            }
        }

        public WallpaperScraper()
        {

            Assets = new ObservableCollection<Asset>();

            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {

                    var appDataLocal = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    LoadAssets(appDataLocal);

                    var appDataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    LoadAssets(appDataRoaming);

                    var appDataCommon = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    LoadAssets(appDataCommon);

                    Application.Current.Dispatcher.Invoke(
                        () => CurrentFile = "Done",
                        DispatcherPriority.Normal
                    );

                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            });
            
            SaveImageCommand = new Command(a => SaveImage(a as Asset));

        }

        private void LoadAssets(string path)
        {

            //Recurse sub directories
            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    if (!File.GetAttributes(dir).HasFlag(FileAttributes.ReparsePoint)) //Skip symbolic links
                    {
                        LoadAssets(dir);
                    }
                }
            }
            catch
            {
            }

            //Parse Files
            try
            {
                foreach (var file in Directory.GetFiles(path))
                {

                    Application.Current.Dispatcher.Invoke(
                        () => CurrentFile = file,
                        DispatcherPriority.Normal
                    );

                    var asset = ParseLoadAsset(file);

                    if (asset.Image != null)
                    {
                        Application.Current.Dispatcher.Invoke(
                            () => Assets.Add(asset),
                            DispatcherPriority.Normal
                        );
                    }
                }
            }
            catch
            {
            }

        }

        private Asset ParseLoadAsset(string file)
        {

            var asset = new Asset();
            asset.Path = file;

            try
            {
                using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    var buffer = new Byte[4];
                    fileStream.Read(buffer, 0, 4);

                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(buffer);
                    }

                    var header = BitConverter.ToUInt32(buffer, 0);

                    switch (header)
                    {
                        case JPG_JFIF:
                            asset.AssetType = Asset.AssetTypes.JPG_JFIF;
                            break;

                        case JPG_EXIF:
                            asset.AssetType = Asset.AssetTypes.JPG_EXIF;
                            break;

                        case PNG:
                            asset.AssetType = Asset.AssetTypes.PNG;
                            break;

                        default:
                            break;

                    }

                    fileStream.Close();
                }

                if (asset.AssetType != Asset.AssetTypes.Unknown)
                {

                    var tempImage = new BitmapImage();
                    tempImage.BeginInit();
                    tempImage.UriSource = new Uri(file);
                    tempImage.EndInit();

                    if (tempImage.Width >= 1000 && tempImage.Height >= 1000)
                    {

                        asset.Dimensions = $"{tempImage.Width} x {tempImage.Height}";

                        asset.PreviewWidth = 180;
                        if (tempImage.Width > tempImage.Height)
                        {
                            asset.PreviewWidth = 320;
                        }

                        asset.Image = new BitmapImage();
                        asset.Image.BeginInit();
                        asset.Image.UriSource = new Uri(file);
                        asset.Image.DecodePixelWidth = asset.PreviewWidth;
                        asset.Image.EndInit();
                        asset.Image.Freeze(); //2 hours of my life lost to this line - Fixes must create dependencysource on same thread as the dependencyobject error

                        var fileInfo = new FileInfo(file);
                        asset.CreatedDate = fileInfo.CreationTime;

                    }
                }
            }
            catch
            {
                //Ignore
            }

            return asset;

        }

        private void SaveImage(Asset asset)
        {

            var extension = "";
            switch (asset.AssetType)
            {
                case Asset.AssetTypes.JPG_EXIF:
                case Asset.AssetTypes.JPG_JFIF:
                    extension = "jpg";
                    break;

                case Asset.AssetTypes.PNG:
                    extension = "png";
                    break;

            }

            if (!string.IsNullOrEmpty(extension))
            {
                var saveFileDialog = new SaveFileDialog();
                saveFileDialog.FileName = $"Wallpaper Scraper {asset.CreatedDate.ToString("yyyy-MM-dd")}.{extension}";
                saveFileDialog.DefaultExt = $".{extension}";
                saveFileDialog.Filter = $"{extension} images |*.{extension}";
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                if (saveFileDialog.ShowDialog() == true)
                {
                    File.Copy(asset.Path, saveFileDialog.FileName, true);
                    System.Diagnostics.Process.Start(saveFileDialog.FileName);
                }
            }

        }

    }
}
