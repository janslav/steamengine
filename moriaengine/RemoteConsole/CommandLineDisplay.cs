using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace SteamEngine.RemoteConsole {
	public partial class CommandLineDisplay : UserControl {
		int id;

		public CommandLineDisplay(string name, int id) {
			this.Name = name;
			this.id = id;

			InitializeComponent();
		}

		private void txtCommandLine_KeyPress(object sender, KeyPressEventArgs e) {
			if (e.KeyChar=='\r') {
				string cmd=txtCommandLine.Text;
				txtCommandLine.Items.Remove(cmd);

				//if (txtCommandLine.Items.Count>maxCmdHistory)
				//    txtCommandLine.Items.RemoveAt(txtCommandLine.Items.Count-1);
				txtCommandLine.Items.Insert(0, cmd);

				if (cmd.Trim()!="") {
					ConsoleClient.SendCommand(this.id, cmd);
					txtCommandLine.Text="";
				}
				e.Handled=true;
			} else if (e.KeyChar==27) {
				txtCommandLine.Text="";
				e.Handled=true;
			}
		}
	}
}
