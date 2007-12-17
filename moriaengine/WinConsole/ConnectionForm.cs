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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

namespace SteamClients {
	/// <summary>
	/// Summary description for ConnectionForm.
	/// </summary>
	public class ConnectionForm : System.Windows.Forms.Form {
		private const string defaultNewName="New connection";
		private const string defaultNewAddress="localhost";
		private const int defaultNewPort=2594;
		private const string defaultNewPassword="";
		private const string defaultNewUserName="";

		private System.Windows.Forms.ComboBox comboName;
		private System.Windows.Forms.TextBox textAddress;
		private System.Windows.Forms.TextBox textPort;
		private System.Windows.Forms.TextBox textPassword;
		private System.Windows.Forms.TextBox textUserName;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnConnect;
		private System.Windows.Forms.Button btnAddConnection;
		private System.Windows.Forms.Button btnRemove;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private int oldComboNameIndex;
		private bool changeEnabled;
		private bool shouldConnect;
		private bool savePasswords;
		private System.Windows.Forms.CheckBox checkSavePassword;
		private SteamConnection selectedConnection;

		public bool SavePasswords {
			get { return savePasswords; }
		}

		public bool ShouldConnect {
			get { return shouldConnect; }
		}

		public SteamConnection SelectedConnection {
			get { return selectedConnection; }
		}

		public void ExportConnections(ref List<SteamConnection> conlist) {
			conlist.Clear();
			if (comboName==null || comboName.Items.Count<=0) {
				return;
			}
			foreach (object obj in comboName.Items) {
				//Debug.Assert(obj is SteamConnection,"comboName.Items contains can contain only SteamConnection objects");
				conlist.Add((SteamConnection) obj);
			}
		}

		public void ImportConnections(List<SteamConnection> cons) {
			if (cons.Count>0) {
				SuspendLayout();
				changeEnabled=false;
				ClearInput();
				comboName.Items.Clear();
				foreach (object obj in cons) {
					//Debug.Assert(obj is SteamConnection,"only SteamConection objects can be imported");
					comboName.Items.Add((SteamConnection) obj);
				}
				SetInput(true);
				changeEnabled=true;
				ResumeLayout();
				oldComboNameIndex=-1;
				comboName.SelectedIndex=0;
			} else {
				ClearInput();
				comboName.Items.Clear();
				SetInput(false);
			}
		}

		public ConnectionForm(bool save_password) {
			InitializeComponent();
			oldComboNameIndex=-1;
			changeEnabled=true;
			btnRemove.Enabled=false;
			shouldConnect=false;
			savePasswords=save_password;
			checkSavePassword.Checked=save_password;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing) {
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.comboName = new System.Windows.Forms.ComboBox();
			this.textAddress = new System.Windows.Forms.TextBox();
			this.textPort = new System.Windows.Forms.TextBox();
			this.textUserName = new System.Windows.Forms.TextBox();
			this.textPassword = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnConnect = new System.Windows.Forms.Button();
			this.btnAddConnection = new System.Windows.Forms.Button();
			this.btnRemove = new System.Windows.Forms.Button();
			this.checkSavePassword = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			// 
			// comboName
			// 
			this.comboName.Enabled = false;
			this.comboName.Location = new System.Drawing.Point(104, 8);
			this.comboName.Name = "comboName";
			this.comboName.Size = new System.Drawing.Size(272, 21);
			this.comboName.TabIndex = 0;
			this.comboName.DropDown += new System.EventHandler(this.comboNameDropDown);
			this.comboName.SelectedIndexChanged += new System.EventHandler(this.comboNameSelectedIndexChanged);
			// 
			// textAddress
			// 
			this.textAddress.Enabled = false;
			this.textAddress.Location = new System.Drawing.Point(8, 56);
			this.textAddress.Name = "textAddress";
			this.textAddress.Size = new System.Drawing.Size(304, 20);
			this.textAddress.TabIndex = 1;
			this.textAddress.Text = "";
			// 
			// textPort
			// 
			this.textPort.Enabled = false;
			this.textPort.Location = new System.Drawing.Point(328, 56);
			this.textPort.Name = "textPort";
			this.textPort.Size = new System.Drawing.Size(48, 20);
			this.textPort.TabIndex = 2;
			this.textPort.Text = "";
			this.textPort.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textPortKeyPress);
			// 
			// textUserName
			// 
			this.textUserName.Enabled = false;
			this.textUserName.Location = new System.Drawing.Point(8, 96);
			this.textUserName.Name = "textUserName";
			this.textUserName.Size = new System.Drawing.Size(168, 20);
			this.textUserName.TabIndex = 3;
			this.textUserName.Text = "";
			// 
			// textPassword
			// 
			this.textPassword.Enabled = false;
			this.textPassword.Location = new System.Drawing.Point(208, 96);
			this.textPassword.Name = "textPassword";
			this.textPassword.PasswordChar = '*';
			this.textPassword.Size = new System.Drawing.Size(168, 20);
			this.textPassword.TabIndex = 4;
			this.textPassword.Text = "";
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(96, 16);
			this.label1.TabIndex = 2;
			this.label1.Text = "Connection name:";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(8, 32);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(56, 24);
			this.label2.TabIndex = 3;
			this.label2.Text = "Address:";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(328, 32);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(48, 24);
			this.label3.TabIndex = 3;
			this.label3.Text = "Port:";
			this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(8, 80);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(80, 16);
			this.label4.TabIndex = 3;
			this.label4.Text = "Username:";
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(208, 80);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(80, 16);
			this.label5.TabIndex = 3;
			this.label5.Text = "Password:";
			// 
			// btnCancel
			// 
			this.btnCancel.Location = new System.Drawing.Point(296, 160);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(80, 23);
			this.btnCancel.TabIndex = 8;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancelClick);
			// 
			// btnConnect
			// 
			this.btnConnect.Enabled = false;
			this.btnConnect.Location = new System.Drawing.Point(208, 160);
			this.btnConnect.Name = "btnConnect";
			this.btnConnect.Size = new System.Drawing.Size(80, 23);
			this.btnConnect.TabIndex = 7;
			this.btnConnect.Text = "Connect";
			this.btnConnect.Click += new System.EventHandler(this.btnConnectClick);
			// 
			// btnAddConnection
			// 
			this.btnAddConnection.Location = new System.Drawing.Point(8, 160);
			this.btnAddConnection.Name = "btnAddConnection";
			this.btnAddConnection.Size = new System.Drawing.Size(80, 23);
			this.btnAddConnection.TabIndex = 5;
			this.btnAddConnection.Text = "Add";
			this.btnAddConnection.Click += new System.EventHandler(this.btnAddConnectionClick);
			// 
			// btnRemove
			// 
			this.btnRemove.Location = new System.Drawing.Point(96, 160);
			this.btnRemove.Name = "btnRemove";
			this.btnRemove.Size = new System.Drawing.Size(80, 23);
			this.btnRemove.TabIndex = 6;
			this.btnRemove.Text = "Remove";
			this.btnRemove.Click += new System.EventHandler(this.btnRemoveClick);
			// 
			// checkSavePassword
			// 
			this.checkSavePassword.Location = new System.Drawing.Point(8, 128);
			this.checkSavePassword.Name = "checkSavePassword";
			this.checkSavePassword.Size = new System.Drawing.Size(136, 24);
			this.checkSavePassword.TabIndex = 9;
			this.checkSavePassword.Text = "Save passwords";
			// 
			// ConnectionForm
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(386, 191);
			this.Controls.Add(this.checkSavePassword);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.textAddress);
			this.Controls.Add(this.textPort);
			this.Controls.Add(this.textUserName);
			this.Controls.Add(this.textPassword);
			this.Controls.Add(this.comboName);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.btnConnect);
			this.Controls.Add(this.btnAddConnection);
			this.Controls.Add(this.btnRemove);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ConnectionForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Connect";
			this.Closing += new System.ComponentModel.CancelEventHandler(this.ConnectionFormClosing);
			this.ResumeLayout(false);

		}
		#endregion

		private void AddNewConnection() {
			changeEnabled=false;
			SteamConnection item=new SteamConnection();
			item.Name=defaultNewName;
			item.Address=defaultNewAddress;
			item.Port=defaultNewPort;
			item.Password=defaultNewPassword;
			item.UserName=defaultNewUserName;
			int idx=comboName.Items.Add(item);
			SaveValuesTo(oldComboNameIndex);
			LoadValuesFrom(idx);
			comboName.SelectedIndex=idx;
			changeEnabled=true;
			if (comboName.Items.Count>0) {
				SetInput(true);
			}
			comboName.Focus();
		}

		private void btnAddConnectionClick(object sender, System.EventArgs e) {
			AddNewConnection();
		}

		private void LoadValuesFrom(int idx) {
			changeEnabled=false;
			if (idx<0 && comboName.Items.Count>idx)
				return;
			Debug.Assert(comboName.Items[idx] is SteamConnection, "comboName doesn't contain SteamConnection at index "+idx);
			SteamConnection con=(SteamConnection) comboName.Items[idx];
			comboName.Text=con.Name;
			textAddress.Text=con.Address;
			textPort.Text=con.Port.ToString();
			textPassword.Text=con.Password;
			textUserName.Text=con.UserName;
			changeEnabled=true;
		}

		private void SaveValuesTo(int idx) {
			if (idx<0) {
				return;
			}
			changeEnabled=false;
			Debug.Assert(comboName.Items[idx] is SteamConnection, "comboName doesn't contain SteamConnection at index "+idx);
			SteamConnection con=(SteamConnection) comboName.Items[idx];
			con.Name=comboName.Text;
			con.Address=textAddress.Text;
			con.UserName=textUserName.Text;
			con.Password=textPassword.Text;
			try {
				if (textPort.Text.Length==0) {
					con.Port=0;
				} else {
					con.Port=int.Parse(textPort.Text);
				}
			} catch (FormatException) {
				MessageBox.Show("Port must be valid integer number.", "Invalid port number");
			} catch (OverflowException) {
				MessageBox.Show("Port must be integer number from range 0 to 65535", "Invalid port number");
				textPort.Focus();
			}
			comboName.Items[idx]=con;
			changeEnabled=true;
		}

		private void comboNameSelectedIndexChanged(object sender, System.EventArgs e) {
			int idx=comboName.SelectedIndex;
			if (changeEnabled) {
				changeEnabled=false;
				if (oldComboNameIndex>0) {
					int old=oldComboNameIndex;
					comboName.Text=comboName.Items[old].ToString();
					SaveValuesTo(old);
				}
				LoadValuesFrom(idx);
				changeEnabled=true;
			}
			oldComboNameIndex=idx;
		}

		private void comboNameDropDown(object sender, System.EventArgs e) {
			changeEnabled=false;
			SaveValuesTo(oldComboNameIndex);
			changeEnabled=true;
		}

		private void RemoveConnection(int idx) {
			if (idx<0)
				return;
			int new_idx=idx;
			comboName.Items.RemoveAt(idx);
			if (comboName.Items.Count>0) {
				if (new_idx>comboName.Items.Count-1) {
					new_idx=comboName.Items.Count-1;
				}
				oldComboNameIndex=-1;
				comboName.SelectedIndex=new_idx;
			} else {
				SetInput(false);
				ClearInput();
				oldComboNameIndex=-1;
			}
		}

		private void SetInput(bool enabled) {
			comboName.Enabled=enabled;
			textAddress.Enabled=enabled;
			textPort.Enabled=enabled;
			textUserName.Enabled=enabled;
			textPassword.Enabled=enabled;
			btnConnect.Enabled=enabled;
			btnRemove.Enabled=enabled;
		}

		private void ClearInput() {
			comboName.Text="";
			textAddress.Text="";
			textPort.Text="";
			textUserName.Text="";
			textPassword.Text="";
		}

		private void btnRemoveClick(object sender, System.EventArgs e) {
			RemoveConnection(comboName.SelectedIndex);
		}

		private void btnCancelClick(object sender, System.EventArgs e) {
			shouldConnect=false;
			this.Close();
		}

		private void btnConnectClick(object sender, System.EventArgs e) {
			SaveValuesTo(oldComboNameIndex);
			if (comboName.SelectedIndex>=0) {
				Debug.Assert(comboName.SelectedIndex<comboName.Items.Count, "SelectedIndex out of range");
				shouldConnect=true;
				selectedConnection=(SteamConnection) comboName.Items[comboName.SelectedIndex];
			} else {
				shouldConnect=false;
			}
			this.Close();
		}

		private void textPortKeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e) {
			e.Handled=false;
			if (char.IsControl(e.KeyChar))
				return;
			if (char.IsDigit(e.KeyChar)) {
				if (textPort.Text.Length>=5 && textPort.SelectionLength==0)
					e.Handled=true;
			} else {
				e.Handled=true;
			}
		}

		private void ConnectionFormClosing(object sender, System.ComponentModel.CancelEventArgs e) {
			SaveValuesTo(oldComboNameIndex);
			savePasswords=checkSavePassword.Checked;
		}
	}
}