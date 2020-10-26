using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

// Cyotek ImageBox
// Copyright (c) 2010-2015 Cyotek Ltd.
// http://cyotek.com
// http://cyotek.com/blog/tag/imagebox

// Licensed under the MIT License. See license.txt for the full text.

// If you use this control in your applications, attribution, donations or contributions are welcome.

// This code is derived from http://stackoverflow.com/a/13292894/148962 and http://stackoverflow.com/a/11034674/148962

namespace Cyotek.Demo.Scroll
{
  internal partial class DemoScrollControl
  {
    #region Internal Classes

    /// <summary>
    /// A message filter for WM_MOUSEWHEEL and WM_MOUSEHWHEEL. This class cannot be inherited.
    /// </summary>
    /// <seealso cref="T:System.Windows.Forms.IMessageFilter"/>
    internal sealed class MouseWheelMessageFilter : IMessageFilter
    {
      #region Private Fields

      private const int WM_MOUSEHWHEEL = 0x20e;

      private const int WM_MOUSEWHEEL = 0x20a;

      private static bool _active;

      private static MouseWheelMessageFilter _instance;

      #endregion Private Fields

      #region Private Constructors

      /// <summary>
      /// Constructor that prevents a default instance of this class from being created.
      /// </summary>
      private MouseWheelMessageFilter()
      {
      }

      #endregion Private Constructors

      #region Public Properties

      /// <summary>
      /// Gets or sets a value indicating whether the filter is active
      /// </summary>
      /// <value>
      /// <c>true</c> if the message filter is active, <c>false</c> if not.
      /// </value>
      public static bool Active
      {
        get { return _active; }
        set
        {
          if (_active != value)
          {
            _active = value;

            if (_active)
            {
              Interlocked.CompareExchange(ref _instance, new MouseWheelMessageFilter(), null);

              Application.AddMessageFilter(_instance);
            }
            else if (_instance != null)
            {
              Application.RemoveMessageFilter(_instance);
            }
          }
        }
      }

      #endregion Public Properties

      #region Public Methods

      /// <summary>
      /// Filters out a message before it is dispatched.
      /// </summary>
      /// <param name="m">  [in,out] The message to be dispatched. You cannot modify this message. </param>
      /// <returns>
      /// <c>true</c> to filter the message and stop it from being dispatched; <c>false</c> to allow the message to
      /// continue to the next filter or control.
      /// </returns>
      /// <seealso cref="M:System.Windows.Forms.IMessageFilter.PreFilterMessage(Message@)"/>
      bool IMessageFilter.PreFilterMessage(ref Message m)
      {
        bool result;

        switch (m.Msg)
        {
          case WM_MOUSEWHEEL: // 0x020A
          case WM_MOUSEHWHEEL: // 0x020E
            IntPtr hControlUnderMouse;

            hControlUnderMouse = WindowFromPoint(new Point((int)m.LParam));

            if (hControlUnderMouse == m.HWnd)
            {
              // already headed for the right control
              result = false;
            }
            else
            {
              DemoScrollControl control;

              control = Control.FromHandle(hControlUnderMouse) as DemoScrollControl;

              if (control == null /* || !control.AllowUnfocusedMouseWheel */)
              {
                // window under the mouse either isn't managed, isn't an imagebox,
                // or it is an imagebox but the unfocused whell option is disabled.
                // whatever the case, do not try and handle the message
                result = false;
              }
              else
              {
                // redirect the message to the control under the mouse
                SendMessage(hControlUnderMouse, m.Msg, m.WParam, m.LParam);

                // eat the message (otherwise it's possible two controls will scroll
                // at the same time, which looks awful... and is probably confusing!)
                result = true;
              }
            }
            break;

          default:
            // not a message we can process, don't try and block it
            result = false;
            break;
        }

        return result;
      }

      #endregion Public Methods

      #region Private Methods

      [DllImport("user32.dll", SetLastError = false)]
      private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

      [DllImport("user32.dll")]
      private static extern IntPtr WindowFromPoint(Point point);

      #endregion Private Methods
    }

    #endregion Internal Classes
  }
}