using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace SteamEngine.RemoteConsole {
	public partial class LogStrDisplay : UserControl, ILogStrDisplay {

		delegate void WriteDeferredDelegate(string str);
		WriteDeferredDelegate writeDeferred;

		public LogStrDisplay() {
			InitializeComponent();
			writeDeferred = this.WriteDeferred;
		}

		public void WriteLine(string data) {
			this.Invoke(this.writeDeferred, data + Environment.NewLine);
		}

		public void WriteLine(SteamEngine.Common.LogStr data) {
			this.Invoke(this.writeDeferred, data.RawString + Environment.NewLine);
		}

		public void Write(string data) {
			this.Invoke(this.writeDeferred, data);
		}

		public void Write(SteamEngine.Common.LogStr data) {
			this.Invoke(this.writeDeferred, data.RawString);
		}

		private void WriteDeferred(string str) {
			this.txtBox.Text += str;
		}
	}
}
