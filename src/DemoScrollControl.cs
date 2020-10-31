using Cyotek.Windows.Forms;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Cyotek.Demo.Scroll
{
  /// <summary> A demonstration scroll control. </summary>
  /// <seealso cref="Control"/>
  [DefaultProperty(nameof(ItemCount))]
  internal partial class DemoScrollControl : Control
  {
    #region Private Fields

    private static readonly object _eventTopItemChanged = new object();

    private int _columns;

    private int _fullyVisibleRows;

    private int _gap;

    private int _itemCount;

    private int _itemHeight;

    private int _rows;

    private VScrollBar _scrollBar;

    private int _topItem;

    private int _visibleRows;

    #endregion Private Fields

    #region Public Constructors

    /// <summary> Static constructor. </summary>
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

    /// <summary> Default constructor. </summary>
    public DemoScrollControl()
    {
      this.SetStyle(ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);

      base.DoubleBuffered = true;
      base.BackColor = SystemColors.Window;
      base.ForeColor = SystemColors.WindowText;

      _itemHeight = 32;
      _columns = 1;

      _scrollBar = new VScrollBar
      {
        Enabled = false,
        Visible = false
      };

      _scrollBar.ValueChanged += this.ScrollbarValueChangedHandler;

      this.Controls.Add(_scrollBar);
    }

    #endregion Public Constructors

    #region Public Events

    /// <summary> Event queue for all listeners interested in TextChanged events. </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public new event EventHandler TextChanged
    {
      add { base.TextChanged += value; }
      remove { base.TextChanged -= value; }
    }

    /// <summary> Event queue for all listeners interested in TopItemChanged events. </summary>
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

    /// <summary> Gets or sets the background color for the control. </summary>
    /// <value>
    /// A <see cref="T:System.Drawing.Color" /> that represents the background color of the control.
    /// The default is the value of the <see cref="P:System.Windows.Forms.Control.DefaultBackColor" />
    /// property.
    /// </value>
    /// <seealso cref="System.Windows.Forms.Control.BackColor"/>
    [DefaultValue(typeof(Color), "Window")]
    public override Color BackColor
    {
      get { return base.BackColor; }
      set { base.BackColor = value; }
    }

    /// <summary> Gets or sets the number of columns in the list. </summary>
    /// <value> The number of columns in the list. </value>
    [DefaultValue(1)]
    [Category("ScrollDemo")]
    public int Columns
    {
      get { return _columns; }
      set
      {
        if (_columns != value)
        {
          _columns = value;

          this.DefineRows();
          this.Invalidate();
        }
      }
    }

    /// <summary> Gets or sets the foreground color of the control. </summary>
    /// <value>
    /// The foreground <see cref="T:System.Drawing.Color" /> of the control. The default is the value
    /// of the <see cref="P:System.Windows.Forms.Control.DefaultForeColor" /> property.
    /// </value>
    /// <seealso cref="System.Windows.Forms.Control.ForeColor"/>
    [DefaultValue(typeof(Color), "WindowText")]
    public override Color ForeColor
    {
      get { return base.ForeColor; }
      set { base.ForeColor = value; }
    }

    /// <summary> Gets or sets the inner gap between columns and rows. </summary>
    /// <value> The inner gap between columns and rows. </value>
    [DefaultValue(0)]
    [Category("ScrollDemo")]
    public int Gap
    {
      get { return _gap; }
      set
      {
        if (_gap != value)
        {
          _gap = value;

          this.DefineRows();
          this.Invalidate();
        }
      }
    }

    /// <summary> Gets or sets the number of items in the list. </summary>
    /// <value> The number of items in the list. </value>
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

    /// <summary> Gets or sets the height of each row of items. </summary>
    /// <value> The height of items in each row. </value>
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

    /// <summary> Gets or sets the text associated with this control. </summary>
    /// <value> The text associated with this control. </value>
    /// <seealso cref="System.Windows.Forms.Control.Text"/>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public override string Text
    {
      get { return base.Text; }
      set { base.Text = value; }
    }

    /// <summary> Gets or sets the index of the item at the top left of the control. </summary>
    /// <value> The index of the item at the top left of the control. </value>
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

          if (_columns > 0)
          {
            this.SetScrollValue(value / _columns);
          }
        }
      }
    }

    #endregion Public Properties

    #region Private Properties

    /// <summary> Gets the inner client rectangle. </summary>
    /// <value> A <see cref="Rectangle"/> that describes the inner client area. </value>
    private Rectangle InnerClient
    {
      get
      {
        Padding padding;
        Size size;
        int w;

        padding = this.Padding;
        size = this.ClientSize;
        w = size.Width - padding.Horizontal;
        if (_scrollBar?.Visible == true)
        {
          w -= _scrollBar.Width;
        }

        return new Rectangle(padding.Left, padding.Top, w, size.Height - padding.Vertical);
      }
    }

    #endregion Private Properties

    #region Public Methods

    /// <summary> Determines which item (if any) has been hit. </summary>
    /// <param name="x">  The x coordinate to test. </param>
    /// <param name="y">  The y coordinate to test. </param>
    /// <returns> The index of the item if a hit is made, otherwise <c>-1</c>. </returns>
    public int HitTest(int x, int y)
    {
      Rectangle innerClient;
      int index;

      innerClient = this.InnerClient;

      if (_visibleRows > 0 && _columns > 0 && innerClient.Contains(x, y))
      {
        int r;
        int c;
        int rh;
        int cw;

        rh = _itemHeight + _gap;
        cw = innerClient.Width / _columns;

        r = (y - innerClient.Y) / rh;
        c = (x - innerClient.X) / cw;

        index = _topItem + (r * _columns) + c;

        if (index < 0 || index > _itemCount)
        {
          index = -1;
        }
      }
      else
      {
        index = -1;
      }

      return index;
    }

    /// <summary> Determines which item (if any) has been hit. </summary>
    /// <param name="point">  The point to test. </param>
    /// <returns> The index of the hit item if a hit is made, otherwise <c>-1</c>. </returns>
    public int HitTest(Point point)
    {
      return this.HitTest(point.X, point.Y);
    }

    #endregion Public Methods

    #region Protected Methods

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="T:System.Windows.Forms.Control" /> and
    /// its child controls and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing"> <see langword="true" /> to release both managed and unmanaged
    ///  resources; <see langword="false" /> to release only unmanaged resources. </param>
    /// <seealso cref="System.Windows.Forms.Control.Dispose(bool)"/>
    protected override void Dispose(bool disposing)
    {
      if (disposing && _scrollBar != null)
      {
        this.Controls.Remove(_scrollBar);
        _scrollBar.ValueChanged -= this.ScrollbarValueChangedHandler;
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

    /// <summary>
    /// Raises the <see cref="E:System.Windows.Forms.Control.FontChanged" /> event.
    /// </summary>
    /// <param name="e">  An <see cref="T:System.EventArgs" /> that contains the event data. </param>
    /// <seealso cref="System.Windows.Forms.Control.OnFontChanged(EventArgs)"/>
    protected override void OnFontChanged(EventArgs e)
    {
      base.OnFontChanged(e);

      this.DefineRows();
      this.Invalidate();
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

    /// <summary> Raises the <see cref="E:System.Windows.Forms.Control.MouseDown" /> event. </summary>
    /// <param name="e"> A <see cref="T:System.Windows.Forms.MouseEventArgs" /> that contains the
    ///  event data. </param>
    /// <seealso cref="System.Windows.Forms.Control.OnMouseDown(MouseEventArgs)"/>
    protected override void OnMouseDown(MouseEventArgs e)
    {
      base.OnMouseDown(e);

      if (!this.Focused && this.CanFocus)
      {
        this.Focus();
      }
    }

    /// <summary> Raises the <see cref="E:System.Windows.Forms.Control.MouseWheel" /> event. </summary>
    /// <param name="e"> A <see cref="T:System.Windows.Forms.MouseEventArgs" /> that contains the
    ///  event data. </param>
    /// <seealso cref="System.Windows.Forms.Control.OnMouseWheel(MouseEventArgs)"/>
    protected override void OnMouseWheel(MouseEventArgs e)
    {
      base.OnMouseWheel(e);

      if (_fullyVisibleRows > 0)
      {
        this.ScrollControl(WheelHelper.WheelScrollLines(this.Handle, e.Delta, _fullyVisibleRows, true));
      }
    }

    /// <summary>
    /// Raises the <see cref="E:System.Windows.Forms.Control.PaddingChanged" /> event.
    /// </summary>
    /// <param name="e">  A <see cref="T:System.EventArgs" /> that contains the event data. </param>
    /// <seealso cref="System.Windows.Forms.Control.OnPaddingChanged(EventArgs)"/>
    protected override void OnPaddingChanged(EventArgs e)
    {
      base.OnPaddingChanged(e);

      this.DefineRows();
      this.Invalidate();
    }

    /// <summary> Raises the <see cref="E:System.Windows.Forms.Control.Paint" /> event. </summary>
    /// <param name="e"> A <see cref="T:System.Windows.Forms.PaintEventArgs" /> that contains the
    ///  event data. </param>
    /// <seealso cref="System.Windows.Forms.Control.OnPaint(PaintEventArgs)"/>
    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);

      if (_visibleRows > 0)
      {
        Rectangle innerClient;
        int ix;
        int iy;
        int iw;
        int ih;
        int itemIndex;

        innerClient = this.InnerClient;
        e.Graphics.SetClip(innerClient);

        iy = innerClient.Y;
        iw = (innerClient.Width - (_gap * (_columns - 1))) / _columns;
        ih = _itemHeight;

        itemIndex = _topItem;

        for (int r = 0; r < _visibleRows; r++)
        {
          ix = innerClient.X;

          for (int c = 0; c < _columns; c++)
          {
            if (itemIndex < _itemCount)
            {
              Rectangle bounds;

              bounds = new Rectangle(ix, iy, iw - 1, ih - 1);

              e.Graphics.DrawRectangle(SystemPens.Control, bounds);

              TextRenderer.DrawText(
                e.Graphics,
                itemIndex.ToString("N"),
                this.Font,
                new Rectangle(ix, iy, iw, ih),
                this.ForeColor,
                TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.PreserveGraphicsClipping | TextFormatFlags.EndEllipsis);

              ix += iw + _gap;
              itemIndex++;
            }
            else
            {
              break;
            }
          }

          if (itemIndex >= _itemCount)
          {
            break;
          }

          iy += ih + _gap;
        }
      }
    }

    /// <summary> Raises the <see cref="E:System.Windows.Forms.Control.Resize" /> event. </summary>
    /// <param name="e">  An <see cref="T:System.EventArgs" /> that contains the event data. </param>
    /// <seealso cref="System.Windows.Forms.Control.OnResize(EventArgs)"/>
    protected override void OnResize(EventArgs e)
    {
      base.OnResize(e);

      if (_scrollBar != null)
      {
        Size size;

        size = this.ClientSize;

        _scrollBar.Bounds = new Rectangle(size.Width - _scrollBar.Width, 0, _scrollBar.Width, size.Height);
        this.DefineRows();
      }
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

    /// <summary> Defines the number of rows the list contains and updates scrollbar attributes. </summary>
    private void DefineRows()
    {
      if (_itemCount > 0 && _columns > 0)
      {
        int height;

        _rows = _itemCount / _columns;
        if (_itemCount % _columns != 0)
        {
          _rows++;
        }

        height = this.ClientSize.Height;

        _fullyVisibleRows = height / (_itemHeight + _gap);
        _visibleRows = _fullyVisibleRows;

        if (_fullyVisibleRows == 0)
        {
          _fullyVisibleRows = 1; // always make sure there is at least one row, otherwise you can't scroll
        }

        if (_rows > _visibleRows && height % (_itemHeight + _gap) != 0)
        {
          _visibleRows++;
        }

        if (_scrollBar != null)
        {
          _scrollBar.LargeChange = _fullyVisibleRows;
          _scrollBar.Maximum = _rows;
          this.SetScrollValue(_scrollBar.Value);
        }
      }

      if (_scrollBar != null)
      {
        _scrollBar.Enabled = _rows > _fullyVisibleRows;
        _scrollBar.Visible = _rows > _fullyVisibleRows;
      }
    }

    /// <summary> Processes shortcut keys for scrolling the control. </summary>
    /// <param name="e">  Key event information. </param>
    private void ProcessScrollKeys(KeyEventArgs e)
    {
      switch (e.KeyCode)
      {
        case Keys.Up:
          this.ScrollControl(-1);
          break;

        case Keys.Down:
          this.ScrollControl(1);
          break;

        case Keys.PageUp:
          this.ScrollControl(-_fullyVisibleRows);
          break;

        case Keys.PageDown:
          this.ScrollControl(_fullyVisibleRows);
          break;

        case Keys.Home:
          this.ScrollControl(-_itemCount);
          break;

        case Keys.End:
          this.ScrollControl(_itemCount);
          break;
      }
    }

    /// <summary> Handler, called when the scrollbar value changed. </summary>
    /// <param name="sender"> Source of the event. </param>
    /// <param name="e">  Event information. </param>
    private void ScrollbarValueChangedHandler(object sender, EventArgs e)
    {
      _topItem = _scrollBar.Value * _columns;
      this.Invalidate();

      this.OnTopItemChanged(EventArgs.Empty);
    }

    /// <summary> Scrolls the list by the specified number of lines. </summary>
    /// <param name="lines">  The number lines to scroll by. </param>
    private void ScrollControl(int lines)
    {
      this.SetScrollValue(_scrollBar.Value + lines);
    }

    /// <summary> Sets the scrollbar value. </summary>
    /// <param name="value">  The value to apply. </param>
    private void SetScrollValue(int value)
    {
      value = Math.Min(value, _scrollBar.Maximum - (_scrollBar.LargeChange + 1));

      if (value < 0)
      {
        value = 0;
      }

      _scrollBar.Value = value;
    }

    #endregion Private Methods
  }
}