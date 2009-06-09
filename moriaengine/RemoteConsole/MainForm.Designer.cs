namespace SteamEngine.RemoteConsole {
	partial class MainForm {
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.components = new System.ComponentModel.Container();
			this.mainMenu = new System.Windows.Forms.MainMenu(this.components);
			this.menuConsole = new System.Windows.Forms.MenuItem();
			this.menuConnect = new System.Windows.Forms.MenuItem();
			this.menuDisconnect = new System.Windows.Forms.MenuItem();
			this.menuItem2 = new System.Windows.Forms.MenuItem();
			this.menuExit = new System.Windows.Forms.MenuItem();
			this.menuRemoteServer = new System.Windows.Forms.MenuItem();
			this.menuStartGameServer = new System.Windows.Forms.MenuItem();
			this.menuRestartAuxServer = new System.Windows.Forms.MenuItem();
			this.tabControl = new System.Windows.Forms.TabControl();
			this.systemTab = new System.Windows.Forms.TabPage();
			this.systemTabPage = new SteamEngine.RemoteConsole.LogStrDisplay();
			this.reconnectingTimer = new System.Windows.Forms.Timer(this.components);
			this.tabControl.SuspendLayout();
			this.systemTab.SuspendLayout();
			this.SuspendLayout();
			// 
			// mainMenu
			// 
			this.mainMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuConsole,
            this.menuRemoteServer});
			// 
			// menuConsole
			// 
			this.menuConsole.Index = 0;
			this.menuConsole.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuConnect,
            this.menuDisconnect,
            this.menuItem2,
            this.menuExit});
			this.menuConsole.Text = "&Console";
			// 
			// menuConnect
			// 
			this.menuConnect.Index = 0;
			this.menuConnect.Text = "&Connect";
			this.menuConnect.Click += new System.EventHandler(this.menuConnect_Click);
			// 
			// menuDisconnect
			// 
			this.menuDisconnect.Enabled = false;
			this.menuDisconnect.Index = 1;
			this.menuDisconnect.Text = "&Disconnect";
			this.menuDisconnect.Click += new System.EventHandler(this.menuDisconnect_Click);
			// 
			// menuItem2
			// 
			this.menuItem2.Index = 2;
			this.menuItem2.Text = "-";
			// 
			// menuExit
			// 
			this.menuExit.Index = 3;
			this.menuExit.Text = "&Exit";
			this.menuExit.Click += new System.EventHandler(this.menuExit_Click);
			// 
			// menuRemoteServer
			// 
			this.menuRemoteServer.Enabled = false;
			this.menuRemoteServer.Index = 1;
			this.menuRemoteServer.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.menuStartGameServer,
            this.menuRestartAuxServer});
			this.menuRemoteServer.Text = "Remote Server";
			// 
			// menuStartGameServer
			// 
			this.menuStartGameServer.Index = 0;
			this.menuStartGameServer.Text = "Start Gameserver";
			this.menuStartGameServer.Click += new System.EventHandler(this.menuStartGameServer_Click);
			// 
			// menuRestartAuxServer
			// 
			this.menuRestartAuxServer.Index = 1;
			this.menuRestartAuxServer.Text = "Restart AuxServer";
			this.menuRestartAuxServer.Click += new System.EventHandler(this.menuRestartAuxServer_Click);
			// 
			// tabControl
			// 
			this.tabControl.Controls.Add(this.systemTab);
			this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl.Location = new System.Drawing.Point(0, 0);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			this.tabControl.Size = new System.Drawing.Size(632, 433);
			this.tabControl.TabIndex = 0;
			// 
			// systemTab
			// 
			this.systemTab.Controls.Add(this.systemTabPage);
			this.systemTab.Location = new System.Drawing.Point(4, 22);
			this.systemTab.Name = "systemTab";
			this.systemTab.Padding = new System.Windows.Forms.Padding(3);
			this.systemTab.Size = new System.Drawing.Size(624, 407);
			this.systemTab.TabIndex = 0;
			this.systemTab.Text = "System";
			this.systemTab.UseVisualStyleBackColor = true;
			// 
			// systemTabPage
			// 
			this.systemTabPage.DefaultTitle = null;
			this.systemTabPage.Dock = System.Windows.Forms.DockStyle.Fill;
			this.systemTabPage.Location = new System.Drawing.Point(3, 3);
			this.systemTabPage.Name = "systemTabPage";
			this.systemTabPage.Size = new System.Drawing.Size(618, 401);
			this.systemTabPage.TabIndex = 0;
			this.systemTabPage.Load += new System.EventHandler(this.systemTabPage_Load);
			// 
			// reconnectingTimer
			// 
			this.reconnectingTimer.Interval = 1000;
			this.reconnectingTimer.Tick += new System.EventHandler(this.reconnectingTimer_Tick);
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(632, 433);
			this.Controls.Add(this.tabControl);
			this.Menu = this.mainMenu;
			this.Name = "MainForm";
			this.Text = "SteamEngine remote console";
			this.Load += new System.EventHandler(this.MainForm_Load);
			this.tabControl.ResumeLayout(false);
			this.systemTab.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.MainMenu mainMenu;
		private System.Windows.Forms.MenuItem menuConsole;
		private System.Windows.Forms.MenuItem menuExit;
		private System.Windows.Forms.TabControl tabControl;
		private System.Windows.Forms.TabPage systemTab;
		private LogStrDisplay systemTabPage;
		private System.Windows.Forms.MenuItem menuItem2;
		private System.Windows.Forms.MenuItem menuDisconnect;
		private System.Windows.Forms.MenuItem menuConnect;
		private System.Windows.Forms.MenuItem menuRemoteServer;
		private System.Windows.Forms.MenuItem menuStartGameServer;
		private System.Windows.Forms.MenuItem menuRestartAuxServer;
		private System.Windows.Forms.Timer reconnectingTimer;
	}
}

