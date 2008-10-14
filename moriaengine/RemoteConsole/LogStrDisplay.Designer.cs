namespace SteamEngine.RemoteConsole {
	partial class LogStrDisplay {
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.txtBox = new System.Windows.Forms.RichTextBox();
			this.chckAutoScroll = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// txtBox
			// 
			this.txtBox.Anchor = ((System.Windows.Forms.AnchorStyles) ((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.txtBox.Location = new System.Drawing.Point(0, 26);
			this.txtBox.Name = "txtBox";
			this.txtBox.Size = new System.Drawing.Size(306, 267);
			this.txtBox.TabIndex = 0;
			this.txtBox.Text = "";
			// 
			// chckAutoScroll
			// 
			this.chckAutoScroll.AutoSize = true;
			this.chckAutoScroll.Checked = true;
			this.chckAutoScroll.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chckAutoScroll.Location = new System.Drawing.Point(3, 3);
			this.chckAutoScroll.Name = "chckAutoScroll";
			this.chckAutoScroll.Size = new System.Drawing.Size(72, 17);
			this.chckAutoScroll.TabIndex = 1;
			this.chckAutoScroll.Text = "Autoscroll";
			this.chckAutoScroll.UseVisualStyleBackColor = true;
			// 
			// LogStrDisplay
			// 
			this.Controls.Add(this.chckAutoScroll);
			this.Controls.Add(this.txtBox);
			this.Name = "LogStrDisplay";
			this.Size = new System.Drawing.Size(306, 293);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.RichTextBox txtBox;
		private System.Windows.Forms.CheckBox chckAutoScroll;

	}
}
