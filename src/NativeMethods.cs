using System;
using System.Drawing;
using System.Runtime.InteropServices;

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

    [DllImport("user32.dll", SetLastError = false)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    public static extern IntPtr WindowFromPoint(Point point);

    #endregion Public Methods
  }
}