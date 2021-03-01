namespace FourDSecurity.QuicksetUtility.QuicksetControl
{
	partial class ContropObservationOptions
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ContropObservationOptions));
			this.b_Ok = new System.Windows.Forms.Button();
			this.b_Cancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// b_Ok
			// 
			this.b_Ok.Location = new System.Drawing.Point(111, 114);
			this.b_Ok.Name = "b_Ok";
			this.b_Ok.Size = new System.Drawing.Size(75, 23);
			this.b_Ok.TabIndex = 0;
			this.b_Ok.Text = "&OK";
			this.b_Ok.UseVisualStyleBackColor = true;
			this.b_Ok.Click += new System.EventHandler(this.b_Ok_Click);
			// 
			// b_Cancel
			// 
			this.b_Cancel.Location = new System.Drawing.Point(186, 114);
			this.b_Cancel.Name = "b_Cancel";
			this.b_Cancel.Size = new System.Drawing.Size(75, 23);
			this.b_Cancel.TabIndex = 1;
			this.b_Cancel.Text = "&Cancel";
			this.b_Cancel.UseVisualStyleBackColor = true;
			this.b_Cancel.Click += new System.EventHandler(this.b_Cancel_Click);
			// 
			// CO30Options
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(372, 149);
			this.Controls.Add(this.b_Cancel);
			this.Controls.Add(this.b_Ok);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "CO30Options";
			this.Text = "CO30 Options";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button b_Ok;
		private System.Windows.Forms.Button b_Cancel;
	}
}