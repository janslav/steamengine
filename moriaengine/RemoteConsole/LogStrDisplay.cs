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

		Dictionary<LogStyles, Font> fonts = new Dictionary<LogStyles, Font>();
		Dictionary<LogStyles, Color> colors = new Dictionary<LogStyles, Color>();

		public LogStrDisplay() {
			InitializeComponent();
			writeDeferred = this.WriteDeferred;
			this.parser = new LogStrParser(this);
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
			this.Invoke(this.writeDeferred, logStrEncoded+Environment.NewLine);
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

		private static ConAttrs conAttrs = new ConAttrs();

		public void Write(string data, LogStyles style) {
			int dataLen = data.Length;
			if (dataLen > 0) {
				SetFontAndColor(style);

				while (this.txtBox.TextLength + dataLen > this.txtBox.MaxLength) {
					this.txtBox.Text = this.txtBox.Text.Substring(this.txtBox.Text.IndexOf(Environment.NewLine) + Environment.NewLine.Length);
				}
				this.txtBox.AppendText(data);
			}
		}

		private void SetFontAndColor(LogStyles style) {
			Font f;
			if (!this.fonts.TryGetValue(style, out f)) {
				f = new Font(conAttrs.GetFontFamily(style),conAttrs.GetSize(style),conAttrs.GetFontStyle(style));
				this.fonts[style] = f;
			}
			this.txtBox.SelectionFont = f;


			Color color = conAttrs.GetColor(style);
			this.txtBox.SelectionColor = color;
			
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




