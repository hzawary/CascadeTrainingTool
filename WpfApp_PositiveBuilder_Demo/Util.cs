using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MessageBox = System.Windows.MessageBox;
using Point = System.Drawing.Point;
using Rectangle = System.Drawing.Rectangle;

namespace WpfApp_PositiveBuilder_Demo
{
    public static class Util
    {
        public static Bitmap BitmapSourceToBitmap(ref BitmapImage bitmapImage)
        {
            using (var outStream = new MemoryStream())
            {
                var enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                var bitmap = new Bitmap(outStream);

                return bitmap.Clone() as Bitmap;
            }
        }
        public static Bitmap BitmapSourceToBitmap(BitmapSource source)
        {
            var bmp = new Bitmap(
                source.PixelWidth,
                source.PixelHeight,
                PixelFormat.Format32bppPArgb);

            var data = bmp.LockBits(
                new Rectangle(Point.Empty, bmp.Size),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppPArgb);

            source.CopyPixels(
                Int32Rect.Empty,
                data.Scan0,
                data.Height * data.Stride,
                data.Stride);

            bmp.UnlockBits(data);

            return bmp;
        }

        public static BitmapFrame BitmapToBitmapFrame(
            ref Bitmap bitmap,
            BitmapCacheOption bmpCacheOption,
            BitmapCreateOptions bmpCreateOptions)
        {
            BitmapFrame bf = null;

            try
            {
                using (var ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Bmp);
                    ms.Seek(0, SeekOrigin.Begin);

                    bf = BitmapFrame.Create(new MemoryStream(ms.ToArray()), bmpCreateOptions, bmpCacheOption);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "BitmapToBitmapFrame");
                Debug.WriteLine(ex.Message);
            }

            return bf;
        }

        public static BitmapImage BitmapToBitmapImage(
            ref Bitmap bitmap,
            BitmapCacheOption bmpCacheOption,
            BitmapCreateOptions bmpCreateOptions)
        {
            BitmapImage bi = null;

            try
            {
                //using (var ms = new MemoryStream())
                //{
                var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);

                bi = new BitmapImage();
                bi.BeginInit();
                bi.CreateOptions = bmpCreateOptions;
                bi.CacheOption = bmpCacheOption;
                bi.StreamSource = ms;
                //bi.StreamSource = new MemoryStream(ms.ToArray());
                bi.EndInit();
                bi.Freeze();
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Util.BitmapToBitmapImage has failed.");
                Debug.WriteLine(ex.Message);
            }

            return bi;
        }

        public static BitmapImage LoadImage(string imagePath)
        {
            BitmapImage bi = null;

            try
            {
                using (var imageStream = new FileStream(
                    imagePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    10240,
                    FileOptions.SequentialScan))
                {
                    bi = new BitmapImage();
                    bi.BeginInit();
                    //bi.CreateOptions = BitmapCreateOptions.DelayCreation;
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    var buffer = new byte[imageStream.Length];
                    imageStream.Read(buffer, 0, buffer.Length);
                    bi.StreamSource = new MemoryStream(buffer);
                    bi.EndInit();
                    bi.Freeze();
                }
            }
            catch (Exception excptn)
            {
                MessageBox.Show(excptn.Message, "LoadImage");

                while (excptn != null)
                {
                    Debug.WriteLine(excptn.Message);
                    excptn = excptn.InnerException;
                }
            }

            return bi;
        }

        /// <summary>
        ///     Convert from any format to Format24bppRgb.
        /// </summary>
        /// <param name="imagePath">Image path.</param>
        /// <returns>Returns a bitmap in Format24bppRgb.</returns>
        public static Bitmap Get24BppRgb(string imagePath)
        {
            var bitmap = new Bitmap(imagePath);
            var bitmap24 = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);
            using (var gr = Graphics.FromImage(bitmap24))
            {
                gr.DrawImage(bitmap, new Rectangle(0, 0, bitmap24.Width, bitmap24.Height));
            }
            return bitmap24;
        }

        public static void TryBoundShapeToCanvas(ref Shape shape, Canvas canvas)
        {
            var x = Canvas.GetLeft(shape);
            var y = Canvas.GetTop(shape);

            if (x < 0)
            {
                Canvas.SetLeft(shape, 0);

                shape.Width += x;
            }
            if (y < 0)
            {
                Canvas.SetTop(shape, 0);

                shape.Height += y;
            }

            var deltaWidth = x + shape.Width - canvas.ActualWidth;
            var deltaHeight = y + shape.Height - canvas.ActualHeight;

            if (deltaWidth > 0)
                shape.Width -= deltaWidth;

            if (deltaHeight > 0)
                shape.Height -= deltaHeight;
        }

        public static Task StartAndWaitProcess(string filePath, string arguments)
        {
            var p = new Process
            {
                StartInfo = { FileName = filePath, Arguments = arguments },
                EnableRaisingEvents = true
            };

            var tcs = new TaskCompletionSource<bool>();

            p.Exited += (sender, e) =>
            {
                tcs.SetResult(true);
                p.Dispose();
            };

            Task.Factory.StartNew(() => p.Start());

            return tcs.Task;
        }
    }
}