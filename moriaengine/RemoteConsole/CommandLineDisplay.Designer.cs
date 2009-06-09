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
			this.txtCommandLine = new System.Windows.Forms.ComboBox();
			this.pnlGameServerButtons = new System.Windows.Forms.Panel();
			this.btnResync = new System.Windows.Forms.Button();
			this.btnRecompile = new System.Windows.Forms.Button();
			this.btnExit = new System.Windows.Forms.Button();
			this.txtDisplay = new SteamEngine.RemoteConsole.LogStrDisplay();
			this.pnlGameServerButtons.SuspendLayout();
			this.SuspendLayout();
			// 
			// txtCommandLine
			// 
			this.txtCommandLine.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.txtCommandLine.Enabled = false;
			this.txtCommandLine.Location = new System.Drawing.Point(0, 379);
			this.txtCommandLine.Name = "txtCommandLine";
			this.txtCommandLine.Size = new System.Drawing.Size(400, 21);
			this.txtCommandLine.TabIndex = 2;
			this.txtCommandLine.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtCommandLine_KeyPress);
			// 
			// pnlGameServerButtons
			// 
			this.pnlGameServerButtons.Controls.Add(this.btnExit);
			this.pnlGameServerButtons.Controls.Add(this.btnRecompile);
			this.pnlGameServerButtons.Controls.Add(this.btnResync);
			this.pnlGameServerButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.pnlGameServerButtons.Location = new System.Drawing.Point(0, 350);
			this.pnlGameServerButtons.Name = "pnlGameServerButtons";
			this.pnlGameServerButtons.Size = new System.Drawing.Size(400, 29);
			this.pnlGameServerButtons.TabIndex = 1;
			// 
			// btnResync
			// 
			this.btnResync.Location = new System.Drawing.Point(3, 3);
			this.btnResync.Name = "btnResync";
			this.btnResync.Size = new System.Drawing.Size(75, 23);
			this.btnResync.TabIndex = 0;
			this.btnResync.Text = "&Resync";
			this.btnResync.UseVisualStyleBackColor = true;
			this.btnResync.Click += new System.EventHandler(this.btnResync_Click);
			// 
			// btnRecompile
			// 
			this.btnRecompile.Location = new System.Drawing.Point(84, 3);
			this.btnRecompile.Name = "btnRecompile";
			this.btnRecompile.Size = new System.Drawing.Size(75, 23);
			this.btnRecompile.TabIndex = 1;
			this.btnRecompile.Text = "Re&compile";
			this.btnRecompile.UseVisualStyleBackColor = true;
			this.btnRecompile.Click += new System.EventHandler(this.btnRecompile_Click);
			// 
			// btnExit
			// 
			this.btnExit.Location = new System.Drawing.Point(165, 3);
			this.btnExit.Name = "btnExit";
			this.btnExit.Size = new System.Drawing.Size(75, 23);
			this.btnExit.TabIndex = 2;
			this.btnExit.Text = "E&xit";
			this.btnExit.UseVisualStyleBackColor = true;
			this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
			// 
			// txtDisplay
			// 
			this.txtDisplay.DefaultTitle = null;
			this.txtDisplay.Dock = System.Windows.Forms.DockStyle.Fill;
			this.txtDisplay.Location = new System.Drawing.Point(0, 0);
			this.txtDisplay.Name = "txtDisplay";
			this.txtDisplay.Size = new System.Drawing.Size(400, 350);
			this.txtDisplay.TabIndex = 0;
			// 
			// CommandLineDisplay
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.txtDisplay);
			this.Controls.Add(this.pnlGameServerButtons);
			this.Controls.Add(this.txtCommandLine);
			this.Name = "CommandLineDisplay";
			this.Size = new System.Drawing.Size(400, 400);
			this.pnlGameServerButtons.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ComboBox txtCommandLine;
		public LogStrDisplay txtDisplay;
		private System.Windows.Forms.Panel pnlGameServerButtons;
		private System.Windows.Forms.Button btnResync;
		private System.Windows.Forms.Button btnExit;
		private System.Windows.Forms.Button btnRecompile;
	}
}
