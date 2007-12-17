namespace SteamEngine.RemoteConsole {
	partial class CommandLineDisplay {
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
			this.display = new SteamEngine.RemoteConsole.LogStrDisplay();
			this.txtCommandLine = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			// 
			// display
			// 
			this.display.Dock = System.Windows.Forms.DockStyle.Fill;
			this.display.Location = new System.Drawing.Point(0, 0);
			this.display.Name = "display";
			this.display.Size = new System.Drawing.Size(496, 391);
			this.display.TabIndex = 0;
			// 
			// txtCommandLine
			// 
			this.txtCommandLine.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.txtCommandLine.Enabled = false;
			this.txtCommandLine.Location = new System.Drawing.Point(0, 391);
			this.txtCommandLine.Name = "txtCommandLine";
			this.txtCommandLine.Size = new System.Drawing.Size(496, 21);
			this.txtCommandLine.TabIndex = 2;
			this.txtCommandLine.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtCommandLine_KeyPress);
			// 
			// CommandLineDisplay
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.display);
			this.Controls.Add(this.txtCommandLine);
			this.Name = "CommandLineDisplay";
			this.Size = new System.Drawing.Size(496, 412);
			this.ResumeLayout(false);

		}

		#endregion

		private LogStrDisplay display;
		private System.Windows.Forms.ComboBox txtCommandLine;
	}
}
