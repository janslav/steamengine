/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;

namespace SteamClients
{
	/// <summary>
	/// Summary description for FileView.
	/// </summary>
	public class FileViewForm : System.Windows.Forms.Form
	{
		private System.Windows.Forms.Label labelPosition;
		private System.Windows.Forms.RichTextBox richTextView;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public FileViewForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.labelPosition = new System.Windows.Forms.Label();
			this.richTextView = new System.Windows.Forms.RichTextBox();
			this.SuspendLayout();
			// 
			// labelPosition
			// 
			this.labelPosition.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.labelPosition.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.labelPosition.Location = new System.Drawing.Point(540, 436);
			this.labelPosition.Name = "labelPosition";
			this.labelPosition.Size = new System.Drawing.Size(88, 16);
			this.labelPosition.TabIndex = 1;
			this.labelPosition.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// richTextView
			// 
			this.richTextView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
				| System.Windows.Forms.AnchorStyles.Left) 
				| System.Windows.Forms.AnchorStyles.Right)));
			this.richTextView.Location = new System.Drawing.Point(0, 0);
			this.richTextView.Name = "richTextView";
			this.richTextView.ReadOnly = true;
			this.richTextView.Size = new System.Drawing.Size(632, 432);
			this.richTextView.TabIndex = 2;
			this.richTextView.Text = "";
			this.richTextView.WordWrap = false;
			this.richTextView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnKeyDown);
			this.richTextView.SelectionChanged += new System.EventHandler(this.richTextSelectionChanged);
			// 
			// FileViewForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(632, 453);
			this.Controls.Add(this.richTextView);
			this.Controls.Add(this.labelPosition);
			this.Name = "FileViewForm";
			this.Text = "View";
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnKeyDown);
			this.ResumeLayout(false);

		}
		#endregion

		private void OnKeyDown(object sender,System.Windows.Forms.KeyEventArgs e) {
			if (e.KeyCode==Keys.Escape) {
				this.Close();
			}
		}

		public void ShowLine(int line) {
			labelPosition.Text="Please wait";
			if (line>richTextView.Lines.Length) {
				Debug.WriteLine("FileViewForm.ShowLine: richTextView doesn't contain line "+line.ToString());
				line=richTextView.Lines.Length;
			}
			int i=0;
			int pos=0;
			while(i<line) {
				pos+=richTextView.Lines[i].Length+1;
				i++;
			}
			richTextView.SelectionStart=pos;
			richTextView.SelectionLength=0;
			UpdatePosition();
		}

		public void ShowFile(string name,int line) {
			try {
				StreamReader reader=new StreamReader(name);
				long len=reader.BaseStream.Length;
				if (len>int.MaxValue) {
					throw new Exception("File is too long");
				}
				char[] buf=new char[len];
				reader.Read(buf,0,(int)len);
				string text=new string(buf);
				richTextView.Text=text;
				ShowLine(line);
				UpdatePosition();
				this.Text="View: "+name;
				reader.Close();
			}
			catch (Exception e) {
				MessageBox.Show(this,"Cannot read file "+name+Environment.NewLine+e.Message);
			}
		}

		private void UpdatePosition() {
			int line=richTextView.GetLineFromCharIndex(richTextView.SelectionStart);
			labelPosition.Text="Ln "+line.ToString();
		}

		private void richTextSelectionChanged(object sender,System.EventArgs e) {
			UpdatePosition();
		}
	}
}
