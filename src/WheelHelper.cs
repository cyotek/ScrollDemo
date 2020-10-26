using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

// Derived from https://www.codeproject.com/articles/1042516/custom-controls-in-win-api-scrolling

namespace Cyotek.Windows.Forms
{
  internal static class WheelHelper
  {
    #region Private Fields

    private const int SPI_GETWHEELSCROLLCHARS = 0x006C;

    private const int SPI_GETWHEELSCROLLLINES = 0x0068;

    private const int WHEEL_DELTA = 120;

    private const int WHEEL_PAGESCROLL = int.MaxValue;

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

      int uSysParam;
      int uLinesPerWHEELDELTA;   // Scrolling speed (how much to scroll per WHEEL_DELTA).
      int iLines;                 // How much to scroll for currently accumulated value.
      int iDirIndex = isVertical ? 0 : 1;  // The index into iAccumulator[].
      uint dwNow;

      dwNow = GetTickCount();

      uLinesPerWHEELDELTA = 0;

      // Even when nPage is below one line, we still want to scroll at least a little.
      if (nPage < 1)
      {
        nPage = 1;
      }

      // Ask the system for scrolling speed.
      uSysParam = isVertical ? SPI_GETWHEELSCROLLLINES : SPI_GETWHEELSCROLLCHARS;

      if (!SystemParametersInfo(uSysParam, 0, ref uLinesPerWHEELDELTA, 0))
      {
        uLinesPerWHEELDELTA = 3;  // default when SystemParametersInfo() fails.
      }

      if (uLinesPerWHEELDELTA == WHEEL_PAGESCROLL)
      {
        // System tells to scroll over whole pages.
        uLinesPerWHEELDELTA = nPage;
      }

      if (uLinesPerWHEELDELTA > nPage)
      {
        // Slow down if page is too small. We don't want to scroll over multiple
        // pages at once.
        uLinesPerWHEELDELTA = nPage;
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
        else if (dwNow - _lastActivity[iDirIndex] > SystemInformation.DoubleClickTime * 2)
        {
          // Reset the accumulator if there was a long time of wheel inactivity.
          _accumulator[iDirIndex] = 0;
        }
        else if ((_accumulator[iDirIndex] > 0) == (iDelta < 0))
        {
          // Reset the accumulator if scrolling direction has been reversed.
          _accumulator[iDirIndex] = 0;
        }

        if (uLinesPerWHEELDELTA > 0)
        {
          // Accumulate the delta.
          _accumulator[iDirIndex] += iDelta;

          // Compute the lines to scroll.
          iLines = _accumulator[iDirIndex] * (int)uLinesPerWHEELDELTA / WHEEL_DELTA;

          // Decrease the accumulator for the consumed amount.
          // (Corresponds to the remainder of the integer divide above.)
          _accumulator[iDirIndex] -= iLines * WHEEL_DELTA / (int)uLinesPerWHEELDELTA;
        }
        else
        {
          // uLinesPerWHEELDELTA == 0, i.e. likely configured to no scrolling
          // with mouse wheel.
          iLines = 0;
          _accumulator[iDirIndex] = 0;
        }

        _lastActivity[iDirIndex] = dwNow;
      }

      // Note that for vertical wheel, Windows provides the delta with opposite
      // sign. Hence the minus.
      return isVertical ? -iLines : iLines;
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