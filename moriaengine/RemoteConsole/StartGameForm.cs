using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using SteamEngine.Common;

namespace SteamEngine.RemoteConsole {
	public partial class StartGameForm : Form {
		private SendServersToStartPacket.GameServerEntry[] entries;

		public StartGameForm(SendServersToStartPacket.GameServerEntry[] entries) {
			this.entries = entries;

			InitializeComponent();
		}

		private void StartGameForm_Load(object sender, EventArgs e) {
			this.ddBuild.DataSource = EnumItem<SEBuild>.GetAllItemsAsList();

			this.gameServerEntryBindingSource.DataSource = this.entries;
		}

		private void btnStart_Click(object sender, EventArgs e) {

		}

		private void gameServerEntryBindingSource_CurrentChanged(object sender, EventArgs e) {
			SendServersToStartPacket.GameServerEntry entry = (SendServersToStartPacket.GameServerEntry)
				this.gameServerEntryBindingSource.Current;

			this.btnStart.Enabled = !entry.Running;
		}
	}
}