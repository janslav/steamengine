using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using SteamEngine.Communication;

namespace SteamEngine.RemoteConsole {
	public partial class MainForm : Form {
		Dictionary<int, CommandLineDisplay> displays = new Dictionary<int, CommandLineDisplay>();


		public MainForm() {
			InitializeComponent();
		}

		private void menuExit_Click(object sender, EventArgs e) {
			this.Close();
		}

		private void MainForm_Load(object sender, EventArgs e) {

		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e) {

		}

		public ILogStrDisplay SystemDisplay {
			get {
				return this.systemTabPage;
			}
		}

		public void AddCmdLineDisplay(int id, string name) {
			TabPage tab = new TabPage(name);
			CommandLineDisplay cld = new CommandLineDisplay(name, id);
			tab.Controls.Add(cld);
			cld.Dock = DockStyle.Fill;
			this.tabControl.Controls.Add(tab);

			RemoveCmdLineDisplay(id);
			this.displays.Add(id, cld);
		}

		public void RemoveCmdLineDisplay(int id) {
			CommandLineDisplay cld;
			if (this.displays.TryGetValue(id, out cld)) {
				cld.Parent.Dispose();

				this.displays.Remove(id);
			}
		}

		public void ClearCmdLineDisplays() {
			Control[] controls = new Control[this.tabControl.Controls.Count];
			this.tabControl.Controls.CopyTo(controls, 0);
			foreach (Control ctrl in controls) {
				if ((ctrl != this.systemTabPage) && (ctrl is TabPage)) {
					ctrl.Dispose();
				}
			}
		}

		private void menuConnect_Click(object sender, EventArgs e) {
			ConnectionForm cf = new ConnectionForm();
			cf.ShowDialog();
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

		internal void SetConnected(bool connected) {
			this.menuConnect.Enabled = !connected;
			this.menuDisconnect.Enabled = connected;
		}

		private void menuDisconnect_Click(object sender, EventArgs e) {
			ConsoleClient.Disconnect("Disconnect menu item used.");
		}
	}
}