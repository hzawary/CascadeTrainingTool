using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;
using Standard;
using Size = System.Windows.Size;

namespace WpfApp_PositiveBuilder_Demo
{
    public class ImageManager
    {
        public class ImageDescriptor
        {
            public BitmapSource BitmapSource { get; internal set; }
            public string CodecName { get; private set; }
            public string ImagePath { get; private set; }

            public ImageDescriptor() : this(null, null, null) { }
            public ImageDescriptor(BitmapSource bitmapSource, string codecName, string imagePath)
            {
                BitmapSource = bitmapSource;
                CodecName = codecName;
                ImagePath = imagePath;
            }
        }

        public static List<ImageDescriptor> LoadImages(Uri uri, SearchOption searchOption)
        {

            var folderPath = uri.OriginalString;

            var fullNames = new string[] { };

            if (!folderPath.Equals(string.Empty))
                fullNames = Directory.GetFiles(folderPath, "*.*", searchOption);

            var list = new List<ImageDescriptor>();

            for (var i = 0; i < fullNames.Length; i++)
            {
                var filePath = fullNames[i];

                var imgInfo = DecodeBitmapImage(ref filePath, 128);

                if (imgInfo != null)
                    list.Add(imgInfo);
            }

            GC.Collect();

            return list;
        }

        public static ImageDescriptor DecodeBitmapImage(ref string filePath, int decodePixelWidth)
        {
            ImageDescriptor result = null;

            try
            {
                Bitmap bmp;

                using (var fileStream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    10240,
                    FileOptions.SequentialScan))
                {

                    var bf = BitmapFrame.Create(
                        fileStream,
                        BitmapCreateOptions.DelayCreation,
                        BitmapCacheOption.OnLoad);

                    result = new ImageDescriptor(null, bf.Decoder.CodecInfo.FriendlyName, filePath);

                    var icon = Utility.GenerateHICON(
                        bf, new Size(decodePixelWidth, Convert.ToSingle(bf.PixelHeight) / bf.PixelWidth * decodePixelWidth));

                    bmp = Util.BitmapSourceToBitmap(icon);

                    bf = null;
                    icon = null;
                }

                if (bmp != null)
                {
                    result.BitmapSource = Util.BitmapToBitmapImage(ref bmp, BitmapCacheOption.OnLoad, BitmapCreateOptions.DelayCreation);

                    bmp.Dispose();
                }
            }
            catch (Exception excptn)
            {
                while (excptn != null)
                {
                    Debug.WriteLine(excptn.Message);
                    excptn = excptn.InnerException;
                }
            }

            return result;
        }
    }
}
