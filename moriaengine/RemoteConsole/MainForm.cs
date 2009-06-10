using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using SteamEngine.Communication;

namespace SteamEngine.RemoteConsole {
	public partial class MainForm : Form {
		private Dictionary<int, CommandLineDisplay> displays = new Dictionary<int, CommandLineDisplay>();

		private EndPointSetting epsBeingReconnected;

		EventHandler txtDisplay_TitleChanged;

		public MainForm() {
			InitializeComponent();
			txtDisplay_TitleChanged = new EventHandler(TxtDisplay_TitleChanged);
		}

		private void menuExit_Click(object sender, EventArgs e) {
			this.Close();
		}

		public LogStrDisplay SystemDisplay {
			get {
				return this.systemTabPage;
			}
		}

		public void AddCmdLineDisplay(int id, string name) {
			if (!this.displays.ContainsKey(id)) {
				TabPage tab = new TabPage(name);
				CommandLineDisplay cld = new CommandLineDisplay(name, id);
				if (id < 1) {
					cld.RemoveGameServerButtons();
				}
				cld.txtDisplay.TitleChanged += txtDisplay_TitleChanged;
				tab.Controls.Add(cld);
				cld.Dock = DockStyle.Fill;
				this.tabControl.Controls.Add(tab);

				RemoveCmdLineDisplay(id);
				this.displays.Add(id, cld);
			}
		}

		public void RemoveCmdLineDisplay(int id) {
			CommandLineDisplay cld;
			if (this.displays.TryGetValue(id, out cld)) {
				cld.Parent.Dispose();

				this.displays.Remove(id);
			}
		}

		public void EnableCommandLineOnDisplay(int id) {
			CommandLineDisplay cld;
			if (this.displays.TryGetValue(id, out cld)) {
				cld.EnableComandLine();
			}
		}

		public void ClearCmdLineDisplays() {
			Control[] controls = new Control[this.tabControl.Controls.Count];
			this.tabControl.Controls.CopyTo(controls, 0);
			foreach (Control ctrl in controls) {
				if ((ctrl != this.systemTab) && (ctrl is TabPage)) {
					ctrl.Dispose();
				}
			}
			this.displays.Clear();
		}

		private void TxtDisplay_TitleChanged(object sender, EventArgs ignored) {
			LogStrDisplay display = (LogStrDisplay) sender;
			TabPage page = display.Parent.Parent as TabPage;
			if (page != null) {
				page.Text = display.Title;
			}
		}

		private void menuConnect_Click(object sender, EventArgs e) {
			this.reconnectingTimer.Stop();
			ConnectionForm cf = new ConnectionForm();
			cf.ShowDialog();
		}

		protected override void OnControlRemoved(ControlEventArgs e) {
			TabPage page = e.Control as TabPage;
			if (page != null) {
				foreach (Control pageControl in page.Controls) {
					CommandLineDisplay cld = pageControl as CommandLineDisplay;
					if (cld != null) {
						cld.txtDisplay.TitleChanged -= txtDisplay_TitleChanged;
					}
				}
			}

			base.OnControlRemoved(e);
		}

		//ConsoleClient cc;

		//private void displayBox_MouseDoubleClick(object sender, MouseEventArgs e) {
		//    cc = Pool<ConsoleClient>.Acquire();

		//    cc.Connect("localhost", 12345);
		//}
		//private void statusBar_MouseDoubleClick(object sender, MouseEventArgs e) {
		//    ConsolePacketGroup pg = Pool<ConsolePacketGroup>.Acquire();
		//    pg.AddPacket(Pool<ConsoleOutgoingPacket>.Acquire());

		//    cc.SendPacketGroup(pg);
		//}

		internal void SetConnectedTrue() {
			SetConnected(true);
		}

		internal void SetConnectedFalse() {
			SetConnected(false);
		}

		internal void SetConnected(bool connected) {
			this.menuConnect.Enabled = !connected;
			this.menuDisconnect.Enabled = connected;
			this.menuRemoteServer.Enabled = connected;
		}

		private void menuDisconnect_Click(object sender, EventArgs e) {
			this.reconnectingTimer.Stop();
			ConsoleClient.Disconnect("Disconnect menu item used.");
		}

		internal void WriteLine(int uid, string str) {
			if (uid >= 0) {
				CommandLineDisplay cld;
				if (this.displays.TryGetValue(uid, out cld)) {
					cld.txtDisplay.WriteLine(str);
				}
			} else {
				systemTabPage.WriteLine(str);
			}
		}

		internal void Write(int uid, string str) {
			if (uid >= 0) {
				CommandLineDisplay cld;
				if (this.displays.TryGetValue(uid, out cld)) {
					cld.txtDisplay.Write(str);
				}
			} else {
				systemTabPage.Write(str);
			}
		}

		private void menuStartGameServer_Click(object sender, EventArgs e) {
			ConsoleClient cc = ConsoleClient.ConnectedInstance;
			if (cc != null) {
				cc.Conn.SendPacketGroup(RequestServersToStartPacket.group);
			}
		}

		private void menuRestartAuxServer_Click(object sender, EventArgs e) {
			ConsoleClient.SendCommand(0, "restart");
		}

		private void systemTabPage_Load(object sender, EventArgs e) {

		}

		private void MainForm_Load(object sender, EventArgs e) {

		}

		internal void DelayReconnect(EndPointSetting eps) {
			this.Invoke(new Action<EndPointSetting>(this.InvokedDelayReconnect), eps);
		}

		private void InvokedDelayReconnect(EndPointSetting ipe) {
			this.epsBeingReconnected = ipe;
			//this.reconnectingTimer.Stop();
			this.reconnectingTimer.Start();
		}

		private void reconnectingTimer_Tick(object sender, EventArgs e) {
			this.reconnectingTimer.Stop();

			Console.WriteLine(String.Concat("Reconnecting to ", 
				this.epsBeingReconnected.UserName,  "@", this.epsBeingReconnected.Address, ":" ,
				this.epsBeingReconnected.Port.ToString(System.Globalization.CultureInfo.InvariantCulture)));
			ConsoleClient.Connect(this.epsBeingReconnected);
		}

		private void packetHandlingTimer_Tick(object sender, EventArgs e) {
			ConsoleIncomingPacket.HandleQueuedPackets();
		}
	}
}