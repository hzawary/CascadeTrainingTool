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

namespace Standard
{
    using Microsoft.Win32.SafeHandles;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    // Some COM interfaces and Win32 structures are already declared in the framework.
    // Interesting ones to remember in System.Runtime.InteropServices.ComTypes are:
    using IStream = System.Runtime.InteropServices.ComTypes.IStream;

    /// <summary>
    /// GetDeviceCaps nIndex values.
    /// </summary>
    internal enum DeviceCap
    {
        /// <summary>Number of bits per pixel
        /// </summary>
        BITSPIXEL = 12,
        /// <summary>
        /// Number of planes
        /// </summary>
        PLANES = 14,
        /// <summary>
        /// Logical pixels inch in X
        /// </summary>
        LOGPIXELSX = 88,
        /// <summary>
        /// Logical pixels inch in Y
        /// </summary>
        LOGPIXELSY = 90,
    }

    /// <summary>
    /// GDI+ Status codes
    /// </summary>
    internal enum Status
    {
        Ok = 0,
        GenericError = 1,
        InvalidParameter = 2,
        OutOfMemory = 3,
        ObjectBusy = 4,
        InsufficientBuffer = 5,
        NotImplemented = 6,
        Win32Error = 7,
        WrongState = 8,
        Aborted = 9,
        FileNotFound = 10,
        ValueOverflow = 11,
        AccessDenied = 12,
        UnknownImageFormat = 13,
        FontFamilyNotFound = 14,
        FontStyleNotFound = 15,
        NotTrueTypeFont = 16,
        UnsupportedGdiplusVersion = 17,
        GdiplusNotInitialized = 18,
        PropertyNotFound = 19,
        PropertyNotSupported = 20,
        ProfileNotFound = 21,
    }

    internal sealed class SafeDC : SafeHandleZeroOrMinusOneIsInvalid
    {
        private static class NativeMethods
        {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            [DllImport("user32.dll")]
            public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            [DllImport("user32.dll")]
            public static extern SafeDC GetDC(IntPtr hwnd);

            // Weird legacy function, documentation is unclear about how to use it...
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
            public static extern SafeDC CreateDC([MarshalAs(UnmanagedType.LPWStr)] string lpszDriver, [MarshalAs(UnmanagedType.LPWStr)] string lpszDevice, IntPtr lpszOutput, IntPtr lpInitData);

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            [DllImport("gdi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern SafeDC CreateCompatibleDC(IntPtr hdc);

            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            [DllImport("gdi32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DeleteDC(IntPtr hdc);
        }

        private IntPtr? _hwnd;
        private bool _created;

        public IntPtr Hwnd
        {
            set
            {
                Assert.NullableIsNull(_hwnd);
                _hwnd = value;
            }
        }

        private SafeDC() : base(true) { }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        protected override bool ReleaseHandle()
        {
            if (_created)
            {
                return NativeMethods.DeleteDC(handle);
            }

            if (!_hwnd.HasValue || _hwnd.Value == IntPtr.Zero)
            {
                return true;
            }

            return NativeMethods.ReleaseDC(_hwnd.Value, handle) == 1;
        }

        [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes"), SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static SafeDC CreateDC(string deviceName)
        {
            SafeDC dc = null;
            try
            {
                // Should this really be on the driver parameter?
                dc = NativeMethods.CreateDC(deviceName, null, IntPtr.Zero, IntPtr.Zero);
            }
            finally
            {
                if (dc != null)
                {
                    dc._created = true;
                }
            }

            if (dc.IsInvalid)
            {
                dc.Dispose();
                throw new SystemException("Unable to create a device context from the specified device information.");
            }

            return dc;
        }

        [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes"), SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static SafeDC CreateCompatibleDC(SafeDC hdc)
        {
            SafeDC dc = null;
            try
            {
                IntPtr hPtr = IntPtr.Zero;
                if (hdc != null)
                {
                    hPtr = hdc.handle;
                }
                dc = NativeMethods.CreateCompatibleDC(hPtr);
                if (dc == null)
                {
                    HRESULT.ThrowLastError();
                }
            }
            finally
            {
                if (dc != null)
                {
                    dc._created = true;
                }
            }

            if (dc.IsInvalid)
            {
                dc.Dispose();
                throw new SystemException("Unable to create a device context from the specified device information.");
            }

            return dc;
        }

        public static SafeDC GetDC(IntPtr hwnd)
        {
            SafeDC dc = null;
            try
            {
                dc = NativeMethods.GetDC(hwnd);
            }
            finally
            {
                if (dc != null)
                {
                    dc.Hwnd = hwnd;
                }
            }

            if (dc.IsInvalid)
            {
                // GetDC does not set the last error...
                HRESULT.E_FAIL.ThrowIfFailed();
            }

            return dc;
        }

        public static SafeDC GetDesktop()
        {
            return GetDC(IntPtr.Zero);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static SafeDC WrapDC(IntPtr hdc)
        {
            // This won't actually get released by the class, but it allows an IntPtr to be converted for signatures.
            return new SafeDC
            {
                handle = hdc,
                _created = false,
                _hwnd = IntPtr.Zero,
            };
        }
    }

    internal static class NativeMethods
    {
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(SafeDC hdc, DeviceCap nIndex);

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DllImport("gdiplus.dll")]
        public static extern Status GdipCreateBitmapFromStream(IStream stream, out IntPtr bitmap);

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DllImport("gdiplus.dll")]
        public static extern Status GdipCreateHICONFromBitmap(IntPtr bitmap, out IntPtr hbmReturn);

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [DllImport("gdiplus.dll")]
        public static extern Status GdipDisposeImage(IntPtr image);
    }
}
