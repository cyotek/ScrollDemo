using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Derived from https://www.codeproject.com/articles/1042516/custom-controls-in-win-api-scrolling

namespace Cyotek.Windows.Forms
{
  internal static class WheelHelper
  {
    #region Private Fields

#pragma warning disable IDE1006 // Naming Styles

    private const int SPI_GETWHEELSCROLLCHARS = 0x006C;

    private const int SPI_GETWHEELSCROLLLINES = 0x0068;

    private const int WHEEL_DELTA = 120;

    private const int WHEEL_PAGESCROLL = int.MaxValue;

#pragma warning restore IDE1006 // Naming Styles

    private static readonly int[] _accumulator = new int[2];

    private static readonly uint[] _lastActivity = new uint[2];

    private static readonly object _lock = new object();

    private static IntPtr _hwndCurrent = IntPtr.Zero;

    #endregion Private Fields

    #region Public Methods

    // HWND we accumulate the delta for.
    // The accumulated value (vert. and horiz.).
    public static int WheelScrollLines(IntPtr hwnd, int iDelta, int nPage, bool isVertical)
    {
      // We accumulate the wheel_delta until there is enough to scroll for
      // at least a single line. This improves the feel for strange values
      // of SPI_GETWHEELSCROLLLINES and for some mouses.

      int scrollSysParam;
      int linesPerWheelDelta;   // Scrolling speed (how much to scroll per WHEEL_DELTA).
      int lines;                 // How much to scroll for currently accumulated value.
      int dirIndex = isVertical ? 0 : 1;  // The index into iAccumulator[].
      uint now;

      now = GetTickCount();

      linesPerWheelDelta = 0;

      // Even when nPage is below one line, we still want to scroll at least a little.
      if (nPage < 1)
      {
        nPage = 1;
      }

      // Ask the system for scrolling speed.
      scrollSysParam = isVertical ? SPI_GETWHEELSCROLLLINES : SPI_GETWHEELSCROLLCHARS;

      if (!SystemParametersInfo(scrollSysParam, 0, ref linesPerWheelDelta, 0))
      {
        linesPerWheelDelta = 3;  // default when SystemParametersInfo() fails.
      }

      if (linesPerWheelDelta == WHEEL_PAGESCROLL)
      {
        // System tells to scroll over whole pages.
        linesPerWheelDelta = nPage;
      }

      if (linesPerWheelDelta > nPage)
      {
        // Slow down if page is too small. We don't want to scroll over multiple
        // pages at once.
        linesPerWheelDelta = nPage;
      }

      lock (_lock)
      {
        // In some cases, we do want to reset the accumulated value(s).
        if (hwnd != _hwndCurrent)
        {
          // Do not carry accumulated values between different HWNDs.
          _hwndCurrent = hwnd;
          _accumulator[0] = 0;
          _accumulator[1] = 0;
        }
        else if (now - _lastActivity[dirIndex] > SystemInformation.DoubleClickTime * 2)
        {
          // Reset the accumulator if there was a long time of wheel inactivity.
          _accumulator[dirIndex] = 0;
        }
        else if ((_accumulator[dirIndex] > 0) == (iDelta < 0))
        {
          // Reset the accumulator if scrolling direction has been reversed.
          _accumulator[dirIndex] = 0;
        }

        if (linesPerWheelDelta > 0)
        {
          // Accumulate the delta.
          _accumulator[dirIndex] += iDelta;

          // Compute the lines to scroll.
          lines = _accumulator[dirIndex] * linesPerWheelDelta / WHEEL_DELTA;

          // Decrease the accumulator for the consumed amount.
          // (Corresponds to the remainder of the integer divide above.)
          _accumulator[dirIndex] -= lines * WHEEL_DELTA / linesPerWheelDelta;
        }
        else
        {
          // uLinesPerWHEELDELTA == 0, i.e. likely configured to no scrolling
          // with mouse wheel.
          lines = 0;
          _accumulator[dirIndex] = 0;
        }

        _lastActivity[dirIndex] = now;
      }

      // Note that for vertical wheel, Windows provides the delta with opposite
      // sign. Hence the minus.
      return isVertical ? -lines : lines;
    }

    #endregion Public Methods

    #region Private Methods

    [DllImport("kernel32.dll")]
    private static extern uint GetTickCount();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(int uiAction, uint uiParam, ref int pvParam, int fWinIni);

    #endregion Private Methods
  }
}