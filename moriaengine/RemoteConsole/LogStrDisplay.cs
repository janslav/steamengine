using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Runtime;
using System.Runtime.InteropServices;

using SteamEngine.Common;

namespace SteamEngine.RemoteConsole {
	public partial class LogStrDisplay : UserControl, ILogStrDisplay {
		string title;
		string defaultTitle;

		LogStrParser parser;

		delegate void WriteDeferredDelegate(string str);
		WriteDeferredDelegate writeDeferred;

		public LogStrDisplay() {
			InitializeComponent();
			writeDeferred = this.WriteDeferred;
			this.parser = new LogStrParser(this);
			this.txtBox.LinkClicked += new LinkClickedEventHandler(txtBox_LinkClicked);
		}

		void txtBox_LinkClicked(object sender, LinkClickedEventArgs e) {
			string filename;
			int line;
			if (!LogStrParser.TryParseFileLine(e.LinkText, out filename, out line)) {
				filename = e.LinkText;
			}
			string ext = System.IO.Path.GetExtension(filename);

			filename = LogStrParser.TranslateToLocalPath(filename);

			string exe, args;
			if (Settings.GetCommandLineForExt(ext, out exe, out args)) {
				args = String.Format(args, filename, line);
				System.Diagnostics.Process.Start(exe, args);
			} else {
				System.Diagnostics.Process.Start(filename);
			}
		}

		private const int WM_VSCROLL = 0x115;
		private const int SB_BOTTOM = 7;

		[DllImportAttribute("user32.dll")]
		private static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, Int32 lParam);

		private void ScrollToEnd() {
			SendMessage(this.txtBox.Handle, WM_VSCROLL, SB_BOTTOM, 0);
		}

		public void Write(string logStrEncoded) {
			this.Invoke(this.writeDeferred, logStrEncoded);
		}

		public void WriteLine(string logStrEncoded) {
			this.Invoke(this.writeDeferred, logStrEncoded + Environment.NewLine);
		}

		public void Write(SteamEngine.Common.LogStr data) {
			this.Invoke(this.writeDeferred, data.RawString);
		}

		private void WriteDeferred(string str) {
			this.parser.ProcessLogStr(str);

			if (this.chckAutoScroll.Checked) {
				this.ScrollToEnd();
			}
		}

		public string Title {
			get {
				if (string.IsNullOrEmpty(this.title)) {
					return this.defaultTitle;
				}
				return this.title;
			}
		}

		public string DefaultTitle {
			get {
				return this.defaultTitle;
			}
			set {
				this.defaultTitle = value;
				if (string.IsNullOrEmpty(this.title)) {
					this.OnTitleChanged();
				}
			}
		}

		public void Write(string data, LogStyleInfo style) {
			int dataLen = data.Length;
			if (dataLen > 0) {
				//while (this.txtBox.TextLength + dataLen > this.txtBox.MaxLength) {
				//    this.txtBox.Text = this.txtBox.Text.Substring(this.txtBox.Text.IndexOf(Environment.NewLine) + Environment.NewLine.Length);
				//}

				this.txtBox.SelectionFont = style.font;
				this.txtBox.SelectionColor = style.textColor;
				if (style.isLink) {
					this.txtBox.InsertLink(data);
				} else {
					this.txtBox.AppendText(data);
				}
			}
		}

		public void SetTitle(string data) {
			this.title = data;
			this.OnTitleChanged();
		}

		public void SetTitleToDefault() {
			if (!string.IsNullOrEmpty(this.title)) {
				this.title = null;
				this.OnTitleChanged();
			}
		}

		public event EventHandler TitleChanged;

		protected virtual void OnTitleChanged() {
			EventHandler handler = this.TitleChanged;
			if (handler != null) {
				handler(this, EventArgs.Empty);
			}
		}
	}
}




