using System;
using System.Windows.Forms;

// Creating a custom single-axis scrolling control in WinForms
// https://www.cyotek.com/blog/creating-a-custom-single-axis-scrolling-control-in-winforms

// Copyright © 2020 Cyotek Ltd. All Rights Reserved.

// This work is licensed under the MIT License.
// See LICENSE.TXT for the full text

// Found this example useful?
// https://www.paypal.me/cyotek

namespace Cyotek.Demo.Windows.Forms
{
  internal partial class MainForm : BaseForm
  {
    #region Public Constructors

    public MainForm()
    {
      this.InitializeComponent();
    }

    #endregion Public Constructors

    #region Protected Methods

    protected override void OnShown(EventArgs e)
    {
      base.OnShown(e);

      demoScrollControl.ItemCount = 100;
    }

    #endregion Protected Methods

    #region Private Methods

    private void AboutToolStripMenuItem_Click(object sender, EventArgs e)
    {
      AboutDialog.ShowAboutDialog();
    }

    private void CyotekLinkToolStripStatusLabel_Click(object sender, EventArgs e)
    {
      AboutDialog.OpenCyotekHomePage();

      cyotekLinkToolStripStatusLabel.LinkVisited = true;
    }

    private void DemoScrollControl_MouseLeave(object sender, EventArgs e)
    {
      statusToolStripStatusLabel.Text = string.Empty;
    }

    private void DemoScrollControl_MouseMove(object sender, MouseEventArgs e)
    {
      statusToolStripStatusLabel.Text = demoScrollControl.HitTest(e.Location).ToString();
    }

    private void DemoScrollControl_TopItemChanged(object sender, EventArgs e)
    {
      this.Text = string.Format("{0} (TopItem: {1})", Application.ProductName, demoScrollControl.TopItem);
    }

    private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
    {
      this.Close();
    }

    #endregion Private Methods
  }
}