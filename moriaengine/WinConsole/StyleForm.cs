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
using System.Drawing;
using System.Drawing.Text;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using SteamEngine.Common;

namespace SteamClients {
	public class StyleForm : System.Windows.Forms.Form {
		private System.Windows.Forms.ListBox stylesList;
		private System.Windows.Forms.CheckBox boldCheckBox;
		private System.Windows.Forms.CheckBox italicCheckBox;
		private System.Windows.Forms.CheckBox strikeoutCheckBox;
		private System.Windows.Forms.CheckBox underlineCheckBox;
		private System.Windows.Forms.ComboBox colorCombo;
		private System.Windows.Forms.ComboBox fontCombo;
		private System.Windows.Forms.ComboBox sizeCombo;
		private System.Windows.Forms.Label testText;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnReset;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.ComponentModel.Container components=null;

		private string[] predefinedColors={"Black","White","Red","Green","Blue","LightGreen",
														 "LightBlue","DarkRed","DarkGreen","DarkBlue","Maroon",
														 "Yellow","Cyan","Purple","LightGray","Gray",
														 "DarkGray","Magenta","Aquamarine","Olive","Orange"};

		private const GenericFontFamilies defaultFontFamily=GenericFontFamilies.SansSerif;
		private static readonly Color defaultColor=Color.Black;
		private const float defaultSize=10;
		private const LogStyles defaultSelectedStyle=LogStyles.Default;
		private bool updateEnabled;
		private bool updateAttrs;
		private ConAttrs conAttrs;

		#region Properties
		private LogStyles SelectedStyle {
			get {
				if (stylesList.SelectedIndex>=0 && stylesList.Items[stylesList.SelectedIndex] is LogStyles)
					return ((LogStyles)stylesList.Items[stylesList.SelectedIndex]);
				return defaultSelectedStyle;
			}
		}

		public bool UpdateAttributes {
			get {return updateAttrs;}
		}
		public ConAttrs ConsoleAttributes {
			get {return conAttrs;}
		}
		#endregion

		#region Init
		public StyleForm(ConAttrs attrs) {
			updateEnabled=true;
			updateAttrs=false;
			conAttrs=attrs;

			InitializeComponent();
			InitCombos();  // initialize combos first(!)
			InitLists();
		}

		private void InitializeComponent() {
			this.stylesList = new System.Windows.Forms.ListBox();
			this.colorCombo = new System.Windows.Forms.ComboBox();
			this.boldCheckBox = new System.Windows.Forms.CheckBox();
			this.italicCheckBox = new System.Windows.Forms.CheckBox();
			this.strikeoutCheckBox = new System.Windows.Forms.CheckBox();
			this.underlineCheckBox = new System.Windows.Forms.CheckBox();
			this.fontCombo = new System.Windows.Forms.ComboBox();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.sizeCombo = new System.Windows.Forms.ComboBox();
			this.testText = new System.Windows.Forms.Label();
			this.btnReset = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// stylesList
			// 
			this.stylesList.Location = new System.Drawing.Point(8, 24);
			this.stylesList.Name = "stylesList";
			this.stylesList.Size = new System.Drawing.Size(112, 108);
			this.stylesList.TabIndex = 0;
			this.stylesList.SelectedIndexChanged += new System.EventHandler(this.stylesListSelectedIndexChanged);
			// 
			// colorCombo
			// 
			this.colorCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.colorCombo.Location = new System.Drawing.Point(128, 24);
			this.colorCombo.Name = "colorCombo";
			this.colorCombo.Size = new System.Drawing.Size(121, 21);
			this.colorCombo.TabIndex = 1;
			this.colorCombo.SelectedIndexChanged += new System.EventHandler(this.TextStyleChanged);
			// 
			// boldCheckBox
			// 
			this.boldCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.boldCheckBox.Location = new System.Drawing.Point(296, 8);
			this.boldCheckBox.Name = "boldCheckBox";
			this.boldCheckBox.Size = new System.Drawing.Size(80, 24);
			this.boldCheckBox.TabIndex = 2;
			this.boldCheckBox.Text = "Bold";
			this.boldCheckBox.CheckedChanged += new System.EventHandler(this.TextStyleChanged);
			// 
			// italicCheckBox
			// 
			this.italicCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.italicCheckBox.Location = new System.Drawing.Point(296, 32);
			this.italicCheckBox.Name = "italicCheckBox";
			this.italicCheckBox.Size = new System.Drawing.Size(80, 24);
			this.italicCheckBox.TabIndex = 3;
			this.italicCheckBox.Text = "Italic";
			this.italicCheckBox.CheckedChanged += new System.EventHandler(this.TextStyleChanged);
			// 
			// strikeoutCheckBox
			// 
			this.strikeoutCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.strikeoutCheckBox.Location = new System.Drawing.Point(296, 80);
			this.strikeoutCheckBox.Name = "strikeoutCheckBox";
			this.strikeoutCheckBox.Size = new System.Drawing.Size(80, 24);
			this.strikeoutCheckBox.TabIndex = 4;
			this.strikeoutCheckBox.Text = "Strikeout";
			this.strikeoutCheckBox.CheckedChanged += new System.EventHandler(this.TextStyleChanged);
			// 
			// underlineCheckBox
			// 
			this.underlineCheckBox.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.underlineCheckBox.Location = new System.Drawing.Point(296, 56);
			this.underlineCheckBox.Name = "underlineCheckBox";
			this.underlineCheckBox.Size = new System.Drawing.Size(80, 24);
			this.underlineCheckBox.TabIndex = 5;
			this.underlineCheckBox.Text = "Underline";
			this.underlineCheckBox.CheckedChanged += new System.EventHandler(this.TextStyleChanged);
			// 
			// fontCombo
			// 
			this.fontCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.fontCombo.Location = new System.Drawing.Point(128, 64);
			this.fontCombo.Name = "fontCombo";
			this.fontCombo.Size = new System.Drawing.Size(160, 21);
			this.fontCombo.TabIndex = 6;
			this.fontCombo.SelectedIndexChanged += new System.EventHandler(this.TextStyleChanged);
			// 
			// btnOK
			// 
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnOK.Location = new System.Drawing.Point(136, 216);
			this.btnOK.Name = "btnOK";
			this.btnOK.TabIndex = 9;
			this.btnOK.Text = "OK";
			this.btnOK.Click += new System.EventHandler(this.btnOKClick);
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnCancel.Location = new System.Drawing.Point(216, 216);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.TabIndex = 10;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancelClick);
			// 
			// sizeCombo
			// 
			this.sizeCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.sizeCombo.Location = new System.Drawing.Point(128, 104);
			this.sizeCombo.Name = "sizeCombo";
			this.sizeCombo.Size = new System.Drawing.Size(56, 21);
			this.sizeCombo.TabIndex = 11;
			this.sizeCombo.SelectedIndexChanged += new System.EventHandler(this.TextStyleChanged);
			// 
			// testText
			// 
			this.testText.BackColor = System.Drawing.SystemColors.Window;
			this.testText.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.testText.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.testText.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(238)));
			this.testText.Location = new System.Drawing.Point(8, 144);
			this.testText.Name = "testText";
			this.testText.Size = new System.Drawing.Size(368, 64);
			this.testText.TabIndex = 12;
			this.testText.Text = "Steamengine";
			this.testText.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// btnReset
			// 
			this.btnReset.Location = new System.Drawing.Point(296, 216);
			this.btnReset.Name = "btnReset";
			this.btnReset.TabIndex = 13;
			this.btnReset.Text = "Reset";
			this.btnReset.Click += new System.EventHandler(this.btnResetClick);
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(128, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(96, 16);
			this.label1.TabIndex = 14;
			this.label1.Text = "Foreground Color:";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(128, 48);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(32, 16);
			this.label2.TabIndex = 15;
			this.label2.Text = "Font:";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(128, 88);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(32, 16);
			this.label3.TabIndex = 16;
			this.label3.Text = "Size:";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(24, 8);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(80, 16);
			this.label4.TabIndex = 17;
			this.label4.Text = "Message style:";
			// 
			// StyleForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(386, 247);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.btnReset);
			this.Controls.Add(this.testText);
			this.Controls.Add(this.sizeCombo);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.fontCombo);
			this.Controls.Add(this.underlineCheckBox);
			this.Controls.Add(this.strikeoutCheckBox);
			this.Controls.Add(this.italicCheckBox);
			this.Controls.Add(this.boldCheckBox);
			this.Controls.Add(this.colorCombo);
			this.Controls.Add(this.stylesList);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "StyleForm";
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Message Styles";
			this.ResumeLayout(false);

		}

		private void InitLists() {
			// fullfill style list
			Array styles=Enum.GetValues(typeof(LogStyles));
			IEnumerator ie=styles.GetEnumerator();
			while (ie.MoveNext()) {
				if (ie.Current is LogStyles)
					stylesList.Items.Add((LogStyles)ie.Current);
			}
			if (stylesList.Items.Count>0)
				stylesList.SelectedIndex=0;
		}

		private void InitCombos() {
			int i;

			// fullfill size combo
			sizeCombo.Items.Add(6f);
			sizeCombo.Items.Add(7f);
			sizeCombo.Items.Add(8f);
			sizeCombo.Items.Add(8.25f);
			for (i=9;i<=24;i++)
				sizeCombo.Items.Add((float)i);
			for (i=28;i<=36;i+=4)
				sizeCombo.Items.Add((float)i);

			// fullfill font family combo
			FontFamily[] families=FontFamily.Families;
			foreach (FontFamily family in families) {
				fontCombo.Items.Add(family.GetName(0));
			}

			// fullfill color combo
			foreach (string color in predefinedColors) {
				colorCombo.Items.Add(color);
			}
		}
		#endregion

		protected override void Dispose(bool disposing) {
			if (disposing) {
				if(components!=null) {
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		private void UpdateStyle() {
			if (!updateEnabled)
				return;

			FontStyle style=FontStyle.Regular;
			FontFamily family=new FontFamily(defaultFontFamily);
			Color color=defaultColor;
			float size=defaultSize;
			
			if (boldCheckBox.Checked)
				style|=FontStyle.Bold;
			if (italicCheckBox.Checked)
				style|=FontStyle.Italic;
			if (strikeoutCheckBox.Checked)
				style|=FontStyle.Strikeout;
			if (underlineCheckBox.Checked)
				style|=FontStyle.Underline;

			try {
				if (fontCombo.SelectedItem is string)
					family=new FontFamily((string)fontCombo.SelectedItem);
				if (colorCombo.SelectedItem is string)
					color=Color.FromName((string)colorCombo.SelectedItem);
				if (sizeCombo.SelectedItem is float)
					size=(float)sizeCombo.SelectedItem;
			}
			catch {
			}

			testText.ForeColor=color;
			testText.Font=new Font(family,size,style);

			conAttrs.SetStyle(SelectedStyle,color,style,family,size);
		}

		#region Events
		private void TextStyleChanged(object sender,System.EventArgs e) {
			UpdateStyle();
		}

		private void stylesListSelectedIndexChanged(object sender, System.EventArgs e) {
			if (stylesList.SelectedIndex<0)
				return;

			updateEnabled=false;
			LogStyles style;
			int idx;
			if (stylesList.Items[stylesList.SelectedIndex] is LogStyles) {
				style=(LogStyles)stylesList.Items[stylesList.SelectedIndex];
				idx=sizeCombo.Items.IndexOf(conAttrs.GetSize(style));
				if (idx>=0) {
					sizeCombo.SelectedIndex=idx;
				} else {
					sizeCombo.SelectedIndex=sizeCombo.Items.Add(conAttrs.GetSize(style));
				}
				idx=fontCombo.Items.IndexOf(conAttrs.GetFontFamilyName(style));
				if (idx>=0) {
					fontCombo.SelectedIndex=idx;
				} else {
					fontCombo.SelectedIndex=fontCombo.Items.Add(conAttrs.GetFontFamilyName(style));
				}
				idx=colorCombo.Items.IndexOf(conAttrs.GetColor(style).Name);
				if (idx>=0) {
					colorCombo.SelectedIndex=idx;
				} else {
					colorCombo.SelectedIndex=colorCombo.Items.Add(conAttrs.GetColor(style).Name);
				}

				FontStyle fnt=conAttrs.GetFontStyle(style);
				boldCheckBox.Checked=(fnt&FontStyle.Bold)==FontStyle.Bold;
				italicCheckBox.Checked=(fnt&FontStyle.Italic)==FontStyle.Italic;
				underlineCheckBox.Checked=(fnt&FontStyle.Underline)==FontStyle.Underline;
				strikeoutCheckBox.Checked=(fnt&FontStyle.Strikeout)==FontStyle.Strikeout;
			}

			updateEnabled=true;
			UpdateStyle();
		}

		private void btnOKClick(object sender,System.EventArgs e) {
			updateAttrs=true;
			Close();
		}

		private void btnCancelClick(object sender,System.EventArgs e) {
			updateAttrs=false;
			Close();
		}

		private void btnResetClick(object sender,System.EventArgs e) {
			conAttrs.DefaultSettings();

			// update combos and example
			if (stylesList.Items.Count>0) {
				int idx=stylesList.SelectedIndex;
				stylesList.SelectedIndex=-1;
				if (idx>=0) {
					stylesList.SelectedIndex=idx;
				} else {
					stylesList.SelectedIndex=0;
				}
			}
		}
		#endregion
	}
}

