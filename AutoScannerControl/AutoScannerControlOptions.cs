using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace FourDSecurity.QuicksetUtility.QuicksetControl
{
	public partial class ContropObservationOptions : Form
	{
		public ContropObservationOptions()
		{
			InitializeComponent();
		}

		private void b_Cancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		private void b_Ok_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
			this.Close();
		}
	}
}
