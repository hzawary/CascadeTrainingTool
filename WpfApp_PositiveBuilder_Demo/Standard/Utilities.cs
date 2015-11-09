/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

/**************************************************************************\
    Copyright Microsoft Corporation. All Rights Reserved.
\**************************************************************************/

// This file contains general utilities to aid in development.
// Classes here generally shouldn't be exposed publicly since
// they're not particular to any library functionality.
// Because the classes here are internal, it's likely this file
// might be included in multiple assemblies.
namespace Standard
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    internal static class Utility
    {
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static BitmapFrame GenerateHICON(ImageSource image, Size dimensions)
        {
            if (image == null)
            {
                //return IntPtr.Zero;
                return null;
            }

            // If we're getting this from a ".ico" resource, then it comes through as a BitmapFrame.
            // We can use leverage this as a shortcut to get the right 16x16 representation
            // because DrawImage doesn't do that for us.
            var bf = image as BitmapFrame;

            if (bf != null &&
                bf.Decoder.CodecInfo.FriendlyName.Contains("ICO Decoder"))
            {
                bf = GetBestMatch(bf.Decoder.Frames, (int)dimensions.Width, (int)dimensions.Height);
            }
            else
            {
                // Constrain the dimensions based on the aspect ratio.
                var drawingDimensions = new Rect(0, 0, dimensions.Width, dimensions.Height);

                // There's no reason to assume that the requested image dimensions are square.
                var renderRatio = dimensions.Width / dimensions.Height;
                var aspectRatio = image.Width / image.Height;

                // If it's smaller than the requested size, then place it in the middle and pad the image.
                if (image.Width <= dimensions.Width && image.Height <= dimensions.Height)
                {
                    drawingDimensions = new Rect((dimensions.Width - image.Width) / 2, (dimensions.Height - image.Height) / 2, image.Width, image.Height);
                }
                else if (renderRatio > aspectRatio)
                {
                    var scaledRenderWidth = (image.Width / image.Height) * dimensions.Width;
                    drawingDimensions = new Rect((dimensions.Width - scaledRenderWidth) / 2, 0, scaledRenderWidth, dimensions.Height);
                }
                else if (renderRatio < aspectRatio)
                {
                    var scaledRenderHeight = (image.Height / image.Width) * dimensions.Height;
                    drawingDimensions = new Rect(0, (dimensions.Height - scaledRenderHeight) / 2, dimensions.Width, scaledRenderHeight);
                }

                var dv = new DrawingVisual();
                var dc = dv.RenderOpen();
                dc.DrawImage(image, drawingDimensions);
                dc.Close();

                var bmp = new RenderTargetBitmap((int)dimensions.Width, (int)dimensions.Height, 96, 96, PixelFormats.Pbgra32);
                bmp.Render(dv);
                bf = BitmapFrame.Create(bmp);
            }

            return bf;
        }

        public static BitmapFrame GetBestMatch(IList<BitmapFrame> frames, int width, int height)
        {
            return _GetBestMatch(frames, _GetBitDepth(), width, height);
        }

        private static int _MatchImage(BitmapFrame frame, int bitDepth, int width, int height, int bpp)
        {
            var score = 2 * _WeightedAbs(bpp, bitDepth, false) +
                    _WeightedAbs(frame.PixelWidth, width, true) +
                    _WeightedAbs(frame.PixelHeight, height, true);

            return score;
        }

        private static int _WeightedAbs(int valueHave, int valueWant, bool fPunish)
        {
            var diff = (valueHave - valueWant);

            if (diff < 0)
            {
                diff = (fPunish ? -2 : -1) * diff;
            }

            return diff;
        }

        /// From a list of BitmapFrames find the one that best matches the requested dimensions.
        /// The methods used here are copied from Win32 sources.  We want to be consistent with
        /// system behaviors.
        private static BitmapFrame _GetBestMatch(IList<BitmapFrame> frames, int bitDepth, int width, int height)
        {
            var bestScore = int.MaxValue;
            var bestBpp = 0;
            var bestIndex = 0;

            var isBitmapIconDecoder = frames[0].Decoder is IconBitmapDecoder;

            for (var i = 0; i < frames.Count && bestScore != 0; ++i)
            {
                var currentIconBitDepth = isBitmapIconDecoder ? frames[i].Thumbnail.Format.BitsPerPixel : frames[i].Format.BitsPerPixel;

                if (currentIconBitDepth == 0)
                {
                    currentIconBitDepth = 8;
                }

                var score = _MatchImage(frames[i], bitDepth, width, height, currentIconBitDepth);
                if (score < bestScore)
                {
                    bestIndex = i;
                    bestBpp = currentIconBitDepth;
                    bestScore = score;
                }
                else if (score == bestScore && bestBpp < currentIconBitDepth)
                {
                    bestIndex = i;
                    bestBpp = currentIconBitDepth;
                }
            }

            return frames[bestIndex];
        }

        // This can be cached.  It's not going to change under reasonable circumstances.
        private static int _bitDepth; // = 0;
        private static int _GetBitDepth()
        {
            if (_bitDepth != 0) return _bitDepth;

            using (var dc = SafeDC.GetDesktop())
            {
                _bitDepth = NativeMethods.GetDeviceCaps(dc, DeviceCap.BITSPIXEL) * NativeMethods.GetDeviceCaps(dc, DeviceCap.PLANES);
            }
            return _bitDepth;
        }

        /// <summary>GDI+'s DisposeImage</summary>
        /// <param name="gdipImage"></param>
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void SafeDisposeImage(ref IntPtr gdipImage)
        {
            var p = gdipImage;
            gdipImage = IntPtr.Zero;
            if (IntPtr.Zero != p)
            {
                NativeMethods.GdipDisposeImage(p);
            }
        }
    }
}
