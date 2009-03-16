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

		private const string contractedSign = "[+]";

		List<string> contractedTexts = new List<string>();

		public LogStrDisplay() {
			InitializeComponent();
			writeDeferred = this.WriteDeferred;
			this.parser = new LogStrParser(this);
			this.txtBox.LinkClicked += new LinkClickedEventHandler(txtBox_LinkClicked);
		}

		void txtBox_LinkClicked(object sender, LinkClickedEventArgs e) {
			string filename;
			int line;
			if (this.TryUncontract(e.LinkText)) {
				return;
			}

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
			//this.txtBox.BeginUpdate();
			int prevLen = this.txtBox.Text.Length;
			this.parser.ProcessLogStr(str);

			if (this.chckAutoScroll.Checked) {
				this.txtBox.ScrollToBottom();
			}

			if (this.chkContract.Checked) {				
				this.TryContract(prevLen);				
			}
			//this.txtBox.EndUpdate();
		}

		private void TryContract(int prevLen) {
			string data = this.txtBox.Text.Substring(prevLen);
			int firstNextLine;
			int firstN = data.IndexOf('\n');
			int firstR = data.IndexOf('\r');
			if (firstN == -1) {
				firstNextLine = firstR;
			} else if (firstR == -1) {
				firstNextLine = firstN;
			} else {
				firstNextLine = Math.Min(firstN, firstR);
			}

			if (firstNextLine > -1) { //data is multiline. We want to contract it
				int start = prevLen + firstNextLine;
				int len = data.Length - firstNextLine;
				this.txtBox.Select(start, len);
				if (this.txtBox.SelectedText.Trim().Length > 0) {
					string rtf = this.txtBox.SelectedRtf;
					this.contractedTexts.Add(rtf);
					this.txtBox.SelectedText = " ";
					this.txtBox.InsertLink(contractedSign, String.Concat(this.contractedTexts.Count - 1));
					this.txtBox.AppendText(Environment.NewLine);
				}
			}
		}

		private bool TryUncontract(string linkText) {
			if (linkText.StartsWith(contractedSign+"#")) {
				string indexStr = linkText.Substring(4);
				int i = int.Parse(indexStr);
				string rtf = this.contractedTexts[i];
				this.contractedTexts[i] = null;

				string contractedSignDecorated = " " + contractedSign;
				int signLen = contractedSignDecorated.Length;
				int selectionStart = this.txtBox.Text.IndexOf(contractedSignDecorated, this.txtBox.CurrentMouseCharIndex - signLen);
				this.txtBox.Select(selectionStart, signLen + 1 + Environment.NewLine.Length);
				this.txtBox.SelectedText = " "; //erases the [+]

				if (!string.IsNullOrEmpty(rtf)) {
					this.txtBox.SelectedRtf = rtf;
				}
				return true;
			}
			return false;
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

				this.txtBox.SelectionFont = style.Font;
				this.txtBox.SelectionColor = style.TextColor;
				if (style.IsLink) {
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

		private void btnClear_Click(object sender, EventArgs e) {
			this.ClearText();
		}

		public void ClearText() {
			this.txtBox.Clear();
			this.contractedTexts.Clear();
		}
	}
}




