using Cyotek.Windows.Forms;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Cyotek.Demo.Scroll
{
  [DefaultProperty(nameof(ItemCount))]
  internal partial class DemoScrollControl : Control
  {
    #region Private Fields

    private static readonly object _eventTopItemChanged = new object();

    private int _fullyVisibleRows;

    private int _itemCount;

    private int _itemHeight;

    private int _rows;

    private VScrollBar _scrollBar;

    private int _topItem;

    private int _visibleRows;

    #endregion Private Fields

    #region Public Constructors

    static DemoScrollControl()
    {
      OperatingSystem os;

      os = Environment.OSVersion;

      // Windows 10 (or at least recent versions of it) automatically
      // send WM_MOUSEWHEEL and WM_MOUSEHWHEEL messages to the window
      // under the cursor even if it doesn't have focus.
      //
      // Therefore, we don't need to add a message filter unless we
      // are running under an older version of Windows.
      //
      // Note: Remember that attempting to get the OS version will lie
      // unless you are using a manifest that explicitly states you
      // support a given version of Windows

      if (os.Platform == PlatformID.Win32NT && os.Version.Major < 10)
      {
        MouseWheelMessageFilter.Active = true;
      }
    }

    public DemoScrollControl()
    {
      this.SetStyle(ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);

      base.DoubleBuffered = true;
      base.BackColor = SystemColors.Window;
      base.ForeColor = SystemColors.WindowText;

      _itemHeight = 32;

      _scrollBar = new VScrollBar
      {
        Enabled = false,
        Visible = false
      };

      _scrollBar.ValueChanged += this.ScrollbarValueChangedHandler;
      _scrollBar.Scroll += this.ScrollbarValueChangedHandler;

      this.Controls.Add(_scrollBar);
    }

    #endregion Public Constructors

    #region Public Events

    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public new event EventHandler TextChanged
    {
      add { base.TextChanged += value; }
      remove { base.TextChanged -= value; }
    }

    [Category("Property Changed")]
    public event EventHandler TopItemChanged
    {
      add
      {
        this.Events.AddHandler(_eventTopItemChanged, value);
      }
      remove
      {
        this.Events.RemoveHandler(_eventTopItemChanged, value);
      }
    }

    #endregion Public Events

    #region Public Properties

    [DefaultValue(typeof(Color), "Window")]
    public override Color BackColor
    {
      get { return base.BackColor; }
      set { base.BackColor = value; }
    }

    [DefaultValue(typeof(Color), "WindowText")]
    public override Color ForeColor
    {
      get { return base.ForeColor; }
      set { base.ForeColor = value; }
    }

    [DefaultValue(0)]
    [Category("ScrollDemo")]
    public int ItemCount
    {
      get { return _itemCount; }
      set
      {
        if (_itemCount != value)
        {
          _itemCount = value;

          this.DefineRows();
          this.Invalidate();
        }
      }
    }

    [DefaultValue(32)]
    [Category("ScrollDemo")]
    public int ItemHeight
    {
      get { return _itemHeight; }
      set
      {
        if (_itemHeight != value)
        {
          _itemHeight = value;

          this.DefineRows();
          this.Invalidate();
        }
      }
    }

    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public override string Text
    {
      get { return base.Text; }
      set { base.Text = value; }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int TopItem
    {
      get { return _topItem; }
      set
      {
        if (value < 0)
        {
          value = 0;
        }
        else if (value > _rows)
        {
          value = _rows;
        }

        if (_topItem != value)
        {
          _topItem = value;

          _scrollBar.Value = value;
        }
      }
    }

    #endregion Public Properties

    #region Protected Methods

    protected override void Dispose(bool disposing)
    {
      if (disposing && _scrollBar != null)
      {
        this.Controls.Remove(_scrollBar);
        _scrollBar.ValueChanged -= this.ScrollbarValueChangedHandler;
        _scrollBar.Scroll -= this.ScrollbarValueChangedHandler;
        _scrollBar.Dispose();
        _scrollBar = null;
      }

      base.Dispose(disposing);
    }

    /// <summary>
    /// Determines whether the specified key is a regular input key or a special key that requires
    /// preprocessing.
    /// </summary>
    /// <param name="keyData">  One of the <see cref="T:System.Windows.Forms.Keys" /> values. </param>
    /// <returns>
    /// <see langword="true" /> if the specified key is a regular input key; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <seealso cref="M:System.Windows.Forms.Control.IsInputKey(Keys)"/>
    protected override bool IsInputKey(Keys keyData)
    {
      return keyData == Keys.Up
        || keyData == Keys.Down
        || keyData == Keys.Home
        || keyData == Keys.End
        || keyData == Keys.PageUp
        || keyData == Keys.PageDown
        || base.IsInputKey(keyData);
    }

    protected override void OnFontChanged(EventArgs e)
    {
      base.OnFontChanged(e);

      this.DefineRows();
    }

    /// <summary> Raises the <see cref="E:System.Windows.Forms.Control.KeyDown" /> event. </summary>
    /// <param name="e"> A <see cref="T:System.Windows.Forms.KeyEventArgs" /> that contains the event
    ///  data. </param>
    /// <seealso cref="M:System.Windows.Forms.Control.OnKeyDown(KeyEventArgs)"/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
      base.OnKeyDown(e);

      if (!e.Handled)
      {
        this.ProcessScrollKeys(e);
      }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
      base.OnMouseDown(e);

      if (!this.Focused && this.CanFocus)
      {
        this.Focus();
      }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
      base.OnMouseWheel(e);

      if (_fullyVisibleRows > 0)
      {
        this.HandleScroll(WheelHelper.WheelScrollLines(this.Handle, e.Delta, _fullyVisibleRows, true));
      }
    }

    protected override void OnPaddingChanged(EventArgs e)
    {
      base.OnPaddingChanged(e);

      this.DefineRows();
      this.Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);

      if (_visibleRows > 0)
      {
        Padding padding;
        Size size;
        int x;
        int y;
        int w;
        int h;
        int rows;

        padding = this.Padding;
        size = this.ClientSize;
        x = padding.Left;
        y = padding.Top;
        w = size.Width - padding.Horizontal;
        if (_scrollBar?.Visible == true)
        {
          w -= _scrollBar.Width;
        }
        h = _itemHeight;

        rows = Math.Min(_itemCount - _topItem, _visibleRows);

        for (int i = 0; i < rows; i++)
        {
          Rectangle bounds;

          bounds = new Rectangle(x, y, w - 1, h - 1);

          e.Graphics.DrawRectangle(SystemPens.Control, bounds);

          TextRenderer.DrawText(e.Graphics, (_topItem + i).ToString(), this.Font, Rectangle.Inflate(bounds, -3, -3), this.ForeColor, TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine);

          y += _itemHeight;
        }
      }
    }

    protected override void OnResize(EventArgs e)
    {
      base.OnResize(e);

      if (_scrollBar != null)
      {
        Size size;

        size = this.ClientSize;

        _scrollBar.Bounds = new Rectangle(size.Width - _scrollBar.Width, 0, _scrollBar.Width, size.Height);
      }

      this.DefineRows();
    }

    /// <summary>
    /// Raises the <see cref="TopItemChanged" /> event.
    /// </summary>
    /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
    protected virtual void OnTopItemChanged(EventArgs e)
    {
      ((EventHandler)this.Events[_eventTopItemChanged])?.Invoke(this, e);
    }

    #endregion Protected Methods

    #region Private Methods

    private void DefineRows()
    {
      if (_itemCount > 0)
      {
        int height;

        _rows = _itemCount;

        height = this.ClientSize.Height;

        _fullyVisibleRows = height / _itemHeight;
        _visibleRows = _fullyVisibleRows;

        if (height % _itemHeight != 0)
        {
          _visibleRows++;
        }

        _scrollBar.LargeChange = _fullyVisibleRows;
        _scrollBar.Maximum = _itemCount;
      }

      _scrollBar.Enabled = _itemCount > _fullyVisibleRows;

      if (_scrollBar.Visible != _scrollBar.Enabled)
      {
        _scrollBar.Visible = _scrollBar.Enabled;
        this.Invalidate();
      }
    }

    private void HandleScroll(int lines)
    {
      int value;

      value = _scrollBar.Value + lines;

      if (value > (_rows - _fullyVisibleRows))
      {
        value = _rows - _fullyVisibleRows;
      }

      if (value < 0)
      {
        value = 0;
      }

      _scrollBar.Value = value;
    }

    private void ProcessScrollKeys(KeyEventArgs e)
    {
      switch (e.KeyCode)
      {
        case Keys.Up:
          this.HandleScroll(-1);
          break;

        case Keys.Down:
          this.HandleScroll(1);
          break;

        case Keys.PageUp:
          this.HandleScroll(-_fullyVisibleRows);
          break;

        case Keys.PageDown:
          this.HandleScroll(_fullyVisibleRows);
          break;

        case Keys.Home:
          this.HandleScroll(-_itemCount);
          break;

        case Keys.End:
          this.HandleScroll(_itemCount);
          break;
      }
    }

    private void ScrollbarValueChangedHandler(object sender, EventArgs e)
    {
      _topItem = _scrollBar.Value;
      this.Invalidate();

      this.OnTopItemChanged(EventArgs.Empty);
    }

    #endregion Private Methods
  }
}