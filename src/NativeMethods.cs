using System;
using System.Drawing;
using System.Runtime.InteropServices;

// Creating a custom single-axis scrolling control in WinForms
// https://www.cyotek.com/blog/creating-a-custom-single-axis-scrolling-control-in-winforms

// Copyright © 2020 Cyotek Ltd. All Rights Reserved.

// This work is licensed under the MIT License.
// See LICENSE.TXT for the full text

// Found this example useful?
// https://www.paypal.me/cyotek

namespace Cyotek.Windows.Forms
{
  internal static class NativeMethods
  {
    #region Public Fields

    public const int SPI_GETWHEELSCROLLCHARS = 0x006C;

    public const int SPI_GETWHEELSCROLLLINES = 0x0068;

    public const int WHEEL_DELTA = 120;

    public const int WHEEL_PAGESCROLL = int.MaxValue;

    public const int WM_MOUSEHWHEEL = 0x20e;

    public const int WM_MOUSEWHEEL = 0x20a;

    #endregion Public Fields

    #region Public Methods

    [DllImport("kernel32.dll")]
    public static extern uint GetTickCount();

    [DllImport("user32.dll", SetLastError = false)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SystemParametersInfo(int uiAction, uint uiParam, ref int pvParam, int fWinIni);

    [DllImport("user32.dll")]
    public static extern IntPtr WindowFromPoint(Point point);

    #endregion Public Methods
  }
}